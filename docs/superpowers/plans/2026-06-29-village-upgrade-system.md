# Village Upgrade System — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the Farm building and Village upgrade mechanic — seed buildings (CampFire, Hunting Camp) transform permanently into a Village Center when 15 animals are garrisoned, 500 grain and 500 divine favor are in the faction resource pool.

**Architecture:** Three pieces: (1) a Farm building that auto-generates grain scaled by garrisoned villager count using the existing `ResourceGenerator.requireGarrisonedWorkers` field; (2) a `VillageUpgradeCondition` MonoBehaviour that polls a dedicated animal carrier + two resource levels and calls `EntityComponentUpgrade.LaunchLocal` when all three are met; (3) Village Center prefabs that each seed building permanently upgrades into. Market Outpost and Mining Camp seed buildings are out of scope — only CampFire and Hunting Camp are wired in this plan.

**Tech Stack:** Unity 2022.3.36f1, RTS Engine 2, Built-in Render Pipeline. No new third-party dependencies.

---

## Key Reference Values

| Item | Value |
|---|---|
| Grain resource guid | `c0ced80dc07a894428ddaeb3e67467ca` |
| ResourceGenerator script guid | `edddac3108d6221418e8a42fc680a8e2` |
| UnitCarrier script guid | `6c6ba43bf9895db4fae646c44ad26ae3` |
| EntityComponentUpgrade trigger | `upgrade.LaunchLocal(gameMgr, upgradeIndex, factionID)` |
| Carrier current count property | `UnitCarrier.CurrAmount` |
| Resource amount check pattern | `resourceMgr.FactionResources[factionID].ResourceHandlers[type].Amount` |
| Animal garrison threshold | 15 |
| Grain threshold | 500 |
| Divine favor threshold | 500 |
| CampFire prefab | `Assets/Resources/Prefabs/CampFire.prefab` |
| Hunting Camp prefab | `Assets/Resources/Prefabs/hunting_camp.prefab` |
| Farm building limit (already set on CampFire border) | 6 per territory |

---

## Task 1: VillageUpgradeCondition Script

**Files:**
- Create: `Assets/Scripts/VillageUpgradeCondition.cs`

The script holds a direct serialized reference to the animal `UnitCarrier` on the same building — no marker component needed. The reference is assigned in the Inspector when the component is added to each seed building prefab in Task 5.

- [ ] **Step 1: Write the script**

```csharp
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
    [SerializeField] private EntityComponentUpgrade villageUpgrade;

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
```

- [ ] **Step 2: Verify script compiles**

Open Unity and check the Console for compile errors. Fix any before continuing.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/VillageUpgradeCondition.cs
git commit -m "feat: add VillageUpgradeCondition script"
```

---

## Task 2: Farm Building Prefab

**Files:**
- Create: `Assets/Resources/Prefabs/farm.prefab`

The Farm uses the existing `ResourceGenerator` with `requireGarrisonedWorkers: 1` — no custom script needed. Each garrisoned villager adds a full production cycle (`workerSpeedMultiplier: 1`). Capacity is 5 villagers. Produces 4 grain every 5 seconds per active villager.

The Farm is placed *inside* an existing territory (CampFire or Hunting Camp). It must not have a `BuildingBorder` component — that component makes a building a territory center, which the Farm is not. The CampFire's border already limits farms to 6 per territory via the `farm` building code.

- [ ] **Step 1: Duplicate CampFire prefab as starting point**

In Unity Project window: right-click `Assets/Resources/Prefabs/CampFire.prefab` → Duplicate. Rename to `farm`.

- [ ] **Step 2: Configure the building entity component**

On the root GameObject's building entity MonoBehaviour, set:
- `_name`: Farm
- `code`: farm
- `description`: Garrison villagers to generate grain. More villagers produce more.
- `category`: civilian_building

- [ ] **Step 3: Remove the BuildingBorder component**

On the `building_extension` child GameObject, remove the `BuildingBorder` component (guid: `5cec133f991d51641aad6d481eff731a`). This prevents the Farm from claiming its own territory — it belongs inside the CampFire or Hunting Camp's existing territory, which is what enforces the 6-farm limit.

- [ ] **Step 4: Configure the grain ResourceGenerator**

On the `grain_generator` child GameObject, set:

```
code: farm_ResourceGenerator_Grain
requireGarrisonedWorkers: 1
workerSpeedMultiplier: 1
period: 5
resources:
  - type: grain (guid c0ced80dc07a894428ddaeb3e67467ca), amount: 4
