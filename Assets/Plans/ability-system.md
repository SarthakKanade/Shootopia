# Project Overview
- Game Title: Velocity Wars
- High-Level Concept: Fast-paced shooter with a momentum-based "Velocity Gauge" that unlocks special abilities.
- Players: Single player (Multiplayer supported in architecture).
- Inspiration: Doom, Quake, Ultrakill.
- Target Platform: PC (Standalone).
- Render Pipeline: URP (PC_RPAsset).

# Game Mechanics
## Core Gameplay Loop
Kill enemies to fill the Velocity Gauge. Once full, activate powerful abilities to maintain momentum and deal more damage.
## Controls and Input Methods
- **F Key**: Activate the selected ability when the gauge is at 100%.

# UI
- Velocity Gauge bar (existing).
- Ability icon (to be wired to `AbilitySO`).
- Ability ready indicator (glowing bar).

# Key Asset & Context
- `AbilitySO.cs`: Base class for all player abilities.
- `OverclockAbilitySO.cs`: Boosts fire rate and provides temporary ammo.
- `TrackerBotAbilitySO.cs`: Spawns a drone to assist the player.
- `VelocityGaugeManager.cs`: Handles gauge logic and ability activation.
- `ActiveWeapon.cs`: Handles weapon firing and will be modified to support overclocking.

# Implementation Steps

## 1. Modify Core Systems for Ability Support
### 1.1 Update `ActiveWeapon.cs`
- **File**: `Assets/Scripts/Player/ActiveWeapon.cs`
- **Changes**:
    - Add `public float FireRateMultiplier { get; set; } = 1f;`
    - Add `public int CurrentUses { get => _currentUses; set => _currentUses = value; }`
    - Add `public int CurrentAmmo { get => currentAmmo; set { currentAmmo = value; ammoText.text = currentAmmo.ToString("D2"); } }`
    - Modify `HandleShoot()` to use `currentWeaponSO.FireRate / FireRateMultiplier`.
- **Dependency**: None.

### 1.2 Refactor `AbilitySO.cs`
- **File**: `Assets/Scripts/Abilities/AbilitySO.cs`
- **Changes**:
    - Make class `abstract`.
    - Add `public string description;`.
    - Add `public float duration;`.
    - Add `public abstract void Activate(GameObject player, GameContext context);`.
    - Add `public abstract void Deactivate(GameObject player);`.
    - Define `public enum GameContext { SinglePlayer, Multiplayer }`.
- **Dependency**: None.

## 2. Implement Specific Abilities
### 2.1 Create `OverclockAbilitySO.cs`
- **File**: `Assets/Scripts/Abilities/OverclockAbilitySO.cs`
- **Logic**:
    - `Activate`: Get `ActiveWeapon` and set `FireRateMultiplier = 1.5f` and `CurrentUses = 999` (or `CurrentAmmo = 999`).
    - `Deactivate`: Reset `FireRateMultiplier = 1.0f`.
- **Dependency**: Step 1.1, 1.2.

### 2.2 Create `TrackerBotAbilitySO.cs`
- **File**: `Assets/Scripts/Abilities/TrackerBotAbilitySO.cs`
- **Logic**:
    - `Activate`: Instantiate `trackerBotPrefab` at player position.
    - `Deactivate`: No-op (bot handles its own lifecycle or just stays until destroyed).
- **Dependency**: Step 1.2.

## 3. Update Ability Activation Logic
### 3.1 Modify `VelocityGaugeManager.cs`
- **File**: `Assets/Scripts/Player/VelocityGaugeManager.cs`
- **Changes**:
    - Add `private bool _isAbilityActive;`.
    - Add `Update()` method.
    - Check for `_abilityReady && !_isAbilityActive && Input.GetKeyDown(KeyCode.F)`.
    - Implement `ExecuteAbility(AbilitySO ability)` Coroutine:
        - Call `ability.Activate()`.
        - Reset gauge (`_gaugeFill = 0`, `_abilityReady = false`).
        - Raise events.
        - Wait for `ability.duration`.
        - Call `ability.Deactivate()`.
        - Set `_isAbilityActive = false`.
- **Dependency**: Step 1.2.

## 4. Asset Creation & Wiring
- Create `Overclock` and `TrackerBot` assets in `Assets/Data/Abilities/`.
- Assign `Overclock` asset to the `VelocityGaugeManager` on the `Player` prefab.
- Create a placeholder prefab for the Tracker Bot (using a sphere or the robot enemy model).

# Verification & Testing
1. **Gauge Filling**: Kill enemies or use a debug command to fill the gauge to 100%. Verify the bar glows/indicates readiness.
2. **Activation**: Press 'F'. Verify the gauge resets to 0.
3. **Overclock Test**: Verify the weapon fires 50% faster and ammo doesn't run out (or stays at 999) for the duration. Verify it returns to normal after the duration.
4. **Tracker Bot Test**: Verify a bot spawns upon activation.
5. **Lockout Test**: Ensure pressing 'F' again while an ability is active does nothing.
6. **Persistence**: Ensure switching weapons during Overclock doesn't break the multiplier (multiplier should apply to the new weapon if possible, or we handle weapon switches in the ability). *Note: The current plan applies it to ActiveWeapon which persists.*
