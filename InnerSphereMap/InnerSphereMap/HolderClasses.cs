using BattleTech;
using BattleTech.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace InnerSphereMap {
    public class Settings {

        public float MinFov; // this is the vertical FOV
        public float MaxFov; // this is the vertical FOV

        public float MapWidth;
        public float MapHeight;

        public float MapTopViewBuffer;
        public float MapLeftViewBuffer;
        public float MapRightViewBuffer;
        public float MapBottomViewBuffer;

        public string splashTitle = "";
        public string splashText = "";

        public List<LogoItem> logos = new List<LogoItem>();
        public bool reducedClanLogos = true;

        // AIEmpires fork: system labels follow the camera — systems within
        // ViewLabelRadius (world units) of the view center get name labels,
        // re-evaluated as the player pans. 0 disables. Capitals in
        // alwaysLabeledSystems are always labeled.
        public float ViewLabelRadius = 0f;
        public bool ShowLabelDifficulty = true;
        public List<string> alwaysLabeledSystems = new List<string>();

        // AIEmpires fork: camera distance from the map plane. Vanilla parks the
        // camera at z=-100; larger MapWidth/Height needs a proportionally
        // farther camera or max zoom-out can no longer frame the whole sphere.
        public float CameraDistance = 100f;

        // AIEmpires fork: visual tuning that must scale with MapWidth/Height.
        public float LabelCharacterSize = 0.28f;
        // Added to the template's base scale (legacy behavior was original+4;
        // multiplying instead explodes crest size when the template's base
        // scale is not 1).
        public float LogoScaleAdd = 4f;
        public float LogoOpacity = 1f;
        public bool logoTintByFaction;

        // AIEmpires fork: scale crests to their faction's territory extent so
        // 17 Kerensky-cluster clans don't wear house-sized crests. Factor 0
        // keeps the legacy fixed LogoScaleAdd.
        public float LogoExtentFactor = 0f;
        public float LogoMinScaleAdd = 1f;
        public float LogoMaxScaleAdd = 4f;
        // Factions whose territory bounding half-extent is below this hide
        // their crest entirely (a dozen Kerensky-cluster clans stacking
        // min-size crests is still an unreadable glob). 0 disables.
        public float LogoMinExtentToShow = 0f;
        public float TerritoryHaloSize = 0f;  // world units, 0 disables halos
        public float TerritoryHaloOpacity = 0.18f;

        // AIEmpires fork: faction border lines from a coarse ownership grid.
        // BorderOpacity 0 disables; resolution is cells across the map.
        public float BorderOpacity = 0f;
        public int BorderGridResolution = 160;

        // AIEmpires fork: vanilla StarmapBorders political-map support (ported
        // from BTA's fork, repaired). drawBorders re-enables the GPU territory
        // renderer; rescaleBorders stretches its canvas to MapWidth/Height.
        // BTA swapped plusTex to solid black which multiplies the territory
        // fill to nothing — white keeps the poster-style fill. TravelWash 0
        // kills the all-white travel-zone overlay that greys the whole map
        // when every system is reachable; negative leaves vanilla behavior.
        public bool drawBorders;
        public bool rescaleBorders;
        public bool borderFillWhite = true;
        public float TravelWashIntensity = -1f;

    }

    public class LogoItem
    {
        public string factionName = "";
        public string logoImage = "";
    }

    public class Fields {
        public static float cbill = 0;
        public static Transform originalTransform = null;
    }

    public struct PotentialContract {
        // Token: 0x040089A4 RID: 35236
        public ContractOverride contractOverride;

        // Token: 0x040089A5 RID: 35237
        public Faction employer;

        // Token: 0x040089A6 RID: 35238
        public Faction target;

        // Token: 0x040089A7 RID: 35239
        public int difficulty;
    }
 
}