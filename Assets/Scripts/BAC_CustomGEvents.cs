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

public class BAC_CustomGEvents : MonoBehaviour, IPreRunGameService
{
    protected IGameManager GameMgr { get; private set; }
    protected IGlobalEventPublisher GEvents { get; private set; }
    protected ISelectionManager SelectionMgr { get; private set; }

    public void Init(IGameManager gameMgr)
    {
        GameMgr = gameMgr;
        GEvents = gameMgr.GetService<IGlobalEventPublisher>();
        SelectionMgr = gameMgr.GetService<ISelectionManager>();

        GEvents.EntityInstanceUpgradedGlobal += CheckingUpgrades;

    }
    public void CheckingUpgrades(IEntity Upgraded, UpgradeEventArgs<IEntity> EventArgs)
    {
        if(EventArgs.UpgradedInstance.Code == "villagerderekshunter")
        {
            IBuilding selected = SelectionMgr.GetSingleSelectedEntity(EntityType.building, true) as IBuilding;
            if(selected.Code == "hunting_camp")
            {
                if(selected.UnitCarrier.CurrAmount > 0)
                {
                    foreach(IUnit occupier in selected.UnitCarrier.CarrierSlots)
                    {
                        if(occupier.Code == "villagerdereks")
                        {
                            if (occupier != null && occupier.gameObject.TryGetComponent(out UpgradeLauncher launcher))
                            {
                                Debug.Log($"to upgrade: {occupier.Code}");
                                
                                launcher.LaunchTaskAction(0,false);
                                /*
                                upgrade.LaunchLocal(GameMgr, 0, occupier.FactionID);
                                Debug.Log(
                                    $"upgrading: {Upgraded.Name} | " +
                                    $"upgraded: {EventArgs.UpgradedInstance.Name} | " +
                                    $"selected: {selected.Name}");
                                */
                            }
                        }
                    }
                }
                
            }
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
    }
}
