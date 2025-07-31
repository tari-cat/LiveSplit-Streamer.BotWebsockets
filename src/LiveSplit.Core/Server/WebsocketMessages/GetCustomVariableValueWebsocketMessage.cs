namespace LiveSplit.Server.WebsocketMessages;

public class GetCustomVariableValueWebsocketMessage
{
    public string CustomVariable;

    public GetCustomVariableValueWebsocketMessage(string customVariable)
    {
        CustomVariable = customVariable;
    }
}
