using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseGeneration : MonoBehaviour {

	public float[, ] GeneratePerlinNoiseMap (int width, int depth, float scale, float offsetX, float offsetZ, Wave[] waves) {
		// create an empty noise map
		float[, ] noiseMap = new float[width, depth];

		for (int xIndex = 0; xIndex < width; xIndex++) {
			for (int zIndex = 0; zIndex < depth; zIndex++) {
				// calculate sample indices based on the coordinates and the scale
				float sampleX = (xIndex + offsetX) / scale;
				float sampleZ = (zIndex + offsetZ) / scale;

				float noise = 0f;
				float normalization = 0f;
				foreach (Wave wave in waves) {
					// generate noise value using PerlinNoise for a given Wave
					noise += wave.amplitude * Mathf.PerlinNoise (sampleX * wave.frequency + wave.seed, sampleZ * wave.frequency + wave.seed);
					normalization += wave.amplitude;
				}
				// normalize the noise value so that it is within 0 and 1
				noise /= normalization;

				// save the noise in the noiseMap
				noiseMap[xIndex, zIndex] = noise;
			}
		}

		return noiseMap;
	}

	public float[, ] GenerateUniformNoiseMap (int width, int depth, float centerVertexZ, float maxVertexDistanceZ, float vertexOffsetZ) {
		// create an empty noise map
		float[, ] noiseMap = new float[width, depth];

		for (int zIndex = 0; zIndex < depth; zIndex++) {
			// calculate the sampleZ by summing the index and the offset
			float sampleZ = zIndex + vertexOffsetZ;
			// calculate the noise proportional to the distance of the sample to the center of the level
			float noise = Mathf.Abs (sampleZ - centerVertexZ) / maxVertexDistanceZ;

			// apply the noise for all points with this Z coordinate
			for (int xIndex = 0; xIndex < width; xIndex++) {
				noiseMap[xIndex, depth - zIndex - 1] = noise;
			}
		}

		return noiseMap;
	}
}

[System.Serializable]
public class Wave {
	public float seed;
	public float frequency;
	public float amplitude;
}