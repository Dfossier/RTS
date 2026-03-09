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
    [SerializeField] private float growSpeed = 1f;
    public IBuilding building { get; private set; }
    public IResourceBuilding resBuilding { get; private set; }

    public List<GameObject> ResourceObjects = new();

    public bool CanGrow = false;
    public bool IsGrowing = false;
    public bool HasGrownMax = false;

    [Range(0, 50)] public float MinScale = 0;
    [Range(0, 50)] public float MaxScale = 3;

    public float ResourceMax = 100;

    [SerializeField,Range(0,50)] private float _startGrowthDelay = 5;
    [SerializeField] private bool _startDelay = false;
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
        if(!building.IsBuilt && building.IsPlacementInstance) return;

        if (_startDelay)
        {
            float mplier = 0;
            mplier = growSpeed / 60;
            float calc = 100 / mplier;
            if (currentDelay < _startGrowthDelay)
            {
                currentDelay += Time.deltaTime;
                Debug.Log($"delay timer: {currentDelay}");

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
                            float smooth = Mathf.Lerp(0, (resBuilding as IResource).Health.MaxHealth, growSpeed);
                            IResourceHealth ResHealth = (resBuilding as IResource).Health;
                            ResHealth.Add(new HealthUpdateArgs(Mathf.FloorToInt(smooth), resBuilding));
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
