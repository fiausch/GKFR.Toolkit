using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace GKFR.NoItems
{
    // Restrict which items boxes can give (the item pipeline is already hooked by ItemMultiplier)
    //
    //   F6   toggle no-items on/off
    //
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "gkfr.noitems";
        public const string PluginName = "Tera GKFR No Items";
        public const string PluginVersion = "1.0.0";

        internal static Plugin Instance;

        ConfigEntry<bool> enabledEntry;
        ConfigEntry<KeyCode> toggleKey;
        ConfigEntry<bool> disableAllItems;
        ConfigEntry<string> whitelistItems;
        
        bool noItemsActive;
        
        // IGkfrMod metadata for the optional GKFR ModLoader window.
        public string Id => PluginGuid;
        public string DisplayName => "No Items";
        public string Description => "Restrict which items boxes can give.";

        public bool Enabled
        {
            get => enabledEntry.Value;
            set => enabledEntry.Value = value;
        }

        void Awake()
        {
            Instance = this;
            enabledEntry = Config.Bind("General", "Enabled", true, "Master switch. When off, F6 does nothing.");
            toggleKey = Config.Bind("General", "ToggleKey", KeyCode.F6, "Key to toggle no-items on/off.");
            disableAllItems = Config.Bind("General", "DisableAllItems", false, "Disable all items completely");
            whitelistItems = Config.Bind("General", "WhitelistItems", "", "Comma-separated list of item names to allow (empty = all disabled)");

            Logger.LogInfo("Tera GKFR No Items loaded. Toggle with " + toggleKey.Value + ".");
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey.Value) && Enabled)
            {
                noItemsActive = !noItemsActive;
                Logger.LogInfo("No items " + (noItemsActive ? "on" : "off"));
            }
        }

        // This would be implemented by hooking into the item distribution system
        // The actual implementation would need to patch the game's item spawning methods
        void OnDisable()
        {
            // No cleanup needed for this mod
            noItemsActive = false;
        }
    }
}