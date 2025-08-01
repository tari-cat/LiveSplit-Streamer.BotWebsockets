using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using LiveSplit.Model;
using LiveSplit.Options;
using LiveSplit.Server.WebsocketMessages;
using LiveSplit.TimeFormatters;

using Newtonsoft.Json;

using WebSocketSharp.Server;

namespace LiveSplit.Server;

public class CommandServer
{
    public TcpListener Server { get; set; }
    public WebSocketServer WsServer { get; set; }
    public List<Connection> PipeConnections { get; set; }
    public List<TcpConnection> TcpConnections { get; set; }

    protected LiveSplitState State { get; set; }
    protected Form Form { get; set; }
    protected TimerModel Model { get; set; }
    protected ITimeFormatter TimeFormatter { get; set; }
    protected NamedPipeServerStream WaitingServerPipe { get; set; }

    protected bool AlwaysPauseGameTime { get; set; }

    public CommandServer(LiveSplitState state)
    {
        Model = new TimerModel();
        PipeConnections = [];
        TcpConnections = [];
        TimeFormatter = new PreciseTimeFormatter();

        State = state;
        Form = state.Form;

        Model.CurrentState = State;

        State.OnStart -= State_OnStart;
        State.OnSplit -= State_OnSplit;
        State.OnUndoSplit -= State_OnUndoSplit;
        State.OnSkipSplit -= State_OnSkipSplit;
        State.OnReset -= State_OnReset;
        State.OnPause -= State_OnPause;
        //State.OnUndoAllPauses -= State_OnUndoAllPauses;
        State.OnResume -= State_OnResume;
        //State.OnScrollUp -= State_OnScrollUp;
        //State.OnScrollDown -= State_OnScrollDown;
        //State.OnSwitchComparisonPrevious -= State_OnSwitchComparisonPrevious;
        //State.OnSwitchComparisonNext -= State_OnSwitchComparisonNext;
        //State.RunManuallyModified -= State_RunManuallyModified;
        //State.ComparisonRenamed -= State_ComparisonRenamed;

        State.OnStart += State_OnStart;
        State.OnSplit += State_OnSplit;
        State.OnUndoSplit += State_OnUndoSplit;
        State.OnSkipSplit += State_OnSkipSplit;
        State.OnReset += State_OnReset;
        State.OnPause += State_OnPause;
        //State.OnUndoAllPauses += State_OnUndoAllPauses;
        State.OnResume += State_OnResume;
        //State.OnScrollUp += State_OnScrollUp;
        //State.OnScrollDown += State_OnScrollDown;
        //State.OnSwitchComparisonPrevious += State_OnSwitchComparisonPrevious;
        //State.OnSwitchComparisonNext += State_OnSwitchComparisonNext;
        //State.RunManuallyModified += State_RunManuallyModified;
        //State.ComparisonRenamed += State_ComparisonRenamed;
    }

    public void StartTcp()
    {
        StopTcp();
        Server = new TcpListener(IPAddress.Any, State.Settings.ServerPort);
        Server.Start();
        Server.BeginAcceptTcpClient(AcceptTcpClient, null);
    }

    public void StartWs()
    {
        StopWs();
        WsServer = new WebSocketServer(State.Settings.ServerPort);
        WsServer.AddWebSocketService("/livesplit", () => new WsConnection(connection_MessageReceived));
        WsServer.Start();
    }

    public void StartNamedPipe()
    {
        StopPipe();
        WaitingServerPipe = CreateServerPipe();
        WaitingServerPipe.BeginWaitForConnection(AcceptPipeClient, null);
    }

    public void StopAll()
    {
        StopTcp();
        StopPipe();
        StopWs();
    }

    public void StopTcp()
    {
        foreach (TcpConnection connection in TcpConnections)
        {
            connection.Dispose();
        }

        TcpConnections.Clear();
        Server?.Stop();
    }

    public void StopWs()
    {
        WsServer?.Stop();
    }

    public void StopPipe()
    {
        WaitingServerPipe?.Dispose();

        foreach (Connection connection in PipeConnections)
        {
            connection.Dispose();
        }

        PipeConnections.Clear();
    }

