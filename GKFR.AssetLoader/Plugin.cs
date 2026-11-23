using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GKFR.AssetLoader
{
    // This attribute tells BepInEx that this is a plugin
    // PluginGuid is a unique identifier for this plugin
    // PluginName is the display name shown in BepInEx logs
    // PluginVersion is the version number
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        // Constants are values that never change
        // const means the value is fixed at compile time
        public const string PluginGuid = "gkfr.assetloader";
        public const string PluginName = "Tera GKFR Asset Loader";
        public const string PluginVersion = "1.0.0";

        // ConfigEntry<T> is a BepInEx type for storing configuration values
        // T is a generic type that can be bool, int, string, etc.
        ConfigEntry<bool> enabledEntry;          // Whether the mod is enabled
        ConfigEntry<string> customAssetsPath;    // Path to custom assets folder
        ConfigEntry<string> characterColorConfig; // Character color configuration
        
        // Static property to access character color config
        public static string ArleneColor => staticInstance?.characterColorConfig?.Value ?? "#FF0000";

        // Harmony is the library we use to patch game code at runtime
        // It allows us to intercept and modify game functions
        Harmony harmony;

        // Dictionary stores key-value pairs, like a real dictionary
        // This caches loaded custom assets so we don't reload them repeatedly
        static readonly Dictionary<string, UnityEngine.Object> customAssetsCache = new Dictionary<string, UnityEngine.Object>();

        // Static instance allows other parts of the code to access this plugin
        // static means it belongs to the class, not to instances
        public static Plugin staticInstance;

        // Public logger allows patches to write log messages
        // ManualLogSource is BepInEx's logging system
        public static ManualLogSource PublicLogger;

        // Properties are like methods that look like variables
        // get allows reading the value, set allows writing it
        public string Id => PluginGuid;
        public string DisplayName => "Asset Loader";
        public string Description => "Load custom assets from ./CustomAssets folder for hot-swapping.";

        // This property controls whether the mod is enabled
        public bool Enabled
        {
            get => enabledEntry.Value; // Return the config value
            set
            {
                // Only proceed if the value is actually changing
                if (enabledEntry.Value == value)
                {
                    return;
                }

                // Update the config value
                enabledEntry.Value = value;
                // Apply or remove patches based on the new state
                ApplyPatches();
            }
        }

        // Awake is called by Unity when the plugin loads
        // This is our initialization function
        void Awake()
        {
            // Set up static references for patch access
            staticInstance = this;
            PublicLogger = Logger;

            // Config.Bind creates configuration entries
            // Parameters: (section name, key name, default value, description)
            enabledEntry = Config.Bind("General", "Enabled", true, "Enable custom asset loading.");
            customAssetsPath = Config.Bind("General", "CustomAssetsPath", "./CustomAssets", "Path to folder containing custom assets (relative to game exe).");
            characterColorConfig = Config.Bind("CharacterColors", "ArleneColor", "#FF0000", "Hex color for Arlene character (e.g., #FF0000 for red).");

            // Apply the Harmony patches
            ApplyPatches();

            // Log that we loaded successfully
            Logger.LogInfo("Tera GKFR Asset Loader loaded. Custom assets path: " + customAssetsPath.Value);
            string fullPath = GetFullPath(customAssetsPath.Value);
            Logger.LogInfo("Looking for custom assets for Arlene in: " + fullPath);
            Logger.LogInfo("Custom assets directory exists: " + Directory.Exists(fullPath));
            Logger.LogInfo("WARNING: Mesh replacement is aggressive - may affect multiple objects");
        }

        // Get the full path relative to the game executable
        string GetFullPath(string relativePath)
        {
            string gamePath = Application.dataPath;
            // Application.dataPath points to the game's data folder
            // We need to go up to find the exe directory
            string exePath = Path.GetDirectoryName(Application.dataPath);
            
            // Normalize the path to handle mixed separators
            string combinedPath = Path.Combine(exePath, relativePath);
            return Path.GetFullPath(combinedPath);
        }

        // This function applies or removes Harmony patches
        void ApplyPatches()
        {
            if (Enabled)
            {
                // If enabling and no harmony instance exists, create one
                if (harmony == null)
                {
                    harmony = new Harmony(PluginGuid);

                    // Try to patch various asset loading methods
                    TryPatchResourcesLoad();
                    TryPatchSkinnedMeshRenderer();
                    TryPatchMeshFilter();

                    Logger.LogInfo("Asset loader patches applied.");
                }
            }
            else if (harmony != null)
            {
                // If disabling, remove all patches
                harmony.UnpatchSelf();
                harmony = null;
                customAssetsCache.Clear();
                Logger.LogInfo("Asset loader patches removed.");
            }
        }

        // Try to patch Unity's Resources.Load method
        // This is how Unity loads assets from the game's built-in resources
        // DISABLED: Too many overloads causing "Ambiguous match found" errors
        // We'll focus on the SkinnedMeshRenderer patch for now
        void TryPatchResourcesLoad()
        {
            // Skipping Resources.Load patch due to ambiguous method overloads
            // The SkinnedMeshRenderer patch is sufficient for our cube replacement demo
            Logger.LogInfo("Skipping Resources.Load patch (ambiguous overloads)");
        }

        // Try to patch SkinnedMeshRenderer for character model replacement
        void TryPatchSkinnedMeshRenderer()
        {
            try
            {
                // Patch the sharedMesh property getter
                var sharedMeshGetter = AccessTools.Property(typeof(SkinnedMeshRenderer), "sharedMesh").GetGetMethod();
                if (sharedMeshGetter != null)
                {
                    harmony.Patch(sharedMeshGetter,
                        postfix: new HarmonyMethod(typeof(AssetPatches).GetMethod(nameof(AssetPatches.SkinnedMeshRendererPostfix))));
                    Logger.LogInfo("Patched SkinnedMeshRenderer.sharedMesh");
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Failed to patch SkinnedMeshRenderer: " + ex.Message);
            }
        }

        // Try to patch MeshFilter for static mesh replacement
        void TryPatchMeshFilter()
        {
            try
            {
                // Patch the sharedMesh property getter
                var sharedMeshGetter = AccessTools.Property(typeof(MeshFilter), "sharedMesh").GetGetMethod();
                if (sharedMeshGetter != null)
                {
                    harmony.Patch(sharedMeshGetter,
                        postfix: new HarmonyMethod(typeof(AssetPatches).GetMethod(nameof(AssetPatches.MeshFilterPostfix))));
                    Logger.LogInfo("Patched MeshFilter.sharedMesh");
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Failed to patch MeshFilter: " + ex.Message);
            }
        }

        // This function tries to load a custom asset from our folder
        // out parameter is a C# feature that allows a method to return multiple values
        public static bool TryLoadCustomAsset(string path, Type type, out UnityEngine.Object asset)
        {
            asset = null;

            // Use the static instance instead of looking it up each time
            var instance = staticInstance;
            if (instance == null || !instance.Enabled)
            {
                return false;
            }

            // Check if we already loaded this asset (cache hit)
            if (customAssetsCache.TryGetValue(path, out var cachedAsset))
            {
                asset = cachedAsset;
                return true;
            }

            // Get the full path to the custom assets folder
            string customAssetsFullPath = instance.GetFullPath(instance.customAssetsPath.Value);
            string customPath = Path.Combine(customAssetsFullPath, path);

            Plugin.PublicLogger?.LogInfo($"Looking for custom asset: {customPath}");

            // Try different file extensions
            string[] extensions = { ".asset", ".png", ".jpg", ".obj", ".fbx", ".mat" };

            foreach (var ext in extensions)
            {
                string testPath = customPath + ext;
                if (File.Exists(testPath))
                {
                    try
                    {
                        // For textures, we can load them directly from image files
                        if (type == typeof(Texture2D) && (ext == ".png" || ext == ".jpg"))
                        {
                            // Read all bytes from the file
                            byte[] fileData = File.ReadAllBytes(testPath);
                            // Create a new texture
                            Texture2D tex = new Texture2D(2, 2);
                            // Load the image data into the texture
                            if (tex.LoadImage(fileData))
                            {
                                asset = tex;
                                customAssetsCache[path] = asset;
                                instance.Logger.LogInfo("Loaded custom texture: " + testPath);
                                return true;
                            }
                        }

                        // For meshes, try to load OBJ files
                        if (type == typeof(Mesh) && ext == ".obj")
                        {
                            Mesh loadedMesh = LoadOBJFile(testPath);
                            if (loadedMesh != null)
                            {
                                asset = loadedMesh;
                                customAssetsCache[path] = asset;
                                instance.Logger.LogInfo("Loaded custom mesh from OBJ: " + testPath);
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        instance.Logger.LogWarning("Failed to load custom asset " + testPath + ": " + ex.Message);
                    }
                }
            }

            return false;
        }

        // This function checks if a character should have custom colors applied
        public static bool ShouldApplyCharacterColor(GameObject gameObject, out string characterName)
        {
            characterName = "";
            if (gameObject == null) return false;

            // Check current object and all parents for character identification
            Transform current = gameObject.transform;
            int depth = 0;
            while (current != null && depth < 20) // Limit depth to prevent infinite loops
            {
                string name = current.name.ToLower();
                
                // Direct Arlene detection
                if (name.Contains("arlene"))
                {
                    Plugin.PublicLogger?.LogInfo($"Arlene detected in hierarchy: {current.name} at depth {depth}");
                    characterName = "Arlene";
                    return true;
                }
                
                // Check for character-specific components or naming patterns
                if (name.Contains("character") || name.Contains("player") || name.Contains("racer"))
                {
                    Plugin.PublicLogger?.LogInfo($"Found character object: {current.name}, checking for Arlene identifiers...");
                    // Look for Arlene-specific identifiers in siblings or children
                    if (HasArleneIdentifier(current.gameObject))
                    {
                        Plugin.PublicLogger?.LogInfo($"Arlene identifiers found in: {current.name}");
                        characterName = "Arlene";
                        return true;
                    }
                }
                
                current = current.parent;
                depth++;
            }

            return false;
        }

        // This function applies runtime mesh modifications
        // Only targets Arlene specifically
        public static Mesh ApplyMeshModifications(Mesh original, GameObject gameObject)
        {
            if (original == null || gameObject == null) return null;

            // Check if this is Arlene by checking the game object name or hierarchy
            string objectName = gameObject.name.ToLower();
            string parentName = gameObject.transform.parent != null ? gameObject.transform.parent.name.ToLower() : "";
            
            // Check entire hierarchy for character identification
            bool isArlene = IsArleneCharacter(gameObject);

            if (isArlene)
            {
                Plugin.PublicLogger?.LogInfo($"FOUND ARLENE: GameObject={objectName}, Parent={parentName}, attempting to load custom mesh");
                
                // Try to load custom OBJ file for Arlene
                if (TryLoadCustomAsset("Arlene/cube", typeof(Mesh), out UnityEngine.Object customMesh))
                {
                    Plugin.PublicLogger?.LogInfo("Successfully loaded custom mesh for Arlene");
                    return customMesh as Mesh;
                }
                else
                {
                    Plugin.PublicLogger?.LogInfo("Custom mesh not found, keeping original mesh");
                    return original;
                }
            }

            return original;
        }

        // Helper function to check if a GameObject belongs to Arlene character
        static bool IsArleneCharacter(GameObject gameObject)
        {
            if (gameObject == null) return false;

            // Check current object and all parents
            Transform current = gameObject.transform;
            int depth = 0;
            while (current != null && depth < 20) // Limit depth to prevent infinite loops
            {
                string name = current.name.ToLower();
                
                // Direct Arlene detection
                if (name.Contains("arlene"))
                {
                    Plugin.PublicLogger?.LogInfo($"Arlene detected in hierarchy: {current.name} at depth {depth}");
                    return true;
                }
                
                // Check for character-specific components or naming patterns
                if (name.Contains("character") || name.Contains("player") || name.Contains("racer"))
                {
                    Plugin.PublicLogger?.LogInfo($"Found character object: {current.name}, checking for Arlene identifiers...");
                    // Look for Arlene-specific identifiers in siblings or children
                    if (HasArleneIdentifier(current.gameObject))
                    {
                        Plugin.PublicLogger?.LogInfo($"Arlene identifiers found in: {current.name}");
                        return true;
                    }
                }
                
                current = current.parent;
                depth++;
            }

            return false;
        }

        // Check for Arlene-specific identifiers in the GameObject's components or children
        static bool HasArleneIdentifier(GameObject gameObject)
        {
            // Check all children for Arlene references
            foreach (Transform child in gameObject.transform)
            {
                string childName = child.name.ToLower();
                if (childName.Contains("arlene"))
                {
                    Plugin.PublicLogger?.LogInfo($"Arlene found in child: {child.name}");
                    return true;
                }
            }
            
            // Check object name for girl/female character indicators that might be Arlene
            string name = gameObject.name.ToLower();
            if (name.Contains("girl") || name.Contains("female") || name.Contains("woman"))
            {
                Plugin.PublicLogger?.LogInfo($"Female character indicator found in: {gameObject.name}");
                // Additional checks could be added here for more specific identification
                return true;
            }

            return false;
        }

        // This function loads an OBJ file and creates a Unity mesh from it
        static Mesh LoadOBJFile(string objPath)
        {
            try
            {
                string[] lines = File.ReadAllLines(objPath);
                List<Vector3> vertices = new List<Vector3>();
                List<Vector2> uvs = new List<Vector2>();
                List<Vector3> normals = new List<Vector3>();
                List<int> triangles = new List<int>();

                // Temporary storage for face parsing
                List<string> faceData = new List<string>();

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                        continue;

                    string[] parts = trimmedLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length == 0)
                        continue;

                    switch (parts[0])
                    {
                        case "v": // Vertex
                            if (parts.Length >= 4)
                            {
                                float x = float.Parse(parts[1]);
                                float y = float.Parse(parts[2]);
                                float z = float.Parse(parts[3]);
                                vertices.Add(new Vector3(x, y, z));
                            }
                            break;

                        case "vt": // Texture coordinate
                            if (parts.Length >= 3)
                            {
                                float u = float.Parse(parts[1]);
                                float v = float.Parse(parts[2]);
                                uvs.Add(new Vector2(u, v));
                            }
                            break;

                        case "vn": // Normal
                            if (parts.Length >= 4)
                            {
                                float x = float.Parse(parts[1]);
                                float y = float.Parse(parts[2]);
                                float z = float.Parse(parts[3]);
                                normals.Add(new Vector3(x, y, z));
                            }
                            break;

                        case "f": // Face
                            if (parts.Length >= 4)
                            {
                                // Parse face indices (handle both triangles and quads)
                                List<int> faceVertexIndices = new List<int>();
                                
                                for (int i = 1; i < parts.Length; i++)
                                {
                                    string[] indices = parts[i].Split('/');
                                    
                                    // OBJ indices are 1-based, convert to 0-based
                                    int vertexIndex = int.Parse(indices[0]) - 1;
                                    
                                    // Validate vertex index
                                    if (vertexIndex >= 0 && vertexIndex < vertices.Count)
                                    {
                                        faceVertexIndices.Add(vertexIndex);
                                    }
                                }
                                
                                // Triangulate if quad (4 vertices) or use as-is if triangle
                                if (faceVertexIndices.Count >= 3)
                                {
                                    // For n-gons, triangulate by fanning from first vertex
                                    for (int i = 1; i < faceVertexIndices.Count - 1; i++)
                                    {
                                        triangles.Add(faceVertexIndices[0]);
                                        triangles.Add(faceVertexIndices[i]);
                                        triangles.Add(faceVertexIndices[i + 1]);
                                    }
                                }
                            }
                            break;
                    }
                }

                if (vertices.Count == 0)
                {
                    Plugin.PublicLogger?.LogWarning("OBJ file has no vertices: " + objPath);
                    return null;
                }

                // Create the mesh
                Mesh mesh = new Mesh();
                mesh.name = "LoadedOBJ_" + Path.GetFileNameWithoutExtension(objPath);
                mesh.vertices = vertices.ToArray();
                mesh.triangles = triangles.ToArray();

                // Add UVs if available
                if (uvs.Count > 0)
                {
                    mesh.uv = uvs.ToArray();
                }

                // Add normals if available, otherwise recalculate
                if (normals.Count > 0)
                {
                    mesh.normals = normals.ToArray();
                }
                else
                {
                    mesh.RecalculateNormals();
                }

                mesh.RecalculateBounds();
                return mesh;
            }
            catch (Exception ex)
            {
                Plugin.PublicLogger?.LogWarning("Failed to load OBJ file " + objPath + ": " + ex.Message);
                return null;
            }
        }

        // This function creates a simple cube mesh
        static Mesh CreateCubeMesh()
        {
            // Create a new mesh object
            Mesh mesh = new Mesh();
            mesh.name = "CustomCube";

            // Vertices are the corners of the 3D shape
            // Vector3 represents a point in 3D space (x, y, z)
            Vector3[] vertices = new Vector3[8]
            {
                new Vector3(-0.5f, -0.5f, -0.5f), // Bottom-left-back
                new Vector3(0.5f, -0.5f, -0.5f),  // Bottom-right-back
                new Vector3(0.5f, 0.5f, -0.5f),   // Top-right-back
                new Vector3(-0.5f, 0.5f, -0.5f),  // Top-left-back
                new Vector3(-0.5f, -0.5f, 0.5f),  // Bottom-left-front
                new Vector3(0.5f, -0.5f, 0.5f),   // Bottom-right-front
                new Vector3(0.5f, 0.5f, 0.5f),    // Top-right-front
                new Vector3(-0.5f, 0.5f, 0.5f)    // Top-left-front
            };

            // Triangles define which vertices connect to form faces
            // Each triangle is 3 vertex indices
            int[] triangles = new int[36]
            {
                // Back face
                0, 2, 1, 0, 3, 2,
                // Front face
                4, 5, 6, 4, 6, 7,
                // Left face
                0, 4, 7, 0, 7, 3,
                // Right face
                1, 2, 6, 1, 6, 5,
                // Bottom face
                0, 1, 5, 0, 5, 4,
                // Top face
                3, 7, 6, 3, 6, 2
            };

            // Assign the vertices and triangles to the mesh
            mesh.vertices = vertices;
            mesh.triangles = triangles;

            // Recalculate normals for proper lighting
            // Normals are vectors that point perpendicular to surfaces
            mesh.RecalculateNormals();

            return mesh;
        }
    }

    // This static class contains our Harmony patches
    // Patches are the functions that intercept and modify game code
    public static class AssetPatches
    {
        // This patch modifies mesh assignments for animated characters
        // HarmonyPostfix means this runs AFTER the original method
        public static void SkinnedMeshRendererPostfix(SkinnedMeshRenderer __instance, ref Mesh __result)
        {
            try
            {
                if (__result == null || __instance == null) return;

                // Log for debugging - see what objects we're encountering
                string objectName = __instance.gameObject != null ? __instance.gameObject.name : "null";
                string parentName = __instance.transform.parent != null ? __instance.transform.parent.name : "null";
                string meshName = __result != null ? __result.name : "null";

                // Only log character-related objects to reduce spam
                bool isCharacter = objectName.ToLower().Contains("character") || 
                                   objectName.ToLower().Contains("player") || 
                                   objectName.ToLower().Contains("racer") ||
                                   objectName.ToLower().Contains("arlene") ||
                                   parentName.ToLower().Contains("character") || 
                                   parentName.ToLower().Contains("player") || 
                                   parentName.ToLower().Contains("racer") ||
                                   parentName.ToLower().Contains("arlene");

                if (isCharacter)
                {
                    Plugin.PublicLogger?.LogInfo($"SkinnedMeshRendererPostfix: GameObject={objectName}, Parent={parentName}, Mesh={meshName}");
                }

                // Check if this character should have custom colors applied
                if (Plugin.ShouldApplyCharacterColor(__instance.gameObject, out string characterName))
                {
                    // Apply solid color to character
                    AssetPatches.ApplySolidColorToCharacter(__instance as Renderer, characterName);
                }
            }
            catch (Exception ex)
            {
                Plugin.PublicLogger?.LogWarning("Error in SkinnedMeshRendererPostfix: " + ex.Message);
            }
        }

        // This patch modifies mesh assignments for static objects (karts, etc.)
        public static void MeshFilterPostfix(MeshFilter __instance, ref Mesh __result)
        {
            try
            {
                if (__result == null || __instance == null) return;

                // Log for debugging
                string objectName = __instance.gameObject != null ? __instance.gameObject.name : "null";
                string parentName = __instance.transform.parent != null ? __instance.transform.parent.name : "null";
                string meshName = __result != null ? __result.name : "null";

                // Only log character-related objects to reduce spam
                bool isCharacter = objectName.ToLower().Contains("character") || 
                                   objectName.ToLower().Contains("player") || 
                                   objectName.ToLower().Contains("racer") ||
                                   objectName.ToLower().Contains("arlene") ||
                                   parentName.ToLower().Contains("character") || 
                                   parentName.ToLower().Contains("player") || 
                                   parentName.ToLower().Contains("racer") ||
                                   parentName.ToLower().Contains("arlene");

                if (isCharacter)
                {
                    Plugin.PublicLogger?.LogInfo($"MeshFilterPostfix: GameObject={objectName}, Parent={parentName}, Mesh={meshName}");
                }

                // Check if this character should have custom colors applied
                if (Plugin.ShouldApplyCharacterColor(__instance.gameObject, out string characterName))
                {
                    // Apply solid color to character
                    MeshRenderer renderer = __instance.gameObject.GetComponent<MeshRenderer>();
                    AssetPatches.ApplySolidColorToCharacter(renderer, characterName);
                }
            }
            catch (Exception ex)
            {
                Plugin.PublicLogger?.LogWarning("Error in MeshFilterPostfix: " + ex.Message);
            }
        }

        // This function applies custom textures to a renderer's materials
        public static void ApplyCustomTexture(Renderer renderer, string texturePath)
        {
            if (renderer == null || renderer.materials == null) return;

            try
            {
                // Try to load custom texture
                if (Plugin.TryLoadCustomAsset(texturePath, typeof(Texture2D), out UnityEngine.Object customTexture))
                {
                    Texture2D tex = customTexture as Texture2D;
                    if (tex != null)
                    {
                        // Apply texture to all materials
                        foreach (Material mat in renderer.materials)
                        {
                            if (mat != null)
                            {
                                mat.mainTexture = tex;
                                Plugin.PublicLogger?.LogInfo("Applied custom texture to material: " + mat.name);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.PublicLogger?.LogWarning("Failed to apply custom texture: " + ex.Message);
            }
        }

        // This function generates a solid color texture
        static Texture2D CreateSolidColorTexture(Color color, int width = 256, int height = 256)
        {
            Texture2D texture = new Texture2D(width, height);
            Color[] colors = new Color[width * height];
            
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = color;
            }
            
            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }

        // This function parses a hex color string to Unity Color
        static Color ParseHexColor(string hexColor)
        {
            if (string.IsNullOrEmpty(hexColor) || !hexColor.StartsWith("#"))
            {
                return Color.white; // Default to white if invalid
            }

            string hex = hexColor.TrimStart('#');
            
            if (hex.Length == 6)
            {
                byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                return new Color(r / 255f, g / 255f, b / 255f);
            }
            
            return Color.white;
        }

        // This function applies solid color to character materials
        public static void ApplySolidColorToCharacter(Renderer renderer, string characterName)
        {
            if (renderer == null || renderer.materials == null) return;

            try
            {
                // Get color from config based on character name
                string colorHex = "#FF0000"; // Default red
                
                if (characterName.ToLower().Contains("arlene"))
                {
                    colorHex = Plugin.ArleneColor;
                }
                
                Color characterColor = ParseHexColor(colorHex);
                Texture2D solidColorTexture = CreateSolidColorTexture(characterColor);

                Plugin.PublicLogger?.LogInfo($"Applying solid color {colorHex} to character {characterName}");

                // Apply solid color texture to all materials
                foreach (Material mat in renderer.materials)
                {
                    if (mat != null)
                    {
                        mat.mainTexture = solidColorTexture;
                        Plugin.PublicLogger?.LogInfo("Applied solid color texture to material: " + mat.name);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.PublicLogger?.LogWarning("Failed to apply solid color: " + ex.Message);
            }
        }
    }
}
