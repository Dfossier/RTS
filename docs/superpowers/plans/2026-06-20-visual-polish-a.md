# Visual Polish A Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Improve the overall visual quality of the RTS game by adding a warm lighting rig, post-processing effects, and per-asset material cleanup — all within the Built-in render pipeline.

**Architecture:** Three sequential layers — (1) environment render settings edited directly in YAML, (2) lighting rig and post-processing configured in the Unity Editor, (3) material file edits applied via a helper script and direct YAML changes.

**Tech Stack:** Unity Built-in Render Pipeline, Post Processing Stack v2 (Unity package), C# Editor script for batch material tinting, YAML file editing for `GameScene.unity` and `.mat` files.

---

## Branch

All work is on `visual-polish-a`. To undo everything: `git checkout development-unstable`.

---

## Task 1: Fog & Ambient — Edit GameScene.unity

**Files:**
- Modify: `Assets/GameScene.unity` lines 17–27

These are RenderSettings properties stored in the scene YAML. Edit them directly without opening Unity.

- [ ] **Step 1: Open `Assets/GameScene.unity` and find the RenderSettings block (lines 13–42)**

The block begins with `--- !u!104 &2` and `RenderSettings:`.

- [ ] **Step 2: Replace the fog and ambient values**

Change lines 17–27 from:
```yaml
  m_Fog: 0
  m_FogColor: {r: 0.5, g: 0.5, b: 0.5, a: 1}
  m_FogMode: 3
  m_FogDensity: 0.01
  m_LinearFogStart: 0
  m_LinearFogEnd: 300
  m_AmbientSkyColor: {r: 0.212, g: 0.227, b: 0.259, a: 1}
  m_AmbientEquatorColor: {r: 0.114, g: 0.125, b: 0.133, a: 1}
  m_AmbientGroundColor: {r: 0.047, g: 0.043, b: 0.035, a: 1}
  m_AmbientIntensity: 1
  m_AmbientMode: 0
```

To:
```yaml
  m_Fog: 1
  m_FogColor: {r: 0.690, g: 0.784, b: 0.847, a: 1}
  m_FogMode: 3
  m_FogDensity: 0.01
  m_LinearFogStart: 80
  m_LinearFogEnd: 200
  m_AmbientSkyColor: {r: 0.690, g: 0.784, b: 0.847, a: 1}
  m_AmbientEquatorColor: {r: 0.847, g: 0.784, b: 0.627, a: 1}
  m_AmbientGroundColor: {r: 0.227, g: 0.165, b: 0.094, a: 1}
  m_AmbientIntensity: 1
  m_AmbientMode: 1
```

`m_AmbientMode: 1` = Gradient (from Skybox). `m_Fog: 1` enables fog. `m_FogMode: 3` = Linear (already set).

- [ ] **Step 3: Also assign the skybox material in the same block**

Change line 29 from:
```yaml
  m_SkyboxMaterial: {fileID: 0}
```
To:
```yaml
  m_SkyboxMaterial: {fileID: 2100000, guid: 1700716a8dd8948cf8c66ea34a2786d5, type: 2}
```

This sets `FS000_Day_04.mat` from `Assets/DerekAssets/Fantasy Skybox FREE/Materials/Classic/`. If you prefer a different Fantasy Skybox variant after reviewing them in the Editor, replace the guid with the one from the desired `.mat.meta` file.

- [ ] **Step 4: Open Unity, let it reimport the scene, then verify in the Scene view**

You should see: soft blue-grey sky, fog fading terrain at distance, ambient light warmer than before.

- [ ] **Step 5: Commit**

```bash
git add Assets/GameScene.unity
git commit -m "feat: enable linear fog and gradient ambient for warm late-afternoon atmosphere"
```

---

## Task 2: Sun Light — Unity Editor

**Files:**
- Modify: `Directional Light` GameObject in GameScene hierarchy

- [ ] **Step 1: Open `GameScene.unity` in Unity**

In the Hierarchy, find `Directional Light` (usually near the top of the hierarchy).

- [ ] **Step 2: In the Inspector, set the Transform Rotation**

```
X: 45
Y: 135
Z: 0
```

This casts diagonal shadows from the upper-right — legible from the RTS camera angle.

- [ ] **Step 3: Set the Light component properties**

