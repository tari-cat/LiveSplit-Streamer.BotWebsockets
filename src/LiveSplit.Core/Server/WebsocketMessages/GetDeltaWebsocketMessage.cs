namespace LiveSplit.Server.WebsocketMessages;

public class GetDeltaWebsocketMessage
{
    public string Delta;
    public float DeltaSeconds;

    public GetDeltaWebsocketMessage(string delta, float deltaSeconds)
    {
        Delta = delta;
        DeltaSeconds = deltaSeconds;
    }
}
