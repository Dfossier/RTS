using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using RTSEngine.Game;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using RTSEngine.Service;
using RTSEngine.UnitExtension;
using RTSEngine.ResourceExtension;
using System;

public class BAC_AdvancedBuilding : MonoBehaviour, IEntityPostInitializable
{
    protected IGameManager GameMgr { get; private set; }
    protected IUnitManager UnitMgr { get; private set; }
    protected IUnitCreator UnitCreator { get; private set; }
    protected IResourceManager ResMgr { get; private set; }
    public IEntity Entity { get; private set; }
    public IBuilding Building { get; private set; }

    public bool SpawnUnitsOnComplete = false;
    [SerializeField] private bool UnitsSpawned = false;
    public List<SpawnStartUnit> StartUnits = new();

    private bool StartSpawn = false;

    [Serializable]
    public class SpawnStartUnit
    {
        public int CreatorTaskIndex = 0;
        public int SpawnCount = 0;
    }

    public void OnEntityPostInit(IGameManager gameMgr, IEntity entity)
    {
        GameMgr = gameMgr;
        UnitMgr = gameMgr.GetService<IUnitManager>();
        ResMgr = gameMgr.GetService<IResourceManager>();
        Entity = entity;
        if(entity.Type == EntityType.building)
        {
            Building = entity as IBuilding;
            UnitCreator = Building.UnitCreator;
            StartSpawn = true;

        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Building == null) return;

        if (Building.Health.CurrHealth > Building.Health.MaxHealth / 2)
        {
            if(StartSpawn && !UnitsSpawned)
            {
                SpawnStrtingUnits();
            }
        }
    }

    public void SpawnStrtingUnits()
    {
        if (this.StartUnits.Count > 0)
        {
            int counter = -1;
            foreach (SpawnStartUnit startUnit in this.StartUnits)
            {
                if (startUnit != null && startUnit.SpawnCount > 0)
                {
                    counter++;
                    for (int j = 0; j < startUnit.SpawnCount; j++)
                    {
                        if (ResMgr != null)
                        {
                            var requiredResources = UnitCreator.Tasks[startUnit.CreatorTaskIndex].RequiredResources;
                            if (requiredResources != null && requiredResources.Count > 0)
                            {
                                ResourceInput resourceInput = new ResourceInput();
                                resourceInput.type = requiredResources[0].type;
                                resourceInput.value = requiredResources[0].value;
                                ResMgr.UpdateResource(Building.FactionID, resourceInput, true);
                            }
                        }
                        Debug.Log($"Villager {startUnit.CreatorTaskIndex} created");
                        UnitCreator.LaunchTaskAction(startUnit.CreatorTaskIndex, false);
                    }
                }
            }
            StartSpawn = false;
            UnitsSpawned = true;
        }
    }

    public void Disable()
    {

    }
}
