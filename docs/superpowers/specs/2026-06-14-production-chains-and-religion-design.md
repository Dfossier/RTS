# Production Chains & Religion System — Design Spec
**Date:** 2026-06-14
**Project:** Bronze Age Collapse (RTS Engine v2.0 Beta)
**Scope:** Phase 1 (production chains) + Phase 2 Tier 1–2 (religion: totem + altar/deity)

---

## Overview

The core experience is geography-driven specialization: your biome determines what you can produce efficiently, which determines your identity, which shapes your trade relationships and ultimately your path to conquest. Production chains give resources meaningful depth, and the religion system gives factions cultural identity from the first moment of play.

**Vertical slice goal:** Get one complete thread playable end-to-end before expanding.
Wheat → Mill → Grain → Sacrifice at Altar → Gorgon deity unlocked → grain yield bonus active.

---

## Section 1: Production Chains

### Processing Buildings

Four new buildings convert raw resources into processed goods. Each requires worker assignment and runs continuously while workers are present.

| Building | Input | Output | Worker Slots |
|---|---|---|---|
| Mill | Wheat | Grain | 1–2 |
| Cookfire | Raw Meat | Cooked Meat | 1 |
| Lumber Yard | Raw Wood | Lumber | 1–2 |
| Stonecutter | Rough Stone | Finished Stone | 1–2 |

### Dual-Use Outputs

Processed resources have multiple valid uses. The player allocates them manually or via building configuration:

| Resource | Use A | Use B |
|---|---|---|
| Grain | Food (population consumption) | Seeds (replant wheat fields) |
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

No faction is locked out of any chain, but geography makes some chains significantly cheaper and faster.

---

## Section 2: Religion System (Tier 1 + Tier 2)

### Design Intent

Religion is the connective tissue between economy, identity, and diplomacy. It is historically grounded: ancient societies used religion to enable large-scale cooperation, inspire excellence, and differentiate cultures. The system reflects this — earlier tiers cost little but give modest rewards; later tiers require investment but define the faction's identity.

### Tier 1 — Totem (Game Start)

Player selects one totem before or immediately after spawning. No building required. Provides a small passive bonus for the entire game.

| Totem | English | Bonus |
|---|---|---|
| Helios | (Sun) | +15% farm produce |
| Aeolus | (Wind) | +15% unit movement speed |
| Lykaios | (Wolf) | +15% damage vs. animals |
| Potamos | (River) | +15% water/fishing resource yield |
| Lithos | (Stone) | +15% construction speed |

Totem is permanent — it cannot be changed after selection.

### Tier 2 — Altar & Chief Deity (Mid Game)

The Altar is a buildable structure. **Altar placement determines which chief deities are available.** Once built, the player selects one deity from the eligible list. The deity is permanent for that altar.

#### Deity Unlock Conditions

| Deity | Unlock Condition |
|---|---|
| Dzues (Zeus) | Altar built at high elevation / hilltop |
| Gorgon | Altar built on fertile plains |
| Pseidon (Poseidon) | Altar built at river source or coastline |
| Ares | Any altar placement |

#### Deity Bonuses

| Deity | Domain | Bonuses |
|---|---|---|
| Dzues (Zeus) | Sky / Leadership | +movement speed, +attack damage |
| Gorgon | Harvest / Fertility | +grain yield |
| Pseidon (Poseidon) | Sea / Horses | +movement speed, +herding bonus |
| Ares | War / Livestock | +herding bonus, +attack damage |

*Note: Poseidon Hippios (god of horses) makes the herding bonus historically grounded.*

#### Sacrifice

Cooked Meat or Grain can be sacrificed at the altar to accumulate **Divine Favor** — a resource used to activate deity abilities. This creates a direct tradeoff: feed your population or appease your god.

### Tier 3 — Temple (Late Game, Out of Scope)

Tier 3 design deferred. Temple infrastructure and advanced deity powers will be specced separately.

---

## Vertical Slice — Implementation Order

Build in this order to get something playable as fast as possible:

1. Define Grain as a resource type in RTS Engine (if not already done)
2. Build Mill building — takes Wheat input, produces Grain output, requires 1 worker
3. Wire Grain dual-use: food pool OR sacrifice inventory
4. Build Altar building with placement detection (biome/elevation check)
5. Wire Gorgon deity unlock when Altar placed on plains
6. Implement sacrifice: drop Grain at Altar → accumulate Divine Favor
7. Apply Gorgon bonus (grain yield %) when Divine Favor threshold reached
8. Playtest this loop before adding remaining chains and deities

---

## Out of Scope (This Spec)

- Tier 3 Temple system
- Victory conditions (military, religious, science, culture)
- Pantheon diplomacy / faction confederation
- Additional pantheons beyond Greek
- Cookfire, Lumber Yard, Stonecutter (build after Mill vertical slice validates)
- Additional deities beyond Gorgon (build after vertical slice validates)
