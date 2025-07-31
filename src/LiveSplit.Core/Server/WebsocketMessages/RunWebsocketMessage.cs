using System.Linq;

using LiveSplit.Model;
using LiveSplit.TimeFormatters;

namespace LiveSplit.Server.WebsocketMessages;

public class RunWebsocketMessage
{
    public string FilePath;
    public string LayoutPath;

    public string Game;
    public string Category;
    public int Attempt;

    public int SplitCount;

    public string Offset;
    public float OffsetSeconds;

    public string[] Comparisons;
    public string[] CustomComparisons;

    public RunWebsocketMessage(IRun run, ITimeFormatter formatter)
    {
        Game = run.GameName;
        Category = run.CategoryName;
        Attempt = run.AttemptCount;

        FilePath = run.FilePath;
        LayoutPath = run.LayoutPath;

        SplitCount = run.Count;
        Comparisons = run.Comparisons.ToArray();
        CustomComparisons = run.CustomComparisons.ToArray();

        Offset = formatter.Format(run.Offset);
        OffsetSeconds = (float)run.Offset.TotalSeconds;
    }
}
