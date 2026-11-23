using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace GKFR.LapTimes
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "gkfr.laptimes";
        public const string PluginName = "Tera GKFR Lap Times";
        public const string PluginVersion = "2.1.0";

        ConfigEntry<bool> enabledEntry;
        ConfigEntry<KeyCode> overlayKey;
        Rect windowRect = new Rect(Screen.width - 400f, 150f, 400f, 310f);

        public string Id => PluginGuid;
        public string DisplayName => "Race Standings";
        public string Description => "Shows placement, speed, lap times, and gap behind 1st for all racers. F3 toggles the overlay.";

        public bool Enabled
        {
            get => enabledEntry.Value;
            set => enabledEntry.Value = value;
        }

        void Awake()
        {
            enabledEntry = Config.Bind("General", "Enabled", true, "Master switch for the overlay. F3 toggles it.");
            overlayKey = Config.Bind("General", "OverlayKey", KeyCode.F3, "Show or hide the overlay during a race.");
            Logger.LogInfo("Tera GKFR Lap Times loaded. F3 toggles the overlay. Shows placement, speed, lap times, and gap behind 1st.");
        }

        void Update()
        {
            if (Input.GetKeyDown(overlayKey.Value))
            {
                Enabled = !Enabled;
            }
        }

        void OnGUI()
        {
            if (!Enabled || !ShouldDraw())
            {
                return;
            }

            // Position window on right side under lap counter area
            windowRect.x = Screen.width - windowRect.width - 20f;
            windowRect = GUI.Window(0x4C415054, windowRect, DrawOverlay, "");
        }

        bool ShouldDraw()
        {
            var gameManager = Singleton<GameManager>.Instance;
            if (gameManager == null || gameManager.GameMode == null)
            {
                return false;
            }

            E_GameState state = gameManager.GameMode.GameState;
            if (state != E_GameState.Race && state != E_GameState.Start && state != E_GameState.End)
            {
                return false;
            }

            return true;
        }

        void DrawOverlay(int id)
        {
            var rows = BuildRows();
            
            // Header
            GUILayout.BeginHorizontal();
            GUILayout.Label("#", GUILayout.Width(25f));
            GUILayout.Label("Name", GUILayout.Width(90f));
            GUILayout.Label("Speed", GUILayout.Width(70f));
            GUILayout.Label("Lap", GUILayout.Width(60f));
            GUILayout.Label("Last Lap", GUILayout.Width(70f));
            GUILayout.Label("Gap", GUILayout.Width(60f));
            GUILayout.EndHorizontal();
            
            GUILayout.Space(2f);

            // Rows sorted by placement (descending = 1st first)
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label(row.RankText, GUILayout.Width(25f));
                GUILayout.Label(row.Name, GUILayout.Width(90f));
                GUILayout.Label(row.SpeedText, GUILayout.Width(70f));
                GUILayout.Label(row.LapText, GUILayout.Width(60f));
                GUILayout.Label(row.LastText, GUILayout.Width(70f));
                GUILayout.Label(row.GapText, GUILayout.Width(70f));
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(4f);
            GUILayout.Label("F3 to toggle");
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        List<Row> BuildRows()
        {
            var rows = new List<Row>();
            var gameManager = Singleton<GameManager>.Instance;
            if (gameManager == null || gameManager.GameMode == null)
            {
                return rows;
            }

            // Find the leader (1st place)
            Driver leader = null;
            float leaderDist = 0f;

            foreach (var pair in gameManager.GameMode.Drivers)
            {
                Driver driver = pair.Value;
                if (!IsListed(driver))
                {
                    continue;
                }

                var stats = driver.Kart.RaceStats;
                if (leader == null || stats.GetRank() < leader.Kart.RaceStats.GetRank())
                {
                    leader = driver;
                    leaderDist = stats.GetDistToEndOfRace();
                }
            }

            // Build rows for all drivers
            foreach (var pair in gameManager.GameMode.Drivers)
            {
                Driver driver = pair.Value;
                if (!IsListed(driver))
                {
                    continue;
                }

                var stats = driver.Kart.RaceStats;
                float speed = driver.Kart.GetLocalSpeed();
                int last = stats.GetLastLapTime();
                
                // Calculate gap behind leader
                string gapText = "—";
                if (leader != null && driver != leader)
                {
                    float distBehind = stats.GetDistToEndOfRace() - leaderDist;
                    float gapSeconds = distBehind / Mathf.Max(1f, speed);
                    gapText = "+" + gapSeconds.ToString("F1") + "s";
                }

                rows.Add(new Row
                {
                    Rank = stats.GetRank(),
                    RankText = (stats.GetRank() + 1).ToString(),
                    Name = GetName(driver),
                    SpeedText = speed.ToString("F0") + " km/h",
                    LapText = (stats.GetNbLapCompleted() < 4 ? stats.GetNbLapCompleted().ToString() + "/3" : "Done"),
                    LastText = GKFR.ModLoader.Plugin.FormatRaceMs(last),
                    GapText = gapText
                });
            }

            // Sort by placement descending (1st place first)
            rows.Sort((a, b) => a.Rank.CompareTo(b.Rank));
            return rows;
        }

        bool IsListed(Driver driver)
        {
            if (driver == null || driver.Kart == null || driver.Kart.RaceStats == null)
            {
                return false;
            }

            return true;
        }

        static string GetName(Driver driver)
        {
            var list = Singleton<GameConfigurator>.Instance.PlayerDataList;
            if (list != null && list.TryGetValue(driver.Id, out PlayerData data) && data != null && !string.IsNullOrEmpty(data.Pseudo))
            {
                return data.Pseudo;
            }

            return driver.IsAi ? "CPU " + driver.Id : "P" + (driver.Id + 1);
        }

        class Row
        {
            public int Rank;
            public string RankText;
            public string Name;
            public string SpeedText;
            public string LapText;
            public string LastText;
            public string GapText;
        }
    }
}
