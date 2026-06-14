# Project Overview
- Game Title: Velocity Wars
- High-Level Concept: High-speed shooter with expiring weapon mechanics.
- Players: Single player
- Inspiration / Reference Games: Fast-paced arena shooters
- Tone / Art Direction: Sci-fi / Cyberpunk (URP)
- Target Platform: Standalone (PC/Mac)
- Screen Orientation / Resolution: Landscape
- Render Pipeline: URP

# Game Mechanics
## Core Gameplay Loop
Players collect weapons from crates. Each weapon has a limited lifespan based on uses, time, or ammo. Once a weapon expires, the player must find another one to continue the fight, creating a "tension of expiry".
## Controls and Input Methods
- WASD for movement
- Mouse for aiming/shooting
- New Input System and Legacy Input Manager both active.

# UI
A small HUD element in the bottom-left corner will show the remaining durability (uses, time, or ammo).
- **Durability Text**: Displays numerical value (e.g., "12 uses", "15.0s").
- **Durability Bar**: Proportional fill amount representing remaining durability.

# Key Asset & Context
- `WeaponSO.cs`: ScriptableObject defining weapon properties including `DurabilityType`, `maxUses`, and `weaponDuration`.
- `ActiveWeapon.cs`: Player component managing the currently equipped weapon and its state.
- `UIManager.cs`: Manager for HUD elements.
- `GameEvent.cs` / `FloatGameEvent.cs`: Event system for broadcasting weapon state changes.

# Implementation Steps
## 1. Create OnWeaponExpired GameEvent Asset
- Create a new `GameEvent` asset at `Assets/Data/Events/OnWeaponExpired.asset`.
- This event will be raised when a weapon reaches 0 uses or its time expires.

## 2. Update ActiveWeapon.cs
- **Add Using Statement**: `using VelocityWars.Core;`
- **Add Serialized Fields**:
    - `[SerializeField] private GameEvent _onWeaponExpired;`
    - `[SerializeField] private FloatGameEvent _onDurabilityChanged;`
- **Add Private State Variables**:
    - `private int _currentUses;`
    - `private float _timeRemaining;`
    - `private Coroutine _timeBasedRoutine;`
- **Refactor SwitchWeapon**:
    - Stop any existing `_timeBasedRoutine`.
    - Initialize durability based on `weaponSO.durabilityType`.
    - For `Uses`: Set `_currentUses = weaponSO.maxUses` and raise `_onDurabilityChanged`.
    - For `TimeBased`: Set `_timeRemaining = weaponSO.weaponDuration` and start `TimeBasedRoutine`.
    - For `Ammo`: Use existing `MagazineSize` logic.
- **Implement DecrementDurability**:
    - A new method to handle durability reduction after a shot.
    - `Uses`: Decrement `_currentUses`, raise event, and call `ExpireWeapon` if <= 0.
    - `Ammo`: Continue using `AdjustAmmo`.
- **Implement TimeBasedRoutine**:
    - Coroutine that decrements `_timeRemaining` and raises `_onDurabilityChanged` until 0.
    - Calls `ExpireWeapon` when time runs out.
- **Implement ExpireWeapon**:
    - Destroys the `currentWeapon` instance.
    - Sets `currentWeaponSO` and `currentWeapon` to null.
    - Raises `_onWeaponExpired`.
    - Raises `_onDurabilityChanged(0)`.

## 3. Update UIManager.cs
- **Add UI References**:
    - `[SerializeField] private TMP_Text _durabilityText;`
    - `[SerializeField] private Image _durabilityBar;`
- **Implement Event Handlers**:
    - `OnDurabilityChanged(float value)`:
        - Updates text based on current weapon's `durabilityType`.
        - Calculates and sets `_durabilityBar.fillAmount`.
    - `OnWeaponExpired()`:
        - Resets text to "—" and bar to 0.

## 4. Scene Setup & Asset Configuration
- Update `Pistol` and `MachineGun` WeaponSO assets with `durabilityType`, `maxUses`, etc.
- In the Scene, assign the new references in the `ActiveWeapon` and `UIManager` components.
- Add `GameEventListener` and `FloatGameEventListener` components to the UI to link the events to the `UIManager` methods.

# Verification & Testing
- **Uses Test**: Equip a Pistol (12 uses). Fire 12 times. Verify weapon disappears and HUD resets.
- **Time Test**: Equip a Time-based weapon (if any). Wait for duration. Verify weapon disappears.
- **HUD Accuracy**: Verify the text and bar update correctly during firing and time depletion.
- **Edge Case**: Ensure shooting is disabled when no weapon is equipped.
