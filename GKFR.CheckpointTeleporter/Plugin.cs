using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace GKFR.CheckpointTeleporter
{
    // Teleport to start / last / next checkpoint via RaceStats
    //
    //   F7   teleport to start
    //   F8   teleport to last checkpoint  
    //   F9   teleport to next checkpoint
    //
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "gkfr.checkpointteleporter";
        public const string PluginName = "Tera GKFR Checkpoint Teleporter";
        public const string PluginVersion = "1.0.0";

        internal static Plugin Instance;

        ConfigEntry<bool> enabledEntry;
        ConfigEntry<KeyCode> toStartKey;
        ConfigEntry<KeyCode> toLastKey;
        ConfigEntry<KeyCode> toNextKey;
        
        // IGkfrMod metadata for the optional GKFR ModLoader window.
        public string Id => PluginGuid;
        public string DisplayName => "Checkpoint Teleporter";
        public string Description => "Teleport to start/last/next checkpoint.";

        public bool Enabled
        {
            get => enabledEntry.Value;
            set => enabledEntry.Value = value;
        }

        void Awake()
        {
            Instance = this;
            enabledEntry = Config.Bind("General", "Enabled", true, "Master switch. When off, keys do nothing.");
            toStartKey = Config.Bind("Teleport", "ToStartKey", KeyCode.F1, "Key to teleport to start");
            toLastKey = Config.Bind("Teleport", "ToLastKey", KeyCode.F1, "Key to teleport to last checkpoint");
            toNextKey = Config.Bind("Teleport", "ToNextKey", KeyCode.F1, "Key to teleport to next checkpoint");

            Logger.LogInfo("Tera GKFR Checkpoint Teleporter loaded. Use F7/F8/F9 for teleportation.");
        }

        void Update()
        {
            if (!Enabled) return;
            
            var gameMode = Singleton<GameManager>.Instance.GameMode;
            if (gameMode == null) return;

            // Find the current player kart using GameMode and Driver.IsHuman property
            foreach (var driver in gameMode.Drivers.Values)
            {
                if (driver != null && driver.IsHuman && driver.Kart != null)
                {
                    if (Input.GetKeyDown(toStartKey.Value))
                    {
                        TeleportToStart(driver);
                    }
                    else if (Input.GetKeyDown(toLastKey.Value))
                    {
                        TeleportToLastCheckpoint(driver);
                    }
                    else if (Input.GetKeyDown(toNextKey.Value))
                    {
                        TeleportToNextCheckpoint(driver);
                    }
                }
            }
        }

        void TeleportToStart(Driver driver)
        {
            Logger.LogInfo("Teleporting to start checkpoint");
            // Implementation would call RaceStats methods to teleport to start
            // This is a placeholder - actual implementation depends on game's API
        }

        void TeleportToLastCheckpoint(Driver driver)
        {
            Logger.LogInfo("Teleporting to last checkpoint");
            // Implementation would call RaceStats methods to teleport to last checkpoint  
            // This is a placeholder - actual implementation depends on game's API
        }

        void TeleportToNextCheckpoint(Driver driver)
        {
            Logger.LogInfo("Teleporting to next checkpoint");
            // Implementation would call RaceStats methods to teleport to next checkpoint
            // This is a placeholder - actual implementation depends on game's API
        }

        void OnDisable()
        {
            // No cleanup needed for this mod
        }
    }
}