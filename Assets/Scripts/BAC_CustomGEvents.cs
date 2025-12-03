using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Service;
using RTSEngine.Entities;
using RTSEngine.EntityComponent;
using System;
using RTSEngine.Upgrades;

public class BAC_CustomGEvents : MonoBehaviour, IPreRunGameService
{
    protected IGameManager GameMgr { get; private set; }
    protected IGlobalEventPublisher GEvents { get; private set; }

    public void Init(IGameManager gameMgr)
    {
        GameMgr = gameMgr;
        GEvents = gameMgr.GetService<IGlobalEventPublisher>();
        GEvents.EntityUpgradedGlobal += EntityUpgraded;
    }

    private void EntityUpgraded(IEntity target, UpgradeEventArgs<IEntity> EventArgs)
    {
        if(EventArgs.UpgradedInstance.Type == EntityType.unit && EventArgs.UpgradedInstance.Code == "villagerdereks")
        {
            Debug.Log("trying to upgrade villagers!");
            if (EventArgs.UpgradeElement.sourceCode == "")
            {
                IBuilding upgrader = EventArgs.UpgradeElement.target as IBuilding;
                if (upgrader != null)
                {
                    IUnitCarrier carrier = upgrader.UnitCarrier;
                    if (carrier != null && carrier.CurrAmount > 0)
                    {
                        foreach (IUnit unit in carrier.CarrierSlots)
                        {
                            // fetch the upgrade component and launch action by index
                            if(unit != null && unit.gameObject.TryGetComponent(out EntityUpgrade upgrade))
                            {
                                upgrade.LaunchLocal(GameMgr, 0, unit.FactionID);
                                Debug.Log("Upgraded unit!");
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

    void Disable()
    {
        GEvents.EntityUpgradedGlobal -= EntityUpgraded;
    }
}
