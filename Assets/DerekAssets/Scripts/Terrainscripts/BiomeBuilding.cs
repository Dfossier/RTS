//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class BiomeBuilding : MonoBehaviour {

//	[SerializeField]
//	private BiomeRow[] biomes;

//	[SerializeField]
//	private Color waterColor;

//	public Texture2D BuildBiomeTexture(TerrainType[,] heightTerrainTypes, TerrainType[,] heatTerrainTypes, TerrainType[,] moistureTerrainTypes, Biome[,] chosenBiomes) {
//		int tileWidth = heatTerrainTypes.GetLength (0);
//		int tileDepth = heatTerrainTypes.GetLength (1);

//		Color[] colorMap = new Color[tileWidth * tileDepth];
//		for (int xIndex = 0; xIndex < tileWidth; xIndex++) {
//			for (int zIndex = 0; zIndex < tileDepth; zIndex++) {
//				int colorIndex = zIndex * tileWidth + xIndex;

//				TerrainType heightTerrainType = heightTerrainTypes [xIndex, zIndex];
//				// check if the current coordinate is a water region
//				if (heightTerrainType.name != "water") {
//					// if a coordinate is not water, its biome will be defined by the heat and moisture values
//					TerrainType heatTerrainType = heatTerrainTypes [xIndex, zIndex];
//					TerrainType moistureTerrainType = moistureTerrainTypes [xIndex, zIndex];

//					// terrain type index is used to access the biomes table
//					Biome biome = this.biomes [moistureTerrainType.index].biomes [heatTerrainType.index];
//					// assign the color according to the selected biome
//					colorMap [colorIndex] = biome.color;

//					// save biome in chosenBiomes matrix only when it is not water
//					chosenBiomes [xIndex, zIndex] = biome;
//				} else {
//					// water regions don't have biomes, they always have the same color
//					colorMap [colorIndex] = this.waterColor;
//				}
//			}
//		}

//		// create a new texture and set its pixel colors
//		Texture2D tileTexture = new Texture2D (tileWidth, tileDepth);
//		tileTexture.wrapMode = TextureWrapMode.Clamp;
//		tileTexture.filterMode = FilterMode.Point;
//		tileTexture.SetPixels (colorMap);
//		tileTexture.Apply ();
        
//		return tileTexture;
//	}

//}

//[System.Serializable]
//public class Biome {
//	public int index;
//	public string name;
//	public Color color;
//}

//[System.Serializable]
//public class BiomeRow {
//	public Biome[] biomes;
//}