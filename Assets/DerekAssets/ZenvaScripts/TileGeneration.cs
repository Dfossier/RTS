//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class TileGeneration : MonoBehaviour {

//	[SerializeField]
//	NoiseGeneration noiseGeneration;

//	[SerializeField]
//	TextureBuilding textureBuilding;

//	[SerializeField]
//	private MeshFilter meshFilter;

//	[SerializeField]
//	private MeshRenderer tileRenderer;

//	[SerializeField]
//	private MeshCollider meshCollider;

//	[SerializeField]
//	private float levelScale;

//	[SerializeField]
//	private TerrainType[] heightTerrainTypes;

//	[SerializeField]
//	private Wave[] heightWaves;

//	[SerializeField]
//	private float heightMultiplier;

//	[SerializeField]
//	private AnimationCurve heightCurve;

//	[SerializeField]
//	private TerrainType[] heatTerrainTypes;

//	[SerializeField]
//	private Wave[] heatWaves;

//	[SerializeField]
//	private TerrainType[] moistureTerrainTypes;

//	[SerializeField]
//	private Wave[] moistureWaves;

//	[SerializeField]
//	private VisualizationMode visualizationMode;

//	[SerializeField]
//	private BiomeBuilding biomeBuilding;
	
//	public TileData GenerateTile(float centerVertexZ, float maxDistanceZ) {
//		// calculate tile depth and width based on the mesh vertices
//		Vector3[] meshVertices = this.meshFilter.mesh.vertices;
//		int tileDepth = (int)Mathf.Sqrt (meshVertices.Length);
//		int tileWidth = tileDepth;

//		// calculate the offsets based on the tile position
//		float offsetX = -this.gameObject.transform.position.x;
//		float offsetZ = -this.gameObject.transform.position.z;

//		// generate the height noise map
//		float[,] heightMap = noiseGeneration.GeneratePerlinNoiseMap (tileWidth, tileDepth, levelScale, offsetX, offsetZ, heightWaves);

//		// generate a texture using the heightMap
//		TerrainType[,] chosenHeightTerrainTypes = new TerrainType[tileWidth, tileDepth];
//		Texture2D heightTexture = textureBuilding.BuildTexture (heightMap, heightTerrainTypes, chosenHeightTerrainTypes);

//		// calculate vertex offset based on the Tile position and the distance between vertices
//		Vector3 tileDimensions = this.meshFilter.mesh.bounds.size;
//		float distanceBetweenVertices = tileDimensions.z / (float)tileDepth;
//		float vertexOffsetZ = this.gameObject.transform.position.z / distanceBetweenVertices;

//		// generate a uniformHeatMap using uniform noise
//		float[,] uniformHeatMap = noiseGeneration.GenerateUniformNoiseMap (tileWidth, tileDepth, centerVertexZ, maxDistanceZ, vertexOffsetZ);
//		// generate a randomHeatMap using Perlin Noise
//		float[,] randomHeatMap = noiseGeneration.GeneratePerlinNoiseMap (tileWidth, tileDepth, this.levelScale, offsetX, offsetZ, this.heatWaves);
//		float[,] heatMap = new float[tileWidth, tileDepth];
//		for (int xIndex = 0; xIndex < tileWidth; xIndex++) {
//			for (int zIndex = 0; zIndex < tileDepth; zIndex++) {
//				// mix both heat maps together by multiplying their values
//				heatMap [xIndex, zIndex] = uniformHeatMap [xIndex, zIndex] * randomHeatMap [xIndex, zIndex];
//				TerrainType heightTerrainType = chosenHeightTerrainTypes [xIndex, zIndex];
//				if (heightTerrainType.name == "mountain") {
//					// makes mountains even colder, by using a greater multiplier
//					heatMap [xIndex, zIndex] += 0.8f * heightMap [xIndex, zIndex];
//				} else {
//					// makes higher regions colder, by adding the height value to the heat map
//					heatMap [xIndex, zIndex] += 0.5f * heightMap [xIndex, zIndex];
//				}
//			}
//		}

//		// generate a moistureMap using Perlin Noise
//		float[,] moistureMap = noiseGeneration.GeneratePerlinNoiseMap (tileWidth, tileDepth, this.levelScale, offsetX, offsetZ, this.moistureWaves);
//		for (int xIndex = 0; xIndex < tileWidth; xIndex++) {
//			for (int zIndex = 0; zIndex < tileDepth; zIndex++) {
//				TerrainType heightTerrainType = chosenHeightTerrainTypes [xIndex, zIndex];
//				if (heightTerrainType.name == "water") {
//					// makes it wetter, since it is already water
//					moistureMap [xIndex, zIndex] += 0.7f * heightMap [xIndex, zIndex];
//				} else {
//					// makes higher regions dryer, by reducing the height value from the heat map
//					moistureMap [xIndex, zIndex] -= 0.1f * heightMap [xIndex, zIndex];
//				}
//			}
//		}

