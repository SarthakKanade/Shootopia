# Project Overview
- Game Title: Sharp Shooter (Velocity Wars)
- High-Level Concept: Fast-paced shooter where killing enemies fills a "Velocity Gauge" to unlock abilities.
- Players: Single player.
- Render Pipeline: URP (PC_RPAsset).
- Target Platform: StandaloneOSX (PC).

# Game Mechanics
## Core Gameplay Loop
- Move fast, kill enemies, fill the Velocity Gauge.
- Once the gauge hits 100%, an ability becomes ready.
- Higher streaks (implemented later) will increase gauge gain.

## Controls and Input Methods
- Mouse and Keyboard (New Input System & Legacy support).
- Shooting with various weapons (Pistol, Machine Gun, etc.).

# UI
- Velocity Gauge (handled by `_onGaugeChanged` event).
- Ability Ready notification (handled by `_onAbilityReady` event).
- Streak display (handled by `_onStreakChanged` event).

# Key Asset & Context
- `VelocityGaugeManager.cs`: Manages the gauge fill and streak.
- `EnemyHealth.cs`: Handles enemy HP and death.
- `WeaponSO.cs`: ScriptableObject defining weapon stats including `gaugeKillFill`.
- `IDamageable.cs`: Interface for receiving damage.

# Implementation Steps
## Step 1: Update IDamageable Interface
- Modify `Assets/Scripts/Core/Interfaces/IDamageable.cs` to include an optional `WeaponSO` parameter in `TakeDamage`.
- **Dependency**: None.

## Step 2: Implement Gauge Logic in VelocityGaugeManager
- Open `Assets/Scripts/Player/VelocityGaugeManager.cs`.
- Implement `AddCharge(float amount)` to clamp fill and raise events.
- Implement `OnEnemyKilled(WeaponSO weaponUsed)` to increment streak and add charge based on `weaponUsed.gaugeKillFill`.
- **Dependency**: Step 1.

## Step 3: Update Weapon Shooting Logic
- Open `Assets/Scripts/Weapons/Weapon.cs`.
- Pass the `WeaponSO` to `TakeDamage` when hitting a target.
- **Dependency**: Step 1.

## Step 4: Update Enemy Health and Death Logic
- Open `Assets/Scripts/Enemies/EnemyHealth.cs`.
- Store the `WeaponSO` from the last `TakeDamage` call.
- In `OnDeath()`, call `VelocityGaugeManager.OnEnemyKilled` with the stored weapon.
- **Dependency**: Steps 1, 2, 3.

# Verification & Testing
## Manual Tests
1. **Pistol Kill Test**: Play the scene, kill a robot with the Pistol. Verify the gauge increases by exactly 20%.
2. **Full Gauge Test**: Kill 5 robots with the Pistol. Verify the gauge hits 100% and `_onAbilityReady` is raised.
3. **Streak Test (Basic)**: Verify `_currentStreak` increments on each kill via the `_onStreakChanged` event (even if no multiplier is applied yet).
4. **Non-Weapon Death Test**: If an enemy dies without a weapon (e.g., self-destruct), verify it doesn't crash (should handle `null` weapon).
