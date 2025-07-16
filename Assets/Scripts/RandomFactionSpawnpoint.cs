using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;

public class RandomFactionSpawnpoint : MonoBehaviour
{
    private float timer = 0;
    public float radius = 160f;
    public float time = 15f;
    public GameObject[] chunkMiddle;

    public int NPC_Count = 1;
    public Vector3[] NPCsSpawnpoint;

    [SerializeField] private float minDistanceBetweenFactions = 50f;

    public void Start()
    {
        Random.InitState((int)System.DateTime.Now.Ticks);
    }

    public void DefineFactionsStartingpoint()
    {
        DefinePlayerFaction();
        DefineNPCsFaction();
    }

    private void DefinePlayerFaction()
    {
        // Pick a random chunk from chunkMiddle array
        if (chunkMiddle != null && chunkMiddle.Length > 0)
        {
            GameObject randomChunk = chunkMiddle[Random.Range(0, chunkMiddle.Length)];

            // Use randomChunk position as center for navmesh location sampling
            transform.position = RandomNavmeshLocation(radius, randomChunk.transform.position);
        }
        else
        {
            // Fallback to using this object's position if no chunks available
            transform.position = RandomNavmeshLocation(radius, transform.position);
        }
    }

    private void DefineNPCsFaction()
    {
        if (NPC_Count > 0)
        {
            NPCsSpawnpoint = new Vector3[NPC_Count];

            int maxAttemptsPerNPC = 30;

            for (int i = 0; i < NPC_Count; i++)
            {
                bool found = false;

                for (int attempt = 0; attempt < maxAttemptsPerNPC; attempt++)
                {
                    Vector3 center;

                    if (chunkMiddle != null && chunkMiddle.Length > 0)
                    {
                        GameObject randomChunk = chunkMiddle[Random.Range(0, chunkMiddle.Length)];
                        center = randomChunk.transform.position;
                    }
                    else
                    {
                        center = transform.position;
                    }

                    Vector3 potentialPos = RandomNavmeshLocation(radius, center);

                    // Check distance from player faction
                    if (Vector3.Distance(potentialPos, transform.position) < minDistanceBetweenFactions)
                        continue;

                    // Check distance from other NPCs
                    bool tooCloseToOtherNPC = false;
                    for (int j = 0; j < i; j++)
                    {
                        if (Vector3.Distance(potentialPos, NPCsSpawnpoint[j]) < minDistanceBetweenFactions)
                        {
                            tooCloseToOtherNPC = true;
                            break;
                        }
                    }

                    if (tooCloseToOtherNPC)
                        continue;

                    // Accept this position
                    NPCsSpawnpoint[i] = potentialPos;
                    found = true;
                    break;
                }

                if (!found)
                {
                    Debug.LogWarning($"[FactionSpawn] Could not find valid position for NPC #{i} after {maxAttemptsPerNPC} tries.");
                    NPCsSpawnpoint[i] = transform.position; // fallback
                }
            }

            Debug.Log("NPC spawnpoints defined: " + NPCsSpawnpoint.Length);
        }
    }


    // Modified to accept center position as parameter
    public Vector3 RandomNavmeshLocation(float radius, Vector3 center)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += center;
        NavMeshHit hit;
        Vector3 finalPosition = Vector3.zero;
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
        {
            finalPosition = hit.position;
        }
        return finalPosition;
    }

}
