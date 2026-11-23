using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace GKFR.TrackLoader
{
    // This attribute tells BepInEx that this is a plugin
    // PluginGuid is a unique identifier for this plugin
    // PluginName is the display name shown in BepInEx logs
    // PluginVersion is the version number for tracking updates
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        // Constants are values that never change during program execution
        // const means the value is fixed at compile time
        public const string PluginGuid = "gkfr.trackloader";
        public const string PluginName = "Tera GKFR Track Loader";
        public const string PluginVersion = "1.0.0";

        // ConfigEntry<T> is a BepInEx type for storing configuration values
        // T is a generic type that can be bool, int, string, etc.
        ConfigEntry<bool> enabledEntry;          // Whether the mod is enabled
        ConfigEntry<string> customTracksPath;    // Path to custom tracks folder
        ConfigEntry<bool> autoLoadMappings;      // Whether to auto-detect track mappings

        // Harmony is the library we use to patch game code at runtime
        // It allows us to intercept and modify game functions
        Harmony harmony;

        // Dictionary stores key-value pairs, like a real dictionary
        // Here we map original track names to custom file paths
        static readonly Dictionary<string, string> trackMappings = new Dictionary<string, string>();

        // Static instance allows other parts of the code to access this plugin
        // static means it belongs to the class, not to instances
        public static Plugin staticInstance;

        // Public logger allows patches to write log messages
        // ManualLogSource is BepInEx's logging system
        public static ManualLogSource PublicLogger;

        // Properties are like methods that look like variables
        // get allows reading the value, set allows writing it
        public string Id => PluginGuid;
        public string DisplayName => "Track Loader";
        public string Description => "Load custom track scenes. Requires Unity scene files matching game structure.";

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
            // Config.Bind creates configuration entries
            // Parameters: (section, key, default value, description)
            enabledEntry = Config.Bind("General", "Enabled", false, "Enable custom track loading.");
            customTracksPath = Config.Bind("General", "CustomTracksPath", "./CustomTracks", "Path to folder containing custom track scenes.");
            autoLoadMappings = Config.Bind("General", "AutoLoadMappings", true, "Automatically detect track mappings from file names.");

            // Set up static references for patch access
            staticInstance = this;
            PublicLogger = Logger;

            // Auto-load track mappings if enabled
            if (autoLoadMappings.Value)
            {
                LoadTrackMappings();
            }

            // Apply Harmony patches
            ApplyPatches();

            // Log successful loading
            Logger.LogInfo("Tera GKFR Track Loader loaded. Custom tracks path: " + customTracksPath.Value);
            Logger.LogInfo("Found " + trackMappings.Count + " track mappings.");
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

                    // Try to patch scene loading methods
                    TryPatchSceneLoad();

                    Logger.LogInfo("Track loader patches applied.");
                }
            }
            else if (harmony != null)
            {
                // If disabling, remove all patches
                harmony.UnpatchSelf();
                harmony = null;
                trackMappings.Clear();
                Logger.LogInfo("Track loader patches removed.");
            }
        }

        // Try to patch Unity's scene loading methods
        void TryPatchSceneLoad()
        {
            try
            {
                // AccessTools.Method finds a method using reflection
                // Reflection allows examining code at runtime
                var loadSceneMethod = AccessTools.Method(typeof(SceneManager), "LoadScene", new[] { typeof(string) });
                if (loadSceneMethod != null)
                {
                    // Patch the method with our prefix
                    harmony.Patch(loadSceneMethod,
                        new HarmonyMethod(typeof(TrackPatches).GetMethod(nameof(TrackPatches.LoadScenePrefix))));
                    Logger.LogInfo("Patched SceneManager.LoadScene(string)");
                }

                // Also patch the async version
                var loadSceneAsyncMethod = AccessTools.Method(typeof(SceneManager), "LoadSceneAsync", new[] { typeof(string) });
                if (loadSceneAsyncMethod != null)
                {
                    harmony.Patch(loadSceneAsyncMethod,
                        new HarmonyMethod(typeof(TrackPatches).GetMethod(nameof(TrackPatches.LoadSceneAsyncPrefix))));
                    Logger.LogInfo("Patched SceneManager.LoadSceneAsync(string)");
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Failed to patch scene loading: " + ex.Message);
            }
        }

        // This function scans the custom tracks folder for track files
        // It builds a mapping of original track names to custom files
        void LoadTrackMappings()
        {
            trackMappings.Clear();
            string path = customTracksPath.Value;

            // Check if the directory exists
            if (!Directory.Exists(path))
            {
                Logger.LogWarning("Custom tracks directory does not exist: " + path);
                Logger.LogInfo("Create the directory and place your custom track .unity files there.");
                return;
            }

            // Search for .unity files (Unity scene files)
            // SearchOption.AllDirectories searches subdirectories too
            string[] unityFiles = Directory.GetFiles(path, "*.unity", SearchOption.AllDirectories);

            foreach (var file in unityFiles)
            {
                // Get the relative path from the custom tracks folder
                string relativePath = file.Substring(path.Length).TrimStart(Path.DirectorySeparatorChar);

                // Get the file name without extension
                string sceneName = Path.GetFileNameWithoutExtension(file);

                // Try to map to original track names
                // Garfield Kart uses codes like "E1C1" (Episode 1, Circuit 1)
                // or "E4C3" (Episode 4, Circuit 3)
                if (sceneName.Length == 4 && char.IsUpper(sceneName[0]) && sceneName[1] == '1' && sceneName[2] == 'C')
                {
                    trackMappings[sceneName] = file;
                    Logger.LogInfo("Mapped track: " + sceneName + " -> " + relativePath);
                }
                else
                {
                    // Also allow direct name matching
                    // If your custom track is named exactly like the original
                    trackMappings[sceneName] = file;
                    Logger.LogInfo("Added track mapping: " + sceneName + " -> " + relativePath);
                }
            }

            Logger.LogInfo("Loaded " + trackMappings.Count + " track mappings.");
        }

        // This function checks if a custom scene exists for a given track name
        // out parameter allows returning multiple values
        public static bool TryGetCustomScene(string originalSceneName, out string customScenePath)
        {
            customScenePath = null;

            // Get our plugin instance
            var instance = BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(PluginGuid, out var info) ? info.Instance as Plugin : null;
            if (instance == null || !instance.Enabled)
            {
                return false;
            }

            // Check if we have a mapping for this scene
            if (trackMappings.TryGetValue(originalSceneName, out string mappedPath))
            {
                customScenePath = mappedPath;
                return true;
            }

            return false;
        }

        // This function manually adds a track mapping
        // Useful for tracks that don't match the auto-detection pattern
        public static void AddTrackMapping(string originalName, string customFilePath)
        {
            trackMappings[originalName] = customFilePath;
            if (PublicLogger != null)
            {
                PublicLogger.LogInfo("Added manual track mapping: " + originalName + " -> " + customFilePath);
            }
        }

        // This function removes a track mapping
        public static void RemoveTrackMapping(string originalName)
        {
            if (trackMappings.Remove(originalName) && PublicLogger != null)
            {
                PublicLogger.LogInfo("Removed track mapping: " + originalName);
            }
        }
    }

    // This static class contains our Harmony patches
    // Patches intercept and modify game code execution
    public static class TrackPatches
    {
        // This patch intercepts SceneManager.LoadScene(string)
        // HarmonyPrefix means this runs BEFORE the original method
        public static bool LoadScenePrefix(string sceneName)
        {
            // Check if we have a custom scene for this track
            if (Plugin.TryGetCustomScene(sceneName, out string customPath))
            {
                try
                {
                    if (Plugin.PublicLogger != null)
                    {
                        Plugin.PublicLogger.LogInfo("Loading custom track: " + sceneName + " from " + customPath);
                    }

                    // IMPORTANT: Loading custom Unity scenes at runtime is complex
                    // Unity's scene system requires proper serialization and loading
                    // This is a simplified placeholder for demonstration

                    // In a full implementation, you would need to:
                    // 1. Use AssetBundle.LoadFromFile to load a packaged scene
                    // 2. Use SceneManager.LoadSceneAsync with the loaded scene
                    // 3. Handle scene dependencies and references

                    // For now, we'll just log that we found a custom track
                    // and let the original method load the default track
                    Plugin.PublicLogger.LogWarning("Custom track loading requires Unity AssetBundles. Using original track for now.");

                    // Return true to continue with the original method
                    // In a full implementation, you would return false after loading the custom scene
                    return true;
                }
                catch (Exception ex)
                {
                    if (Plugin.PublicLogger != null)
                    {
                        Plugin.PublicLogger.LogWarning("Failed to load custom scene: " + ex.Message);
                    }
                }
            }

            // Return true to continue with the original method
            return true;
        }

        // This patch intercepts SceneManager.LoadSceneAsync(string)
        // Used for asynchronous scene loading (shows loading screens)
        public static bool LoadSceneAsyncPrefix(string sceneName)
        {
            // Use the same logic as the synchronous version
            return LoadScenePrefix(sceneName);
        }
    }
}
