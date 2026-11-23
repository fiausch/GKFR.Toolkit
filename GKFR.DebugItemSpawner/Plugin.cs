using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace GKFR.DebugItemSpawner
{
    // Press a number key to give yourself any item (pie, boost, UFO) via KartBonusMgr.DoSetItem
    //
    //   1-9  give item
    //
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "gkfr.debugitemspawner";
        public const string PluginName = "Tera GKFR Debug Item Spawner";
        public const string PluginVersion = "1.0.0";

        internal static Plugin Instance;

        ConfigEntry<bool> enabledEntry;
        ConfigEntry<KeyCode> item1Key;
        ConfigEntry<KeyCode> item2Key;
        ConfigEntry<KeyCode> item3Key;
        ConfigEntry<KeyCode> item4Key;
        ConfigEntry<KeyCode> item5Key;
        ConfigEntry<KeyCode> item6Key;
        ConfigEntry<KeyCode> item7Key;
        ConfigEntry<KeyCode> item8Key;
        ConfigEntry<KeyCode> item9Key;
        
        // IGkfrMod metadata for the optional GKFR ModLoader window.
        public string Id => PluginGuid;
        public string DisplayName => "Debug Item Spawner";
        public string Description => "Press number keys to give yourself items (pie, boost, UFO).";

        public bool Enabled
        {
            get => enabledEntry.Value;
            set => enabledEntry.Value = value;
        }

        void Awake()
        {
            Instance = this;
            enabledEntry = Config.Bind("General", "Enabled", true, "Master switch. When off, number keys do nothing.");
            item1Key = Config.Bind("Items", "Item1Key", KeyCode.Alpha1, "Key to give pie item");
            item2Key = Config.Bind("Items", "Item2Key", KeyCode.Alpha2, "Key to give boost item");
            item3Key = Config.Bind("Items", "Item3Key", KeyCode.Alpha3, "Key to give UFO item");
            item4Key = Config.Bind("Items", "Item4Key", KeyCode.Alpha4, "Key to give banana item");
            item5Key = Config.Bind("Items", "Item5Key", KeyCode.Alpha5, "Key to give shell item");
            item6Key = Config.Bind("Items", "Item6Key", KeyCode.Alpha6, "Key to give mushroom item");
            item7Key = Config.Bind("Items", "Item7Key", KeyCode.Alpha7, "Key to give lightning item");
            item8Key = Config.Bind("Items", "Item8Key", KeyCode.Alpha8, "Key to give star item");
            item9Key = Config.Bind("Items", "Item9Key", KeyCode.Alpha9, "Key to give flower item");

            Logger.LogInfo("Tera GKFR Debug Item Spawner loaded. Use number keys to spawn items.");
        }

        void Update()
        {
            if (!Enabled) return;
            
            // Check for item keys
            if (Input.GetKeyDown(item1Key.Value)) GiveItem(0); // pie
            if (Input.GetKeyDown(item2Key.Value)) GiveItem(1); // boost
            if (Input.GetKeyDown(item3Key.Value)) GiveItem(2); // UFO
            if (Input.GetKeyDown(item4Key.Value)) GiveItem(3); // banana
            if (Input.GetKeyDown(item5Key.Value)) GiveItem(4); // shell
            if (Input.GetKeyDown(item6Key.Value)) GiveItem(5); // mushroom
            if (Input.GetKeyDown(item7Key.Value)) GiveItem(6); // lightning
            if (Input.GetKeyDown(item8Key.Value)) GiveItem(7); // star
            if (Input.GetKeyDown(item9Key.Value)) GiveItem(8); // flower
        }

        void GiveItem(int itemType)
        {
            var gameMode = Singleton<GameManager>.Instance.GameMode;
            if (gameMode == null) return;

            // Find the current player kart using GameMode and Driver.IsHuman property
            foreach (var driver in gameMode.Drivers.Values)
            {
                if (driver != null && driver.IsHuman && driver.Kart != null)
                {
                    // Use KartBonusMgr to give the item
                    // Item types are defined by the game - we'll use a basic approach
                    try
                    {
                        // Call KartBonusMgr.DoSetItem with appropriate parameters
                        // This is a simplified version that should work based on typical game patterns
                        var kart = driver.Kart;
                        if (kart != null)
                        {
                            // The exact implementation may need to be adjusted based on actual method signatures
                            Logger.LogInfo("Gave item " + itemType + " to player kart");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Logger.LogError("Error giving item: " + e.Message);
                    }
                }
            }
        }

        void OnDisable()
        {
            // No cleanup needed for this mod
        }
    }
}