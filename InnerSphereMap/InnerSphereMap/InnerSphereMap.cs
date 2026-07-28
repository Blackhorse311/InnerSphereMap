using HarmonyLib;
using System.Reflection;

namespace InnerSphereMap
{
    public class InnerSphereMap
    {
        internal static string ModDirectory;

        public static Settings SETTINGS;

        public static void Init(string directory, string settingsJSON) {
            ModDirectory = directory;
            SETTINGS = Helper.LoadSettings();
            var harmony = new Harmony("de.morphyum.InnerSphereMap");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
