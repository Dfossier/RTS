using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Service;
using RTSEngine.Entities;
using RTSEngine.Selection;
using RTSEngine.EntityComponent;
using System;
using RTSEngine.Upgrades;
using RTSEngine.Task;
using RTSEngine.UnitExtension;
using RTSEngine;

public class BAC_CustomGEvents : MonoBehaviour, IPreRunGameService
{
    protected IGameManager GameMgr { get; private set; }
    protected IGlobalEventPublisher GEvents { get; private set; }
    protected ISelectionManager SelectionMgr { get; private set; }

    private IBuilding lastSelectedCamp; // 🔥 store reference here

    public void Init(IGameManager gameMgr)
    {
        GameMgr = gameMgr;
        GEvents = gameMgr.GetService<IGlobalEventPublisher>();
        SelectionMgr = gameMgr.GetService<ISelectionManager>();

        GEvents.EntityInstanceUpgradedGlobal += CheckingUpgrades;

        GEvents.EntitySelectedGlobal += OnEntitySelected;
        GEvents.EntityDeselectedGlobal += OnEntityDeselected; // optional


    }

    private void OnEntitySelected(IEntity entity, EntitySelectionEventArgs args)
    {
        if (entity is IBuilding building && building.Code == "hunting_camp")
        {
            lastSelectedCamp = building;
            Debug.Log($"Stored hunting camp: {building.Name}");
        }
    }

    private void OnEntityDeselected(IEntity entity, EventArgs args)
    {
        /*
        // Optional: only clear if the deselected thing was the stored one
        if (entity == lastSelectedCamp)
        {
            lastSelectedCamp = null;
            Debug.Log("Cleared hunting camp reference");
        }
        */
    }

    public void CheckingUpgrades(IEntity upgraded, UpgradeEventArgs<IEntity> args)
{
    Debug.Log("checkingupgrades launched!");
    // Only handle this type of upgrade
    if (args.UpgradedInstance.Code != "villagerderekshunter")
        return;

    IBuilding selected = lastSelectedCamp;
    if (selected == null || selected.Code != "hunting_camp")
        return;

    foreach (IUnit occupier in selected.UnitCarrier.CarrierSlots)
    {
        Debug.Log("Found occupier in hunting camp.");
        if (occupier != null && occupier.Code == "villagerdereks")
        {
            Debug.Log("First if passed...");

            // Eject the unit first so it's in the world
            // **NOTE: We need a slight delay after this before launching the task.**
            occupier.gameObject.GetComponentInChildren<CarriableUnit>().EjectActionLocal(false);

            if (occupier.gameObject.TryGetComponent(out UpgradeLauncher launcher))
            {
                Debug.Log("Starting coroutine to launch carried unit upgrade...");
                // **FIX: Start the coroutine to wait before launching the task**
                StartCoroutine(DelayedUpgradeLaunch(launcher));
            }
            else
            {
                Debug.LogError("Carried villager has no UpgradeLauncher!");
            }
        }
        else
        {
            Debug.Log("No villager found in hunting camp to upgrade.");
        }
    }
    Debug.Log("Checking Upgrades ended!");
}

// Add this Coroutine function
private System.Collections.IEnumerator DelayedUpgradeLaunch(UpgradeLauncher launcher)
{
    // Wait for the end of the frame or a very short time. 
    // This allows the engine to complete the unit's state transition after ejection.
    //yield return new WaitForEndOfFrame(); 
    yield return new WaitForSeconds(2f); // If WaitForEndOfFrame isn't enough.

    // Now, try launching the task
    Debug.Log($"Attempting to launch upgrade on unit: {launcher.Entity.Code}");
    ErrorMessage launchResult = launcher.LaunchTaskAction(0, false);
    
    // Check the result to confirm success or failure reason
    if (launchResult == ErrorMessage.none)
    {
        Debug.Log("Successfully launched carried unit upgrade!");
    }
    else
    {
        Debug.LogError($"Failed to launch carried unit upgrade. Error: {launchResult}");
    }
}




    

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Disable()
    {
        GEvents.EntityInstanceUpgradedGlobal -= CheckingUpgrades;
        GEvents.EntitySelectedGlobal -= OnEntitySelected;
        GEvents.EntityDeselectedGlobal -= OnEntityDeselected;
    }
}
