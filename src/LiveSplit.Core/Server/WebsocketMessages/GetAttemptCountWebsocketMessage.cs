namespace LiveSplit.Server.WebsocketMessages;

public class GetAttemptCountWebsocketMessage
{
    public int AttemptCount;

    public GetAttemptCountWebsocketMessage(int attemptCount)
    {
        AttemptCount = attemptCount;
    }
}
