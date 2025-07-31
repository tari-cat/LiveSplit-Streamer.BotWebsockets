namespace LiveSplit.Server.WebsocketMessages;

public class GetSplitIndexWebsocketMessage
{
    public int SplitIndex;

    public GetSplitIndexWebsocketMessage(int splitIndex)
    {
        SplitIndex = splitIndex;
    }
}
