using HarmonyLib;
using KMod;
using System.IO;
using System.Reflection;
using PeterHan.PLib.UI; 

namespace WirelessAutomation
{
    public static class WirelessAutomationPatches
    {
        [HarmonyPatch(typeof(Game), "OnPrefabInit")]
        public static class Game_OnPrefabInit_Patch
        {
            public static void Postfix(Game __instance)
            {
                WirelessAutomationManager.ResetEmittersList();
                WirelessAutomationManager.ResetReceiversList();
            }
        }

        [HarmonyPatch(typeof(Game), "OnLoadLevel")]
        public static class Game_OnLoadLevel_Patch
        {
            public static void Postfix()
            {
                WirelessAutomationManager.ResetEmittersList();
                WirelessAutomationManager.ResetReceiversList();
            }
        }

        [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
        public static class GeneratedBuildings_LoadGeneratedBuildings_Patch
        {
            public static void Prefix()
            {
                ModUtil.AddBuildingToPlanScreen(new HashedString("Automation"), WirelessSignalEmitterConfig.Id);
                ModUtil.AddBuildingToPlanScreen(new HashedString("Automation"), WirelessSignalReceiverConfig.Id);
            }
        }

        [HarmonyPatch(typeof(Db), "Initialize")]
        public static class Db_Initialize_Patch
        {
            public static void Postfix()
            {
                Db.Get().Techs.Get("DupeTrafficControl").unlockedItemIDs.Add(WirelessSignalEmitterConfig.Id);
                Db.Get().Techs.Get("DupeTrafficControl").unlockedItemIDs.Add(WirelessSignalReceiverConfig.Id);
            }
        }

        [HarmonyPatch(typeof(Localization), "Initialize")]
        public static class Localization_Initialize_Patch
        {
            public static void Postfix()
            {
                Localization.RegisterForTranslation(typeof(STRINGS));

                string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string translationsPath = Path.Combine(modPath, "translations");
                string localeCode = Localization.GetLocale()?.Code;
                if (!string.IsNullOrEmpty(localeCode))
                {
                    string poFile = Path.Combine(translationsPath, localeCode + ".po");
                    if (File.Exists(poFile))
                        Localization.OverloadStrings(Localization.LoadStringsFile(poFile, false));
                }
                LocString.CreateLocStringKeys(typeof(STRINGS), null);
                Localization.GenerateStringsTemplate(typeof(STRINGS), Path.Combine(Manager.GetDirectory(), "strings_templates"));
            }
        }

        [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
        public class AddSideScreen
        {
            public static void Postfix()
            {
                PUIUtils.AddSideScreenContent<WirelessChannelSideScreen>();
            }
        }
    }
}