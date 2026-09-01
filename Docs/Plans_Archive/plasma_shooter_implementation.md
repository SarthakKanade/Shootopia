# Project Overview
- Game Title: Velocity Wars (Sharp Shooter)
- High-Level Concept: Fast-paced arena shooter with momentum-based mechanics.
- Players: Single player.
- Render Pipeline: URP.
- Target Platform: PC.

# Game Mechanics
## Core Gameplay Loop
Maintain momentum by killing enemies, managing weapon durability, and using abilities when the gauge is full.
## Controls and Input Methods
New Input System (StarterAssets). Auto-fire weapon behavior when the trigger is held.

# UI
Active weapon ammo/durability display (UGUI).

# Key Asset & Context
- `ActiveWeapon.cs`: Manages weapon firing and lifecycle.
- `WeaponSO.cs`: Data container for weapon stats.
- `BaseProjectile.cs`: Base class for projectile logic.
- `Cosmic_Retro_Blaster_3_4`: Model to be used for the new weapon.

# Implementation Steps
## 1. Modify Damage Types
- **File**: `Assets/Scripts/Core/Interfaces/IDamageable.cs`
- **Change**: Add `Bullet` to the `DamageType` enum to support the new projectile's damage classification.

## 2. Create Plasma Projectile Controller
- **File**: `Assets/Scripts/Weapons/PlasmaProjectileController.cs`
- **Logic**: 
  - Inherit from `BaseProjectile`.
  - In `Awake`, set `Rigidbody.linearVelocity` to `transform.forward * 28f`.
  - In `OnTriggerEnter`, check for `IDamageable`. If found, apply 18 damage with `DamageType.Bullet`.
  - Instantiate `hitSparkVFX` on hit.
  - Destroy projectile on collision or after a 4s safety fallback.

## 3. Create Projectile & VFX Assets
- **Material**: `Assets/Materials/PlasmaProjectile_Mat.mat` (URP Unlit, Cyan #88FFFF, Emission intensity 2.5, Additive blending).
- **Prefab**: `Assets/Prefabs/Weapons/PlasmaProjectile.prefab` (Sphere mesh 0.05m radius, Rigidbody no gravity, SphereCollider trigger, `PlasmaProjectileController`).
- **Prefab**: `Assets/VFX/PlasmaHitSpark.prefab` (Particle System: 8 cyan particles, burst, start speed 2-4, size 0.03-0.07, lifetime 0.1s, Stop Action: Destroy).

## 4. Create Base Shooter Weapon Prefab
- **Prefab**: `Assets/Prefabs/Weapons/BaseShooter.prefab`
- **Components**: 
  - `Cosmic_Retro_Blaster_3_4` model as child.
  - `Weapon` script.
  - Add a `MuzzlePoint` child transform at the blaster's barrel tip.

## 5. Create WeaponSO Asset
- **Path**: `Assets/Data/Weapons/BaseShooter.asset`
- **Settings**:
  - `Damage`: 18
  - `FireRate`: 0.15
  - `projectileSpeed`: 28
  - `maxUses`: 20
  - `durabilityType`: Uses
  - `gaugeKillFill`: 20
  - `isAutoFire`: true
  - `projectilePrefab`: `PlasmaProjectile.prefab`
  - `weaponPrefab`: `BaseShooter.prefab`

## 6. Setup Scene Muzzle Point
- **Action**: Ensure the `muzzlePoint` field on the `Active Weapon` object in `MainLevel` is assigned. If no suitable transform exists, create a "MuzzlePoint" child on `Active Weapon` and assign it.

# Verification & Testing
1. **Manual Check**: Equip the "Base Shooter" (e.g., by setting it as `startingWeapon` in `ActiveWeapon`).
2. **Firing Test**: Hold the fire button. Verify cyan bolts fire every 0.15s.
3. **Durability Test**: Verify the weapon breaks/vanishes after exactly 20 shots.
4. **Combat Test**: Verify hitting a robot deals damage and spawns the cyan hit spark.
