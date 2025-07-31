namespace LiveSplit.Server.WebsocketMessages;

public class GetCompletedCountWebsocketMessage
{
    public int CompletedCount;

    public GetCompletedCountWebsocketMessage(int completedCount)
    {
        CompletedCount = completedCount;
    }
}
