using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public static class HeatMapGenerator {

    public static HeatMap GenerateHeatMap (int width, int height, HeightMapSettings settings, Vector2 sampleCentre, float [,] heightvalues) {

        float[,] uheatvalues = Noise.GenerateUniformNoiseMap (width, height, settings.noiseSettingsForHeat, sampleCentre);
        float[,] rheatvalues = Noise.GenerateNoiseMap (width, height, settings.noiseSettingsForHeat, sampleCentre);
        float[,] heatvalues = new float[width, height];

        float minHeat = float.MaxValue;
        float maxHeat = float.MinValue;

        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                // mix both heat maps together by multiplying their values
                 
                //use this to look at uniform map in MapPreview.cs
                //heatvalues[i, j] = uheatvalues[i, j];

                heatvalues[i, j] = uheatvalues[i, j] * rheatvalues[i, j];

                if (heightvalues[i,j] > .8)
                {
                    // makes mountains even colder, by using a greater multiplier
                    heatvalues[i, j] += 0.01f * heightvalues[i, j];
                }
                else
                {
                    // makes higher regions colder, by adding the height value to the heat map
                    heatvalues[i, j] += 0.0025f * heightvalues[i, j];
                }


                if (heatvalues[i, j] > maxHeat) {
                    maxHeat = heatvalues[i, j];
                }
                if (heatvalues[i, j] < minHeat) {
                    minHeat = heatvalues[i, j];
                }
            }
        }

        return new HeatMap (heatvalues, minHeat, maxHeat);
    }

    public struct HeatMap {
        public readonly float[, ] heatvalues;
        public readonly float minHeat;
        public readonly float maxHeat;

        public HeatMap (float[, ] heatvalues, float minHeat, float maxHeat) {
            this.heatvalues = heatvalues;
            this.minHeat = minHeat;
            this.maxHeat = maxHeat;
        }
    }
}