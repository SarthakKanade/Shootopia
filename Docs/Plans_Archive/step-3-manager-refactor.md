# Project Overview
- Game Title: Velocity Wars (FPS)
- Step 3: Refactor GameManager into a state machine and introduce UIManager to handle UI logic via SO events.

# Game Mechanics
## Core Gameplay Loop
GameManager tracks game states (Playing, Victory, GameOver) and enemy counts. It broadcasts state changes via SO events.
## Controls and Input Methods
N/A for this step.

# UI
UIManager replaces direct UI manipulation in GameManager. It listens to `OnEnemyCountChanged`, `OnVictory`, `OnGameOver`, and `OnPlayerHealthChanged` events to update the HUD and screens.

# Key Asset & Context
- `Assets/Scripts/Misc/GameManager.cs`: To be rewritten as a singleton state machine.
- `Assets/Scripts/Core/Managers/UIManager.cs`: New script to handle UI updates.
- SO Events: `OnEnemyCountChanged`, `OnVictory`, `OnGameOver`, `OnPlayerHealthChanged` (created in Step 2).

# Implementation Steps
1. **Rewrite GameManager.cs**:
    - Implement `GameState` enum.
    - Implement Singleton pattern.
    - Add Serialized fields for `GameEvent` and `FloatGameEvent`.
    - Implement `SetState(GameState)` logic.
    - Implement `EnemyKilled()` and `RegisterEnemy()` logic.
    - Remove all UI/TMP references.
2. **Create UIManager.cs**:
    - Add serialized fields for UI elements: `enemiesLeftText` (TMP_Text), `youWinText` (GameObject), `gameOverContainer` (GameObject), `shieldBars` (Image[]).
    - Implement methods: `ShowGameOver()`, `ShowVictory()`, `OnEnemyCountChanged(float)`, `OnPlayerHealthChanged(float)`.
3. **Verification**:
    - Ensure code compiles.
    - Validate logic (logic for `EnemyKilled` triggering `Victory` state).

# Scene Wiring Instructions
1. **UIManager Setup**:
    - Create an empty GameObject named `UIManager`.
    - Attach `UIManager` script.
    - Assign UI references (Canvas/Panel children).
    - Add `GameEventListener`:
        - Event: `OnGameOver.asset` -> Response: `UIManager.ShowGameOver`
        - Event: `OnVictory.asset` -> Response: `UIManager.ShowVictory`
    - Add `FloatGameEventListener`:
        - Event: `OnEnemyCountChanged.asset` -> Response: `UIManager.OnEnemyCountChanged`
2. **GameManager Setup**:
    - Assign SO events to `GameManager` inspector fields:
        - `_onGameStart` -> `OnGameStarted.asset` (Wait, was this created in Step 2? Let me check).
        - `_onGameOver` -> `OnGameOver.asset`
        - `_onVictory` -> `OnVictory.asset`
        - `_onEnemyCountChanged` -> `OnEnemyCountChanged.asset`

# Verification & Testing
1. **Compilation**: Zero errors.
2. **Testing**: 
    - Calling `EnemyKilled()` until `_remainingEnemies == 0` should trigger `SetState(GameState.Victory)`.
    - `SetState(GameState.Victory)` should call `_onVictory.Raise()`.
    - `UIManager` listening to `_onVictory` should activate `youWinText`.
