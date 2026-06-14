# Project Overview
- Game Title: Sharp Shooter (Velocity Wars)
- High-Level Concept: Fast-paced shooter where killing enemies fills a "Velocity Gauge" to unlock abilities.
- Streak System: Players gain more gauge charge as they maintain a kill streak.

# Game Mechanics
## Streak Multiplier
- Multiplier kicks in at a specific kill threshold (default 3).
- Each kill above the threshold increases the multiplier by a fixed amount (default 0.5x).
- Multiplier is capped at a maximum streak level.
- Multiplier table (default settings):
  - Kills 1-2: 1.0x
  - Kill 3: 1.5x
  - Kill 4: 2.0x
  - Kill 5+: 2.5x

# Key Asset & Context
- `VelocityGaugeManager.cs`: Handles streak logic and charge calculation.
- `WeaponSO.cs`: Provides base `gaugeKillFill` values.

# Implementation Steps
## Step 1: Implement GetStreakMultiplier
- Add a private helper method `GetStreakMultiplier(int streak)` to `VelocityGaugeManager.cs`.
- Use serialized fields: `_multStartsAtKill`, `_maxStreakCap`, `_multPerStreakLevel`.
- **Dependency**: None.

## Step 2: Update OnEnemyKilled
- Modify `OnEnemyKilled(WeaponSO weaponUsed)` in `VelocityGaugeManager.cs`.
- Calculate multiplier based on the *new* streak value.
- Apply multiplier to `weaponUsed.gaugeKillFill`.
- Add a `Debug.Log` to verify calculations during gameplay.
- **Dependency**: Step 1.

# Verification & Testing
## Manual Tests
1. **Kill 1 & 2**: Kill two enemies with Sniper (35% base). Gauge should increase by 35% each time.
2. **Kill 3 (Multiplier starts)**: Kill a third enemy with Sniper. Calculation: 35 * 1.5 = 52.5%.
3. **Kill 4**: Kill a fourth enemy with Pistol (20% base). Calculation: 20 * 2.0 = 40%.
4. **Kill 5+**: Kill a fifth enemy. Verify multiplier is capped at 2.5x.
5. **Logs**: Check Unity Console for the `Debug.Log` output verifying these specific numbers.
