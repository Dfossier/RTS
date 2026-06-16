# Religion System — Gorgon Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build one complete religion thread end-to-end: place Altar on plains → sacrifice Grain → unlock Gorgon deity → grain generation speeds up faction-wide. Then add the Helios totem selection panel that fires at game start.

**Architecture:** Sacrifice reuses the existing `ResourceTraderComponent` (Grain → divine_favor). `AltarManager` (custom MonoBehaviour on the Altar prefab) detects placement context via terrain elevation and water proximity, monitors divine_favor via polling, and presents `DeitySelectionUI` when the threshold is reached. Deity bonuses are applied via `EntityComponentUpgrade.LaunchLocal()` — no custom modifier system needed.

**Tech Stack:** Unity 2019.4+, RTS Engine v2.0 Beta, C#, Unity UI (uGUI + TextMeshPro)

---

## Confirmed RTS Engine APIs

```csharp
// Building construction complete event
(altar.Health as IBuildingHealth).BuildingBuilt += OnAltarBuilt;  // CustomEventHandler<IBuilding, EventArgs>

// Read resource amount
gameMgr.GetService<IResourceManager>().FactionResources[factionID].ResourceHandlers[resourceType].Amount;

// Apply a component upgrade faction-wide
gorgonUpgrade.LaunchLocal(gameMgr, 0, factionID);  // EntityComponentUpgrade.LaunchLocal(IGameManager, int index, int factionID)

// Local player faction ID
gameMgr.LocalFactionSlotID;  // int
```

---

## File Map

**New Scripts:**
- `Assets/Scripts/Religion/AltarManager.cs` — MonoBehaviour on Altar prefab; detects placement, polls divine_favor, shows deity UI, applies upgrade
- `Assets/Scripts/Religion/DeitySelectionUI.cs` — singleton Canvas panel; shows available deities based on placement; triggers AltarManager
- `Assets/Scripts/Religion/TotemSelectionUI.cs` — singleton Canvas panel; shows at game start; pauses game; applies selected totem upgrade

**New Prefabs / Assets (Unity Editor):**
- `Assets/DerekAssets/Resources/Types/divine_favor_resource_type.asset` — new RTS Engine ResourceTypeInfo
- `Assets/Resources/Prefabs/Altar.prefab` — new building with ResourceTraderComponent (sacrifice) and AltarManager
- `Assets/DerekAssets/Upgrades/Components/GorgonGrainGenerator.prefab` — child GameObject prefab with ResourceGenerator, period reduced 15%
- `Assets/DerekAssets/Upgrades/GorgonUpgrade.prefab` — prefab holding EntityComponentUpgrade component (Gorgon)
- `Assets/DerekAssets/Upgrades/Components/HeliosGrainGenerator.prefab` — identical period reduction, separate asset for Helios
- `Assets/DerekAssets/Upgrades/HeliosUpgrade.prefab` — prefab holding EntityComponentUpgrade component (Helios)

**Modified Assets:**
- `Assets/DerekAssets/Resources/Working Files/RTSEngine.prefab` — add divine_favor to ResourceManager
- `Assets/GameScene.unity` — add DeitySelectionCanvas, TotemSelectionCanvas, add Altar to buildable list

---

## Task 1: Verify Phase 1 Production Chain Buildings

**Files:** Read-only verification — no changes unless corrections needed.

- [ ] **Step 1: Open grain_storage_facility prefab in Inspector**

In Unity Project window, select `Assets/Resources/Prefabs/grain_storage_facility.prefab`. Expand the prefab hierarchy and click each child GameObject. Find the one with a `ResourceGenerator` component.

- [ ] **Step 2: Confirm wheat→grain conversion is wired**

On that `ResourceGenerator` component, verify:
- `period` field has a value (default 30 from commit `308fdd4d`)
- `input` field references `wheat_resource_type` asset (not `{fileID: 0}`)
- The output resource references `grain_resource_type`

If `input` is missing, drag `Assets/DerekAssets/Resources/Types/wheat_resource_type.asset` into the input field and save the prefab.

- [ ] **Step 3: Note the grain ResourceGenerator's `code` value**

In the same ResourceGenerator component, note the `code` string (e.g., `grain_storage_ResourceGenerator_Grain`). Write it down — you will need it in Task 7 when configuring the EntityComponentUpgrade.

