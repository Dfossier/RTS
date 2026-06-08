using System.Collections.Generic;
using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.ResourceExtension;

/// <summary>
/// A single barter offer at a trade building: pay <see cref="cost"/>, instantly receive <see cref="reward"/>.
/// No gold/currency is involved - this is pure resource-to-resource trading (typically pay food).
/// </summary>
[System.Serializable]
public struct TradeOffer
{
    [Tooltip("Text shown on the trade button, e.g. 'Buy 10 Amber (50 Food)'.")]
    public string label;

    [Tooltip("Optional icon shown on the trade button (e.g. the bought resource's icon).")]
    public Sprite icon;

    [Tooltip("What the player PAYS for this trade (e.g. 50 Food). Leave empty for a free grant.")]
    public ResourceInput[] cost;

    [Tooltip("What the player RECEIVES from this trade (e.g. 10 Amber).")]
    public ResourceInput[] reward;
}

/// <summary>
/// Attached to a neutral ("free", factionID -1) trade building. Instantly converts resources
/// for the local player faction by bartering: deduct <see cref="TradeOffer.cost"/>, add
/// <see cref="TradeOffer.reward"/>. Offers can be authored on the prefab in the inspector, or
/// injected per map-direction at spawn time by <see cref="TradeBuildingSpawner"/> via <see cref="SetOffers"/>.
///
/// This component only performs the trade. Triggering it (selection + "friendly unit nearby" gate
/// + buttons) is handled by <see cref="TradeBuildingPanelUIHandler"/>.
/// </summary>
public class ResourceTraderComponent : EntityComponentBase
{
    #region Attributes
    [SerializeField, Tooltip("The trades this building offers. Each entry: pay 'cost' -> receive 'reward'.")]
    private TradeOffer[] offers = new TradeOffer[0];
    public IReadOnlyList<TradeOffer> Offers => offers;

    private IResourceManager resourceMgr;
    #endregion

    #region Initializing
    protected override void OnInit()
    {
        resourceMgr = gameMgr.GetService<IResourceManager>();
    }
    #endregion

    #region Offers
    /// <summary>
    /// Replaces the offer list at runtime. Used by the spawner so a single prefab can serve all
    /// four edge buildings, each selling its own specialty (North = amber, East = lapis, etc.).
    /// </summary>
    public void SetOffers(TradeOffer[] newOffers)
    {
        offers = newOffers ?? new TradeOffer[0];
    }
    #endregion

    #region Trading
    /// <summary> True if the given faction currently holds enough resources to pay for the offer. </summary>
    public bool CanAfford(int offerIndex, int factionID)
    {
        if (resourceMgr == null || offerIndex < 0 || offerIndex >= offers.Length)
            return false;

        ResourceInput[] cost = offers[offerIndex].cost;
        return cost == null || cost.Length == 0 || resourceMgr.HasResources(cost, factionID);
    }

    /// <summary>
    /// Instantly performs the trade for the given faction: deducts the cost and adds the reward.
    /// Returns false (and does nothing) if the faction can't afford it. "Instant" = synchronous.
    /// </summary>
    public bool ExecuteTrade(int offerIndex, int factionID)
    {
        if (!CanAfford(offerIndex, factionID))
            return false;

        TradeOffer offer = offers[offerIndex];

        if (offer.cost != null && offer.cost.Length > 0)
            resourceMgr.UpdateResource(factionID, offer.cost, add: false);

        if (offer.reward != null && offer.reward.Length > 0)
            resourceMgr.UpdateResource(factionID, offer.reward, add: true);

        return true;
    }
    #endregion
}
