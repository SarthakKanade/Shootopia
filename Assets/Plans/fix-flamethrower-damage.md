# Project Overview
- Game Title: Sharp Shooter (Velocity Wars)
- High-Level Concept: Fast-paced FPS where the player manages weapon durability and momentum.
- Players: Single player.
- Render Pipeline: URP (PC_RPAsset).
- Screen Orientation: Landscape 1920x1080.
- Key Systems: Weapon System, Velocity Gauge, Status Effects (Frozen, Afterburn).

# Problem Analysis
The user reports that the Flamethrower (FlameGun) is not dealing damage to robots and has no burn effect.
The investigation revealed:
1. **Empty Interaction Layers**: The `FlameGun_Gun` prefab has its `interactionLayers` set to 0. This causes the `Physics.OverlapCapsule` used by the flamethrower to return no hits, meaning no damage is applied and no status effects (Afterburn) are triggered.
2. **Brittle Hit Detection**: `FlameGunWeapon.cs` uses `TryGetComponent<IDamageable>`, which fails if the flamethrower hits a child collider (common on robot models).
3. **Inaccurate Angle Check**: The angle check for the flame cone uses `c.transform.position` (the object pivot), which is often at the feet of the robot, causing hits to be rejected if the player is looking at the chest.
4. **Widespread Configuration Issue**: Several other weapon prefabs (`AreaBlaster`, `FreezeGun`, `SniperGun`, `VoidGun`) also have empty `interactionLayers`, likely causing similar issues.

# Implementation Plan

## Step 1: Fix Weapon Prefab Configuration
Modify the weapon prefabs to ensure they can interact with the "Enemy" layer (Layer 6).
- **Files to modify**: 
    - `Assets/Prefabs/Weapons/FlameGun_Gun.prefab`
    - `Assets/Prefabs/Weapons/AreaBlaster_Gun.prefab`
    - `Assets/Prefabs/Weapons/FreezeGun_Gun.prefab`
    - `Assets/Prefabs/Weapons/SniperGun.prefab`
    - `Assets/Prefabs/Weapons/VoidGun_Gun.prefab`
- **Action**: Set `interactionLayers` to include "Enemy" and "Default" (and any other relevant layers like "Environment" if projectiles should hit walls).

## Step 2: Enhance FlameGunWeapon Script Robustness
Update `FlameGunWeapon.cs` to handle complex enemy hierarchies and provide more accurate cone detection.
- **File**: `Assets/Scripts/Weapons/FlameGunWeapon.cs`
- **Changes**:
    - Replace `c.TryGetComponent(out IDamageable t)` with `c.GetComponentInParent<IDamageable>()`.
    - Use `c.bounds.center` instead of `c.transform.position` for the `Vector3.Angle` check to ensure the check is performed against the volume of the hit part, not the floor pivot.
    - Ensure `ApplyAfterburn` also uses `GetComponentInParent<Robot>()` for consistency.

## Step 3: Verification
- Equip the FlameGun and verify that Robots take damage (indicated by VFX or health reduction).
- Verify that Afterburn (burning VFX) appears on the Robot after it is flamed or when the player stops firing.
- Test other weapons (AreaBlaster, FreezeGun) to ensure they now correctly hit targets.

# Key Asset & Context
### FlameGunWeapon.cs Logic Improvement
```csharp
// Inside ProcessFlameTick loop
foreach (Collider c in hits)
{
    // Use bounds.center for better angle accuracy
    Vector3 toTgt = (c.bounds.center - start).normalized;
    if (Vector3.Angle(Camera.main.transform.forward, toTgt) > _so.coneAngle) continue;

    // Search parents for IDamageable
    IDamageable t = c.GetComponentInParent<IDamageable>();
    if (t != null)
    {
        float damagePerTick = (_so.damagePerSecond * _so.tickRate);
        t.TakeDamage(damagePerTick, DamageType.Environment, _so);
        thisFrame.Add(t);
    }
}
```

# Verification & Testing
1. **Manual Playtest**: Start `MainLevel`, pick up a FlameGun, and fire at a Robot.
2. **Log Verification**: Check console for double damage logs if a robot is frozen then flamed (verifying `EnemyHealth` interaction).
3. **Hierarchy Check**: Verify that hitting the "Sphere Glow" child object of the Robot still applies damage to the Robot.
