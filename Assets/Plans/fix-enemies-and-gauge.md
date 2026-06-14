# Project Overview
- Game Title: Sharp Shooter (Velocity Wars)
- High-Level Concept: A fast-paced shooter where movement velocity fuels your special abilities.
- Players: Single player
- Target Platform: PC (macOS)
- Render Pipeline: URP

# Game Mechanics
## Core Gameplay Loop
- Move at high speeds to fill the Velocity Gauge.
- Defeat enemies to increase your streak and gain extra gauge charge.
- Use special abilities (like Overclock or Tracker Bot) once the gauge is full.
## Controls and Input Methods
- Standard First Person Controls (WASD + Mouse).
- Space to jump, Shift to sprint.
- 'Q' or Ability key to fire the current ability.

# UI
- **Enemies Left**: Displays the count of remaining enemies in the level.
- **Velocity Gauge**: A bar that fills as the player moves and kills enemies.
- **Ability Icon**: Shows the currently equipped ability.

# Key Asset & Context
- **Scripts**: `GameManager.cs`, `VelocityGaugeManager.cs`, `EnemyHealth.cs`, `Turret.cs`.
- **Assets**: `Assets/Data/Enemies/Turret.asset`, `Assets/Data/Enemies/BasicRobot.asset`.

# Implementation Steps

## 1. Fix Enemy Counter UI
- **File**: `Assets/Scripts/Misc/GameManager.cs`
- **Action**: Update `RegisterEnemy()` to raise the `_onEnemyCountChanged` event so the UI updates correctly at the start of the game.
- **Dependency**: None.

## 2. Implement Velocity-Based Gauge Filling
- **File**: `Assets/Scripts/Player/VelocityGaugeManager.cs`
- **Action**:
    - Add horizontal velocity tracking in `Update()` using the `CharacterController` from the player.
    - Increment `_gaugeFill` based on the player's current speed if it exceeds a minimum threshold.
    - Ensure the gauge UI is initialized to 0 on `Start`.
    - Add tunable fields for `_velocityFillMultiplier` and `_minVelocityThreshold`.
- **Dependency**: None.

## 3. Fix Enemy Firing and Configurations
- **Scene/Assets**:
    - Assign `Turret.asset` to the `_config` field of all `Turret` components in the scene.
    - Assign appropriate `EnemySO` (Turret or BasicRobot) to all `EnemyHealth` components in the scene.
- **Dependency**: None.

# Verification & Testing
- **Enemy Count**: Start the game and verify "Enemies Left" shows the correct number (e.g., 3 if there are 3 turrets).
- **Enemy Firing**: Approach a turret and verify it looks at the player and fires projectiles.
- **Velocity Gauge**: 
    - Verify the gauge starts at 0.
    - Move around (sprint) and verify the gauge fills up.
    - Stop moving and verify it stops filling.
    - Kill an enemy and verify it grants a chunk of charge.
- **Ability Activation**: Fill the gauge and press the ability key to verify it triggers and empties the gauge.
