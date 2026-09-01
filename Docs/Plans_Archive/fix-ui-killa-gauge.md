# Project Overview
- Game Title: Sharp Shooter (Velocity Wars)
- High-Level Concept: Fast-paced shooter where killing enemies charges a special ability gauge (the "Velocity Gauge").
- Current Issue: Most UI elements (Gauge, Health, Enemy Count) are non-functional. Only the bullet count (Ammo Text) works.

# Game Mechanics
## Core Gameplay Loop
- Kill enemies to charge the Velocity Gauge.
- Once the gauge is full, activate a special ability.
- Manage health (Shield Bars) while eliminating all enemies.

## Key Findings
- **Velocity Gauge**: The user clarified the gauge should be based on **kills**, not movement. The current implementation has movement-based logic that is likely conflicting or confusing ("a mess").
- **Enemy Count**: Not updating because the `GameManager` is missing the `OnEnemyCountChanged` event assignment.
- **Shield Bars**: Not updating correctly due to a bug in `UIManager` that deactivates the Shield Icon incorrectly and potential event initialization issues.
- **Durability**: References are missing in the scene, which is fine for now as long as the primary HUD works.

# Key Asset & Context
- `Assets/Scripts/Player/VelocityGaugeManager.cs`: Primary logic for charging the gauge.
- `Assets/Scripts/Core/Managers/UIManager.cs`: Main UI controller.
- `Assets/Scripts/Misc/GameManager.cs`: Scene-level game state manager.
- `Assets/Scripts/Enemies/EnemyHealth.cs`: Triggers the gauge charge on death.

# Implementation Steps
## 1. Clean up Velocity Gauge Logic
- **Modify `VelocityGaugeManager.cs`**:
    - Remove `UpdateVelocityCharge()` and all movement-related fields (`_minVelocityThreshold`, `_velocityFillMultiplier`).
    - Focus `AddCharge` and `ConsumeCharge` on kill-based input.
    - **Crucial**: Call `_ui.OnGaugeChanged()` directly in `AddCharge` and `ConsumeCharge` to ensure the UI updates even if the ScriptableObject event system has issues in the current scene setup.
    - Remove unused `_onStreakChanged` if no UI exists for it, or keep it as optional.

## 2. Fix Event Assignments
- **In the Main Scene**:
    - Assign `OnEnemyCountChanged.asset` to the `GameManager` script.
    - Assign `OnDurabilityChanged.asset` and `OnMaxDurabilityChanged.asset` to the `ActiveWeapon` script on the Player (for future-proofing).

## 3. Fix UIManager & Health Display
- **Modify `UIManager.cs`**:
    - Fix `OnPlayerHealthChanged` to correctly handle the `shieldBars` array. Ensure the last element (the Shield Icon) is not deactivated by the loop if it's meant to stay visible.
    - Add a null check for `shieldBars` and ensure it handles the health value correctly (e.g. `Mathf.CeilToInt(health)`).
- **In `PlayerHealth.cs`**:
    - Ensure `_onPlayerHealthChanged` is raised in `Start` to initialize the UI.

## 4. Verification & Testing
- **Kill Test**: Kill an enemy and verify the "Velocity Gauge" (Kill Gauge) increases and "Enemies Left" decreases.
- **Health Test**: Take damage (if possible) or trigger damage via script to verify Shield Bars deactivate.
- **Movement Test**: Verify that moving/sprinting NO LONGER charges the gauge (as requested).

# Refined Recommendation
The user specifically pointed out that the gauge is "according to the killa". I will streamline the `VelocityGaugeManager` to be a pure kill-based gauge, removing the "messy" movement code that was causing confusion and potentially masking the fact that kill-based charging wasn't showing up on the UI.
