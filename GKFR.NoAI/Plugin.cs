using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace GKFR.NoAI
{
    [BepInPlugin(PluginGuid, "Tera GKFR No AI", "1.0.6")]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "gkfr.noai";

        internal static Plugin Instance;
        ConfigEntry<bool> enabledEntry;
        ConfigEntry<KeyCode> toggleKey;
        Harmony harmony;

        public string Id => PluginGuid;
        public string DisplayName => "No AI racers";
        public string Description => "Stops CPU karts from spawning in both single-player and multiplayer. Results only show players. Toggle with F6 (no restart needed).";

        public bool Enabled 
        {
            get => enabledEntry.Value;
            set
            {
                if (enabledEntry.Value != value)
                {
                    enabledEntry.Value = value;
                    Logger.LogInfo("NoAI: " + (value ? "Enabled" : "Disabled"));
                }
            }
        }

        void Awake()
        {
            Instance = this;
            enabledEntry = Config.Bind("General", "Enabled", true, "When on, CPU racers are not created. Toggle with F6 - no restart needed.");
            toggleKey = Config.Bind("General", "ToggleKey", KeyCode.F6, "Key to toggle NoAI on/off.");
            
            // Apply patches exactly like decompiled NoAI
            harmony = new Harmony(PluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            
            Logger.LogInfo("Tera GKFR No AI loaded. AI: " + (Enabled ? "Disabled" : "Enabled") + ". Toggle with " + toggleKey.Value + " (no restart needed).");
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey.Value))
            {
                bool newState = !Enabled;
                Enabled = newState;
                Logger.LogInfo("NoAI toggled: " + (newState ? "Enabled" : "Disabled"));
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
    public class NoAIPatch
    {
        public static bool Prefix(ECharacter _Character, ECharacter _Kart, string customName, string hatName, int nbStars, int driverId, int localHumanId, bool lockk, bool isAI, E_AILevel aiLevel)
        {
            // Check if NoAI is enabled via config or toggle
            if (Plugin.Instance == null || !Plugin.Instance.Enabled)
            {
                return true; // Allow AI when disabled
            }
            return !isAI; // Disable AI when enabled
        }
    }

    // Results patch to only show real players
    [HarmonyPatch(typeof(MenuHDResultsLeaderboard), "SetPlayerValuesSingleRace")]
    public class ResultsSingleRacePatch
    {
        public static bool Prefix(MenuHDResultsLeaderboard __instance, int i, RankingManager rankManager)
        {
            // Only apply results filter when NoAI is enabled
            if (Plugin.Instance == null || !Plugin.Instance.Enabled)
            {
                return true;
            }

            bool hasData = rankManager.GetRaceScoreData(i) != null;
            
            if (hasData)
            {
                return true;
            }

            // Hide slots that don't have real player data
            var players = AccessTools.Field(typeof(MenuHDResultsLeaderboard), "m_players").GetValue(__instance) as LeaderboardPlayer[];
            if (players != null && i >= 0 && i < players.Length && players[i] != null)
            {
                players[i].Show(false);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(MenuHDResultsLeaderboard), "SetPlayerValuesGrandPrix")]
    public class ResultsGrandPrixPatch
    {
        public static bool Prefix(MenuHDResultsLeaderboard __instance, int i, RankingManager rankManager)
        {
            // Only apply results filter when NoAI is enabled
            if (Plugin.Instance == null || !Plugin.Instance.Enabled)
            {
                return true;
            }

            bool hasData = rankManager.GetChampionshipScoreData(i) != null;
            
            if (hasData)
            {
                return true;
            }

            // Hide slots that don't have real player data
            var players = AccessTools.Field(typeof(MenuHDResultsLeaderboard), "m_players").GetValue(__instance) as LeaderboardPlayer[];
            if (players != null && i >= 0 && i < players.Length && players[i] != null)
            {
                players[i].Show(false);
            }

            return false;
        }
    }
}
