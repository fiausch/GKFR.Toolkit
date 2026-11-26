# GKFR Toolkit

The GKFR Toolkit is a collection of BepInEx mods for Garfield Kart: Furious Racing, providing various enhancements and tools to improve gameplay experience.

## Mods Included

### Core Functionality Mods

1. **ModLoader Mod** (Optional)
   - Optional in-game plugin manager window
   - F1 toggles mod loader window
   - View and toggle all installed plugins
   - Works independently without requiring other mod loader

2. **NoAI Mod**
   - Stops CPU karts from spawning in single-player and multiplayer
   - Results only show players (no AI entries)
   - F6 toggles No AI on/off
   - No restart needed

3. **Lap Times Mod**
   - Shows placement, speed, lap times, and gap behind 1st for all racers
   - F3 toggles lap times overlay on/off
   - Displays race standings on the right side of screen

4. **CC Class Mod**
   - Overrides top speed based on engine class
   - 50 / 100 / 150 match the menu; slider 0-999 accepts any CC
   - F2 toggles CC settings window

5. **Item Multiplier Mod**
   - Multiplies items from item boxes
   - Slider 1-10; the box accepts any value
   - F4 toggles item multiplier settings

6. **Item Probability Mod** (Advanced)
   - Customize item probabilities for each race position
   - F7 opens item probability editor
   - Requires code editing and recompilation to customize

7. **MaxAI Mod**
   - Limits the number of AI players that spawn
   - F7 opens Max AI settings
   - Quick presets: 0 AI, 3 AI, 7 AI, Unlimited (999)

8. **All Jon Mod**
   - Replaces all AI racers with Jon character
   - F6 toggles All Jon on/off
   - No restart needed

### Asset/Custom Content Mods

9. **Asset Loader Mod**
   - Load custom assets from ./CustomAssets folder
   - Hot-swapping for textures and meshes
   - Configure in BepInEx/config/

10. **Track Loader Mod**
    - Load custom tracks
    - Configure track paths in config file

11. **Single Player Multiplayer Mod**
    - Allows single-player races in multiplayer mode
    - Bypasses the 2-player minimum requirement
    - Works automatically when loaded

### Utility/Debug Mods

12. **Noclip Mod**
    - Fly the local kart through walls and floors
    - F8 toggles noclip, F11 toggles first-person camera
    - WASD move, Space/Ctrl up/down, Shift faster, mouse rotates

13. **Wireframe Mod**
    - Wireframe overlay for models + colliders
    - F9 toggles, F10 cycles modes (All/Invisible/Death), F12 solid colour
    - Debug and inspection tool

14. **FOV Mod**
    - Wider FOV and pushed far-clip for a less claustrophobic view
    - F12 toggles FOV adjustment
    - Increases field of view by 1.5x and far clip by 3.0x

15. **Godmode Mod**
    - Makes karts invincible to damage
    - F5 toggles godmode on/off
    - Cycle through 4 targeting modes by pressing F5 repeatedly

16. **Debug Item Spawner Mod**
    - Press number keys (1-9) to give yourself items
    - 1: pie, 2: boost, 3: UFO, 4: banana, 5: shell
    - 6: mushroom, 7: lightning, 8: star, 9: flower

17. **Practice Mode Mod**
    - No countdown, no lap limit, instant-respawn toggle
    - F4 toggles practice mode on/off
    - R key restarts race in practice mode

18. **Ghost Replay Mod**
    - Record your best lap, replay it as a translucent kart
    - F10 toggle ghost, F11 start/stop recording, F2 play back

19. **Kart Scale Mod**
    - Grow/shrink your kart + hitbox
    - NUMPAD + to increase, NUMPAD - to decrease

20. **Telemetry HUD Mod**
    - Speed, boost level, drift charge, gear, slipstream overlay
    - F3 toggles telemetry HUD on/off

## Installation

1. Copy the desired .dll files to your BepInEx/plugins folder
2. Launch the game
3. Use the configured keybindings to enable features

## Configuration

Each mod has its own configuration file in BepInEx/config/:
- com.gkfr.modloader.cfg
- com.gkfr.noai.cfg
- com.gkfr.laptimes.cfg
- com.gkfr.cc.cfg
- com.gkfr.itemmultiplier.cfg
- com.gkfr.itemprobability.cfg
- com.gkfr.maxai.cfg
- com.gkfr.alljon.cfg
- com.gkfr.assetloader.cfg
- com.gkfr.noclip.cfg
- com.gkfr.wireframe.cfg
- com.gkfr.fov.cfg
- com.gkfr.godmode.cfg
- com.gkfr.debugitemspawner.cfg
- com.gkfr.practicemode.cfg
- com.gkfr.ghostreplay.cfg
- com.gkfr.kartscale.cfg
- com.gkfr.telemetryhud.cfg
- gkfr.singleplayermultiplayer.cfg

## Keybindings Summary

| Key | Function(s) |
|-----|-------------|
| F1 | ModLoader toggle |
| F2 | CC Class / Ghost Replay playback |
| F3 | Lap Times / Telemetry HUD toggle |
| F4 | Practice Mode / Item Multiplier toggle |
| F5 | Godmode toggle |
| F6 | NoAI / No Items / All Jon toggle |
| F7 | MaxAI / Item Probability / Checkpoint Teleporter (start) |
| F8 | Checkpoint Teleporter (last) / Noclip toggle |
| F9 | Checkpoint Teleporter (next) / Wireframe toggle |
| F10 | Ghost Replay toggle / Wireframe mode cycle |
| F11 | Noclip first-person toggle / Ghost Replay recording |
| F12 | FOV toggle / Wireframe solid colour |

## Notes
- Some mods may conflict with each other (see Keybinding Conflicts section)
- Test each mod individually before combining
- Configuration changes may require restart or runtime toggle
- See individual HOW_TO_USE.txt files for more details
