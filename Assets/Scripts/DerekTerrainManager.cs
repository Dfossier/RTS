using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RTSEngine.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class DerekTerrainManager : MonoBehaviour
{
    public GameObject rtsReEntities;
    public GameObject GameManager;
    private float timer;

    // public GameObject npcFactionPrefab;
    // public GameObject[] npcFactionSpawned;

    public GameObject[] npcFactionsList;

    public void Start()
    {
        InitializeDerekTerrain();
    }

    // run this on rts engine initialization before anything else to set the values, this is only related to the terrain stuff like resources, trees etc and not rts pre configs from the lobby, there will be a different manager for that
    public void InitializeDerekTerrain()
    {
        ReParentingObjects();
        SetPlayerFactionPosition();
        LoadNPCsAndSetPosition();
        GameManager.SetActive(true);
    }

    private void ReParentingObjects()
    {
        // change parent of all resources from derek's ResourceEntities to RTS ResourceEntities
        Transform mapgen = GameObject.Find("Map Generator").transform;
        Transform DerekReEntities = mapgen.Find("ResourceEntities").transform;

        // create a temporary list to avoid modifying the collection while iterating
        List<Transform> children = new List<Transform>();

        foreach (Transform child in DerekReEntities)
        {
            children.Add(child);
        }

        // reparent all children
        foreach (Transform child in children)
        {
            child.SetParent(rtsReEntities.transform, true);
        }
    }

    private void SetPlayerFactionPosition()
    {
        Transform playerSpawnpoint = GameObject.Find("debugRandomFactionSpawnpoint").transform;
        GameObject playerFaction = GameObject.Find("playerFaction");

        // Vector3 localPosition = playerFaction.transform.parent.InverseTransformPoint(playerSpawnpoint.position);

        playerFaction.transform.position = RandomNavmeshLocation(10f, playerSpawnpoint.position);

        // Loop through each child
        foreach (Transform child in playerFaction.transform)
        {
            Vector3 randomNearbyPosition = RandomNavmeshLocation(5f, playerFaction.transform.position);
            child.position = randomNearbyPosition;
        }

        // playerFaction.transform.position = playerSpawnpoint.position;

        Debug.Log(playerSpawnpoint);
        Debug.Log(playerFaction.transform.position);
    }

    private void LoadNPCsAndSetPosition()
    {
        RandomFactionSpawnpoint factionsController = GameObject.Find("debugRandomFactionSpawnpoint").GetComponent<RandomFactionSpawnpoint>();
        // if (factionsController.NPC_Count <= 0) return;
        /*
        for (int i = 0; i < factionsController.NPC_Count; i++)
        {
            var new_npcFaction = Instantiate(npcFactionPrefab);
            new_npcFaction.transform.position = factionsController.NPCsSpawnpoint[i];
            new_npcFaction.transform.SetParent(GameObject.Find("FactionEntities").transform, true);
            npcFactionSpawned[i] = new_npcFaction;
        }
        */

        // set stuff on RTS Engine GameManager
        // GameManager.GetComponent<GameManager>().FactionSlots[0].Enabled = false;

        for (int i = 0; i < npcFactionsList.Count(); i++)
        {
            npcFactionsList[i].transform.position = RandomNavmeshLocation(10f, factionsController.NPCsSpawnpoint[i]);

            foreach (Transform child in npcFactionsList[i].transform)
            {
                Vector3 randomNearbyPosition = RandomNavmeshLocation(5f, npcFactionsList[i].transform.position);
                child.position = randomNearbyPosition;
            }
        }
    }

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
