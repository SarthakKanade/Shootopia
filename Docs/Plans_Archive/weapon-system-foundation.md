# Project Overview
- Game Title: Sharp Shooter (Velocity Wars)
- High-Level Concept: Fast-paced arena FPS with high mobility and a momentum-based ability system.
- Players: Single player.
- Render Pipeline: URP (Unity 6).
- Namespace: VelocityWars.

# Game Mechanics
## Core Gameplay Loop
The player fights robotic enemies to build a Velocity Gauge, which unlocks powerful abilities. Weapons are temporary and obtained from spawn nodes, requiring constant movement and resource management.

## Controls and Input Methods
- **WASD / Mouse**: Movement and Aiming (New Input System).
- **Left Click**: Fire weapon (handles Auto, Semi, Charged, Continuous, and Traps).
- **Right Click**: ADS (Aim Down Sights).
- **Abilities**: Triggered when the Velocity Gauge is full.

# Key Asset & Context
- **WeaponSO.cs**: ScriptableObject defining weapon stats and behaviors.
- **ActiveWeapon.cs**: Player component managing weapon state, input, and durability.
- **BaseProjectile.cs**: Abstract base for all projectile-based weapons.
- **BaseTrap.cs**: Abstract base for all trap-based weapons.
- **IDamageable.cs**: Interface for health and combat logic.

# Implementation Steps

## 1. Extend WeaponSO.cs
- Wrap in `namespace VelocityWars`.
- Add the requested fields under specific `[Header]` groupings.
- Ensure all existing fields are preserved.

## 2. Create Base Classes
- **Assets/Scripts/Weapons/BaseProjectile.cs**:
    - Abstract class inheriting from `MonoBehaviour`.
    - Methods: `Init(WeaponSO data, GameObject owner)`.
- **Assets/Scripts/Weapons/BaseTrap.cs**:
    - Abstract class inheriting from `MonoBehaviour`.
    - Methods: `SetOwner(GameObject owner)`.

## 3. Refactor ActiveWeapon.cs
- Wrap in `namespace VelocityWars`.
- Add `[SerializeField] Transform muzzlePoint;`.
- Add private state fields:
    - `_isCharging`, `_chargeTimer`, `_isADSActive`, `_fuelRemaining`, `_flameRoutine`, `_adsHeld`.
    - `_lastShoot`, `_lastZoom` (for transition detection).
    - `_originalMoveSpeed`, `_originalSprintSpeed`.
- Modify `Awake()`:
    - Capture `_originalMoveSpeed` and `_originalSprintSpeed` from `firstPersonController`.
- Modify `Update()`:
    - Call `HandleADS()` (replacing/refactoring `HandleZoom`).
    - Update `HandleShoot()` to implement the new state machine logic for:
        - Charged Firing.
        - Auto Firing.
        - Trap Throwing.
        - Continuous (Flame) Stream.
        - Standard Semi-Auto (with ADS check).
- Implement Helper Methods:
    - `EnterADS()` / `ExitADS()`: Handle FOV and move speed changes.
    - `FireProjectile()`: Instantiate projectile and decrement durability.
    - `ThrowTrap()`: Instantiate trap with physics force and decrement durability.
    - `FlameRoutine()`: Coroutine for continuous damage and fuel depletion.
    - `CanFire()`: Centralized durability check.

# Verification & Testing
- **Compilation**: Ensure the project compiles without errors.
- **WeaponSO**: Verify new fields appear in the Inspector for `WeaponSO` assets.
- **Input Transitions**: Test hold/release logic for Charged and Flame weapons.
- **ADS**: Verify FOV changes and movement speed penalty when holding Right Click.
- **Traps**: Verify traps are thrown with the correct angle and force.
- **Projectiles**: Verify projectiles are instantiated at the muzzle point and initialized correctly.
