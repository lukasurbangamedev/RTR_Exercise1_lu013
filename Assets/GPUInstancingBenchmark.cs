using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


public enum EMaterialMode
{
    Single = 0, 
    Single_Expensive =1,
    Ten =2,
    Unique=3
    
}


[AddComponentMenu("Benchmarks/GPU Instancing Benchmark")]
public class GPUInstancingBenchmark : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Mesh & Material")]
    [Tooltip("Any mesh. Tip: create a default cube, copy its MeshFilter.sharedMesh here, then delete the cube.")]
    public Mesh meshRef;

    private Mesh _mesh;

    [Tooltip("Source URP material. A runtime copy is made — your asset is never modified.")]
    public Material sourceMaterial;

    public Material matExpensive;


    [Header("Grid")]
    [Range(1, 250)]
    public int gridSize = 10;
    public float spacing = 1.5f;

    [Header("Mode")]

    public EMaterialMode materialMode = EMaterialMode.Single;

    private Material[] _materials10;
    private Material[] _perObjectMaterials;


    [Tooltip("Toggled at runtime with the I key.")]
    public bool useGPUInstancing = true;


    public bool useFrustumCulling = true;


    [Header("Controls")]
    public KeyCode increaseKey = KeyCode.UpArrow;
    public KeyCode decreaseKey = KeyCode.DownArrow;
    public KeyCode toggleMatKey = KeyCode.RightArrow;
    public KeyCode toggleInstancingKey = KeyCode.I;
    public KeyCode rebuildKey = KeyCode.R;
    public int minGridSize = 1;
    public int maxGridSize = 250;

    // ── Private ───────────────────────────────────────────────────────────────
    private const int MaxBatchSize = 1023; // Unity hard limit for DrawMeshInstanced

    // Two runtime material copies — the source asset is never touched.
    private Material _matInstanced;  // enableInstancing = true
    private Material _matBaseline;   // enableInstancing = false  ← prevents auto-instancing
    private Material _matExpensive;

    private Matrix4x4[] _matrices;
    private Matrix4x4[] _scratch;    // reused overflow buffer for batches after the first
    public int _instanceCount { get; private set; }
    private const int _submeshIndex = 0;
    [SerializeField] private Camera _mainCam;

    // Shared MPB avoids per-draw heap allocations.
    private MaterialPropertyBlock _mpb;

    // ── GUI ───────────────────────────────────────────────────────────────────
    private GUIStyle _labelStyle;
    private readonly Rect _guiRect = new Rect(10, 10, 430, 200);

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _mesh = Instantiate(meshRef);

        if (!useFrustumCulling) _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1e18f);

        // test. This renders fine.
        //        _mesh = new Mesh();
        //        _mesh.vertices = new Vector3[] {
        //    new(-0.5f,0,-0.5f), new(0.5f,0,-0.5f),
        //    new(0.5f,0,0.5f),   new(-0.5f,0,0.5f)
        //};
        //        _mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        //        _mesh.RecalculateNormals();
        //        _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1e18f);


        _mpb = new MaterialPropertyBlock();

        if (sourceMaterial == null)
        {
            Debug.LogError("[GPUInstancingBenchmark] No source material assigned!");
            enabled = false;
            return;
        }

        // Create two independent runtime copies so the original asset is never dirtied.
        _matInstanced = new Material(sourceMaterial) { name = sourceMaterial.name + "_INSTANCED" };
        Debug.Log(_matInstanced.shader.name);
        _matBaseline = new Material(sourceMaterial) { name = sourceMaterial.name + "_BASELINE" };
        Debug.Log(_matBaseline.shader.name);

        _matExpensive = new Material(matExpensive) { name = sourceMaterial.name + "_EXPENSIVE" };


        // Hard-lock each copy into its instancing state for the lifetime of this session.
        _matInstanced.enableInstancing = true;
        _matBaseline.enableInstancing = false; // this is the actual fix
        _matExpensive.enableInstancing = false;


        GenerateMaterials();

        RebuildGrid();
    }

    private void GenerateMaterials()
    {
        // -----------------------------
        // 10 SHARED MATERIALS (still slightly batchable within group)
        // -----------------------------
        _materials10 = new Material[10];

        for (int i = 0; i < 10; i++)
        {
            var mat = new Material(sourceMaterial)
            {
                name = sourceMaterial.name + $"_VAR_{i}"
            };

            // still useful visual differentiation
            mat.color = Color.HSVToRGB(i / 10f, 0.8f, 1f);

            // small keyword variation helps break SRP batching further
            if (i % 2 == 0)
                mat.EnableKeyword("VARIANT_A");
            else
                mat.EnableKeyword("VARIANT_B");

            // UNIQUE procedural texture per material (this is the big breaker)
            Texture2D tex = GenerateNoiseTexture(32, 32, i * 1337);
            mat.mainTexture = tex;

            mat.enableInstancing = false;

            _materials10[i] = mat;
        }

        // -----------------------------
        // PER OBJECT MATERIALS (FULL STRESS TEST)
        // -----------------------------
        _perObjectMaterials = new Material[_instanceCount];

        for (int i = 0; i < _instanceCount; i++)
        {
            var mat = new Material(sourceMaterial)
            {
                name = sourceMaterial.name + $"_OBJ_{i}"
            };

            mat.color = Color.HSVToRGB((i * 0.618033f) % 1f, 0.95f, 1f);

            // 🔥 unique GPU texture per object (this kills batching hard)
            Texture2D tex = GenerateNoiseTexture(16, 16, i * 9781);
            mat.mainTexture = tex;

            // optional extra entropy: shader keyword toggles
            if ((i & 1) == 0)
                mat.EnableKeyword("UNI_A");
            else
                mat.EnableKeyword("UNI_B");

            _perObjectMaterials[i] = mat;

            mat.enableInstancing = false;
        }
    }

    private Texture2D GenerateNoiseTexture(int width, int height, int seed)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Point;

        System.Random rng = new System.Random(seed);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float v = (float)rng.NextDouble();

                Color c = new Color(
                    v,
                    (float)rng.NextDouble(),
                    (float)rng.NextDouble(),
                    1f
                );

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply(false, false);
        return tex;
    }

    private void Start()
    {
        if (!_mainCam)
            _mainCam = Camera.main;

        // Fallback: find ANY camera in the scene
        if (_mainCam == null)
            _mainCam = FindFirstObjectByType<Camera>();

        if (_mainCam == null)
            throw new Exception("No camera found in scene!");

        Debug.Log($"[Benchmark] Using camera: {_mainCam.name}, tag: {_mainCam.tag}");

    }

    void OnDestroy()
    {
        if (_matInstanced) Destroy(_matInstanced);
        if (_matBaseline) Destroy(_matBaseline);
        if (_mesh) Destroy(_mesh);

        if (_materials10 != null)
            foreach (var m in _materials10)
                Destroy(m);

        if (_perObjectMaterials != null)
            foreach (var m in _perObjectMaterials)
                Destroy(m);
    }

    // ── Update ────────────────────────────────────────────────────────────────
    void Update()
    {
        UpdateVisibilityStats();
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
        if (Input.GetKeyDown(toggleMatKey))
        {
            materialMode = (EMaterialMode)(((int)materialMode + 1) % 4);
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

        if (materialMode == EMaterialMode.Unique)
        {
            GenerateMaterials();
        }
    }

    // ── Rendering (zero GC per frame) ─────────────────────────────────────────
    void Render()
    {
        if (_mesh == null) return;

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
                _mesh,
                _submeshIndex,
                _matInstanced,
                src,
                count,
                _mpb,
                ShadowCastingMode.On,
                receiveShadows: true,
                layer: gameObject.layer,
                camera: null);

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
            Material mat = _matBaseline;

            switch (materialMode)
            {
                case EMaterialMode.Single:
                    mat = _matBaseline;
                    break;

                case EMaterialMode.Single_Expensive:
                    mat = _matExpensive;
                    break;

                case EMaterialMode.Ten:
                    int idx10 = i % 10;
                    mat = _materials10[idx10];
                    break;

                case EMaterialMode.Unique:
                    mat = _perObjectMaterials[i];
                    break;
            }

            Graphics.DrawMesh(
                _mesh,
                _matrices[i],
                mat,
                gameObject.layer,
                null,
                _submeshIndex,
                _mpb,
                true,
                receiveShadows: true,
                useLightProbes: true);
        }

    }


    public int _visibleCount { get; private set; }
    public int _culledCount { get; private set; }


    //    // ── HUD ───────────────────────────────────────────────────────────────────
    //    void OnGUI()
    //    {
    //        if (_labelStyle == null)
    //        {
    //            _labelStyle = new GUIStyle(GUI.skin.label)
    //            {
    //                fontSize = 14,
    //                fontStyle = FontStyle.Bold,
    //                richText = true
    //            };
    //        }

    //        GUI.Box(_guiRect, GUIContent.none);
    //        GUILayout.BeginArea(new Rect(_guiRect.x + 8, _guiRect.y + 8,
    //                                     _guiRect.width - 16, _guiRect.height - 16));

    //        string modeLabel = useGPUInstancing
    //            ? "<color=#00ff88>GPU INSTANCING  ON   (DrawMeshInstanced)</color>"
    //            : "<color=#ff6644>GPU INSTANCING  OFF  (SRP Batcher baseline)</color>";

    //        GUILayout.Label(modeLabel, _labelStyle);
    //        GUILayout.Label($"Grid  {gridSize} × {gridSize}  =  {_instanceCount:N0} objects", _labelStyle);
    //        GUILayout.Label($"FPS   {1f / Time.smoothDeltaTime:F1}   |   " +
    //                        $"ms  {Time.smoothDeltaTime * 1000f:F2}", _labelStyle);
    //        GUILayout.Space(2);
    //        GUILayout.Label(
    //            $"[{increaseKey}/{decreaseKey}] resize    [{toggleInstancingKey}] toggle    [{rebuildKey}] rebuild",
    //            _labelStyle);

    //        GUILayout.EndArea();
    //    }

    //    // ── Editor gizmo ──────────────────────────────────────────────────────────
    //#if UNITY_EDITOR
    //    void OnDrawGizmosSelected()
    //    {
    //        if (!Application.isPlaying) return;
    //        Gizmos.color = new Color(0f, 1f, 0.5f, 0.12f);
    //        float size = gridSize * spacing;
    //        Gizmos.DrawWireCube(transform.position, new Vector3(size, 0.02f, size));
    //    }
    //#endif


    private readonly Plane[] _frustumPlanes = new Plane[6];


    void UpdateVisibilityStats()
    {
        if (_mainCam == null || _matrices == null) return;

        GeometryUtility.CalculateFrustumPlanes(_mainCam, _frustumPlanes);

        int visible = 0;

        for (int i = 0; i < _instanceCount; i++)
        {
            // Extract position from matrix
            Vector3 pos = _matrices[i].GetColumn(3);

            // Approximate bounds (adjust to your mesh size if needed)
            Bounds bounds = new Bounds(pos, Vector3.one);

            if (GeometryUtility.TestPlanesAABB(_frustumPlanes, bounds))
                visible++;
        }

        _visibleCount = useFrustumCulling ? visible : _instanceCount;    
        _culledCount = useFrustumCulling ? (_instanceCount - visible) : 0;
    }

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

        GUILayout.BeginArea(new Rect(
            _guiRect.x + 8,
            _guiRect.y + 8,
            _guiRect.width - 16,
            _guiRect.height - 16));

        string modeLabel = useGPUInstancing
            ? "<color=#00ff88>GPU INSTANCING  ON   (DrawMeshInstanced)</color>"
            : "<color=#ff6644>GPU INSTANCING  OFF  (SRP Batcher baseline)</color>";

        GUILayout.Label(modeLabel, _labelStyle);

        string modeLabel2 = useFrustumCulling
            ? "<color=#00ff88>FRUSTUM CULLING  ON</color>"
            : "<color=#ff6644>FRUSTUM CULLING OFF  (SRP Batcher baseline)</color>";

        GUILayout.Label(modeLabel2, _labelStyle);


        GUILayout.Label(
            $"Grid  {gridSize} × {gridSize}  =  {_instanceCount:N0} objects",
            _labelStyle);

        GUILayout.Label(
            $"Visible  {(useFrustumCulling ? _visibleCount : _instanceCount):N0}   |   Culled  {(useFrustumCulling ? _culledCount : 0):N0}",
            _labelStyle);

        GUILayout.Label(
            $"FPS   {1f / Time.smoothDeltaTime:F1}   |   ms  {Time.smoothDeltaTime * 1000f:F2}",
            _labelStyle);

        GUILayout.Space(2);

        GUILayout.Label(
            $"[{increaseKey}/{decreaseKey}] resize    [{toggleInstancingKey}] toggle    [{rebuildKey}] rebuild",
            _labelStyle);

        GUILayout.EndArea();
    }

}