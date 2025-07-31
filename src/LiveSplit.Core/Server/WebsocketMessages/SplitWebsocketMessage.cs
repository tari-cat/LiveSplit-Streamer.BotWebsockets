using LiveSplit.Model;
using LiveSplit.TimeFormatters;

namespace LiveSplit.Server.WebsocketMessages;

public class SplitWebsocketMessage
{
    public int SplitIndex;
    public string SplitName;

    public string SplitGameTime;
    public string SplitRealTime;
    public string PersonalBestSplitGameTime;
    public string PersonalBestSplitRealTime;
    public string BestSplitGameTime;
    public string BestSplitRealTime;

    public float SplitGameTimeSeconds;
    public float SplitRealTimeSeconds;
    public float PersonalBestSplitGameTimeSeconds;
    public float PersonalBestSplitRealTimeSeconds;
    public float BestSplitGameTimeSeconds;
    public float BestSplitRealTimeSeconds;

    public SplitWebsocketMessage(ISegment segment, int index, ITimeFormatter formatter)
    {
        SplitIndex = index;
        SplitName = segment.Name;
        Time segmentSplitTime = segment.SplitTime;
        Time segmentPersonalBestTime = segment.PersonalBestSplitTime;
        Time segmentBestTime = segment.BestSegmentTime;

        SplitRealTime = formatter.Format(segmentSplitTime.RealTime);
        SplitGameTime = formatter.Format(segmentSplitTime.GameTime);
        PersonalBestSplitRealTime = formatter.Format(segmentPersonalBestTime.RealTime);
        PersonalBestSplitGameTime = formatter.Format(segmentPersonalBestTime.GameTime);
        BestSplitRealTime = formatter.Format(segmentBestTime.RealTime);
        BestSplitGameTime = formatter.Format(segmentBestTime.GameTime);

        SplitRealTimeSeconds = segmentSplitTime.RealTime.HasValue ? (float)segmentSplitTime.RealTime.Value.TotalSeconds : 0;
        SplitGameTimeSeconds = segmentSplitTime.GameTime.HasValue ? (float)segmentSplitTime.GameTime.Value.TotalSeconds : 0;
        PersonalBestSplitRealTimeSeconds = segmentPersonalBestTime.RealTime.HasValue ? (float)segmentPersonalBestTime.RealTime.Value.TotalSeconds : 0;
        PersonalBestSplitGameTimeSeconds = segmentPersonalBestTime.GameTime.HasValue ? (float)segmentPersonalBestTime.GameTime.Value.TotalSeconds : 0;
        BestSplitRealTimeSeconds = segmentBestTime.RealTime.HasValue ? (float)segmentBestTime.RealTime.Value.TotalSeconds : 0;
        BestSplitGameTimeSeconds = segmentBestTime.GameTime.HasValue ? (float)segmentBestTime.GameTime.Value.TotalSeconds : 0;
    }
}
