using System;
using UnityEngine;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.ResourceExtension;
using RTSEngine.Upgrades;

public class AltarManager : MonoBehaviour, IEntityPreInitializable
{
    public enum AltarPlacement { Plains, Hilltop, River, Any }

    [Header("Placement Thresholds")]
    [SerializeField] private float hilltopMinElevation = 40f;
    [SerializeField] private float plainsMaxElevation = 25f;
    [SerializeField] private float riverDetectionRadius = 15f;

    [Header("Deity Upgrades")]
    [SerializeField] private EntityComponentUpgrade gorgonUpgrade;
    [SerializeField] private EntityComponentUpgrade dzuesUpgrade;
    [SerializeField] private EntityComponentUpgrade pseidonUpgrade;
    [SerializeField] private EntityComponentUpgrade aresUpgrade;

    [Header("Sacrifice Settings")]
    [SerializeField] private ResourceTypeInfo divineFavorType;
    [SerializeField] private int divineThreshold = 20;

    private IBuilding altar;
    private IGameManager gameMgr;
    private bool deityChosen = false;
    private AltarPlacement placement;

    public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
    {
        this.gameMgr = gameMgr;
        altar = entity as IBuilding;
        if (altar == null) return;
        altar.BuildingBuilt += OnAltarBuilt;
    }

    public void Disable()
    {
        if (altar != null)
            altar.BuildingBuilt -= OnAltarBuilt;
        CancelInvoke(nameof(CheckDivineFavor));
    }

    private void OnAltarBuilt(IBuilding building, EventArgs args)
    {
        placement = DetectPlacement(altar.transform.position);
        InvokeRepeating(nameof(CheckDivineFavor), 5f, 5f);
    }

    private AltarPlacement DetectPlacement(Vector3 pos)
    {
        if (pos.y >= hilltopMinElevation)
            return AltarPlacement.Hilltop;

        Collider[] nearby = Physics.OverlapSphere(pos, riverDetectionRadius);
        foreach (var col in nearby)
            if (col.CompareTag("Water"))
                return AltarPlacement.River;

        if (pos.y <= plainsMaxElevation)
            return AltarPlacement.Plains;

        return AltarPlacement.Any;
    }

    private void CheckDivineFavor()
    {
        if (deityChosen) { CancelInvoke(nameof(CheckDivineFavor)); return; }
        if (altar == null || altar.IsFree) return;

        if (altar.FactionID != gameMgr.LocalFactionSlotID) return;

        var handlers = gameMgr.GetService<IResourceManager>()
            .FactionResources[altar.FactionID].ResourceHandlers;

        if (!handlers.ContainsKey(divineFavorType)) return;

        if (handlers[divineFavorType].Amount >= divineThreshold)
            DeitySelectionUI.Instance.Show(placement, this);
    }

    public void SelectDeity(string deityCode)
    {
        if (deityChosen) return;
        deityChosen = true;

        EntityComponentUpgrade upgrade = null;
        switch (deityCode)
        {
            case "gorgon":   upgrade = gorgonUpgrade;   break;
            case "dzues":    upgrade = dzuesUpgrade;    break;
            case "pseidon":  upgrade = pseidonUpgrade;  break;
            case "ares":     upgrade = aresUpgrade;     break;
        }

        upgrade?.LaunchLocal(gameMgr, 0, altar.FactionID);
        DeitySelectionUI.Instance.Hide();
    }
}
