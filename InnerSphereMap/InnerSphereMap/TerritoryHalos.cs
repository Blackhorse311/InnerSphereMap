using BattleTech;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace InnerSphereMap {

    // First-pass territory delineation: a soft owner-colored disc behind every
    // owned system. Dense same-owner regions blend into colored clouds, so
    // faction areas and their seams read at a glance without computing real
    // border polylines.
    [HarmonyPatch(typeof(StarmapRenderer), "RefreshSystems")]
    public static class StarmapRenderer_RefreshSystems_Halos_Patch {

        private const string HaloName = "AIEmpiresTerritoryHalo";
        private static Material _haloMaterial;
        private static Texture2D _haloTexture;
        private static bool _shaderMissingLogged;

        // Radial gradient with alpha falling to zero at the edge — gives a
        // soft round glow instead of a hard opaque square, independent of
        // whether the shader honors _Color alpha.
        private static Texture2D HaloTexture {
            get {
                if (_haloTexture == null) {
                    const int size = 64;
                    _haloTexture = new Texture2D(size, size, TextureFormat.ARGB32, false);
                    float half = (size - 1) / 2f;
                    for (int y = 0; y < size; y++) {
                        for (int x = 0; x < size; x++) {
                            float dx = (x - half) / half;
                            float dy = (y - half) / half;
                            float distance = Mathf.Sqrt(dx * dx + dy * dy);
                            float alpha = Mathf.Clamp01(1f - distance);
                            alpha *= alpha;
                            _haloTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                        }
                    }
                    _haloTexture.Apply();
                }
                return _haloTexture;
            }
        }

        private static Material HaloMaterial {
            get {
                if (_haloMaterial == null) {
                    Shader shader = Shader.Find("Sprites/Default");
                    if (shader != null) {
                        _haloMaterial = new Material(shader) { mainTexture = HaloTexture };
                    }
                }
                return _haloMaterial;
            }
        }

        static void Postfix(StarmapRenderer __instance) {
            try {
                Settings settings = InnerSphereMap.SETTINGS;
                if (settings.TerritoryHaloSize <= 0f) {
                    return;
                }
                if (HaloMaterial == null) {
                    if (!_shaderMissingLogged) {
                        _shaderMissingLogged = true;
                        Logger.LogLine("TerritoryHalos disabled: Sprites/Default shader not found");
                    }
                    return;
                }

                var systemDictionary = (Dictionary<GameObject, StarmapSystemRenderer>)ReflectionHelper.GetPrivateField(__instance, "systemDictionary");
                foreach (StarmapSystemRenderer renderer in systemDictionary.Values) {
                    StarSystem system = renderer.system?.System;
                    if (system == null) {
                        continue;
                    }
                    string owner = system.OwnerValue.Name;
                    bool owned = owner != "NoFaction" && owner != "Locals" && owner != "INVALID_UNSET";
                    SetHalo(renderer, owned, settings);
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }

        private static void SetHalo(StarmapSystemRenderer renderer, bool owned, Settings settings) {
            Transform existing = renderer.transform.Find(HaloName);
            if (!owned) {
                if (existing != null) {
                    existing.gameObject.SetActive(false);
                }
                return;
            }

            GameObject halo;
            if (existing == null) {
                halo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                halo.name = HaloName;
                halo.layer = renderer.gameObject.layer;
                // The primitive ships with a MeshCollider that would swallow
                // map clicks meant for the system underneath.
                UnityEngine.Object.Destroy(halo.GetComponent<Collider>());
                halo.GetComponent<MeshRenderer>().material = HaloMaterial;
                halo.transform.SetParent(renderer.transform, false);
                halo.transform.localPosition = new Vector3(0f, 0f, 0.5f);
            }
            else {
                halo = existing.gameObject;
            }

            float parentScale = renderer.transform.lossyScale.x;
            float size = settings.TerritoryHaloSize;
            if (parentScale > 0.001f) {
                size /= parentScale;
            }
            halo.transform.localScale = new Vector3(size, size, 1f);

            Color color = renderer.systemColor;
            color.a = settings.TerritoryHaloOpacity;
            halo.GetComponent<MeshRenderer>().material.color = color;
            halo.SetActive(true);
        }
    }
}
