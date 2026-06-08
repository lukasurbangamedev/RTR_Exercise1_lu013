using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class LogManager_Exercise_Render_Texture : MonoBehaviour
{
    public RenderingModeManager renderingModeManager;
    public Camera mainCamera;
    public Camera renderTextureCamera;


    private void OnValidate()
    {

    }


    string filePath;

    void Start()
    {
        filePath = Application.dataPath + "/render_texture_log.csv";

        File.WriteAllText(filePath, "timestamp|mode|fps|frametime_ms|render_texture_width|render_texture_height|secondary_camera_enabled|secondary_camera_update_rate|draw_calls|batches|tris|visible_object_count\n");
    }


    Quaternion rotatorCurrent;
    Quaternion rotatorLast;
    void Update()
    {
        #region a

        float timestamp = Time.realtimeSinceStartup;
        string mode = renderingModeManager.renderingMode;
        float fps = 1.0f / Time.deltaTime;
        float frametime_ms = Time.deltaTime * 1000f;

        int render_texture_width = 0;
        int render_texture_height = 0;

        if (mode == "include_secondary_view")
        {
            render_texture_width = 2048;
            render_texture_height = 2048;
        }
        if (mode == "include_secondary_view_optimized")
        {
            render_texture_height = 256;
            render_texture_width = 256;
        }

        string secondary_camera_enabled = mode == "baseline" ? "False" : "True";
        string secondary_camera_update_rate = "1:1";
        int drawCalls = UnityEditor.UnityStats.drawCalls;
        int batches = UnityEditor.UnityStats.batches;
        int tris = UnityEditor.UnityStats.triangles;

        int visible_object_count = CountVisibleObjects(mainCamera) + CountVisibleObjects(renderTextureCamera);

        #endregion






        string line = timestamp + "|" + mode + "|" + fps + "|" + frametime_ms + "|" + render_texture_width + "|" + render_texture_height + "|" + secondary_camera_enabled + "|" + secondary_camera_update_rate + "|" + drawCalls + "|" + batches + "|" + tris + "|" + visible_object_count + "\n";
        File.AppendAllText(filePath, line);

    }





    /// <summary>
    /// Returns the count of visible Renderers within the given camera's view frustum.
    /// Checks are done dynamically each call — suitable for per-frame or on-demand use.
    /// </summary>
    /// <param name="camera">The camera whose frustum to test against.</param>
    /// <param name="layerMask">Optional layer mask to filter objects (default: all layers).</param>
    /// <returns>Number of visible (frustum-intersecting, enabled) renderers.</returns>
    public static int CountVisibleObjects(Camera camera, int layerMask = ~0)
    {
        if (camera == null)
        {
            Debug.LogWarning("CountVisibleObjects: camera is null.");
            return 0;
        }

        // Extract the 6 frustum planes from the camera
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);

        // Gather all active renderers in the scene
        Renderer[] allRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        int count = 0;

        foreach (Renderer renderer in allRenderers)
        {
            // Respect layer mask
            if ((layerMask & (1 << renderer.gameObject.layer)) == 0)
                continue;

            // Skip disabled renderers
            if (!renderer.enabled)
                continue;

            // Test the renderer's world-space bounds against the frustum planes
            if (GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds))
                count++;
        }

        return count;
    }
}
