using UnityEngine;

public class UnitMapGenerator
{

    public static UnitMap GenerateUnitMap(int width, int height, HeightMapSettings settings, Vector2 sampleCentre)
    {

        //   float[, ] uunitvalues = Noise.GenerateUniformNoiseMap (width, height, noiseSettingsForUnits, sampleCentre);
        float[,] unitvalues = Noise.GenerateNoiseMap(width, height, settings.noiseSettingsForUnits, sampleCentre);
        //    float[, ] runitvalues = new float[width, height];

        float minUnit = float.MaxValue;
        float maxUnit = float.MinValue;


        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (unitvalues[i, j] > maxUnit)
                {
                    maxUnit = unitvalues[i, j];
                }
                if (unitvalues[i, j] < minUnit)
                {
                    minUnit = unitvalues[i, j];
                }
            }
        }
        //    for (int i = 0; i < width; i++) {
        //      for (int j = 0; j < height; j++) {
        //        // mix both heat maps together by multiplying their values
        //      unitvalues[i, j] = uunitvalues[i, j] * runitvalues[i, j];
        //
        //    if (unitvalues[i, j] > maxUnit) {
        //      maxUnit = unitvalues[i, j];
        //}
        //if (unitvalues[i, j] < minUnit) {
        //  minUnit = unitvalues[i, j];
        // }
        //}
        // }
        //used for shader testing
        //Debug.Log("maxUnit");
        //Debug.Log(maxUnit);

        return new UnitMap(unitvalues, minUnit, maxUnit);
    }

    public struct UnitMap
    {
        public readonly float[,] unitvalues;
        public readonly float minunit;
        public readonly float maxunit;

        public UnitMap(float[,] unitvalues, float minunit, float maxunit)
        {
            this.unitvalues = unitvalues;
            this.minunit = minunit;
            this.maxunit = maxunit;
        }
    }
}