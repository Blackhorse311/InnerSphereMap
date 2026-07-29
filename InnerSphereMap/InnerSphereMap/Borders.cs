using BattleTech;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace InnerSphereMap {

    // Faction border lines. Ownership is rasterized onto a coarse grid (each
    // system stamps a distance-weighted vote into nearby cells; best vote
    // wins), then every edge between cells with different owners emits a line
    // segment colored by the owning side — adjacent factions draw their own
    // color on their own side, like a printed atlas. All segments go into one
    // vertex-colored Lines-topology mesh.
    [HarmonyPatch(typeof(StarmapRenderer), "RefreshSystems")]
    public static class StarmapRenderer_RefreshSystems_Borders_Patch {

        private const string RootName = "AIEmpiresBorders";
        private const int StampRadius = 2;
        private static Material _lineMaterial;

        private static Material LineMaterial {
            get {
                if (_lineMaterial == null) {
                    Shader shader = Shader.Find("Sprites/Default");
                    if (shader != null) {
                        _lineMaterial = new Material(shader);
                    }
                }
                return _lineMaterial;
            }
        }

        static void Postfix(StarmapRenderer __instance) {
            try {
                Settings settings = InnerSphereMap.SETTINGS;
                GameObject root = GameObject.Find(RootName);
                if (settings.BorderOpacity <= 0f) {
                    if (root != null) {
                        root.SetActive(false);
                    }
                    return;
                }
                if (LineMaterial == null) {
                    return;
                }

                var systemDictionary = (Dictionary<GameObject, StarmapSystemRenderer>)ReflectionHelper.GetPrivateField(__instance, "systemDictionary");

                int resolution = Mathf.Clamp(settings.BorderGridResolution, 32, 400);
                float halfMap = Mathf.Max(settings.MapWidth, settings.MapHeight) + 1f;
                float cellSize = halfMap * 2f / resolution;

                var ownerGrid = new int[resolution * resolution];
                var voteGrid = new float[resolution * resolution];
                var factionColors = new List<Color> { Color.clear }; // index 0 = unowned
                var factionIndex = new Dictionary<string, int>();

                foreach (StarmapSystemRenderer renderer in systemDictionary.Values) {
                    StarSystem system = renderer.system?.System;
                    if (system == null) {
                        continue;
                    }
                    string owner = system.OwnerValue.Name;
                    if (owner == "NoFaction" || owner == "Locals" || owner == "INVALID_UNSET") {
                        continue;
                    }
                    if (!factionIndex.TryGetValue(owner, out int index)) {
                        index = factionColors.Count;
                        factionIndex[owner] = index;
                        Color c = renderer.systemColor;
                        c.a = settings.BorderOpacity;
                        factionColors.Add(c);
                    }
                    Vector3 pos = renderer.transform.position;
                    int cx = Mathf.FloorToInt((pos.x + halfMap) / cellSize);
                    int cy = Mathf.FloorToInt((pos.y + halfMap) / cellSize);
                    for (int oy = -StampRadius; oy <= StampRadius; oy++) {
                        for (int ox = -StampRadius; ox <= StampRadius; ox++) {
                            int gx = cx + ox;
                            int gy = cy + oy;
                            if (gx < 0 || gy < 0 || gx >= resolution || gy >= resolution) {
                                continue;
                            }
                            float weight = 1f / (1f + ox * ox + oy * oy);
                            int cell = gy * resolution + gx;
                            if (weight > voteGrid[cell]) {
                                voteGrid[cell] = weight;
                                ownerGrid[cell] = index;
                            }
                        }
                    }
                }

                var vertices = new List<Vector3>();
                var colors = new List<Color>();
                // A cell edge between different owners emits a segment per
                // owned side, nudged inward so two-faction borders show both
                // colors side by side.
                for (int y = 0; y < resolution; y++) {
                    for (int x = 0; x < resolution; x++) {
                        int self = ownerGrid[y * resolution + x];
                        if (x + 1 < resolution) {
                            EmitEdge(self, ownerGrid[y * resolution + x + 1], x, y, true, halfMap, cellSize, factionColors, vertices, colors);
                        }
                        if (y + 1 < resolution) {
                            EmitEdge(self, ownerGrid[(y + 1) * resolution + x], x, y, false, halfMap, cellSize, factionColors, vertices, colors);
                        }
                        if (vertices.Count > 60000) {
                            break;
                        }
                    }
                }

                if (root == null) {
                    root = new GameObject(RootName);
                    root.layer = __instance.gameObject.layer;
                    root.transform.SetParent(__instance.transform, false);
                    root.AddComponent<MeshFilter>();
                    root.AddComponent<MeshRenderer>().material = LineMaterial;
                }
                MeshFilter filter = root.GetComponent<MeshFilter>();
                if (filter.sharedMesh != null) {
                    UnityEngine.Object.Destroy(filter.sharedMesh);
                }
                var mesh = new Mesh { name = "AIEmpiresBordersMesh" };
                mesh.SetVertices(vertices);
                mesh.SetColors(colors);
                var indices = new int[vertices.Count];
                for (int i = 0; i < indices.Length; i++) {
                    indices[i] = i;
                }
                mesh.SetIndices(indices, MeshTopology.Lines, 0);
                filter.sharedMesh = mesh;
                root.SetActive(true);
                Logger.LogLine($"Borders: {vertices.Count / 2} segments, {factionIndex.Count} factions, grid {resolution}");
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }

        private static void EmitEdge(int ownerA, int ownerB, int x, int y, bool vertical, float halfMap, float cellSize, List<Color> factionColors, List<Vector3> vertices, List<Color> colors) {
            if (ownerA == ownerB) {
                return;
            }
            // Shared edge between cell (x,y) and its +x (vertical edge) or +y
            // (horizontal edge) neighbor, in world space.
            float worldX = x * cellSize - halfMap;
            float worldY = y * cellSize - halfMap;
            const float inset = 0.12f;
            const float z = 0.3f;

            if (ownerA != 0) {
                Color color = factionColors[ownerA];
                float offset = -inset * cellSize;
                AddSegment(vertices, colors, worldX, worldY, vertical, cellSize, offset, z, color);
            }
            if (ownerB != 0) {
                Color color = factionColors[ownerB];
                float offset = inset * cellSize;
                AddSegment(vertices, colors, worldX, worldY, vertical, cellSize, offset, z, color);
            }
        }

        private static void AddSegment(List<Vector3> vertices, List<Color> colors, float worldX, float worldY, bool vertical, float cellSize, float offset, float z, Color color) {
            if (vertical) {
                float edgeX = worldX + cellSize + offset;
                vertices.Add(new Vector3(edgeX, worldY, z));
                vertices.Add(new Vector3(edgeX, worldY + cellSize, z));
            }
            else {
                float edgeY = worldY + cellSize + offset;
                vertices.Add(new Vector3(worldX, edgeY, z));
                vertices.Add(new Vector3(worldX + cellSize, edgeY, z));
            }
            colors.Add(color);
            colors.Add(color);
        }
    }
}
