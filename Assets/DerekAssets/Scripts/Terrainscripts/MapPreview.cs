using System.Collections;
using UnityEngine;

public class MapPreview : MonoBehaviour
{

    public Wave[] waves;
    public TextureBuilding textureBuilding;
    public Renderer textureRender;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public enum DrawMode { NoiseMap, HeatMap, MoistureMap, UnitMap, Mesh, FalloffMap };
    public DrawMode drawMode;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureData;
    public Material terrainMaterial;

    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int editorPreviewLOD;
    public bool autoUpdate;

    public void DrawMapInEditor()
    {
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings, heightMapSettings, Vector2.zero, editorPreviewLOD);

        //adding terraintypes to build biomes
        TerrainType[,] chosenHeightTerrainTypes = new TerrainType[meshSettings.numVertsPerLine, meshSettings.numVertsPerLine];
        Texture2D heightTexture = textureBuilding.BuildTexture(heightMap.heightvalues, heightMapSettings.noiseSettings.terrainTypes, chosenHeightTerrainTypes);

        TerrainType[,] chosenHeatTerrainTypes = new TerrainType[meshSettings.numVertsPerLine, meshSettings.numVertsPerLine];
        Texture2D heatTexture = textureBuilding.BuildTexture(heightMap.heat, heightMapSettings.noiseSettingsForHeat.terrainTypes, chosenHeatTerrainTypes);

        TerrainType[,] chosenMoistureTerrainTypes = new TerrainType[meshSettings.numVertsPerLine, meshSettings.numVertsPerLine];
        Texture2D moistureTexture = textureBuilding.BuildTexture(heightMap.moisture, heightMapSettings.noiseSettingsForMoisture.terrainTypes, chosenMoistureTerrainTypes);

        TerrainType[,] chosenUnitTerrainTypes = new TerrainType[meshSettings.numVertsPerLine, meshSettings.numVertsPerLine];
        Texture2D unitTexture = textureBuilding.BuildTexture(heightMap.unitvalues, heightMapSettings.noiseSettingsForUnits.terrainTypes, chosenUnitTerrainTypes);

        //       Biome[,] chosenBiomes = new Biome[meshSettings.numVertsPerLine, meshSettings.numVertsPerLine];
        //       Texture2D biomeTexture = biomeBuilding.BuildBiomeTexture(chosenHeightTerrainTypes, chosenHeatTerrainTypes, chosenMoistureTerrainTypes, chosenBiomes);


        if (drawMode == DrawMode.NoiseMap)
        {
            DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
        }
        else if (drawMode == DrawMode.HeatMap)
        {
            DrawTexture(TextureGenerator.TextureFromHeatMap(heightMap));
        }
        else if (drawMode == DrawMode.MoistureMap)
        {
            DrawTexture(TextureGenerator.TextureFromMoistureMap(heightMap));
        }
        else if (drawMode == DrawMode.UnitMap)
        {
            DrawTexture(TextureGenerator.TextureFromUnitMap(heightMap));
        }
        //else if (drawMode == DrawMode.BiomeMap)
        //{
        //    DrawTexture(biomeTexture);
        //}
        else if (drawMode == DrawMode.Mesh)
        {
            //
            DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.heightvalues, heightMap.heat, heightMap.moisture, meshSettings, editorPreviewLOD));
        }
        else if (drawMode == DrawMode.FalloffMap)
        {
            //change the perams once the textures are drawing properly in game
            //DrawTexture (TextureGenerator.TextureFromHeightMap (new HeightMap (FalloffGenerator.GenerateFalloffMap (meshSettings.numVertsPerLine), 0, 1,
            //FalloffGenerator.GenerateFalloffMap (meshSettings.numVertsPerLine), 0, 1), FalloffGenerator.GenerateFalloffMap (meshSettings.numVertsPerLine), 0, 1));
        }
    }

    public void DrawTexture(Texture2D texture)
    {
        textureRender.sharedMaterial.mainTexture = texture;
        textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f;

        textureRender.gameObject.SetActive(true);
        meshFilter.gameObject.SetActive(false);
    }

    public void DrawMesh(MeshData meshData)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();

        textureRender.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);
    }

    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    void OnTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    void OnValidate()
    {

        if (meshSettings != null)
        {
            meshSettings.OnValuesUpdated -= OnValuesUpdated;
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (heightMapSettings != null)
        {
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (textureData != null)
        {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }

    }

}