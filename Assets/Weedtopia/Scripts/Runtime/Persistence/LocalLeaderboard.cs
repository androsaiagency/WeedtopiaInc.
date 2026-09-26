using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Weedtopia.Application;

namespace Weedtopia.Persistence
{
    public sealed class LocalLeaderboard : ILeaderboardService
    {
        [Serializable] private sealed class Entries { public List<LeaderboardEntry> entries = new List<LeaderboardEntry>(); }
        private readonly string path;
        public bool IsOnline => false;
        public string StatusKey => "local.title";
        public LocalLeaderboard(string path) { this.path = path; }
        public IReadOnlyList<LeaderboardEntry> Read()
        {
            if (!File.Exists(path)) return Array.Empty<LeaderboardEntry>();
            try { return JsonUtility.FromJson<Entries>(File.ReadAllText(path)).entries.Where(x=>x.earned.IsValid && x.earned>=0).OrderByDescending(x=>x.earned).Take(20).ToArray(); }
            catch(Exception ex) when(ex is IOException || ex is ArgumentException || ex is NullReferenceException) { return Array.Empty<LeaderboardEntry>(); }
        }
        public void Record(LeaderboardEntry entry)
        {
            var entries=Read().ToList(); var previous=entries.FirstOrDefault(x=>x.player==entry.player && x.cityId==entry.cityId);
            if(previous!=null && previous.earned>=entry.earned)return;
            if(previous!=null)entries.Remove(previous);entries.Add(entry);
            string json=JsonUtility.ToJson(new Entries{entries=entries.OrderByDescending(x=>x.earned).Take(20).ToList()},true);
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path+".tmp",json);
            if(File.Exists(path))File.Replace(path+".tmp",path,null);else File.Move(path+".tmp",path);
        }
    }
}
