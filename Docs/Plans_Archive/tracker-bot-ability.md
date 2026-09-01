# Project Overview
- Game Title: Sharp Shooter (Velocity Wars)
- High-Level Concept: Fast-paced arena FPS with a "Velocity Gauge" mechanic that rewards aggressive play and grants powerful abilities.
- Players: Single player (battling robotic waves).
- Inspiration / Reference Games: Doom Eternal, Ultrakill.
- Tone / Art Direction: Sci-fi, robotic, neon/orange accents.
- Target Platform: PC / MacOS.
- Screen Orientation / Resolution: Landscape (1920x1080).
- Render Pipeline: URP.

# Game Mechanics
## Core Gameplay Loop
The player kills enemies to fill the Velocity Gauge. At 100%, an ability can be activated. The Tracker Bot ability automates the retrieval of weapon crates from nodes, allowing the player to stay in the fight without manual scavenging.

## Controls and Input Methods
- 'F': Activate selected ability (when gauge is at 100%).
- Movement: WASD + Mouse for looking/aiming.

# UI
- Velocity Gauge HUD (already exists).
- Tracker Bot does not have a specific UI, but is visually distinct (orange emissive).

# Key Asset & Context
- **TrackerBotAbilitySO.cs**: ScriptableObject defining the ability's spawn logic.
- **TrackerBotController.cs**: New script managing bot AI, movement, and lifespan.
- **WeaponSpawnNode.cs**: Modified to allow bot interaction and manual cooldown triggering.
- **WeaponSpawnManager.cs**: Modified to expose nodes for the bot to scan.
- **TrackerBot.prefab**: New prefab for the bot.

# Implementation Steps

## 1. Modify Core Weapon Systems
- **WeaponSpawnNode.cs**:
    - Expose `_spawnedCrate` via a public property.
    - Add `OnCrateDelivered()` to destroy the crate and start the cooldown routine.
- **WeaponSpawnManager.cs**:
    - Expose `_allNodes` via `GetAllNodes()`.

## 2. Implement Tracker Bot Controller
- **TrackerBotController.cs**:
    - Implement `IDamageable` and `IKillable` from `VelocityWars.Core`.
    - Implement logic to find the nearest active node, navigate to it, and "slide" the crate to the player.
    - Handle 15s lifespan and health (50 HP).

## 3. Update Ability Asset logic
- **TrackerBotAbilitySO.cs**:
    - Update `Activate()` to spawn the bot on the NavMesh and initialize it.
    - Set `Deactivate()` as a no-op.

## 4. Create Tracker Bot Prefab
- Create `Assets/Prefabs/TrackerBot.prefab`.
- Add `NavMeshAgent`.
- Add `TrackerBotController`.
- Create a visual representation (Capsule with Orange emissive material).
- Set Tag to "AllyBot" (new) and ensure robots do not target this tag or its layer.

# Verification & Testing
1. **Basic Spawn**: Activate ability and verify bot spawns 2m in front on NavMesh.
2. **Navigation**: Verify bot moves to nearest active node.
3. **Delivery**: Verify crate lerps toward player's current position over 2s.
4. **Node State**: Verify node enters cooldown after delivery.
5. **Lifespan**: Verify bot self-destructs after 15s.
6. **Health**: Verify bot can be destroyed by taking 50 damage.
7. **Enemy Target**: Verify robots ignore the bot.
8. **No Nodes**: Verify bot idles safely when no nodes are active.
