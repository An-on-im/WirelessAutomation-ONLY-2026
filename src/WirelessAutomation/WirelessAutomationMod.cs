using HarmonyLib;
using KMod;

namespace WirelessAutomation
{
    public class WirelessAutomationMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            ModLogger.Log("Mod loaded successfully.");
            base.OnLoad(harmony);
        }
    }
}