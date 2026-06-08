using System.Collections;
using System.Collections.Generic;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.UnitExtension;
using UnityEngine;
using UnityEngine.AI;

public class AnimalsSpawnController : MonoBehaviour
{
    [Header("Animal Prefabs")]
    public GameObject cowPrefab;
    public GameObject deerPrefab;
    public GameObject wolfPrefab;
    public GameObject horsePrefab;

    [Header("Spawn Settings")]
    public int initialSpawnCount = 10;
    public int maxAnimalCount = 20;
    public float spawnInterval = 12f; // 4 minutes for 10 animals => 240/10 = 24s
    public float cleanupInterval = 5f; // prune destroyed animals from the lists every 5s

    private List<GameObject> cows = new List<GameObject>();
    private List<GameObject> deers = new List<GameObject>();
    private List<GameObject> wolves = new List<GameObject>();
    private List<GameObject> horses = new List<GameObject>();

    private float spawnTimer = 0f;
    private float cleanupTimer = 0f;
    private Transform mapCenter;

    public GameObject rtsController;

    public UnitManager unitManager;

    // heat stuff for horses to spawn on colder areas
    [Header("Temperature Map")]
    public Renderer terrainRenderer;
    public Texture2D heatMapTexture;
    public float coldThreshold = 0.4f; // 0 = very cold, 1 = hot


    void Start()
    {
        mapCenter = GameObject.Find("middleOfTheMap").transform;
        // CheckAndInitialSpawn();
    }

    void Update()
    {
        if (rtsController.activeSelf == false) return;

        CheckAndInitialSpawn();

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            TrySpawnAnimal(cowPrefab, cows);
            TrySpawnAnimal(deerPrefab, deers);
            TrySpawnAnimal(wolfPrefab, wolves);
            TrySpawnAnimal(horsePrefab, horses);
            spawnTimer = 0f;
        }

        cleanupTimer += Time.deltaTime;
        if (cleanupTimer >= cleanupInterval)
        {
            CleanupLists();
            cleanupTimer = 0f;
        }
    }

    void CheckAndInitialSpawn()
    {
        if (cows.Count == 0 && deers.Count == 0 && wolves.Count == 0 && horses.Count == 0)
        {
            for (int i = 0; i < initialSpawnCount; i++)
            {
                cows.Add(SpawnAnimal(cowPrefab));
                deers.Add(SpawnAnimal(deerPrefab));
                wolves.Add(SpawnAnimal(wolfPrefab));
                horses.Add(SpawnAnimal(horsePrefab));
            }
        }
    }

    void TrySpawnAnimal(GameObject prefab, List<GameObject> list)
    {
        if (list.Count < maxAnimalCount)
        {
            GameObject newAnimal = SpawnAnimal(prefab);
            if (newAnimal != null)
            {
                list.Add(newAnimal);
            }
        }
    }

    GameObject SpawnAnimal(GameObject prefab)
    {
        Vector3 spawnPos;
        bool preferCold = prefab == horsePrefab;
        if (FindValidNavMeshPosition(out spawnPos, preferCold))
        {
            GameObject animal = Instantiate(prefab, spawnPos, Quaternion.identity);
            animal.transform.SetParent(transform, worldPositionStays: true);

            animal.GetComponent<IUnit>().Init(rtsController.GetComponent<GameManager>(), new InitUnitParameters
                {
                    free = true,
                    factionID = -1,

                    setInitialHealth = false,

                    rallypoint = null,
                    gotoPosition = animal.transform.position,
                });
            return animal;
        }
        return null;
    }

    bool FindValidNavMeshPosition(out Vector3 position, bool preferCold = false)
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector3 basePos = mapCenter.position;

            // Bias north if preferCold (horses)
            Vector3 randomPoint = basePos + new Vector3(
                Random.Range(-100f, 100f),
                50f,
                preferCold ? Random.Range(50f, 200f) : Random.Range(-100f, 100f) //  first one is Z north for horses to spawn on colder areas
            );

            if (Physics.Raycast(randomPoint, Vector3.down, out RaycastHit hit, 200f))
            {
                // Skip temperature check for all other animals
                if (!preferCold)
                {
                    if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
                    {
                        position = navHit.position;
                        return true;
                    }
                }
                else
                {
                    // Horses: TEMPORARY easy mode — accept any hit, just log temperature
                    MeshCollider meshCollider = hit.collider as MeshCollider;
                    if (meshCollider != null && meshCollider.sharedMesh != null && heatMapTexture != null)
                    {
                        Vector2 uv = hit.textureCoord2;
                        Color heatColor = heatMapTexture.GetPixelBilinear(uv.x, uv.y);
                        float temperature = heatColor.r;

                        /*
                        // TEMP: Allow *any* temperature (0–1)
                        if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
                        {
                            position = navHit.position;
                            return true;
                        }
                        */
                        // ✅ Only spawn if the temperature is below the cold threshold
                        if (temperature <= coldThreshold)
                        {
                            if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
                            {
                                position = navHit.position;
                                return true;
                            }
                        }
                    }
                    else
                    {
                        // fallback if not mesh collider
                        if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
                        {
                            position = navHit.position;
                            return true;
                        }
                    }
                }
            }
        }

        position = Vector3.zero;
        return false;
    }



    void CleanupLists()
    {
        cows.RemoveAll(item => item == null);
        deers.RemoveAll(item => item == null);
        wolves.RemoveAll(item => item == null);
        horses.RemoveAll(item => item == null);
    }
}
