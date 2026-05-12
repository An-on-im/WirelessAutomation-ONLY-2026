using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PeterHan.PLib.UI;
using System.Collections.Generic;

namespace WirelessAutomation
{
    public class WirelessChannelSideScreen : SideScreenContent
    {
        private WIRELESSSIGNALEMITTER targetEmitter;
        private WIRELESSSIGNALRECEIVER targetReceiver;
        private bool isEmitter;
        private int currentChannel;
        private TextMeshProUGUI channelLabel;
        private GameObject channelScrollPane;
        private List<GameObject> channelButtons = new List<GameObject>();
        private bool builded = false;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            Destroy(gameObject.GetComponent<Image>());
            Build();
        }

        private void Build()
        {
            BuildContentContainer();
            BuildChannelLabel();
            BuildChannelList();
            builded = true;
        }

        private void BuildContentContainer()
        {
            ContentContainer = new GameObject("Content");
            ContentContainer.transform.SetParent(transform, false);
            var layout = ContentContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
        }

        private void BuildChannelLabel()
        {
            var go = new GameObject("ChannelListLabel");
            go.transform.SetParent(ContentContainer.transform, false);
            channelLabel = go.AddComponent<TextMeshProUGUI>();
            channelLabel.text = "СПИСОК КАНАЛОВ";
            channelLabel.fontSize = 16;
            channelLabel.fontStyle = FontStyles.Bold;
            channelLabel.alignment = TextAlignmentOptions.Center;
            channelLabel.color = Color.black;
        }

        private void BuildChannelList()
        {
            var buttonPanel = new PPanel("ButtonPanel")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 2,
                FlexSize = Vector2.right
            };

            for (int ch = 0; ch <= 100; ch++)
            {
                string label = ch == 0 ? "Выкл" : $"Канал {ch}";
                int channel = ch;
                var button = new PButton($"Channel_{channel}")
                {
                    Text = label,
                    FlexSize = Vector2.right,
                    Margin = new RectOffset(2, 2, 1, 1)
                };
                button.OnClick += _ => OnChannelClicked(channel);
                buttonPanel.AddChild(button);
            }

            var scrollPane = new PScrollPane("ChannelScroll")
            {
                Child = buttonPanel,
                ScrollHorizontal = false,
                ScrollVertical = true,
                AlwaysShowVertical = true,
                FlexSize = new Vector2(1f, 0f)
            };
            channelScrollPane = scrollPane.Build();
            channelScrollPane.transform.SetParent(ContentContainer.transform, false);

            var scrollRT = channelScrollPane.AddOrGet<RectTransform>();
            scrollRT.sizeDelta = new Vector2(0, 200);
            scrollRT.anchorMin = new Vector2(0, 0);
            scrollRT.anchorMax = new Vector2(1, 0);

            CacheChannelButtons();
            foreach (var btn in channelButtons)
                PButton.SetButtonEnabled(btn, false);
        }

        private void CacheChannelButtons()
        {
            channelButtons.Clear();
            foreach (Transform child in channelScrollPane.GetComponentsInChildren<Transform>())
            {
                if (child.name.StartsWith("Channel_"))
                {
                    channelButtons.Add(child.gameObject);
                }
            }
        }

        private bool IsChannelActive(int channel)
        {
            if (targetEmitter == null && targetReceiver == null)
                return false;

            if (channel == 0)
                return true;

            if (isEmitter)
            {
                if (channel == currentChannel)
                    return true;
                return !WirelessAutomationManager.HasEmitterOnChannel(channel);
            }
            else 
            {
                return WirelessAutomationManager.HasEmitterOnChannel(channel);
            }
        }

        private void OnChannelClicked(int channel)
        {
            if (!IsChannelActive(channel))
                return;

            if (channel == currentChannel)
                return;

            currentChannel = channel;
            if (isEmitter && targetEmitter != null)
                targetEmitter.ChangeEmitChannel(channel);
            else if (targetReceiver != null)
                targetReceiver.ChangeListeningChannel(channel);

            Refresh();
        }

        private void Refresh()
        {
            if (!builded) return;
            UpdateData();
        }

        private void UpdateData()
        {
            foreach (var buttonGO in channelButtons)
            {
                if (int.TryParse(buttonGO.name.Substring(8), out int channel))
                {
                    bool active = IsChannelActive(channel);
                    if (channel == currentChannel)
                        PButton.SetButtonEnabled(buttonGO, true);
                    else
                        PButton.SetButtonEnabled(buttonGO, active);

                    KImage bgImage = buttonGO.GetComponentInChildren<KImage>();
                    if (bgImage != null)
                    {
                        if (channel == currentChannel)
                        {
                            bgImage.colorStyleSetting = PUITuning.Colors.ButtonPinkStyle;
                        }
                        else if (active)
                        {
                            bgImage.colorStyleSetting = PUITuning.Colors.ButtonBlueStyle;
                        }
                        bgImage.ApplyColorStyleSetting();
                    }
                }
            }
        }

        public override void SetTarget(GameObject target)
        {
            if (targetEmitter != null)
                targetEmitter.Unsubscribe(WirelessAutomationManager.ChannelListChangedEvent, OnChannelsChanged);
            if (targetReceiver != null)
                targetReceiver.Unsubscribe(WirelessAutomationManager.ChannelListChangedEvent, OnChannelsChanged);

            targetEmitter = target?.GetComponent<WIRELESSSIGNALEMITTER>();
            targetReceiver = target?.GetComponent<WIRELESSSIGNALRECEIVER>();
            isEmitter = targetEmitter != null;
            currentChannel = isEmitter
                ? (targetEmitter != null ? targetEmitter.EmitChannel : 0)
                : (targetReceiver != null ? targetReceiver.ReceiveChannel : 0);

            if (targetEmitter != null)
                targetEmitter.Subscribe(WirelessAutomationManager.ChannelListChangedEvent, OnChannelsChanged);
            if (targetReceiver != null)
                targetReceiver.Subscribe(WirelessAutomationManager.ChannelListChangedEvent, OnChannelsChanged);

            Refresh();
        }

        private void OnChannelsChanged(object _)
        {
            Refresh();
        }

        public override void ClearTarget()
        {
            if (targetEmitter != null)
                targetEmitter.Unsubscribe(WirelessAutomationManager.ChannelListChangedEvent, OnChannelsChanged);
            if (targetReceiver != null)
                targetReceiver.Unsubscribe(WirelessAutomationManager.ChannelListChangedEvent, OnChannelsChanged);

            targetEmitter = null;
            targetReceiver = null;
            if (builded)
                foreach (var btn in channelButtons)
                    PButton.SetButtonEnabled(btn, false);
        }

        public override bool IsValidForTarget(GameObject target) =>
            target != null &&
            (target.GetComponent<WIRELESSSIGNALEMITTER>() != null ||
             target.GetComponent<WIRELESSSIGNALRECEIVER>() != null);

        public override string GetTitle() => STRINGS.SLIDER.TITLE;
    }
}