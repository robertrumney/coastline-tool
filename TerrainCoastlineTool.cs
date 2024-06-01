using UnityEngine;
using UnityEditor;

public class TerrainCoastlineTool : EditorWindow
{
    public Terrain[] terrains;
    public float smoothFactor = 0.5f;
    public int brushSize = 10;

    [MenuItem("Tools/Terrain Coastline Tool")]
    public static void ShowWindow()
    {
        GetWindow<TerrainCoastlineTool>("Terrain Coastline Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Terrain Coastline Smoothing Tool", EditorStyles.boldLabel);

        // Smooth Factor
        smoothFactor = EditorGUILayout.Slider("Smooth Factor", smoothFactor, 0.1f, 1.0f);

        // Brush Size
        brushSize = EditorGUILayout.IntSlider("Brush Size", brushSize, 1, 100);

        // Select Terrains
        SerializedObject serializedObject = new SerializedObject(this);
        SerializedProperty terrainsProperty = serializedObject.FindProperty("terrains");
        EditorGUILayout.PropertyField(terrainsProperty, true);
        serializedObject.ApplyModifiedProperties();

        if (GUILayout.Button("Smooth Coastlines"))
        {
            SmoothCoastlines();
        }
    }

    private void SmoothCoastlines()
    {
        foreach (Terrain terrain in terrains)
        {
            if (terrain != null)
            {
                SmoothTerrain(terrain);
            }
        }
        Debug.Log("Coastlines smoothed successfully.");
    }

    private void SmoothTerrain(Terrain terrain)
    {
        TerrainData terrainData = terrain.terrainData;
        float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                float averageHeight = GetAverageHeight(heights, x, y, terrainData.heightmapResolution);
                heights[y, x] = Mathf.Lerp(heights[y, x], averageHeight, smoothFactor);
            }
        }

        terrainData.SetHeights(0, 0, heights);
    }

    private float GetAverageHeight(float[,] heights, int x, int y, int resolution)
    {
        float total = 0f;
        int count = 0;

        for (int yOffset = -brushSize; yOffset <= brushSize; yOffset++)
        {
            for (int xOffset = -brushSize; xOffset <= brushSize; xOffset++)
            {
                int nx = x + xOffset;
                int ny = y + yOffset;

                if (nx >= 0 && ny >= 0 && nx < resolution && ny < resolution)
                {
                    total += heights[ny, nx];
                    count++;
                }
            }
        }

        return total / count;
    }
}
