using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace GKFR.GhostReplay
{
    // Record your best lap, replay it as a translucent kart you race against
    //
    //   F10  toggle ghost replay on/off
    //
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "gkfr.ghostreplay";
        public const string PluginName = "Tera GKFR Ghost Replay";
        public const string PluginVersion = "1.0.0";

        internal static Plugin Instance;

        ConfigEntry<bool> enabledEntry;
        ConfigEntry<KeyCode> toggleKey;
        ConfigEntry<KeyCode> recordKey;
        ConfigEntry<KeyCode> replayKey;
        ConfigEntry<float> ghostTransparency;
        
        bool ghostReplayActive;
        bool recording;
        
        // IGkfrMod metadata for the optional GKFR ModLoader window.
        public string Id => PluginGuid;
        public string DisplayName => "Ghost Replay";
        public string Description => "Record your best lap, replay it as a translucent kart.";

        public bool Enabled
        {
            get => enabledEntry.Value;
            set => enabledEntry.Value = value;
        }

        void Awake()
        {
            Instance = this;
            enabledEntry = Config.Bind("General", "Enabled", true, "Master switch. When off, F10 does nothing.");
            toggleKey = Config.Bind("General", "ToggleKey", KeyCode.F10, "Key to toggle ghost replay on/off.");
            recordKey = Config.Bind("Recording", "RecordKey", KeyCode.F11, "Key to start/stop recording");
            replayKey = Config.Bind("Recording", "ReplayKey", KeyCode.F2, "Key to play back ghost replay");
            ghostTransparency = Config.Bind("Display", "GhostTransparency", 0.5f, "Transparency level of ghost kart (0-1)");

            Logger.LogInfo("Tera GKFR Ghost Replay loaded. Toggle with " + toggleKey.Value + ".");
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey.Value) && Enabled)
            {
                ghostReplayActive = !ghostReplayActive;
                Logger.LogInfo("Ghost replay " + (ghostReplayActive ? "on" : "off"));
            }
            
            // Handle recording
            if (Input.GetKeyDown(recordKey.Value) && Enabled && ghostReplayActive)
            {
                if (!recording)
                {
                    StartRecording();
                }
                else
                {
                    StopRecording();
                }
            }
            
            // Handle replay
            if (Input.GetKeyDown(replayKey.Value) && Enabled && ghostReplayActive)
            {
                PlayReplay();
            }
        }

        void StartRecording()
        {
            recording = true;
            Logger.LogInfo("Starting ghost recording");
            // Implementation would need to record kart positions and movements
        }

        void StopRecording()
        {
            recording = false;
            Logger.LogInfo("Stopped ghost recording");
            // Implementation would need to save the recorded data
        }

        void PlayReplay()
        {
            Logger.LogInfo("Playing back ghost replay");
            // Implementation would need to create a translucent kart and replay positions
        }

        void OnDisable()
        {
            // Cleanup when disabled
            ghostReplayActive = false;
            recording = false;
        }
    }
}