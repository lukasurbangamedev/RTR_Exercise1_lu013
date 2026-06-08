using UnityEngine;

public class RenderingModeManager : MonoBehaviour
{
    public GameObject renderTexturePlane_normal;
    public GameObject renderTextureCamera_normal;


    [Space(10)]
    public GameObject renderTexturePlane_optimized;
    public GameObject renderTextureCamera_optimized;


    public string renderingMode { get; private set; } = "baseline";


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {


        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            renderingMode = "baseline";

            renderTexturePlane_normal.SetActive(false);
            renderTextureCamera_normal.SetActive(false);

            renderTexturePlane_optimized.SetActive(false);
            renderTextureCamera_optimized.SetActive(false);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            renderingMode = "include_secondary_view";

            renderTexturePlane_normal.SetActive(true);
            renderTextureCamera_normal.SetActive(true);

            renderTexturePlane_optimized.SetActive(false);
            renderTextureCamera_optimized.SetActive(false);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            renderingMode = "include_secondary_view_optimized";

            renderTexturePlane_normal.SetActive(false);
            renderTextureCamera_normal.SetActive(false);

            renderTexturePlane_optimized.SetActive(true);
            renderTextureCamera_optimized.SetActive(true);
        }
    }
}
