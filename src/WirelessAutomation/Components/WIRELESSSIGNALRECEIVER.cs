using KSerialization;
using UnityEngine;

namespace WirelessAutomation
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class WIRELESSSIGNALRECEIVER : KMonoBehaviour
    {
        [field: Serialize] public int ReceiveChannel { get; set; }
        [field: Serialize] public int Signal { get; set; }

        [Serialize] private int _receiverId;
        [Serialize] private bool _channelsConfigured;

        [MyCmpGet] private LogicPorts _logicPorts;

        private static StatusItem ChannelUnassignedStatus;
        private static StatusItem ReceiverActiveStatus;
        private static StatusItem ReceiverIdleStatus;

        private static readonly EventSystem.IntraObjectHandler<WIRELESSSIGNALRECEIVER> OnCopySettingsDelegate =
            new EventSystem.IntraObjectHandler<WIRELESSSIGNALRECEIVER>((comp, data) => comp.OnCopySettings(data));

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            Subscribe(WirelessAutomationManager.ChannelListChangedEvent, OnChannelListChanged);
            Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
            InitStatusItems();
        }

        private void InitStatusItems()
        {
            if (ChannelUnassignedStatus == null)
            {
                ChannelUnassignedStatus = new StatusItem("WirelessSignalReceiver_ChannelUnassigned",
                    STRINGS.STATUSITEMS.CHANNEL_UNASSIGNED.NAME, STRINGS.STATUSITEMS.CHANNEL_UNASSIGNED.TOOLTIP, "",
                    StatusItem.IconType.Info, NotificationType.BadMinor, false, OverlayModes.None.ID);
                ReceiverActiveStatus = new StatusItem("WirelessSignalReceiver_Active",
                    STRINGS.STATUSITEMS.RECEIVER_ACTIVE.NAME, "", "",
                    StatusItem.IconType.Info, NotificationType.Neutral, false, OverlayModes.None.ID, 129022, true,
                    (str, data) => {
                        var rec = data as WIRELESSSIGNALRECEIVER;
                        if (rec == null) return str;
                        bool hasEmitter = WirelessAutomationManager.HasEmitterOnChannel(rec.ReceiveChannel);
                        string yesNo = hasEmitter ? STRINGS.STATUSITEMS.EMITTER_PRESENT.YES : STRINGS.STATUSITEMS.EMITTER_PRESENT.NO;
                        return string.Format(str, rec.ReceiveChannel, yesNo);
                    });
                ReceiverIdleStatus = new StatusItem("WirelessSignalReceiver_Idle",
                    STRINGS.STATUSITEMS.RECEIVER_IDLE.NAME, "", "",
                    StatusItem.IconType.Info, NotificationType.Neutral, false, OverlayModes.None.ID, 129022, true,
                    (str, data) => {
                        var rec = data as WIRELESSSIGNALRECEIVER;
                        if (rec == null) return str;
                        bool hasEmitter = WirelessAutomationManager.HasEmitterOnChannel(rec.ReceiveChannel);
                        string yesNo = hasEmitter ? STRINGS.STATUSITEMS.EMITTER_PRESENT.YES : STRINGS.STATUSITEMS.EMITTER_PRESENT.NO;
                        return string.Format(str, rec.ReceiveChannel, yesNo);
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
            _receiverId = WirelessAutomationManager.RegisterReceiver(new SignalReceiver(ReceiveChannel, gameObject));

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
            Unsubscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
            WirelessAutomationManager.UnregisterReceiver(_receiverId);
            base.OnCleanUp();
        }

        private void OnWirelessLogicEventChanged(object data)
        {
            if (ReceiveChannel == 0) return;
            var ev = (WirelessLogicValueChanged)data;
            if (ev.Channel == ReceiveChannel && Signal != ev.Signal)
                ChangeState(ev.Signal);
        }

        private void OnChannelListChanged(object data) => UpdateStatuses();

        private void ChangeState(int signal)
        {
            Signal = signal;
            UpdateVisualState(signal != 0);
            _logicPorts?.SendSignal(LogicSwitch.PORT_ID, signal);
            UpdateStatuses();
        }

        public void ChangeListeningChannel(int channel)
        {
            if (ReceiveChannel == channel) return;
            ReceiveChannel = channel;
            WirelessAutomationManager.ChangeReceiverChannel(_receiverId, channel);
            if (channel == 0) ChangeState(0);
            UpdateStatuses();
            DetailsScreen.Instance?.Refresh(gameObject);
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
            selectable.AddStatusItem(Signal != 0 ? ReceiverActiveStatus : ReceiverIdleStatus, this);
        }

        private void UpdateVisualState(bool isOn) =>
            GetComponent<KBatchedAnimController>().Play(isOn ? "on_pst" : "off", KAnim.PlayMode.Loop);

        private void OnCopySettings(object data)
        {
            var other = ((GameObject)data).GetComponent<WIRELESSSIGNALRECEIVER>();
            if (other != null) ChangeListeningChannel(other.ReceiveChannel);
        }
    }
}