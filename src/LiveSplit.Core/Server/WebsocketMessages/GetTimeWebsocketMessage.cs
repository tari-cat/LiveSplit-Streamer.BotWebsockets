namespace LiveSplit.Server.WebsocketMessages;

public class GetTimeWebsocketMessage
{
    public string Time;
    public float TimeSeconds;

    public GetTimeWebsocketMessage(string time, float timeSeconds)
    {
        Time = time;
        TimeSeconds = timeSeconds;
    }
}
