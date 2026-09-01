# Project Overview
- Game Title: Velocity Wars
- High-Level Concept: URP FPS project.
- Players: Single player (currently), expanding to Multiplayer later.
- Target Platform: PC (StandaloneOSX).
- Render Pipeline: URP.

# Game Mechanics
## Core Gameplay Loop
The player uses weapons (hitscan or projectile) to damage entities that implement `IDamageable`. Currently, `EnemyHealth` implements `IDamageable`.

## Controls and Input Methods
- Shoot: Handled by `Weapon.Shoot` (hitscan) or `Projectile.OnTriggerEnter` (projectile).

# Key Asset & Context
- `Assets/Scripts/Player/Weapon.cs`: Handles hitscan shooting.
- `Assets/Scripts/Enemies/Projectile.cs`: Handles projectile damage.
- `Assets/Scripts/Core/Interfaces/IDamageable.cs`: The interface being implemented.
- `Assets/Scripts/Enemies/EnemyHealth.cs`: Already implements `IDamageable`.

# Implementation Steps
1. **Modify `Assets/Scripts/Player/Weapon.cs`**:
    - Add `using VelocityWars.Core;`.
    - Replace the `GetComponentInParent<EnemyHealth>` call and null check with `TryGetComponent<IDamageable>`.
    - Pass `DamageType.Bullet` as the second argument to `TakeDamage`.
2. **Modify `Assets/Scripts/Enemies/Projectile.cs`**:
    - Add `using VelocityWars.Core;`.
    - Replace the `GetComponent<PlayerHealth>` call and null check with `TryGetComponent<IDamageable>`.
    - Pass `DamageType.Bullet` as the second argument to `TakeDamage`.
    - *Note: This will temporarily stop the player from taking damage until `PlayerHealth` implements `IDamageable`, as per the PRD's instruction.*

# Verification & Testing
1. **Compile Verification**: Ensure no compiler errors.
2. **Robot Damage Test**: In Editor Play mode, shoot a robot and confirm it takes damage and dies.
3. **Environment Test**: Shoot a wall/object without `IDamageable` and confirm no errors occur in the console.
4. **Projectile Test (Optional)**: Confirm enemy projectiles hit objects without error (though player won't take damage yet).
