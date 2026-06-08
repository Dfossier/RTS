using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using RTSEngine;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Event;
using System.Linq;
using RTSEngine.Health;
using System;

public class GrowTreeController : MonoBehaviour, IEntityPreInitializable
{
    [HideInInspector] public int TabID = 0;
    protected IGameManager GameMgr { get; private set; }
    public IEntity Instance { get; private set; }

    [SerializeField, Tooltip("Controls how quickly the tree scales up visually. " +
        "This value is divided by 10 to get the lerp duration in seconds " +
        "(e.g. 1200 → 120s to reach full scale). Also affects the delay timer calculation.")]
    private float growSpeed = 1f;

    public IBuilding building { get; private set; }
    public IResourceBuilding resBuilding { get; private set; }

    [Tooltip("The child GameObjects that will be scaled from zero to a random size between MinScale and MaxScale " +
        "when the grow animation triggers. Typically the visible crop or tree mesh.")]
    public List<GameObject> ResourceObjects = new();

    [Tooltip("Read-only status: true once the start delay has elapsed and the grow coroutine is allowed to fire.")]
    public bool CanGrow = false;

    [Tooltip("Read-only status: true while the ScaleTreeForGrowth coroutine is running.")]
    public bool IsGrowing = false;

    [Tooltip("Read-only status: true once the scale coroutine has completed and the resource object is at full size.")]
    public bool HasGrownMax = false;

    [Range(0, 50), Tooltip("Lower bound of the randomised final scale applied to each ResourceObject. " +
        "A random value between MinScale and MaxScale is chosen each time the grow animation fires.")]
    public float MinScale = 0;

    [Range(0, 50), Tooltip("Upper bound of the randomised final scale applied to each ResourceObject.")]
    public float MaxScale = 3;

    [Tooltip("Reference maximum used for health calculations (currently mirrors ResourceHealth.MaxHealth on the building).")]
    public float ResourceMax = 100;

    [SerializeField, Range(0, 50), Tooltip("Seconds to wait after ActivateGrowingSystem(true) is called " +
        "(i.e. after construction completes) before the scale animation begins.")]
    private float _startGrowthDelay = 5;

    [SerializeField, Tooltip("Internal flag set by ActivateGrowingSystem(). True while the delay countdown is running.")]
    private bool _startDelay = false;
    float currentDelay = 0;

    public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
    {
        GameMgr = gameMgr;
        Instance = entity;
        if(entity != null)
        {
            building = entity as IBuilding;
            resBuilding = entity as IResourceBuilding;
        }
    }
    public void SetGrowthDelay(float delay)
    {
        _startGrowthDelay = Mathf.Max(0f, delay);
    }

    public void SetCanGrow(IBuilding building, EventArgs eventArgs)
    {
        if(building != null && building.IsBuilt)
        {
            CanGrow = true;
        }
    }
    public void ActivateGrowingStystem(bool isActive)
    {
        if (isActive)
        {
            _startDelay = true;
        }
        else
        {
            _startDelay = false;
        }
    }
    private void Update()
    {
        if (Instance == null) return;
        if(building == null || !building.IsBuilt) return;

        if (_startDelay)
        {
            if (currentDelay < _startGrowthDelay)
            {
                currentDelay += Time.deltaTime;
            }else
            {
                CanGrow = true;
                _startDelay = false;
                if (CanGrow && !HasGrownMax)
                {
                    if (ResourceObjects.Count > 0)
                    {
                        for (int i = 0; i < ResourceObjects.Count; i++)
                        {
                            CanGrow = false;
                            IsGrowing = true;
                            float finalScale = UnityEngine.Random.Range(MinScale, MaxScale);
                            Vector3 final = new(finalScale, finalScale, finalScale);
                            StartCoroutine(ScaleTreeForGrowth(ResourceObjects.ElementAtOrDefault(i).transform, final, growSpeed / 10));
                            IEntityHealth resEntityHealth = (resBuilding as IResource).Health;
                            resEntityHealth.SetMax(new HealthUpdateArgs(Mathf.FloorToInt(ResourceMax), resBuilding));
                            float smooth = Mathf.Lerp(0, ResourceMax, growSpeed);
                            resEntityHealth.Add(new HealthUpdateArgs(Mathf.FloorToInt(smooth), resBuilding));
                        }
                    }
                }
            }
        }
        else
        {
            
        }
    }

    public IEnumerator ScaleTreeForGrowth(Transform tree, Vector3 maxScale, float growSpeed)
    {
        float elapsed = 0;

        Vector3 startScale = new(0,0,0);

        while(elapsed < growSpeed)
        {
            elapsed += Time.deltaTime;

            tree.localScale = Vector3.Lerp(startScale, maxScale, elapsed / growSpeed);

            yield return null;
        }

        HasGrownMax = true;

    }

    public void Disable()
    {
        if (building != null)
        {
            building.BuildingBuilt -= SetCanGrow;
        }
    }
}
