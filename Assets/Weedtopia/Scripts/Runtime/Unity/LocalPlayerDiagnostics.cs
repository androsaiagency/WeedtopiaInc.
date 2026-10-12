using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>Bounded local technical events. Never records log messages, player names, save data or device identifiers.</summary>
    public static class LocalPlayerDiagnostics
    {
        public enum Operation { Save, Load, OfflineContinuation, StartupLoad, OfflineSimulation, OfflineCommit, StartupReady }
        [Serializable] public sealed class TechnicalEvent
        {
            public string schema = "weedtopia_local_diagnostic_v1", utc = "", buildGuid = "", unity = "", version = "", platform = "", type = "", code = "", operation = "";
            public string[] frames = Array.Empty<string>();
            public double elapsedMs = -1;
            public long offlineSeconds = -1;
            public int workers = -1, buildings = -1;
        }
        public const int MaximumEventsPerLaunch = 256;
        public const int MaximumFileBytes = 256 * 1024;
        private static readonly object Gate = new object();
        private static readonly Regex Frame = new Regex(@"(?:^|\n)\s*(?:at\s+)?(?<method>(?:Weedtopia|UnityEngine|System)\.[A-Za-z0-9_.+<>:]+)\s*\(", RegexOptions.CultureInvariant);
        private static readonly string[] ExceptionCodes = { "IOException", "InvalidDataException", "UnauthorizedAccessException", "ArgumentException", "NullReferenceException", "InvalidOperationException", "IndexOutOfRangeException", "OutOfMemoryException", "Exception" };
        private static string file = "", buildGuid = "", unity = "", version = "", platform = "";
        private static int events;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            UnityEngine.Application.logMessageReceivedThreaded -= OnLog;
            UnityEngine.Application.quitting -= Shutdown;
            lock (Gate) { file = ""; events = 0; }
            if (UnityEngine.Application.isEditor) return;
            buildGuid = Token(UnityEngine.Application.buildGUID); unity = Token(UnityEngine.Application.unityVersion);
            version = Token(UnityEngine.Application.version); platform = UnityEngine.Application.platform.ToString();
            try
            {
                string folder = Path.Combine(UnityEngine.Application.persistentDataPath, "WeedtopiaDiagnostics");
                Directory.CreateDirectory(folder);
                lock (Gate) file = Path.Combine(folder, "player-events.ndjson");
                UnityEngine.Application.logMessageReceivedThreaded += OnLog;
                UnityEngine.Application.quitting += Shutdown;
                Write(CreateLaunchEvent());
            }
            catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
        private static void Shutdown()
        {
            UnityEngine.Application.logMessageReceivedThreaded -= OnLog;
            UnityEngine.Application.quitting -= Shutdown;
            lock (Gate) file = "";
        }
        private static string Token(string value)
        {
            string token = Regex.Replace(value ?? "", "[^A-Za-z0-9._-]", "");
            return token.Substring(0, Math.Min(128, token.Length));
        }
        internal static TechnicalEvent CreateLaunchEvent()
            => new TechnicalEvent {utc=DateTime.UtcNow.ToString("O"),buildGuid=buildGuid,unity=unity,version=version,platform=platform,
                type="Lifecycle",code="PlayerStarted",frames=Array.Empty<string>()};
        public static void RecordException(Operation operation, Exception error)
        {
            string type = error == null ? "Exception" : error.GetType().Name;
            if (!ExceptionCodes.Contains(type)) type = "Exception";
            var record = CreateEvent(LogType.Exception, type + ":", error == null ? "" : error.StackTrace);
            record.operation = operation.ToString();
            Write(record);
        }
        /// <summary>Records only finite, nonnegative technical startup measurements; invalid observations are discarded.</summary>
        public static void RecordTiming(Operation operation, double elapsedMs, long offlineSeconds, int workers, int buildings)
        {
            var record = CreateTimingEvent(operation, elapsedMs, offlineSeconds, workers, buildings);
            if (record != null) Write(record);
        }
        public static TechnicalEvent CreateTimingEvent(Operation operation, double elapsedMs, long offlineSeconds, int workers, int buildings)
        {
            if (operation != Operation.StartupLoad && operation != Operation.OfflineSimulation && operation != Operation.OfflineCommit && operation != Operation.StartupReady) return null;
            if (double.IsNaN(elapsedMs) || double.IsInfinity(elapsedMs) || elapsedMs < 0 || offlineSeconds < 0 || workers < 0 || buildings < 0) return null;
            return new TechnicalEvent
            {
                utc = DateTime.UtcNow.ToString("O"), buildGuid = buildGuid, unity = unity, version = version, platform = platform,
                type = "Timing", code = "StartupPhase", operation = operation.ToString(),
                elapsedMs = elapsedMs, offlineSeconds = offlineSeconds, workers = workers, buildings = buildings
            };
        }
        public static TechnicalEvent CreateEvent(LogType type, string condition, string stack)
        {
            string trimmed = (condition ?? "").TrimStart();
            string code = type == LogType.Warning && trimmed.StartsWith("Save operation failed:", StringComparison.Ordinal) ? "StorageOperationFailed" : "Unclassified";
            foreach (string candidate in ExceptionCodes)
                if (trimmed.StartsWith(candidate + ":", StringComparison.Ordinal) || trimmed.StartsWith("System." + candidate + ":", StringComparison.Ordinal)) { code = candidate; break; }
            return new TechnicalEvent
            {
                utc = DateTime.UtcNow.ToString("O"), buildGuid = buildGuid, unity = unity, version = version, platform = platform,
                type = type.ToString(), code = code,
                frames = Frame.Matches(stack ?? "").Cast<Match>().Take(16).Select(match => match.Groups["method"].Value.Replace(':', '.'))
                    .Where(method => method.Length <= 160).ToArray()
            };
        }
        private static void OnLog(string condition, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert && type != LogType.Warning) return;
            // JsonUtility.ToJson supports background threads; all Unity context is captured on the main thread at initialization.
            Write(CreateEvent(type, condition, stack));
        }
        private static void Write(TechnicalEvent record)
        {
            string line = JsonUtility.ToJson(record) + "\n";
            lock (Gate)
            {
                if (file.Length == 0 || events >= MaximumEventsPerLaunch) return;
                events++;
                try
                {
                    int bytes = Encoding.UTF8.GetByteCount(line);
                    if (bytes > MaximumFileBytes) return;
                    if (File.Exists(file) && new FileInfo(file).Length + bytes > MaximumFileBytes)
                    {
                        string previous = file + ".previous";
                        if (File.Exists(previous)) File.Delete(previous);
                        File.Move(file, previous);
                    }
                    File.AppendAllText(file, line, new UTF8Encoding(false));
                }
                catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }
        /// <summary>Returns the already redacted local event snapshot for an explicit support export; performs no upload.</summary>
        public static string ReadSnapshot()
        {
            lock (Gate)
            {
                return ReadSnapshotFiles(file);
            }
        }
        internal static string ReadSnapshotFiles(string currentFile)
        {
            if(string.IsNullOrEmpty(currentFile))return "";
            var lines=new Queue<string>();int bytes=0;
            foreach(string path in new[]{currentFile+".previous",currentFile})
            {
                try
                {
                    if(!File.Exists(path)||new FileInfo(path).Length>MaximumFileBytes)continue;
                    using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))
                    {
                        if(stream.Length==0||stream.Length>MaximumFileBytes)continue;
                        stream.Position=stream.Length-1;bool completeTail=stream.ReadByte()==10;stream.Position=0;
                        using(var reader=new StreamReader(stream,Encoding.UTF8,true))
                        {
                            string line;
                            while((line=reader.ReadLine())!=null)
                            {
                                if(reader.EndOfStream&&!completeTail)break;
                                if(string.IsNullOrWhiteSpace(line))continue;
                                line+="\n";int lineBytes=Encoding.UTF8.GetByteCount(line);
                                if(lineBytes>MaximumFileBytes)continue;
                                while(bytes+lineBytes>MaximumFileBytes&&lines.Count>0)
                                    bytes-=Encoding.UTF8.GetByteCount(lines.Dequeue());
                                lines.Enqueue(line);bytes+=lineBytes;
                            }
                        }
                    }
                }
                catch(IOException) { } catch(UnauthorizedAccessException) { }
            }
            return string.Concat(lines);
        }
    }
}
