namespace LiveSplit.Server.WebsocketMessages;

public class GetSplitTimeWebsocketMessage
{
    public string Time;
    public float TimeSeconds;

    public GetSplitTimeWebsocketMessage(string time, float timeSeconds)
    {
        Time = time;
        TimeSeconds = timeSeconds;
    }
}
