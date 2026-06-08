using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

using RTSEngine;
using RTSEngine.Game;
using RTSEngine.Event;
using RTSEngine.Entities;
using RTSEngine.Selection;
using RTSEngine.Logging;

/// <summary>
/// Custom selection panel for neutral trade buildings.
///
/// The stock task panel only shows buttons for the local player's OWN faction entities
/// (TaskPanelUIHandler -> GetEntitiesDictionary(localPlayerFaction: true)), so a neutral building
/// can't use it. This handler fills that gap: when the player selects a building that has a
/// <see cref="ResourceTraderComponent"/>, it shows one "buy" button per trade offer.
///
/// A trade is allowed only when the player can afford the cost AND one of the player's units is
/// within <see cref="tradeRadius"/> of the building (the "require a unit nearby" rule). Otherwise
/// the buttons are greyed out and a hint is shown.
///
/// Implemented as an IPreRunGameService (like the other BasicUI handlers), so it must live under
/// the RTS Engine GameManager hierarchy to be auto-initialized.
/// </summary>
public class TradeBuildingPanelUIHandler : MonoBehaviour, IPreRunGameService
{
    #region Inspector
    [Header("Panel")]
    [SerializeField, Tooltip("Root panel object, shown only while a trade building is selected.")]
    private GameObject panel = null;
    [SerializeField, Tooltip("Optional title text, e.g. the building name.")]
    private TMP_Text titleText = null;
    [SerializeField, Tooltip("Optional hint text, shown when no friendly unit is in range.")]
    private TMP_Text hintText = null;
    [SerializeField, Tooltip("Message shown in the hint text when no unit is nearby.")]
    private string noUnitNearbyHint = "Move a unit closer to trade";

    [Header("Buttons")]
    [SerializeField, Tooltip("Parent transform under which one button is created per offer.")]
    private Transform buttonsParent = null;
    [SerializeField, Tooltip("Button prefab for a single offer. Should contain a TMP text child for the label.")]
    private Button offerButtonPrefab = null;

    [Header("Trade gate")]
    [SerializeField, Tooltip("A friendly unit must be within this distance of the building to trade."), Min(0f)]
    private float tradeRadius = 15f;
    [SerializeField, Tooltip("How often (seconds) to re-check affordability / nearby-unit while the panel is open."), Min(0.05f)]
    private float refreshInterval = 0.25f;
    #endregion

    #region State / services
    private IGlobalEventPublisher globalEvent;
    private ISelectionManager selectionMgr;
    private IGameLoggingService logger;
    private IGameManager gameMgr;

    private IEntity currEntity;
    private ResourceTraderComponent currTrader;
    private readonly List<Button> spawnedButtons = new List<Button>();
    private float refreshTimer;
    #endregion

    #region Init / teardown
    public void Init(IGameManager gameMgr)
    {
        this.gameMgr = gameMgr;
        this.globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
        this.selectionMgr = gameMgr.GetService<ISelectionManager>();
        this.logger = gameMgr.GetService<IGameLoggingService>();

        globalEvent.EntitySelectedGlobal += HandleEntitySelectionUpdate;
        globalEvent.EntityDeselectedGlobal += HandleEntitySelectionUpdate;

        Hide();
    }

    private void OnDestroy()
    {
        if (globalEvent == null)
            return;

        globalEvent.EntitySelectedGlobal -= HandleEntitySelectionUpdate;
        globalEvent.EntityDeselectedGlobal -= HandleEntitySelectionUpdate;
    }
    #endregion

    #region Selection handling
    // Subscribed to both EntitySelected/Deselected; param is the more general EventArgs (contravariant).
    private void HandleEntitySelectionUpdate(IEntity entity, EventArgs e)
    {
        if (selectionMgr.Count == 1)
            Show(selectionMgr.GetSingleSelectedEntity(EntityType.all));
        else
            Hide();
    }

