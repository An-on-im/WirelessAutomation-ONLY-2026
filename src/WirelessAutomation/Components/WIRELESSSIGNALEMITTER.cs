using KSerialization;
using UnityEngine;

namespace WirelessAutomation
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class WIRELESSSIGNALEMITTER : KMonoBehaviour
    {
        [field: Serialize] public int EmitChannel { get; set; }

        [Serialize] private int _emitterId;
        [Serialize] private bool _channelsConfigured;

        [MyCmpGet] private LogicPorts _logicPorts;

        private static StatusItem ChannelUnassignedStatus;
        private static StatusItem ChannelOccupiedStatus;
        private static StatusItem EmitterActiveStatus;
        private static StatusItem EmitterIdleStatus;

        private bool _channelOccupied;

        private static readonly EventSystem.IntraObjectHandler<WIRELESSSIGNALEMITTER> OnCopySettingsDelegate =
            new EventSystem.IntraObjectHandler<WIRELESSSIGNALEMITTER>((comp, data) => comp.OnCopySettings(data));

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            Subscribe((int)GameHashes.LogicEvent, OnLogicEventChanged);
            Subscribe(WirelessAutomationManager.ChannelListChangedEvent, OnChannelListChanged);
            Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
            InitStatusItems();
        }

        private void InitStatusItems()
        {
            if (ChannelUnassignedStatus == null)
            {
                ChannelUnassignedStatus = new StatusItem("WirelessSignalEmitter_ChannelUnassigned",
                    STRINGS.STATUSITEMS.CHANNEL_UNASSIGNED.NAME,
                    STRINGS.STATUSITEMS.CHANNEL_UNASSIGNED.TOOLTIP, "",
                    StatusItem.IconType.Info, NotificationType.BadMinor, false, OverlayModes.None.ID);
                ChannelOccupiedStatus = new StatusItem("WirelessSignalEmitter_ChannelOccupied",
                    STRINGS.STATUSITEMS.CHANNEL_OCCUPIED.NAME,
                    STRINGS.STATUSITEMS.CHANNEL_OCCUPIED.TOOLTIP, "",
                    StatusItem.IconType.Exclamation, NotificationType.BadMinor, false, OverlayModes.None.ID);
                EmitterActiveStatus = new StatusItem("WirelessSignalEmitter_Active",
                    STRINGS.STATUSITEMS.EMITTER_ACTIVE.NAME, "", "",
                    StatusItem.IconType.Info, NotificationType.Neutral, false, OverlayModes.None.ID, 129022, true,
                    (str, data) => {
                        var em = data as WIRELESSSIGNALEMITTER;
                        if (em == null) return str;
                        int cnt = WirelessAutomationManager.GetReceiverCountOnChannel(em.EmitChannel);
                        return string.Format(str, em.EmitChannel, cnt);
                    });
                EmitterIdleStatus = new StatusItem("WirelessSignalEmitter_Idle",
                    STRINGS.STATUSITEMS.EMITTER_IDLE.NAME, "", "",
                    StatusItem.IconType.Info, NotificationType.Neutral, false, OverlayModes.None.ID, 129022, true,
                    (str, data) => {
                        var em = data as WIRELESSSIGNALEMITTER;
                        if (em == null) return str;
                        int cnt = WirelessAutomationManager.GetReceiverCountOnChannel(em.EmitChannel);
                        return string.Format(str, em.EmitChannel, cnt);
                    });
            }
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            if (!_channelsConfigured)
            {
                EmitChannel = WirelessAutomationManager.GetFirstFreeChannel();
                _channelsConfigured = true;
            }
            _emitterId = WirelessAutomationManager.RegisterEmitter(
                new SignalEmitter(EmitChannel, _logicPorts.GetInputValue(LogicSwitch.PORT_ID), gameObject));
            _channelOccupied = false;

            if (EmitChannel > 0)
            {
                int signal = _logicPorts.GetInputValue(LogicSwitch.PORT_ID);
                WirelessAutomationManager.SetEmitterSignal(_emitterId, signal);
            }
            else UpdateVisualState(false);

            UpdateStatuses();
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.LogicEvent, OnLogicEventChanged);
            Unsubscribe(WirelessAutomationManager.ChannelListChangedEvent, OnChannelListChanged);
            Unsubscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
            WirelessAutomationManager.UnregisterEmitter(_emitterId);
            base.OnCleanUp();
        }

        private void OnLogicEventChanged(object data)
        {
            if (EmitChannel == 0) return;
            int signal = ((LogicValueChanged)data).newValue;
            UpdateVisualState(signal != 0);
            WirelessAutomationManager.SetEmitterSignal(_emitterId, signal);
            UpdateStatuses();
        }

        private void OnChannelListChanged(object data) => UpdateStatuses();

        private void UpdateVisualState(bool isOn) =>
            GetComponent<KBatchedAnimController>().Play(isOn ? "on_pst" : "off", KAnim.PlayMode.Loop);

        public void ChangeEmitChannel(int channel)
        {
            if (EmitChannel == channel) return;
            if (channel != 0 && !WirelessAutomationManager.IsChannelFreeForEmitter(_emitterId, channel))
            {
                _channelOccupied = true;
                WirelessAutomationManager.ChangeEmitterChannel(_emitterId, 0);
                EmitChannel = 0;
                UpdateStatuses();
                UpdateVisualState(false);
                return;
            }
            _channelOccupied = false;
            WirelessAutomationManager.ChangeEmitterChannel(_emitterId, channel);
            EmitChannel = channel;
            UpdateStatuses();
            if (channel == 0) UpdateVisualState(false);
            DetailsScreen.Instance?.Refresh(gameObject);
        }

        private void UpdateStatuses()
        {
            var selectable = GetComponent<KSelectable>();
            if (selectable == null) return;
            selectable.RemoveStatusItem(ChannelUnassignedStatus);
            selectable.RemoveStatusItem(ChannelOccupiedStatus);
            selectable.RemoveStatusItem(EmitterActiveStatus);
            selectable.RemoveStatusItem(EmitterIdleStatus);

            if (EmitChannel == 0)
            {
                selectable.AddStatusItem(_channelOccupied ? ChannelOccupiedStatus : ChannelUnassignedStatus, this);
                return;
            }
            int signal = _logicPorts.GetInputValue(LogicSwitch.PORT_ID);
            selectable.AddStatusItem(signal != 0 ? EmitterActiveStatus : EmitterIdleStatus, this);
        }

        private void OnCopySettings(object data)
        {
            var other = ((GameObject)data).GetComponent<WIRELESSSIGNALEMITTER>();
            if (other != null) ChangeEmitChannel(other.EmitChannel);
        }
    }
}