//		// generate a texture using the heatMap
//		TerrainType[,] chosenHeatTerrainTypes = new TerrainType[tileWidth, tileDepth];
//		Texture2D heatTexture = textureBuilding.BuildTexture (heatMap, heatTerrainTypes, chosenHeatTerrainTypes);

//		// build a Texture2D from the moisture map
//		TerrainType[,] chosenMoistureTerrainTypes = new TerrainType[tileWidth, tileDepth];
//		Texture2D moistureTexture = textureBuilding.BuildTexture (moistureMap, moistureTerrainTypes, chosenMoistureTerrainTypes);

//		// build a biomes Texture2D from the three other noise variables
//		Biome[,] chosenBiomes = new Biome[tileWidth, tileDepth];
//		Texture2D biomeTexture = biomeBuilding.BuildBiomeTexture (chosenHeightTerrainTypes, chosenHeatTerrainTypes, chosenMoistureTerrainTypes, chosenBiomes);

//		switch (this.visualizationMode) {
//		case VisualizationMode.Height:
//			// assign material texture to be the heightTexture
//			this.tileRenderer.material.mainTexture = heightTexture;
//			break;
//		case VisualizationMode.Heat:
//			// assign material texture to be the heatTexture
//			this.tileRenderer.material.mainTexture = heatTexture;
//			break;
//		case VisualizationMode.Moisture:
//			// assign material texture to be the moistureTexture
//			this.tileRenderer.material.mainTexture = moistureTexture;
//			break;
//		case VisualizationMode.Biome:
//			// assign material texture to be the biomeTexture
//			this.tileRenderer.material.mainTexture = biomeTexture;
//			break;
//		}

//		this.UpdateMeshVertices (heightMap);

//		// create and return a new TileData
//		TileData tileData = new TileData (heightMap, heatMap, moistureMap, 
//			chosenHeightTerrainTypes, chosenHeatTerrainTypes, chosenMoistureTerrainTypes, chosenBiomes, 
//			this.meshFilter.mesh, (Texture2D)this.tileRenderer.material.mainTexture);

//		return tileData;
//	}

//	private void UpdateMeshVertices(float[,] heightMap) {
//		int tileWidth = heightMap.GetLength (0);
//		int tileDepth = heightMap.GetLength (1);

//		Vector3[] meshVertices = this.meshFilter.mesh.vertices;

//		// iterate through all the heightMap coordinates, updating the vertex index
//		for (int xIndex = 0; xIndex < tileWidth; xIndex++) {
//			for (int zIndex = 0; zIndex < tileDepth; zIndex++) {
//				// transform the 2D map index is an Array index
//				int vertexIndex = zIndex * tileWidth + xIndex;

//				float height = heightMap [xIndex, zIndex];

//				Vector3 vertex = meshVertices [vertexIndex];
//				// change the vertex Y coordinate, proportional to the height value. The height value is evaluated by the heightCurve function, in order to correct it.
//				meshVertices[vertexIndex] = new Vector3(vertex.x, this.heightCurve.Evaluate(height) * this.heightMultiplier, vertex.z);
//			}
//		}

//		// update the vertices in the mesh and update its properties
//		this.meshFilter.mesh.vertices = meshVertices;
//		this.meshFilter.mesh.RecalculateBounds ();
//		this.meshFilter.mesh.RecalculateNormals ();
//		// update the mesh collider
//		this.meshCollider.sharedMesh = this.meshFilter.mesh;
//	}
//}

//// class to store all data for a single tile
//public class TileData {
//	public float[,]  heightMap;
//	public float[,]  heatMap;
//	public float[,]  moistureMap;
//	public TerrainType[,] chosenHeightTerrainTypes;
//	public TerrainType[,] chosenHeatTerrainTypes;
//	public TerrainType[,] chosenMoistureTerrainTypes;
//	public Biome[,] chosenBiomes;
//	public Mesh mesh;
//	public Texture2D texture;

//	public TileData(float[,]  heightMap, float[,]  heatMap, float[,]  moistureMap, 
//		TerrainType[,] chosenHeightTerrainTypes, TerrainType[,] chosenHeatTerrainTypes, TerrainType[,] chosenMoistureTerrainTypes,
//		Biome[,] chosenBiomes, Mesh mesh, Texture2D texture) {
//		this.heightMap = heightMap;
//		this.heatMap = heatMap;
//		this.moistureMap = moistureMap;
//		this.chosenHeightTerrainTypes = chosenHeightTerrainTypes;
//		this.chosenHeatTerrainTypes = chosenHeatTerrainTypes;
//		this.chosenMoistureTerrainTypes = chosenMoistureTerrainTypes;
//		this.chosenBiomes = chosenBiomes;
//		this.mesh = mesh;
//		this.texture = texture;
//	}
//}

//enum VisualizationMode {Height, Heat, Moisture, Biome}