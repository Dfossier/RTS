# WorkforceUnitCarrier Documentation

## Overview
The `WorkforceUnitCarrier` is an extension of the RTS Engine's `UnitCarrier` component that adds workforce-based resource generation functionality. Units carried by this component act as workers that boost resource generation speed and enable/disable resource production.

## Features
- **Workforce-based Generation**: Resource generation only occurs when units are carried
- **Speed Scaling**: More units = faster resource generation
- **Configurable Multipliers**: Adjust how much each unit contributes to generation speed
- **Automatic Management**: Handles enabling/disabling the ResourceGenerator automatically
- **Runtime Monitoring**: Debug information and runtime controls in the inspector

## Setup

### 1. Component Requirements
Your entity needs these components:
- `WorkforceUnitCarrier` (replaces standard `UnitCarrier`)
- `ResourceGenerator` (either on same entity or assign manually)

### 2. Basic Setup
1. Remove any existing `UnitCarrier` component
2. Add `WorkforceUnitCarrier` component
3. Configure standard UnitCarrier settings (capacity, positions, etc.)
4. Assign a `ResourceGenerator` or use "Auto-Find" button
5. Set the `Force Multiplier` (1.0 = 100% speed boost per unit)

### 3. Configuration Options

#### Target Resource Generator
- **Auto-Detection**: Component automatically finds ResourceGenerator on same entity
- **Manual Assignment**: Drag and drop any ResourceGenerator component
- **Runtime Changes**: Can be changed during gameplay via code

#### Force Multiplier
- **Range**: 0.1 - 5.0 (recommended: 0.5 - 2.0)
- **Effect**: Each carried unit multiplies generation speed by this amount
- **Example**: 1.0 multiplier with 3 units = 3x generation speed

## How It Works

### Resource Generation Formula
```
New Period = Original Period / (Force Multiplier × Number of Units)
```

### Example Calculations
With original period of 2.0 seconds and force multiplier of 1.0:
- 0 units: Generator disabled
- 1 unit: 2.0s / (1.0 × 1) = 2.0s (normal speed)
- 2 units: 2.0s / (1.0 × 2) = 1.0s (2x faster)
- 3 units: 2.0s / (1.0 × 3) = 0.67s (3x faster)

### Automatic Behavior
- **Empty Carrier**: ResourceGenerator is disabled (IsActive = false)
- **Units Added**: ResourceGenerator becomes active, period is recalculated
- **Units Removed**: Period is recalculated, disabled if empty
- **Component Disabled**: Original ResourceGenerator state is restored

## Usage Examples

### Mining Operation
```csharp
// Create a mining facility that requires workers
// - Base generation: 10 stone every 5 seconds
// - With 1 worker: 10 stone every 5 seconds (1x speed)
// - With 2 workers: 10 stone every 2.5 seconds (2x speed)
// - With 3 workers: 10 stone every 1.67 seconds (3x speed)
```

### Power Plant
```csharp
// Power generator that needs technicians
// - Force multiplier: 0.5 (each worker provides 50% boost)
// - 2 workers needed for normal speed
// - 4 workers for 2x speed
```

### Research Lab
```csharp
// Research facility requiring scientists
// - High force multiplier: 1.5 (each scientist provides 150% boost)
// - Faster scaling for specialized units
```

## Runtime API

### Properties
```csharp
int WorkforceCount                // Current number of carried units
float CurrentSpeedMultiplier      // Current total speed multiplier
float CurrentPeriod              // Current effective generation period
```

### Methods
```csharp
SetForceMultiplier(float newMultiplier)          // Change multiplier at runtime
SetTargetResourceGenerator(ResourceGenerator target)  // Change target generator
```

### Events
The component inherits all UnitCarrier events:
```csharp
UnitAdded    // Fired when workforce is added
UnitRemoved  // Fired when workforce is removed
UnitCalled   // Fired when units are called to carrier
```

## Best Practices

### Design Considerations
1. **Balance**: Higher multipliers make individual units very powerful
2. **Capacity**: Consider max workforce size vs. generation scaling
3. **Unit Types**: Use `customUnitSlots` for different unit values
4. **Terrain**: Ensure workers can reach the facility

### Performance
- Component uses reflection minimally (only during workforce changes)
- Original ResourceGenerator values are cached and restored
- Automatic period clamping prevents extremely fast generation

### Game Design
- **Worker Units**: Create specialized worker unit types
- **Upgrades**: Use force multiplier changes for technology upgrades
- **Logistics**: Combine with transport systems for worker delivery
- **Risk/Reward**: Workers in carriers can't defend but boost production

## Troubleshooting

### Common Issues
1. **No Effect**: Check if ResourceGenerator is assigned and has resources configured
2. **Wrong Speed**: Verify force multiplier and unit count calculations
3. **Not Activating**: Ensure units are properly added to carrier (check capacity)
4. **Console Errors**: Enable debug logging to see detailed calculations

### Debug Features
- **Enable Debug Logging**: See detailed workforce calculations in console
- **Runtime Inspector**: View current workforce stats during play
- **Force Update Button**: Manually trigger resource generator update
- **Log Current State**: Output current configuration to console

## Integration Notes

### Compatibility
- Works with all ResourceGenerator configurations
- Compatible with NPC factions
- Supports multiplayer (uses standard RTS Engine networking)
- Works with building upgrades and faction resource systems

### Customization
The component can be extended further by:
- Overriding `UpdateResourceGenerator()` for custom formulas
- Adding unit type-specific multipliers
- Implementing time-based workforce effects
- Adding visual feedback for workforce status

## Example Use Cases

1. **Stone Quarry**: Requires miners to operate, more miners = faster extraction
2. **Research Facility**: Needs scientists, each adds research speed
3. **Factory**: Workers increase production rate of manufactured goods
4. **Farm**: Farmers are needed to tend crops and boost food production
5. **Power Plant**: Technicians required for operation and efficiency