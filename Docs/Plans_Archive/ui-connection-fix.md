# Project Overview
- Game Title: Velocity Wars (Sharp Shooter)
- High-Level Concept: Fast-paced FPS where movement and speed fuel combat abilities.
- Players: Single player
- Target Platform: PC
- Render Pipeline: URP

# Game Mechanics
## Core Gameplay Loop
The player navigates levels at high speeds, killing enemies to fill a "Velocity Gauge." Filling the gauge allows the use of powerful abilities. The player must manage health (shield bars) and ammo/durability while clearing the stage of enemies.

## Controls and Input Methods
- WASD: Movement
- Mouse: Aim/Shoot
- E: Ability (when Gauge is full)
- UI: Real-time feedback on health, enemy count, and momentum.

# UI
- **Health Bar**: A series of "Shield Bars" (segments) that disappear as the player takes damage.
- **Enemies Left**: A text counter showing remaining enemies in the level.
- **Velocity Gauge**: A fill bar representing momentum charge (0-100%).
- **Ammo/Durability**: A counter for the current weapon's remaining uses.

# Key Asset & Context
- `Assets/Scripts/Core/Events/FloatGameEventListener.cs`: The bridge between ScriptableObject events and the UI.
- `Assets/Scripts/Core/Managers/UIManager.cs`: Manages the display of health and enemy count.
- `Assets/Scripts/Player/VelocityGaugeUI.cs`: Manages the fill bar for the ability gauge.
- `Assets/Scripts/Misc/GameManager.cs`: Tracks the global enemy count.

# Implementation Steps

1. **Fix Event Binding Errors (FloatGameEventListener)**
   - **File**: `Assets/Scripts/Core/Events/FloatGameEventListener.cs`
   - **Change**: Define a concrete `[System.Serializable] public class FloatEvent : UnityEvent<float> {}` and use it for the `_response` field.
   - **Reason**: Using `UnityEvent<float>` directly can cause `ArgumentException` during delegate binding in the Inspector if Unity's serialization gets confused. A concrete subclass is more stable.

2. **Improve UIManager Robustness**
   - **File**: `Assets/Scripts/Core/Managers/UIManager.cs`
   - **Change**: 
     - Add `Mathf.Clamp` to health and enemy count logic.
     - Add logging to `OnEnemyCountChanged` and `OnPlayerHealthChanged` to verify data flow.
     - Ensure `shieldBars` are properly updated even if health exceeds the array length.

3. **Fix Velocity Gauge Initialization**
   - **File**: `Assets/Scripts/Player/VelocityGaugeUI.cs`
   - **Change**:
     - Ensure `OnGaugeChanged` correctly handles the 0-100 to 0-1 conversion.
     - Add a check to prevent the gauge from appearing full if no data has been received.

4. **Verify GameManager Enemy Registration**
   - **File**: `Assets/Scripts/Misc/GameManager.cs`
   - **Change**: Ensure `_onEnemyCountChanged` is raised with the current count after all enemies have registered, or verify that `EnemyHealth.Start` calls are correctly captured by the UI.

5. **Inspector Re-wiring (Instructional)**
   - **Action**: After code changes, the user (or assistant) must ensure the UnityEvent entries on the "HUD Manager" and "Velocity Gauge Container" are set to **Dynamic float** (at the bottom of the method list) rather than **Static** (at the top).

# Verification & Testing
1. **Console Check**: Verify that `ArgumentException: method arguments are incompatible` no longer appears when shooting or taking damage.
2. **Health Test**: Walk into a robot; verify one shield bar segment disappears.
3. **Enemy Test**: Kill one robot; verify "Enemies Left" changes from 6 to 5.
4. **Gauge Test**: Kill multiple robots; verify the yellow velocity bar fills incrementally.
5. **Ammo Test**: Verify ammo still decrements (as it was the only part working).
