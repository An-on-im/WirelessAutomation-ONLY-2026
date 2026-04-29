using KSerialization;
using UnityEngine;

namespace WirelessAutomation
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class WIRELESSSIGNALEMITTER : KMonoBehaviour, IIntSliderControl
    {
        [field: Serialize]
        public int EmitChannel { get; set; }

        [Serialize]
        private int _emitterId;

        [Serialize]
        private bool _channelsConfigured;

        [MyCmpGet]
        private LogicPorts _logicPorts;

        private static StatusItem ChannelUnassignedStatus;
        private static StatusItem ChannelOccupiedStatus;
        private static StatusItem EmitterActiveStatus;
        private static StatusItem EmitterIdleStatus;

        private bool _channelOccupied;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            Subscribe((int)GameHashes.LogicEvent, OnLogicEventChanged);
            Subscribe(WirelessAutomationManager.ChannelListChangedEvent, OnChannelListChanged);
            InitStatusItems();
        }

        private void InitStatusItems()
        {
            if (ChannelUnassignedStatus == null)
            {
                ChannelUnassignedStatus = new StatusItem(
                    "WirelessSignalEmitter_ChannelUnassigned",
                    STRINGS.STATUSITEMS.CHANNEL_UNASSIGNED.NAME,
                    STRINGS.STATUSITEMS.CHANNEL_UNASSIGNED.TOOLTIP,
                    "",
                    StatusItem.IconType.Info,
                    NotificationType.BadMinor,
                    false,
                    OverlayModes.None.ID);
            }
            if (ChannelOccupiedStatus == null)
            {
                ChannelOccupiedStatus = new StatusItem(
                    "WirelessSignalEmitter_ChannelOccupied",
                    STRINGS.STATUSITEMS.CHANNEL_OCCUPIED.NAME,
                    STRINGS.STATUSITEMS.CHANNEL_OCCUPIED.TOOLTIP,
                    "",
                    StatusItem.IconType.Exclamation,
                    NotificationType.BadMinor,
                    false,
                    OverlayModes.None.ID);
            }
            if (EmitterActiveStatus == null)
            {
                EmitterActiveStatus = new StatusItem(
                    "WirelessSignalEmitter_Active",
                    STRINGS.STATUSITEMS.EMITTER_ACTIVE.NAME,
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
                        var emitter = data as WIRELESSSIGNALEMITTER;
                        if (emitter == null) return str;
                        int cnt = WirelessAutomationManager.GetReceiverCountOnChannel(emitter.EmitChannel);
                        return string.Format(str, emitter.EmitChannel, cnt);
                    });
            }
            if (EmitterIdleStatus == null)
            {
                EmitterIdleStatus = new StatusItem(
                    "WirelessSignalEmitter_Idle",
                    STRINGS.STATUSITEMS.EMITTER_IDLE.NAME,
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
                        var emitter = data as WIRELESSSIGNALEMITTER;
                        if (emitter == null) return str;
                        int cnt = WirelessAutomationManager.GetReceiverCountOnChannel(emitter.EmitChannel);
                        return string.Format(str, emitter.EmitChannel, cnt);
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
                int currentSignal = _logicPorts.GetInputValue(LogicSwitch.PORT_ID);
                WirelessAutomationManager.SetEmitterSignal(_emitterId, currentSignal);
            }
            else
            {
                UpdateVisualState(false);
            }

            UpdateStatuses();
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.LogicEvent, OnLogicEventChanged);
            Unsubscribe(WirelessAutomationManager.ChannelListChangedEvent, OnChannelListChanged);
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

        private void OnChannelListChanged(object data)
        {
            UpdateStatuses();
        }

        private void UpdateVisualState(bool isOn)
        {
            GetComponent<KBatchedAnimController>().Play(isOn ? "on_pst" : "off", KAnim.PlayMode.Loop);
        }

        private void ChangeEmitChannel(int channel)
        {
            if (EmitChannel == channel) return;

            if (channel != 0 && !WirelessAutomationManager.IsChannelFreeForEmitter(_emitterId, channel))
            {
                _channelOccupied = true;
                WirelessAutomationManager.ChangeEmitterChannel(_emitterId, 0);
                EmitChannel = 0;
                UpdateStatuses();
                RefreshSideScreen();
                UpdateVisualState(false);
                return;
            }

            _channelOccupied = false;
            WirelessAutomationManager.ChangeEmitterChannel(_emitterId, channel);
            EmitChannel = channel;
            UpdateStatuses();
            RefreshSideScreen();

            if (channel == 0)
                UpdateVisualState(false);
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
                if (_channelOccupied)
                    selectable.AddStatusItem(ChannelOccupiedStatus, this);
                else
                    selectable.AddStatusItem(ChannelUnassignedStatus, this);
                return;
            }

            int signal = _logicPorts.GetInputValue(LogicSwitch.PORT_ID);
            if (signal != 0)
                selectable.AddStatusItem(EmitterActiveStatus, this);
            else
                selectable.AddStatusItem(EmitterIdleStatus, this);
        }

        private void RefreshSideScreen()
        {
            if (DetailsScreen.Instance != null)
                DetailsScreen.Instance.Refresh(gameObject);
        }

        public int SliderDecimalPlaces(int index) => 0;
        public float GetSliderMin(int index) => 0;
        public float GetSliderMax(int index) => 100;
        public float GetSliderValue(int index) => EmitChannel;
        public void SetSliderValue(float value, int index) => ChangeEmitChannel(Mathf.RoundToInt(value));
        public string GetSliderTooltipKey(int index)
        {
            if (EmitChannel == 0)
                return "STRINGS.SLIDER.DISABLED";
            return string.Format("STRINGS.SLIDER.EMITTER_CHANNEL", EmitChannel);
        }
        public string GetSliderTooltip(int index)
        {
            if (EmitChannel == 0)
                return STRINGS.SLIDER.DISABLED;
            return string.Format(STRINGS.SLIDER.EMITTER_CHANNEL, EmitChannel);
        }
        public string SliderTitleKey => "STRINGS.SLIDER.TITLE";
        public string SliderUnits => string.Empty;
    }
}