# Production Chains & Religion System — Design Spec
**Date:** 2026-06-14 (revised 2026-06-15)
**Project:** Bronze Age Collapse (RTS Engine v2.0 Beta)
**Scope:** Phase 1 (production chains) + Phase 2 Tier 1–2 (religion: totem + altar/deity)

---

## Overview

The core experience is geography-driven specialization: your biome determines what you can produce efficiently, which determines your identity, which shapes your trade relationships and ultimately your path to conquest. Production chains give resources meaningful depth, and the religion system gives factions cultural identity from the first moment of play.

**Vertical slice goal:** Get one complete thread playable end-to-end before expanding.
Wheat → Grain Storage Facility → Grain → Sacrifice at Altar → Gorgon deity unlocked → grain yield bonus active.

---

## Section 1: Production Chains

### Current State (as of 2026-06-15)

Most resource types and processing buildings already exist. Gaps are noted below.

**Resource types already defined:** wheat, grain, meat, lumber, roughStone, stone (finished), livestock, copperOre, tinOre, amber, lapisLazuli

**Buildings already in place:**
- `horticultural_plot` — wheat farming
- `hunting_camp` — meat production
- `grain_storage_facility` — grain storage + conversion (wheat → grain happens here, not a dedicated mill)
- `quarry` — stone processing
- `CampFire` — has grain and lumber generator components

**Deferred:** Dedicated Lumber Yard building is out of scope for this phase.

### Processing Buildings

| Building | Input | Output | Worker Slots | Status |
|---|---|---|---|---|
| Grain Storage Facility | Wheat | Grain | 1–2 | Exists — verify conversion is wired |
| Cookfire | Raw Meat | Cooked Meat | 1 | Exists — verify output |
| Quarry | Rough Stone | Finished Stone | 1–2 | Exists — verify output |
| Lumber Yard | Raw Wood | Lumber | 1–2 | Deferred |

### Dual-Use Outputs

| Resource | Use A | Use B |
|---|---|---|
| Grain | Food (population consumption) | Sacrifice (religion system) |
| Cooked Meat | Food (population consumption) | Sacrifice (religion system) |
| Lumber | Tier-2 building material | Trade export |
| Finished Stone | Tier-2 building material | Trade export |

Raw stone and raw wood remain valid for basic (tier-1) buildings. Finished stone and lumber unlock advanced (tier-2) buildings.

### Geography & Specialization

Biome placement naturally advantages certain chains:
- Fertile plains → wheat surplus → grain specialist → feeds neighbors
- Forest biome → wood surplus → lumber specialist
- River valley → wheat + water → grain + fishing combo
- Rocky highland → stone surplus → finished stone specialist

---

## Section 2: Religion System (Tier 1 + Tier 2)

### Design Intent

Religion is the connective tissue between economy, identity, and diplomacy. It is historically grounded: ancient societies used religion to enable large-scale cooperation, inspire excellence, and differentiate cultures. The system reflects this — earlier tiers cost little but give modest rewards; later tiers require investment but define the faction's identity.

### Implementation Architecture

**All bonuses are implemented via the RTS Engine Entity Component Upgrade system.** Each totem and deity maps to a pre-configured `EntityComponentUpgrade` asset containing an upgraded component prefab with adjusted stat values. This avoids any custom modifier system and uses 100% existing engine infrastructure.

- **Totem bonuses** are applied at game start by a lightweight custom script that calls `IEntityComponentUpgradeManager.LaunchLocal()` with the selected totem's upgrade asset.
- **Deity bonuses** are applied via `UpgradeLauncher` tasks on the Altar building — deities appear as researchable tasks in the existing RTS Engine task UI. No custom panel needed.

### Tier 1 — Totem (Game Start)

A UI panel appears at game start (before the player can issue commands) presenting five totem choices. Selection is permanent. The chosen totem's `EntityComponentUpgrade` is immediately applied faction-wide.