- [ ] **Step 4: Verify CampFire meat output**

Select `Assets/Resources/Prefabs/CampFire.prefab`. Confirm it has a ResourceGenerator that takes meat as input and outputs cooked_meat. If missing, this is out of scope for this plan — note as a separate follow-up.

- [ ] **Step 5: Commit any corrections**

```bash
cd /mnt/c/Users/dfoss/Desktop/rts_engine_2_beta-master
git add Assets/Resources/Prefabs/
git commit -m "fix: verify and correct production chain building inputs"
```

If no changes were needed, skip the commit.

---

## Task 2: Create divine_favor Resource Type

**Files:**
- Create: `Assets/DerekAssets/Resources/Types/divine_favor_resource_type.asset`
- Modify: `Assets/DerekAssets/Resources/Working Files/RTSEngine.prefab`

- [ ] **Step 1: Duplicate amber resource type**

In Project window: right-click `Assets/DerekAssets/Resources/Types/amber_resource_type.asset` → Duplicate. Rename the copy to `divine_favor_resource_type.asset`.

- [ ] **Step 2: Configure divine_favor fields**

Select `divine_favor_resource_type.asset`. In the Inspector, set:
- `code`: `divine_favor`
- `name`: `Divine Favor`
- Starting amount: `0`
- Max amount: `100`
- Capacity type: limit-based (so it caps at 100)
- Icon: leave as-is or assign a placeholder

- [ ] **Step 3: Register in RTSEngine resource manager**

Open `Assets/DerekAssets/Resources/Working Files/RTSEngine.prefab`. Find the `ResourceManager` component. In the faction resource definitions, add `divine_favor_resource_type` for the player faction (same way amber and lapis lazuli are registered).

- [ ] **Step 4: Commit**

```bash
git add "Assets/DerekAssets/Resources/Types/divine_favor_resource_type.asset"
git add "Assets/DerekAssets/Resources/Working Files/RTSEngine.prefab"
git commit -m "feat: add divine_favor resource type (max 100, starts at 0)"
```

---

## Task 3: Create Altar Building Prefab

**Files:**
- Create: `Assets/Resources/Prefabs/Altar.prefab`

- [ ] **Step 1: Duplicate a simple building as base**

Duplicate `Assets/Resources/Prefabs/grain_storage_facility.prefab` (or the lightest building prefab available). Rename to `Altar.prefab`.

- [ ] **Step 2: Configure building identity**

Select `Altar.prefab`. On the root Building component (RTS Engine):
- Set `code` to `altar`
- Set `name` to `Altar`

Remove any ResourceGenerator components carried over from the source prefab — the Altar does not produce resources directly.

- [ ] **Step 3: Add sacrifice trade (ResourceTraderComponent)**

Add `ResourceTraderComponent` (from `Assets/Scripts/ResourceTraderComponent.cs`) to the Altar root. Configure two offers:
- Offer 0: cost = 5 grain (`grain_resource_type`), reward = 1 divine_favor (`divine_favor_resource_type`)
- Offer 1: cost = 5 cooked_meat, reward = 1 divine_favor

This reuses the same trade infrastructure as the neutral trade buildings.

- [ ] **Step 4: Commit**

```bash
git add "Assets/Resources/Prefabs/Altar.prefab"
git commit -m "feat: add Altar building prefab with sacrifice trade (grain/meat -> divine_favor)"
```

---

## Task 4: Write AltarManager.cs

**Files:**
- Create: `Assets/Scripts/Religion/AltarManager.cs`

- [ ] **Step 1: Create Religion scripts folder**

```bash
mkdir -p "/mnt/c/Users/dfoss/Desktop/rts_engine_2_beta-master/Assets/Scripts/Religion"
```

- [ ] **Step 2: Write AltarManager.cs**

Create `Assets/Scripts/Religion/AltarManager.cs`:

```csharp
using System;
using UnityEngine;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.Health;
using RTSEngine.ResourceExtension;
using RTSEngine.Upgrades;

public class AltarManager : MonoBehaviour
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

    void Start()
    {
        altar = GetComponent<IBuilding>();
        if (altar == null) return;

        gameMgr = altar.GameMgr;

        if (altar.Health is IBuildingHealth buildingHealth)
            buildingHealth.BuildingBuilt += OnAltarBuilt;
    }

    void OnDestroy()
    {
        if (altar?.Health is IBuildingHealth buildingHealth)
            buildingHealth.BuildingBuilt -= OnAltarBuilt;

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

        // Only show UI to local player
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
```

