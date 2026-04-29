using HarmonyLib;
using KMod;

namespace WirelessAutomation
{
    public class WirelessAutomationMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            Debug.Log("[WirelessAutomation] Mod loaded successfully.");
            base.OnLoad(harmony);
        }
    }
}