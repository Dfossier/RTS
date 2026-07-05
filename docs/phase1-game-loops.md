# Phase 1 Core Game Loops

## World Generation
Each session generates a unique procedural map: terrain mesh with biome layers (plains, forest, hills, tundra), rivers flowing downhill, and scattered stone deposits. Biome determines what resources grow where and which animals appear.

---

## Food Economy — Two Tracks

Food is the central resource. Two parallel tracks evolve from scarcity to abundance. Each track progresses through pressure — the current stage depletes naturally, motivating the player to develop the next one. The tracks remain independent until they converge at the Village.

> **Scope note:** Three-stage progressions within each track (gathering → horticulture → agriculture; hunting → taming → herding) are future work. The current implementation covers the Farm building, the Herder unit, the animal capture/garrison mechanic, and the Village upgrade.

### Plant Track
Wild grain and berries are harvested directly by villagers. Nodes regrow over time but can be permanently depleted by sustained harvesting. The **Farm** building provides a more reliable and higher-yield alternative — it produces more wheat than gathering but requires villager labor to plant and harvest each cycle.

### Animal Track
Villagers hunt wild animals; carcasses spawn on death and yield meat. The **Herder** unit (`villager_dereks_hunter.prefab`) is created by upgrading a villager at the Hunting Camp. It uses the RTS Engine `Converter` component (targeting the "animal" unit category) to tame wild animals — the herder approaches, spends 4 seconds and costs 5 of a resource, and the animal is converted to the player's faction. The converted animal then follows the herder home via `AnimalsOwnerController` → `AnimalsMovementation.ownerTarget`. The herder has a carrier (maxCount: 4) allowing it to escort multiple animals at once. Once back at a seed building, animals are garrisoned in to contribute to the herd count threshold.

---

## Village — Convergence of All Three Tracks

The Village is not a building the player constructs — it is a permanent transformation of an existing settlement building when it meets upgrade conditions from all three tracks: farming, herding, and religion.

The upgrade is handled by the `EntityComponentUpgrade` system (same mechanism as the Altar deity selection). The seed building is permanently replaced in-place by the Village Center prefab, inheriting its territory radius.

### Upgrade Conditions (all three required)

| Track | Condition |
|---|---|
| Herding | 15 animals garrisoned in the building |
| Farming | 500 grain in the faction's resource pool |
| Religion | 500 Divine Favor in the faction's resource pool |

The religious requirement cannot be accelerated by throwing more villagers at it — it paces the upgrade independently of the food economy.

### Seed Buildings and Village Identity

Any of four core building types can become a Village Center. The starting type shapes the village's administrative bonus:

| Seed Building | Village Identity | Bonus |
|---|---|---|
| Granary | Agricultural village | Crop yield bonus, larger farm radius |
| Hunting Camp | Frontier village | Hunting efficiency, military bonus |
| Market Outpost | Trading village | Resource exchange rates |
| Mining Camp | Industrial village | Production/construction bonus |

### What the Village Enables

- **Territory**: The Village Center is the administrative anchor for its territory radius. Other factions cannot place overlapping centers.
- **Symbiosis**: Farms and herds reinforce each other — garrisoned animals fertilize nearby farm plots (yield bonus); harvested grain feeds the garrison automatically. Net villager overhead drops relative to food output.
- **Expansion**: Additional villages can be founded by repeating the three-track convergence elsewhere on the map. Each new village claims new territory.
- **Specialization**: Freed villager capacity can be redirected to military, trade routes, dungeon expeditions, or altar development.

---

## Trade

Four neutral trade buildings spawn at the midpoints of the map edges (North, East, West, South). Each sells a direction-specific specialty resource in exchange for food:

| Direction | Specialty |
|-----------|-----------|
| North | Amber |
| East | Lapis Lazuli |
| West | Tin |
| South | Food & Wood |

A friendly unit must be nearby to interact. Trades are instant. Specialty resources gate advanced buildings and upgrades, creating a strategic reason to expand toward the edges.

---

## Religion

Building an Altar on a placement-sensitive location (plains, hilltop, or riverbank) starts accumulating Divine Favor. Once enough favor is gathered the player chooses a deity — Ares, Zeus, Poseidon, or the Gorgon — each granting a distinct upgrade. Placement determines which deities are available, so altar positioning is a meaningful early decision.

Divine Favor is also a universal requirement for the Village upgrade (500 required) — the religion track cannot be neglected without blocking settlement advancement.

---

## Progression Arc

```
Early:   Harvest wild resources + hunt animals
           ↓ (depletion pressure)
Mid:     Build Farm (wheat) + train Herder (capture animals) + Altar (divine favor)
           ↓ (all three conditions met: 15 animals garrisoned, 500 grain, 500 divine favor)
Village: Seed building transforms → territory claimed → farm/herd symbiosis unlocked
           ↓
Late:    Found additional villages → expand territory → specialty resources → deity upgrades
```

---

## Open Design Questions

- Village identity bonus specifics (mechanism and tuning values TBD)
- What happens to garrisoned animals after village upgrade (stay, release, convert to passive producer)
- Whether specialty resources (amber, lapis, tin) gate village-tier buildings specifically
- Dungeon system as mid-game objective (DungeonManager exists in codebase)
- Victory condition (deferred)
- Six-stage track progressions: gathering → horticulture → agriculture; hunting → taming → herding (future work)
