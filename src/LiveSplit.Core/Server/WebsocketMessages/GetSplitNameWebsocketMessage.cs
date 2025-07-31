namespace LiveSplit.Server.WebsocketMessages;

public class GetSplitNameWebsocketMessage
{
    public string SplitName;

    public GetSplitNameWebsocketMessage(string splitName)
    {
        SplitName = splitName;
    }
}
