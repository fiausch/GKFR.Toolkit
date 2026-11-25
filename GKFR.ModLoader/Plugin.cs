using System;
using System.IO;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace GKFR.ModLoader
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "gkfr.modloader";
        public const string PluginName = "Tera GKFR Mod Loader";
        public const string PluginVersion = "1.0.0";

        ConfigEntry<KeyCode> toggleKey;
        Rect windowRect = new Rect(36f, 70f, 520f, 520f);
        Vector2 scroll;
        bool windowOpen;
        bool cursorWasVisible;
        CursorLockMode previousLock;

        void Awake()
        {
            toggleKey = Config.Bind("General", "ToggleKey", KeyCode.F1, "Opens or closes the mod loader.");
            new Harmony(PluginGuid).PatchAll();
            Logger.LogInfo("Tera GKFR Mod Loader ready (optional). Press F1 to toggle plugin management window.");
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey.Value))
            {
                SetWindowOpen(!windowOpen);
            }
        }

        void OnGUI()
        {
            if (!windowOpen)
            {
                return;
            }

            GarfieldTheme.Ensure();
            GUISkin previous = GUI.skin;
            GUI.skin = GarfieldTheme.Skin;
            windowRect = GUI.Window(0x474B4652, windowRect, DrawWindow, "GARFIELD KART  ·  MODS");
            GUI.skin = previous;
        }

        void DrawWindow(int id)
        {
            GUILayout.Label("GKFR mod loader - Optional plugin manager. F1 toggles this window.");
            GUILayout.Space(6f);

            scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));

            GUILayout.Label("GKFR mods (legacy registration)");
            var mods = GkfrModRegistry.Mods;
            if (mods.Count == 0)
            {
                GUILayout.Label("No legacy GKFR mods registered (all mods are now standalone).");
            }
            else
            {
                for (int i = 0; i < mods.Count; i++)
                {
                    DrawMod(mods[i]);
                }
            }

            GUILayout.Space(12f);
            GUILayout.Label("BepInEx / plugins (standalone mods)");
            DrawPluginFolder();

            GUILayout.EndScrollView();

            GUILayout.Space(8f);
            if (GUILayout.Button("Close"))
            {
                SetWindowOpen(false);
            }

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 28f));
        }

        static void DrawMod(IGkfrMod mod)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            bool next = GUILayout.Toggle(mod.Enabled, "  " + mod.DisplayName);
            if (next != mod.Enabled)
            {
                mod.Enabled = next;
            }

            if (!string.IsNullOrEmpty(mod.Description))
            {
                GUILayout.Label(mod.Description);
            }

            if (mod is IGkfrModSettings settings)
            {
                GUILayout.Space(4f);
                settings.DrawExtraGui();
            }

            GUILayout.EndVertical();
            GUILayout.Space(6f);
        }

        void DrawPluginFolder()
        {
            string pluginPath = Paths.PluginPath;
            if (string.IsNullOrEmpty(pluginPath) || !Directory.Exists(pluginPath))
            {
                GUILayout.Label("Could not find BepInEx/plugins.");
                return;
            }

            GUILayout.Label("These plugins work independently without requiring the mod loader.");
            GUILayout.Space(4f);

            string[] files = Directory.GetFiles(pluginPath, "*.dll", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            int shown = 0;
            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                string name = Path.GetFileName(file);
                if (!name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                    !name.EndsWith(".dll.disabled", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (name.IndexOf("GKFR.ModLoader", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    GUILayout.Label("  " + name + "  (this loader, optional)");
                    shown++;
                    continue;
                }

                bool fileEnabled = name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
                PluginInfo loaded = FindLoaded(file);
                IGkfrMod registered = FindRegistered(loaded);
                bool runtimeOn = registered != null ? registered.Enabled : (loaded != null && loaded.Instance != null && loaded.Instance.enabled);
                bool shownOn = fileEnabled && runtimeOn;
                bool next = GUILayout.Toggle(shownOn, "  " + StripDisabledSuffix(name));
                shown++;
                if (next == shownOn)
                {
                    continue;
                }

                TogglePlugin(file, loaded, registered, next);
            }

            if (shown == 0)
            {
                GUILayout.Label("No plugin DLLs found.");
            }
        }

        void TogglePlugin(string file, PluginInfo loaded, IGkfrMod registered, bool enable)
        {
            // Try to toggle via registry if it's a legacy GKFR mod
            if (registered != null)
            {
                registered.Enabled = enable;
            }

            // Try to toggle the BepInEx plugin instance directly
            if (loaded != null && loaded.Instance != null)
            {
                loaded.Instance.enabled = enable;
            }

            // Toggle the file by renaming
            try
            {
                if (enable && file.EndsWith(".dll.disabled", StringComparison.OrdinalIgnoreCase))
                {
                    string dest = file.Substring(0, file.Length - ".disabled".Length);
                    if (!File.Exists(dest))
                    {
                        File.Move(file, dest);
                    }
                    Logger.LogInfo("Enabled plugin file " + Path.GetFileName(dest) + ". Restart if it was not loaded this session.");
                }
                else if (!enable && file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && !file.EndsWith(".dll.disabled", StringComparison.OrdinalIgnoreCase))
                {
                    string dest = file + ".disabled";
                    if (!File.Exists(dest))
                    {
                        File.Move(file, dest);
                    }
                    Logger.LogInfo("Disabled plugin file " + Path.GetFileName(file) + ".");
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not rename plugin file (it stays loaded until restart): " + ex.Message);
            }
        }

        static string StripDisabledSuffix(string name)
        {
            if (name.EndsWith(".dll.disabled", StringComparison.OrdinalIgnoreCase))
            {
                return name.Substring(0, name.Length - ".disabled".Length) + "  (disabled file)";
            }

            return name;
        }

        static PluginInfo FindLoaded(string file)
        {
            string full = Path.GetFullPath(file);
            foreach (var pair in Chainloader.PluginInfos)
            {
                PluginInfo info = pair.Value;
                if (info == null || string.IsNullOrEmpty(info.Location))
                {
                    continue;
                }

                try
                {
                    if (string.Equals(Path.GetFullPath(info.Location), full, StringComparison.OrdinalIgnoreCase))
                    {
                        return info;
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        static IGkfrMod FindRegistered(PluginInfo info)
        {
            if (info == null)
            {
                return null;
            }

            var mods = GkfrModRegistry.Mods;
            for (int i = 0; i < mods.Count; i++)
            {
                if (mods[i].Id == info.Metadata.GUID)
                {
                    return mods[i];
                }
            }

            return null;
        }

        void SetWindowOpen(bool open)
        {
            if (open == windowOpen)
            {
                return;
            }

            windowOpen = open;
            if (open)
            {
                cursorWasVisible = Cursor.visible;
                previousLock = Cursor.lockState;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.visible = cursorWasVisible;
                Cursor.lockState = previousLock;
            }
        }

        public static string FormatRaceMs(int ms)
        {
            if (ms <= 0)
            {
                return "--:--.---";
            }

            var span = TimeSpan.FromMilliseconds(ms);
            return string.Format("{0:00}:{1:00}.{2:000}", (int)span.TotalMinutes, span.Seconds, span.Milliseconds);
        }

        public static string FormatSignedMs(int ms)
        {
            if (ms == 0)
            {
                return "+0.000";
            }

            string sign = ms > 0 ? "+" : "-";
            int abs = ms < 0 ? -ms : ms;
            var span = TimeSpan.FromMilliseconds(abs);
            if (span.TotalMinutes >= 1.0)
            {
                return sign + string.Format("{0:00}:{1:00}.{2:000}", (int)span.TotalMinutes, span.Seconds, span.Milliseconds);
            }

            return sign + string.Format("{0:00}.{1:000}", span.Seconds, span.Milliseconds);
        }
    }
}
