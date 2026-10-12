namespace Weedtopia.Application
{
    /// <summary>One completed batch of deterministic offline simulation; the final batch carries its saved summary.</summary>
    public sealed class OfflineProgress
    {
        public int CompletedSeconds { get; }
        public int TotalSeconds { get; }
        public OfflineSummary Summary { get; }
        public bool IsComplete => Summary != null;
        public double Fraction => TotalSeconds == 0 ? (IsComplete ? 1 : 0) : (double)CompletedSeconds / TotalSeconds;
        internal OfflineProgress(int completed, int total, OfflineSummary summary = null)
        { CompletedSeconds = completed; TotalSeconds = total; Summary = summary; }
    }
}
