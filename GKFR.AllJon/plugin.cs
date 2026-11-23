using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace GKFR.AllJon
{
    [BepInPlugin(PluginGuid, "Tera GKFR All Jon", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "gkfr.alljon";

        internal static Plugin Instance;
        ConfigEntry<bool> enabledEntry;
        ConfigEntry<KeyCode> toggleKey;
        Harmony harmony;

        public string Id => PluginGuid;
        public string DisplayName => "All Jon";
        public string Description => "Replaces all AI racers with Jon. Toggle with F6 (no restart needed).";

        public bool Enabled 
        {
            get => enabledEntry.Value;
            set
            {
                if (enabledEntry.Value != value)
                {
                    enabledEntry.Value = value;
                    Logger.LogInfo("All Jon: " + (value ? "Enabled" : "Disabled"));
                }
            }
        }

        void Awake()
        {
            Instance = this;
            enabledEntry = Config.Bind("General", "Enabled", true, "When on, CPU racers are not created. Toggle with F6 - no restart needed.");
            toggleKey = Config.Bind("General", "ToggleKey", KeyCode.F6, "Key to toggle All Jon on/off.");
            
            // Apply patches exactly like decompiled NoAI
            harmony = new Harmony(PluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            
            Logger.LogInfo("Tera GKFR All Jon loaded. AI: " + (Enabled ? "Disabled" : "Enabled") + ". Toggle with " + toggleKey.Value + " (no restart needed).");
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey.Value))
            {
                bool newState = !Enabled;
                Enabled = newState;
                Logger.LogInfo("All Jon toggled: " + (newState ? "Enabled" : "Disabled"));
            }
        }

        void OnDestroy()
        {
            if (harmony != null)
            {
                harmony.UnpatchSelf();
                harmony = null;
            }
        }
    }

    [HarmonyPatch(typeof(GameMode), "CreatePlayer")]
    public class AllJonPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref ECharacter _Character, ECharacter _Kart, string customName, string hatName, int nbStars, int driverId, int localHumanId, bool lockk, bool isAI, E_AILevel aiLevel)
        {
            if (Plugin.Instance.Enabled)
            {
                _Character = ECharacter.JON; // Replace AI character with Jon
            }
            return true; // Continue with the original method
        }
    }

    // Patch PlayerBuilder.Build to override character index in multiplayer
    [HarmonyPatch(typeof(PlayerBuilder), "Build")]
    public class PlayerBuilderPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref int characterIndex)
        {
            if (Plugin.Instance.Enabled)
            {
                characterIndex = (int)ECharacter.JON; 
            }
            return true; 
        }
    }

    // Patch GkNetMgr.BuildDriver to override character in network synchronization
    [HarmonyPatch(typeof(GkNetMgr), "BuildDriver")]
    public class GkNetMgrPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(GameObject driverObject, Vector3 color, string onlineId, int rankScore)
        {
            if (Plugin.Instance.Enabled)
            {
                Debug.Log("[AllJon] Intercepting GkNetMgr.BuildDriver for network synchronization");
                // We'll handle character override in PlayerBuilder.Build instead
            }
            return true; // Continue with the original method
        }
    }

    // Additional patch to catch character setup in Driver component
    [HarmonyPatch(typeof(Driver), "SetCharacterID")]
    public class DriverPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref int characterId)
        {
            if (Plugin.Instance.Enabled)
            {
                characterId = (int)ECharacter.JON; // Force Jon character ID
                Debug.Log("[AllJon] Overriding character ID to Jon in Driver.SetCharacterID");
            }
            return true; // Continue with the original method
        }
    }
}