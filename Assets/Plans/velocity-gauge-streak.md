# Project Overview
- **Game Title**: Sharp Shooter
- **High-Level Concept**: A shooter focusing on momentum and kill streaks to power up abilities.
- **Players**: Single player.
- **Render Pipeline**: URP (Custom Asset: PC_RPAsset).
- **Target Platform**: PC (StandaloneOSX).

# Game Mechanics
## Core Gameplay Loop
Players kill enemies to fill a Velocity Gauge. Higher streaks provide multipliers to the gauge fill rate. Once full, an ability can be fired.
## Controls and Input Methods
Standard shooter controls (Keyboard/Mouse or Gamepad).

# UI
A velocity gauge bar (handled by `_onGaugeChanged` event) and a streak counter (handled by `_onStreakChanged` event).

# Key Asset & Context
- `Assets/Scripts/Player/VelocityGaugeManager.cs`: Manages the gauge fill and streak logic.
- `Assets/Scripts/Weapons/WeaponSO.cs`: Stores the base gauge fill per weapon.

# Implementation Steps
## Step 1: Implement Streak Multiplier Logic
- **File**: `Assets/Scripts/Player/VelocityGaugeManager.cs`
- **Change**: Add the `GetStreakMultiplier` method and update `OnEnemyKilled`.
- **Logic**:
    - `GetStreakMultiplier(int streak)`: Returns 1.0x if streak < 3. Scales by 0.5x per kill from kill 3 to 5 (max 2.5x).
    - `OnEnemyKilled(WeaponSO weaponUsed)`: Increments streak, calculates multiplier, applies to `weaponUsed.gaugeKillFill`, and calls `AddCharge`.
- **Dependency**: None.

# Verification & Testing
## Manual Verification
- **Kill 1 with Sniper**: Verify gauge increases by 35% (multiplier 1.0).
- **Kill 3 with Sniper**: Verify gauge increases by 52.5% (multiplier 1.5).
- **Kill 4 with Pistol**: Verify gauge increases by 40% (multiplier 2.0).
- **Kill 5 with MachineGun**: Verify gauge increases by 96% (multiplier 2.5).
- **Debug Logs**: Ensure `OnEnemyKilled` logs the calculated fill amount for verification.
