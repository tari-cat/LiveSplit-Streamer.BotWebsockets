namespace LiveSplit.Server.WebsocketMessages;

public class MultiSplitWebsocketMessage
{
    public StateWebsocketMessage State;
    public SplitWebsocketMessage PreviousSplit;
    public SplitWebsocketMessage CurrentSplit;

    public MultiSplitWebsocketMessage(StateWebsocketMessage state, SplitWebsocketMessage previousSplit, SplitWebsocketMessage currentSplit)
    {
        State = state;
        PreviousSplit = previousSplit;
        CurrentSplit = currentSplit;
    }
}
