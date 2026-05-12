using STRINGS;

namespace WirelessAutomation
{
    public class STRINGS
    {
        public class BUILDINGS
        {
            public class PREFABS
            {
                public class WIRELESSSIGNALEMITTER
                {
                    public static LocString NAME = UI.FormatAsLink("Wireless Signal Emitter", "WIRELESSSIGNALEMITTER");

                    public static LocString DESC = "Emitters allow automation signals to permeate space and solid rock, proving that wires are often just a state of mind.";

                    public static LocString EFFECT = $"Broadcasts an automation signal on a selected channel. Signals can be intercepted by any {UI.FormatAsLink("Wireless Receivers", "WIRELESSSIGNALRECEIVER")} tuned to the same frequency.";

                    public static LocString PORT_ACTIVE
                        = $"{UI.FormatAsAutomationState("Green Signal", UI.AutomationState.Active)}: Broadcasts an {UI.FormatAsAutomationState("Active", UI.AutomationState.Active)} signal on the current channel.";

                    public static LocString PORT_INACTIVE
                        = $"{UI.FormatAsAutomationState("Red Signal", UI.AutomationState.Standby)}: Broadcasts a {UI.FormatAsAutomationState("Standby", UI.AutomationState.Standby)} signal on the current channel.";
                }

                public class WIRELESSSIGNALRECEIVER
                {
                    public static LocString NAME = UI.FormatAsLink("Wireless Signal Receiver", "WIRELESSSIGNALRECEIVER");

                    public static LocString DESC = "Receivers pick up invisible automation waves, provided they aren't drowned out by the static of a nearby Duplicant's enthusiastic humming.";

                    public static LocString EFFECT = $"Outputs the automation signal received from a {UI.FormatAsLink("Wireless Emitter", "WIRELESSSIGNALEMITTER")} on the same channel.";

                    public static LocString PORT_ACTIVE
                        = $"{UI.FormatAsAutomationState("Green Signal", UI.AutomationState.Active)}: An {UI.FormatAsAutomationState("Active", UI.AutomationState.Active)} signal is detected on the selected channel.";

                    public static LocString PORT_INACTIVE
                        = $"{UI.FormatAsAutomationState("Red Signal", UI.AutomationState.Standby)}: No signal or a {UI.FormatAsAutomationState("Standby", UI.AutomationState.Standby)} signal is detected.";
                }
            }
        }


        public class SLIDER
        {

            public static LocString TITLE = "Channel";
            public static LocString DISABLED = "Disabled";
            public static LocString EMITTER_CHANNEL = "Will broadcast received signal on channel {0}.";
            public static LocString RECEIVER_CHANNEL = "Will listen to signal broadcast on channel {0}.";

        }

        public class STATUSITEMS
        {
            public class CHANNEL_UNASSIGNED
            {
                public static LocString NAME = "Channel unassigned";
                public static LocString TOOLTIP = "This device is not assigned to any channel and will not function.";
            }
            public class CHANNEL_OCCUPIED
            {
                public static LocString NAME = "Channel occupied";
                public static LocString TOOLTIP = "Another emitter is already using this channel.";
            }
            public class EMITTER_ACTIVE
            {
                public static LocString NAME = "Channel {0}: broadcasting signal ({1} receiver(s))";
            }
            public class EMITTER_IDLE
            {
                public static LocString NAME = "Channel {0}: no signal ({1} receiver(s))";
            }
            public class RECEIVER_ACTIVE
            {
                public static LocString NAME = "Channel {0}: receiving signal (emitter: {1})";
            }
            public class RECEIVER_IDLE
            {
                public static LocString NAME = "Channel {0}: no signal (emitter: {1})";
            }
            public class EMITTER_PRESENT
            {
                public static LocString YES = "yes";
                public static LocString NO = "no";
            }
        }
    }
}