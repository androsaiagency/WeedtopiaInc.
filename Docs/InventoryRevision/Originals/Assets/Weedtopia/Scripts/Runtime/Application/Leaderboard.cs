using System;
using System.Collections.Generic;
using Weedtopia.Domain;

namespace Weedtopia.Application
{
    [Serializable] public sealed class LeaderboardEntry { public string player, cityId; public Amount earned; public long playSeconds; public bool freeplay; }
    public interface ILeaderboardService { bool IsOnline { get; } string StatusKey { get; } IReadOnlyList<LeaderboardEntry> Read(); void Record(LeaderboardEntry entry); }
    public sealed class DisabledOnlineLeaderboard : ILeaderboardService
    {
        public bool IsOnline => false;
        public string StatusKey => "online.disabled";
        public IReadOnlyList<LeaderboardEntry> Read() => Array.Empty<LeaderboardEntry>();
        public void Record(LeaderboardEntry entry) { /* Intentionally offline: no endpoint, credentials or network call. */ }
    }
}
