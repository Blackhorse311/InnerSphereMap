using BattleTech;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace InnerSphereMap {

    // Vanilla shows system names only in the single hover tooltip. We attach
    // world-space TextMesh labels to systems near the CAMERA's view center
    // (re-evaluated as the player pans) plus a configured always-on list
    // (faction capitals).
    public static class SystemLabels {

        private const string LabelName = "AIEmpiresSystemLabel";
        private static readonly Color CapitalColor = new Color(0.854f, 0.647f, 0.125f);
        private static readonly Color NearbyColor = new Color(0.85f, 0.85f, 0.85f);
        private static Font _labelFont;
        internal static Vector3 LastLabelCenter = new Vector3(float.MaxValue, 0f, 0f);

        internal static Font LabelFont {
            get {
                if (_labelFont == null) {
                    _labelFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                return _labelFont;
            }
        }

        internal static void RefreshLabels(StarmapRenderer renderer, Vector3 viewCenter) {
            Settings settings = InnerSphereMap.SETTINGS;
            SimGameState sim = (SimGameState)AccessTools.Field(typeof(Starmap), "sim").GetValue(renderer.starmap);
            if (sim == null) {
                return;
            }
            float radiusSq = settings.ViewLabelRadius * settings.ViewLabelRadius;
            var alwaysOn = new HashSet<string>(settings.alwaysLabeledSystems);
            HashSet<string> jumpSet = settings.JumpLabelDepth > 0
                ? SystemsWithinJumps(renderer.starmap, sim, settings.JumpLabelDepth)
                : null;

            var systemDictionary = (Dictionary<GameObject, StarmapSystemRenderer>)ReflectionHelper.GetPrivateField(renderer, "systemDictionary");
            foreach (StarmapSystemRenderer systemRenderer in systemDictionary.Values) {
                StarSystem system = systemRenderer.system?.System;
                if (system == null) {
                    continue;
                }
                bool isCapital = alwaysOn.Contains(system.Name);
                bool isNear = jumpSet != null && jumpSet.Contains(system.ID);
                if (!isNear && !isCapital && settings.ViewLabelRadius > 0f) {
                    Vector3 pos = systemRenderer.transform.position;
                    float dx = pos.x - viewCenter.x;
                    float dy = pos.y - viewCenter.y;
                    isNear = dx * dx + dy * dy <= radiusSq;
                }
                SetLabel(systemRenderer, system, sim, isNear || isCapital, isCapital, settings);
            }
        }

        private static HashSet<string> SystemsWithinJumps(Starmap starmap, SimGameState sim, int depth) {
            var reached = new HashSet<string>();
            StarSystemNode start = starmap.GetSystemByID(sim.CurSystem.ID);
            if (start == null) {
                return reached;
            }
            var frontier = new List<StarSystemNode> { start };
            reached.Add(start.System.ID);
            for (int hop = 0; hop < depth; hop++) {
                var next = new List<StarSystemNode>();
                foreach (StarSystemNode node in frontier) {
                    foreach (StarSystemNode adjacent in node.AdjacentSystems) {
                        if (reached.Add(adjacent.System.ID)) {
                            next.Add(adjacent);
                        }
                    }
                }
                frontier = next;
            }
            return reached;
        }

        private static void SetLabel(StarmapSystemRenderer renderer, StarSystem system, SimGameState sim, bool show, bool isCapital, Settings settings) {
            Transform existing = renderer.transform.Find(LabelName);
            if (!show) {
                if (existing != null) {
                    existing.gameObject.SetActive(false);
                }
                return;
            }

            TextMesh textMesh;
            GameObject labelObject;
            if (existing == null) {
                labelObject = new GameObject(LabelName);
                // Must live on the starmap render layer: the starmap camera culls
                // Default-layer objects while other scene cameras (mech bay)
                // happily render them mid-hangar.
                labelObject.layer = renderer.gameObject.layer;
                labelObject.transform.SetParent(renderer.transform, false);
                textMesh = labelObject.AddComponent<TextMesh>();
                textMesh.font = LabelFont;
                labelObject.GetComponent<MeshRenderer>().material = LabelFont.material;
                textMesh.anchor = TextAnchor.UpperCenter;
                textMesh.alignment = TextAlignment.Center;
            }
            else {
                labelObject = existing.gameObject;
                textMesh = labelObject.GetComponent<TextMesh>();
                if (textMesh == null) {
                    return;
                }
            }

            // Renderers scale up when selected/visited; counter it so labels
            // keep a constant world size.
            float parentScale = renderer.transform.lossyScale.x;
            if (parentScale > 0.001f) {
                float inverse = 1f / parentScale;
                labelObject.transform.localScale = new Vector3(inverse, inverse, inverse);
            }
            labelObject.transform.localPosition = new Vector3(0f, settings.LabelOffsetY, -0.2f);
            // Rasterize near on-screen size: oversized glyph textures alias
            // badly when downsampled to small labels.
            textMesh.fontSize = 64;
            textMesh.characterSize = settings.LabelCharacterSize;

            string text = ResolveRenamed(system.Name, sim.CurrentDate.Year);
            if (settings.ShowLabelDifficulty) {
                int difficulty = system.Def.GetDifficulty(sim.SimGameMode);
                text += "\n" + DifficultyRow(difficulty);
            }
            textMesh.text = text;
            float b = settings.LabelBrightness;
            Color baseColor = isCapital ? CapitalColor : NearbyColor;
            textMesh.color = new Color(baseColor.r * b, baseColor.g * b, baseColor.b * b, settings.LabelOpacity);
            labelObject.SetActive(true);
        }

        // Careers serialize system defs at creation, so saves made before the
        // era-aware rename fix still carry raw mm-data ids like
        // "Untran (Achtur 2822+)". Resolve at display time against the current
        // in-game year — which also flips names live when a career crosses
        // the rename date.
        private static readonly Regex RenameRe = new Regex(@"^(.+?) \((.+?) (\d{3,4})\+\)$", RegexOptions.Compiled);

        private static string ResolveRenamed(string name, int year) {
            Match m = RenameRe.Match(name);
            if (!m.Success) {
                return name;
            }
            return year >= int.Parse(m.Groups[3].Value) ? m.Groups[2].Value : m.Groups[1].Value;
        }

        private static bool? _skullGlyphAvailable;

        // BT reads difficulty in half-skull steps; render real skulls when
        // the bundled font has the glyph, otherwise fall back to [n].
        private static string DifficultyRow(int difficulty) {
            if (_skullGlyphAvailable == null) {
                _skullGlyphAvailable = LabelFont.HasCharacter('☠');
            }
            if (_skullGlyphAvailable != true) {
                return "[" + difficulty + "]";
            }
            int full = difficulty / 2;
            bool half = difficulty % 2 == 1;
            var row = new System.Text.StringBuilder();
            for (int i = 0; i < full; i++) {
                row.Append('☠');
            }
            if (half) {
                row.Append('½');
            }
            return row.Length > 0 ? row.ToString() : "½";
        }
    }

    [HarmonyPatch(typeof(StarmapRenderer), "RefreshSystems")]
    public static class StarmapRenderer_RefreshSystems_Labels_Patch {

        static void Postfix(StarmapRenderer __instance) {
            try {
                // Force a label pass on map open/refresh at the current camera.
                SystemLabels.LastLabelCenter = new Vector3(float.MaxValue, 0f, 0f);
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(StarmapRenderer), "Update")]
    public static class StarmapRenderer_Update_ViewLabels_Patch {

        static void Postfix(StarmapRenderer __instance) {
            try {
                Settings settings = InnerSphereMap.SETTINGS;
                if (settings.ViewLabelRadius <= 0f && settings.JumpLabelDepth <= 0 && settings.alwaysLabeledSystems.Count == 0) {
                    return;
                }
                Camera camera = __instance.starmapCamera;
                if (camera == null) {
                    return;
                }
                Vector3 center = camera.transform.position;
                center.z = 0f;
                float threshold = Mathf.Max(settings.ViewLabelRadius * 0.25f, 1f);
                float dx = center.x - SystemLabels.LastLabelCenter.x;
                float dy = center.y - SystemLabels.LastLabelCenter.y;
                if (dx * dx + dy * dy < threshold * threshold) {
                    return;
                }
                SystemLabels.LastLabelCenter = center;
                SystemLabels.RefreshLabels(__instance, center);
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }
}
