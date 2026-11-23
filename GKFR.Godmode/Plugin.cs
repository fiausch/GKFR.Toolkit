using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace GKFR.Godmode
{
    // Enables godmode for karts - makes them invincible to damage.
    //
    //   F5   toggle godmode on/off
    //
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "gkfr.godmode";
        public const string PluginName = "Tera GKFR Godmode";
        public const string PluginVersion = "1.0.0";

        internal static Plugin Instance;

        ConfigEntry<bool> enabledEntry;
        ConfigEntry<KeyCode> toggleKey;
        ConfigEntry<bool> disableDamage;
        ConfigEntry<int> godmodeMode; // 0=player only, 1=all humans, 2=all karts, 3=AI only
        
        bool godmodeActive;
        int currentMode = 0; // 0=player only, 1=all humans, 2=all karts, 3=AI only
        
        // IGkfrMod metadata for the optional GKFR ModLoader window.
        public string Id => PluginGuid;
        public string DisplayName => "Godmode";
        public string Description => "Makes karts invincible to damage. F5 toggles godmode.";

        public bool Enabled
        {
            get => enabledEntry.Value;
            set => enabledEntry.Value = value;
        }

        void Awake()
        {
            Instance = this;
            enabledEntry = Config.Bind("General", "Enabled", true, "Master switch. When off, F5 does nothing.");
            toggleKey = Config.Bind("General", "ToggleKey", KeyCode.F5, "Key to toggle godmode on/off.");
            disableDamage = Config.Bind("General", "DisableDamage", true, "Disable damage to player karts when godmode is active.");
            godmodeMode = Config.Bind("General", "GodmodeMode", 0, "0=player only, 1=all humans, 2=all karts, 3=AI only");

            Logger.LogInfo("Tera GKFR Godmode loaded. Toggle with " + toggleKey.Value + ".");
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey.Value) && Enabled)
            {
                godmodeActive = !godmodeActive;
                
                // Cycle through modes when toggling
                currentMode = (currentMode + 1) % 4;
                Logger.LogInfo("Godmode " + (godmodeActive ? "on" : "off") + " - Mode: " + currentMode);
                
                // Apply the godmode effect
                ApplyGodmodeEffect();
            }
        }

        void ApplyGodmodeEffect()
        {
            if (!godmodeActive || !disableDamage.Value)
                return;

            // Find the current player kart using GameMode and Driver.IsHuman property
            var gameMode = Singleton<GameManager>.Instance.GameMode;
            if (gameMode == null)
                return;

            switch (currentMode)
            {
                case 0: // Player only
                    ApplyGodmodeToPlayer(gameMode);
                    break;
                case 1: // All humans
                    ApplyGodmodeToAllHumans(gameMode);
                    break;
                case 2: // All karts
                    ApplyGodmodeToAllKarts(gameMode);
                    break;
                case 3: // AI only
                    ApplyGodmodeToAIOnly(gameMode);
                    break;
            }
        }

        void ApplyGodmodeToPlayer(GameMode gameMode)
        {
            // Find the current player kart using GameMode and Driver.IsHuman property
            foreach (var driver in gameMode.Drivers.Values)
            {
                if (driver != null && driver.IsHuman && driver.Kart != null)
                {
                    // Enable invincibility by activating blinking invulnerability 
                    driver.ActivateBlinkingInvulnerability();
                }
            }
        }

        void ApplyGodmodeToAllHumans(GameMode gameMode)
        {
            // Apply to all human drivers
            foreach (var driver in gameMode.Drivers.Values)
            {
                if (driver != null && driver.IsHuman && driver.Kart != null)
                {
                    driver.ActivateBlinkingInvulnerability();
                }
            }
        }

        void ApplyGodmodeToAllKarts(GameMode gameMode)
        {
            // Apply to all karts regardless of driver type
            foreach (var driver in gameMode.Drivers.Values)
            {
                if (driver != null && driver.Kart != null)
                {
                    driver.ActivateBlinkingInvulnerability();
                }
            }
        }

        void ApplyGodmodeToAIOnly(GameMode gameMode)
        {
            // Apply to all AI drivers only
            foreach (var driver in gameMode.Drivers.Values)
            {
                if (driver != null && !driver.IsHuman && driver.Kart != null)
                {
                    driver.ActivateBlinkingInvulnerability();
                }
            }
        }

        void OnDisable()
        {
            // Re-enable normal damage when disabled
            if (godmodeActive)
            {
                godmodeActive = false;
                // Reset any godmode effects
                var gameMode = Singleton<GameManager>.Instance.GameMode;
                if (gameMode == null)
                    return;

                // Apply cleanup to all karts that were affected
                foreach (var driver in gameMode.Drivers.Values)
                {
                    if (driver != null && driver.Kart != null)
                    {
                        // Stop blinking invulnerability to restore normal behavior
                        driver.StopBlinkingInvulnerability();
                    }
                }
            }
        }
    }
}