# Project Overview
- Game Title: Velocity Wars
- High-Level Concept: Fast-paced arena shooter with weapon pickups.
- Players: Single player / AI
- Render Pipeline: URP
- Target Platform: StandaloneOSX
- Screen Orientation: Landscape

# Game Mechanics
## Core Gameplay Loop
Combat and exploration in a tactical arena. Weapon nodes provide upgrades/resources.
## Controls and Input Methods
Standard FPS controls (New Input System and Legacy supported).

# UI
- Scene View: Yellow gizmo spheres (0.8m radius) with vertical lines (2m) indicate weapon spawn points.

# Key Asset & Context
- **Script**: `Assets/Scripts/Weapons/WeaponSpawnNode.cs`
- **Scene**: `Assets/Scenes/MainLevel.unity`
- **Parent Object**: `WeaponSpawnNodes`
- **Child Objects**: `SpawnNode_01` to `SpawnNode_12`

# Implementation Steps
1. **Create WeaponSpawnNode Script**:
    - File: `Assets/Scripts/Weapons/WeaponSpawnNode.cs`
    - Content: Simple class with `OnDrawGizmos` to draw yellow wire spheres and lines.
    - Dependency: None.
2. **Setup Node Hierarchy in MainLevel**:
    - Open `Assets/Scenes/MainLevel.unity`.
    - Create an empty GameObject named `WeaponSpawnNodes` at (0,0,0).
    - Create 12 empty children named `SpawnNode_01` through `SpawnNode_12`.
    - Add the `WeaponSpawnNode` component to each child.
    - Dependency: Step 1.
3. **Position Nodes**:
    - Set positions according to the strategic layout:
        - **Centre Hub** (3 nodes): (3, 0.5, 3), (-3, 0.5, 3), (0, 0.5, -4)
        - **Side Corridor Left** (2 nodes): (-18, 0.5, 12), (-18, 0.5, -5)
        - **Side Corridor Right** (2 nodes): (18, 0.5, 12), (18, 0.5, -5)
        - **Elevated Section** (2 nodes): (16.5, 7.5, -10) [Catwalk], (14, 0.5, -12) [Ramp Base]
        - **Transition Areas** (2 nodes): (0, 0.5, 18), (0, 0.5, -18)
        - **Hard-to-reach Bonus** (1 node): (-21, 3, 20) [Elevated Cube]
    - Dependency: Step 2.

# Verification & Testing
- **Visual Check**: Open Scene view and verify 12 yellow spheres are visible.
- **Gizmo Check**: Ensure spheres have a 0.8m radius and a 2m vertical line.
- **Positioning Check**: Ensure no two nodes are closer than 5m.
- **NavMesh Check**: Hover over node positions in Scene view with NavMesh visualization on to ensure blue tint (reachability).
- **Strategic Review**: Check if nodes are visible from movement paths and not in dead-ends.
