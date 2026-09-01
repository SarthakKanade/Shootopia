# Project Overview
- Game Title: Velocity Wars (FPS)
- High-Level Concept: Modular refactor of a Sharp Shooter prototype into a high-octane FPS.
- Players: Single player (FPS)
- Inspiration / Reference Games: Doom Eternal, Ultrakill (implied by "Momentum Gauge" and "Velocity")
- Target Platform: PC (StandaloneOSX)
- Render Pipeline: URP

# Game Mechanics
## Core Gameplay Loop
High-speed shooting, movement-based combat (Momentum Gauge), and pickup-based resource management.
## Controls and Input Methods
Keyboard & Mouse (New Input System & Legacy Input Manager).

# UI
N/A for this step.

# Key Asset & Context
- `Assets/Scripts/Core/Interfaces/IDamageable.cs`: Interface for objects that take damage.
- `Assets/Scripts/Core/Interfaces/IKillable.cs`: Interface for objects with a death state.
- `Assets/Scripts/Core/Interfaces/IPickupable.cs`: Interface for pickup items.
- `Assets/Scripts/Core/Interfaces/IMomentum.cs`: Interface for momentum-related stats.

# Implementation Steps
1. **Create Directory**: Ensure `Assets/Scripts/Core/Interfaces/` exists.
2. **Create IDamageable**: Implement `IDamageable` interface and `DamageType` enum in `Assets/Scripts/Core/Interfaces/IDamageable.cs`.
3. **Create IKillable**: Implement `IKillable` interface in `Assets/Scripts/Core/Interfaces/IKillable.cs`.
4. **Create IPickupable**: Implement `IPickupable` interface in `Assets/Scripts/Core/Interfaces/IPickupable.cs`.
5. **Create IMomentum**: Implement `IMomentum` interface in `Assets/Scripts/Core/Interfaces/IMomentum.cs`.

# Verification & Testing
1. **Compilation Check**: Use `GetConsoleLogs` to ensure no compilation errors were introduced.
2. **Path Validation**: Verify all 4 files exist at their designated paths.