- [ ] **Step 3: Add AltarManager to Altar prefab**

Select `Assets/Resources/Prefabs/Altar.prefab`. Add `AltarManager` component. Set:
- `hilltopMinElevation`: 40
- `plainsMaxElevation`: 25
- `riverDetectionRadius`: 15
- `divineThreshold`: 20
- `divineFavorType`: assign `divine_favor_resource_type.asset`
- Leave upgrade fields empty for now (assigned in Task 8)

- [ ] **Step 4: Commit**

```bash
git add "Assets/Scripts/Religion/AltarManager.cs"
git add "Assets/Resources/Prefabs/Altar.prefab"
git commit -m "feat: add AltarManager — placement detection, divine_favor polling, deity dispatch"
```

---

## Task 5: Write DeitySelectionUI.cs and Scene Canvas

**Files:**
- Create: `Assets/Scripts/Religion/DeitySelectionUI.cs`
- Modify: `Assets/GameScene.unity` — add DeitySelectionCanvas

- [ ] **Step 1: Write DeitySelectionUI.cs**

Create `Assets/Scripts/Religion/DeitySelectionUI.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeitySelectionUI : MonoBehaviour
{
    public static DeitySelectionUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Button gorgonButton;
    [SerializeField] private Button dzuesButton;
    [SerializeField] private Button pseidonButton;
    [SerializeField] private Button aresButton;

    private AltarManager currentAltar;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void Show(AltarManager.AltarPlacement placement, AltarManager altar)
    {
        currentAltar = altar;

        gorgonButton.gameObject.SetActive(false);
        dzuesButton.gameObject.SetActive(false);
        pseidonButton.gameObject.SetActive(false);
        aresButton.gameObject.SetActive(true); // Ares available at any placement

        switch (placement)
        {
            case AltarManager.AltarPlacement.Plains:
                gorgonButton.gameObject.SetActive(true);
                break;
            case AltarManager.AltarPlacement.Hilltop:
                dzuesButton.gameObject.SetActive(true);
                break;
            case AltarManager.AltarPlacement.River:
                pseidonButton.gameObject.SetActive(true);
                break;
        }

        panel.SetActive(true);
    }

    public void Hide() => panel.SetActive(false);

    // Wired to button OnClick in Inspector
    public void OnGorgonSelected()  => currentAltar?.SelectDeity("gorgon");
    public void OnDzuesSelected()   => currentAltar?.SelectDeity("dzues");
    public void OnPseidonSelected() => currentAltar?.SelectDeity("pseidon");
    public void OnAresSelected()    => currentAltar?.SelectDeity("ares");
}
```

- [ ] **Step 2: Create DeitySelectionCanvas in GameScene**

In GameScene hierarchy:
1. Create → UI → Canvas. Name it `DeitySelectionCanvas`.
2. Add `DeitySelectionUI` component to the Canvas root.
3. Create a child Panel named `DeitySelectionPanel`. Set background color to semi-transparent dark.
4. Add a TextMeshPro header: "Choose Your Deity"
5. Add four Buttons as children of the panel:
   - `GorgonButton` — label: "Gorgon (Harvest) — +15% Grain Yield"
   - `DzuesButton` — label: "Dzues (Sky) — +Speed, +Attack"
   - `PseidonButton` — label: "Pseidon (Sea) — +Speed, +Herding"
   - `AresButton` — label: "Ares (War) — +Herding, +Attack"
