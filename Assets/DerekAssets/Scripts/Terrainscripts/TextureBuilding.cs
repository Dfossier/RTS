using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureBuilding : MonoBehaviour {

	public Texture2D BuildTexture(float[,] noiseMap, TerrainType[] terrainTypes, TerrainType[,] chosenTerrainTypes) {
		int tileWidth = noiseMap.GetLength (0);
		int tileDepth = noiseMap.GetLength (1);

		Color[] colorMap = new Color[tileDepth * tileWidth];
		for (int xIndex = 0; xIndex < tileWidth; xIndex++) {
			for (int zIndex = 0; zIndex < tileDepth; zIndex++) {
				// transform the 2D map index is an Array index
				int colorIndex = zIndex * tileWidth + xIndex;

				float noise = noiseMap [xIndex, zIndex];

				// choose a terrain type according to the noise value
				TerrainType terrainType = ChooseTerrainType (noise, terrainTypes);

				// assign the color according to the chosen terrain type
				colorMap[colorIndex] = terrainType.color;

				// save the chosen terrain type
				chosenTerrainTypes [xIndex, zIndex] = terrainType;
			}
		}

		// create a new texture and set its pixel colors
		Texture2D tileTexture = new Texture2D (tileWidth, tileDepth);
		tileTexture.wrapMode = TextureWrapMode.Clamp;
		tileTexture.filterMode = FilterMode.Point;
		tileTexture.SetPixels (colorMap);
		tileTexture.Apply ();

		return tileTexture;
	}

	public TerrainType ChooseTerrainType(float noise, TerrainType[] terrainTypes) {
		// for each terrain type, check if the noise is lower than the one for the terrain type
		foreach (TerrainType terrainType in terrainTypes) {
			// return the first terrain type whose noise is higher than the generated one
			if (noise < terrainType.threshold) {
				return terrainType;
			}
		}
		return terrainTypes [terrainTypes.Length - 1];
	}
}

[System.Serializable]
public class TerrainType {
	public int index;
	public string name;
	public float threshold;
	public Color color;
}
