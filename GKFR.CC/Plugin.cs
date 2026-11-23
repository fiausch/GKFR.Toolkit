using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GKFR.CC
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "gkfr.cc";
        public const string PluginName = "Tera GKFR CC Class";
        public const string PluginVersion = "1.1.0";

        internal static Plugin Instance;
        static readonly AccessTools.FieldRef<RcVehiclePhysic, float> ModifiedSpeedRef =
            AccessTools.FieldRefAccess<RcVehiclePhysic, float>("modifiedSpeed");
        static readonly AccessTools.FieldRef<RcVehiclePhysic, float> ModifiedTorqueRef =
            AccessTools.FieldRefAccess<RcVehiclePhysic, float>("modifiedTorque");

        ConfigEntry<bool> enabledEntry;
        ConfigEntry<int> ccEntry;
        ConfigEntry<KeyCode> toggleKey;
        Harmony harmony;
        string ccText = "150";
        Rect windowRect = new Rect(20f, 20f, 300f, 200f);
        bool showWindow;

        bool capturedSpeeds;
        public float speed50 = 72f;
        public float speed100 = 90f;
        public float speed150 = 108f;
        float torque50 = 1f;
        float torque100 = 1f;
        float torque150 = 1f;

        bool capturedPercents;
        float percent50 = -20f;
        float percent100;
        float percent150 = 20f;

        // Cache computed values to avoid recalculating
        int lastComputedCc = -1;
        float cachedSpeedKph;
        float cachedTorque;
        float cachedPercent;

        public string Id => PluginGuid;
        public string DisplayName => "CC class";
        public string Description => "Overrides top speed. 50 / 100 / 150 match the menu. Slider 0–999; the box accepts any CC.";

        public bool Enabled
        {
            get => enabledEntry.Value;
            set
            {
                if (enabledEntry.Value == value)
                {
                    return;
                }

                enabledEntry.Value = value;
                ApplyPatches();
                RefreshAll();
            }
        }

        public int CubicCentimetres
        {
            get => ccEntry.Value;
            set
            {
                ccEntry.Value = value;
                ccText = value.ToString();
                lastComputedCc = -1; // Invalidate cache when CC changes
                RefreshAll();
            }
        }

        void Awake()
        {
            Instance = this;
            enabledEntry = Config.Bind("General", "Enabled", true, "Apply custom CC to kart top speed.");
            ccEntry = Config.Bind("General", "CubicCentimetres", 150, "Engine class in CC.");
            toggleKey = Config.Bind("General", "ToggleKey", KeyCode.F2, "Toggle CC settings window.");
            ccText = ccEntry.Value.ToString();
            ApplyPatches();
            Logger.LogInfo("Tera GKFR CC Class loaded. Enabled: " + Enabled + ", CC: " + CubicCentimetres);
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
            if (!showWindow)
            {
                return;
            }

            windowRect = GUI.Window(0x43434343, windowRect, DrawSettings, "Tera CC Settings  F2 close");
        }

        void DrawSettings(int id)
        {
            int sliderValue = Mathf.Clamp(CubicCentimetres, 0, 999);
            GUILayout.Label("Engine class: " + CubicCentimetres + "cc");
            int rounded = Mathf.RoundToInt(GUILayout.HorizontalSlider(sliderValue, 0f, 999f));
            if (rounded != sliderValue)
            {
                CubicCentimetres = rounded;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Custom CC", GUILayout.Width(90f));
            ccText = GUILayout.TextField(ccText ?? "", GUILayout.Width(90f));
            if (GUILayout.Button("Apply", GUILayout.Width(80f)))
            {
                if (int.TryParse(ccText.Trim(), out int parsed))
                {
                    CubicCentimetres = parsed;
                }
                else
                {
                    ccText = CubicCentimetres.ToString();
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("50cc")) CubicCentimetres = 50;
            if (GUILayout.Button("100cc")) CubicCentimetres = 100;
            if (GUILayout.Button("150cc")) CubicCentimetres = 150;
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            bool nextEnabled = GUILayout.Toggle(Enabled, "Enabled");
            if (nextEnabled != Enabled)
            {
                Enabled = nextEnabled;
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
                    harmony.PatchAll(typeof(SpeedPatches));
                    Logger.LogInfo("Harmony patches applied for CC mod.");
                }
            }
            else if (harmony != null)
            {
                Logger.LogInfo("Removing harmony patches for CC mod.");
                harmony.UnpatchSelf();
                harmony = null;
                RefreshAll();
            }
        }

        internal float SpeedKphForCc()
        {
            if (lastComputedCc != CubicCentimetres)
            {
                UpdateCachedValues();
            }
            return cachedSpeedKph;
        }

        internal float TorqueForCc()
        {
            if (lastComputedCc != CubicCentimetres)
            {
                UpdateCachedValues();
            }
            return cachedTorque;
        }

        internal float PercentForCc()
        {
            if (lastComputedCc != CubicCentimetres)
            {
                UpdateCachedValues();
            }
            return cachedPercent;
        }

        void UpdateCachedValues()
        {
            lastComputedCc = CubicCentimetres;
            cachedSpeedKph = MapCc(CubicCentimetres, speed50, speed100, speed150);
            cachedTorque = MapCc(CubicCentimetres, torque50, torque100, torque150);
            cachedPercent = MapCc(CubicCentimetres, percent50, percent100, percent150);
        }

        public static float MapCc(int cc, float v50, float v100, float v150)
        {
            if (cc <= 50)
            {
                return Lerp(0f, 0f, 50f, v50, cc);
            }

            if (cc <= 100)
            {
                return Lerp(50f, v50, 100f, v100, cc);
            }

            return Lerp(100f, v100, 150f, v150, cc);
        }

        static float Lerp(float x0, float y0, float x1, float y1, float x)
        {
            if (Mathf.Abs(x1 - x0) < 0.001f)
            {
                return y1;
            }

            return y0 + (y1 - y0) * ((x - x0) / (x1 - x0));
        }

        internal void CapturePhysic(RcVehiclePhysic physic)
        {
            if (capturedSpeeds || physic == null || physic.m_Speeds == null || physic.m_Speeds.Length < 3)
            {
                return;
            }

            if (physic.m_Speeds[0] == null || physic.m_Speeds[2] == null)
            {
                return;
            }

            speed50 = physic.m_Speeds[0].m_MaxSpeedKPH;
            speed100 = physic.m_Speeds[1].m_MaxSpeedKPH;
            speed150 = physic.m_Speeds[2].m_MaxSpeedKPH;
            if (physic.m_MaxMotorTorque != null && physic.m_MaxMotorTorque.Length >= 3)
            {
                torque50 = physic.m_MaxMotorTorque[0];
                torque100 = physic.m_MaxMotorTorque[1];
                torque150 = physic.m_MaxMotorTorque[2];
            }

            capturedSpeeds = true;
        }

        internal void CapturePercents(KartArcadeGearBox box)
        {
            if (capturedPercents || box == null || box.PercentDifficulty == null || box.PercentDifficulty.Length < 3)
            {
                return;
            }

            percent50 = box.PercentDifficulty[0];
            percent100 = box.PercentDifficulty[1];
            percent150 = box.PercentDifficulty[2];
            capturedPercents = true;
        }

        internal void ApplyPhysicTables(RcVehiclePhysic physic)
        {
            if (!Enabled || physic == null || physic.m_Speeds == null)
            {
                return;
            }

            CapturePhysic(physic);
            float kph = SpeedKphForCc();
            float torque = TorqueForCc();
            int n = Mathf.Min(physic.m_Speeds.Length, 3);
            for (int i = 0; i < n; i++)
            {
                if (physic.m_Speeds[i] != null)
                {
                    physic.m_Speeds[i].m_MaxSpeedKPH = kph;
                }
            }

            if (physic.m_MaxMotorTorque != null)
            {
                int t = Mathf.Min(physic.m_MaxMotorTorque.Length, 3);
                for (int i = 0; i < t; i++)
                {
                    physic.m_MaxMotorTorque[i] = torque;
                }
            }
        }

        internal void RewriteModifiedStats(RcVehiclePhysic physic)
        {
            if (!Enabled || physic == null || physic.m_Speeds == null || physic.m_Speeds.Length == 0)
            {
                return;
            }

            int index = Mathf.Clamp((int)physic.Difficulty, 0, physic.m_Speeds.Length - 1);
            float tableSpeed = physic.m_Speeds[index] != null ? physic.m_Speeds[index].m_MaxSpeedKPH : speed100;
            float extras = ModifiedSpeedRef(physic) - tableSpeed;
            ModifiedSpeedRef(physic) = extras + SpeedKphForCc();
            if (physic.m_MaxMotorTorque != null && physic.m_MaxMotorTorque.Length > index)
            {
                float extrasT = ModifiedTorqueRef(physic) - physic.m_MaxMotorTorque[index];
                ModifiedTorqueRef(physic) = extrasT + TorqueForCc();
            }
        }

        static void RefreshAll()
        {
            var physics = Object.FindObjectsOfType<KartKinematicPhysic>();
            var updateStats = AccessTools.Method(typeof(KartKinematicPhysic), "UpdateStats");
            for (int i = 0; i < physics.Length; i++)
            {
                if (Instance != null)
                {
                    Instance.ApplyPhysicTables(physics[i]);
                }

                if (updateStats != null)
                {
                    updateStats.Invoke(physics[i], null);
                }
            }

            var boxes = Object.FindObjectsOfType<KartArcadeGearBox>();
            for (int i = 0; i < boxes.Length; i++)
            {
                if (Instance != null)
                {
                    Instance.CapturePercents(boxes[i]);
                }

                boxes[i].ComputeDiffSpeed();
            }
        }
    }

    static class SpeedPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(KartKinematicPhysic), "UpdateStats")]
        static void CaptureBeforeStats(KartKinematicPhysic __instance)
        {
            if (Plugin.Instance != null)
            {
                Plugin.Instance.CapturePhysic(__instance);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(KartKinematicPhysic), "UpdateStats")]
        static void ApplyAfterStats(KartKinematicPhysic __instance)
        {
            if (Plugin.Instance != null && Plugin.Instance.Enabled)
            {
                Plugin.Instance.ApplyPhysicTables(__instance);
                Plugin.Instance.RewriteModifiedStats(__instance);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(KartArcadeGearBox), nameof(KartArcadeGearBox.GetDifficultyPercent))]
        static void OverridePercent(KartArcadeGearBox __instance, ref float __result)
        {
            if (Plugin.Instance == null || !Plugin.Instance.Enabled)
            {
                return;
            }

            Plugin.Instance.CapturePercents(__instance);
            __result = Plugin.Instance.PercentForCc();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(KartArcadeGearBox), nameof(KartArcadeGearBox.ComputeDiffSpeed))]
        static void SwapPercentCompute(KartArcadeGearBox __instance, out float[] __state)
        {
            SwapPercentsIn(__instance, out __state);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(KartArcadeGearBox), nameof(KartArcadeGearBox.ComputeDiffSpeed))]
        static void RestorePercentCompute(KartArcadeGearBox __instance, float[] __state)
        {
            SwapPercentsOut(__instance, __state);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(KartArcadeGearBox), nameof(KartArcadeGearBox.ApplySpeedAdvantages))]
        static void SwapPercentApply(KartArcadeGearBox __instance, out float[] __state)
        {
            SwapPercentsIn(__instance, out __state);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(KartArcadeGearBox), nameof(KartArcadeGearBox.ApplySpeedAdvantages))]
        static void RestorePercentApply(KartArcadeGearBox __instance, float[] __state)
        {
            SwapPercentsOut(__instance, __state);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(KartArcadeGearBox), nameof(KartArcadeGearBox.GetMaxSpeed))]
        static void ScaleGearboxMax(KartArcadeGearBox __instance, ref float __result)
        {
            if (Plugin.Instance == null || !Plugin.Instance.Enabled)
            {
                return;
            }

            Plugin.Instance.CapturePercents(__instance);
            float vanilla = 1f + Plugin.Instance.PercentForCc() / 100f;
            float selected;
            
            // Cache GetComponent result to avoid multiple calls
            var vehiclePhysic = __instance.GetComponent<RcVehiclePhysic>();
            if (vehiclePhysic != null)
            {
                selected = vehiclePhysic.Difficulty switch
                {
                    Difficulty.EASY => 50f,
                    Difficulty.HARD => 150f,
                    _ => 100f
                };
            }
            else
            {
                selected = Singleton<GameConfigurator>.Instance.Difficulty switch
                {
                    Difficulty.EASY => 50f,
                    Difficulty.HARD => 150f,
                    _ => 100f
                };
            }

            float wanted = Plugin.Instance.SpeedKphForCc();
            float baseline = selected <= 50f ? 50f : selected;
            if (baseline > 0.01f && wanted > 0f)
            {
                __result *= wanted / MapSelectedBaseline(selected);
            }
        }

        static float MapSelectedBaseline(float selectedCc)
        {
            if (Plugin.Instance == null)
            {
                return selectedCc;
            }

            int cc = Mathf.RoundToInt(selectedCc);
            // Direct calculation without modifying instance state
            return Plugin.MapCc(cc, Plugin.Instance.speed50, Plugin.Instance.speed100, Plugin.Instance.speed150);
        }

        static void SwapPercentsIn(KartArcadeGearBox box, out float[] state)
        {
            state = null;
            if (Plugin.Instance == null || !Plugin.Instance.Enabled || box.PercentDifficulty == null)
            {
                return;
            }

            Plugin.Instance.CapturePercents(box);
            // Store original values inline instead of cloning
            state = new float[box.PercentDifficulty.Length];
            System.Array.Copy(box.PercentDifficulty, state, box.PercentDifficulty.Length);
            float percent = Plugin.Instance.PercentForCc();
            for (int i = 0; i < box.PercentDifficulty.Length; i++)
            {
                box.PercentDifficulty[i] = percent;
            }
        }

        static void SwapPercentsOut(KartArcadeGearBox box, float[] state)
        {
            if (state == null || box.PercentDifficulty == null)
            {
                return;
            }

            for (int i = 0; i < state.Length && i < box.PercentDifficulty.Length; i++)
            {
                box.PercentDifficulty[i] = state[i];
            }
        }
    }
}
