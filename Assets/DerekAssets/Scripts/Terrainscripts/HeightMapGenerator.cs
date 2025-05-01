using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HeatMap = HeatMapGenerator.HeatMap;
using MoistureMap = MoistureMapGenerator.MoistureMap;
using UnitMap = UnitMapGenerator.UnitMap;

public static class HeightMapGenerator {

	public static HeightMap GenerateHeightMap (MeshSettings meshSettings, HeightMapSettings settings, Vector2 sampleCentre, int colliderLODIndex) {

		float[, ] heightvalues = Noise.GenerateNoiseMap (meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, settings.noiseSettings, sampleCentre);
        AnimationCurve heightCurve_threadsafe = new AnimationCurve (settings.heightCurve.keys);

		float minHeight = float.MaxValue;
		float maxHeight = float.MinValue;

		for (int i = 0; i < meshSettings.numVertsPerLine; i++) {
			for (int j = 0; j < meshSettings.numVertsPerLine; j++) {
				heightvalues[i, j] *= heightCurve_threadsafe.Evaluate (heightvalues[i, j]) * settings.heightMultiplier;

				if (heightvalues[i, j] > maxHeight) {
					maxHeight = heightvalues[i, j];
				}
				if (heightvalues[i, j] < minHeight) {
					minHeight = heightvalues[i, j];
				}
			}
		}

        //TODO make the heatmap more realistic by passing heightmap to Heatmap and Moisturemap then adding heightmap*.5 to the heatmap
        MoistureMap moistureMap = MoistureMapGenerator.GenerateMoistureMap (meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, settings, sampleCentre);
		HeatMap heatMap = HeatMapGenerator.GenerateHeatMap (meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, settings, sampleCentre, heightvalues);
        UnitMap unitMap = UnitMapGenerator.GenerateUnitMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, settings, sampleCentre);


            return new HeightMap (
                heightvalues, minHeight, maxHeight,
			heatMap.heatvalues, heatMap.minHeat, heatMap.maxHeat,
			moistureMap.moisturevalues, moistureMap.minmoisture, moistureMap.maxmoisture, unitMap.unitvalues, unitMap.minunit, unitMap.maxunit, colliderLODIndex, meshSettings);
			}

}

public struct HeightMap {
	public readonly float[, ] heightvalues;
	public readonly float minHeight;
	public readonly float maxHeight;

	public readonly float[, ] heat;
	public readonly float minHeat;
	public readonly float maxHeat;

	public readonly float[, ] moisture;
	public readonly float minMoisture;
	public readonly float maxMoisture;

    public readonly float[,] unitvalues;
    public readonly float minUnit;
    public readonly float maxUnit;

	public readonly float colliderLODIndex;
	public readonly MeshSettings meshSettings;

	public HeightMap (float[, ] heightvalues, float minHeight, float maxHeight, float[, ] heat, float minHeat, float maxHeat, float[, ] moisture, float minMoisture,
		float maxMoisture, float[,] unitvalues, float minUnit, float maxUnit, float colliderLODIndex, MeshSettings meshSettings) 
	{
		this.heightvalues = heightvalues;
		this.minHeight = minHeight;
		this.maxHeight = maxHeight;

		this.heat = heat;
		this.minHeat = minHeat;
		this.maxHeat = maxHeat;

		this.moisture = moisture;
		this.minMoisture = minMoisture;
		this.maxMoisture = maxMoisture;

        this.unitvalues = unitvalues;
        this.minUnit = minUnit;
        this.maxUnit = maxUnit;

		this.colliderLODIndex = colliderLODIndex;
		this.meshSettings = meshSettings;
    }
}