```
Color: #FFE0A0   (warm amber — use the color picker, Hex field)
Intensity: 1.2
Shadow Type: Soft Shadows
Shadow Resolution: High
Shadow Distance: 150      (Quality Settings → Shadows Distance, or Light component if using custom)
Shadow Cascades: 4        (Edit → Project Settings → Quality → Shadow Cascades)
```

Shadow Distance and Shadow Cascades are in **Edit → Project Settings → Quality**. Set them there for the highest quality tier.

- [ ] **Step 4: Enter Play mode (Ctrl+P) and review from the RTS camera**

Buildings should cast long diagonal shadows. The scene should feel warmer than before. Exit Play mode.

- [ ] **Step 5: Save the scene (Ctrl+S) and commit**

```bash
git add Assets/GameScene.unity
git commit -m "feat: set warm amber sun angle for late-afternoon RTS shadows"
```

---

## Task 3: Fill Light — Unity Editor

**Files:**
- Modify: `Assets/GameScene.unity` (new Directional Light added via Editor)

- [ ] **Step 1: In the Hierarchy, right-click → Light → Directional Light**

Name it `Fill Light`.

- [ ] **Step 2: Set Transform Rotation**

```
X: -45
Y: -45
Z: 0
```

This points from the lower-left, opposite the sun.

- [ ] **Step 3: Set Light component properties**

```
Color: #A0B8D0   (cool blue)
Intensity: 0.25
Shadow Type: No Shadows
```

- [ ] **Step 4: Verify in Scene view**

The shadow side of buildings should now have a faint cool-blue fill instead of going black. Shadows should still read clearly — if fill feels too strong, lower Intensity to 0.15.

- [ ] **Step 5: Save and commit**

```bash
git add Assets/GameScene.unity
git commit -m "feat: add cool-blue fill light to prevent pure-black shadows"
```

---

## Task 4: Install Post Processing Stack v2

**Files:**
- Modify: `Packages/manifest.json` (Package Manager handles this)

- [ ] **Step 1: In Unity, open Window → Package Manager**

- [ ] **Step 2: In the top-left dropdown, select "Unity Registry"**

- [ ] **Step 3: Search for "Post Processing"**

Find **Post Processing** by Unity Technologies (not any third-party package). Click **Install**.

Wait for import to complete.

- [ ] **Step 4: Verify the package is installed**

In Project view, you should see `Packages/Post Processing` appear. The menu **Component → Rendering → Post-Process Volume** should now exist.

- [ ] **Step 5: Commit**

```bash
git add Packages/manifest.json Packages/packages-lock.json
git commit -m "feat: install Post Processing Stack v2"
```

---

## Task 5: Configure Post-Processing

**Files:**
- Modify: Main Camera GameObject in GameScene
- Create: `Assets/DerekAssets/PostProcessing/GameProfile.asset`

- [ ] **Step 1: Create the profile asset folder**

In Project view, right-click `Assets/DerekAssets` → Create → Folder → name it `PostProcessing`.

- [ ] **Step 2: Create a Post-Process Profile**

Right-click the new `PostProcessing` folder → Create → Post-processing → Post-process Profile. Name it `GameProfile`.

- [ ] **Step 3: Select `GameProfile` and add effects**

Click `Add effect...` for each:

**Ambient Occlusion:**
```
Mode: SAO
Intensity: 0.6
Radius: 1.5
```

**Bloom:**
```
Intensity: 0.4
Threshold: 1.1
Diffusion: 5
Color: white
```

**Color Grading:**
```
Mode: ACES
Temperature: 8
Saturation: 15
Lift: {r:-0.02, g:-0.02, b:0.02}   (slight blue shadows — drag the lift wheel down-left)
Gain: {r:0.02, g:0.01, b:-0.01}    (slight warm highlights — drag gain wheel up-right)
```

**Vignette:**
```
Color: black
Intensity: 0.25
Smoothness: 0.5
Rounded: checked
```

- [ ] **Step 4: Add Post-Process Volume to the scene**

In the Hierarchy, right-click → Create Empty. Name it `PostProcessVolume`.

Add Component → Rendering → **Post-process Volume**.

```
Is Global: checked (tick the checkbox)
Profile: drag GameProfile.asset into the Profile slot
```

- [ ] **Step 5: Add Post-Process Layer to the Camera**

