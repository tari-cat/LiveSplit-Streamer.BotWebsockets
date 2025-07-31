namespace LiveSplit.Server.WebsocketMessages;

public class GetTimerPhaseWebsocketMessage
{
    public string TimerPhase;

    public GetTimerPhaseWebsocketMessage(string timerPhase)
    {
        TimerPhase = timerPhase;
    }
}
