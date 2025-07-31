namespace LiveSplit.Server.WebsocketMessages;

public class LiveSplitWebsocketMessage<T>
{
    public string Type;
    public T Data;

    public LiveSplitWebsocketMessage(string type, T data)
    {
        Type = type;
        Data = data;
    }
}
