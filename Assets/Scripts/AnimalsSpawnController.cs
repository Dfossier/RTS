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

    [Header("Spawn Settings")]
    public int initialSpawnCount = 10;
    public int maxAnimalCount = 20;
    public float spawnInterval = 12f; // 4 minutes for 10 animals => 240/10 = 24s

    private List<GameObject> cows = new List<GameObject>();
    private List<GameObject> deers = new List<GameObject>();
    private List<GameObject> wolves = new List<GameObject>();

    private float spawnTimer = 0f;

    public GameObject rtsController;

    public UnitManager unitManager;

    void Start()
    {
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
            spawnTimer = 0f;
        }

        CleanupLists();
    }

    void CheckAndInitialSpawn()
    {
        if (cows.Count == 0 && deers.Count == 0 && wolves.Count == 0)
        {
            for (int i = 0; i < initialSpawnCount; i++)
            {
                cows.Add(SpawnAnimal(cowPrefab));
                deers.Add(SpawnAnimal(deerPrefab));
                wolves.Add(SpawnAnimal(wolfPrefab));
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
        if (FindValidNavMeshPosition(out spawnPos))
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

    bool FindValidNavMeshPosition(out Vector3 position)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 randomPoint = GameObject.Find("middleOfTheMap").transform.position + Random.insideUnitSphere * 100f;
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                position = hit.position;
                return true;
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
    }
}
