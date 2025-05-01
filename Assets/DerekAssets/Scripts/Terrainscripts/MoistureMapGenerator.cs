using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoistureMapGenerator {

    public static MoistureMap GenerateMoistureMap (int width, int height, HeightMapSettings settings, Vector2 sampleCentre) {

        //   float[, ] umoisturevalues = Noise.GenerateUniformNoiseMap (width, height, noiseSettingsForMoisture, sampleCentre);
        float[, ] moisturevalues = Noise.GenerateNoiseMap (width, height, settings.noiseSettingsForMoisture, sampleCentre);
    //    float[, ] moisturevalues = new float[width, height];

        float minMoisture = float.MaxValue;
        float maxMoisture = float.MinValue;


        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (moisturevalues[i, j] > maxMoisture)
                {
                    maxMoisture = moisturevalues[i, j];
                }
                if (moisturevalues[i, j] < minMoisture)
                {
                    minMoisture = moisturevalues[i, j];
                }
            }
        }
        //    for (int i = 0; i < width; i++) {
        //      for (int j = 0; j < height; j++) {
        //        // mix both heat maps together by multiplying their values
        //      moisturevalues[i, j] = umoisturevalues[i, j] * rmoisturevalues[i, j];
        //
        //    if (moisturevalues[i, j] > maxMoisture) {
        //      maxMoisture = moisturevalues[i, j];
        //}
        //if (moisturevalues[i, j] < minMoisture) {
        //  minMoisture = moisturevalues[i, j];
        // }
        //}
        // }
        //used for shader testing
        //Debug.Log("maxMoisture");
        //Debug.Log(maxMoisture);

        return new MoistureMap (moisturevalues, minMoisture, maxMoisture);
    }

    public struct MoistureMap {
        public readonly float[, ] moisturevalues;
        public readonly float minmoisture;
        public readonly float maxmoisture;

        public MoistureMap (float[, ] moisturevalues, float minmoisture, float maxMoisture) {
            this.moisturevalues = moisturevalues;
            this.minmoisture = minmoisture;
            this.maxmoisture = maxMoisture;
        }
    }
}