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

        // AIEmpires fork: always-visible system labels. Radius is in def-space
        // light-years (raw Position units), 0 disables proximity labels.
        public float ProximityLabelRadiusLy = 100f;
        public bool ShowLabelDifficulty = true;
        public List<string> alwaysLabeledSystems = new List<string>();

        // AIEmpires fork: camera distance from the map plane. Vanilla parks the
        // camera at z=-100; larger MapWidth/Height needs a proportionally
        // farther camera or max zoom-out can no longer frame the whole sphere.
        public float CameraDistance = 100f;

        // AIEmpires fork: visual tuning that must scale with MapWidth/Height.
        public float LabelCharacterSize = 0.28f;
        public float LogoScale = 5f;      // 5 replicates the legacy original+4 sizing
        public float LogoOpacity = 1f;
        public float TerritoryHaloSize = 0f;  // world units, 0 disables halos
        public float TerritoryHaloOpacity = 0.18f;

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