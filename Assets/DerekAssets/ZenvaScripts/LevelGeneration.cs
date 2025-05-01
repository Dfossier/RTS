using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public class LevelGeneration : MonoBehaviour {

//	[SerializeField]
//	private int levelWidthInTiles, levelDepthInTiles;

//	[SerializeField]
//	private GameObject tilePrefab;

//	[SerializeField]
//	private float centerVertexZ, maxDistanceZ;

//	[SerializeField]
//	private TreeGeneration treeGeneration;

//	//[SerializeField]
//	//private RiverGeneration riverGeneration;

//	// Use this for initialization
//	void Start () {
//		// get the tile dimensions from the tile Prefab
//		Vector3 tileSize = tilePrefab.GetComponent<MeshRenderer> ().bounds.size;
//		int tileWidth = (int)tileSize.x;
//		int tileDepth = (int)tileSize.z;

//		// calculate the number of vertices of the tile in each axis using its mesh
//		Vector3[] tileMeshVertices = tilePrefab.GetComponent<MeshFilter> ().sharedMesh.vertices;
//		int tileDepthInVertices = (int)Mathf.Sqrt (tileMeshVertices.Length);
//		int tileWidthInVertices = tileDepthInVertices;

//		// build an empty LevelData object, to be filled with the tiles to be generated
//		LevelData levelData = new LevelData (tileDepthInVertices, tileWidthInVertices, this.levelDepthInTiles, this.levelWidthInTiles);

//		// for each Tile, instantiate a Tile in the correct position
//		for (int xTileIndex = 0; xTileIndex < levelWidthInTiles; xTileIndex++) {
//			for (int zTileIndex = 0; zTileIndex < levelDepthInTiles; zTileIndex++) {
//				// calculate the tile position based on the X and Z indices
//				Vector3 tilePosition = new Vector3(this.gameObject.transform.position.x + xTileIndex * tileWidth, 
//					this.gameObject.transform.position.y, 
//					this.gameObject.transform.position.z + zTileIndex * tileDepth);
//				// instantiate a new Tile
//				GameObject tile = Instantiate (tilePrefab, tilePosition, Quaternion.identity) as GameObject;
//				// generate the Tile texture
//				TileData tileData = tile.GetComponent<TileGeneration> ().GenerateTile (centerVertexZ, maxDistanceZ);
//				levelData.AddTileData (tileData, xTileIndex, zTileIndex);
//			}
//		}

//		// generate trees for the level
//		float distanceBetweenVertices = (float)tileDepth / (float)tileDepthInVertices;
//		treeGeneration.GenerateTrees (this.levelWidthInTiles * tileWidthInVertices, this.levelDepthInTiles * tileDepthInVertices, distanceBetweenVertices, levelData);

//		//riverGeneration.GenerateRivers (this.levelWidthInTiles * tileWidthInVertices, this.levelDepthInTiles * tileDepthInVertices, levelData);
//	}

//}

//// class to store all the merged tiles data
//public class LevelData {
//	private int tileDepthInVertices, tileWidthInVertices;

//	public TileData[,] tilesData;

//	public LevelData(int tileDepthInVertices, int tileWidthInVertices, int levelDepthInTiles, int levelWidthInTiles) {
//		// build the tilesData matrix based on the level depth and width
//		tilesData = new TileData[tileWidthInVertices * levelWidthInTiles, tileDepthInVertices * levelDepthInTiles];

//		this.tileDepthInVertices = tileDepthInVertices;
//		this.tileWidthInVertices = tileWidthInVertices;
//	}

//	public void AddTileData(TileData tileData, int tileXIndex, int tileZIndex) {
//		// save the TileData in the corresponding coordinate
//		tilesData [tileXIndex, tileZIndex] = tileData;
//	}

//	public TileCoordinate ConvertToTileCoordinate(int xIndex, int zIndex) {
//		// the tile index is calculated by dividing the index by the number of tiles in that axis
//		int tileXIndex = (int)Mathf.Floor ((float)xIndex / (float)this.tileWidthInVertices);
//		int tileZIndex = (int)Mathf.Floor ((float)zIndex / (float)this.tileDepthInVertices);
//		// the coordinate index is calculated by getting the remainder of the division above
//		// we also need to translate the origin to the bottom left corner
//		int coordinateXIndex = this.tileWidthInVertices - (xIndex % this.tileDepthInVertices) - 1;
//		int coordinateZIndex = this.tileDepthInVertices - (zIndex % this.tileDepthInVertices) - 1;

//		TileCoordinate tileCoordinate = new TileCoordinate (tileXIndex, tileZIndex, coordinateXIndex, coordinateZIndex);
//		return tileCoordinate;
//	}

//}

//// class to represent a coordinate in the Tile Coordinate System
//public class TileCoordinate {
//	public int tileXIndex;
//	public int tileZIndex;
//	public int coordinateXIndex;
//	public int coordinateZIndex;

//	public TileCoordinate(int tileXIndex, int tileZIndex, int coordinateXIndex, int coordinateZIndex) {
//		this.tileXIndex = tileXIndex;
//		this.tileZIndex = tileZIndex;
//		this.coordinateXIndex = coordinateXIndex;
//		this.coordinateZIndex = coordinateZIndex;
//	}
//}
