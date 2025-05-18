using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DerekTerrainManager : MonoBehaviour
{
    // run this on rts engine initialization before anything else to set the values, this is only related to the terrain stuff like resources, trees etc and not rts pre configs from the lobby, there will be a different manager for that
    public void InitializeDerekTerrain()
    {
        // change parent of all resources from derek's ResourceEntities to RTS ResourceEntities
        Transform mapgen = GameObject.Find("MapGenerator").transform;
        Transform DerekReEntities = mapgen.Find("ResourcesEntities").transform;

        Transform rtsReEntities = GameObject.Find("ResourcesEntities").transform;

        // create a temporary list to avoid modifying the collection while iterating
        List<Transform> children = new List<Transform>();

        foreach (Transform child in DerekReEntities)
        {
            children.Add(child);
        }

        // reparent all children
        foreach (Transform child in children)
        {
            child.SetParent(rtsReEntities);
        }
    }

}
