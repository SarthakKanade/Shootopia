# Project Overview
- Game Title: Sharp Shooter (Velocity Wars)
- High-Level Concept: Fast-paced FPS with health and velocity mechanics.
- Issue: Health and Velocity Gauge UI elements are unresponsive despite backend systems working.
- Diagnosis: A combination of a "stale listener" bug in the ScriptableObject event system and incorrect UnityEvent bindings in the scene.

# Game Mechanics
## Core Gameplay Loop
Players engage in combat, taking damage and killing enemies to fill a velocity gauge.
## Controls and Input Methods
Standard FPS controls.

# UI
- HUD Manager: Displays health segments, lives, and enemy counts.
- Velocity Gauge: Displays momentum and ability readiness.

# Key Asset & Context
- `FloatGameEvent.cs`: The ScriptableObject event class for float values.
- `GameEvent.cs`: The ScriptableObject event class for generic triggers.
- `FloatGameEventListener.cs`: Component that bridges SO events to UnityEvents.
- `MainLevel.unity`: The scene containing the UI HUD bindings.

# Implementation Steps

## 1. Fix ScriptableObject Event System (Code)
The current `Raise` methods in `FloatGameEvent.cs` and `GameEvent.cs` do not handle null or destroyed listeners, which accumulate in the Editor across Play Mode sessions. This causes `NullReferenceExceptions` that halt the event broadcast.

### 1.1 Patch `FloatGameEvent.cs`
- **File**: `Assets/Scripts/Core/Events/FloatGameEvent.cs`
- **Action**: Update the `Raise` method to include a null check and cleanup.
- **Code Snippet**:
  ```csharp
  public void Raise(float value)
  {
      for (int i = _listeners.Count - 1; i >= 0; i--)
      {
          if (_listeners[i] == null)
          {
              _listeners.RemoveAt(i);
              continue;
          }
          _listeners[i].OnEventRaised(value);
      }
      _onEventRaised?.Invoke(value);
  }
  ```

### 1.2 Patch `GameEvent.cs`
- **File**: `Assets/Scripts/Core/Events/GameEvent.cs`
- **Action**: Update the `Raise` method similarly.
- **Code Snippet**:
  ```csharp
  public void Raise()
  {
      for (int i = _listeners.Count - 1; i >= 0; i--)
      {
          if (_listeners[i] == null)
          {
              _listeners.RemoveAt(i);
              continue;
          }
          _listeners[i].OnEventRaised();
      }
      _onEventRaised?.Invoke();
  }
  ```

## 2. Fix UI Event Bindings (Scene)
Some `FloatGameEventListener` components in the scene are using static method calls instead of dynamic float calls, causing them to ignore the actual event data.

- **File**: `Assets/Scenes/MainLevel.unity`
- **Action**: 
    - Identify `FloatGameEventListener` components on `HUD Manager` and `Velocity Gauge Container`.
    - Change the `_response` UnityEvent bindings to use **Dynamic** calls (e.g., `UIManager -> OnPlayerHealthChanged` under the 'Dynamic float' section in the dropdown).
    - Specifically fix: `OnPlayerHealthChanged`, `OnGaugeChanged`, and `OnLifeLost`.

## 3. Standardize Call States
Ensure all listeners are set to `Runtime Only` to avoid unintended behavior in the editor.

# Verification & Testing
1. **Console Check**: Verify no `NullReferenceException` appears when damage is taken or enemies are killed.
2. **Health Bar**: Confirm health segments deactivate as HP decreases.
3. **Lives**: Confirm life icons disappear when the player dies.
4. **Velocity Gauge**: Confirm the gauge fills and the slider moves when killing enemies.
5. **Logs**: Ensure debug logs like `[UIManager] HP: ...` show the actual varying health values.
