# Terrain Biome System Documentation

## Heat & Moisture Generation System

### Overview
The terrain uses heat and moisture values to determine biomes. **IMPORTANT: "heat" values are COUNTER-INTUITIVE** - they represent latitude/altitude, NOT temperature.

---

## Heat Value Generation

### Step 1: Uniform Gradient (Latitude-based)
`GenerateUniformNoiseMap()` creates a latitudinal gradient:
```
Equator (center):  uheatvalues ≈ 0.0  (minimum)
Poles (edges):     uheatvalues ≈ 1.0  (maximum)
```

**Formula:** `noise = Mathf.Abs(sampleY - (mapHeight/2)) / mapHeight`

### Step 2: Random Noise
`GenerateNoiseMap()` creates random Perlin noise (0-1 range)

### Step 3: Multiplication
```csharp
heatvalues[i, j] = uheatvalues[i, j] * rheatvalues[i, j];
```

Result: Equator gets LOW values, poles get HIGH values

### Step 4: Altitude Adjustment
```csharp
if (heightvalues[i,j] > 0.8)
    heatvalues[i, j] += 0.01f * heightvalues[i, j];  // Mountains
else
    heatvalues[i, j] += 0.0025f * heightvalues[i, j]; // Hills
```

**CRITICAL:** Comments say "makes mountains even COLDER" by **ADDING** to heatvalues

---

## Heat Value Interpretation

### ⚠️ COUNTER-INTUITIVE NAMING!

**LOW heat values = WARM climate (equator, lowlands)**
**HIGH heat values = COLD climate (poles, mountains)**

| Heat Value | Climate | Location |
|------------|---------|----------|
| 0.0 - 0.3  | **HOT** | Equator, lowlands |
| 0.3 - 0.6  | Temperate | Mid-latitudes |
| 0.6 - 1.0  | **COLD** | Poles, high mountains |

---

## Moisture Value Generation

### Simple Perlin Noise
`MoistureMapGenerator.GenerateMoistureMap()` creates pure Perlin noise (0-1):

```
0.0 = DRY (deserts)
0.5 = MODERATE (grasslands)
1.0 = WET (forests, jungles)
```

**Moisture is intuitive:** High values = wet, low values = dry

---

## Shader Biome Selection Logic

Located in `Terrain.shader`, line 124-133:

```hlsl
twoHeat = 2/3;  // Threshold at 0.667
isCold = saturate(sign(IN.biomeMap.x - twoHeat));
```

### Two Biome Systems:

**System A: Warm Areas (heat < 0.667)**
- Uses **MOISTURE** to select biomes
- Equation: `moistureStrength = inverseLerp(..., moisturePercent - baseStartMoistures[i])`
- Dry → Moderate → Wet transitions

**System B: Cold Areas (heat ≥ 0.667)**
- Uses **HEAT** to select biomes
- Equation: `heatStrength = inverseLerp(..., heatPercent - baseStartHeats[i])`
- Shows tundra/snow

---

## Current Problem

With `twoHeat = 0.25` and inverted logic, we have:
- **Equator (heat ≈ 0.1)** → Shows SNOW (wrong!)
- **Poles (heat ≈ 0.8)** → Shows DESERT (wrong!)

---

## Correct Biome Configuration

### Warm Biomes (heat < 0.667) - Moisture-based:

| Layer | Biome | startHeat | startMoisture | Description |
|-------|-------|-----------|---------------|-------------|
| 3 | Desert | 0 | 0.15 | Hot & dry |
| 4 | Steppe | 0 | 0.45 | Hot & moderate |
| 5 | Forest | 0 | 0.75 | Hot & wet |

### Cold Biomes (heat ≥ 0.667) - Heat-based:

| Layer | Biome | startHeat | startMoisture | Description |
| 6 | Tundra/Snow | 0.67 | 0 | Cold poles/mountains |

---

## Shader Threshold Settings

```hlsl
twoHeat = 0.667;  // Original value - CORRECT
isCold = saturate(sign(IN.biomeMap.x - twoHeat));  // NOT inverted
```

**Logic:** When heat ≥ 0.667 (cold areas), switch to heat-based biome selection

---

## Summary

**Heat values are BACKWARDS:**
- 0.0 = Equator (HOT) ☀️
- 1.0 = Poles (COLD) ❄️

**Moisture values are NORMAL:**
- 0.0 = Dry 🏜️
- 1.0 = Wet 🌲

**Shader splits at heat = 0.667:**
- Below: Use moisture (desert/steppe/forest)
- Above: Use heat (tundra/snow)
