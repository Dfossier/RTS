using System.Collections;
using System.Linq;
using UnityEngine;

[CreateAssetMenu()]
public class TextureData : UpdatableData
{

    const int textureSize = 512;
    const TextureFormat textureFormat = TextureFormat.RGB565;

    public LayerHeat[] heats;
    public Layer[] layers;

    float savedMinHeight;
    float savedMaxHeight;
    float savedMinHeat;
    float savedMaxHeat;
    float savedMinMoisture;
    float savedMaxMoisture;

    public void ApplyToMaterial(Material material, HeightMap? heightMap = null)
    {



        material.SetInt("layerCount", layers.Length);
        material.SetColorArray("baseColours", layers.Select(x => x.tint).ToArray());
        material.SetFloatArray("baseStartHeights", layers.Select(x => x.startHeight).ToArray());
        material.SetFloatArray("baseBlends", layers.Select(x => x.blendStrength).ToArray());
        material.SetFloatArray("baseColourStrength", layers.Select(x => x.tintStrength).ToArray());
        material.SetFloatArray("baseTextureScales", layers.Select(x => x.textureScale).ToArray());

        //modified 
        material.SetFloatArray("baseStartHeats", layers.Select(x => x.startHeat).ToArray());
        material.SetFloatArray("baseStartMoistures", layers.Select(x => x.startMoisture).ToArray());

        if (heightMap != null)
        {
            HeightMap _heightMap = (HeightMap)heightMap;
         //   Texture2D heatTexture = _2DArrayToTexture(_heightMap.heat);
        //    Texture2D moistureTexture = _2DArrayToTexture(_heightMap.moisture);
        //    material.SetTexture("HeatLayerTexture", heatTexture);
          //  Debug.Log("Heat Texture Created: " + heatTexture.width + "x" + heatTexture.height);
          //  Debug.Log("Moisture Texture Created: " + moistureTexture.width + "x" + moistureTexture.height);
        }


        Texture2DArray texturesArray = GenerateTextureArray(layers.Select(x => x.texture).ToArray());
        material.SetTexture("baseTextures", texturesArray);

        UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);
    }

    public Texture2D _2DArrayToTexture(float[,] array_2d)
    {

        Texture2D answer = new Texture2D(array_2d.GetLength(0), array_2d.GetLength(1));

        float average = 0;
        for (int y = 0; y < array_2d.GetLength(1); y++)
        {
            for (int x = 0; x < array_2d.GetLength(0); x++)
            {
                average += array_2d[x, y];
                answer.SetPixel(x, y, new Color(1, 1, 1, array_2d[x, y]));
            }
        }

        //Debug.Log("Avarage " + average / (float)(answer.width * answer.height));

        answer.Apply();
        return answer;
    }

    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
    {
        savedMinHeight = minHeight;
        savedMaxHeight = maxHeight;

        //heat
        //moisture

        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);
    }

    Texture2DArray GenerateTextureArray(Texture2D[] textures)
    {
        Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);
        for (int i = 0; i < textures.Length; i++)
        {
            textureArray.SetPixels(textures[i].GetPixels(), i);
        }
        textureArray.Apply();
        return textureArray;
    }

    [System.Serializable]
    public class LayerHeat
    {
        public Layer [] heats;
    }



    [System.Serializable]
    public class Layer
    {
        public Texture2D texture;
        public Color tint;
        [Range(0, 1)]
        public float tintStrength;
        [Range(0, 1)]
        public float startHeight;
        [Range(0, 1)]
        public float blendStrength;
        public float textureScale;

        //modifying 
        [Range(0, 1)]
        public float startHeat;
        [Range(0, 1)]
        public float startMoisture;

    }

}