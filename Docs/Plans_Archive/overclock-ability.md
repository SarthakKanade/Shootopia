# Project Overview
- Game Title: Sharp Shooter (Velocity Wars)
- High-Level Concept: Fast-paced first-person shooter with a momentum-based "Velocity Gauge" that unlocks powerful abilities.
- Players: Single player.
- Target Platform: PC (StandaloneOSX in settings).
- Render Pipeline: URP.

# Game Mechanics
## Core Gameplay Loop
The player kills enemies to fill the Velocity Gauge. At 100%, they can activate an ability like "Overclock" to gain a combat advantage.
## Controls and Input Methods
- Shoot: Mouse Button 1 / Input Action.
- Activate Ability: 'F' key.

# UI
- Velocity Gauge HUD: Shows current charge and streak.
- Weapon HUD: Shows current ammo/durability. During Overclock, this should show 999.

# Key Asset & Context
- `Assets/Scripts/Player/ActiveWeapon.cs`: Handles weapon shooting logic and durability tracking.
- `Assets/Scripts/Abilities/OverclockAbilitySO.cs`: Logic for the Overclock ability.
- `Assets/Data/Abilities/Overclock.asset`: Configuration for the Overclock ability.

# Implementation Steps
## 1. Modify ActiveWeapon.cs
- Add `public void SetCurrentUses(int uses)` to allow external systems to override weapon durability.
- Verify `FireRateMultiplier` application in `HandleShoot`.
- Dependency: None.

## 2. Update OverclockAbilitySO.cs
- Replace `Activate` and `Deactivate` methods to match PRD requirements.
- Use `SetCurrentUses` to set durability to 999 on activation and 1 on deactivation.
- Dependency: Step 1.

## 3. Update Overclock.asset
- Change `duration` to 8.
- Verify `fireRateMultiplier` is 1.5 and `tempUses` is 999.
- Dependency: None.

# Verification & Testing
- **Activation Test**: Fill gauge, press F. Check if firing is faster and HUD shows 999.
- **Timer Test**: Ensure ability lasts exactly 8 seconds.
- **Deactivation Test**: After 8s, HUD should show 1 use remaining. Firing once should expire the weapon.
- **Null Case Test**: Activate ability with no weapon equipped. Ensure no crash and gauge resets.
