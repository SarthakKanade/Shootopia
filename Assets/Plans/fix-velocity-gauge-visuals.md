# Project Overview
- Game Title: Sharp Shooter (Velocity Wars)
- High-Level Concept: Fast-paced shooter where movement and kills fuel a "Velocity Gauge" for abilities.
- Players: Single player.
- Render Pipeline: URP.
- Target Platform: PC.

# Game Mechanics
## Core Gameplay Loop
The player builds momentum by killing enemies, which fills the Velocity Gauge. Once full, the player can activate a special ability. The gauge should reset or deplete based on usage and penalties.

# UI
The Velocity Gauge is represented by a horizontal fill bar in the HUD. It is updated via a `FloatGameEvent` raised by the `VelocityGaugeManager`.

# Key Asset & Context
- `Assets/Scripts/Player/VelocityGaugeManager.cs`: Manages the gauge state.
- `Assets/Scripts/Player/VelocityGaugeUI.cs`: Updates the visual Fill image.
- `Assets/Scripts/Core/Events/FloatGameEventListener.cs`: Listens for gauge changes.
- `Overlay Canvas/Velocity Gauge Container/Fill`: The actual Image component being modified.

# Implementation Steps

## 1. Stabilize Event Serialization
**File**: `Assets/Scripts/Core/Events/FloatGameEventListener.cs`
**Change**: Define a concrete `[System.Serializable]` class for the `UnityEvent<float>`.
**Reason**: Unity's Inspector sometimes fails to correctly bind dynamic parameters to generic `UnityEvent<T>` fields, defaulting to "Static" values (often 0 or 100) which would explain the gauge being "always full" or "always empty" despite code logic.

## 2. Remove Redundant UI Calls
**File**: `Assets/Scripts/Player/VelocityGaugeManager.cs`
**Change**: Remove direct calls to `_ui.OnGaugeChanged()` and rely solely on the `FloatGameEvent`.
**Reason**: Currently, the manager calls both the event and the UI directly. This creates double-triggering and makes debugging harder. Standardizing on the Event system is cleaner.

## 3. Enhance UI Debugging
**File**: `Assets/Scripts/Player/VelocityGaugeUI.cs`
**Change**: Add a check to ensure `percent` is valid and log more context if the fill amount doesn't change as expected.
**Reason**: To verify if the value reaching the UI is actually what we expect.

## 4. Initialization Fix
**File**: `Assets/Scripts/Player/VelocityGaugeManager.cs`
**Change**: Ensure the initial event is raised in `Start()` so the UI doesn't sit at its prefab default (which might be 1.0/Full).

# Verification & Testing
1. **Inspector Verification**: 
   - Select the `Velocity Gauge Container` in the Hierarchy.
   - Check the `Float Game Event Listener` component.
   - Ensure the `Response` is set to `VelocityGaugeUI.OnGaugeChanged` under the **Dynamic float** section (NOT the static one).
2. **Runtime Test**:
   - Start the game. The gauge should be empty (or at 0%).
   - Kill one enemy with a Pistol. Verify it jumps to ~20%.
   - Kill more enemies and verify it fills incrementally.
3. **Console Check**:
   - Watch for logs: `[VelocityGaugeUI] Gauge updated: X%`. Verify X is not always 100.
