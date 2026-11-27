using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace GKFR.MaxAI
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "gkfr.maxai";
        public const string PluginName = "GKFR Max AI";
        public const string PluginVersion = "1.0.4";

        internal static Plugin Instance;
        internal static new ManualLogSource Logger;
        Harmony harmony;
        ConfigEntry<bool> enabledEntry;
        ConfigEntry<int> maxAIPlayersEntry;
        ConfigEntry<KeyCode> toggleKey;

        // UI variables
        Rect windowRect = new Rect(20f, 20f, 300f, 200f);
        bool showWindow;
        string maxAIText = "7";

        public string Id => PluginGuid;
        public string DisplayName => "Max AI";
        public string Description => "Limits the number of AI players that spawn. F7 to open settings.";

        public bool Enabled
        {
            get => enabledEntry.Value;
            set
            {
                if (enabledEntry.Value != value)
                {
                    enabledEntry.Value = value;
                    ApplyPatches();
                }
            }
        }

        public int MaxAIPlayers
        {
            get => maxAIPlayersEntry.Value;
            set
            {
                int clamped = Mathf.Max(0, value); // Only limit minimum to 0, no upper limit
                maxAIPlayersEntry.Value = clamped;
                maxAIText = clamped.ToString();
            }
        }

        void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            enabledEntry = Config.Bind("General", "Enabled", true, "Enable AI player limit.");
            maxAIPlayersEntry = Config.Bind("General", "MaxAIPlayers", 999, "Maximum number of AI players (0 = no AI, higher = more AI). No upper limit.");
            toggleKey = Config.Bind("General", "ToggleKey", KeyCode.F1, "Toggle MaxAI settings window.");

            maxAIText = "999"; // Set default text to match default config

            ApplyPatches();
            Logger.LogInfo("GKFR Max AI loaded. Max AI players: " + MaxAIPlayers + ". Press F7 for settings.");
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey.Value))
            {
                showWindow = !showWindow;
            }
        }

        void OnGUI()
        {
            if (!showWindow) return;

            windowRect = GUI.Window(0x49494951, windowRect, DrawSettingsWindow, "Max AI Settings  F7 close");
        }

        void DrawSettingsWindow(int id)
        {
            GUILayout.Label("Max AI Settings", GUI.skin.box);
            GUILayout.Space(10f);

            GUILayout.Label("Maximum AI Players: " + MaxAIPlayers);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Custom Value", GUILayout.Width(90f));
            maxAIText = GUILayout.TextField(maxAIText ?? "", GUILayout.Width(90f));
            if (GUILayout.Button("Apply", GUILayout.Width(80f)))
            {
                if (int.TryParse(maxAIText.Trim(), out int parsed) && parsed >= 0)
                {
                    MaxAIPlayers = parsed;
                    Logger.LogInfo("Max AI players set to: " + MaxAIPlayers);
                }
                else
                {
                    maxAIText = MaxAIPlayers.ToString();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);

            GUILayout.Label("Quick Presets:", GUI.skin.box);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("0 AI")) MaxAIPlayers = 0;
            if (GUILayout.Button("3 AI")) MaxAIPlayers = 3;
            if (GUILayout.Button("7 AI")) MaxAIPlayers = 7;
            if (GUILayout.Button("Unlimited")) MaxAIPlayers = 999;
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);

            bool nextEnabled = GUILayout.Toggle(Enabled, "Enabled");
            if (nextEnabled != Enabled)
            {
                Enabled = nextEnabled;
            }

            GUILayout.Space(5f);

            if (GUILayout.Button("Reset to Default"))
            {
                MaxAIPlayers = 999;
            }

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        void ApplyPatches()
        {
            if (Enabled)
            {
                if (harmony == null)
                {
                    harmony = new Harmony(PluginGuid);
                    harmony.PatchAll(typeof(MaxAIPatches));
                    Logger.LogInfo("Max AI patches applied.");
                }
            }
            else if (harmony != null)
            {
                harmony.UnpatchSelf();
                harmony = null;
                Logger.LogInfo("Max AI patches removed.");
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

    static class MaxAIPatches
    {
        private static int aiCount = 0;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InGameGameMode), "NbLocalAisToCreate", MethodType.Getter)]
        static void OverrideLocalAICount(ref int __result)
        {
            if (Plugin.Instance == null || !Plugin.Instance.Enabled)
                return;

            int originalCount = __result;
            __result = Plugin.Instance.MaxAIPlayers;
            Plugin.Logger?.LogInfo("Local AI count override: " + originalCount + " -> " + __result);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameMode), "CreatePlayer")]
        static bool CountAndLimitAI(ECharacter _Character, ECharacter _Kart, string customName, string hatName, int nbStars, int driverId, int localHumanId, bool lockk, bool isAI, E_AILevel aiLevel)
        {
            if (Plugin.Instance == null || !Plugin.Instance.Enabled)
                return true;

            // Always allow human players
            if (!isAI)
                return true;

            // Count this AI
            aiCount++;
            
            // Check if we've exceeded the limit
            if (aiCount > Plugin.Instance.MaxAIPlayers)
            {
                Plugin.Logger?.LogInfo("AI limit exceeded (" + aiCount + " > " + Plugin.Instance.MaxAIPlayers + "), blocking AI creation");
                aiCount--; // Undo the count
                return false; // Block this AI
            }

            Plugin.Logger?.LogInfo("Creating AI #" + aiCount + " (limit: " + Plugin.Instance.MaxAIPlayers + ")");
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InGameGameMode), "Start")]
        static void ResetAICounter()
        {
            aiCount = 0;
            Plugin.Logger?.LogInfo("AI counter reset. Max AI: " + Plugin.Instance.MaxAIPlayers);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InGameGameMode), "OnDestroy")]
        static void ResetAICounterOnDestroy()
        {
            aiCount = 0;
        }
    }
}