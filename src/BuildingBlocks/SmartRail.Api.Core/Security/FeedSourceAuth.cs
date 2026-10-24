namespace SmartRail.Api.Core.Security;

// How a machine that sends live data (the train detection feed, the simulator) proves who it is.
public static class FeedSourceAuth
{
    public const string Scheme = "FeedKey";
    public const string Policy = "FeedSource";

    public const string CodeHeader = "X-Source-Code";
    public const string KeyHeader = "X-Source-Key";

    public const string SourceIdClaim = "source_id";
    public const string SourceCodeClaim = "source_code";
    public const string SourceRankClaim = "source_rank";
}
