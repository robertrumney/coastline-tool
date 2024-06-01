using UnityEngine;
using UnityEditor;

public class TerrainCoastlineTool : EditorWindow
{
    public Terrain[] terrains;
    public float smoothFactor = 0.5f;
    public int brushSize = 10;
    public int blendDistance = 5; // Distance to blend nearby vertices

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

        // Blend Distance
        blendDistance = EditorGUILayout.IntSlider("Blend Distance", blendDistance, 1, 50);

        // Select Terrains
        SerializedObject serializedObject = new SerializedObject(this);
        SerializedProperty terrainsProperty = serializedObject.FindProperty("terrains");
        EditorGUILayout.PropertyField(terrainsProperty, true);
        serializedObject.ApplyModifiedProperties();

        if (GUILayout.Button("Smooth Coastlines"))
        {
            SmoothCoastlines();
        }

        if (GUILayout.Button("Stitch Terrains"))
        {
            StitchTerrains();
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

    private void StitchTerrains()
    {
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrainA = terrains[i];
            if (terrainA == null) continue;

            for (int j = i + 1; j < terrains.Length; j++)
            {
                Terrain terrainB = terrains[j];
                if (terrainB == null) continue;

                if (AreTerrainsAdjacent(terrainA, terrainB))
                {
                    StitchTerrainPair(terrainA, terrainB);
                }
            }
        }
        Debug.Log("Terrains stitched successfully.");
    }

    private bool AreTerrainsAdjacent(Terrain terrainA, Terrain terrainB)
    {
        Vector3 positionA = terrainA.transform.position;
        Vector3 positionB = terrainB.transform.position;
        Vector3 sizeA = terrainA.terrainData.size;

        bool adjacentX = Mathf.Approximately(positionA.x + sizeA.x, positionB.x) || Mathf.Approximately(positionB.x + sizeA.x, positionA.x);
        bool adjacentZ = Mathf.Approximately(positionA.z + sizeA.z, positionB.z) || Mathf.Approximately(positionB.z + sizeA.z, positionA.z);

        return adjacentX || adjacentZ;
    }

    private void StitchTerrainPair(Terrain terrainA, Terrain terrainB)
    {
        TerrainData terrainDataA = terrainA.terrainData;
        TerrainData terrainDataB = terrainB.terrainData;

        int resolution = terrainDataA.heightmapResolution;
        float[,] heightsA = terrainDataA.GetHeights(0, 0, resolution, resolution);
        float[,] heightsB = terrainDataB.GetHeights(0, 0, resolution, resolution);

        for (int x = 0; x < resolution; x++)
        {
            if (AreTerrainsAdjacentOnXAxis(terrainA, terrainB))
            {
                for (int d = 0; d < blendDistance; d++)
                {
                    float t = (float)d / blendDistance;
                    float blendedHeight = Mathf.Lerp(heightsA[x, resolution - 1 - d], heightsB[x, d], t);
                    heightsA[x, resolution - 1 - d] = blendedHeight;
                    heightsB[x, d] = blendedHeight;
                }
            }
        }

        for (int z = 0; z < resolution; z++)
        {
            if (AreTerrainsAdjacentOnZAxis(terrainA, terrainB))
            {
                for (int d = 0; d < blendDistance; d++)
                {
                    float t = (float)d / blendDistance;
                    float blendedHeight = Mathf.Lerp(heightsA[resolution - 1 - d, z], heightsB[d, z], t);
                    heightsA[resolution - 1 - d, z] = blendedHeight;
                    heightsB[d, z] = blendedHeight;
                }
            }
        }

        terrainDataA.SetHeights(0, 0, heightsA);
        terrainDataB.SetHeights(0, 0, heightsB);
    }

    private bool AreTerrainsAdjacentOnXAxis(Terrain terrainA, Terrain terrainB)
    {
        Vector3 positionA = terrainA.transform.position;
        Vector3 positionB = terrainB.transform.position;
        Vector3 sizeA = terrainA.terrainData.size;

        return Mathf.Approximately(positionA.x + sizeA.x, positionB.x) || Mathf.Approximately(positionB.x + sizeA.x, positionA.x);
    }

    private bool AreTerrainsAdjacentOnZAxis(Terrain terrainA, Terrain terrainB)
    {
        Vector3 positionA = terrainA.transform.position;
        Vector3 positionB = terrainB.transform.position;
        Vector3 sizeA = terrainA.terrainData.size;

        return Mathf.Approximately(positionA.z + sizeA.z, positionB.z) || Mathf.Approximately(positionB.z + sizeA.z, positionA.z);
    }
}
