# Project Overview
- Game Title: Sharp Shooter (Velocity Wars)
- High-Level Concept: High-speed first-person shooter where movement charges a special ability gauge.
- Players: Single player.
- Render Pipeline: URP (PC_RPAsset).
- Input System: Both New and Legacy.

# Game Mechanics
## Core Gameplay Loop
- Move fast to charge the Velocity Gauge.
- Shoot enemies using various weapons with different durability types (Ammo, Uses, Time).
- Manage health (Shield Bars).
- Defeat all enemies to win.

## UI Issues
- Only the "Bullets" (Ammo Text) is working.
- Velocity Gauge is not updating (likely due to high movement threshold and indirect event connection).
- Enemies Left count is not updating (missing event assignment).
- Shield Bars (Health) might not be initializing correctly or visually bugged (icon deactivation).
- Durability Bar/Text is missing connections and scene references.

# Key Asset & Context
- `Assets/Scripts/Core/Managers/UIManager.cs`: Handles most HUD elements.
- `Assets/Scripts/Player/VelocityGaugeManager.cs`: Handles gauge charging logic.
- `Assets/Scripts/Player/VelocityGaugeUI.cs`: Updates the gauge visual.
- `Assets/Scripts/Misc/GameManager.cs`: Tracks enemy count.
- `Assets/Scripts/Player/ActiveWeapon.cs`: Handles weapon shooting and durability.
- `Assets/Data/Events/`: Contains the `FloatGameEvent` assets.

# Implementation Steps
## 1. Fix Event Assignments (Scene/Prefab changes)
- Assign `OnEnemyCountChanged.asset` to `GameManager`.
- Assign `OnDurabilityChanged.asset` and `OnMaxDurabilityChanged.asset` to `ActiveWeapon` on the Player.
- Assign `OnGaugeChanged.asset` to `VelocityGaugeManager` on the Player (Verify existing assignment).
- Assign `OnPlayerHealthChanged.asset` to `PlayerHealth` on the Player (Verify existing assignment).

## 2. Fix UIManager Logic and References
- Modify `UIManager.OnPlayerHealthChanged` to avoid deactivating the Shield Icon (last element in the array) if it's meant to be persistent.
- Assign `_durabilityBar` and `_durabilityText` references in the `UIManager` if corresponding UI elements can be found or created. (Note: Currently missing from hierarchy, will check if they should be added to the Ammo Container).
- Add `FloatGameEventListener` components to the `HUD Manager` for `OnDurabilityChanged` and `OnMaxDurabilityChanged` and link them to `UIManager` methods.

## 3. Improve Velocity Gauge Responsiveness
- Modify `VelocityGaugeManager.cs`:
    - Lower `_minVelocityThreshold` from 4.0 to 3.0 to allow the gauge to charge while walking.
    - Update the logic to call `_ui.OnGaugeChanged` directly if the event system is perceived as unreliable by the user, or ensure the event-based connection is robust.
    - Added direct UI update call in `AddCharge` and `ConsumeCharge` as a fallback since a direct reference already exists.

## 4. Fix Health UI Initialization
- Ensure `PlayerHealth` raises the health event in `Start` (already does) and check if `UIManager` is ready.

# Verification & Testing
- **Movement Test**: Run and walk to see if the Velocity Gauge increases.
- **Combat Test**: Take damage to see if Shield Bars decrease.
- **Enemy Test**: Kill an enemy to see if "Enemies Left" count decreases.
- **Weapon Test**: Pick up a weapon and check if the ammo/durability display updates.
- **Console Check**: Ensure no "NullReferenceException" or "UnassignedReferenceException" appears during gameplay.
