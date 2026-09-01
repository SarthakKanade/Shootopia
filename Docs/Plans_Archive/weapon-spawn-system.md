# Project Overview
- Game Title: Velocity Wars
- High-Level Concept: High-speed shooter with dynamic weapon spawning and resource management.
- Players: Single player (based on FirstPersonController and Cinemachine setup).
- Tone / Art Direction: Likely stylized/low-poly (referenced KayKit and URP).
- Target Platform: Standalone (PC/Mac).
- Screen Orientation / Resolution: Landscape.
- Render Pipeline: URP.

# Game Mechanics
## Core Gameplay Loop
Players engage in combat using various weapons. Weapons have durability and are found in crates that spawn periodically at specific nodes. Players must move between nodes to stay armed.
## Controls and Input Methods
- WASD + Mouse for movement/looking.
- Mouse Left Click to shoot.
- Interaction with crates is trigger-based (proximity).

# UI
- Ammo display (already implemented in `ActiveWeapon`).
- Glow colors on crates to indicate weapon category (Control=cyan, Burst=red, Precision=yellow, Chaos=purple, Default=white).

# Key Asset & Context
- `WeaponSpawnNode.cs`: Manages individual spawn point states.
- `WeaponSpawnManager.cs`: Orchestrates global spawning and round starts.
- `WeaponCratePool.cs`: ScriptableObject for weighted weapon selection.
- `WeaponCrate.prefab`: Visual representation of a weapon pickup.
- `WeaponCrateTrigger.cs`: Interaction logic.

# Implementation Steps

## Day 1: Foundation & Data
1. **WeaponCratePool ScriptableObject** in `Assets/Scripts/Weapons/WeaponCratePool.cs`.
    - Implement `WeaponSpawnEntry` class.
    - Implement `GetRandom()` with weighted selection logic.
2. **Create Asset** `Assets/Data/Weapons/DefaultCratePool.asset`.
    - Populate with Pistol (w=35), Machine Gun (w=25), Sniper Rifle (w=15).
3. **WeaponSpawnNode Structure** in `Assets/Scripts/Weapons/WeaponSpawnNode.cs`.
    - Implement `NodeState` enum.
    - Implement `Activate`, `Deactivate`, and state properties.
    - Add `OnDrawGizmos` for state visualization.
    - **Verification**: Drag nodes into scene, verify gizmo colors change when calling `Activate` via debug menu (if applicable) or Manager log.

## Day 2: Visuals & Interaction
1. **WeaponCrate Visuals & Prefab** in `Assets/Prefabs/WeaponCrate.prefab`.
    - Create a Cube-based crate (until KayKit assets found).
    - Add `BoxCollider` (isTrigger=true).
    - Add a `Point Light` for the glow.
    - Create `WeaponCrateTrigger.cs` to handle `OnTriggerEnter`.
2. **Spawn Animation in WeaponSpawnNode**.
    - Implement `SpawnRoutine` coroutine.
    - Lerp the crate position from +4 units height to ground level over 0.6s.
    - Set the `Point Light` color based on `WeaponSO.category`.
3. **Equip Logic**.
    - Wire `WeaponCrateTrigger` to call `ActiveWeapon.SwitchWeapon`.
    - Call `WeaponSpawnNode.OnCratePickedUp` to transition to Cooldown.
4. **Verification**: Manually activate a node in the inspector, walk over the crate, verify weapon switches and crate disappears.

## Day 3: Global Management
1. **WeaponSpawnManager** in `Assets/Scripts/Weapons/WeaponSpawnManager.cs`.
    - Implement `Start()` logic to activate a subset (6 of 12).
    - Implement `MidRoundActivationRoutine` to activate inactive nodes every 30s.
    - Fisher-Yates shuffle for random node selection.
2. **Scene Integration**.
    - Place 12 `WeaponSpawnNode` objects in the scene.
    - Assign the `WeaponSpawnManager`.
3. **Cleanup**.
    - Remove or mark `WeaponPickup.cs` as obsolete if it conflicts with the new system.
4. **Final Verification**: Enter Play mode, verify 6 crates drop, pick them up, verify cooldown and respawn after 20s.

# Verification & Testing
- **Unit Test (Manual)**: Verify `GetRandom()` distribution over 100 iterations (log results).
- **Edge Case**: Picking up a weapon exactly as it's spawning.
- **Visual Check**: Glow colors match weapon categories.
- **Timing Check**: Respawn timer is exactly 20s as specified.