    private void Show(IEntity entity)
    {
        // Only neutral buildings carrying a trader component get the trade panel.
        ResourceTraderComponent trader = entity.IsValid() && entity.IsFree
            ? entity.EntityComponents.Values.OfType<ResourceTraderComponent>().FirstOrDefault()
            : null;

        if (trader == null)
        {
            Hide();
            return;
        }

        // Already showing this building - just refresh state, don't rebuild the buttons.
        if (entity == currEntity)
        {
            RefreshButtonStates();
            return;
        }

        currEntity = entity;
        currTrader = trader;

        if (titleText)
            titleText.text = entity.Name;

        BuildButtons();
        if (panel)
            panel.SetActive(true);

        refreshTimer = 0f;
        RefreshButtonStates();

        // --- TEMP DIAGNOSTIC: prints why the buy buttons are enabled/disabled. Remove once working. ---
        {
            int fid = gameMgr.LocalFactionSlotID;
            bool nearby = IsFriendlyUnitNearby();
            float nearest = NearestUnitDistance();
            var sb = new System.Text.StringBuilder();
            sb.Append($"[TradePanel] '{entity.Name}' localFactionID={fid} unitNearby={nearby} (tradeRadius={tradeRadius}, nearestUnit={(nearest < 0f ? "NONE" : nearest.ToString("0.0"))}). ");
            for (int i = 0; i < currTrader.Offers.Count; i++)
                sb.Append($"offer[{i}]='{currTrader.Offers[i].label}' canAfford={currTrader.CanAfford(i, fid)}; ");
            Debug.Log(sb.ToString(), this);
        }
    }

    private float NearestUnitDistance()
    {
        if (currEntity == null || gameMgr.LocalFactionSlot == null || gameMgr.LocalFactionSlot.FactionMgr == null)
            return -1f;
        Vector3 p = currEntity.transform.position;
        float best = -1f;
        foreach (var u in gameMgr.LocalFactionSlot.FactionMgr.Units)
        {
            if (u == null) continue;
            float d = Vector3.Distance(u.transform.position, p);
            if (best < 0f || d < best) best = d;
        }
        return best;
    }

    private void Hide()
    {
        currEntity = null;
        currTrader = null;
        ClearButtons();

        if (hintText)
            hintText.gameObject.SetActive(false);
        if (panel)
            panel.SetActive(false);
    }
    #endregion

    #region Buttons
    private void BuildButtons()
    {
        ClearButtons();

        if (offerButtonPrefab == null || buttonsParent == null)
        {
            logger?.LogError("[TradeBuildingPanelUIHandler] Button prefab or parent not assigned.", source: this);
            return;
        }

        IReadOnlyList<TradeOffer> offers = currTrader.Offers;
        for (int i = 0; i < offers.Count; i++)
        {
            int offerIndex = i; // capture for the closure
            Button button = Instantiate(offerButtonPrefab, buttonsParent);
            button.transform.localScale = Vector3.one;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label)
                label.text = offers[i].label;

            button.onClick.AddListener(() => OnOfferClicked(offerIndex));
            spawnedButtons.Add(button);
        }
    }

    private void ClearButtons()
    {
        foreach (Button button in spawnedButtons)
        {
            if (button == null)
                continue;
            button.onClick.RemoveAllListeners();
            Destroy(button.gameObject);
        }
        spawnedButtons.Clear();
    }

    private void OnOfferClicked(int offerIndex)
    {
        if (currTrader == null)
            return;

        // Re-check the gate at click time (state may have changed since the last refresh).
        if (!IsFriendlyUnitNearby() || !currTrader.CanAfford(offerIndex, gameMgr.LocalFactionSlotID))
            return;

        currTrader.ExecuteTrade(offerIndex, gameMgr.LocalFactionSlotID);
        RefreshButtonStates();
    }

    private void RefreshButtonStates()
    {
        if (currTrader == null)
            return;

        bool unitNearby = IsFriendlyUnitNearby();

        if (hintText)
        {
            hintText.gameObject.SetActive(!unitNearby);
            if (!unitNearby)
                hintText.text = noUnitNearbyHint;
        }

        int localFactionID = gameMgr.LocalFactionSlotID;
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] == null)
                continue;
            spawnedButtons[i].interactable = unitNearby && currTrader.CanAfford(i, localFactionID);
        }
    }
    #endregion

    #region Nearby-unit gate
    private bool IsFriendlyUnitNearby()
    {
        if (currEntity == null || gameMgr.LocalFactionSlot == null || gameMgr.LocalFactionSlot.FactionMgr == null)
            return false;

        Vector3 buildingPos = currEntity.transform.position;
        float sqrRadius = tradeRadius * tradeRadius;

        foreach (var unit in gameMgr.LocalFactionSlot.FactionMgr.Units)
        {
            if (unit == null)
                continue;
            if ((unit.transform.position - buildingPos).sqrMagnitude <= sqrRadius)
                return true;
        }

        return false;
    }
    #endregion

    #region Refresh loop
    private void Update()
    {
        if (currTrader == null)
            return;

        refreshTimer += Time.deltaTime;
        if (refreshTimer < refreshInterval)
            return;

        refreshTimer = 0f;
        RefreshButtonStates();
    }
    #endregion
}
