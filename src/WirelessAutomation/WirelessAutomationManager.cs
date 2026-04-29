using KSerialization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WirelessAutomation
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public static class WirelessAutomationManager
    {
        public static int WirelessLogicEvent = Hash.SDBMLower("WirelessAutomation_WirelessLogicEvent");
        public static int ChannelListChangedEvent = Hash.SDBMLower("WirelessAutomation_ChannelListChanged");

        private static List<SignalEmitter> Emitters { get; } = new List<SignalEmitter>();
        private static List<SignalReceiver> Receivers { get; } = new List<SignalReceiver>();

        public static void ResetEmittersList()
        {
            Emitters.Clear();
        }

        public static void ResetReceiversList()
        {
            Receivers.Clear();
        }

        public static int RegisterEmitter(SignalEmitter emitter)
        {
            var newId = 0;
            if (Emitters.Count > 0)
                newId = Emitters.Max(e => e.Id) + 1;

            emitter.Id = newId;
            if (emitter.EmitChannel != 0 && Emitters.Any(e => e.EmitChannel == emitter.EmitChannel))
                emitter.EmitChannel = 0;

            Emitters.Add(emitter);
            TriggerChannelListChanged();
            return emitter.Id;
        }

        public static int RegisterReceiver(SignalReceiver receiver)
        {
            var newId = 0;
            if (Receivers.Count > 0)
                newId = Receivers.Max(e => e.Id) + 1;

            receiver.Id = newId;
            Receivers.Add(receiver);
            TriggerChannelListChanged();
            return receiver.Id;
        }

        public static void UnregisterEmitter(int emitterId)
        {
            var emitter = Emitters.FirstOrDefault(e => e.Id == emitterId);
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
            var receiver = Receivers.FirstOrDefault(e => e.Id == id);
            if (receiver != null)
            {
                Receivers.Remove(receiver);
                TriggerChannelListChanged();
            }
        }

        public static void SetEmitterSignal(int emitterId, int signal)
        {
            var emitter = Emitters.FirstOrDefault(e => e.Id == emitterId);
            if (emitter == null || emitter.EmitChannel == 0) return; // Канал 0 – не передаём

            emitter.Signal = signal;
            NotifyReceivers(emitter.EmitChannel, signal);
        }

        public static void ChangeEmitterChannel(int emitterId, int channel)
        {
            var emitter = Emitters.FirstOrDefault(e => e.Id == emitterId);
            if (emitter == null) return;

            NotifyReceivers(emitter.EmitChannel, 0);
            emitter.EmitChannel = channel;

            if (channel != 0)
                NotifyReceivers(channel, emitter.Signal);

            TriggerChannelListChanged();
        }

        public static void ChangeReceiverChannel(int receiverId, int channel)
        {
            var receiver = Receivers.FirstOrDefault(r => r.Id == receiverId);
            if (receiver == null) return;

            if (channel != 0)
            {
                var emitterOnChannel = Emitters.FirstOrDefault(e => e.EmitChannel == channel);
                int signal = emitterOnChannel?.Signal ?? 0;
                var eventData = new WirelessLogicValueChanged { Channel = channel, Signal = signal };
                receiver.GameObject?.Trigger(WirelessLogicEvent, (object)eventData);
            }
            TriggerChannelListChanged();
        }

        private static void NotifyReceivers(int channel, int signal)
        {
            var eventData = new WirelessLogicValueChanged { Signal = signal, Channel = channel };
            foreach (var receiver in Receivers)
            {
                if (receiver.Channel == channel)
                    receiver.GameObject?.Trigger(WirelessLogicEvent, (object)eventData);
            }
        }

        private static void TriggerChannelListChanged()
        {
            var objects = new HashSet<GameObject>();
            foreach (var e in Emitters)
                objects.Add(e.GameObject);
            foreach (var r in Receivers)
                objects.Add(r.GameObject);

            foreach (var go in objects)
                go?.Trigger(ChannelListChangedEvent, (object)null);
        }

        public static bool IsChannelFreeForEmitter(int emitterId, int channel)
        {
            if (channel == 0) return true;
            return !Emitters.Any(e => e.Id != emitterId && e.EmitChannel == channel);
        }

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

        public static int GetEmitterSignalOnChannel(int channel)
        {
            return Emitters.FirstOrDefault(e => e.EmitChannel == channel)?.Signal ?? 0;
        }

        public static bool HasEmitterOnChannel(int channel)
        {
            return Emitters.Any(e => e.EmitChannel == channel);
        }

        public static int GetReceiverCountOnChannel(int channel)
        {
            return Receivers.Count(r => r.Channel == channel);
        }
    }
}