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
    /*
    public void Update()
    {
        if (timer >= time)
        {
            timer = 0;

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

            Debug.Log("vish");
        }
        else
        {
            timer += Time.deltaTime;
        }
    }
    */

    public void DefineFactionsStartingpoint()
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
        Debug.Log("vish");
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
