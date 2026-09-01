# Project Overview
- Game Title: Velocity Wars (FPS)
- High-Level Concept: Modular refactor of a Sharp Shooter prototype into a high-octane FPS.
- Step 2: Implementation of a ScriptableObject-based event system to decouple game systems.

# Game Mechanics
## Core Gameplay Loop
The event system will serve as the communication layer between decoupled systems (e.g., Health, Scoring, UI).
## Controls and Input Methods
No changes to input in this step.

# UI
The event system will eventually drive UI updates (Victory screen, Health bars) via listeners.

# Key Asset & Context
- `GameEvent.cs` & `GameEventListener.cs`: Basic SO event system.
- `FloatGameEvent.cs` & `FloatGameEventListener.cs`: SO event system for passing float values (e.g., health, count).
- `Assets/Data/Events/`: Directory for holding all event SO instances.

# Implementation Steps
1. **Create Directories**:
    - `Assets/Scripts/Core/Events/`
    - `Assets/Data/Events/`
2. **Create Scripts**:
    - `GameEvent.cs`: Broadcaster SO.
    - `GameEventListener.cs`: MonoBehaviour listener.
    - `FloatGameEvent.cs`: Broadcaster SO with float parameter.
    - `FloatGameEventListener.cs`: MonoBehaviour listener with float parameter.
3. **Generate SO Assets**:
    - Create 7 SO assets in `Assets/Data/Events/` using `AssetDatabase.CreateInstance` and `AssetDatabase.CreateAsset` via a tool command to ensure they are correctly instantiated.
4. **Verification**:
    - Check for compilation errors.
    - Confirm all 7 assets are visible in the Project window.

# Verification & Testing
1. **Compilation Check**: Use `GetConsoleLogs` to ensure no errors.
2. **Asset Validation**: List all created files and assets to confirm paths.
3. **Sanity Check**: Verify `FloatGameEventListener` uses `UnityEvent<float>` for its response.
