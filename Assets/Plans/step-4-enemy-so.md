# Project Overview
- Game Title: Velocity Wars (FPS)
- Step 4: Refactor enemy stats into `EnemySO` data assets and update health/combat scripts to use them.

# Game Mechanics
## Core Gameplay Loop
Decouple enemy stats (health, speed, damage) from behavior scripts to allow for easier tuning and addition of new enemy types via ScriptableObjects.
## Controls and Input Methods
No changes to input.

# UI
No changes to UI.

# Key Asset & Context
- `Assets/Scripts/Enemies/EnemySO.cs`: Data container for enemy statistics and VFX references.
- `Assets/Data/Enemies/BasicRobot.asset`: Config for the basic robot enemy.
- `Assets/Data/Enemies/Turret.asset`: Config for the turret enemy.
- `Assets/Scripts/Enemies/EnemyHealth.cs`: Updated to implement `IDamageable` and `IKillable`.
- `Assets/Scripts/Enemies/Robot.cs`: Updated to use `EnemySO` for movement and detection.
- `Assets/Scripts/Enemies/Turret.cs`: Updated to use `EnemySO` for fire rate and damage.

# Implementation Steps
1. **Create Directory**: `Assets/Data/Enemies/` if it doesn't exist.
2. **Implement EnemySO.cs**: Define the `EnemySO` class in `Assets/Scripts/Enemies/EnemySO.cs` with the specified fields and namespace `VelocityWars.Enemies`.
3. **Create SO Assets**:
    - `BasicRobot.asset`: Health=3 (as in current code), Speed=3.5 (NavMesh default), Damage=20 (prompt default), etc.
    - `Turret.asset`: Health=3 (as in current code), FireRate=2.0 (as in current code), Damage=2 (as in current code).
4. **Refactor EnemyHealth.cs**:
    - Implement `IDamageable`, `IKillable`.
    - Add `EnemySO` reference.
    - Remove hardcoded fields.
    - Update `TakeDamage` and `OnDeath` as per PRD.
5. **Refactor Robot.cs**:
    - Add `EnemySO` reference.
    - Update `Update()` to use `_config.moveSpeed` (via `agent.speed`).
    - Note: Current `Robot.cs` doesn't have detection range logic, but I will add a placeholder check or use the SO field if I implement the detection range. The prompt says "Replace hardcoded detection range", suggesting it might be there or expected.
6. **Refactor Turret.cs**:
    - Add `EnemySO` reference.
    - Update `FireRoutine` to use `_config.attackCooldown` and `_config.damage`.

# Verification & Testing
1. **Compilation Check**: Ensure no errors.
2. **Behavioral Check**: Robots should still chase the player. Turrets should still fire. Enemies should still die and trigger the win condition.
3. **Prefab Wiring**: List prefabs that need the SO assigned.
    - `Assets/Prefabs/Enemies/Robot.prefab` -> Needs `BasicRobot.asset`.
    - `Assets/Prefabs/Enemies/Turret.prefab` -> Needs `Turret.asset`.

# Step-by-Step implementation
1. Create `EnemySO.cs`.
2. Create `BasicRobot.asset` and `Turret.asset` via script.
3. Modify `EnemyHealth.cs`.
4. Modify `Robot.cs`.
5. Modify `Turret.cs`.
6. Final verification.
