using System.Collections;
using UnityEngine;

public static class TextureGenerator {

	public static Texture2D TextureFromColourMap (Color[] colourMap, int width, int height) {
		Texture2D texture = new Texture2D (width, height);
		texture.filterMode = FilterMode.Point;
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.SetPixels (colourMap);
		texture.Apply ();
		return texture;
	}

	public static Texture2D TextureFromHeightMap (HeightMap heightMap) {
		int width = heightMap.heightvalues.GetLength (0);
		int height = heightMap.heightvalues.GetLength (1);

		Color[] colourMap = new Color[width * height];
		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				colourMap[y * width + x] = Color.Lerp (Color.black, Color.white, Mathf.InverseLerp (heightMap.minHeight, heightMap.maxHeight, heightMap.heightvalues[x, y]));
			}
		}
        return TextureFromColourMap (colourMap, width, height);
	}

    public static Texture2D TextureFromHeatMap(HeightMap heightMap)
    {
        int width = heightMap.heat.GetLength(0);
        int height = heightMap.heat.GetLength(1);

        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colourMap[y * width + x] = Color.Lerp(Color.red, Color.blue, Mathf.InverseLerp(heightMap.minHeat, heightMap.maxHeat, heightMap.heat[x, y]));
            }
        }
        return TextureFromColourMap(colourMap, width, height);
    }

    public static Texture2D TextureFromMoistureMap(HeightMap heightMap)
    {
        int width = heightMap.moisture.GetLength(0);
        int height = heightMap.moisture.GetLength(1);

        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colourMap[y * width + x] = Color.Lerp(Color.red, Color.blue, Mathf.InverseLerp(heightMap.minMoisture, heightMap.maxMoisture, heightMap.moisture[x, y]));
            }
        }
        return TextureFromColourMap(colourMap, width, height);
    }

    public static Texture2D TextureFromUnitMap(HeightMap heightMap)
    {
        int width = heightMap.unitvalues.GetLength(0);
        int height = heightMap.unitvalues.GetLength(1);

        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colourMap[y * width + x] = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(heightMap.minUnit, heightMap.maxUnit, heightMap.unitvalues[x, y]));
            }
        }
        return TextureFromColourMap(colourMap, width, height);
    }

}