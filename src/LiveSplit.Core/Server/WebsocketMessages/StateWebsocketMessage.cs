﻿using LiveSplit.Model;
using LiveSplit.TimeFormatters;

namespace LiveSplit.Server.WebsocketMessages;

public class StateWebsocketMessage
{
    public string Comparison;

    public string AttemptDuration;
    public float AttemptDurationSeconds;

    public string RealTime;
    public float RealTimeSeconds;

    public string GameTime;
    public float GameTimeSeconds;

    public string TimingMethod;

    public bool IsGameTimeInitialized;
    public bool IsGameTimePaused;

    public SplitWebsocketMessage Split;
    public RunWebsocketMessage Run;

    public StateWebsocketMessage(LiveSplitState state, ITimeFormatter formatter)
    {
        Run = new RunWebsocketMessage(state.Run, formatter);
        Split = new SplitWebsocketMessage(state.CurrentSplit, state.CurrentSplitIndex, formatter);
        Comparison = state.CurrentComparison;
        AttemptDuration = formatter.Format(state.CurrentAttemptDuration);
        AttemptDurationSeconds = (float)state.CurrentAttemptDuration.TotalSeconds;

        Time currentTime = state.CurrentTime;

        RealTime = formatter.Format(currentTime.RealTime);
        RealTimeSeconds = currentTime.RealTime.HasValue ? (float)currentTime.RealTime.Value.TotalSeconds : 0;

        GameTime = formatter.Format(currentTime.GameTime);
        GameTimeSeconds = currentTime.GameTime.HasValue ? (float)currentTime.GameTime.Value.TotalSeconds : 0;

        TimingMethod = state.CurrentTimingMethod.ToString();

        IsGameTimeInitialized = state.IsGameTimeInitialized;
        IsGameTimePaused = state.IsGameTimePaused;
    }
}
