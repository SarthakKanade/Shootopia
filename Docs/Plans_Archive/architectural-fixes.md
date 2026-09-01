# Project Overview
- Game Title: Sharp Shooter
- High-Level Concept: High-velocity first-person shooter with momentum-based mechanics.
- Players: Single player
- Inspiration / Reference Games: Ultrakill, Doom Eternal
- Tone / Art Direction: Fast-paced, stylized
- Target Platform: PC (macOS)
- Screen Orientation / Resolution: Landscape
- Render Pipeline: URP (PC_RPAsset)

# Game Mechanics
## Core Gameplay Loop
- Players move fast, kill enemies to fill a Velocity Gauge, and use abilities when the gauge is full.
- Maintaining momentum and streaks is key to high scores and ability availability.

## Controls and Input Methods
- Standard FPS controls (WASD, Mouse).
- Ability activation (Special key).

# UI
- Velocity Gauge UI showing charge and streak multiplier.

# Key Asset & Context
- `EnemyHealth.cs`: Handles enemy damage and death logic.
- `VelocityGaugeManager.cs`: Manages player momentum and ability charge.
- `PlayerHealth.cs`: Handles player damage and game over logic.
- `GameManager.cs`: Controls game state and lives system.
- `Pistol.asset`: A WeaponSO asset that currently has the wrong category.
- `OnEnemyKilledWithGauge.asset`: A new FloatGameEvent to decouple systems.

# Implementation Steps

## 1. Create the Event Asset
- Create a new `FloatGameEvent` asset at `Assets/Data/Events/OnEnemyKilledWithGauge.asset`.

## 2. Refactor EnemyHealth (Decoupling)
- **File**: `Assets/Scripts/Enemies/EnemyHealth.cs`
- **Changes**:
    - Add `[SerializeField] private FloatGameEvent _onEnemyKilledWithGauge;`.
    - In `OnDeath()`, remove `Object.FindFirstObjectByType<VelocityGaugeManager>()`.
    - Instead, call `_onEnemyKilledWithGauge.Raise(_lastWeaponUsed != null ? _lastWeaponUsed.gaugeKillFill : 0f);`.
- **Dependency**: Step 1

## 3. Update VelocityGaugeManager (Listener)
- **File**: `Assets/Scripts/Player/VelocityGaugeManager.cs`
- **Changes**:
    - Add a public method `OnEnemyKilledWithGauge(float gaugeFill)` that implements the logic previously found in `OnEnemyKilled(WeaponSO weaponUsed)`.
    - This method will increment `_currentStreak`, calculate the multiplier, and call `AddCharge()`.
- **Dependency**: None

## 4. Implement Lives System in GameManager
- **File**: `Assets/Scripts/Misc/GameManager.cs`
- **Changes**:
    - Add `_maxLives` (serialized) and `_currentLives` (private).
    - Initialize `_currentLives` in `Awake`.
    - Add `public void LoseLife()`.
    - In `LoseLife()`, decrement lives. If lives <= 0, call `SetState(GameState.GameOver)`.
    - (Optional) Add a placeholder for respawn logic if lives > 0.
- **Dependency**: None

## 5. Update PlayerHealth (Use Lives System)
- **File**: `Assets/Scripts/Player/PlayerHealth.cs`
- **Changes**:
    - In `PlayerGameOver()`, replace `GameManager.Instance.SetState(GameState.GameOver);` with `GameManager.Instance.LoseLife();`.
- **Dependency**: Step 4

## 6. Fix Weapon Category
- **Asset**: `Assets/Data/Weapons/Pistol.asset`
- **Changes**: Set `category` to `Default` (0) in the Inspector.
- **Dependency**: None

## 7. Wire Up Scene and Prefabs
- **Prefabs**: `Robot`, `Spawn Gate`, `Turret`. Assign the `OnEnemyKilledWithGauge` event to the `EnemyHealth` component.
- **Scene**: `MainLevel.unity`. Add a `FloatGameEventListener` to the `Player` GameObject.
    - Event: `OnEnemyKilledWithGauge`.
    - Response: `VelocityGaugeManager.OnEnemyKilledWithGauge`.
- **Dependency**: Steps 1, 2, 3

# Verification & Testing
- **Enemy Death**: Kill an enemy and verify the Velocity Gauge still fills correctly and the streak increments. Check the console for any `NullReferenceException` related to missing gauge manager.
- **Player Death**: Take enough damage to "die". Verify that the game doesn't immediately end (if lives > 0) or that it ends after the final life is lost.
- **Pistol Category**: Inspect the Pistol asset and verify the category is "Default".
