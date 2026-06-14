# Project Overview: Sharp Shooter (Velocity Wars)

## 1. Project Description
Sharp Shooter (internally referred to as **Velocity Wars**) is a fast-paced first-person shooter built in Unity 6. It centers around high-mobility combat and a "Velocity Gauge" mechanic that rewards aggressive play. Players battle waves of robotic enemies, managing limited weapon durability while building up a streak-based multiplier to unlock powerful abilities. The project is designed as an extensible arena shooter prototype emphasizing tight gunplay, dynamic weapon spawning, and strategic resource management.

## 2. Gameplay Flow / User Loop
1.  **Boot & Initialization**: The game starts in the `Main` scene, which initializes core managers and transitions to the `MainLevel` (gameplay scene).
2.  **Combat & Momentum**: Players engage enemies (Robots and Turrets). Each kill increases the **Velocity Gauge**. Maintaining a kill streak grants a multiplier to gauge gain.
3.  **Weapon Management**: Weapons are not permanent. They have varied durability types (Ammo, Time, or Uses). Players must scavenge for new weapons from `WeaponSpawnNodes` throughout the arena.
4.  **Ability Activation**: Once the Velocity Gauge reaches 100%, the player can activate a powerful `AbilitySO` (e.g., Overclock or Tracker Bot).
5.  **Victory/Defeat**: The session ends when all enemies are defeated (Victory) or the player loses all lives (GameOver). The loop then returns the player to the main menu or restarts the level.

## 3. Architecture
The project follows a **Decoupled Event-Driven Architecture** utilizing ScriptableObject-based Game Events to communicate between systems.

*   **Core Managers**:
    *   `GameManager`: Controls the high-level game state (Menu, Playing, Victory, etc.) and tracks global enemy counts and player lives.
    *   `WeaponSpawnManager`: Orchestrates the activation of spawn nodes and distribution of weapon crates.
    *   `VelocityGaugeManager`: Manages the momentum system, kill streaks, and ability execution.
*   **Communication Pattern**: Uses `GameEvent` and `FloatGameEvent` (ScriptableObjects) to allow components to trigger actions (like `OnEnemyKilled`) without direct references, improving modularity and reducing compile-time dependencies.
*   **Interface-Based Design**: Systems interact through contracts such as `IDamageable` for health/combat and `IPickupable` for world interactions.

`Location: Assets/Scripts/Core`

## 4. Game Systems & Domain Concepts

### Combat & Health System
*   `IDamageable`: Interface defining `TakeDamage` and `DamageType`.
*   `PlayerHealth`: Manages player-specific health and damage feedback.
*   `EnemyHealth`: Handles enemy destruction and notifies the `GameManager` and `VelocityGaugeManager` upon death.
*   `Projectile`: Physical or raycast-based damage delivery used by turrets and weapons.

`Location: Assets/Scripts/Enemies, Assets/Scripts/Player`

### Velocity Gauge & Ability System
*   `VelocityGaugeManager`: Tracks `_gaugeFill` (0-100%). It calculates a `GetStreakMultiplier` based on recent kills.
*   `AbilitySO`: Abstract ScriptableObject for player abilities. Concrete implementations like `OverclockAbilitySO` and `TrackerBotAbilitySO` define unique `Activate`/`Deactivate` logic.
*   `IMomentum`: Interface for charging and consuming gauge resources.

`Location: Assets/Scripts/Abilities, Assets/Scripts/Player`

### Weapon & Durability System
*   `ActiveWeapon`: The player's weapon controller. It handles shooting, zooming, and durability depletion logic.
*   `WeaponSO`: Defines weapon stats (Damage, FireRate, Category) and its `DurabilityType` (Ammo, Uses, or TimeBased).
*   `WeaponSpawnNode`: Static locations in the scene that can "Activate" to present a weapon crate to the player.

`Location: Assets/Scripts/Weapons, Assets/Scripts/Player`

## 5. Scene Overview
*   **Main**: The entry point scene containing global persistence managers.
*   **MainLevel**: The primary gameplay arena. It contains the NavMesh for AI navigation, weapon spawn nodes, and enemy spawn gates.
*   **SampleScene**: A playground used for testing new mechanics and VFX in isolation.

`Location: Assets/Scenes`

## 6. UI System
The UI is built using **UGUI** and is driven by the event system to maintain separation from game logic.
*   **VelocityGaugeUI**: Listens for `OnGaugeChanged` and `OnStreakChanged` events to update the HUD progress bar and multiplier text.
*   **ActiveWeapon UI**: `ActiveWeapon.cs` directly updates the `ammoText` and durability visuals on the `Overlay Canvas`.
*   **State UI**: The `Overlay Canvas.prefab` contains screens for Victory and Game Over, triggered by `GameManager` state transitions.

`Location: Assets/Prefabs/Player/Overlay Canvas.prefab`

## 7. Asset & Data Model
*   **ScriptableObjects (SO)**: Used extensively for data configuration.
    *   `WeaponSO`: Defines weapon behavior.
    *   `EnemySO`: Defines enemy stats and reward values.
    *   `GameEvent`: Orchestrates cross-system communication.
*   **Prefabs**:
    *   `Enemies`: Self-contained AI units (Robot, Turret).
    *   `Weapons`: Visual representations of weapons that are instantiated into the player's hands.
    *   `Pickups`: World-space objects like `HealthCrate` or `WeaponPickup`.

`Location: Assets/Data`

## 8. Notes, Caveats & Gotchas
*   **Weapon Expiration**: When a weapon's durability reaches zero, the `ActiveWeapon` script destroys the weapon GameObject immediately. Ensure any cleanup logic (like stopping audio loops) is handled in `OnDestroy`.
*   **NavMesh Dependencies**: AI (Robots) requires a `NavMeshSurface` to be baked in the scene. Moving or adding large obstacles requires a rebake of `NavMesh-Nav Mesh Surface.asset`.
*   **Input System**: The project uses the **New Input System**. Player inputs are funneled through `StarterAssetsInputs`. When adding new inputs (like a second ability), they must be defined in `InputSystem_Actions.inputactions` and read via the input component.
*   **Event Listeners**: Ensure that `GameEventListener` components in the scene are correctly linked to their corresponding ScriptableObject events; otherwise, UI or state transitions will fail silently fail.