| Totem | English | Bonus | Upgraded Component |
|---|---|---|---|
| Helios | (Sun) | +15% farm produce | ResourceGenerator with period × 0.85 |
| Aeolus | (Wind) | +15% unit movement speed | UnitMovement with speed × 1.15 |
| Lykaios | (Wolf) | +15% damage vs. animals | Attack component with damage × 1.15 |
| Potamos | (River) | +15% water/fishing resource yield | ResourceGenerator (fishing) with period × 0.85 |
| Lithos | (Stone) | +15% construction speed | Builder component with speed × 1.15 |

Totem is permanent — it cannot be changed after selection.

**Custom code required:** One script (`TotemSelectionManager`) that:
1. Shows the selection panel at game start and pauses unit commands
2. On selection, calls `IEntityComponentUpgradeManager.LaunchLocal()` for the local player's faction
3. Closes the panel and resumes normal play

### Tier 2 — Altar & Chief Deity (Mid Game)

The Altar is a buildable structure. Placement determines which deity tasks are visible in the `UpgradeLauncher` UI. The player selects a deity by completing its task (instant or short timer). The deity's `EntityComponentUpgrade` is then applied faction-wide. Deity choice is permanent per altar.

#### Deity Unlock Conditions

| Deity | Unlock Condition |
|---|---|
| Dzues (Zeus) | Altar built at high elevation / hilltop |
| Gorgon | Altar built on fertile plains |
| Pseidon (Poseidon) | Altar built at river source or coastline |
| Ares | Any altar placement |

**Custom code required:** One script (`AltarPlacementDetector`) that:
1. On altar construction complete, samples terrain elevation and biome at the altar's position
2. Enables/disables the relevant deity `UpgradeLauncher` tasks based on placement result

#### Deity Bonuses

| Deity | Domain | Bonuses | Upgraded Components |
|---|---|---|---|
| Dzues (Zeus) | Sky / Leadership | +movement speed, +attack damage | UnitMovement × 1.15, Attack × 1.15 |
| Gorgon | Harvest / Fertility | +grain yield | ResourceGenerator (grain) period × 0.85 |
| Pseidon (Poseidon) | Sea / Horses | +movement speed, +herding bonus | UnitMovement × 1.15, Converter speed × 1.15 |
| Ares | War / Livestock | +herding bonus, +attack damage | Converter speed × 1.15, Attack × 1.15 |

*Note: Poseidon Hippios (god of horses) makes the herding bonus historically grounded.*

#### Sacrifice

Cooked Meat or Grain can be sacrificed at the altar to accumulate **Divine Favor** — a resource type defined in RTS Engine. Divine Favor is the currency for activating deity tasks. This creates a direct tradeoff: feed your population or appease your god.

### Tier 3 — Temple (Late Game, Out of Scope)

Tier 3 design deferred. Temple infrastructure and advanced deity powers will be specced separately.

---

## Vertical Slice — Implementation Order

Build in this order to get something playable as fast as possible:

1. Verify Grain conversion is wired at `grain_storage_facility` (wheat in → grain out)
2. Define `divine_favor` as a resource type in RTS Engine
3. Wire sacrifice: Grain dropped at Altar → adds to Divine Favor pool
4. Create `gorgon_resource_generator_upgraded` prefab (grain ResourceGenerator, period × 0.85)
5. Create `EntityComponentUpgrade` asset for Gorgon pointing at that prefab
6. Add `UpgradeLauncher` to Altar with Gorgon task gated on Divine Favor cost
7. Build `AltarPlacementDetector` — enables Gorgon task only when altar is on plains
8. Playtest: build altar on plains → sacrifice grain → unlock Gorgon → verify grain yield increases
9. Then add totem selection panel and remaining deities

---

## Out of Scope (This Spec)

- Tier 3 Temple system
- Victory conditions (military, religious, science, culture)
- Pantheon diplomacy / faction confederation
- Additional pantheons beyond Greek
- Lumber Yard building
- Additional deities beyond Gorgon (add after vertical slice validates)
- Additional totems beyond Helios (add after vertical slice validates)
