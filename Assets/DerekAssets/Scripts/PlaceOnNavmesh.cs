using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSEngine.Entities;
using RTSEngine.Faction;
using RTSEngine.Game;

public class PlaceOnNavmesh : MonoBehaviour
{
    [SerializeField]
    GameObject factionEntities;
    [SerializeField]
    public GameObject[] placeonnavmeshlist;
    private int placeAttempts = 0;
    private int capitalRadius = 15;
    private int unitRadius = 5;
    private int placeRadius = 1;

    // Start is called before the first frame update
    void Start()
    {
        //I think you want to place your objects before starting the game manager
        //gameMgr = GameObject.Find("GameManager").GetComponent<GameManager>();

            foreach (Building building in factionEntities.transform)
            {
                if (placeAttempts <= 100)
                {
                    UnityEngine.AI.NavMeshHit hit;

                    if (UnityEngine.AI.NavMesh.SamplePosition(building.transform.position, out hit, 5.0f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        building.transform.position = hit.position;
                        placeAttempts = 0;
                        //break;
                    }
                    else
                    {
                        float newX = building.transform.position.x + Random.value * capitalRadius;
                        float newZ = building.transform.position.z + Random.value * capitalRadius;
                        Vector3 newPosition = new Vector3(newX, 1.5f, newZ);
                        building.transform.position = newPosition;
                        //Debug.Log("new position" + newPosition);
                        placeAttempts++;
                    }
                }
                else
                {
                    Debug.Log("Could not place " + building);
                }
            }

            foreach (Unit unit in factionEntities.transform)
            {
                if (placeAttempts <= 100)
                {
                    UnityEngine.AI.NavMeshHit hit;

                    if (UnityEngine.AI.NavMesh.SamplePosition(unit.transform.position, out hit, 5.0f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        unit.transform.position = hit.position;
                        placeAttempts = 0;
                        //break;
                    }
                    else
                    {
                        float newX = unit.transform.position.x + Random.value * unitRadius;
                        float newZ = unit.transform.position.z + Random.value * unitRadius;
                        Vector3 newPosition = new Vector3(newX, 1.5f, newZ);
                        unit.transform.position = newPosition;
                        //Debug.Log("new position" + newPosition);
                        placeAttempts++;
                    }
                }
                else
                {
                    Debug.Log("Could not place " + unit);
                }
            }

        
        foreach (GameObject tobeplaced in placeonnavmeshlist)
        {
            if (placeAttempts <= 100)
            {
                UnityEngine.AI.NavMeshHit hit;

                if (UnityEngine.AI.NavMesh.SamplePosition(tobeplaced.transform.position, out hit, 5.0f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    tobeplaced.transform.position = hit.position;
                    placeAttempts = 0;
                    //break;
                }
                else
                {
                    float newX = tobeplaced.transform.position.x + Random.value * placeRadius;
                    float newZ = tobeplaced.transform.position.z + Random.value * placeRadius;
                    Vector3 newPosition = new Vector3(newX, 1.5f, newZ);
                    tobeplaced.transform.position = newPosition;
                    //Debug.Log("new position" + newPosition);
                    placeAttempts++;
                }
            }
            else
            {
                Debug.Log("Could not place " + tobeplaced);
            }

        }
    }
}