6. Wire each button's `OnClick()` to the matching `DeitySelectionUI.OnXxxSelected()` method.
7. Assign `panel` and all four buttons to the `DeitySelectionUI` component fields.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Scripts/Religion/DeitySelectionUI.cs"
git add "Assets/GameScene.unity"
git commit -m "feat: add DeitySelectionUI — placement-aware deity choice panel"
```

---

## Task 6: Create Gorgon Upgraded Component Prefab

**Files:**
- Create: `Assets/DerekAssets/Upgrades/Components/GorgonGrainGenerator.prefab`

- [ ] **Step 1: Create Upgrades folder structure**

In Project window: right-click `Assets/DerekAssets/` → Create Folder → name it `Upgrades`. Inside `Upgrades`, create a `Components` subfolder.

- [ ] **Step 2: Find the grain ResourceGenerator child GameObject**

On `Assets/Resources/Prefabs/grain_storage_facility.prefab`, find the child GameObject that contains the grain `ResourceGenerator` component (the one with the code you noted in Task 1 Step 3).

- [ ] **Step 3: Create the upgraded prefab**

Right-click that child GameObject in the Prefab editor → select the child → drag it into `Assets/DerekAssets/Upgrades/Components/` to create a prefab. Name it `GorgonGrainGenerator.prefab`.

- [ ] **Step 4: Reduce period by 15%**

Select `GorgonGrainGenerator.prefab`. On the `ResourceGenerator` component, multiply the `period` value by 0.85. If `period` is `30`, set it to `25.5`. If it's a different value, multiply by 0.85 and round to one decimal place.

- [ ] **Step 5: Commit**

```bash
git add "Assets/DerekAssets/Upgrades/Components/GorgonGrainGenerator.prefab"
git commit -m "feat: add GorgonGrainGenerator prefab — grain generation period -15%"
```

---

## Task 7: Create Gorgon EntityComponentUpgrade Prefab

**Files:**
- Create: `Assets/DerekAssets/Upgrades/GorgonUpgrade.prefab`

- [ ] **Step 1: Create a new empty prefab**

In Project window: right-click `Assets/DerekAssets/Upgrades/` → Create → Prefab. Name it `GorgonUpgrade.prefab`. Open it for editing.

- [ ] **Step 2: Add EntityComponentUpgrade component**

With `GorgonUpgrade.prefab` open in the Prefab editor, add the `EntityComponentUpgrade` component (search in Add Component → RTS Engine → Upgrades → Entity Component Upgrade, or search by name).

- [ ] **Step 3: Configure the EntityComponentUpgrade**

On the `EntityComponentUpgrade` component:
- `Source Entity`: assign `grain_storage_facility.prefab`
- `Source Instance Only`: unchecked (applies faction-wide)
- `Update Spawned Instances`: checked (applies to existing buildings)
- `Upgrades` array: add one element:
  - `sourceComponentCode`: paste the code you noted in Task 1 Step 3 (e.g., `grain_storage_ResourceGenerator_Grain`)
  - `upgradeTarget`: assign `GorgonGrainGenerator.prefab`

- [ ] **Step 4: Commit**

```bash
git add "Assets/DerekAssets/Upgrades/GorgonUpgrade.prefab"
git commit -m "feat: add GorgonUpgrade EntityComponentUpgrade prefab"
```

---

## Task 8: Wire AltarManager and Add Altar to Build Menu

**Files:**
- Modify: `Assets/Resources/Prefabs/Altar.prefab`
- Modify: `Assets/GameScene.unity`

- [ ] **Step 1: Assign Gorgon upgrade to AltarManager**

Select `Assets/Resources/Prefabs/Altar.prefab`. On the `AltarManager` component:
- `gorgonUpgrade`: assign `GorgonUpgrade.prefab`
- Leave `dzuesUpgrade`, `pseidonUpgrade`, `aresUpgrade` empty for now

- [ ] **Step 2: Add Altar to the player's buildable buildings list**

Open `Assets/GameScene.unity`. Find the `BuildingManager` component in the scene hierarchy. In the list of buildings available to the player faction, add `Altar.prefab`.

Also ensure the `ResourceTraderComponent` on the Altar is connected to the existing trade UI (check how `TradeBuilding.prefab` wires its `ResourceTraderComponent` to `TradeBuildingPanelUIHandler` and replicate if needed).

- [ ] **Step 3: Commit**

```bash
git add "Assets/Resources/Prefabs/Altar.prefab"
git add "Assets/GameScene.unity"
git commit -m "feat: wire AltarManager to GorgonUpgrade, add Altar to build menu"
```

---

## Task 9: Playtest the Gorgon Vertical Slice

**Files:** None — manual verification only.

- [ ] **Step 1: Run the game**

Press Play in Unity. Confirm the scene loads with no errors in the console.

- [ ] **Step 2: Build Altar on flat low-elevation ground (plains)**

Assign a villager to build the Altar on flat terrain well below elevation 25. Wait for construction to complete.

- [ ] **Step 3: Sacrifice Grain to accumulate Divine Favor**

Select the Altar. Use the trade UI to sacrifice Grain → Divine Favor. Sacrifice at least 4 times (5 grain × 4 = 20 divine_favor). Watch the divine_favor counter in the resource UI climb.

- [ ] **Step 4: Verify DeitySelectionUI appears**

When divine_favor hits 20, the `DeitySelectionUI` panel should appear. Confirm:
- Gorgon button is visible
- Ares button is visible
- Dzues and Pseidon buttons are hidden

- [ ] **Step 5: Select Gorgon and verify bonus**

Click Gorgon. The panel should close. In the Unity Inspector (with the grain_storage_facility selected), confirm its grain ResourceGenerator `period` has changed from 30 to 25.5. Grain should generate noticeably faster.

- [ ] **Step 6: Test hilltop placement**

Build a second Altar on high-elevation terrain (y > 40). Sacrifice grain again to divine_favor 20. Confirm DeitySelectionUI now shows Dzues + Ares (not Gorgon or Pseidon).

- [ ] **Step 7: Commit a playtest note**

```bash
git commit --allow-empty -m "test: Gorgon vertical slice verified — altar placement, sacrifice, deity upgrade all working"
```

---

## Task 10: Write TotemSelectionUI.cs and Scene Canvas

**Files:**
- Create: `Assets/Scripts/Religion/TotemSelectionUI.cs`
- Modify: `Assets/GameScene.unity` — add TotemSelectionCanvas

- [ ] **Step 1: Write TotemSelectionUI.cs**

Create `Assets/Scripts/Religion/TotemSelectionUI.cs`:

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;
using RTSEngine.Game;
using RTSEngine.Upgrades;

public class TotemSelectionUI : MonoBehaviour, IPreRunGameService
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button heliosButton;
    [SerializeField] private Button aeolusButton;
    [SerializeField] private Button lykaiosButton;
    [SerializeField] private Button potamosButton;
    [SerializeField] private Button lithosButton;

    [Header("Totem Upgrades")]
    [SerializeField] private EntityComponentUpgrade heliosUpgrade;
    [SerializeField] private EntityComponentUpgrade aeolusUpgrade;
    [SerializeField] private EntityComponentUpgrade lykaiosUpgrade;
    [SerializeField] private EntityComponentUpgrade potamosUpgrade;
    [SerializeField] private EntityComponentUpgrade lithosUpgrade;

    private IGameManager gameMgr;

    // Called by RTS Engine's service locator when the game initializes
    public void Init(IGameManager gameMgr)
    {
        this.gameMgr = gameMgr;
        gameMgr.GameBuilt += OnGameBuilt;
    }

    private void OnGameBuilt(IGameManager gm, EventArgs args)
    {
        Time.timeScale = 0f;
        panel.SetActive(true);
    }

    private void ApplyTotem(EntityComponentUpgrade upgrade)
    {
        if (upgrade == null) { ClosePanel(); return; }

        upgrade.LaunchLocal(gameMgr, 0, gameMgr.LocalFactionSlotID);
        ClosePanel();
    }

    private void ClosePanel()
    {
        Time.timeScale = 1f;
        panel.SetActive(false);
    }

    // Wired to button OnClick in Inspector
    public void OnHeliosSelected()  => ApplyTotem(heliosUpgrade);
    public void OnAeolusSelected()  => ApplyTotem(aeolusUpgrade);
    public void OnLykaiosSelected() => ApplyTotem(lykaiosUpgrade);
    public void OnPotamosSelected() => ApplyTotem(potamosUpgrade);
    public void OnLithosSelected()  => ApplyTotem(lithosUpgrade);
}
```

