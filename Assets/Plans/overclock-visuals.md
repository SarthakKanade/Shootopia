# Project Overview
- Game Title: Sharp Shooter (Velocity Wars)
- High-Level Concept: Fast-paced arena shooter where aggressive play builds a "Velocity Gauge" to unlock powerful abilities.
- Players: Single player.
- Inspiration / Reference Games: DOOM, Ultrakill.
- Tone / Art Direction: Robotic/Cybernetic, high-contrast, glowing accents.
- Target Platform: PC (Mac/Windows).
- Screen Orientation / Resolution: Landscape.
- Render Pipeline: URP.

# Game Mechanics
## Core Gameplay Loop
Maintain momentum by killing enemies, building the Velocity Gauge, and activating abilities like Overclock to dominate waves of robotic enemies.
## Overclock Visuals
- **Weapon Glow**: The active weapon glows with a warm amber emission (#FFB300) that pulses subtly (1Hz).
- **HUD Timer**: A radial countdown timer near the ability icon shows the remaining duration (8s), changing to red (#FF4400) in the final 2 seconds.

# UI
- **OverclockTimer Prefab**:
  - Radial Image (Fill Method: Radial 360, Origin: Top).
  - Background Circle (Alpha 75%, Dark Grey).
  - Center TMP Text (Countdown 8 -> 1).
  - Anchored near the Ability Icon in the `Overlay Canvas`.

# Key Asset & Context
- `Assets/Materials/OverclockWeapon_Active.mat`: URP Lit material for the glowing effect.
- `Assets/Scripts/Player/OverclockVisuals.cs`: Controller for the visual effects and HUD timer.
- `Assets/UI/OverclockTimer.prefab`: UI element for the countdown.
- `Assets/Scripts/Abilities/OverclockAbilitySO.cs`: Modified to trigger the visuals.

# Implementation Steps
1. **Material Creation**: Create `OverclockWeapon_Active.mat` with URP Lit shader, emission #FFB300 (intensity 1.8), metallic 0.7, smoothness 0.6.
2. **UI Prefab Creation**:
   - Create `OverclockTimer.prefab` with a radial fill image and TMP text.
   - Configure fill method, colors, and font as specified.
3. **OverclockVisuals Script**:
   - Implement `OverclockVisuals.cs` to handle:
     - Weapon material swap/emission pulsing.
     - HUD timer activation and countdown logic.
     - Emission fade-out on deactivation.
4. **Integration**:
   - Update `OverclockAbilitySO.cs` to call `OverclockVisuals.Activate` and `Deactivate`.
   - Update `ActiveWeapon.cs` if needed to provide access to the current weapon renderer.
5. **Scene Setup**:
   - Attach `OverclockVisuals` to the Player.
   - Assign the `OverclockTimer` prefab and the target material.
   - Add the `OverclockTimer` to the `Overlay Canvas` hierarchy.

# Verification & Testing
- **Visual Check**: Activate Overclock and verify the weapon glows and pulses as expected.
- **HUD Check**: Verify the radial timer appears, counts down from 8, turns red at 2 seconds, and disappears when finished.
- **Weapon Swap**: Verify the glow persists or reapplies if a weapon is swapped during Overclock (if applicable).
- **Cleanup**: Verify emission fades out smoothly and the original material is restored.
