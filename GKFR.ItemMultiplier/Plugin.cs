using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace GKFR.ItemMultiplier
{
    // This attribute tells BepInEx that this is a plugin
    // PluginGuid is a unique identifier for this plugin
    // PluginName is the display name shown in BepInEx logs
    // PluginVersion is the version number
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        // These are constants - values that never change
        // const means the value is fixed at compile time
        public const string PluginGuid = "gkfr.itemmultiplier";
        public const string PluginName = "Tera GKFR Item Multiplier";
        public const string PluginVersion = "1.0.0";

        // ConfigEntry<T> is a BepInEx type that stores configuration values
        // T is the type of the config value (bool for true/false, int for numbers, etc.)
        // These hold references to the configuration settings
        ConfigEntry<bool> enabledEntry;      // Stores whether the mod is enabled
        ConfigEntry<int> multiplierEntry;    // Stores the item multiplier value
        ConfigEntry<KeyCode> toggleKey;      // Stores the key to open the settings window

        // Harmony is the library we use to patch game code at runtime
        // It allows us to modify game functions without changing the original files
        Harmony harmony;

        // These are simple variables for the UI
        string multiplierText = "1";                 // Text shown in the UI for the multiplier
        Rect windowRect = new Rect(20f, 20f, 300f, 200f); // Position and size of the settings window
        bool showWindow;                             // Whether the settings window is currently visible

        // Static instance for patch access
        public static Plugin staticInstance;
        public static ManualLogSource PublicLogger;

        // Properties are like methods that look like variables
        // get allows reading the value, set allows writing it
        public string Id => PluginGuid;
        public string DisplayName => "Item Multiplier";
        public string Description => "Multiplies items from item boxes. Slider 1–10; textbox accepts any value.";

        // This property controls whether the mod is enabled
        public bool Enabled
        {
            get => enabledEntry.Value; // Return the value from config
            set
            {
                // Only proceed if the value is actually changing
                if (enabledEntry.Value == value)
                {
                    return;
                }

                // Update the config value
                enabledEntry.Value = value;
                // Apply or remove patches based on the new state
                ApplyPatches();
            }
        }

        // This property controls the multiplier value
        public int Multiplier
        {
            get => multiplierEntry.Value; // Return the value from config
            set
            {
                // Update the config value
                multiplierEntry.Value = value;
                // Update the UI text to match
                multiplierText = value.ToString();
            }
        }

        // Awake is called by Unity when the plugin is first loaded
        // This is where we set up our configuration and apply patches
        void Awake()
        {
            // Set up static references for patch access
            staticInstance = this;
            PublicLogger = Logger;

            // Config.Bind creates a configuration entry
            // Parameters: (section, key, default value, description)
            enabledEntry = Config.Bind("General", "Enabled", false, "Enable item multiplication from item boxes.");
            multiplierEntry = Config.Bind("General", "Multiplier", 1, "Item multiplier (1 = normal, 2 = double, etc.).");
            toggleKey = Config.Bind("General", "ToggleKey", KeyCode.F4, "Toggle item multiplier settings window.");

            // Initialize the UI text with the current multiplier value
            multiplierText = multiplierEntry.Value.ToString();

            // Apply the Harmony patches
            ApplyPatches();

            // Log a message to show the plugin loaded successfully
            Logger.LogInfo("Tera GKFR Item Multiplier loaded. Multiplier: " + Multiplier);
        }

        // Update is called every frame by Unity
        // We use it to check for key presses
        void Update()
        {
            // Input.GetKeyDown returns true only on the frame the key is pressed down
            if (Input.GetKeyDown(toggleKey.Value))
            {
                // Toggle the window visibility
                showWindow = !showWindow;
            }
        }

        // OnGUI is called by Unity for rendering the GUI (Graphical User Interface)
        // This is where we draw our settings window
        void OnGUI()
        {
            // Only draw the window if it should be visible
            if (!showWindow)
            {
                return;
            }

            // GUI.Window draws a draggable window
            // Parameters: (window ID, position, draw function, title)
            windowRect = GUI.Window(0x49494949, windowRect, DrawSettings, "Tera Item Multiplier  F4 close");
        }

        // This function draws the contents of the settings window
        // The 'id' parameter is the window ID passed by GUI.Window
        void DrawSettings(int id)
        {
            // Mathf.Clamp restricts a value to be between a minimum and maximum
            int sliderValue = Mathf.Clamp(Multiplier, 1, 10);

            // GUILayout.Label displays text
            GUILayout.Label("Item Multiplier: " + Multiplier + "x");

            // GUILayout.HorizontalSlider creates a horizontal slider control
            // It returns the current value of the slider
            int rounded = Mathf.RoundToInt(GUILayout.HorizontalSlider(sliderValue, 1f, 10f));

            // If the slider value changed, update the multiplier
            if (rounded != sliderValue)
            {
                Multiplier = rounded;
            }

            // GUILayout.BeginHorizontal starts a horizontal layout group
            // Elements inside will be arranged horizontally
            GUILayout.BeginHorizontal();

            GUILayout.Label("Custom Value", GUILayout.Width(90f));

            // GUILayout.TextField creates a text input field
            // Returns the text entered by the user
            multiplierText = GUILayout.TextField(multiplierText ?? "", GUILayout.Width(90f));

            // GUILayout.Button creates a clickable button
            // Returns true when the button is clicked
            if (GUILayout.Button("Apply", GUILayout.Width(80f)))
            {
                // int.TryParse tries to convert a string to a number
                // Returns true if successful, and puts the result in 'parsed'
                if (int.TryParse(multiplierText.Trim(), out int parsed) && parsed >= 1)
                {
                    Multiplier = parsed;
                }
                else
                {
                    // If parsing failed, reset the text to the current multiplier
                    multiplierText = Multiplier.ToString();
                }
            }

            // GUILayout.EndHorizontal ends the horizontal layout group
            GUILayout.EndHorizontal();

            // Another horizontal group for quick preset buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("1x")) Multiplier = 1;
            if (GUILayout.Button("2x")) Multiplier = 2;
            if (GUILayout.Button("3x")) Multiplier = 3;
            GUILayout.EndHorizontal();

            // GUILayout.Space adds vertical spacing
            GUILayout.Space(4f);

            // GUILayout.Toggle creates a checkbox
            bool nextEnabled = GUILayout.Toggle(Enabled, "Enabled");
            if (nextEnabled != Enabled)
            {
                Enabled = nextEnabled;
            }

            // GUI.DragWindow makes the window draggable by the title bar
            // The Rect specifies the area where dragging is allowed
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        // This function applies or removes Harmony patches based on the enabled state
        void ApplyPatches()
        {
            if (Enabled)
            {
                // If enabling and no harmony instance exists, create one and apply patches
                if (harmony == null)
                {
                    // Create a new Harmony instance with our plugin GUID
                    harmony = new Harmony(PluginGuid);
                    // PatchAll applies all methods marked with HarmonyPatch attributes
                    // in the specified class (ItemPatches)
                    harmony.PatchAll(typeof(ItemPatches));
                    Logger.LogInfo("Item multiplier patches applied successfully.");
                }
            }
            else if (harmony != null)
            {
                // If disabling and harmony instance exists, remove all patches
                harmony.UnpatchSelf();
                harmony = null;
                Logger.LogInfo("Item multiplier patches removed.");
            }
        }
    }

    // This static class contains our Harmony patches
    // Static means the class belongs to the type itself, not to instances
    static class ItemPatches
    {
        // This patch modifies the KartBonusMgr.DoSetItem method
        // We use a postfix to modify the quantity after it's been set
        [HarmonyPostfix]
        [HarmonyPatch(typeof(KartBonusMgr), "DoSetItem")]
        static void MultiplyItemQuantityPostfix(KartBonusMgr __instance, int driverId, byte byteItem, bool needToWaitAnim, int iQuantity, int byPassSlot)
        {
            var plugin = Plugin.staticInstance;
            if (plugin == null || !plugin.Enabled || iQuantity <= 0)
            {
                return;
            }

            try
            {
                // Get the m_ItemTab field
                var itemTabField = AccessTools.Field(typeof(KartBonusMgr), "m_ItemTab");
                if (itemTabField == null)
                {
                    Plugin.PublicLogger?.LogWarning("Could not find m_ItemTab field");
                    return;
                }

                var itemTab = itemTabField.GetValue(__instance);
                if (itemTab == null)
                {
                    return;
                }

                // Handle as array - use reflection to access KartBonus[]
                var kartBonusType = AccessTools.TypeByName("KartBonus");
                if (kartBonusType == null)
                {
                    Plugin.PublicLogger?.LogWarning("Could not find KartBonus type");
                    return;
                }

                // Create array of KartBonus if needed
                if (itemTab.GetType().IsArray)
                {
                    var itemArray = (Array)itemTab;
                    for (int i = 0; i < itemArray.Length; i++)
                    {
                        var kartBonus = itemArray.GetValue(i);
                        if (kartBonus != null)
                        {
                            // Get the current quantity field
                            var quantityField = AccessTools.Field(kartBonusType, "m_iQuantity");
                            if (quantityField == null)
                            {
                                continue;
                            }

                            int currentQuantity = (int)quantityField.GetValue(kartBonus);
                            
                            if (currentQuantity > 0)
                            {
                                // Multiply the quantity
                                int newQuantity = currentQuantity * plugin.Multiplier;
                                quantityField.SetValue(kartBonus, newQuantity);
                                
                                Plugin.PublicLogger?.LogInfo($"Multiplied item quantity from {currentQuantity} to {newQuantity} in slot {i}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.PublicLogger?.LogWarning("Failed to multiply item quantity: " + ex.Message);
            }
        }
    }
}
