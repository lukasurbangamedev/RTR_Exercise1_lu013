using UnityEngine;
using UnityEditor;

public class GridDuplicator : EditorWindow
{
    private int countX = 10;
    private int countZ = 10;
    private float spacing = 2f;

    [MenuItem("Tools/Grid Duplicator")]
    public static void ShowWindow()
    {
        GetWindow<GridDuplicator>("Grid Duplicator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Duplicate Selected Object in Grid", EditorStyles.boldLabel);

        countX = EditorGUILayout.IntField("Count X", countX);
        countZ = EditorGUILayout.IntField("Count Z", countZ);
        spacing = EditorGUILayout.FloatField("Spacing", spacing);

        if (GUILayout.Button("Generate Grid"))
        {
            GenerateGrid();
        }
    }

    private void GenerateGrid()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogError("No GameObject selected!");
            return;
        }

        GameObject original = Selection.activeGameObject;

        Undo.RegisterFullObjectHierarchyUndo(original, "Grid Duplicate");

        for (int x = 0; x < countX; x++)
        {
            for (int z = 0; z < countZ; z++)
            {
                // Skip the original position
                if (x == 0 && z == 0) continue;

                Vector3 offset = new Vector3(x * spacing, 0, z * spacing);
                GameObject clone = (GameObject)PrefabUtility.InstantiatePrefab(original);

                if (clone == null)
                {
                    clone = Instantiate(original);
                }

                clone.transform.position = original.transform.position + offset;
                clone.transform.rotation = original.transform.rotation;
                clone.transform.localScale = original.transform.localScale;

                Undo.RegisterCreatedObjectUndo(clone, "Duplicate Grid Object");
            }
        }
    }
}