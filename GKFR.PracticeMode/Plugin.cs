using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace GKFR.PracticeMode
{
    // No countdown, no lap limit, instant-respawn toggle, key-to-restart
    //
    //   F4   toggle practice mode on/off
    //
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "gkfr.practicemode";
        public const string PluginName = "Tera GKFR Practice Mode";
        public const string PluginVersion = "1.0.0";

        internal static Plugin Instance;

        ConfigEntry<bool> enabledEntry;
        ConfigEntry<KeyCode> toggleKey;
        ConfigEntry<KeyCode> restartKey;
        ConfigEntry<bool> noCountdown;
        ConfigEntry<bool> noLapLimit;
        ConfigEntry<bool> instantRespawn;
        
        bool practiceModeActive;
        
        // IGkfrMod metadata for the optional GKFR ModLoader window.
        public string Id => PluginGuid;
        public string DisplayName => "Practice Mode";
        public string Description => "No countdown, no lap limit, instant-respawn toggle.";

        public bool Enabled
        {
            get => enabledEntry.Value;
            set => enabledEntry.Value = value;
        }

        void Awake()
        {
            Instance = this;
            enabledEntry = Config.Bind("General", "Enabled", true, "Master switch. When off, F4 does nothing.");
            toggleKey = Config.Bind("General", "ToggleKey", KeyCode.F4, "Key to toggle practice mode on/off.");
            restartKey = Config.Bind("General", "RestartKey", KeyCode.R, "Key to restart race");
            noCountdown = Config.Bind("Race", "NoCountdown", true, "Disable countdown before race starts");
            noLapLimit = Config.Bind("Race", "NoLapLimit", true, "Disable lap limit");
            instantRespawn = Config.Bind("Race", "InstantRespawn", true, "Instant respawn on crash");

            Logger.LogInfo("Tera GKFR Practice Mode loaded. Toggle with " + toggleKey.Value + ".");
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey.Value) && Enabled)
            {
                practiceModeActive = !practiceModeActive;
                Logger.LogInfo("Practice mode " + (practiceModeActive ? "on" : "off"));
            }
            
            // Check for restart key
            if (Input.GetKeyDown(restartKey.Value) && Enabled && practiceModeActive)
            {
                RestartRace();
            }
        }

        void RestartRace()
        {
            Logger.LogInfo("Restarting race in practice mode");
            // Implementation would need to call game's restart methods
            // This is a placeholder - actual implementation depends on game's API
        }

        void OnDisable()
        {
            // No cleanup needed for this mod
            practiceModeActive = false;
        }
    }
}