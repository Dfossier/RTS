# Visual Polish A — Design Spec
**Date:** 2026-06-20  
**Branch:** `visual-polish-a`  
**Escape hatch:** `git checkout development-unstable` reverts everything instantly.

## Goal

Improve the overall visual quality of the game without migrating the render pipeline (Built-in) or overhauling art assets. Changes should be transferable if a URP migration (Option B) happens later.

## What Transfers to URP Later

- Color palette decisions (material colors/textures survive pipeline migration)
- Lighting artistic direction (angle, warmth, fill ratio — re-tuned but not reinvented)
- Per-asset cleanup choices (always valid regardless of pipeline)

**What does NOT transfer:** Post Processing Stack v2 settings. URP uses its own Volume system. Values must be manually recreated, but knowing the target makes that fast.

## Constraints

- Built-in render pipeline only — no custom HLSL shaders (double work if migrating to URP)
- All changes in `GameScene.unity`, `.mat` files, and `.asset` files — no C# code changes
- Scope is strictly the three layers below; no unrelated art changes

---

## Layer 1 — Lighting Rig

**Location:** Unity Editor → Select Directional Light in scene hierarchy + Window → Rendering → Lighting

### Sun (existing Directional Light)
| Property | Value |
|---|---|
| Rotation | X: 45°, Y: 135°, Z: 0° |
| Color | `#FFE0A0` (warm amber) |
| Intensity | 1.2 |
| Shadow Type | Soft Shadows |
| Shadow Resolution | High |
| Shadow Distance | 150 |
| Shadow Cascades | 4 |

Late-afternoon angle (45° pitch, 135° yaw) casts diagonal shadows across buildings and terrain that read clearly from the RTS camera angle.

### Ambient Light
**Window → Rendering → Lighting → Environment → Source: Gradient**

| Property | Value |
|---|---|
| Sky Color | `#B0C8D8` (cool blue-grey) |
| Equator Color | `#D8C8A0` (pale warm) |
| Ground Color | `#3A2A18` (dark earth) |

Fakes indirect bounce light without baking. Prevents shadow sides from going black.

### Fill Light (new Directional Light, no shadows)
| Property | Value |
|---|---|
| Rotation | Opposite to sun (X: -45°, Y: -45°, Z: 0°) |
| Color | `#A0B8D0` (cool blue) |
| Intensity | 0.25 |
| Cast Shadows | Off |

### Fog
**Window → Rendering → Lighting → Other Settings**

| Property | Value |
|---|---|
| Mode | Linear |
| Start | 80 |
| End | 200 |
| Color | `#B0C8D8` (matches sky ambient) |

---

## Layer 2 — Post-Processing Stack

**Setup:**
1. Install `Post Processing` via Window → Package Manager → search "Post Processing" (Unity Technologies)
2. Add a `Post-Process Volume` GameObject to the scene, check **Is Global**, assign a new **Post-Process Profile**
3. Add the camera's `Post-Process Layer` component, set **Layer** to match the volume's layer

### Effects

**Ambient Occlusion**
| Property | Value |
|---|---|
| Intensity | 0.6 |
| Radius | 1.5 |
| Mode | SAO |

Darkens crevices between buildings and terrain, adds grounding.

**Bloom**
| Property | Value |
|---|---|
| Intensity | 0.4 |
| Threshold | 1.1 |
| Diffusion | 5 |
| Color | White |

Subtle glow on bright surfaces and campfire particles. Keep intensity low — obvious bloom dates quickly.

**Color Grading**
| Property | Value |
|---|---|
| Mode | ACES Tonemapping |
| Temperature | +8 (warmer) |
| Saturation | +15 |
| Lift (shadows) | Slight blue shift |
| Gain (highlights) | Slight warm shift |

ACES tonemapping handles bright/dark contrast better than the default Linear mode and gives a more cinematic feel without extra work.

**Vignette**
| Property | Value |
|---|---|
| Color | Black |
| Intensity | 0.25 |
| Smoothness | 0.5 |
| Rounded | On |

Subtle. Frames the screen and focuses the player's eye toward the center of the map.

**Anti-Aliasing**
- Set on the Camera component directly: **SMAA** (better than FXAA for geometry edges, no TAA ghosting on moving units)

---

## Layer 3 — Per-Asset Cleanup

Work through these in order — each one is independent and can be skipped if time is short.

### 1. Skybox
- Asset already in project: `Assets/DerekAssets/Fantasy Skybox FREE`
- Open Window → Rendering → Lighting → Environment → Skybox Material
- Try each Fantasy Skybox option; pick the warmest/most dramatic that matches the Mediterranean late-afternoon tone
- Coordinates with the `#FFE0A0` sun color

### 2. Terrain Texture
- Asset: `Assets/DerekAssets/Materials/Map Mat.mat` and `Mesh Mat.mat`
- Increase texture tiling (try 2x current value) to reduce visible repetition at ground level
- If a normal map slot is available, assign a subtle rock/dirt normal map to add micro-surface detail

### 3. Water
- Assets: `Assets/DerekAssets/Materials/RiverMat.mat` and `Water.mat`
- Increase opacity (reduce transparency if it's currently too glass-like)
- Boost flow/scroll speed on UV animation if present
- Shift color toward teal-blue `#4A8EA0` to differentiate from terrain

### 4. Building Materials (Boneskin Pack)
- Assets: `Assets/Boneskin settlement pack/`
- Each building material currently has its own neutral grey baseline
- Add a warm tint (`#D4B070`, low intensity) to align with the sun color across all Boneskin materials
- Do this in bulk by selecting all Boneskin `.mat` files and adjusting color in the Inspector multi-edit

### 5. Campfire Particles
- Asset: `Assets/Campfire Pack/particle/`
- Verify particle materials are rendering correctly (not pink — check shader compatibility)
- Increase `Max Particles` and `Start Size` slightly for better visibility from RTS camera height
- Campfire should be the brightest point in the scene at night/dusk — good landmark for the player's base

---

## Out of Scope

- No custom shaders (would need rewriting for URP)
- No terrain mesh changes
- No new art assets
- No UI changes
- No C# code

---

## Success Criteria

- Scene reads as a warm late-afternoon Mediterranean/ancient world setting
- Buildings cast visible shadows and have readable depth from the RTS camera
- Water is visually distinct from terrain
- No obvious asset-pack clashing (all materials pull toward the same warm amber palette)
- Campfire is a recognizable visual anchor for the player's base
