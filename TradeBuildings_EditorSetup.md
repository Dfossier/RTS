# Neutral Trade Buildings — Editor Setup Guide

Code is done. This guide covers the **Unity Editor** steps to make the feature live.

New scripts (all in `Assets/Scripts/`):
- `ResourceTraderComponent.cs` — the per-building trade logic (offers: pay cost → get reward).
- `TradeBuildingSpawner.cs` — spawns the 4 neutral buildings at the map edge midpoints.
- `TradeBuildingPanelUIHandler.cs` — the custom selection panel + "unit nearby" gate.

New resources: `amber_resource_type.asset`, `lapisLazuli_resource_type.asset` (in `Assets/DerekAssets/Resources/Types/`). Tin reuses the existing `tinOre_resource_type.asset`.

---

## 1. Resource icons
1. In `Assets/DerekAssets/Resources/Types/`, select **amber_resource_type** and **lapisLazuli_resource_type**.
2. For each, drag a sprite into the **Icon** field (any placeholder sprite works for now).
3. Confirm **tinOre_resource_type** already has an icon (it does).

> The `key`/`displayName` are already set (Amber, Lapis Lazuli). `startingAmount` is 0.

## 2. Register the resources for the map (REQUIRED — without this they don't exist in-game)
1. Open the active engine config prefab: `Assets/DerekAssets/Resources/Working Files/RTSEngine.prefab`.
2. Find the **ResourceManager** component → **Map Resource Types** list.
3. Increase the size and add: **amber**, **lapisLazuli**, and **tinOre** (if tinOre isn't already there).
4. Save the prefab.

## 3. Show them in the HUD resource bar
1. Find the **ResourcePanelUIHandler** component (on the BasicUI prefab / canvas in your game scene or its prefab).
2. In its **Data** list, add one entry per new resource (amber, lapisLazuli, tinOre), set **type**, and set the **allowedUIFactionTypes** the same way the existing resources (food/wood) are set.

## 4. Build the trade-building prefab
1. Duplicate an existing **in-game building prefab** (one used in the Derek config — e.g. a small house). Rename it `TradeBuilding`.
2. Strip parts you don't want (worker manager, production, border/territory) — keep: the **Building** component, **health**, the **selection collider + renderer**, and the **NavMeshObstacle**.
3. Add the **ResourceTraderComponent** to it (Add Component → search "Resource Trader").
   - Set its **Code** field to something unique like `trade_converter` (it must not stay `CHANGE_ME`).
   - Leave **Offers** empty here — the spawner injects them per direction (Step 6). (You *can* fill them for testing a single prefab.)
4. Give it a distinct model/material so trade posts are recognizable.
5. Make sure the prefab's root is **selectable** (has the selection setup a normal building has).

## 5. Build the trade panel UI
> Where the HUD lives (GameScene.unity): `GameManager → Modules → BasicUI`. Under **BasicUI**
> are the canvases `Canvas.Resources` (top bar), `Canvas.SelectionPanel` (selected-object panel),
> `Canvas.TasksPanel` (action buttons), etc. The **BasicUI** object holds the existing UI handler
> scripts (SingleSelectionPanelUIHandler, ResourcePanelUIHandler, TaskPanelUIHandler).
> Tip: type `BasicUI` in the Hierarchy search bar to find it fast.

1. In the Hierarchy, expand `GameManager → Modules → BasicUI`. **Duplicate `Canvas.SelectionPanel`**
   and rename the copy **`Canvas.TradePanel`** — this gives you a canvas already set up like the rest
   of the HUD. Delete the inner contents you don't need; keep an empty content area.
2. Inside `Canvas.TradePanel`, add an empty child **`Buttons`** (give it a Vertical Layout Group) as
   the button container. Optionally add a TMP **title** text and a TMP **hint** text
   ("Move a unit closer to trade").
3. Create a **button prefab** for one offer: a `Button` with a child **TextMeshPro - Text (UI)** label.
   Save it as `TradeOfferButton`.
4. Select the **BasicUI** object and **Add Component → TradeBuildingPanelUIHandler** (putting it on
   BasicUI guarantees the engine auto-initializes it, since BasicUI is already under GameManager and
   discovered via `GetComponentsInChildren<IPreRunGameService>()`).
5. Wire the handler's fields:
   - **Panel** → `Canvas.TradePanel` (the object to show/hide)
   - **Buttons Parent** → the `Buttons` child
   - **Offer Button Prefab** → `TradeOfferButton`
   - **Title Text** / **Hint Text** → optional TMP texts
   - **Trade Radius** → how close a unit must be (default 15)

## 6. Add the spawner + set the trade rates
1. In the game scene, add an empty GameObject `TradeBuildingSpawner` and add the **TradeBuildingSpawner** component (start it **disabled** — `DerekTerrainManager` enables it).
2. Assign:
   - **Trade Building Prefab** → the `TradeBuilding` prefab from Step 4.
   - **Map Center** → leave empty (it uses `middleOfTheMap`) or assign a transform.
   - **Edge Inset / Nav Sample Radius / Manual Map Size Fallback** → defaults are fine; tune if buildings spawn in the sea.
3. Fill the four **offer lists** — this is where the real **trade rates** live (pay food → get resource). Example for **North**:
   | label | cost | reward |
   |---|---|---|
   | `Buy 10 Amber (50 Food)` | Food ×50 | amber ×10 |
   | `Buy 10 Wood (20 Food)` | Food ×20 | wood ×10 |
   | `Buy 10 Food (20 Wood)` | wood ×20 | food ×10 |
   - **East** → same but specialty = lapisLazuli. **West** → tinOre. **South** → just food/wood.
   - Each offer's `cost`/`reward` is a list of resource entries: set **type** (the ResourceTypeInfo asset) and **value → amount**.
4. On the **DerekTerrainManager** component (in the scene), assign the new **Trade Building Spawner** field to this object.

## 7. Play-test (verification)
1. Press Play. Confirm Amber / Lapis Lazuli / Tin appear in the HUD (amount 0).
2. After terrain + NavMesh finish, 4 trade buildings appear at the N/E/W/S edge midpoints on valid ground.
3. Select a trade building:
   - With a friendly unit within Trade Radius → buy buttons are **enabled**.
   - With no unit nearby → buttons **greyed out** + hint shown.
4. Click an affordable offer → food is deducted, the bought resource is added instantly. Unaffordable offers stay disabled.

## Tuning notes
- All trade rates are in the spawner's offer lists — change them anytime, no recompile.
- If buildings spawn in water/off-mesh, raise **Edge Inset** or **Nav Sample Radius**, or set **Manual Map Size Fallback**.
- "Instant convert" is synchronous; "require unit nearby" is the **Trade Radius** check.
