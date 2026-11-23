using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace GKFR.Godmode
{
    // Enables godmode for the player kart - makes it invincible to damage.
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
        
        bool godmodeActive;
        
        // IGkfrMod metadata for the optional GKFR ModLoader window.
        public string Id => PluginGuid;
        public string DisplayName => "Godmode";
        public string Description => "Makes your kart invincible to damage. F5 toggles godmode.";

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
            disableDamage = Config.Bind("General", "DisableDamage", true, "Disable damage to player kart when godmode is active.");

            Logger.LogInfo("Tera GKFR Godmode loaded. Toggle with " + toggleKey.Value + ".");
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey.Value) && Enabled)
            {
                godmodeActive = !godmodeActive;
                Logger.LogInfo("Godmode " + (godmodeActive ? "on" : "off"));
                
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

            // Iterate through all drivers to find human players
            foreach (var driver in gameMode.Drivers.Values)
            {
                if (driver != null && driver.IsHuman)
                {
                    // Apply invincibility to the player's kart using existing Driver methods
                    if (driver.Kart != null)
                    {
                        // Enable invincibility by activating blinking invulnerability (this is how it's done in the game)
                        driver.ActivateBlinkingInvulnerability();
                    }
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

                foreach (var driver in gameMode.Drivers.Values)
                {
                    if (driver != null && driver.IsHuman && driver.Kart != null)
                    {
                        // Stop blinking invulnerability to restore normal behavior
                        driver.StopBlinkingInvulnerability();
                    }
                }
            }
        }
    }
}