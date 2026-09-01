# Project Overview
Sharp Shooter (Velocity Wars) is a fast-paced FPS where the UI for Health and Velocity Gauge is currently unresponsive. Systems like damage and death are functioning (the player eventually dies), but the HUD does not reflect these changes. The project uses a ScriptableObject-based event system to decouple gameplay from UI.

# Game Mechanics
- **Health System**: Player has 100 HP and 3 lives. UI should show a segmented health bar and life icons.
- **Velocity Gauge**: Increases on kills, rewarding momentum. UI should show a fill bar/slider and an ability ready state.

# UI
- **HUD Manager**: Contains `UIManager` and multiple `FloatGameEventListener` components for HP, Enemy Count, and Lives.
- **Velocity Gauge Container**: Contains `VelocityGaugeUI` and `FloatGameEventListener` for the gauge state.

# Key Asset & Context
- `Assets/Scripts/Core/Events/FloatGameEventListener.cs`: Listens for FloatGameEvents and invokes a UnityEvent.
- `Assets/Scripts/Core/Managers/UIManager.cs`: Manages HUD elements like HP bars and life icons.
- `Assets/Scripts/Player/VelocityGaugeUI.cs`: Manages the momentum gauge display.
- `Assets/Scenes/MainLevel.unity`: The scene where the bindings are configured.

# Implementation Steps

## 1. Fix UnityEvent Bindings in Scene
The investigation revealed that many UI event listeners in the scene are configured with `m_Mode: 1` (Static call) instead of `m_Mode: 0` (Dynamic call). This causes the methods (like `OnPlayerHealthChanged`) to be called with a fixed inspector value instead of the actual value passed by the event.
- **File**: `Assets/Scenes/MainLevel.unity`
- **Action**: Locate the `FloatGameEventListener` components on "HUD Manager" and "Velocity Gauge Container". 
- **Change**: Re-bind `_response` to the **Dynamic float** version of the target methods (e.g., `UIManager.OnPlayerHealthChanged`).
- **Dependencies**: None.

## 2. Enhance UI Logging and Robustness
Add detailed logs to the UI scripts to confirm exactly what values are being received and handle potential null refs.
- **File**: `Assets/Scripts/Core/Managers/UIManager.cs`
- **Action**: Add `Debug.Log` at the very start of `OnPlayerHealthChanged` and `OnLifeLost`.
- **File**: `Assets/Scripts/Player/VelocityGaugeUI.cs`
- **Action**: Ensure `OnGaugeChanged` logs the raw input percentage.

## 3. Standardize Call States
Ensure all UI listeners are set to `RuntimeOnly` (2) to guarantee they fire during Play Mode.
- **File**: `Assets/Scenes/MainLevel.unity`
- **Action**: Verify and set `m_CallState` to `2` for all critical HUD listeners.

# Verification & Testing
- **Manual Check**: Play the game and verify:
    - Health bars deactivate correctly when taking damage.
    - Life icons disappear correctly upon player death.
    - Velocity gauge fills visually when killing enemies.
- **Log Verification**: Check the console for:
    - `[UIManager] HP: <value>`
    - `[UIManager] OnLifeLost called with: <value>`
    - `[VelocityGaugeUI] Gauge updated: <value>%`
