﻿using Robust.Shared.Serialization;

namespace Content.Shared.MachineLinking
{
    [Serializable, NetSerializable]
    public enum SignalTimerUiKey : byte
    {
        Key
    }

    [Serializable, NetSerializable]
    public sealed class SignalTimerBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string CurrentText { get; }
        public string CurrentDelayMinutes { get; }
        public string CurrentDelaySeconds { get; }
        public bool ShowText { get; }
        public TimeSpan TriggerTime { get; }
        public bool TimerStarted { get; }
        public bool? HasAccess { get; }
        public SignalTimerBoundUserInterfaceState(string currentText, string currentDelayMinutes, string currentDelaySeconds, bool showText, TimeSpan triggerTime, bool timerStarted, bool? hasAccess)
        {
            CurrentText = currentText;
            CurrentDelayMinutes = currentDelayMinutes;
            CurrentDelaySeconds = currentDelaySeconds;
            ShowText = showText;
            TriggerTime = triggerTime;
            TimerStarted = timerStarted;
            HasAccess = hasAccess;
        }
    }

    [Serializable, NetSerializable]
    public sealed class SignalTimerTextChangedMessage : BoundUserInterfaceMessage
    {
        public string Text { get; }

        public SignalTimerTextChangedMessage(string text)
        {
            Text = text;
        }
    }

    [Serializable, NetSerializable]
    public sealed class SignalTimerDelayChangedMessage : BoundUserInterfaceMessage
    {
        public TimeSpan Delay { get; }
        public SignalTimerDelayChangedMessage(TimeSpan delay)
        {
            Delay = delay;
        }
    }

    [Serializable, NetSerializable]
    public sealed class SignalTimerStartMessage : BoundUserInterfaceMessage
    {
        public EntityUid User { get; }
        public SignalTimerStartMessage(EntityUid user)
        {
            User = user;
        }
    }
}
