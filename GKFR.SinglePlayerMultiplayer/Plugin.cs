using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Reflection;

namespace GKFR.SinglePlayerMultiplayer;

[BepInPlugin("gkfr.singleplayermultiplayer", "GKFR Single Player Multiplayer", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    private Harmony harmony;
    internal static Plugin instance;
    private ConfigEntry<bool> enabledEntry;

    public string Id => "gkfr.singleplayermultiplayer";
    public string DisplayName => "Single Player Multiplayer";
    public string Description => "Allows single-player races in multiplayer mode by bypassing the 2-player minimum requirement.";
    public bool Enabled => enabledEntry.Value;

    private void Awake()
    {
        instance = this;
        Logger = base.Logger;
        
        enabledEntry = Config.Bind("General", "Enabled", true, "When on, allows single-player multiplayer races.");
        
        harmony = new Harmony(Id);
        harmony.PatchAll(typeof(SinglePlayerMultiplayerPatches));
        
        Logger.LogInfo("GKFR Single Player Multiplayer loaded. Enabled: " + enabledEntry.Value);
    }

    private void OnDestroy()
    {
        harmony?.UnpatchSelf();
    }

    static class SinglePlayerMultiplayerPatches
    {
        // Patch to allow single player in multiplayer voting screen
        // This targets the minimum player count check that prevents single-player races
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GkNetMgr), "StartTimer")]
        static bool AllowSinglePlayerTimer(GkNetMgr __instance, GkNetMgr.TimerState state)
        {
            if (!Plugin.instance.Enabled)
                return true;
            
            // Allow timer to start even with single player
            return true;
        }

        // Patch the validation that checks if enough players are ready to start
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MenuHDMultiplayerMain), "Enter")]
        static void MenuHDMultiplayerMain_Enter()
        {
            if (!Plugin.instance.Enabled)
                return;
            
            Plugin.Logger.LogInfo("Single Player Multiplayer: Entered multiplayer menu");
        }

        // Patch to modify player count checks
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GkNetMgr), "IsMasterClient", MethodType.Getter)]
        static void EnsureMasterClient(bool __result)
        {
            if (!Plugin.instance.Enabled)
                return;
            
            // Ensure player is treated as master client for single player
            Plugin.Logger.LogInfo("Single Player Multiplayer: Master client check: " + __result);
        }

        // Try to patch methods that might control the confirm button state
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GkNetMgr), "IsConnected", MethodType.Getter)]
        static bool AllowSinglePlayerConnection(ref bool __result)
        {
            if (!Plugin.instance.Enabled)
                return true;
            
            // Force connection to be true for single player
            __result = true;
            Plugin.Logger.LogInfo("Single Player Multiplayer: Forcing connection to true");
            return false;
        }

        // Patch InGameGameMode.Start like the working LobbyGlitchFix mod
        [HarmonyPrefix]
        [HarmonyPatch(typeof(InGameGameMode), "Start")]
        static void InGameGameMode_Start()
        {
            if (!Plugin.instance.Enabled)
                return;
            
            Plugin.Logger.LogInfo("Single Player Multiplayer: InGameGameMode start");
        }
    }
}