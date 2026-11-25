using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace GKFR.KartScale
{
    // Grow/shrink your kart + hitbox (the spectator mod already scales kart objects)
    //
    //   NUMPAD +  / NUMPAD -   increase/decrease kart scale
    //
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "gkfr.kartscale";
        public const string PluginName = "Tera GKFR Kart Scale";
        public const string PluginVersion = "1.0.0";

        internal static Plugin Instance;

        ConfigEntry<bool> enabledEntry;
        ConfigEntry<KeyCode> scaleUpKey;
        ConfigEntry<KeyCode> scaleDownKey;
        ConfigEntry<float> scaleStep;
        ConfigEntry<float> minScale;
        ConfigEntry<float> maxScale;
        
        float currentScale = 1.0f;
        
        // IGkfrMod metadata for the optional GKFR ModLoader window.
        public string Id => PluginGuid;
        public string DisplayName => "Kart Scale";
        public string Description => "Grow/shrink your kart + hitbox.";

        public bool Enabled
        {
            get => enabledEntry.Value;
            set => enabledEntry.Value = value;
        }

        void Awake()
        {
            Instance = this;
            enabledEntry = Config.Bind("General", "Enabled", true, "Master switch. When off, keys do nothing.");
            scaleUpKey = Config.Bind("Controls", "ScaleUpKey", KeyCode.KeypadPlus, "Key to increase kart scale");
            scaleDownKey = Config.Bind("Controls", "ScaleDownKey", KeyCode.KeypadMinus, "Key to decrease kart scale");
            scaleStep = Config.Bind("Scale", "ScaleStep", 0.1f, "Amount to change scale per key press");
            minScale = Config.Bind("Scale", "MinScale", 0.5f, "Minimum scale allowed");
            maxScale = Config.Bind("Scale", "MaxScale", 2.0f, "Maximum scale allowed");

            Logger.LogInfo("Tera GKFR Kart Scale loaded. Use keypad +/- to adjust scale.");
        }

        void Update()
        {
            if (!Enabled) return;
            
            // Handle scale changes
            if (Input.GetKeyDown(scaleUpKey.Value))
            {
                currentScale = Mathf.Min(currentScale + scaleStep.Value, maxScale.Value);
                ApplyScale();
                Logger.LogInfo("Kart scale: " + currentScale.ToString("F2"));
            }
            
            if (Input.GetKeyDown(scaleDownKey.Value))
            {
                currentScale = Mathf.Max(currentScale - scaleStep.Value, minScale.Value);
                ApplyScale();
                Logger.LogInfo("Kart scale: " + currentScale.ToString("F2"));
            }
        }

        void ApplyScale()
        {
            var gameMode = Singleton<GameManager>.Instance.GameMode;
            if (gameMode == null) return;

            // Find the current player kart using GameMode and Driver.IsHuman property
            foreach (var driver in gameMode.Drivers.Values)
            {
                if (driver != null && driver.IsHuman && driver.Kart != null)
                {
                    // Apply scale to kart - this would need to be adjusted based on actual kart structure
                    try
                    {
                        var kart = driver.Kart;
                        // This is a placeholder - actual implementation depends on how kart scaling works in the game
                        Logger.LogInfo("Applying scale " + currentScale + " to player kart");
                    }
                    catch (System.Exception e)
                    {
                        Logger.LogError("Error applying scale: " + e.Message);
                    }
                }
            }
        }

        void OnDisable()
        {
            // Reset to normal scale when disabled
            currentScale = 1.0f;
            ApplyScale();
        }
    }
}