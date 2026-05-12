using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WirelessAutomation
{
    public static class WirelessAutomationManager
    {
        public static readonly int WirelessLogicEvent = Hash.SDBMLower("WirelessAutomation_WirelessLogicEvent");
        public static readonly int ChannelListChangedEvent = Hash.SDBMLower("WirelessAutomation_ChannelListChanged");

        private static readonly List<SignalEmitter> Emitters = new List<SignalEmitter>();
        private static readonly List<SignalReceiver> Receivers = new List<SignalReceiver>();

        public static void ResetEmittersList() => Emitters.Clear();
        public static void ResetReceiversList() => Receivers.Clear();

        public static int RegisterEmitter(SignalEmitter emitter)
        {
            int newId = Emitters.Count > 0 ? Emitters.Max(e => e.Id) + 1 : 0;
            emitter.Id = newId;
            if (emitter.EmitChannel != 0 && Emitters.Any(e => e.EmitChannel == emitter.EmitChannel))
                emitter.EmitChannel = 0;

            Emitters.Add(emitter);
            TriggerChannelListChanged();
            return emitter.Id;
        }

        public static int RegisterReceiver(SignalReceiver receiver)
        {
            int newId = Receivers.Count > 0 ? Receivers.Max(r => r.Id) + 1 : 0;
            receiver.Id = newId;
            Receivers.Add(receiver);
            TriggerChannelListChanged();
            return receiver.Id;
        }

        public static void UnregisterEmitter(int id)
        {
            var emitter = Emitters.FirstOrDefault(e => e.Id == id);
            if (emitter != null)
            {
                if (emitter.EmitChannel != 0)
                    NotifyReceivers(emitter.EmitChannel, 0);
                Emitters.Remove(emitter);
                TriggerChannelListChanged();
            }
        }

        public static void UnregisterReceiver(int id)
        {
            var receiver = Receivers.FirstOrDefault(r => r.Id == id);
            if (receiver != null)
            {
                Receivers.Remove(receiver);
                TriggerChannelListChanged();
            }
        }

        public static void SetEmitterSignal(int id, int signal)
        {
            var emitter = Emitters.FirstOrDefault(e => e.Id == id);
            if (emitter == null || emitter.EmitChannel == 0) return;
            emitter.Signal = signal;
            NotifyReceivers(emitter.EmitChannel, signal);
        }

        public static void ChangeEmitterChannel(int id, int channel)
        {
            var emitter = Emitters.FirstOrDefault(e => e.Id == id);
            if (emitter == null) return;
            NotifyReceivers(emitter.EmitChannel, 0);
            emitter.EmitChannel = channel;
            if (channel != 0)
                NotifyReceivers(channel, emitter.Signal);
            TriggerChannelListChanged();
        }

        public static void ChangeReceiverChannel(int id, int channel)
        {
            var receiver = Receivers.FirstOrDefault(r => r.Id == id);
            if (receiver == null) return;
            receiver.Channel = channel;
            if (channel != 0)
            {
                int signal = GetEmitterSignalOnChannel(channel);
                SendWirelessEvent(receiver.GameObject, channel, signal);
            }
            TriggerChannelListChanged();
        }

        public static bool IsChannelFreeForEmitter(int emitterId, int channel) =>
            channel == 0 || !Emitters.Any(e => e.Id != emitterId && e.EmitChannel == channel);

        public static int GetFirstFreeChannel()
        {
            for (int ch = 1; ch <= 100; ch++)
                if (!Emitters.Any(e => e.EmitChannel == ch))
                    return ch;
            return 0;
        }

        public static int GetLastOccupiedChannel()
        {
            var occupied = Emitters.Where(e => e.EmitChannel > 0).Select(e => e.EmitChannel).Distinct();
            return occupied.Any() ? occupied.Max() : 0;
        }

        public static int GetEmitterSignalOnChannel(int channel) =>
            Emitters.FirstOrDefault(e => e.EmitChannel == channel)?.Signal ?? 0;

        public static bool HasEmitterOnChannel(int channel) =>
            Emitters.Any(e => e.EmitChannel == channel);

        public static int GetReceiverCountOnChannel(int channel) =>
            Receivers.Count(r => r.Channel == channel);

        private static void NotifyReceivers(int channel, int signal)
        {
            var eventData = new WirelessLogicValueChanged { Signal = signal, Channel = channel };
            foreach (var receiver in Receivers)
            {
                if (receiver.Channel == channel)
                    SendWirelessEvent(receiver.GameObject, channel, signal);
            }
        }

        private static void SendWirelessEvent(GameObject target, int channel, int signal)
        {
            if (target == null) return;
            try
            {
                target.Trigger(WirelessLogicEvent, (object)new WirelessLogicValueChanged { Channel = channel, Signal = signal });
            }
            catch (System.Exception e)
            {
                ModLogger.Warning($"Failed to trigger wireless event on {target.name}: {e.Message}");
            }
        }

        private static void TriggerChannelListChanged()
        {
            var objects = new HashSet<GameObject>();
            foreach (var e in Emitters) objects.Add(e.GameObject);
            foreach (var r in Receivers) objects.Add(r.GameObject);

            foreach (var go in objects)
            {
                if (go == null) continue;
                try { go.Trigger(ChannelListChangedEvent, null); }
                catch (System.Exception e) { ModLogger.Warning($"Failed to trigger channel list changed on {go.name}: {e.Message}"); }
            }
        }
    }
}