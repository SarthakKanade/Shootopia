# Project Overview
- Game Title: Sharp Shooter
- High-Level Concept: Fast-paced shooter with a momentum-based ability system.
- Players: Single player
- Inspiration / Reference Games: Doom Eternal, Ultrakill
- Tone / Art Direction: Low-poly / Stylized
- Target Platform: StandaloneOSX
- Render Pipeline: URP (PC_RPAsset)

# Game Mechanics
## Core Gameplay Loop
- Players kill enemies to build up a "Velocity Gauge".
- Building the gauge unlocks powerful abilities.
- Streaks provide multipliers to gauge gain.
- Deaths or specific damage types penalize the gauge.

## Controls and Input Methods
- Keyboard/Mouse (Legacy and New Input System supported).

# UI
- Velocity Gauge HUD (to be implemented in later steps).

# Key Asset & Context
- `WeaponSO.cs`: Stores weapon-specific gauge contribution.
- `VelocityGaugeManager.cs`: Manages the gauge state, streaks, and penalties.
- `IMomentum.cs`: Interface for accessing momentum data.
- `AbilitySO.cs`: ScriptableObject defining special abilities.

# Implementation Steps
## 1. Data Model & Interface Setup
- **Modify `WeaponSO.cs`**: Add `gaugeKillFill` float field.
  - File: `Assets/Scripts/Weapons/WeaponSO.cs`
- **Create `AbilitySO.cs`**: Define a base ScriptableObject for abilities.
  - File: `Assets/Scripts/Abilities/AbilitySO.cs`
- **Update `IMomentum.cs`**: Ensure the interface has `CurrentCharge`, `AddCharge`, and `ConsumeCharge`.
  - File: `Assets/Scripts/Core/Interfaces/IMomentum.cs`
- **Create `VelocityGaugeManager.cs`**: Scaffold the class with serialized fields and event wiring.
  - File: `Assets/Scripts/Player/VelocityGaugeManager.cs`

## 2. Asset Configuration
- **Update Weapon Assets**: Set `gaugeKillFill` values for Pistol (20), Machine Gun (12), and Sniper Rifle (35).
  - Assets: `Assets/Data/Weapons/Pistol.asset`, `Assets/Data/Weapons/Machine Gun.asset`, `Assets/Data/Weapons/Sniper Rifle.asset`.

## 3. Scene Integration
- **Attach Component**: Add `VelocityGaugeManager` to the `Player` GameObject in the scene.
  - Dependency: Step 1.

# Verification & Testing
- **Compilation**: Ensure all scripts compile without errors.
- **Inspector Check**: Verify all [SerializeField] fields in `VelocityGaugeManager` are visible on the Player GameObject.
- **Asset Check**: Verify `gaugeKillFill` is visible and correctly set in the WeaponSO assets.
- **Runtime Check**: Enter Play mode and ensure no "Missing Reference" or "Null Reference" errors occur related to the new scripts.
