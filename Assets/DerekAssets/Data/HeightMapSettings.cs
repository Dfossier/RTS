using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu()]
public class HeightMapSettings : UpdatableData
{
    public NoiseSettings noiseSettings;
    public NoiseSettings noiseSettingsForHeat;
    public NoiseSettings noiseSettingsForMoisture;
    public NoiseSettings noiseSettingsForUnits;

    public bool useFalloff;

    public float heightMultiplier;
    public AnimationCurve heightCurve;

    // [Range(2, 50)]
    private int smoothResolutionPerSegment = 50;
	public int oldSmoothResolutionPerSegment = 0; // sometimes it will stay as 50 or any other value smoothResolutionPerSegment is, then you need to manually set it to a different value like 0

    public float minHeight => heightMultiplier * heightCurve.Evaluate(0);
    public float maxHeight => heightMultiplier * heightCurve.Evaluate(1);

    public float minHeat = 0;
    public float maxHeat = 1;

    public float minMoisture = 0;
    public float maxMoisture = 1;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        noiseSettings.ValidateValues();
        base.OnValidate();

        if (smoothResolutionPerSegment != oldSmoothResolutionPerSegment)
        {
            if (smoothResolutionPerSegment > 50) smoothResolutionPerSegment = 50;
            if (smoothResolutionPerSegment < 1) smoothResolutionPerSegment = 1;
            oldSmoothResolutionPerSegment = smoothResolutionPerSegment;
            SmoothHeightCurve();
        }
    }
#endif

    private void SmoothHeightCurve()
    {
        if (heightCurve == null || heightCurve.length < 2)
            return;

        List<Keyframe> smoothKeys = new List<Keyframe>();

        for (int i = 0; i < heightCurve.length - 1; i++)
        {
            Keyframe a = heightCurve[i];
            Keyframe b = heightCurve[i + 1];

            float startTime = a.time;
            float endTime = b.time;
            float startValue = a.value;
            float endValue = b.value;

            for (int j = 0; j <= smoothResolutionPerSegment; j++)
            {
                float t = j / (float)smoothResolutionPerSegment;
                float time = Mathf.Lerp(startTime, endTime, t);
                float value = Mathf.SmoothStep(startValue, endValue, t);
                smoothKeys.Add(new Keyframe(time, value));
            }
        }

        heightCurve = new AnimationCurve(smoothKeys.ToArray());
		
    }
}