In the Hierarchy, select `Main Camera` (or your RTS camera GameObject).

Add Component → Rendering → **Post-process Layer**.

```
Layer: set to the same layer as PostProcessVolume (Default is fine — just ensure they match)
Anti-aliasing Mode: Subpixel Morphological Anti-aliasing (SMAA)
```

- [ ] **Step 6: Verify in Play mode**

Enter Play mode. The scene should have: slightly darker corners (vignette), subtle bloom on bright surfaces, warmer color tone, small contact shadows in building crevices (AO). Exit Play mode.

If bloom is too strong, reduce Intensity to 0.2. If AO is too dark, reduce to 0.4.

- [ ] **Step 7: Save and commit**

```bash
git add Assets/GameScene.unity Assets/DerekAssets/PostProcessing/
git commit -m "feat: add post-processing (AO, bloom, color grading, vignette, SMAA)"
```

---

## Task 6: Terrain Material

**Files:**
- Modify: `Assets/DerekAssets/Materials/Map Mat.mat`

The terrain uses a Standard shader. Currently `_Color` is pure white — adding a warm earth tint shifts it toward Mediterranean soil without requiring a texture.

- [ ] **Step 1: Edit `Assets/DerekAssets/Materials/Map Mat.mat`**

Find:
```yaml
    - _Color: {r: 1, g: 1, b: 1, a: 1}
```

Replace with:
```yaml
    - _Color: {r: 0.85, g: 0.78, b: 0.65, a: 1}
```

Also add a tiling multiplier on `_MainTex` to break up any texture repetition. Find:
```yaml
    - _MainTex:
        m_Texture: {fileID: 0}
        m_Scale: {x: 1, y: 1}
        m_Offset: {x: 0, y: 0}
```

Replace with:
```yaml
    - _MainTex:
        m_Texture: {fileID: 0}
        m_Scale: {x: 4, y: 4}
        m_Offset: {x: 0, y: 0}
```

(If the terrain already has a texture assigned, `x:4, y:4` increases tiling — adjust to taste in the Inspector.)

- [ ] **Step 2: Open Unity, review the terrain color in the Scene view**

Terrain should have a warm sandy-earth tone instead of pure white/grey. If it looks too orange, bring `r` down toward 0.80.

- [ ] **Step 3: Commit**

```bash
git add "Assets/DerekAssets/Materials/Map Mat.mat"
git commit -m "feat: warm earth tint on terrain material"
```

---

## Task 7: Water Material

**Files:**
- Modify: `Assets/DerekAssets/Materials/RiverMat.mat`

The river material uses a custom stylized water shader with many properties. Adjustments: shift color to teal, increase flow speed and opacity.

- [ ] **Step 1: Edit `Assets/DerekAssets/Materials/RiverMat.mat`**

**Color — change `_Color` and `_DeepColor`:**
```yaml
    - _Color: {r: 0.180, g: 0.557, b: 0.580, a: 1}
    - _DeepColor: {r: 0.102, g: 0.380, b: 0.440, a: 1}
```

**Shallow color (shoreline):**
```yaml
    - _ShallowColor: {r: 0.380, g: 0.750, b: 0.820, a: 0.85}
```

**Flow speed — increase from 0.5 to 1.0:**
```yaml
    - _FlowSpeed: 1
```

**Transparency — increase from 0.6 to 0.8 (less see-through):**
```yaml
    - _Transparency: 0.8
```

- [ ] **Step 2: Open Unity and check the river in Scene view**

Water should read as clearly a different element from terrain — teal-blue, visually distinct. If it's too bright, bring `_Brightness` down from 1.2 to 1.0.

- [ ] **Step 3: Commit**

```bash
git add "Assets/DerekAssets/Materials/RiverMat.mat"
git commit -m "feat: shift river to teal-blue, increase flow speed and opacity"
```

---

## Task 8: Boneskin Building Tints

**Files:**
- Create: `Assets/Editor/TintBoneskinMaterials.cs` (temporary editor utility — delete after use)
- Modify: `Assets/Boneskin settlement pack/Models/Materials/*.mat` (44 files)

There are 44 Boneskin materials. Rather than editing each YAML by hand, this task uses a one-shot Unity Editor menu item to apply a warm tint across all of them.