- [ ] **Step 2: Place TotemSelectionUI on the GameManager object**

`IPreRunGameService` components are discovered via `GetComponentsInChildren` on the GameManager GameObject (confirmed from `GameSpeedController.cs` which says "Add this component to a child GameObject of the GameManager object"). 

In GameScene hierarchy, find the GameManager root GameObject. Create a child GameObject named `TotemService`. Add `TotemSelectionUI` to `TotemService`. The GameManager will auto-discover it at startup and call `Init(gameMgr)`.

- [ ] **Step 3: Create TotemSelectionCanvas in GameScene**

In GameScene hierarchy:
1. Create → UI → Canvas at root level (NOT under GameManager — this is a separate UI object). Name it `TotemSelectionCanvas`.
2. Do NOT add `TotemSelectionUI` to this canvas — it lives on the `TotemService` child of GameManager from Step 2 above.
3. Create a child Panel named `TotemSelectionPanel`. Render it above everything (sort order above DeitySelectionCanvas).
4. Add a TextMeshPro header: "Choose Your Nature Aspect"
5. Add five Buttons:
   - `HeliosButton` — label: "Helios (Sun) — +15% Farm Produce"
   - `AeolusButton` — label: "Aeolus (Wind) — +15% Movement Speed"
   - `LykaiosButton` — label: "Lykaios (Wolf) — +15% Damage vs Animals"
   - `PotamosButton` — label: "Potamos (River) — +15% Fishing Yield"
   - `LithosButton` — label: "Lithos (Stone) — +15% Construction Speed"
