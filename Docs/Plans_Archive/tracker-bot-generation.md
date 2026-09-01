# Project Overview
- Game Title: Sharp Shooter (Velocity Wars)
- High-Level Concept: Fast-paced arena FPS where players manage weapon durability and build momentum.
- Players: Single player.
- Inspiration: DOOM, Quake, Ultrakill.
- Tone / Art Direction: Low-poly stylized, sci-fi, high-contrast.
- Target Platform: PC (macOS/Windows).
- Screen Orientation / Resolution: Landscape.
- Render Pipeline: URP.

# Game Mechanics
## Core Gameplay Loop
- Fight waves of robots, maintain kill streaks, build the Velocity Gauge, and activate special abilities like the Tracker Bot to retrieve weapons.

## Controls and Input Methods
- WASD + Mouse for movement and shooting.
- Action-based inputs for abilities.

# UI
- Tracker Bot will feature a world-space HP bar that appears when damaged.
- High-contrast orange emission for visibility.

# Key Asset & Context
- `Assets/Scripts/Abilities/TrackerBotController.cs`: Existing script to be updated.
- `Assets/Prefabs/TrackerBot.prefab`: Target prefab for the new bot.
- `Assets/UI/TrackerBotHPBar.prefab`: New world-space health bar.
- `Assets/Models/TrackerBot.fbx`: To be generated.
- `Assets/Animations/TrackerBot/TrackerBot.controller`: New animator controller.

# Implementation Steps
## 1. 3D Asset Generation
- Generate `TrackerBot.fbx` using the provided detailed prompt (Small, squat robot, burnt copper hull, glowing orange visor, Idle/Walk/Death animations).
- **Dependency**: None.

## 2. HP Bar Prefab Creation
- Create `Assets/UI/TrackerBotHPBar.prefab`.
- Canvas (World Space, Scale 0.01).
- Image (Horizontal Fill, Color #FF6B00).
- Positioned 2m above the bot.
- **Dependency**: None.

## 3. Update TrackerBotController Script
- Modify `TrackerBotController.cs` to include:
    - `[SerializeField] private Animator _animator;`
    - `[SerializeField] private GameObject _hpBarContainer;` (the Canvas)
    - `[SerializeField] private UnityEngine.UI.Image _hpBarFill;`
    - `[SerializeField] private MeshRenderer _visorRenderer;`
    - `[SerializeField] private Light _visorLight;`
- Implement health bar update in `TakeDamage`.
- Implement visor flicker (coroutine pulsing emission color).
- Set Animator parameters based on movement speed.
- Trigger "Death" animation and delay destruction in `OnDeath`.
- **Dependency**: Step 1 & 2.

## 4. Animation Setup
- Create `Assets/Animations/TrackerBot/TrackerBot.controller`.
- Add states: `Idle`, `Walk`, `Death`.
- Setup transitions (e.g., `isMoving` bool or `Speed` float).
- **Dependency**: Step 1.

## 5. Prefab Assembly
- Update `Assets/Prefabs/TrackerBot.prefab`:
    - Swap placeholder mesh with the new `TrackerBot.fbx`.
    - Add `BotVisorLight` (Point Light).
    - Add `TrackerBotHPBar` as a child.
    - Connect all serialised fields in `TrackerBotController`.
- **Dependency**: Steps 1, 2, 3, 4.

# Verification & Testing
- **Visual Check**: Inspect the bot in the scene to ensure materials (burnt copper and glowing visor) and silhouette match the prompt.
- **Animation Check**: Play animations in the Inspector to verify they cycle correctly.
- **Functionality Test**: Spawn the bot via the Ability in `MainLevel`. Verify it moves to crates, the HP bar appears and depletes when shot, and it plays the Death animation when health reaches zero.
- **Visibility Test**: Check readability from 15-20m away in the dark arena.