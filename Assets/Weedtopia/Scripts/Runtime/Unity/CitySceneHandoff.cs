using Weedtopia.Domain;
using Weedtopia.Application;

namespace Weedtopia.Unity
{
    /// <summary>One-shot handoff across scene destruction. Disk saves remain the durable source of truth.</summary>
    internal static class CitySceneHandoff
    {
        internal sealed class Context
        {
            public GameState state;
            public IClock clock;
            public string savePath;
            public string leaderboardPath;
            public OfflineSummary offline;
            public bool recovered;
            public System.Diagnostics.Stopwatch startupTimer;
        }
        private static Context pending;
        public static void Prepare(GameState state, IClock clock, string path, string scores, OfflineSummary offline = null, bool recovered = false, System.Diagnostics.Stopwatch startupTimer = null)
            => pending = new Context { state = state, clock = clock, savePath = path, leaderboardPath = scores, offline = offline, recovered = recovered, startupTimer = startupTimer };
        public static Context Consume(string city)
        {
            if (pending == null || pending.state.activeCity != city) return null;
            var result = pending; pending = null; return result;
        }
        public static void Clear() => pending = null;
    }
}
