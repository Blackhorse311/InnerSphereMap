using BattleTech;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace InnerSphereMap {

    // Vanilla shows system names only in the single hover tooltip. On a
    // 3,000+ system map that leaves the player blind, so we attach small
    // world-space TextMesh labels to systems near the player's current
    // position plus a configured always-on list (faction capitals).
    [HarmonyPatch(typeof(StarmapRenderer), "RefreshSystems")]
    public static class StarmapRenderer_RefreshSystems_Labels_Patch {

        private const string LabelName = "AIEmpiresSystemLabel";
        private static readonly Color CapitalColor = new Color(0.854f, 0.647f, 0.125f);
        private static readonly Color NearbyColor = new Color(0.85f, 0.85f, 0.85f);
        private static Font _labelFont;

        private static Font LabelFont {
            get {
                if (_labelFont == null) {
                    _labelFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                return _labelFont;
            }
        }

        static void Postfix(StarmapRenderer __instance) {
            try {
                Settings settings = InnerSphereMap.SETTINGS;
                bool proximityOn = settings.ProximityLabelRadiusLy > 0f;
                if (!proximityOn && settings.alwaysLabeledSystems.Count == 0) {
                    return;
                }

                SimGameState sim = (SimGameState)AccessTools.Field(typeof(Starmap), "sim").GetValue(__instance.starmap);
                if (sim == null || sim.CurSystem == null) {
                    return;
                }
                FakeVector3 curPos = sim.CurSystem.Def.Position;
                var here = new Vector3(curPos.x, curPos.y, 0f);
                float radiusSq = settings.ProximityLabelRadiusLy * settings.ProximityLabelRadiusLy;
                var alwaysOn = new HashSet<string>(settings.alwaysLabeledSystems);

                var systemDictionary = (Dictionary<GameObject, StarmapSystemRenderer>)ReflectionHelper.GetPrivateField(__instance, "systemDictionary");
                foreach (StarmapSystemRenderer renderer in systemDictionary.Values) {
                    StarSystem system = renderer.system?.System;
                    if (system == null) {
                        continue;
                    }
                    bool isCapital = alwaysOn.Contains(system.Name);
                    bool isNear = false;
                    if (proximityOn && !isCapital) {
                        FakeVector3 pos = system.Def.Position;
                        var delta = new Vector3(pos.x - here.x, pos.y - here.y, 0f);
                        isNear = delta.sqrMagnitude <= radiusSq;
                    }
                    SetLabel(renderer, system, sim, isNear || isCapital, isCapital, settings);
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
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
                labelObject.transform.SetParent(renderer.transform, false);
                labelObject.transform.localPosition = new Vector3(0f, -1.4f, -0.2f);
                textMesh = labelObject.AddComponent<TextMesh>();
                textMesh.font = LabelFont;
                labelObject.GetComponent<MeshRenderer>().material = LabelFont.material;
                textMesh.anchor = TextAnchor.UpperCenter;
                textMesh.alignment = TextAlignment.Center;
                textMesh.characterSize = 0.28f;
                textMesh.fontSize = 32;
            }
            else {
                labelObject = existing.gameObject;
                textMesh = labelObject.GetComponent<TextMesh>();
                if (textMesh == null) {
                    return;
                }
            }

            string text = system.Name;
            if (settings.ShowLabelDifficulty) {
                int difficulty = system.Def.GetDifficulty(sim.SimGameMode);
                text += "  [" + difficulty + "]";
            }
            textMesh.text = text;
            textMesh.color = isCapital ? CapitalColor : NearbyColor;
            labelObject.SetActive(true);
        }
    }
}