requiredResources: []
collectionThreshold:
  - type: grain, amount: 20
stopGeneratingOnThresholdMet: 0
autoCollect: 1
```

- [ ] **Step 5: Remove the lumber generator**

Delete the `lumber_generator` child GameObject.

- [ ] **Step 6: Configure the villager carrier**

On the `UnitCarrier` component:
- `capacity`: 5
- `targetPicker.categories`: `villager` (replace `military_foot_unit`)
- Keep `carrierPositions` referencing the existing `unit_carrier_positions` transforms
- `ejectOnDestroy`: 1

- [ ] **Step 7: Verify in Play mode**

Place a Farm inside a CampFire territory. Garrison a villager — confirm grain increases. Garrison a second — confirm grain increases at double rate. Eject both — confirm generation stops.

- [ ] **Step 8: Commit**

```bash
git add Assets/Resources/Prefabs/farm.prefab
git commit -m "feat: add Farm building prefab with garrison-scaled grain generation"
```

---

## Task 3: Animal Carrier on Seed Buildings

CampFire and Hunting Camp each need a dedicated `UnitCarrier` that accepts tamed animals. This is separate from any existing villager or military carrier — it tracks the herd count for the village upgrade condition. The `VillageUpgradeCondition` script holds a direct serialized reference to this carrier (assigned in Task 5).

**Files:**
- Modify: `Assets/Resources/Prefabs/CampFire.prefab`
- Modify: `Assets/Resources/Prefabs/hunting_camp.prefab`

- [ ] **Step 1: Add animal carrier to CampFire**

Open `CampFire.prefab`. Add a new child GameObject named `animal_carrier_node`. Add a `UnitCarrier` component with:

```
code: animal_carrier
capacity: 20
targetPicker:
  categories: [animal]
  targetUnits: 1
  targetBuildings: 0
carrierPositions: []
ejectOnDestroy: 1
freeFactionBehaviour:
  allowFreeFaction: 0
  allowLocalPlayer: 1
  updateFactionOnOccupy: 0
  freeOnEjection: 1
