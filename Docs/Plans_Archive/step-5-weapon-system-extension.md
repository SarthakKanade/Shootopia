# Project Overview
- Game Title: Velocity Wars (FPS)
- Step 5: Extend `WeaponSO` with categories and durability types to support advanced weapon mechanics in future phases.

# Game Mechanics
## Core Gameplay Loop
Weapons will have categories (Control, Burst, Precision, Chaos, Trap) and different durability mechanics (Ammo, Time, Uses).
## Controls and Input Methods
No functional changes to input or shooting in this step.

# UI
New fields will be visible in the Inspector for balancing and design.

# Key Asset & Context
- `Assets/Scripts/Player/WeaponSO.cs`: Script to be modified with new enums and fields.
- `Assets/Scripts/Player/ActiveWeapon.cs`: Script to receive a TODO comment for future durability handling.
- Weapon Assets: `Pistol`, `Machine Gun`, `Sniper Rifle`.

# Implementation Steps
1. **Modify WeaponSO.cs**:
    - Add `WeaponCategory` and `DurabilityType` enums above the class.
    - Add new fields under a `[Header("Velocity Wars — weapon identity")]`.
2. **Modify ActiveWeapon.cs**:
    - Add a TODO comment block in `HandleShoot` regarding Phase 2 durability handling.
3. **Update Weapon Assets**:
    - Assign categories to existing assets:
        - Pistol: `WeaponCategory.Control`
        - Machine Gun: `WeaponCategory.Burst`
        - Sniper Rifle: `WeaponCategory.Precision`
    - Set all to `DurabilityType.Ammo`.
4. **Verification**:
    - Ensure project compiles without errors.
    - Confirm existing weapon behavior is unchanged.

# Verification & Testing
1. **Compilation Check**: Use `GetConsoleLogs` to ensure no errors.
2. **Asset Inspection**: Verify the new fields appear and are populated correctly in the Inspector.