- [ ] **Step 1: Create the Editor folder if it doesn't exist**

In Project view: right-click `Assets` → Create → Folder → `Editor`.

- [ ] **Step 2: Create `Assets/Editor/TintBoneskinMaterials.cs`**

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class TintBoneskinMaterials
{
    [MenuItem("Tools/Tint Boneskin Materials")]
    static void Apply()
    {
        string folder = "Assets/Boneskin settlement pack/Models/Materials";
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            if (mat.HasProperty("_Color"))
            {
                Color c = mat.GetColor("_Color");
                // Shift toward warm: boost red slightly, reduce blue
                c.r = Mathf.Min(1f, c.r * 1.08f);
                c.g = Mathf.Min(1f, c.g * 1.02f);
                c.b = Mathf.Max(0f, c.b * 0.88f);
                mat.SetColor("_Color", c);
                EditorUtility.SetDirty(mat);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[TintBoneskin] Warmed {count} materials.");
    }
}
#endif
```

- [ ] **Step 3: In Unity, wait for script compilation (check bottom-right status bar)**

- [ ] **Step 4: Run the tint**

Menu: **Tools → Tint Boneskin Materials**

Check the Console — should log: `[TintBoneskin] Warmed 44 materials.`

- [ ] **Step 5: Review buildings in Scene view**

Buildings should lean slightly warmer. The tint is multiplicative and subtle — it won't wash out textures. If it's too strong, edit the multipliers (1.08, 1.02, 0.88) toward 1.0 and re-run.

- [ ] **Step 6: Delete the editor script (it's done its job)**

Delete `Assets/Editor/TintBoneskinMaterials.cs` (and its `.meta` file).

- [ ] **Step 7: Commit**

```bash
git add "Assets/Boneskin settlement pack/Models/Materials/"
git commit -m "feat: apply warm tint to all 44 Boneskin building materials"
```

---

## Task 9: Campfire Particles

**Files:**
- Inspect: `Assets/Campfire Pack/particle/` (verify only — no guaranteed file change)

- [ ] **Step 1: In Unity, select the CampFire prefab in `Assets/Resources/Prefabs/CampFire.prefab`**

Open it in Prefab mode (double-click).

- [ ] **Step 2: Find the particle system child GameObjects**

Expand the CampFire hierarchy. There should be fire and/or smoke particle systems.

- [ ] **Step 3: Check each particle system's renderer material**

Select a particle system → Renderer module → Material. If the Material slot shows a pink/magenta material, the shader is broken — assign a replacement from `Assets/Campfire Pack/particle/`.

- [ ] **Step 4: If particles look correct, increase emission for RTS camera visibility**

For the fire particle system:
```
Emission → Rate over Time: increase by 50% (e.g. 10 → 15)
Main → Start Size: increase by ~20% (e.g. 1 → 1.2)
```

This makes the campfire a recognisable visual anchor from the RTS camera height.

- [ ] **Step 5: Exit Prefab mode, save, and enter Play mode to verify**

The campfire should be the brightest, most visually distinct point in the scene — the player's base landmark.

- [ ] **Step 6: Commit**

```bash
git add Assets/Resources/Prefabs/CampFire.prefab
git commit -m "feat: boost campfire particle emission for RTS camera readability"
```

---

## Final Verification

- [ ] **Open `GameScene.unity`, enter Play mode**
- [ ] **Pan the RTS camera across the full map and check:**
  - [ ] Warm late-afternoon sun angle with visible diagonal shadows
  - [ ] No pure-black shadow sides on buildings (fill light working)
  - [ ] Terrain fades into fog at distance (≈200 units)
  - [ ] River is clearly teal-blue and visually distinct from terrain
  - [ ] Buildings have slight warmth matching the sun color
  - [ ] Campfire is the brightest visual landmark
  - [ ] Bloom subtle but present on bright surfaces
  - [ ] Vignette frames the screen without being obvious
- [ ] **Exit Play mode**
- [ ] **Final commit**

```bash
git add -A
git commit -m "feat: visual polish A complete — lighting rig, post-processing, material cleanup"
```

---

## Rollback

If anything looks wrong at any point:

```bash
git checkout development-unstable
```

To go back to a specific task's checkpoint, use `git log --oneline` to find the commit and `git checkout <hash>`.
