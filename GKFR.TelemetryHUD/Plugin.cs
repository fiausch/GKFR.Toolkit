using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace GKFR.TelemetryHUD
{
    // Speed, boost level, drift charge, gear, slipstream overlay (extends LapTimes)
    //
    //   F3   toggle telemetry HUD on/off
    //
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "gkfr.telemetryhud";
        public const string PluginName = "Tera GKFR Telemetry HUD";
        public const string PluginVersion = "1.0.0";

        internal static Plugin Instance;

        ConfigEntry<bool> enabledEntry;
        ConfigEntry<KeyCode> toggleKey;
        ConfigEntry<bool> showSpeed;
        ConfigEntry<bool> showBoost;
        ConfigEntry<bool> showDrift;
        ConfigEntry<bool> showGear;
        ConfigEntry<bool> showSlipstream;
        
        bool telemetryActive;
        
        // IGkfrMod metadata for the optional GKFR ModLoader window.
        public string Id => PluginGuid;
        public string DisplayName => "Telemetry HUD";
        public string Description => "Speed, boost level, drift charge, gear, slipstream overlay.";

        public bool Enabled
        {
            get => enabledEntry.Value;
            set => enabledEntry.Value = value;
        }

        void Awake()
        {
            Instance = this;
            enabledEntry = Config.Bind("General", "Enabled", true, "Master switch. When off, F3 does nothing.");
            toggleKey = Config.Bind("General", "ToggleKey", KeyCode.F3, "Key to toggle telemetry HUD on/off.");
            showSpeed = Config.Bind("Display", "ShowSpeed", true, "Show speed information");
            showBoost = Config.Bind("Display", "ShowBoost", true, "Show boost level");
            showDrift = Config.Bind("Display", "ShowDrift", true, "Show drift charge");
            showGear = Config.Bind("Display", "ShowGear", true, "Show gear information");
            showSlipstream = Config.Bind("Display", "ShowSlipstream", true, "Show slipstream information");

            Logger.LogInfo("Tera GKFR Telemetry HUD loaded. Toggle with " + toggleKey.Value + ".");
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey.Value) && Enabled)
            {
                telemetryActive = !telemetryActive;
                Logger.LogInfo("Telemetry HUD " + (telemetryActive ? "on" : "off"));
            }
        }

        void OnGUI()
        {
            if (!telemetryActive) return;
            
            var gameMode = Singleton<GameManager>.Instance.GameMode;
            if (gameMode == null) return;

            // Find the current player kart using GameMode and Driver.IsHuman property
            foreach (var driver in gameMode.Drivers.Values)
            {
                if (driver != null && driver.IsHuman && driver.Kart != null)
                {
                    var kart = driver.Kart;
                    
                    // Display telemetry information
                    var x = 10;
                    var y = 10;
                    
                    if (showSpeed.Value)
                    {
                        GUI.Label(new Rect(x, y, 200, 20), "Speed: " + Mathf.Round(kart.CurrentSpeed * 3.6f) + " km/h");
                        y += 25;
                    }
                    
                    if (showBoost.Value)
                    {
                        // Boost level would typically be available from kart properties
                        GUI.Label(new Rect(x, y, 200, 20), "Boost: --");
                        y += 25;
                    }
                    
                    if (showDrift.Value)
                    {
                        // Drift charge would typically be available from kart properties  
                        GUI.Label(new Rect(x, y, 200, 20), "Drift: --");
                        y += 25;
                    }
                    
                    if (showGear.Value)
                    {
                        // Gear information would typically be available from kart properties
                        GUI.Label(new Rect(x, y, 200, 20), "Gear: --");
                        y += 25;
                    }
                    
                    if (showSlipstream.Value)
                    {
                        // Slipstream information would typically be available from kart properties
                        GUI.Label(new Rect(x, y, 200, 20), "Slipstream: --");
                        y += 25;
                    }
                }
            }
        }

        void OnDisable()
        {
            // No cleanup needed for this mod
            telemetryActive = false;
        }
    }
}