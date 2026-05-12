using UnityEngine;

namespace WirelessAutomation
{
    public static class ModLogger
    {
        private const string Prefix = "[WirelessAutomation]";

        public static void Log(string message) => Debug.Log($"{Prefix} {message}");
        public static void Warning(string message) => Debug.LogWarning($"{Prefix} {message}");
        public static void Error(string message) => Debug.LogError($"{Prefix} {message}");
    }
}