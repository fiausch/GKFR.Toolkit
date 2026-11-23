using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace GKFR.ItemProbability
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "gkfr.itemprobability";
        public const string PluginName = "GKFR Item Probability Editor";
        public const string PluginVersion = "1.0.1";

        internal static Plugin Instance;
        internal static new ManualLogSource Logger;
        Harmony harmony;
        ConfigEntry<bool> enabledEntry;
        ConfigEntry<KeyCode> toggleKey;

        // UI variables
        Rect windowRect = new Rect(20f, 20f, 600f, 700f);
        bool showWindow;
        int selectedRank = 0;

        public string Id => PluginGuid;
        public string DisplayName => "Item Probability Editor";
        public string Description => "Customize item probabilities for each race position. F7 to open. Changes require recompile.";

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

        void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            enabledEntry = Config.Bind("General", "Enabled", true, "Enable custom item probabilities.");

            ApplyPatches();
            Logger.LogInfo("GKFR Item Probability Editor loaded. Press F5 to open editor.");
            Logger.LogInfo("Modify probabilities in Plugin.cs CUSTOM_PROBABILITIES section and recompile.");
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

            windowRect = GUI.Window(0x49494950, windowRect, DrawEditorWindow, "Item Probability Editor  F5 close");
        }

        void DrawEditorWindow(int id)
        {
            GUILayout.Label("Item Probability Editor (Compile-Time Configuration)", GUI.skin.box);
            GUILayout.Space(10f);

            // Rank selector
            GUILayout.BeginHorizontal();
            GUILayout.Label("Select Position:", GUILayout.Width(100f));
            if (GUILayout.Button("Previous", GUILayout.Width(80f)))
            {
                selectedRank = Mathf.Max(0, selectedRank - 1);
            }
            GUILayout.Label(GetRankName(selectedRank) + " Place", GUILayout.Width(100f));
            if (GUILayout.Button("Next", GUILayout.Width(80f)))
            {
                selectedRank = Mathf.Min(7, selectedRank + 1);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);

            // Show current custom probabilities
            GUILayout.Label("Current Custom Probabilities (%):", GUI.skin.box);
            GUILayout.Space(5f);

            DrawProbabilityRow("Pie", CUSTOM_PROBABILITIES[selectedRank].Pie);
            DrawProbabilityRow("Auto-Lock Pie", CUSTOM_PROBABILITIES[selectedRank].AutoLockPie);
            DrawProbabilityRow("Spring", CUSTOM_PROBABILITIES[selectedRank].Spring);
            DrawProbabilityRow("Lasagna", CUSTOM_PROBABILITIES[selectedRank].Lasagna);
            DrawProbabilityRow("Diamond", CUSTOM_PROBABILITIES[selectedRank].Diamond);
            DrawProbabilityRow("UFO", CUSTOM_PROBABILITIES[selectedRank].UFO);
            DrawProbabilityRow("Nap", CUSTOM_PROBABILITIES[selectedRank].Nap);
            DrawProbabilityRow("Parfume", CUSTOM_PROBABILITIES[selectedRank].Parfume);
            DrawProbabilityRow("Magic", CUSTOM_PROBABILITIES[selectedRank].Magic);

            GUILayout.Space(10f);

            // Multi-item chances
            GUILayout.Label("Multi-Item Bonus Chances (%):", GUI.skin.box);
            GUILayout.Space(5f);

            DrawProbabilityRow("2 Items Chance", CUSTOM_PROBABILITIES[selectedRank].TwoBonus);
            DrawProbabilityRow("3 Items Chance", CUSTOM_PROBABILITIES[selectedRank].ThreeBonus);

            GUILayout.Space(10f);

            // Total probability display
            int total = GetTotalProbability();
            GUILayout.Label("Total Probability: " + total + "%", total == 100 ? GUI.skin.label : GUI.skin.box);
            if (total != 100)
            {
                GUILayout.Label("Warning: Probabilities should sum to 100% for proper distribution", GUI.skin.box);
            }

            GUILayout.Space(10f);

            // Instructions
            GUILayout.Label("Instructions:", GUI.skin.box);
            GUILayout.Label("1. Modify CUSTOM_PROBABILITIES array in Plugin.cs");
            GUILayout.Label("2. Recompile the mod (dotnet build)");
            GUILayout.Label("3. Copy the new DLL to BepInEx/plugins");
            GUILayout.Label("4. Restart the game");

            GUILayout.Space(10f);

            bool nextEnabled = GUILayout.Toggle(Enabled, "Enabled");
            if (nextEnabled != Enabled)
            {
                Enabled = nextEnabled;
            }

            GUILayout.Space(5f);

            if (GUILayout.Button("Reset to Vanilla Probabilities"))
            {
                ResetToVanilla();
            }

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        void DrawProbabilityRow(string label, int value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(120f));
            GUILayout.Label(value + "%", GUILayout.Width(60f));
            GUILayout.EndHorizontal();
        }

        int GetTotalProbability()
        {
            return CUSTOM_PROBABILITIES[selectedRank].Pie +
                   CUSTOM_PROBABILITIES[selectedRank].AutoLockPie +
                   CUSTOM_PROBABILITIES[selectedRank].Spring +
                   CUSTOM_PROBABILITIES[selectedRank].Lasagna +
                   CUSTOM_PROBABILITIES[selectedRank].Diamond +
                   CUSTOM_PROBABILITIES[selectedRank].UFO +
                   CUSTOM_PROBABILITIES[selectedRank].Nap +
                   CUSTOM_PROBABILITIES[selectedRank].Parfume +
                   CUSTOM_PROBABILITIES[selectedRank].Magic;
        }

        string GetRankName(int rank)
        {
            return (rank + 1) + GetOrdinalSuffix(rank + 1);
        }

        string GetOrdinalSuffix(int num)
        {
            if (num >= 11 && num <= 13) return "th";
            switch (num % 10)
            {
                case 1: return "st";
                case 2: return "nd";
                case 3: return "rd";
                default: return "th";
            }
        }

        void ResetToVanilla()
        {
            CUSTOM_PROBABILITIES = new RankProbabilities[]
            {
                new RankProbabilities { Pie = 35, AutoLockPie = 5, Spring = 30, Lasagna = 5, Diamond = 25, UFO = 0, Nap = 0, Parfume = 0, Magic = 0, TwoBonus = 0, ThreeBonus = 0 },
                new RankProbabilities { Pie = 20, AutoLockPie = 10, Spring = 15, Lasagna = 10, Diamond = 15, UFO = 0, Nap = 0, Parfume = 10, Magic = 0, TwoBonus = 10, ThreeBonus = 0 },
                new RankProbabilities { Pie = 10, AutoLockPie = 15, Spring = 25, Lasagna = 10, Diamond = 25, UFO = 0, Nap = 0, Parfume = 0, Magic = 0, TwoBonus = 15, ThreeBonus = 5 },
                new RankProbabilities { Pie = 10, AutoLockPie = 15, Spring = 20, Lasagna = 5, Diamond = 40, UFO = 0, Nap = 0, Parfume = 0, Magic = 0, TwoBonus = 20, ThreeBonus = 10 },
                new RankProbabilities { Pie = 10, AutoLockPie = 15, Spring = 20, Lasagna = 5, Diamond = 55, UFO = 0, Nap = 0, Parfume = 0, Magic = 0, TwoBonus = 25, ThreeBonus = 15 },
                new RankProbabilities { Pie = 10, AutoLockPie = 0, Spring = 55, Lasagna = 5, Diamond = 0, UFO = 0, Nap = 0, Parfume = 0, Magic = 0, TwoBonus = 30, ThreeBonus = 20 },
                new RankProbabilities { Pie = 5, AutoLockPie = 5, Spring = 20, Lasagna = 5, Diamond = 5, UFO = 5, Nap = 20, Parfume = 0, Magic = 0, TwoBonus = 33, ThreeBonus = 23 },
                new RankProbabilities { Pie = 5, AutoLockPie = 5, Spring = 5, Lasagna = 6, Diamond = 40, UFO = 0, Nap = 15, Parfume = 0, Magic = 0, TwoBonus = 35, ThreeBonus = 25 }
            };
            Logger.LogInfo("Reset to vanilla probabilities. Recompile to apply changes.");
        }

        void ApplyPatches()
        {
            if (Enabled)
            {
                if (harmony == null)
                {
                    harmony = new Harmony(PluginGuid);
                    harmony.PatchAll(typeof(ItemProbabilityPatches));
                    Logger.LogInfo("Item probability patches applied.");
                }
            }
            else if (harmony != null)
            {
                harmony.UnpatchSelf();
                harmony = null;
                Logger.LogInfo("Item probability patches removed.");
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

        // ============================================================================
        // CUSTOM PROBABILITIES - MODIFY THESE VALUES AND RECOMPILE
        // ============================================================================
        // These are the probabilities that will be used in-game
        // Each row represents a position (1st place = index 0, 8th place = index 7)
        // Probabilities should ideally sum to 100% for each position
        // ============================================================================

        public static RankProbabilities[] CUSTOM_PROBABILITIES = new RankProbabilities[]
        {
            // 1st Place - Default vanilla values
            new RankProbabilities { Pie = 0, AutoLockPie = 0, Spring = 0, Lasagna = 0, Diamond = 0, UFO = 0, Nap = 0, Parfume = 0, Magic = 100, TwoBonus = 0, ThreeBonus = 0 },
            
            // 2nd Place - Default vanilla values
            new RankProbabilities { Pie = 0, AutoLockPie = 0, Spring = 0, Lasagna = 0, Diamond = 0, UFO = 0, Nap = 0, Parfume = 0, Magic = 100, TwoBonus = 10, ThreeBonus = 0 },
            
            // 3rd Place - Default vanilla values
            new RankProbabilities { Pie = 0, AutoLockPie = 0, Spring = 0, Lasagna = 0, Diamond = 0, UFO = 0, Nap = 0, Parfume = 0, Magic = 100, TwoBonus = 15, ThreeBonus = 5 },
            
            // 4th Place - Default vanilla values
            new RankProbabilities { Pie = 0, AutoLockPie = 0, Spring = 0, Lasagna = 0, Diamond = 0, UFO = 0, Nap = 0, Parfume = 0, Magic = 100, TwoBonus = 20, ThreeBonus = 10 },
            
            // 5th Place - Default vanilla values
            new RankProbabilities { Pie = 0, AutoLockPie = 0, Spring = 0, Lasagna = 0, Diamond = 0, UFO = 0, Nap = 0, Parfume = 0, Magic = 100, TwoBonus = 25, ThreeBonus = 15 },
            
            // 6th Place - Default vanilla values
            new RankProbabilities { Pie = 0, AutoLockPie = 0, Spring = 0, Lasagna = 0, Diamond = 0, UFO = 0, Nap = 0, Parfume = 0, Magic = 100, TwoBonus = 30, ThreeBonus = 20 },
            
            // 7th Place - Default vanilla values
            new RankProbabilities { Pie = 0, AutoLockPie = 0, Spring = 0, Lasagna = 0, Diamond = 0, UFO = 0, Nap = 0, Parfume = 0, Magic = 100, TwoBonus = 33, ThreeBonus = 23 },
            
            // 8th Place - Default vanilla values
            new RankProbabilities { Pie = 0, AutoLockPie = 0, Spring = 0, Lasagna = 0, Diamond = 0, UFO = 0, Nap = 0, Parfume = 0, Magic = 100, TwoBonus = 35, ThreeBonus = 25 }
        };

        // Example: To make 1st place always get Diamonds, change the first line to:
        // new RankProbabilities { Pie = 0, AutoLockPie = 0, Spring = 0, Lasagna = 0, Diamond = 100, UFO = 0, Nap = 0, Parfume = 0, Magic = 0, TwoBonus = 0, ThreeBonus = 0 },

        // Example: To give 8th place 50% chance of UFO, change the last line to:
        // new RankProbabilities { Pie = 5, AutoLockPie = 5, Spring = 5, Lasagna = 6, Diamond = 29, UFO = 50, Nap = 0, Parfume = 0, Magic = 0, TwoBonus = 35, ThreeBonus = 25 },
    }

    public struct RankProbabilities
    {
        public int Pie;
        public int AutoLockPie;
        public int Spring;
        public int Lasagna;
        public int Diamond;
        public int UFO;
        public int Nap;
        public int Parfume;
        public int Magic;
        public int TwoBonus;
        public int ThreeBonus;
    }

    static class ItemProbabilityPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BoxBonusEntity), "GiveRandomItemToKart")]
        static bool OverrideItemProbabilities(BoxBonusEntity __instance, RcVehicle pKart)
        {
            if (Plugin.Instance == null || !Plugin.Instance.Enabled)
                return true;

            try
            {
                int rank = pKart.RaceStats.GetRank();
                if (rank < 0 || rank > 7)
                    return true;

                bool isAI = pKart.GetControlType() == RcVehicle.ControlType.AI;
                if (isAI)
                    return true; // Only modify human player probabilities for now

                // Use reflection to access and modify the probability arrays
                var bonusRatioField = AccessTools.Field(typeof(BoxBonusEntity), "BonusRatio");
                var twoBonusField = AccessTools.Field(typeof(BoxBonusEntity), "TwoBonusChance");
                var threeBonusField = AccessTools.Field(typeof(BoxBonusEntity), "ThreeBonusChance");

                if (bonusRatioField == null || twoBonusField == null || threeBonusField == null)
                {
                    Plugin.Logger?.LogWarning("Could not access probability fields");
                    return true;
                }

                int[] bonusRatio = (int[])bonusRatioField.GetValue(__instance);
                int[] twoBonus = (int[])twoBonusField.GetValue(__instance);
                int[] threeBonus = (int[])threeBonusField.GetValue(__instance);

                if (bonusRatio == null || twoBonus == null || threeBonus == null)
                {
                    Plugin.Logger?.LogWarning("Probability arrays are null");
                    return true;
                }

                // Get custom probabilities for this rank
                var customProbs = Plugin.CUSTOM_PROBABILITIES[rank];

                // Override probabilities for the current rank
                int startIndex = rank * 9; // 9 items per rank
                bonusRatio[startIndex + 0] = customProbs.Pie;
                bonusRatio[startIndex + 1] = customProbs.AutoLockPie;
                bonusRatio[startIndex + 2] = customProbs.Spring;
                bonusRatio[startIndex + 3] = customProbs.Lasagna;
                bonusRatio[startIndex + 4] = customProbs.Diamond;
                bonusRatio[startIndex + 5] = customProbs.UFO;
                bonusRatio[startIndex + 6] = customProbs.Nap;
                bonusRatio[startIndex + 7] = customProbs.Parfume;
                bonusRatio[startIndex + 8] = customProbs.Magic;

                // Override multi-item chances
                twoBonus[rank] = customProbs.TwoBonus;
                threeBonus[rank] = customProbs.ThreeBonus;

                Plugin.Logger?.LogInfo($"Applied custom probabilities for {rank + 1}{GetOrdinalSuffix(rank + 1)} place");

                return true; // Continue with original method using modified arrays
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError("Error in item probability patch: " + ex.Message);
                return true; // Fall back to original method on error
            }
        }

        static string GetOrdinalSuffix(int num)
        {
            if (num >= 11 && num <= 13) return "th";
            switch (num % 10)
            {
                case 1: return "st";
                case 2: return "nd";
                case 3: return "rd";
                default: return "th";
            }
        }
    }
}