6. Wire each button's `OnClick()` to `TotemSelectionUI.OnXxxSelected()`.
7. Assign `panel` and all five buttons to the `TotemSelectionUI` component fields.

- [ ] **Step 4: Commit**

```bash
git add "Assets/Scripts/Religion/TotemSelectionUI.cs"
git add "Assets/GameScene.unity"
git commit -m "feat: add TotemSelectionUI — game-start totem choice, pauses until selected"
```

---

## Task 11: Create Helios EntityComponentUpgrade Prefab

**Files:**
- Create: `Assets/DerekAssets/Upgrades/Components/HeliosGrainGenerator.prefab`
- Create: `Assets/DerekAssets/Upgrades/HeliosUpgrade.prefab`

Note: Helios and Gorgon both buff grain generation by 15% and use identical period values. They are separate assets so their effects can diverge in future updates.

- [ ] **Step 1: Duplicate GorgonGrainGenerator.prefab**

Duplicate `Assets/DerekAssets/Upgrades/Components/GorgonGrainGenerator.prefab`. Rename to `HeliosGrainGenerator.prefab`. Period value should already be correct (×0.85 of original).

- [ ] **Step 2: Duplicate GorgonUpgrade.prefab**

Duplicate `Assets/DerekAssets/Upgrades/GorgonUpgrade.prefab`. Rename to `HeliosUpgrade.prefab`. Open it — change the `upgradeTarget` in the upgrades array from `GorgonGrainGenerator.prefab` to `HeliosGrainGenerator.prefab`. All other fields stay the same.

- [ ] **Step 3: Assign to TotemSelectionUI**

Select `TotemSelectionCanvas` in GameScene. On the `TotemSelectionUI` component, assign `HeliosUpgrade.prefab` to the `heliosUpgrade` field. Leave the other four totem upgrade fields empty for now.

- [ ] **Step 4: Commit**

```bash
git add "Assets/DerekAssets/Upgrades/Components/HeliosGrainGenerator.prefab"
git add "Assets/DerekAssets/Upgrades/HeliosUpgrade.prefab"
git add "Assets/GameScene.unity"
git commit -m "feat: add Helios totem upgrade (+15% grain generation)"
```

---

## Task 12: Playtest Full Totem + Deity Flow

**Files:** None — manual verification only.

- [ ] **Step 1: Start a fresh game session**

Press Play. Confirm `TotemSelectionUI` panel appears immediately and the game is paused (no units move, no resources tick).

- [ ] **Step 2: Select Helios totem**

Click the Helios button. Confirm:
- Panel closes
- `Time.timeScale` returns to 1 (game resumes)
- In the grain_storage_facility Inspector, the grain ResourceGenerator `period` is now 25.5 (was 30)

- [ ] **Step 3: Test Helios + Gorgon stacking**

Build an Altar on plains. Sacrifice grain to 20 divine_favor. Select Gorgon. In the grain_storage_facility Inspector, the period should now be approximately 21.7 (25.5 × 0.85). Both bonuses have stacked.

- [ ] **Step 4: Test empty upgrade slot doesn't crash**

Start another game session. Select any totem other than Helios (its upgrade field is empty). The game should resume without errors — `ApplyTotem(null)` calls `ClosePanel()` gracefully without crashing.

- [ ] **Step 5: Commit**

```bash
git commit --allow-empty -m "test: full totem + deity flow verified — Helios + Gorgon stacking confirmed"
```
