# GKFR Asset Loader

**Mod to load custom assets from ./CustomAssets folder for hot-swapping.**

## What It Does

This mod intercepts Unity's asset loading and character model rendering to:
- Load custom textures from `./CustomAssets` folder (PNG/JPG support)
- Replace Arlene's character model with a cube (runtime mesh replacement demo)
- Allow hot-swapping assets between game launches without rebuilding

## Current Capabilities

- **Texture Hot-Swapping**: Can load custom PNG/JPG textures from `./CustomAssets`
- **Targeted Mesh Replacement**: Replaces only Arlene's model with a cube (not aggressive)
- **No Hardcoded Assets**: All textures are loaded from the custom assets folder
- **Caching System**: Stores loaded custom assets for performance

## Installation

1. Copy `GKFR.AssetLoader.dll` to your BepInEx plugins folder
2. Create the `CustomAssets` folder next to the game executable
3. The mod is **enabled by default**

## CustomAssets Directory

Place the `CustomAssets` folder next to your game executable:
```
/home/tera/.local/share/Steam/steamapps/common/Garfield Kart - Furious Racing/CustomAssets/
```

## Usage

### 1. Texture Replacement
Place PNG/JPG files in the CustomAssets folder with names matching game assets:
```
CustomAssets/
├── Characters/
│   └── arlene_texture.png
└── UI/
    └── menu_background.jpg
```

### 2. Arlene Cube Demo
The mod automatically replaces Arlene's character model with a cube at runtime. This is a demonstration of the mesh replacement system. Only Arlene is affected - no other characters or objects are modified.

### 3. Determine Asset Paths
To find the correct asset paths:
- Check the BepInEx console log when the mod is enabled
- Look at asset rips in your `rips/` folder
- Use trial and error with common naming patterns

## Limitations

- **Textures Only**: Currently only supports texture replacement (PNG/JPG)
- **Unity Serialization Required**: Meshes, materials, and other complex assets require Unity's asset serialization format
- **Path Matching**: Assets must match the exact internal path the game uses
- **Arlene Only**: The cube replacement only targets Arlene (other characters unchanged)

## Troubleshooting

**Mod not loading assets:**
- Check the BepInEx console for "Loaded custom texture" messages
- Verify the CustomAssets folder exists next to the game exe
- Ensure asset paths match what the game expects

**Arlene not a cube:**
- Check BepInEx log for "Found Arlene object" messages
- Ensure the mod is enabled in the config
- The mesh patch targets objects with "arlene" in their name or parent hierarchy

**Performance issues:**
- The mod uses caching to avoid reloading assets
- Reduce the number of custom assets if needed

## Future Improvements

Potential enhancements:
- Support for more asset types (meshes, materials via AssetBundles)
- Automatic path detection and logging
- GUI for asset management
- Hot-reloading of assets during gameplay
- Support for more character replacements beyond Arlene

## Credits

Experimental mod for Garfield Kart - Furious Racing.
Built with BepInEx and Harmony.
Inspired by the Fargield installer's Texture_Replacer.dll.