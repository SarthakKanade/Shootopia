# Project Overview
- Game Title: Velocity Wars (FPS)
- Step 6: Refactor damage application logic to use the `IDamageable` interface and `TryGetComponent`.

# Game Mechanics
## Core Gameplay Loop
Decouple weapon damage logic from specific classes like `EnemyHealth`. This allows weapons to damage anything that implements `IDamageable`.

# UI
N/A

# Key Asset & Context
- `Assets/Scripts/Player/Weapon.cs`: Raycast shooting logic.
- `Assets/Scripts/Enemies/Projectile.cs`: Projectile-based damage logic.
- `IDamageable`: The interface used to receive damage.

# Implementation Steps
1. **Modify Weapon.cs**:
    - Replace `GetComponentInParent<EnemyHealth>` with `TryGetComponent<IDamageable>` on the collider.
    - Call `TakeDamage` with `weaponSO.Damage` and `DamageType.Bullet`.
2. **Modify Projectile.cs**:
    - Replace the explicit `PlayerHealth` check with `TryGetComponent<IDamageable>`.
    - Call `TakeDamage` with `damage` and `DamageType.Bullet`.
    - *Note*: Per instructions, `PlayerHealth` will not be updated to implement `IDamageable` yet, so turrets may temporarily stop damaging the player until that step.
3. **Verification**:
    - Ensure project compiles.
    - Verify that shooting enemies (Robots/Turrets) still works.

# Verification & Testing
1. **Compilation Check**: Use `GetConsoleLogs` to ensure zero errors.
2. **Logic Check**: Confirm that `TryGetComponent` is used and `DamageType.Bullet` is passed.
