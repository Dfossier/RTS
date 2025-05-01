using UnityEngine;
using System.Collections;

[CreateAssetMenu()]
public class HeightMapSettings : UpdatableData {

	public NoiseSettings noiseSettings;
	public NoiseSettings noiseSettingsForHeat;
	public NoiseSettings noiseSettingsForMoisture;
    public NoiseSettings noiseSettingsForUnits;

    public bool useFalloff;

	public float heightMultiplier;
	public AnimationCurve heightCurve;

	public float minHeight {
		get {
			return heightMultiplier * heightCurve.Evaluate (0);
		}
	}

	public float maxHeight {
		get {
			return heightMultiplier * heightCurve.Evaluate (1);
		}
	}

    public float minHeat = 0;

    public float maxHeat = 1;

    public float minMoisture = 0;

    public float maxMoisture = 1;

#if UNITY_EDITOR

    protected override void OnValidate() {
		noiseSettings.ValidateValues ();
		base.OnValidate ();
	}
	#endif

}
