# Death Penalty and Gauge Drain Implementation Plan

This plan implements the player death penalty for the velocity gauge system, including gauge deduction based on the cause of death and streak reset.

## Project Overview
- **Game Title:** Velocity Wars (Sharp Shooter)
- **High-Level Concept:** Fast-paced shooter with a momentum-based ability gauge.
- **Render Pipeline:** PC_RPAsset (URP likely)
- **Input System:** Both (Legacy and New)

## Game Mechanics
- **Velocity Gauge:** Increases as player moves/kills, drains on death.
- **Death Penalty:** Specific percentages of the gauge are lost depending on what killed the player.
- **Streak:** Resets to 0 on death.
- **Ability Retention:** If the gauge stays at 100% after the penalty, the ability remains ready.

## Key Asset & Context
- `Assets/Scripts/Player/VelocityGaugeManager.cs`: Manages the gauge state.
- `Assets/Scripts/Player/PlayerHealth.cs`: Handles player health and death.
- `Assets/Scripts/Core/Interfaces/IDamageable.cs`: Defines `DamageType` and damage interface.
- `Assets/Scripts/Enemies/Projectile.cs`: Source of turret damage.
- `Assets/Scripts/Weapons/Weapon.cs`: Source of player damage (for MP).

## Implementation Steps

### 1. Update DamageType Enum
Modify `Assets/Scripts/Core/Interfaces/IDamageable.cs` to ensure the required enum values exist and update the default parameter.
- Change `DamageType` to: `Melee`, `Projectile`, `Environment`, `Player`, `Explosion`.
- Update `IDamageable.TakeDamage` default parameter from `Bullet` to `Projectile`.

### 2. Implement Gauge Drain in VelocityGaugeManager
Update `Assets/Scripts/Player/VelocityGaugeManager.cs`:
- Implement `ConsumeCharge(float amount)`:
  - Deduct amount from `_gaugeFill`.
  - Clamp floor at 0.
  - Set `_abilityReady = false` if `_gaugeFill < 100f`.
  - Raise `_onGaugeChanged`.
- Implement `OnPlayerDied(DamageType causeOfDeath)`:
  - Use the switch expression provided in the PRD.
  - Reset `_currentStreak` to 0.
  - Call `ConsumeCharge(penalty)`.

### 3. Integrate with PlayerHealth
Modify `Assets/Scripts/Player/PlayerHealth.cs`:
- Implement `IDamageable` interface.
- Add a private field `_lastDamageType` to track the cause of death.
- Add a private reference to `VelocityGaugeManager`.
- Update `TakeDamage(int amount)` to `TakeDamage(float amount, DamageType type, WeaponSO weaponUsed)` (implementing the interface).
- Keep a legacy `TakeDamage(int amount)` that calls the new one with a default type (e.g., `Environment`).
- In `PlayerGameOver()`, call `_gaugeManager.OnPlayerDied(_lastDamageType)`.

### 4. Update Damage Sources
- **Projectiles:** Update `Projectile.cs` to use `DamageType.Projectile`.
- **Weapons:** Update `Weapon.cs` to use `DamageType.Player` (representing damage from a player-held weapon).
- **Robots:** Update `Robot.cs` to apply `DamageType.Melee` damage to the player before self-destructing (or ensure the explosion it causes uses the correct type).
- **Explosions:** Update `Explosion.cs` to use `IDamageable` and pass `DamageType.Environment` or `Explosion`.

## Verification & Testing
- **Test Robot Death:** Die to a robot melee/explosion. Verify gauge drops 50% and streak resets to 0.
- **Test Turret Death:** Die to turret projectiles. Verify gauge drops 30%.
- **Test Environment Death:** Die to environment (if applicable) or default damage. Verify gauge drops 20%.
- **Test MP Death:** (Simulated) Die to player damage. Verify gauge drops 40%.
- **Test Gauge Clamping:** Die with 10% gauge to a 50% penalty source. Verify gauge stays at 0 (no negative values).
- **Test Ability Retention:** Have 120% gauge (if possible, though capped at 100 in `AddCharge`). If gauge was at 100% and penalty is 0, it should stay ready. (Note: `ConsumeCharge` checks if fill < 100).
