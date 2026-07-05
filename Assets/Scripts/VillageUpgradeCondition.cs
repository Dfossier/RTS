using UnityEngine;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.EntityComponent;
using RTSEngine.ResourceExtension;
using RTSEngine.Upgrades;

public class VillageUpgradeCondition : MonoBehaviour, IEntityPostInitializable
{
    [Header("Thresholds")]
    [SerializeField] private int animalGarrisonThreshold = 15;
    [SerializeField] private int grainThreshold = 500;
    [SerializeField] private int divineFavorThreshold = 500;

    [Header("References")]
    [Tooltip("The UnitCarrier on this building that accepts tamed animals.")]
    [SerializeField] private UnitCarrier animalCarrier;
    [SerializeField] private ResourceTypeInfo grainType;
    [SerializeField] private ResourceTypeInfo divineFavorType;
    [SerializeField] private EntityUpgrade villageUpgrade;

    private IBuilding building;
    private IGameManager gameMgr;
    private IResourceManager resourceMgr;
    private bool upgraded = false;

    public void OnEntityPostInit(IGameManager gameMgr, IEntity entity)
    {
        this.gameMgr = gameMgr;
        this.resourceMgr = gameMgr.GetService<IResourceManager>();
        this.building = entity as IBuilding;

        if (building == null || animalCarrier == null || villageUpgrade == null)
        {
            enabled = false;
            return;
        }

        InvokeRepeating(nameof(CheckConditions), 5f, 5f);
    }

    public void Disable()
    {
        CancelInvoke(nameof(CheckConditions));
    }

    private void CheckConditions()
    {
        if (upgraded || building == null || building.IsFree) return;
        if (building.FactionID != gameMgr.LocalFactionSlotID) return;

        if (animalCarrier.CurrAmount < animalGarrisonThreshold) return;

        var handlers = resourceMgr.FactionResources[building.FactionID].ResourceHandlers;

        if (!handlers.ContainsKey(grainType) || handlers[grainType].Amount < grainThreshold) return;
        if (!handlers.ContainsKey(divineFavorType) || handlers[divineFavorType].Amount < divineFavorThreshold) return;

        upgraded = true;
        CancelInvoke(nameof(CheckConditions));
        villageUpgrade.LaunchLocal(gameMgr, 0, building.FactionID);
    }
}
