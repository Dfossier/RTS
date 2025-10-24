using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSeedManager : MonoBehaviour
{
    public HeightMapSettings heightMapSettings;

    void Awake()
    {
        int mapSeed = heightMapSettings.noiseSettings.seed;
        Random.InitState(mapSeed);
    }
}
