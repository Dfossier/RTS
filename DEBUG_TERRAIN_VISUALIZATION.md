# Terrain Debug Visualization Guide

## How to Use

The terrain shader now has built-in debug modes to visualize heat and moisture values!

### Steps to Enable Debug Mode:

1. **Find your terrain material** (usually in `Assets/DerekAssets/Terrain Assets/`)
2. **Select the material** in the Unity Inspector
3. **Look for "Debug Mode"** property
4. **Change the value** to see different visualizations

---

## Debug Modes

### Mode 0: Normal Biome Rendering (Default)
Shows your terrain with normal textures and biomes.

### Mode 1: Heat Map 🔴
**Visual:** Red gradient
- **Black/Dark Red** = Low heat values (0.0-0.3) = **EQUATOR/LOWLANDS (HOT)**
- **Bright Red** = High heat values (0.7-1.0) = **POLES/MOUNTAINS (COLD)**

**What to look for:**
- Horizontal gradient from center (equator) to edges (poles)
- Brighter red on mountains (altitude increases heat value)
- Remember: Counter-intuitive naming! Low values = hot climate

### Mode 2: Moisture Map 🟢
**Visual:** Green gradient
- **Black/Dark Green** = Low moisture (0.0) = **DRY (Desert)**
- **Bright Green** = High moisture (1.0) = **WET (Forest)**

**What to look for:**
- Random Perlin noise patterns
- No horizontal gradient (moisture is independent of latitude)

### Mode 3: Combined Map 🌈
**Visual:** RGB channels
- **Red channel** = Heat
- **Green channel** = Moisture
- **Blue channel** = Height

**Colors you'll see:**
- **Yellow** (R+G) = Hot & Wet
- **Red** = Hot & Dry
- **Cyan** (G+B) = Wet & High altitude
- **Magenta** (R+B) = Dry & High altitude

**What to look for:**
- Heat/moisture distribution across the map
- How height affects heat (mountains appear more blue/purple)

### Mode 4: Heat Zones 🔵
**Visual:** Two-tone visualization
- **Yellow areas** = Warm zones (heat < 0.667) → Uses **MOISTURE** for biomes
- **Light Blue areas** = Cold zones (heat ≥ 0.667) → Uses **HEAT** for biomes

**What to look for:**
- Should see blue at top/bottom edges (poles)
- Blue on high mountains
- Most of map should be yellow (warm zones)

### Mode 5: Moisture Zones 📊
**Visual:** Color-coded moisture bands
- **Bright Yellow** = Desert zone (moisture < 0.2)
- **Tan** = Steppe zone (moisture 0.2-0.5)
- **Light Green** = Transition (moisture 0.5-0.9)
- **Dark Green** = Forest zone (moisture > 0.9)

**What to look for:**
- Clear boundaries between biome zones
- Compare to normal rendering to see if biomes match expectations
- Desert should appear where moisture is lowest

---

## Troubleshooting with Debug Modes

### Problem: Too much green forest
**Debug Steps:**
1. Set Mode 5 (Moisture Zones)
2. Look at how much dark green you see
3. If > 20% of map is dark green, moisture threshold is too low

**Solution:** Increase forest `startMoisture` value (currently 0.92)

### Problem: Not enough desert
**Debug Steps:**
1. Set Mode 2 (Moisture Map)
2. See if there are large dark green areas
3. Set Mode 5 to see where desert boundary is

**Solution:** Lower desert `startMoisture` or increase `blendStrength`

### Problem: Biomes in wrong locations
**Debug Steps:**
1. Set Mode 4 (Heat Zones)
2. Verify cold zones appear at poles/mountains (should be blue)
3. If inverted, check shader `isCold` calculation

---

## Current Biome Thresholds

Based on debug visualization:

**Warm Zones (Yellow in Mode 4):**
- Desert: moisture 0.0 - 0.3
- Steppe: moisture 0.3 - 0.7
- Forest: moisture 0.9+

**Cold Zones (Blue in Mode 4):**
- Tundra/Snow: heat ≥ 0.667

---

## Tips

1. **Start with Mode 3** to see the raw heat/moisture distribution
2. **Use Mode 4** to verify cold/warm zone split
3. **Use Mode 5** to see if moisture thresholds match your biome settings
4. **Switch back to Mode 0** when done debugging
5. **Take screenshots** of debug modes to compare with biome layout
