using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace GKFR.Noclip
{
    // Free-fly noclip for the local player's kart. The kart passes through walls and floors
    // and can be flown anywhere. Respawns and the under-floor snap-back are suppressed while
    // active. The mouse gives full 3D rotation (pitch + yaw); an optional first-person view
    // parks the camera on the kart so the rotation is a true flycam.
    //
    // Controls (while noclip is on, during a race):
    //   F8            toggle noclip on/off (configurable)
    //   F11           toggle first-person camera on/off (only while noclip is on)
    //   W / S         fly forward / back (along where the kart faces, including pitch)
    //   A / D         strafe left / right
    //   Space / Ctrl  fly up / down (world up)
    //   Shift         hold to fly faster
    //   Mouse         yaw (X) + pitch (Y) - full rotation
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "gkfr.noclip";
        public const string PluginName = "Tera GKFR Noclip";
        public const string PluginVersion = "1.1.0";

        internal static Plugin Instance;

        ConfigEntry<bool> enabledEntry;
        ConfigEntry<KeyCode> toggleKey;
        ConfigEntry<float> moveSpeed;
        ConfigEntry<float> fastMoveSpeed;
        ConfigEntry<float> lookSensitivity;
        ConfigEntry<bool> alsoStrafeWithArrows;
        ConfigEntry<KeyCode> firstPersonKey;
        ConfigEntry<float> firstPersonHeight;
        ConfigEntry<float> firstPersonForward;
        ConfigEntry<bool> invertLookY;

        Harmony harmony;

        // Runtime state read by the Harmony patches.
        internal static bool NoclipActive;
        internal static Kart LocalKart;

        // Per-kart state we change and must restore.
        Kart activeKart;
        Rigidbody activeBody;
        Driver activeDriver;
        List<Collider> disabledColliders;
        bool wasKinematic;
        bool usedGravity;
        bool detectedCollisions;
        CursorLockMode prevLock;
        bool prevCursorVisible;
        bool cursorCaptured;
        bool yawInited;
        float yaw;
        float pitch;
        bool firstPerson;
        bool cameraOverridden;

        // So the IGkfrMod metadata (shown by the optional GKFR ModLoader window) matches the others.
        public string Id => PluginGuid;
        public string DisplayName => "Noclip";
        public string Description => "Fly the local kart through walls and floors. F8 toggles, F11 first-person. WASD move, Space/Ctrl up/down, Shift faster, mouse rotates (pitch+yaw).";

        public bool Enabled
        {
            get => enabledEntry.Value;
            set => enabledEntry.Value = value;
        }

        void Awake()
        {
            Instance = this;
            enabledEntry = Config.Bind("General", "Enabled", true, "Master switch. When off, F8 does nothing and the kart behaves normally.");
            toggleKey = Config.Bind("General", "ToggleKey", KeyCode.F1, "Key to toggle noclip on/off.");
            moveSpeed = Config.Bind("Movement", "MoveSpeed", 25f, "Fly speed in units per second.");
            fastMoveSpeed = Config.Bind("Movement", "FastMoveSpeed", 90f, "Fly speed while holding Shift.");
            lookSensitivity = Config.Bind("Movement", "LookSensitivity", 3f, "How fast the mouse turns the kart.");
            alsoStrafeWithArrows = Config.Bind("Movement", "StrafeWithArrows", true, "If true, Left/Right arrows also strafe (in addition to A/D).");
            firstPersonKey = Config.Bind("General", "FirstPersonKey", KeyCode.F11, "Key to toggle the first-person camera while noclip is on.");
            firstPersonHeight = Config.Bind("FirstPerson", "Height", 0.8f, "How far above the kart the first-person camera sits.");
            firstPersonForward = Config.Bind("FirstPerson", "Forward", 0.3f, "How far in front of the kart the first-person camera sits.");
            invertLookY = Config.Bind("Movement", "InvertLookY", false, "If true, moving the mouse up looks down (inverted flight controls).");

            harmony = new Harmony(PluginGuid);
            harmony.PatchAll(typeof(Patches));

            Logger.LogInfo("Tera GKFR Noclip loaded. Toggle " + toggleKey.Value + ", first-person " + firstPersonKey.Value + ".");
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey.Value) && Enabled)
            {
                NoclipActive = !NoclipActive;
                Logger.LogInfo("Noclip " + (NoclipActive ? "on" : "off"));
                if (!NoclipActive)
                {
                    CleanupCurrent();
                }
            }

            if (Input.GetKeyDown(firstPersonKey.Value) && Enabled && NoclipActive)
            {
                firstPerson = !firstPerson;
                Logger.LogInfo("Noclip first-person: " + (firstPerson ? "on" : "off"));
                if (!firstPerson)
                {
                    RestoreCamera();
                }
            }

            // Master switch off forces noclip off.
            if (!Enabled && NoclipActive)
            {
                NoclipActive = false;
                CleanupCurrent();
            }

            if (NoclipActive && Enabled)
            {
                RunNoclipFrame();
            }
        }

        void OnDisable()
        {
            // The optional ModLoader can disable this plugin at runtime. Undo everything.
            if (NoclipActive)
            {
                NoclipActive = false;
                CleanupCurrent();
            }
        }

        void OnDestroy()
        {
            if (NoclipActive)
            {
                NoclipActive = false;
                CleanupCurrent();
            }
            if (harmony != null)
            {
                harmony.UnpatchSelf();
                harmony = null;
            }
            Camera.onPostRender -= NoclipHUD.Draw;
        }

        Kart FindLocalKart()
        {
            var gm = Singleton<GameManager>.Instance;
            if (gm == null || gm.GameMode == null)
            {
                return null;
            }

            foreach (var pair in gm.GameMode.Drivers)
            {
                Driver d = pair.Value;
                if (d == null || d.Kart == null || d.IsAi)
                {
                    continue;
                }

                if (d.IsLocal)
                {
                    return d.Kart;
                }
            }

            // Fallback used elsewhere in the codebase (HUD treats LocalHumanId 0 as "the" player).
            foreach (var pair in gm.GameMode.Drivers)
            {
                Driver d = pair.Value;
                if (d == null || d.Kart == null || d.IsAi)
                {
                    continue;
                }

                if (d.LocalHumanId == 0)
                {
                    return d.Kart;
                }
            }

            return null;
        }

        void RunNoclipFrame()
        {
            Kart kart = FindLocalKart();
            LocalKart = kart;

            if (kart == null)
            {
                // Between races / in a menu: nothing to grab onto, release anything we held.
                if (activeKart != null)
                {
                    CleanupCurrent();
                }
                return;
            }

            // New or different kart (e.g. a new race started): re-attach.
            if (activeKart != kart)
            {
                CleanupCurrent();
                SetupCurrent(kart);
            }

            if (activeBody == null)
            {
                return;
            }

            // Re-assert physics state every frame in case another system resets it.
            activeBody.isKinematic = true;
            activeBody.useGravity = false;
            activeBody.detectCollisions = false;
            activeBody.velocity = Vector3.zero;
            activeBody.angularVelocity = Vector3.zero;

            // Keep the mouse captured so mouse delta drives rotation.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            cursorCaptured = true;

            Transform bodyTr = activeBody.transform;

            // Initialise yaw + pitch from the kart's current orientation the first time we grab it.
            if (!yawInited)
            {
                Vector3 e = bodyTr.rotation.eulerAngles;
                yaw = e.y;
                pitch = e.x;
                if (pitch > 180f) pitch -= 360f;
                yawInited = true;
            }

            // Full rotation: yaw from Mouse X, pitch from Mouse Y.
            float mx = Input.GetAxisRaw("Mouse X") * lookSensitivity.Value;
            float my = Input.GetAxisRaw("Mouse Y") * lookSensitivity.Value;
            yaw += mx;
            pitch += invertLookY.Value ? my : -my;
            pitch = Mathf.Clamp(pitch, -85f, 85f);
            bodyTr.rotation = Quaternion.Euler(pitch, yaw, 0f);

            // Movement - full 3D along where the kart now faces (pitching + W flies up/down).
            bool fast = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            float speed = (fast ? fastMoveSpeed.Value : moveSpeed.Value) * Time.deltaTime;

            Vector3 forward = bodyTr.forward;
            Vector3 right = bodyTr.right;

            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                move += forward;
            }
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                move -= forward;
            }
            bool strafeLeft = Input.GetKey(KeyCode.A) || (alsoStrafeWithArrows.Value && Input.GetKey(KeyCode.LeftArrow));
            bool strafeRight = Input.GetKey(KeyCode.D) || (alsoStrafeWithArrows.Value && Input.GetKey(KeyCode.RightArrow));
            if (strafeLeft)
            {
                move -= right;
            }
            if (strafeRight)
            {
                move += right;
            }
            if (Input.GetKey(KeyCode.Space))
            {
                move += Vector3.up;
            }
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                move += Vector3.down;
            }

            bodyTr.position += move * speed;

            // Keep the wrong-way HUD quiet while we ignore the track.
            if (kart.Hud != null && kart.Hud.Position != null)
            {
                kart.Hud.Position.WrongWay = false;
            }
        }

        void SetupCurrent(Kart kart)
        {
            activeKart = kart;
            activeDriver = kart.Driver;
            activeBody = kart.GetVehiclePhysic() != null ? kart.GetVehiclePhysic().GetVehicleBody() : null;
            if (activeBody == null)
            {
                return;
            }

            wasKinematic = activeBody.isKinematic;
            usedGravity = activeBody.useGravity;
            detectedCollisions = activeBody.detectCollisions;

            // Disable every collider on the kart so it truly passes through walls and floors.
            disabledColliders = new List<Collider>();
            Collider[] cols = activeBody.transform.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null && cols[i].enabled)
                {
                    cols[i].enabled = false;
                    disabledColliders.Add(cols[i]);
                }
            }

            activeBody.isKinematic = true;
            activeBody.useGravity = false;
            activeBody.detectCollisions = false;
            activeBody.velocity = Vector3.zero;
            activeBody.angularVelocity = Vector3.zero;

            // Invulnerable so stray items / attacks don't interrupt the flight.
            if (activeDriver != null)
            {
                try { activeDriver.SetInvulnerabilityActive(true); } catch { }
            }

            // Capture the mouse for turning.
            prevLock = Cursor.lockState;
            prevCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            cursorCaptured = true;

            Camera.onPostRender -= NoclipHUD.Draw;
            Camera.onPostRender += NoclipHUD.Draw;
        }

        void CleanupCurrent()
        {
            Camera.onPostRender -= NoclipHUD.Draw;
            RestoreCamera();
            firstPerson = false;

            if (cursorCaptured)
            {
                Cursor.lockState = prevLock;
                Cursor.visible = prevCursorVisible;
                cursorCaptured = false;
            }

            if (activeBody != null)
            {
                activeBody.isKinematic = wasKinematic;
                activeBody.useGravity = usedGravity;
                activeBody.detectCollisions = detectedCollisions;
                activeBody.velocity = Vector3.zero;
                activeBody.angularVelocity = Vector3.zero;
            }

            if (disabledColliders != null)
            {
                for (int i = 0; i < disabledColliders.Count; i++)
                {
                    if (disabledColliders[i] != null)
                    {
                        disabledColliders[i].enabled = true;
                    }
                }
                disabledColliders = null;
            }

            if (activeDriver != null)
            {
                try { activeDriver.SetInvulnerabilityActive(false); } catch { }
                // Drop the stale under-floor snapshot so the anti-under-floor check in
                // Driver.FixedUpdate doesn't snap the kart when noclip ends.
                try
                {
                    var last = AccessTools.FieldRefAccess<Driver, Vector3?>("m_lastFramePosition");
                    if (last != null)
                    {
                        last(activeDriver) = null;
                    }
                    var acc = AccessTools.FieldRefAccess<Driver, int>("m_underFloorAcc");
                    if (acc != null)
                    {
                        acc(activeDriver) = 0;
                    }
                }
                catch { }
            }

            activeKart = null;
            activeBody = null;
            activeDriver = null;
            yawInited = false;
            LocalKart = null;
        }

        // Small on-screen hint so it's obvious noclip is on.
        void OnGUI()
        {
            if (!NoclipActive || !Enabled || activeKart == null)
            {
                return;
            }
            NoclipHUD.DrawHint();
        }

        void LateUpdate()
        {
            if (NoclipActive && Enabled && firstPerson && activeBody != null)
            {
                ApplyFirstPerson();
            }
            else if (cameraOverridden)
            {
                RestoreCamera();
            }
        }

        void ApplyFirstPerson()
        {
            Camera cam = activeKart != null ? activeKart.AttachedCamera : null;
            if (cam == null)
            {
                cam = Camera.main;
            }
            if (cam == null || activeBody == null)
            {
                return;
            }

            // Stop the game's camera drivers (CameraBase + the Cinemachine brain) from fighting
            // our manual camera position while first-person is on.
            var cb = cam.GetComponent<CameraBase>();
            if (cb != null && cb.enabled)
            {
                cb.enabled = false;
            }
            var brain = cam.GetComponent("CinemachineBrain") as Behaviour;
            if (brain != null && brain.enabled)
            {
                brain.enabled = false;
            }

            Transform b = activeBody.transform;
            cam.transform.position = b.position + b.up * firstPersonHeight.Value + b.forward * firstPersonForward.Value;
            cam.transform.rotation = b.rotation;
            cameraOverridden = true;
        }

        void RestoreCamera()
        {
            if (!cameraOverridden)
            {
                return;
            }
            Camera cam = activeKart != null ? activeKart.AttachedCamera : null;
            if (cam == null)
            {
                cam = Camera.main;
            }
            if (cam != null)
            {
                var cb = cam.GetComponent<CameraBase>();
                if (cb != null && !cb.enabled)
                {
                    cb.enabled = true;
                }
                var brain = cam.GetComponent("CinemachineBrain") as Behaviour;
                if (brain != null && !brain.enabled)
                {
                    brain.enabled = true;
                }
            }
            cameraOverridden = false;
        }
    }

    // Keeps HUD drawing out of the main plugin file.
    internal static class NoclipHUD
    {
        public static void DrawHint()
        {
            var prev = GUI.color;
            GUI.color = new Color(1f, 0.78f, 0.18f, 0.9f);
            GUI.Label(new Rect(16f, 12f, 760f, 24f), "NOCLIP on  -  WASD move  -  Space/Ctrl up/down  -  Shift fast  -  mouse rotate  -  F11 1st-person  -  F8 off");
            GUI.color = prev;
        }

        // Reserved hook (kept for parity with the wireframe mod's camera callback style).
        public static void Draw(Camera cam) { }
    }

    // Patches that keep the game from respawning / snapping the kart while noclip is on.
    internal static class Patches
    {
        static bool IsActiveFor(RcVehicle vehicle)
        {
            return Plugin.Instance != null && Plugin.Instance.Enabled && Plugin.NoclipActive && vehicle == Plugin.LocalKart;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Kart), nameof(Kart.Respawn))]
        static bool Kart_Respawn(Kart __instance)
        {
            if (IsActiveFor(__instance))
            {
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(RcRespawnChecker), "CheckRespawn")]
        static bool RespawnChecker_CheckRespawn(RcVehicle pVehicle, ref bool __result)
        {
            if (IsActiveFor(pVehicle))
            {
                __result = false;
                return false;
            }
            return true;
        }

        // The anti-under-floor raycast in Driver.FixedUpdate snaps the kart back to the road
        // surface after ~20 frames underground, which would fight a noclip that flies below
        // the track. Skip it entirely for the local kart while noclip is on.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Driver), "FixedUpdate")]
        static bool Driver_FixedUpdate(Driver __instance)
        {
            if (Plugin.Instance == null || !Plugin.Instance.Enabled || !Plugin.NoclipActive)
            {
                return true;
            }
            if (Plugin.LocalKart != null && __instance == Plugin.LocalKart.Driver)
            {
                return false;
            }
            return true;
        }
    }
}
