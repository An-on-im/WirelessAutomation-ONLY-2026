using UnityEngine;

namespace WirelessAutomation
{
    public class SignalEmitter
    {
        public int Id { get; set; }
        public int EmitChannel { get; set; }
        public int Signal { get; set; }
        public GameObject GameObject { get; set; }

        public SignalEmitter(int emitChannel, int signal, GameObject go = null)
        {
            Signal = signal;
            EmitChannel = emitChannel;
            GameObject = go;
        }
    }
}