```

- [ ] **Step 2: Add animal carrier to Hunting Camp**

Repeat Step 1 on `hunting_camp.prefab` with identical settings.

- [ ] **Step 3: Verify tamed animal can garrison**

In Play mode: train a Herder, tame a wild animal, garrison it into the Hunting Camp. Confirm `CurrAmount` increments on the animal carrier (check via Unity Inspector during play).

- [ ] **Step 4: Commit**

```bash
git add Assets/Resources/Prefabs/CampFire.prefab Assets/Resources/Prefabs/hunting_camp.prefab
git commit -m "feat: add animal carrier to CampFire and Hunting Camp"
```

---

## Task 4: Village Center Prefabs

One Village Center prefab per seed building. Each is the permanent in-place replacement when the upgrade fires. Created by duplicating the seed building and adjusting identity.

**Files:**
- Create: `Assets/Resources/Prefabs/village_center_campfire.prefab`
- Create: `Assets/Resources/Prefabs/village_center_hunting_camp.prefab`
- Create: `Assets/Resources/Upgrades/village_upgrade_campfire.asset`
- Create: `Assets/Resources/Upgrades/village_upgrade_hunting_camp.asset`

- [ ] **Step 1: Create village_center_campfire prefab**

Duplicate `CampFire.prefab`, rename to `village_center_campfire`. Set on the building entity component:
- `_name`: Village (Agricultural)
- `code`: village_center_campfire
- `description`: A thriving agricultural settlement. Farm capacity increased within territory.

On the territory border component increase `size` from 30 to 45 and raise the farm building limit from 6 to 10.

- [ ] **Step 2: Create village_center_hunting_camp prefab**

Duplicate `hunting_camp.prefab`, rename to `village_center_hunting_camp`. Set on the building entity component:
- `_name`: Village (Frontier)
- `code`: village_center_hunting_camp
- `description`: A frontier settlement. Military units trained faster.

Add a `BuildingBorder` component (guid: `5cec133f991d51641aad6d481eff731a`) — the Hunting Camp currently has none. Set `size`: 45, building limits: farm (6), house (5).

- [ ] **Step 3: Create EntityComponentUpgrade assets**

In Unity, create two upgrade assets at `Assets/Resources/Upgrades/`:

**`village_upgrade_campfire.asset`**
- Target: the building entity component on `village_center_campfire.prefab`

**`village_upgrade_hunting_camp.asset`**
- Target: the building entity component on `village_center_hunting_camp.prefab`

- [ ] **Step 4: Commit**

```bash
git add Assets/Resources/Prefabs/village_center_campfire.prefab
git add Assets/Resources/Prefabs/village_center_hunting_camp.prefab
git add Assets/Resources/Upgrades/
git commit -m "feat: add Village Center prefabs and upgrade assets"
```

---

## Task 5: Wire VillageUpgradeCondition on Seed Buildings

Add `VillageUpgradeCondition` to CampFire and Hunting Camp and assign all Inspector references.

**Files:**
- Modify: `Assets/Resources/Prefabs/CampFire.prefab`
- Modify: `Assets/Resources/Prefabs/hunting_camp.prefab`

- [ ] **Step 1: Wire CampFire**

Open `CampFire.prefab`. On the root GameObject add `VillageUpgradeCondition`. Assign:
- `animalCarrier`: the `UnitCarrier` on the `animal_carrier_node` child (added in Task 3)
- `grainType`: Grain `ResourceTypeInfo` asset (guid `c0ced80dc07a894428ddaeb3e67467ca`)
- `divineFavorType`: Divine Favor `ResourceTypeInfo` asset (same asset used in `AltarManager.divineFavorType` — find it by opening the Altar prefab in the Inspector)
- `villageUpgrade`: `village_upgrade_campfire.asset`
- Leave thresholds at defaults (15, 500, 500)

- [ ] **Step 2: Wire Hunting Camp**

Repeat on `hunting_camp.prefab`, assigning `village_upgrade_hunting_camp.asset` instead.

- [ ] **Step 3: End-to-end test**

In Play mode:
1. Build a Hunting Camp. Train a Herder. Tame and garrison 15 animals.
2. Accumulate 500 grain and 500 divine favor (temporarily lower thresholds on the component if needed for testing, then restore).
3. Confirm the Hunting Camp transforms into the Village (Frontier) building in-place.
4. Confirm territory radius expands.
5. Confirm garrisoned animals remain after the upgrade.
6. Repeat with CampFire → Village (Agricultural).

- [ ] **Step 4: Restore thresholds if lowered for testing**

Ensure `animalGarrisonThreshold: 15`, `grainThreshold: 500`, `divineFavorThreshold: 500` on both prefabs before committing.

- [ ] **Step 5: Commit**

```bash
git add Assets/Resources/Prefabs/CampFire.prefab Assets/Resources/Prefabs/hunting_camp.prefab
git commit -m "feat: wire VillageUpgradeCondition on CampFire and Hunting Camp"
```

---

## Out of Scope (Future Work)

- Market Outpost and Mining Camp seed buildings (prefabs don't exist yet)
- Village identity bonuses beyond territory radius expansion
- Farm/herd symbiosis (garrisoned animals boosting nearby farm yield)
- Visual differentiation of Village Center models
- Multiple Village Centers per map
