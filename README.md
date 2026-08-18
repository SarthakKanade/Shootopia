# Shootopia

<div align="center">

![Unity](https://img.shields.io/badge/Unity-6.0+-black?style=for-the-badge&logo=unity)
![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP-blue?style=for-the-badge)
![Networking](https://img.shields.io/badge/Networking-Netcode%20for%20GameObjects-green?style=for-the-badge)
![UI](https://img.shields.io/badge/UI-UI%20Toolkit-purple?style=for-the-badge)
![Platform](https://img.shields.io/badge/Platform-PC%20%2F%20Mac-lightgrey?style=for-the-badge)

**A fast-paced, stylized 3D multiplayer chibi arcade shooter built in Unity 6 using Netcode for GameObjects, Universal Render Pipeline (URP), and UI Toolkit.**

</div>

---

## 🎮 Overview

**Shootopia** is an action-packed, third-person chibi arena shooter engineered with server-authoritative multiplayer Netcode, elemental combat mechanics, physical projectile simulations, and custom UI Toolkit interfaces. 

Featuring responsive locomotion mechanics (dash, roll, slide, jump), a tactical arsenal ranging from cryo/flame guns to parabolic throwables, dynamic tiered loot crates with holographic previews, and lethal environmental hazards, Shootopia delivers an arcade combat experience.

---

## ✨ Key Features

### 🌐 Server-Authoritative Multiplayer
- **Netcode for GameObjects (NGO) & UGS**: Integrated with Unity Gaming Services for lobby management, session discovery, and low-latency network synchronization.
- **Game Modes**:
  - **Free-For-All (FFA)**: Real-time dynamic leaderboard ranking and kill-count tracker.
  - **Team Deathmatch (TDM)**: Team-specific spawn points, team scoreboards (Blue vs Red), friendly fire prevention, and team-colored nameplates.
- **Session Resilience**: Graceful host migration/disconnect handling (`DisconnectHandler`), live error toasts (`NetworkNotificationUI`), and automated room player readiness checks.

### 🏃 Locomotion & Character Controller
- **State Machine Architecture (FSM)**: Dedicated states for Grounded, Airborne, Locomotion, Jump Start, Jump Air, Jump Land, Dash, and Roll.
- **Combat Movement**: Smooth sprinting, tactical rolling, and directional sliding.
- **Animation Rigging & Hand IK**: Real-time two-bone IK alignment for single-handed, two-handed, melee, and throwable weapon grips with automatic ragdoll IK release on death.

### 🔫 Dynamic Arsenal & Elemental Status Effects
- **Cryo Freeze Gun**: Direct hits completely freeze player movement and animators for 5 seconds; splash damage applies distance-scaled movement slowing with cyan frost shader effects.
- **Flame Gun**: Fires rapid incendiary streams that ignite targets with Afterburn damage-over-time (DoT), orange emissive shader glow, and flame particle FX.
- **Void Blaster**: Emits cosmic gravity orbs that pull targets inward before detonating in a high-damage implosion.
- **TrickShot Gun**: Fires high-velocity bouncing projectiles with real-time in-game ricochet reflection trajectory line previews.
- **Assault Rifle, Pistol, & Sniper Rifle**: Server-validated hitscan and ballistic projectile trajectories with customized weapon recoil, spread, and audio pitch modulation.
- **Charging Mechanics**: Visual energy buildup sphere tinted to weapon elements with audio pitch-bending modulation (0.8x to 1.6x).

### 💣 Tactical Throwables & Traps
- **Parabolic Trajectory Predictor**: Visual LineRenderer trajectory arc projection with surface-aligned dynamic landing markers while aiming throwables.
- **Grenades & Smoke Canisters**: Physics-driven parabolic tosses for area-denial smoke cover and explosive fragmentation.
- **Trip Mines**: Proximity-triggered laser mines with team-owner discrimination and disarm mechanics.

### 🛡️ Personal Energy Shield & Melee Combat
- **Rechargeable Forcefield**: Networked energy shield (`PlayerShield`) with visual forcefield sphere mesh, intercepting incoming projectiles before health depletion.
- **Melee System**: Sci-Fi neon knife slashing attacks with animation-synchronized delayed hit registration.
- **Deployables**: In-world consumable Health Boxes and portable Shield generators.

### 📦 Dynamic Loot Box System
- **Tiered Chests**: Defense (Chest 1), Traps (Chest 2), and Weapons (Chest 3).
- **Holographic Previews**: Rotating 3D item holograms floating above unopened crates.
- **Lifecycle Management**: Server-authoritative weighted loot distribution and automated respawn timers.

### 🌋 Dynamic Maps & Environmental Hazards
- **Map 1 (Inferno Arena)**: Volcanic battleground featuring modular rock formations, custom lava shader graphs, and lethal `LavaZone` environmental heat hazards.
- **Waiting Room / Lobby**: Interactive pre-match lobby allowing players to move, inspect loadouts, and ready up before match deployment.

### 🎨 Modern UI & Sound Architecture
- **UI Toolkit + USS**: Complete UI system (`ShootopiaTheme.uss`) featuring Main Menu tabs (Play, Lobby Browser, Settings, Loadout, Shop), 3D interactive avatar preview stage, animated loading transitions, and an in-game pause menu.
- **Live Scoreboard & Killfeed**: Hold `[Tab]` for real-time FFA/TDM leaderboard statistics and killfeed system messages.
- **Spatial Audio Engine (`AudioManager`)**: Centralized sound management with 2D/3D spatial audio pooling, pitch variation, footstep surface cues, weapon sounds, and UI feedback.

---

## 🕹️ Controls

| Action | Primary Key / Input |
| :--- | :--- |
| **Move** | `W` / `A` / `S` / `D` |
| **Look / Aim** | `Mouse` |
| **Fire / Throw (Release)** | `Left Mouse Button` |
| **Aim Down Sights (ADS) / Aim Trap** | `Right Mouse Button` (Hold) |
| **Jump** | `Space` |
| **Dash / Slide** | `Left Shift` |
| **Melee Attack (Knife)** | `V` / `F` |
| **Interact / Open Chest** | `E` |
| **In-Game Scoreboard** | `Tab` (Hold) |
| **Pause / Game Menu** | `Escape` |

---

## 🛠️ Project Structure

```
Assets/
├── _Shootopia/                # Core modular assets (animators, data, scripts)
├── Animations/                # Locomotion, combat, throwing & melee animation clips
├── Animators/                 # Shootopia locomotion & upper body mask controllers
├── Audio/                     # Sound effects (SFX) for Player, UI, and Weapons
├── Data/Weapons/              # ScriptableObject weapon data & attachment configs
├── Prefabs/
│   ├── Guns/                  # Weapon prefabs & visual attachments
│   ├── LootBoxes/             # Multi-tiered loot crate prefabs
│   ├── Player/                # Networked player character prefab & rigs
│   └── Weapons/Traps/         # Grenades, smoke clouds, and trip mines
├── Scenes/
│   ├── Bootstrap.unity        # Network initialization scene
│   ├── MainMenu.unity         # Main Menu UI & Avatar showcase stage
│   ├── WaitingRoom.unity      # Multiplayer lobby waiting room
│   └── Map1.unity             # Inferno arena gameplay scene
├── Scripts/
│   ├── Character/             # Motor, FSM states, PlayerHealth, PlayerShield
│   ├── Core/                  # AudioManager, CursorManager, PlayerProfileManager
│   ├── Environment/           # LavaZone hazard controllers
│   ├── Input/                 # Input Intent & Router mapping
│   ├── Multiplayer/           # GameModeBase, FFAMode, TDMMode, SessionManager
│   ├── UI/                    # MainMenuController, HUDController, ScoreboardController
│   └── Weapons/               # WeaponController, LootBoxController, Traps, Projectiles
└── UI/
    ├── Screens/               # UXML layouts (MainMenu, PauseMenu, LoadingOverlay, HUD)
    └── Theme/                 # ShootopiaTheme.uss & panel settings
```

---

## 🚀 Getting Started

### Prerequisites
- **Unity Version**: Unity `6000.0.x` (Unity 6) or higher.
- **Render Pipeline**: Universal Render Pipeline (URP).
- **Unity Packages**:
  - `com.unity.netcode.gameobjects` (Netcode for GameObjects)
  - `com.unity.services.multiplayer` / `com.unity.services.core` (UGS)
  - `com.unity.modules.ui` / `UI Toolkit`
  - `com.unity.animation.rigging` (Animation Rigging)
  - `com.unity.inputsystem` (New Input System)

### Setup & Play
1. Clone the repository:
   ```bash
   git clone https://github.com/SarthakKanade/Shootopia.git
   ```
2. Open the project in **Unity Hub** using **Unity 6**.
3. Open `Assets/Scenes/Bootstrap.unity` or `Assets/Scenes/MainMenu.unity`.
4. Press **Play** in the Unity Editor:
   - Click **Host** to create a lobby or **Join** to connect via Unity Gaming Services.
   - Select game mode (**FFA** or **TDM**) and jump into combat!

---

## 📄 License

This project is developed and maintained by **[Sarthak Kanade](https://github.com/SarthakKanade)**.
All rights reserved.
