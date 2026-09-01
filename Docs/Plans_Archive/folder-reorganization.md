# Project Overview
- Game Title: Velocity Wars
- High-Level Concept: Fast-paced URP FPS project with a clean architecture and modular systems.
- Players: Single player
- Inspiration / Reference Games: Doom, Quake, Ultrakill
- Tone / Art Direction: High-speed, stylized combat
- Target Platform: PC (StandaloneOSX)
- Render Pipeline: URP

# Game Mechanics
## Core Gameplay Loop
The player explores levels, engages in high-speed combat with enemies using various weapons, and collects pickups (ammo, weapons) to survive and progress.
## Controls and Input Methods
Standard FPS controls (WASD, Mouse) using the New Input System and Legacy Input Manager.

# UI
- HUD: Health display, Ammo count, Active weapon indicator.
- Menus: Start, Pause, Game Over, Victory.

# Key Asset & Context
### Scripts to Move:
- `Assets/Scripts/Player/Weapon.cs`
- `Assets/Scripts/Player/WeaponSO.cs`

### Data Assets to Move:
- `Assets/Scriptable Objects/Machine Gun.asset`
- `Assets/Scriptable Objects/Sniper Rifle.asset`
- `Assets/Scriptable Objects/Pistol.asset`

### Target Folder Structure:
- `Assets/Scripts/Weapons/`
- `Assets/Data/Weapons/`

# Implementation Steps
1. **Create Target Folders**
   - Create `Assets/Scripts/Weapons` folder using `AssetDatabase.CreateFolder`.
   - Create `Assets/Data/Weapons` folder using `AssetDatabase.CreateFolder`.
   - *Dependency: None*

2. **Move Weapon Scripts**
   - Move `Assets/Scripts/Player/Weapon.cs` to `Assets/Scripts/Weapons/Weapon.cs` using `AssetDatabase.MoveAsset`.
   - Move `Assets/Scripts/Player/WeaponSO.cs` to `Assets/Scripts/Weapons/WeaponSO.cs` using `AssetDatabase.MoveAsset`.
   - *Dependency: Step 1*

3. **Move Weapon Data Assets**
   - Move `Assets/Scriptable Objects/Machine Gun.asset` to `Assets/Data/Weapons/Machine Gun.asset` using `AssetDatabase.MoveAsset`.
   - Move `Assets/Scriptable Objects/Sniper Rifle.asset` to `Assets/Data/Weapons/Sniper Rifle.asset` using `AssetDatabase.MoveAsset`.
   - Move `Assets/Scriptable Objects/Pistol.asset` to `Assets/Data/Weapons/Pistol.asset` using `AssetDatabase.MoveAsset`.
   - *Dependency: Step 1*

4. **Cleanup**
   - Delete empty `Assets/Scriptable Objects` folder if it's empty.
   - *Dependency: Step 3*

# Verification & Testing
1. **Compilation Check**: Confirm that the project compiles with zero errors in the Console.
2. **Missing Script Check**: Open `Assets/Scenes/Main.unity` and `Assets/Scenes/MainLevel.unity`. Inspect GameObjects (Player, Enemies, etc.) to ensure no components show "Missing (Mono Script)".
3. **Prefab Audit**: Open all prefabs in `Assets/Prefabs/` (if any) and verify script references are intact.
4. **Data Reference Check**: Select the moved `.asset` files and verify they still point to the correct `WeaponSO` script.
5. **Play Mode Test**: Enter Play mode and confirm:
   - Player can shoot weapons.
   - Weapons can be picked up.
   - Damage system (enemies/player) works correctly.
   - UI reflects weapon/health state correctly.
