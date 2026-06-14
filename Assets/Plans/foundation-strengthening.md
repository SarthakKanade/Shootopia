# Project Overview
- Game Title: Velocity Wars
- High-Level Concept: Fast-paced shooter with a momentum-based "Velocity Gauge."
- Players: Single player (Base for Multiplayer).

# Game Mechanics
## Core Gameplay Loop
Kill enemies -> Fill Gauge -> Use Abilities -> Maintain Speed.
## Spawning and AI
Enemies spawn from Gates and target the player. Currently, these systems are disconnected from their data (EnemySO).

# Key Asset & Context
- `Robot.prefab`: Needs `BasicRobot.asset` assigned.
- `Turret` (Scene Objects): Need `Turret.asset` assigned.
- `SpawnGate.cs`: Needs to be more robust.
- `Turret.cs`: Needs to be more robust.

# Implementation Steps

## 1. Fix Configuration Assignments
- **Task**: Assign `BasicRobot.asset` to `Robot` prefab and `Turret.asset` to scene Turrets.
- **Goal**: Give enemies health, speed, and damage values.

## 2. Robust AI References
- **File**: `Assets/Scripts/Enemies/Robot.cs`
- **Changes**: 
    - Add a property to lazily find the player if `_player` is null.
    - Check if `NavMeshAgent` is on a NavMesh before setting destination.
- **File**: `Assets/Scripts/Enemies/Turret.cs`
- **Changes**: 
    - Lazily find the player.
    - Automatically assign `playerTargetPoint` if missing.

## 3. Robust Spawning
- **File**: `Assets/Scripts/Enemies/SpawnGate.cs`
- **Changes**: 
    - Make `SpawnRoutine` check for the player every loop instead of just at the start.
    - Add a check for `GameManager.Instance.CurrentState == GameState.Playing`.

## 4. Input System Migration (Foundation)
- **Task**: Add "Ability" action to `StarterAssets.inputactions`.
- **Task**: Update `VelocityGaugeManager.cs` to use the Input Action instead of `KeyCode.F`.

# Verification & Testing
1. **Spawning**: Confirm robots appear from gates.
2. **AI Action**: Confirm robots move toward player and turrets fire.
3. **Gauge**: Confirm killing these enemies fills the gauge (now that they have config data).
4. **Input**: Confirm 'F' (or the new bound key) still activates the ability.