    public void AcceptTcpClient(IAsyncResult result)
    {
        try
        {
            TcpClient client = Server.EndAcceptTcpClient(result);
            var connection = new TcpConnection(client);
            connection.MessageReceived += connection_MessageReceived;
            connection.Disconnected += tcpConnection_Disconnected;
            TcpConnections.Add(connection);

            Server.BeginAcceptTcpClient(AcceptTcpClient, null);
        }
        catch { }
    }

    public void AcceptPipeClient(IAsyncResult result)
    {
        try
        {
            WaitingServerPipe.EndWaitForConnection(result);
            var connection = new Connection(WaitingServerPipe);
            connection.MessageReceived += connection_MessageReceived;
            connection.Disconnected += pipeConnection_Disconnected;
            PipeConnections.Add(connection);

            WaitingServerPipe = CreateServerPipe();
            WaitingServerPipe.BeginWaitForConnection(AcceptPipeClient, null);
        }
        catch { }
    }

    private void pipeConnection_Disconnected(object sender, EventArgs e)
    {
        var connection = (Connection)sender;
        connection.MessageReceived -= connection_MessageReceived;
        connection.Disconnected -= pipeConnection_Disconnected;
        PipeConnections.Remove(connection);
        connection.Dispose();
    }

    private NamedPipeServerStream CreateServerPipe()
    {
        var pipe = new NamedPipeServerStream("LiveSplit", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        return pipe;
    }

    private TimeSpan? ParseTime(string timeString)
    {
        if (timeString == "-")
        {
            return null;
        }

        return TimeSpanParser.Parse(timeString);
    }

    private void connection_MessageReceived(object sender, MessageEventArgs e)
    {
        ProcessMessage(e.Message, e.Connection);
    }

    private void ProcessMessage(string message, IConnection clientConnection)
    {
        string[] args = message.Split([' '], 2);
        string command = args[0];
        switch (command)
        {
            case "startorsplit":
            {
                if (State.CurrentPhase == TimerPhase.Running)
                {
                    Model.Split();
                }
                else
                {
                    Model.Start();
                }

                break;
            }
            case "split":
            {
                Model.Split();
                break;
            }
            case "undosplit":
            case "unsplit":
            {
                Model.UndoSplit();
                break;
            }
            case "skipsplit":
            {
                Model.SkipSplit();
                break;
            }
            case "pause":
            {
                if (State.CurrentPhase != TimerPhase.Paused)
                {
                    Model.Pause();
                }

                break;
            }
            case "resume":
            {
                if (State.CurrentPhase == TimerPhase.Paused)
                {
                    Model.Pause();
                }

                break;
            }
            case "reset":
            {
                Model.Reset();
                break;
            }
            case "start":
            case "starttimer":
            {
                Model.Start();
                break;
            }
            case "setgametime":
            {
                try
                {
                    TimeSpan? time = ParseTime(args[1]);
                    State.SetGameTime(time);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    Log.Error($"[Server] Failed to parse time while setting game time: {args[1]}");
                }

                break;
            }
            case "setloadingtimes":
            {
                try
                {
                    TimeSpan? time = ParseTime(args[1]);
                    State.LoadingTimes = time ?? TimeSpan.Zero;
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    Log.Error($"[Server] Failed to parse time while setting loading times: {args[1]}");
                }

                break;
            }
            case "addloadingtimes":
            {
                try
                {
                    TimeSpan? time = ParseTime(args[1]);
                    State.LoadingTimes = State.LoadingTimes.Add(time ?? TimeSpan.Zero);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    Log.Error($"[Server] Failed to parse time while adding loading times: {args[1]}");
                }

                break;
            }
            case "pausegametime":
            {
                State.IsGameTimePaused = true;
                break;
            }
            case "unpausegametime":
            {
                AlwaysPauseGameTime = false;
                State.IsGameTimePaused = false;
                break;
            }
            case "alwayspausegametime":
            {
                AlwaysPauseGameTime = true;
                State.IsGameTimePaused = true;
                break;
            }
            // RESPONSES
            case "getdelta":
            {
                string comparison = args.Length > 1 ? args[1] : State.CurrentComparison;
                TimeSpan? delta = null;
                if (State.CurrentPhase is TimerPhase.Running or TimerPhase.Paused)
                {
                    delta = LiveSplitStateHelper.GetLastDelta(State, State.CurrentSplitIndex, comparison, State.CurrentTimingMethod);
                }
                else if (State.CurrentPhase == TimerPhase.Ended)
                {
                    delta = State.Run.Last().SplitTime[State.CurrentTimingMethod] - State.Run.Last().Comparisons[comparison][State.CurrentTimingMethod];
                }

                // Defaults to "-" when delta is null, such as when State.CurrentPhase == TimerPhase.NotRunning
                GetDeltaWebsocketMessage response = new(TimeFormatter.Format(delta), delta.HasValue ? (float)delta.Value.TotalSeconds : 0);
                SendWebsocketMessage(new LiveSplitWebsocketMessage<GetDeltaWebsocketMessage>(command, response), clientConnection);
                break;
            }
            case "getsplitindex":
            {
                int splitindex = State.CurrentSplitIndex;
                GetSplitIndexWebsocketMessage response = new(splitindex);
                SendWebsocketMessage(new LiveSplitWebsocketMessage<GetSplitIndexWebsocketMessage>(command, response), clientConnection);
                break;
            }
            case "getcurrentsplitname":
            {
                GetSplitNameWebsocketMessage response = new(State.CurrentSplit != null ? State.CurrentSplit.Name : "-");
                SendWebsocketMessage(new LiveSplitWebsocketMessage<GetSplitNameWebsocketMessage>(command, response), clientConnection);
                break;
            }
            case "getlastsplitname":
            case "getprevioussplitname":
            {
                GetSplitNameWebsocketMessage response = new(State.CurrentSplitIndex > 0 ? State.Run[State.CurrentSplitIndex - 1].Name : "-");
                SendWebsocketMessage(new LiveSplitWebsocketMessage<GetSplitNameWebsocketMessage>(command, response), clientConnection);
                break;
            }
            case "getlastsplittime":
            case "getprevioussplittime":
            {
                TimeSpan? time = State.CurrentSplitIndex > 0 ? State.Run[State.CurrentSplitIndex - 1].SplitTime[State.CurrentTimingMethod] : null;
                GetSplitTimeWebsocketMessage response = new(TimeFormatter.Format(time), time.HasValue ? time.Value.Seconds : 0);
                SendWebsocketMessage(new LiveSplitWebsocketMessage<GetSplitTimeWebsocketMessage>(command, response), clientConnection);
                break;
            }
            case "getcurrentsplittime":
            case "getcomparisonsplittime":
            {
                TimeSpan? time = null;

                if (State.CurrentSplit != null)
                {
                    string comparison = args.Length > 1 ? args[1] : State.CurrentComparison;
                    time = State.CurrentSplit.Comparisons[comparison][State.CurrentTimingMethod];
                }

                GetSplitTimeWebsocketMessage response = new(TimeFormatter.Format(time), time.HasValue ? time.Value.Seconds : 0);
                SendWebsocketMessage(new LiveSplitWebsocketMessage<GetSplitTimeWebsocketMessage>(command, response), clientConnection);
                break;
            }
            case "getcurrentrealtime":
            {
                TimeSpan? time = GetCurrentTime(State, TimingMethod.RealTime);

                GetTimeWebsocketMessage response = new(TimeFormatter.Format(time), time.HasValue ? time.Value.Seconds : 0);
                SendWebsocketMessage(new LiveSplitWebsocketMessage<GetTimeWebsocketMessage>(command, response), clientConnection);
                break;
            }
            case "getcurrentgametime":
            {
                TimeSpan? time = GetCurrentTime(State, !State.IsGameTimeInitialized ? TimingMethod.RealTime : TimingMethod.GameTime);

                GetTimeWebsocketMessage response = new(TimeFormatter.Format(time), time.HasValue ? time.Value.Seconds : 0);
                SendWebsocketMessage(new LiveSplitWebsocketMessage<GetTimeWebsocketMessage>(command, response), clientConnection);
                break;
            }
            case "getcurrenttime":
            {
                TimeSpan? time = GetCurrentTime(State, State.CurrentTimingMethod == TimingMethod.GameTime && !State.IsGameTimeInitialized ? TimingMethod.RealTime : State.CurrentTimingMethod);

                GetTimeWebsocketMessage response = new(TimeFormatter.Format(time), time.HasValue ? time.Value.Seconds : 0);
                SendWebsocketMessage(new LiveSplitWebsocketMessage<GetTimeWebsocketMessage>(command, response), clientConnection);
                break;
            }
            case "getfinaltime":
            case "getfinalsplittime":
            {
                string comparison = args.Length > 1 ? args[1] : State.CurrentComparison;
                TimeSpan? time = (State.CurrentPhase == TimerPhase.Ended)
                    ? State.CurrentTime[State.CurrentTimingMethod]
                    : State.Run.Last().Comparisons[comparison][State.CurrentTimingMethod];

                GetTimeWebsocketMessage response = new(TimeFormatter.Format(time), time.HasValue ? time.Value.Seconds : 0);
                SendWebsocketMessage(new LiveSplitWebsocketMessage<GetTimeWebsocketMessage>(command, response), clientConnection);
                break;
            }
            case "getbestpossibletime":
            case "getpredictedtime":
            {
                string comparison;
                if (command == "getbestpossibletime")
                {
                    comparison = LiveSplit.Model.Comparisons.BestSegmentsComparisonGenerator.ComparisonName;
                }
                else
                {
                    comparison = args.Length > 1 ? args[1] : State.CurrentComparison;
                }

                TimeSpan? prediction = PredictTime(State, comparison);

                GetTimeWebsocketMessage response = new(TimeFormatter.Format(prediction), prediction.HasValue ? prediction.Value.Seconds : 0);
                SendWebsocketMessage(new LiveSplitWebsocketMessage<GetTimeWebsocketMessage>(command, response), clientConnection);
                break;
            }
            case "gettimerphase":
            case "getcurrenttimerphase":
            {
                GetTimerPhaseWebsocketMessage response = new(State.CurrentPhase.ToString());
                SendWebsocketMessage(new LiveSplitWebsocketMessage<GetTimerPhaseWebsocketMessage>(command, response), clientConnection);
                break;
            }
            case "setcomparison":
            {
                State.CurrentComparison = args[1];
                break;
            }
            case "switchto":
            {
                switch (args[1])
                {
                    case "gametime":
                        State.CurrentTimingMethod = TimingMethod.GameTime;
                        break;
                    case "realtime":
                        State.CurrentTimingMethod = TimingMethod.RealTime;
                        break;
                }

                break;
            }
            case "setsplitname":
            case "setcurrentsplitname":
            {
                if (args.Length < 2)
                {
                    Log.Error($"[Server] Command {command} incorrect usage: missing one or more arguments.");
                    break;
                }

                int index = State.CurrentSplitIndex;
                string title = args[1];

                if (command == "setsplitname")
                {
                    string[] options = args[1].Split([' '], 2);

                    if (options.Length < 2)
                    {
                        Log.Error($"[Server] Command {command} incorrect usage: missing one or more arguments.");
                        break;
                    }

                    if (!int.TryParse(options[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out index))
                    {
                        Log.Error($"[Server] Could not parse {options[0]} as a split index while setting split name.");
                        break;
                    }

                    title = options[1];
                }

                if (index >= 0 && index < State.Run.Count)
                {
                    State.Run[index].Name = title;
                    State.Run.HasChanged = true;
                }
                else
                {
                    Log.Warning($"[Server] Split index {index} out of bounds for command {command}");
                }

                break;
            }
            case "getcustomvariablevalue":
            {
                string value = State.Run.Metadata.CustomVariableValue(args[1]);

                // make sure response isn't null or empty, and doesn't contain line endings
                string newValue = string.IsNullOrEmpty(value) ? "-" : Regex.Replace(value, @"\r\n?|\n", " ");

                GetCustomVariableValueWebsocketMessage response = new(newValue);
                SendWebsocketMessage(new LiveSplitWebsocketMessage<GetCustomVariableValueWebsocketMessage>(command, response), clientConnection);
                break;
            }
            case "setcustomvariable":
            {
                if (args.Length < 2)
                {
                    Log.Error($"[Server] Command {command} incorrect usage: missing one or more arguments.");
                    break;
                }

                string[] options;
                try
                {
                    options = System.Text.Json.JsonSerializer.Deserialize<string[]>(args[1]);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    Log.Error($"[Server] Failed to parse JSON: {args[1]}");
                    break;
                }

                if (options == null || options.Length < 2)
                {
                    Log.Error($"[Server] Command {command} incorrect usage: missing one or more arguments.");
                    break;
                }

                State.Run.Metadata.SetCustomVariable(options[0], options[1]);
                break;
            }
            case "ping":
            {
                PongWebsocketMessage response = new();
                SendWebsocketMessage(new LiveSplitWebsocketMessage<PongWebsocketMessage>(command, response), clientConnection);
                break;
            }
            case "getattemptcount":
            {
                GetAttemptCountWebsocketMessage response = new(Model.CurrentState.Run.AttemptCount);
                SendWebsocketMessage(new LiveSplitWebsocketMessage<GetAttemptCountWebsocketMessage>(command, response), clientConnection);
                break;
            }
            case "getcompletedcount":
            {
                GetAttemptCountWebsocketMessage response = new(State.Run.AttemptHistory.Count(x => x.Time.RealTime != null));
                SendWebsocketMessage(new LiveSplitWebsocketMessage<GetAttemptCountWebsocketMessage>(command, response), clientConnection);

                break;
            }
            default:
            {
                Log.Error($"[Server] Invalid command: {message}");
                break;
            }
        }
    }

    private void SendWebsocketMessage<T>(LiveSplitWebsocketMessage<T> message, IConnection clientConnection)
    {
        if (clientConnection == null)
        {
            return;
        }

        clientConnection.SendMessage(JsonConvert.SerializeObject(message));
    }

    private void BroadcastWebsocketMessage<T>(LiveSplitWebsocketMessage<T> message)
    {
        if (WsServer == null || !WsServer.IsListening)
        {
            return;
        }

        WsServer.WebSocketServices.Broadcast(JsonConvert.SerializeObject(message));
    }

    private void tcpConnection_Disconnected(object sender, EventArgs e)
    {
        var connection = (TcpConnection)sender;
        connection.MessageReceived -= connection_MessageReceived;
        connection.Disconnected -= tcpConnection_Disconnected;
        TcpConnections.Remove(connection);
        connection.Dispose();
    }

    private void State_OnStart(object sender, EventArgs e)
    {
        if (AlwaysPauseGameTime)
        {
            State.IsGameTimePaused = true;
        }

        StateWebsocketMessage broadcast = new(State, TimeFormatter);
        BroadcastWebsocketMessage(new LiveSplitWebsocketMessage<StateWebsocketMessage>("broadcaststart", broadcast));
    }

    private TimeSpan? PredictTime(LiveSplitState state, string comparison)
    {
        if (state.CurrentPhase is TimerPhase.Running or TimerPhase.Paused)
        {
            TimeSpan? delta = LiveSplitStateHelper.GetLastDelta(state, state.CurrentSplitIndex, comparison, State.CurrentTimingMethod) ?? TimeSpan.Zero;
            TimeSpan? liveDelta = state.CurrentTime[State.CurrentTimingMethod] - state.CurrentSplit.Comparisons[comparison][State.CurrentTimingMethod];
            if (liveDelta > delta)
            {
                delta = liveDelta;
            }

            return delta + state.Run.Last().Comparisons[comparison][State.CurrentTimingMethod];
        }
        else if (state.CurrentPhase == TimerPhase.Ended)
        {
            return state.Run.Last().SplitTime[State.CurrentTimingMethod];
        }
        else
        {
            return state.Run.Last().Comparisons[comparison][State.CurrentTimingMethod];
        }
    }

    private TimeSpan? GetCurrentTime(LiveSplitState state, TimingMethod timingMethod)
    {
        if (state.CurrentPhase == TimerPhase.NotRunning)
        {
            return state.Run.Offset;
        }
        else
        {
            return state.CurrentTime[timingMethod];
        }
    }

    #region Websocket Events

    private void State_OnSplit(object sender, EventArgs e)
    {
        int previousIndex = State.CurrentSplitIndex - 1;
        ISegment previousSegment = State.Run[previousIndex];

        int currentIndex = State.CurrentSplitIndex;
        ISegment currentSegment = State.Run[currentIndex];

        StateWebsocketMessage state = new(State, TimeFormatter);
        SplitWebsocketMessage previousSplit = new(previousSegment, previousIndex, TimeFormatter);
        SplitWebsocketMessage currentSplit = new(currentSegment, currentIndex, TimeFormatter);
        MultiSplitWebsocketMessage multi = new(state, previousSplit, currentSplit);
        BroadcastWebsocketMessage(new LiveSplitWebsocketMessage<MultiSplitWebsocketMessage>("broadcastsplit", multi));
    }

    private void State_OnUndoSplit(object sender, EventArgs e)
    {
        int previousIndex = State.CurrentSplitIndex + 1;
        ISegment previousSegment = State.Run[previousIndex];

        int currentIndex = State.CurrentSplitIndex;
        ISegment currentSegment = State.Run[currentIndex];

        StateWebsocketMessage state = new(State, TimeFormatter);
        SplitWebsocketMessage previousSplit = new(previousSegment, previousIndex, TimeFormatter);
        SplitWebsocketMessage currentSplit = new(currentSegment, currentIndex, TimeFormatter);
        MultiSplitWebsocketMessage multi = new(state, previousSplit, currentSplit);
        BroadcastWebsocketMessage(new LiveSplitWebsocketMessage<MultiSplitWebsocketMessage>("broadcastundosplit", multi));
    }

    private void State_OnSkipSplit(object sender, EventArgs e)
    {
        int previousIndex = State.CurrentSplitIndex - 1;
        ISegment previousSegment = State.Run[previousIndex];

        int currentIndex = State.CurrentSplitIndex;
        ISegment currentSegment = State.Run[currentIndex];

        StateWebsocketMessage state = new(State, TimeFormatter);
        SplitWebsocketMessage previousSplit = new(previousSegment, previousIndex, TimeFormatter);
        SplitWebsocketMessage currentSplit = new(currentSegment, currentIndex, TimeFormatter);
        MultiSplitWebsocketMessage multi = new(state, previousSplit, currentSplit);
        BroadcastWebsocketMessage(new LiveSplitWebsocketMessage<MultiSplitWebsocketMessage>("broadcastskipsplit", multi));
    }

    private void State_OnReset(object sender, TimerPhase phase)
    {
        ResetWebsocketMessage stateMessage = new(State, TimeFormatter);
        BroadcastWebsocketMessage(new LiveSplitWebsocketMessage<ResetWebsocketMessage>("broadcastreset", stateMessage));
    }

    private void State_OnPause(object sender, EventArgs e)
    {
        StateWebsocketMessage stateMessage = new(State, TimeFormatter);
        BroadcastWebsocketMessage(new LiveSplitWebsocketMessage<StateWebsocketMessage>("broadcastpause", stateMessage));
    }

    private void State_OnResume(object sender, EventArgs e)
    {
        StateWebsocketMessage stateMessage = new(State, TimeFormatter);
        BroadcastWebsocketMessage(new LiveSplitWebsocketMessage<StateWebsocketMessage>("broadcastresume", stateMessage));
    }

    #endregion

    public void Dispose()
    {
        State.OnStart -= State_OnStart;
        State.OnSplit -= State_OnSplit;
        State.OnUndoSplit -= State_OnUndoSplit;
        State.OnSkipSplit -= State_OnSkipSplit;
        State.OnReset -= State_OnReset;
        State.OnPause -= State_OnPause;
        //State.OnUndoAllPauses -= State_OnUndoAllPauses;
        State.OnResume -= State_OnResume;
        //State.OnScrollUp -= State_OnScrollUp;
        //State.OnScrollDown -= State_OnScrollDown;
        //State.OnSwitchComparisonPrevious -= State_OnSwitchComparisonPrevious;
        //State.OnSwitchComparisonNext -= State_OnSwitchComparisonNext;
        //State.RunManuallyModified -= State_RunManuallyModified;
        //State.ComparisonRenamed -= State_ComparisonRenamed;

        StopAll();

        WaitingServerPipe.Dispose();
    }
}
