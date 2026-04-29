using KSerialization;
using UnityEngine;

namespace WirelessAutomation
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class WIRELESSSIGNALRECEIVER : KMonoBehaviour, IIntSliderControl
    {
        [field: Serialize]
        public int ReceiveChannel { get; set; }

        [field: Serialize]
        public int Signal { get; set; }

        [Serialize]
        private int _receiverId;

        [Serialize]
        private bool _channelsConfigured;

        [MyCmpGet]
        private LogicPorts _logicPorts;

        private static StatusItem ChannelUnassignedStatus;
        private static StatusItem ReceiverActiveStatus;
        private static StatusItem ReceiverIdleStatus;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            Subscribe(WirelessAutomationManager.ChannelListChangedEvent, OnChannelListChanged);
            InitStatusItems();
        }

        private void InitStatusItems()
        {
            if (ChannelUnassignedStatus == null)
            {
                ChannelUnassignedStatus = new StatusItem(
                    "WirelessSignalReceiver_ChannelUnassigned",
                    STRINGS.STATUSITEMS.CHANNEL_UNASSIGNED.NAME,
                    STRINGS.STATUSITEMS.CHANNEL_UNASSIGNED.TOOLTIP,
                    "",
                    StatusItem.IconType.Info,
                    NotificationType.BadMinor,
                    false,
                    OverlayModes.None.ID);
            }
            if (ReceiverActiveStatus == null)
            {
                ReceiverActiveStatus = new StatusItem(
                    "WirelessSignalReceiver_Active",
                    STRINGS.STATUSITEMS.RECEIVER_ACTIVE.NAME,
                    "",
                    "",
                    StatusItem.IconType.Info,
                    NotificationType.Neutral,
                    false,
                    OverlayModes.None.ID,
                    129022,
                    true,
                    (str, data) =>
                    {
                        var receiver = data as WIRELESSSIGNALRECEIVER;
                        if (receiver == null) return str;
                        bool hasEmitter = WirelessAutomationManager.HasEmitterOnChannel(receiver.ReceiveChannel);
                        string yesNo = hasEmitter
                            ? STRINGS.STATUSITEMS.EMITTER_PRESENT.YES
                            : STRINGS.STATUSITEMS.EMITTER_PRESENT.NO;
                        return string.Format(str, receiver.ReceiveChannel, yesNo);
                    });
            }
            if (ReceiverIdleStatus == null)
            {
                ReceiverIdleStatus = new StatusItem(
                    "WirelessSignalReceiver_Idle",
                    STRINGS.STATUSITEMS.RECEIVER_IDLE.NAME,
                    "",
                    "",
                    StatusItem.IconType.Info,
                    NotificationType.Neutral,
                    false,
                    OverlayModes.None.ID,
                    129022,
                    true,
                    (str, data) =>
                    {
                        var receiver = data as WIRELESSSIGNALRECEIVER;
                        if (receiver == null) return str;
                        bool hasEmitter = WirelessAutomationManager.HasEmitterOnChannel(receiver.ReceiveChannel);
                        string yesNo = hasEmitter
                            ? STRINGS.STATUSITEMS.EMITTER_PRESENT.YES
                            : STRINGS.STATUSITEMS.EMITTER_PRESENT.NO;
                        return string.Format(str, receiver.ReceiveChannel, yesNo);
                    });
            }
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();

            if (!_channelsConfigured)
            {
                ReceiveChannel = WirelessAutomationManager.GetLastOccupiedChannel();
                _channelsConfigured = true;
            }

            _receiverId = WirelessAutomationManager.RegisterReceiver(
                new SignalReceiver(ReceiveChannel, gameObject));

            if (ReceiveChannel > 0)
            {
                int signal = WirelessAutomationManager.GetEmitterSignalOnChannel(ReceiveChannel);
                Signal = signal;
                _logicPorts.SendSignal(LogicSwitch.PORT_ID, Signal);
            }
            else
            {
                Signal = 0;
                _logicPorts.SendSignal(LogicSwitch.PORT_ID, 0);
            }

            UpdateVisualState(Signal != 0);
            UpdateStatuses();
            Subscribe(WirelessAutomationManager.WirelessLogicEvent, OnWirelessLogicEventChanged);
        }

        protected override void OnCleanUp()
        {
            Unsubscribe(WirelessAutomationManager.WirelessLogicEvent, OnWirelessLogicEventChanged);
            Unsubscribe(WirelessAutomationManager.ChannelListChangedEvent, OnChannelListChanged);
            WirelessAutomationManager.UnregisterReceiver(_receiverId);
            base.OnCleanUp();
        }

        private void OnWirelessLogicEventChanged(object data)
        {
            if (ReceiveChannel == 0) return;
            var ev = (WirelessLogicValueChanged)data;
            if (ev.Channel == ReceiveChannel && Signal != ev.Signal)
            {
                ChangeState(ev.Signal);
            }
        }

        private void OnChannelListChanged(object data)
        {
            UpdateStatuses();
        }

        private void ChangeState(int signal)
        {
            Signal = signal;
            UpdateVisualState(signal != 0);
            _logicPorts?.SendSignal(LogicSwitch.PORT_ID, signal);
            UpdateStatuses();
        }

        private void ChangeListeningChannel(int channel)
        {
            if (ReceiveChannel == channel) return;

            ReceiveChannel = channel; 
            WirelessAutomationManager.ChangeReceiverChannel(_receiverId, channel);

            if (channel == 0)
                ChangeState(0);
            UpdateStatuses();
        }

        private void UpdateStatuses()
        {
            var selectable = GetComponent<KSelectable>();
            if (selectable == null) return;

            selectable.RemoveStatusItem(ChannelUnassignedStatus);
            selectable.RemoveStatusItem(ReceiverActiveStatus);
            selectable.RemoveStatusItem(ReceiverIdleStatus);

            if (ReceiveChannel == 0)
            {
                selectable.AddStatusItem(ChannelUnassignedStatus, this);
                return;
            }

            if (Signal != 0)
                selectable.AddStatusItem(ReceiverActiveStatus, this);
            else
                selectable.AddStatusItem(ReceiverIdleStatus, this);
        }

        private void UpdateVisualState(bool isOn)
        {
            GetComponent<KBatchedAnimController>().Play(isOn ? "on_pst" : "off", KAnim.PlayMode.Loop);
        }

        public int SliderDecimalPlaces(int index) => 0;
        public float GetSliderMin(int index) => 0;
        public float GetSliderMax(int index) => 100;
        public float GetSliderValue(int index) => ReceiveChannel;
        public void SetSliderValue(float value, int index) => ChangeListeningChannel(Mathf.RoundToInt(value));
        public string GetSliderTooltipKey(int index)
        {
            if (ReceiveChannel == 0)
                return "STRINGS.SLIDER.DISABLED";
            return string.Format("STRINGS.SLIDER.RECEIVER_CHANNEL", ReceiveChannel);
        }
        public string GetSliderTooltip(int index)
        {
            if (ReceiveChannel == 0)
                return STRINGS.SLIDER.DISABLED;
            return string.Format(STRINGS.SLIDER.RECEIVER_CHANNEL, ReceiveChannel);
        }
        public string SliderTitleKey => "STRINGS.SLIDER.TITLE";
        public string SliderUnits => string.Empty;
    }
}