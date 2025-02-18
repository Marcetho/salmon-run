using UnityEngine;
using UnityEditor;
using System.IO;

public class WaterTextureGenerator : EditorWindow
{
    private int textureSize = 512;
    private float noiseScale = 20f;
    private float normalStrength = 2f;

    [MenuItem("Tools/Generate Water Normal Map")]
    public static void ShowWindow()
    {
        GetWindow<WaterTextureGenerator>("Water Texture Generator");
    }

    private void OnGUI()
    {
        textureSize = EditorGUILayout.IntField("Texture Size", textureSize);
        noiseScale = EditorGUILayout.FloatField("Noise Scale", noiseScale);
        normalStrength = EditorGUILayout.FloatField("Normal Strength", normalStrength);

        if (GUILayout.Button("Generate Normal Map"))
        {
            GenerateNormalMap();
        }
    }

    private void GenerateNormalMap()
    {
        // Create height map using Perlin noise
        float[,] heightMap = new float[textureSize, textureSize];
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float xCoord = (float)x / textureSize * noiseScale;
                float yCoord = (float)y / textureSize * noiseScale;
                heightMap[x, y] = Mathf.PerlinNoise(xCoord, yCoord);
            }
        }

        // Create normal map texture
        Texture2D normalMap = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);

        // Calculate normals
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                // Sample neighboring heights
                float left = heightMap[(x - 1 + textureSize) % textureSize, y];
                float right = heightMap[(x + 1) % textureSize, y];
                float top = heightMap[x, (y + 1) % textureSize];
                float bottom = heightMap[x, (y - 1 + textureSize) % textureSize];

                // Calculate normal
                Vector3 normal = new Vector3(
                    (left - right) * normalStrength,
                    (bottom - top) * normalStrength,
                    1.0f
                ).normalized;

                // Convert from -1:1 to 0:1 range
                Color normalColor = new Color(
                    normal.x * 0.5f + 0.5f,
                    normal.y * 0.5f + 0.5f,
                    normal.z
                );

                normalMap.SetPixel(x, y, normalColor);
            }
        }

        normalMap.Apply();

        // Save texture
        byte[] bytes = normalMap.EncodeToPNG();
        string path = "Assets/Textures/WaterNormal.png";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, bytes);
        AssetDatabase.Refresh();

        // Set texture import settings
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
        }

        Debug.Log("Normal map generated at: " + path);
    }
}
