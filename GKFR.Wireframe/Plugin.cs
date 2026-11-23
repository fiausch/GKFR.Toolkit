using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace GKFR.Wireframe
{
    // Draws wireframe overlays so you can see the level's collision and geometry.
    //
    //   F9   toggle the overlay on/off
    //   F10  cycle the view mode:  All  ->  Invisible walls  ->  Death walls  ->  All
    //   F12  toggle single-colour wireframe - draw everything in one colour
    //        (SolidColorTint) instead of colour-coded by type
    //
    // Modes:
    //   All            Every visible model is drawn as a wireframe, plus every collider,
    //                  each colour-coded by type (see the legend in HOW_TO_USE.txt).
    //   Invisible walls  Only colliders that have no visible mesh of their own (out-of-bounds
    //                  zones, invisible barriers, helper-only collision), colour-coded by type.
    //   Death walls    Only colliders on the game's death mask (ColKill / ColKillHole /
    //                  ColKillWater, plus whatever the live kart physics marks as lethal),
    //                  drawn in the death colour.
    //
    // Colours and which layers are drawn are all configurable in gkfr.wireframe.cfg.
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "gkfr.wireframe";
        public const string PluginName = "Tera GKFR Wireframe";
        public const string PluginVersion = "1.3.0";

        internal static Plugin Instance;

        // View modes cycled by F10.
        const int ModeAll = 0;
        const int ModeInvisible = 1;
        const int ModeDeath = 2;
        static readonly string[] ModeNames = { "All (models + colliders)", "Invisible walls", "Death walls" };

        ConfigEntry<bool> enabledEntry;
        ConfigEntry<KeyCode> toggleKey;
        ConfigEntry<KeyCode> modeKey;
        ConfigEntry<bool> drawModels;
        ConfigEntry<bool> drawTriggers;
        ConfigEntry<bool> drawWalls;
        ConfigEntry<bool> drawRoad;
        ConfigEntry<bool> drawVehicles;
        ConfigEntry<bool> drawOther;
        ConfigEntry<bool> drawThroughGeometry;
        ConfigEntry<int> maxMeshVerts;
        ConfigEntry<Color> modelColor;
        ConfigEntry<Color> deathColor;
        ConfigEntry<Color> triggerColor;
        ConfigEntry<Color> invisibleColor;
        ConfigEntry<Color> wallColor;
        ConfigEntry<Color> roadColor;
        ConfigEntry<Color> vehicleColor;
        ConfigEntry<Color> otherColor;
        ConfigEntry<KeyCode> solidColorKey;
        ConfigEntry<bool> solidColor;
        ConfigEntry<Color> solidColorTint;

        Material lineMat;
        bool active;
        int mode = ModeAll;
        float refreshTimer;
        int deathMask;          // combined named + live death mask
        readonly List<Entry> models = new List<Entry>();
        readonly List<Entry> colliders = new List<Entry>();

        // IGkfrMod metadata for the optional GKFR ModLoader window.
        public string Id => PluginGuid;
        public string DisplayName => "Wireframe colliders";
        public string Description => "Wireframe of all models + colliders. F9 toggles, F10 cycles All / invisible walls / death walls. Colour-coded.";

        public bool Enabled
        {
            get => enabledEntry.Value;
            set => enabledEntry.Value = value;
        }

        void Awake()
        {
            Instance = this;
            enabledEntry = Config.Bind("General", "Enabled", true, "Master switch. When off, F9 does nothing.");
            toggleKey = Config.Bind("General", "ToggleKey", KeyCode.F9, "Key to toggle the wireframe overlay.");
            modeKey = Config.Bind("General", "ModeKey", KeyCode.F10, "Key to cycle the view mode (All / Invisible walls / Death walls).");
            drawModels = Config.Bind("Layers", "DrawModels", true, "Draw visible meshes as wireframe (All mode only). Turn off if it runs slow.");
            drawTriggers = Config.Bind("Layers", "DrawTriggers", true, "Draw trigger colliders (event zones) in All mode.");
            drawWalls = Config.Bind("Layers", "DrawWalls", true, "Draw ColWall colliders in All mode.");
            drawRoad = Config.Bind("Layers", "DrawRoad", false, "Draw road-surface colliders in All mode (noisy).");
            drawVehicles = Config.Bind("Layers", "DrawVehicles", false, "Draw kart / vehicle colliders in All mode.");
            drawOther = Config.Bind("Layers", "DrawOther", true, "Draw other environment / item colliders in All mode.");
            drawThroughGeometry = Config.Bind("Render", "DrawThroughGeometry", true, "If true, lines draw on top of geometry so walls behind things stay visible.");
            maxMeshVerts = Config.Bind("Render", "MaxMeshVerts", 6000, "Meshes with more vertices than this are drawn as a bounding box instead of full wireframe (keeps it fast).");
            modelColor = Config.Bind("Colors", "ModelColor", new Color(0.6f, 0.8f, 1f, 0.8f), "Visible model / mesh wireframe (All mode).");
            deathColor = Config.Bind("Colors", "DeathColor", new Color(1f, 0f, 0f, 1f), "Death wall - a collider on the game's death mask (ColKill / ColKillHole / ColKillWater).");
            triggerColor = Config.Bind("Colors", "TriggerColor", new Color(1f, 0.5f, 0f, 1f), "Trigger collider with a visible mesh (event zone).");
            invisibleColor = Config.Bind("Colors", "InvisibleColor", new Color(1f, 0.1f, 1f, 1f), "Invisible barrier - a solid collider with no visible mesh.");
            wallColor = Config.Bind("Colors", "WallColor", new Color(0.2f, 1f, 1f, 1f), "Visible wall (ColWall with a mesh).");
            roadColor = Config.Bind("Colors", "RoadColor", new Color(0.6f, 0.6f, 0.6f, 0.6f), "Road-surface collider.");
            vehicleColor = Config.Bind("Colors", "VehicleColor", new Color(1f, 0.8f, 0.2f, 1f), "Vehicle / kart collider.");
            otherColor = Config.Bind("Colors", "OtherColor", new Color(0.3f, 1f, 0.4f, 0.85f), "Other colliders (items, misc).");
            solidColorKey = Config.Bind("General", "SolidColorKey", KeyCode.F12, "Key to toggle single-colour wireframe (everything in one colour instead of colour-coded).");
            solidColor = Config.Bind("Render", "SolidColor", false, "Draw the whole wireframe in one colour (SolidColorTint) instead of colour-coded by type.");
            solidColorTint = Config.Bind("Colors", "SolidColorTint", Color.white, "Colour used for everything when SolidColor is on.");

            lineMat = new Material(Shader.Find("Hidden/Internal-Colored"));
            lineMat.hideFlags = HideFlags.HideAndDontSave;
            ApplyMaterialSettings();

            Camera.onPostRender += OnPostRender;

            Logger.LogInfo("Tera GKFR Wireframe loaded. Toggle " + toggleKey.Value + ", cycle mode " + modeKey.Value + ".");
        }

        void ApplyMaterialSettings()
        {
            if (lineMat == null)
            {
                return;
            }
            lineMat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            lineMat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            lineMat.SetInt("_Cull", (int)CullMode.Off);
            lineMat.SetInt("_ZWrite", 0);
            lineMat.SetInt("_ZTest", (int)(drawThroughGeometry.Value ? CompareFunction.Always : CompareFunction.LessEqual));
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey.Value) && Enabled)
            {
                active = !active;
                if (active)
                {
                    RebuildCache();
                }
                Logger.LogInfo("Wireframe " + (active ? "on" : "off"));
            }

            if (Input.GetKeyDown(modeKey.Value) && Enabled && active)
            {
                mode = (mode + 1) % 3;
                Logger.LogInfo("Wireframe mode: " + ModeNames[mode]);
            }

            if (Input.GetKeyDown(solidColorKey.Value) && Enabled && active)
            {
                solidColor.Value = !solidColor.Value;
                Logger.LogInfo("Wireframe solid colour: " + (solidColor.Value ? "on" : "off"));
            }

            if (!Enabled && active)
            {
                active = false;
            }

            if (active)
            {
                refreshTimer -= Time.deltaTime;
                if (refreshTimer <= 0f)
                {
                    refreshTimer = 2f;
                    RebuildCache();
                }
            }
        }

        void OnGUI()
        {
            if (!active || !Enabled)
            {
                return;
            }
            var prev = GUI.color;
            GUI.color = new Color(0.2f, 1f, 1f, 0.9f);
            GUI.Label(new Rect(16f, 40f, 780f, 24f), "WIREFRAME  -  " + ModeNames[mode] + "  -  F10 mode  -  F12 solid " + (solidColor.Value ? "ON" : "off") + "  -  F9 off");
            GUI.color = prev;
        }

        void OnDisable()
        {
            active = false;
            models.Clear();
            colliders.Clear();
        }

        void OnDestroy()
        {
            Camera.onPostRender -= OnPostRender;
            if (lineMat != null)
            {
                Object.Destroy(lineMat);
                lineMat = null;
            }
        }

        void OnPostRender(Camera cam)
        {
            if (!Enabled || !active)
            {
                return;
            }
            // Only draw for enabled perspective cameras that render to the screen.
            if (cam == null || !cam.enabled || cam.orthographic || cam.targetTexture != null)
            {
                return;
            }

            ApplyMaterialSettings();
            if (!lineMat.SetPass(0))
            {
                return;
            }

            GL.PushMatrix();
            GL.LoadProjectionMatrix(cam.projectionMatrix);
            // Unity leaves the camera's view matrix on the modelview stack here; reset first so
            // we don't double it and shift everything.
            GL.LoadIdentity();
            GL.MultMatrix(cam.worldToCameraMatrix);
            GL.Begin(GL.LINES);

            if (mode == ModeAll)
            {
                if (drawModels.Value)
                {
                    DrawList(models);
                }
                for (int i = 0; i < colliders.Count; i++)
                {
                    if (ShouldDrawColliderAll(colliders[i]))
                    {
                        Emit(colliders[i]);
                    }
                }
            }
            else if (mode == ModeInvisible)
            {
                for (int i = 0; i < colliders.Count; i++)
                {
                    if (colliders[i].noRenderer)
                    {
                        Emit(colliders[i]);
                    }
                }
            }
            else // ModeDeath
            {
                for (int i = 0; i < colliders.Count; i++)
                {
                    if (colliders[i].cat == CatDeath)
                    {
                        Emit(colliders[i]);
                    }
                }
            }

            GL.End();
            GL.PopMatrix();
        }

        void DrawList(List<Entry> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Emit(list[i]);
            }
        }

        void Emit(Entry e)
        {
            if (e.verts == null || e.verts.Length < 2)
            {
                return;
            }
            // Solid-colour mode overrides the per-category colour with one tint.
            GL.Color(solidColor.Value ? solidColorTint.Value : e.color);
            Vector3[] v = e.verts;
            for (int j = 0; j < v.Length; j++)
            {
                GL.Vertex(v[j]);
            }
        }

        bool ShouldDrawColliderAll(Entry e)
        {
            switch (e.cat)
            {
                case CatDeath:
                case CatInvisible:
                    return true; // the headline categories - always on in All mode
                case CatTrigger:
                    return drawTriggers.Value;
                case CatWall:
                    return drawWalls.Value;
                case CatRoad:
                    return drawRoad.Value;
                case CatVehicle:
                    return drawVehicles.Value;
                default:
                    return drawOther.Value;
            }
        }

        void RebuildCache()
        {
            refreshTimer = 2f;
            RefreshDeathMask();
            models.Clear();
            colliders.Clear();

            if (drawModels.Value)
            {
                Renderer[] renderers = Object.FindObjectsOfType<Renderer>();
                for (int i = 0; i < renderers.Length; i++)
                {
                    Entry e = BuildModelEntry(renderers[i]);
                    if (e != null)
                    {
                        models.Add(e);
                    }
                }
            }

            Collider[] all = Object.FindObjectsOfType<Collider>();
            for (int i = 0; i < all.Length; i++)
            {
                Entry e = BuildColliderEntry(all[i]);
                if (e != null)
                {
                    colliders.Add(e);
                }
            }
        }

        void RefreshDeathMask()
        {
            // Named death layers (always available, even in menus): ColKill, ColKillHole, ColKillWater.
            int mask = (1 << LayerMask.NameToLayer("ColKill"))
                     | (1 << LayerMask.NameToLayer("ColKillHole"))
                     | (1 << LayerMask.NameToLayer("ColKillWater"));

            // Augment with the live death mask from any kart physics in the scene (covers any
            // extra layers the kart prefab marks as lethal).
            var kp = Object.FindObjectOfType<RcKinematicPhysic>();
            if (kp != null)
            {
                mask |= kp.DeathMaskValue;
            }
            deathMask = mask;
        }

        bool IsDeath(Collider c)
        {
            return deathMask != 0 && (deathMask & (1 << c.gameObject.layer)) != 0;
        }

        Entry BuildModelEntry(Renderer r)
        {
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
            {
                return null;
            }
            int layer = r.gameObject.layer;
            string ln = LayerMask.LayerToName(layer);
            if (ln == "UI" || ln == "Wheel" || ln == "WheelNoColl")
            {
                return null;
            }

            Mesh mesh = GetMesh(r);
            if (mesh == null)
            {
                return null;
            }

            Vector3[] verts;
            if (mesh.vertexCount > maxMeshVerts.Value)
            {
                verts = BoundsVerts(r.bounds);
            }
            else
            {
                Vector3[] mv = MeshEdges(mesh, r.transform.localToWorldMatrix);
                verts = mv ?? BoundsVerts(r.bounds);
            }

            if (verts == null || verts.Length < 2)
            {
                return null;
            }
            return new Entry { verts = verts, color = modelColor.Value, cat = CatModel };
        }

        static Mesh GetMesh(Renderer r)
        {
            if (r is SkinnedMeshRenderer smr)
            {
                return smr.sharedMesh;
            }
            if (r is MeshRenderer)
            {
                MeshFilter mf = r.GetComponent<MeshFilter>();
                return mf != null ? mf.sharedMesh : null;
            }
            return null;
        }

        Entry BuildColliderEntry(Collider c)
        {
            if (c == null || !c.enabled || !c.gameObject.activeInHierarchy)
            {
                return null;
            }

            int layer = c.gameObject.layer;
            string ln = LayerMask.LayerToName(layer);
            bool isTrigger = c.isTrigger;
            bool hasRenderer = c.GetComponent<Renderer>() != null;

            // Wheels and UI are pure noise.
            if (ln == "Wheel" || ln == "WheelNoColl" || ln == "UI")
            {
                return null;
            }

            int cat;
            Color color;
            if (IsDeath(c))
            {
                cat = CatDeath;
                color = deathColor.Value;
            }
            else if (isTrigger)
            {
                cat = CatTrigger;
                color = triggerColor.Value;
            }
            else if (!hasRenderer)
            {
                cat = CatInvisible;
                color = invisibleColor.Value;
            }
            else if (ln == "ColWall")
            {
                cat = CatWall;
                color = wallColor.Value;
            }
            else if (IsRoad(ln))
            {
                cat = CatRoad;
                color = roadColor.Value;
            }
            else if (ln == "Vehicle" || ln == "VehicleNoColl")
            {
                cat = CatVehicle;
                color = vehicleColor.Value;
            }
            else
            {
                cat = CatOther;
                color = otherColor.Value;
            }

            Vector3[] verts = BuildVerts(c);
            if (verts == null || verts.Length < 2)
            {
                return null;
            }

            return new Entry { verts = verts, color = color, cat = cat, noRenderer = !hasRenderer };
        }

        Vector3[] BuildVerts(Collider c)
        {
            if (c is BoxCollider box)
            {
                return BoxVerts(c.transform.localToWorldMatrix, box.center, box.size);
            }
            if (c is SphereCollider sphere)
            {
                return SphereVerts(c.transform, sphere.center, sphere.radius);
            }
            if (c is CapsuleCollider capsule)
            {
                return CapsuleVerts(c.transform, capsule.center, capsule.radius, capsule.height, capsule.direction);
            }
            if (c is MeshCollider mesh && mesh.sharedMesh != null)
            {
                Vector3[] mv = MeshEdges(mesh.sharedMesh, c.transform.localToWorldMatrix);
                if (mv != null)
                {
                    return mv;
                }
            }
            return BoundsVerts(c.bounds);
        }

        static readonly int[] BoxEdges =
        {
            0, 1, 1, 2, 2, 3, 3, 0, // bottom
            4, 5, 5, 6, 6, 7, 7, 4, // top
            0, 4, 1, 5, 2, 6, 3, 7  // sides
        };

        static Vector3[] BoxVerts(Matrix4x4 l2w, Vector3 center, Vector3 size)
        {
            Vector3 h = size * 0.5f;
            Vector3[] c = new Vector3[8];
            c[0] = l2w.MultiplyPoint(center + new Vector3(-h.x, -h.y, -h.z));
            c[1] = l2w.MultiplyPoint(center + new Vector3(h.x, -h.y, -h.z));
            c[2] = l2w.MultiplyPoint(center + new Vector3(h.x, -h.y, h.z));
            c[3] = l2w.MultiplyPoint(center + new Vector3(-h.x, -h.y, h.z));
            c[4] = l2w.MultiplyPoint(center + new Vector3(-h.x, h.y, -h.z));
            c[5] = l2w.MultiplyPoint(center + new Vector3(h.x, h.y, -h.z));
            c[6] = l2w.MultiplyPoint(center + new Vector3(h.x, h.y, h.z));
            c[7] = l2w.MultiplyPoint(center + new Vector3(-h.x, h.y, h.z));

            Vector3[] v = new Vector3[BoxEdges.Length];
            for (int i = 0; i < BoxEdges.Length; i++)
            {
                v[i] = c[BoxEdges[i]];
            }
            return v;
        }

        static Vector3[] BoundsVerts(Bounds b)
        {
            Vector3 min = b.min;
            Vector3 max = b.max;
            Vector3[] c = new Vector3[8];
            c[0] = new Vector3(min.x, min.y, min.z);
            c[1] = new Vector3(max.x, min.y, min.z);
            c[2] = new Vector3(max.x, min.y, max.z);
            c[3] = new Vector3(min.x, min.y, max.z);
            c[4] = new Vector3(min.x, max.y, min.z);
            c[5] = new Vector3(max.x, max.y, min.z);
            c[6] = new Vector3(max.x, max.y, max.z);
            c[7] = new Vector3(min.x, max.y, max.z);

            Vector3[] v = new Vector3[BoxEdges.Length];
            for (int i = 0; i < BoxEdges.Length; i++)
            {
                v[i] = c[BoxEdges[i]];
            }
            return v;
        }

        static Vector3[] SphereVerts(Transform t, Vector3 center, float radius)
        {
            Vector3 worldCenter = t.TransformPoint(center);
            Vector3 s = t.lossyScale;
            float r = radius * Mathf.Max(Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y)), Mathf.Abs(s.z));
            List<Vector3> verts = new List<Vector3>(3 * 24 * 2);
            AppendCircle(verts, worldCenter, r, Vector3.right, Vector3.up);
            AppendCircle(verts, worldCenter, r, Vector3.right, Vector3.forward);
            AppendCircle(verts, worldCenter, r, Vector3.up, Vector3.forward);
            return verts.ToArray();
        }

        static Vector3[] CapsuleVerts(Transform t, Vector3 center, float radius, float height, int direction)
        {
            Vector3 axis = direction == 0 ? Vector3.right : (direction == 1 ? Vector3.up : Vector3.forward);
            Vector3 worldCenter = t.TransformPoint(center);
            Vector3 worldAxis = t.TransformDirection(axis);
            Vector3 perpA, perpB;
            if (Mathf.Abs(Vector3.Dot(worldAxis, Vector3.up)) > 0.99f)
            {
                perpA = Vector3.right;
                perpB = Vector3.forward;
            }
            else
            {
                perpA = Vector3.up;
                perpB = Vector3.Cross(worldAxis, perpA).normalized;
            }

            Vector3 s = t.lossyScale;
            float r = radius * Mathf.Max(Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y)), Mathf.Abs(s.z));
            float half = (height * 0.5f - radius);
            Vector3 top = worldCenter + worldAxis * half;
            Vector3 bot = worldCenter - worldAxis * half;

            List<Vector3> verts = new List<Vector3>();
            AppendCircle(verts, top, r, perpA, perpB);
            AppendCircle(verts, bot, r, perpA, perpB);
            Vector3[] dirs = { perpA, -perpA, perpB, -perpB };
            for (int i = 0; i < dirs.Length; i++)
            {
                verts.Add(top + dirs[i] * r);
                verts.Add(bot + dirs[i] * r);
            }
            return verts.ToArray();
        }

        static void AppendCircle(List<Vector3> verts, Vector3 c, float r, Vector3 a, Vector3 b)
        {
            const int seg = 24;
            Vector3 prev = c + r * a;
            for (int i = 1; i <= seg; i++)
            {
                float ang = (i / (float)seg) * Mathf.PI * 2f;
                Vector3 cur = c + r * (Mathf.Cos(ang) * a + Mathf.Sin(ang) * b);
                verts.Add(prev);
                verts.Add(cur);
                prev = cur;
            }
        }

        static Vector3[] MeshEdges(Mesh m, Matrix4x4 l2w)
        {
            if (m == null || m.vertexCount == 0)
            {
                return null;
            }
            int[] tris = m.triangles;
            Vector3[] lv = m.vertices;
            if (tris == null || tris.Length == 0 || lv == null)
            {
                return null;
            }

            Vector3[] w = new Vector3[lv.Length];
            for (int i = 0; i < lv.Length; i++)
            {
                w[i] = l2w.MultiplyPoint(lv[i]);
            }

            HashSet<long> seen = new HashSet<long>();
            List<Vector3> verts = new List<Vector3>();
            for (int i = 0; i < tris.Length; i += 3)
            {
                int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                AddEdge(seen, verts, w, a, b);
                AddEdge(seen, verts, w, b, c);
                AddEdge(seen, verts, w, c, a);
            }
            return verts.ToArray();
        }

        static void AddEdge(HashSet<long> seen, List<Vector3> verts, Vector3[] w, int a, int b)
        {
            if (a == b)
            {
                return;
            }
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (seen.Add(key))
            {
                verts.Add(w[a]);
                verts.Add(w[b]);
            }
        }

        static bool IsRoad(string layerName)
        {
            switch (layerName)
            {
                case "ColMainTarmac":
                case "ColMainDirt":
                case "ColGrass":
                case "ColMud":
                case "ColDirt":
                case "ColSnow":
                case "ColWood":
                    return true;
                default:
                    return false;
            }
        }

        // Collider categories (used for colour and All-mode filtering).
        const int CatModel = 0;
        const int CatDeath = 1;
        const int CatTrigger = 2;
        const int CatInvisible = 3;
        const int CatWall = 4;
        const int CatRoad = 5;
        const int CatVehicle = 6;
        const int CatOther = 7;

        class Entry
        {
            public Vector3[] verts;
            public Color color;
            public int cat;
            public bool noRenderer; // collider has no Renderer on its own GameObject (an invisible wall)
        }
    }
}
