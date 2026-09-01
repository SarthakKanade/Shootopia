# Project Overview
- **Game Title:** Sharp Shooter: Velocity Wars
- **High-Level Concept:** Fast-paced arena FPS built in Unity 6 (URP) focused on momentum-based combat.
- **Players:** Single player (with multiplayer bypass logic for lives).
- **Inspiration:** Doom Eternal, Quake.
- **Tone / Art Direction:** Sci-fi arena, high-intensity visuals.
- **Target Platform:** PC/Mac (Standalone).
- **Render Pipeline:** URP.

# Game Mechanics
## Core Gameplay Loop
The player engages in fast-paced combat, gaining momentum (Velocity Gauge) from kills. This gauge unlocks abilities. Survival is managed through health crates and a lives system. The loop focuses on aggressive movement and resource management (durability and health).

## Controls and Input Methods
- **WASD:** Movement.
- **Mouse:** Aim and Shoot.
- **F:** Ability activation.
- **New Input System:** Integrated with `StarterAssetsInputs`.

# UI
## HUD Elements
- **Velocity Gauge:** A slider (0-100) that pulses when full.
- **Lives HUD:** 3 icons in the top-left, depleting right-to-left.
- **Health/Shield HUD:** Existing shield bars will be updated to reflect the new 100 HP system.

# Key Assets & Context
- **Scripts:** `PlayerHealth.cs`, `HealthCrate.cs`, `HealthCrateNode.cs`, `LivesManager.cs`, `UIManager.cs`.
- **Events:** `GameEvent`, `FloatGameEvent` ScriptableObjects.
- **Prefabs:** `HealthCrate.prefab`.
- **Scene:** `MainLevel` (Primary gameplay).

# Implementation Steps

## Phase 1: Core System Refinement (Steps 6 & 8 Prerequisites)
1. **Modify `IDamageable.cs`**:
   - Update `DamageType` enum to include `Self`.
   - Ensure the interface remains consistent with the project's architecture.
2. **Modify `GameEvent.cs` and `FloatGameEvent.cs`**:
   - Add `RegisterListener(Action)` and `UnregisterListener(Action)` to `GameEvent`.
   - Add `RegisterListener(Action<float>)` and `UnregisterListener(Action<float>)` to `FloatGameEvent`.
   - This enables `LivesManager` and other scripts to subscribe programmatically without a dedicated `GameEventListener` component.

## Phase 2: Player Health System (Step 6)
1. **Rewrite `PlayerHealth.cs`**:
   - Change health from `int` to `float` (100.0f max).
   - Implement `IDamageable` using the new logic.
   - Add `Heal(float amount)` with clamping.
   - Broadcast `OnHpChanged` and `OnPlayerDied` events.
   - Remove visual/camera logic (to be handled by `LivesManager` or existing systems).
2. **Configure Player Prefab**:
   - Attach the rewritten `PlayerHealth`.
   - Assign `OnHpChanged` (FloatGameEvent) and `OnPlayerDied` (GameEvent).

## Phase 3: Health Crate & Respawn Node (Step 6 & 7)
1. **Create `HealthCrate.cs`**:
   - Implement trigger logic to heal player and invoke `OnPickedUp`.
2. **Create `HealthCrateNode.cs`**:
   - Implement spawning and respawn coroutine logic (25s delay).
3. **Create `HealthCrate` Prefab**:
   - Setup visuals (Cube, Point Light, Icon).
   - Add Box Collider (Trigger).
   - Attach `HealthCrate.cs`.
4. **Scene Setup**:
   - Place 3 `HealthCrateNode` GameObjects in `MainLevel` at "mid-risk" positions.

## Phase 4: Lives Manager (Step 8)
1. **Create `LivesManager.cs`**:
   - Track `_currentLives` (default 3).
   - Register for `OnPlayerDied` event.
   - Handle respawn delay (2s), player repositioning, and health reset.
   - Call `GameManager.Instance.SetState(GameState.GameOver)` when lives reach 0.
   - Implement `_isMultiplayer` bypass.
2. **Scene Setup**:
   - Attach `LivesManager` to the `Game Manager` object in `MainLevel`.
   - Create and assign a `SpawnPoint` Transform.

## Phase 5: HUD & UI Implementation (Step 9)
1. **Update `UIManager.cs`**:
   - Add references for `_gaugeSlider`, `_gaugeAnimator`, and `_lifeIcons`.
   - Implement `OnGaugeChanged(float)` and `OnLifeLost(float)`.
2. **Scene UI Setup**:
   - Add `Slider` and `Animator` components to `Velocity Gauge Container`.
   - Create "Lives Container" (Horizontal Layout) with 3 Images in `Overlay Canvas`.
   - Wire the `FloatGameEventListener` components on the `HUD Manager` to call `UIManager` methods.

# Verification & Testing
1. **Health Tests**: 
   - Verify health clamps at 100 when picking up crates.
   - Verify crate disappears and respawns after 25s.
2. **Lives Tests**:
   - Die 3 times in Single Player -> Game Over.
   - Verify respawn delay and HP reset to 100.
   - Toggle `_isMultiplayer` and verify infinite lives.
3. **HUD Tests**:
   - Fill gauge to 100 -> Verify pulsing animation.
   - Lose life -> Verify icon disables right-to-left.
