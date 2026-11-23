using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace GKFR.FOV
{
    // Wider FOV and pushed far-clip for a less claustrophobic view.
    //
    //   F12  toggle FOV adjustment on/off
    //
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "gkfr.fov";
        public const string PluginName = "Tera GKFR FOV";
        public const string PluginVersion = "1.0.0";

        internal static Plugin Instance;

        ConfigEntry<bool> enabledEntry;
        ConfigEntry<KeyCode> toggleKey;
        ConfigEntry<float> fovMultiplier;
        ConfigEntry<float> farClipMultiplier;
        
        bool fovActive;
        float originalFov;
        float originalFarClip;
        
        // IGkfrMod metadata for the optional GKFR ModLoader window.
        public string Id => PluginGuid;
        public string DisplayName => "FOV";
        public string Description => "Wider FOV and pushed far-clip for a less claustrophobic view.";

        public bool Enabled
        {
            get => enabledEntry.Value;
            set => enabledEntry.Value = value;
        }

        void Awake()
        {
            Instance = this;
            enabledEntry = Config.Bind("General", "Enabled", true, "Master switch. When off, F12 does nothing.");
            toggleKey = Config.Bind("General", "ToggleKey", KeyCode.F12, "Key to toggle FOV adjustment on/off.");
            fovMultiplier = Config.Bind("General", "FovMultiplier", 1.5f, "FOV multiplier (default: 1.5)");
            farClipMultiplier = Config.Bind("General", "FarClipMultiplier", 3.0f, "Far clip multiplier (default: 3.0)");

            Logger.LogInfo("Tera GKFR FOV loaded. Toggle with " + toggleKey.Value + ".");
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey.Value) && Enabled)
            {
                fovActive = !fovActive;
                Logger.LogInfo("FOV adjustment " + (fovActive ? "on" : "off"));
                
                ApplyFOVAdjustment();
            }
        }

        void ApplyFOVAdjustment()
        {
            if (!fovActive)
            {
                // Restore original values
                var camera = Camera.main;
                if (camera != null)
                {
                    camera.fieldOfView = originalFov;
                    camera.farClipPlane = originalFarClip;
                }
                return;
            }

            // Apply adjusted values
            var camera = Camera.main;
            if (camera != null)
            {
                // Store original values
                originalFov = camera.fieldOfView;
                originalFarClip = camera.farClipPlane;
                
                // Apply new values
                camera.fieldOfView = originalFov * fovMultiplier.Value;
                camera.farClipPlane = originalFarClip * farClipMultiplier.Value;
            }
        }

        void OnDisable()
        {
            // Restore original FOV when disabled
            if (fovActive)
            {
                fovActive = false;
                var camera = Camera.main;
                if (camera != null)
                {
                    camera.fieldOfView = originalFov;
                    camera.farClipPlane = originalFarClip;
                }
            }
        }
    }
}