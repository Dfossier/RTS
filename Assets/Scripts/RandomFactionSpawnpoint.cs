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
        // Ensure the array is correctly sized
        NPCsSpawnpoint = new Vector3[NPC_Count];

        for (int i = 0; i < NPC_Count; i++)
        {
            Vector3 center;

            // Choose a random chunkMiddle if available
            if (chunkMiddle != null && chunkMiddle.Length > 0)
            {
                GameObject randomChunk = chunkMiddle[Random.Range(0, chunkMiddle.Length)];
                center = randomChunk.transform.position;
                Debug.Log(randomChunk.name);
                Debug.Log(center);
            }
            else
            {
                // Fallback to this object's position
                center = transform.position;
            }

            // Generate random NavMesh position
            NPCsSpawnpoint[i] = RandomNavmeshLocation(radius, center);
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
