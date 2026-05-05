using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// GPU Instancing Benchmark — Unity URP
///
/// ROOT CAUSE OF THE ORIGINAL BUG:
///   Unity auto-instances DrawMesh calls that share the same Material when
///   Material.enableInstancing == true, regardless of which Draw API you use.
///   The ONLY reliable way to prevent instancing is to set
///   Material.enableInstancing = false on the material passed to the draw call.
///   This script keeps TWO runtime material instances (never touching your asset):
///     _matInstanced  — enableInstancing forced TRUE
///     _matBaseline   — enableInstancing forced FALSE  (SRP Batcher path in URP)
///
/// SETUP:
///   1. Attach to an empty GameObject.
///   2. Assign any Mesh (e.g. extract sharedMesh from a cube).
///   3. Assign a URP/Lit Material (the original asset is never modified).
///   4. Press Play.
///
/// CONTROLS:
///   ↑ / ↓   — grow / shrink grid  (x × x)
///   I        — toggle GPU Instancing ON ↔ OFF
///   R        — force rebuild
///
/// RENDER PATHS:
///   ON  → Graphics.DrawMeshInstanced in 1023-instance batches.
///          Keyword INSTANCING_ON will be active. One instanced draw call per batch.
///   OFF → Graphics.DrawMesh per object, material.enableInstancing = false.
///          Prevents all auto-instancing. URP SRP Batcher handles merging instead.
///          No INSTANCING_ON keyword visible in Frame Debugger.
///
/// PERFORMANCE NOTES:
///   • Zero GameObjects — no Transform / Renderer overhead.
///   • Matrix array pre-allocated, only grown never shrunk.
///   • Scratch buffer for overflow batches > 1023, allocated once.
///   • Zero GC allocs per frame after first RebuildGrid().
/// </summary>
[AddComponentMenu("Benchmarks/GPU Instancing Benchmark")]
public class GPUInstancingBenchmark : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Mesh & Material")]
    [Tooltip("Any mesh. Tip: create a default cube, copy its MeshFilter.sharedMesh here, then delete the cube.")]
    public Mesh mesh;

    [Tooltip("Source URP material. A runtime copy is made — your asset is never modified.")]
    public Material sourceMaterial;

    [Header("Grid")]
    [Range(1, 250)]
    public int gridSize = 10;
    public float spacing = 1.5f;

    [Header("Mode")]
    [Tooltip("Toggled at runtime with the I key.")]
    public bool useGPUInstancing = true;

    [Header("Controls")]
    public KeyCode increaseKey = KeyCode.UpArrow;
    public KeyCode decreaseKey = KeyCode.DownArrow;
    public KeyCode toggleInstancingKey = KeyCode.I;
    public KeyCode rebuildKey = KeyCode.R;
    public int minGridSize = 1;
    public int maxGridSize = 250;

    // ── Private ───────────────────────────────────────────────────────────────
    private const int MaxBatchSize = 1023; // Unity hard limit for DrawMeshInstanced

    // Two runtime material copies — the source asset is never touched.
    private Material _matInstanced;  // enableInstancing = true
    private Material _matBaseline;   // enableInstancing = false  ← prevents auto-instancing

    private Matrix4x4[] _matrices;
    private Matrix4x4[] _scratch;    // reused overflow buffer for batches after the first
    public int _instanceCount { get; private set; }
    private const int _submeshIndex = 0;
    private Camera _mainCam;

    // Shared MPB avoids per-draw heap allocations.
    private MaterialPropertyBlock _mpb;

    // ── GUI ───────────────────────────────────────────────────────────────────
    private GUIStyle _labelStyle;
    private readonly Rect _guiRect = new Rect(10, 10, 430, 120);

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _mainCam = Camera.main;
        _mpb = new MaterialPropertyBlock();

        if (sourceMaterial == null)
        {
            Debug.LogError("[GPUInstancingBenchmark] No source material assigned!");
            enabled = false;
            return;
        }

        // Create two independent runtime copies so the original asset is never dirtied.
        _matInstanced = new Material(sourceMaterial) { name = sourceMaterial.name + "_INSTANCED" };
        _matBaseline = new Material(sourceMaterial) { name = sourceMaterial.name + "_BASELINE" };

        // Hard-lock each copy into its instancing state for the lifetime of this session.
        _matInstanced.enableInstancing = true;
        _matBaseline.enableInstancing = false; // this is the actual fix

        RebuildGrid();
    }

    void OnDestroy()
    {
        if (_matInstanced) Destroy(_matInstanced);
        if (_matBaseline) Destroy(_matBaseline);
    }

    // ── Update ────────────────────────────────────────────────────────────────
    void Update()
    {
        HandleInput();
        Render();
    }

    // ── Input ─────────────────────────────────────────────────────────────────
    void HandleInput()
    {
        bool rebuild = false;

        if (Input.GetKeyDown(increaseKey))
        {
            gridSize = Mathf.Min(gridSize + 1, maxGridSize);
            rebuild = true;
        }
        if (Input.GetKeyDown(decreaseKey))
        {
            gridSize = Mathf.Max(gridSize - 1, minGridSize);
            rebuild = true;
        }
        if (Input.GetKeyDown(toggleInstancingKey))
        {
            useGPUInstancing = !useGPUInstancing;
            // No matrix rebuild needed — grid layout is unchanged.
        }
        if (Input.GetKeyDown(rebuildKey))
        {
            rebuild = true;
        }

        if (rebuild) RebuildGrid();
    }

    // ── Grid ──────────────────────────────────────────────────────────────────
    void RebuildGrid()
    {
        _instanceCount = gridSize * gridSize;

        // Only reallocate if we need a larger buffer.
        if (_matrices == null || _matrices.Length < _instanceCount)
            _matrices = new Matrix4x4[_instanceCount];

        float halfExtent = (gridSize - 1) * spacing * 0.5f;
        int idx = 0;

        for (int row = 0; row < gridSize; row++)
            for (int col = 0; col < gridSize; col++)
            {
                _matrices[idx++] = Matrix4x4.TRS(
                    new Vector3(col * spacing - halfExtent, 0f, row * spacing - halfExtent),
                    Quaternion.identity,
                    Vector3.one);
            }
    }

    // ── Rendering (zero GC per frame) ─────────────────────────────────────────
    void Render()
    {
        if (mesh == null) return;

        if (useGPUInstancing)
            RenderInstanced();
        else
            RenderBaseline();
    }

    /// GPU Instancing path.
    /// DrawMeshInstanced has no offset parameter, so we copy overflow slices
    /// into a reused scratch buffer rather than allocating new arrays.
    void RenderInstanced()
    {
        int remaining = _instanceCount;
        int offset = 0;

        while (remaining > 0)
        {
            int count = Mathf.Min(remaining, MaxBatchSize);

            // First batch can use _matrices directly (offset == 0).
            // Subsequent batches need a copy into the scratch buffer.
            Matrix4x4[] src;
            if (offset == 0)
            {
                src = _matrices;
            }
            else
            {
                if (_scratch == null || _scratch.Length < count)
                    _scratch = new Matrix4x4[count];

                System.Array.Copy(_matrices, offset, _scratch, 0, count);
                src = _scratch;
            }

            Graphics.DrawMeshInstanced(
                mesh,
                _submeshIndex,
                _matInstanced,
                src,
                count,
                _mpb,
                ShadowCastingMode.On,
                receiveShadows: true,
                layer: gameObject.layer,
                camera: _mainCam);

            offset += count;
            remaining -= count;
        }
    }

    /// Baseline path — one DrawMesh call per instance.
    /// _matBaseline.enableInstancing = false ensures Unity cannot auto-instance
    /// these calls, so INSTANCING_ON will NOT appear in the Frame Debugger.
    /// URP's SRP Batcher will still merge compatible draws (correct baseline).
    void RenderBaseline()
    {
        for (int i = 0; i < _instanceCount; i++)
        {
            Graphics.DrawMesh(
                mesh,
                _matrices[i],
                _matBaseline,
                gameObject.layer,
                _mainCam,
                _submeshIndex,
                _mpb,
                true,
                receiveShadows: true,
                useLightProbes: true);
        }
    }

    // ── HUD ───────────────────────────────────────────────────────────────────
    void OnGUI()
    {
        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                richText = true
            };
        }

        GUI.Box(_guiRect, GUIContent.none);
        GUILayout.BeginArea(new Rect(_guiRect.x + 8, _guiRect.y + 8,
                                     _guiRect.width - 16, _guiRect.height - 16));

        string modeLabel = useGPUInstancing
            ? "<color=#00ff88>GPU INSTANCING  ON   (DrawMeshInstanced)</color>"
            : "<color=#ff6644>GPU INSTANCING  OFF  (SRP Batcher baseline)</color>";

        GUILayout.Label(modeLabel, _labelStyle);
        GUILayout.Label($"Grid  {gridSize} × {gridSize}  =  {_instanceCount:N0} objects", _labelStyle);
        GUILayout.Label($"FPS   {1f / Time.smoothDeltaTime:F1}   |   " +
                        $"ms  {Time.smoothDeltaTime * 1000f:F2}", _labelStyle);
        GUILayout.Space(2);
        GUILayout.Label(
            $"[{increaseKey}/{decreaseKey}] resize    [{toggleInstancingKey}] toggle    [{rebuildKey}] rebuild",
            _labelStyle);

        GUILayout.EndArea();
    }

    // ── Editor gizmo ──────────────────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.12f);
        float size = gridSize * spacing;
        Gizmos.DrawWireCube(transform.position, new Vector3(size, 0.02f, size));
    }
#endif
}