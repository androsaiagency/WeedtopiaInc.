using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Retains native symbols together with the exact APK and native ELF identities they describe.</summary>
    public static class NativeSymbolArtifacts
    {
        [Serializable] public sealed class NativeIdentity
        {
            public string library = "", abi = "", apkEntry = "", symbolEntry = "", apkBuildId = "", symbolBuildId = "";
            public string apkSha256 = "", symbolSha256 = "";
            public long apkBytes = 0, symbolBytes = 0;
            public bool matched = false;
        }
        [Serializable] private sealed class Manifest
        {
            public string schema = "weedtopia_native_symbols_v1", status = "", utc = "", runId = "", buildGuid = "", unity = "";
            public string packageName = "", version = "", backend = "", architectures = "", buildOptions = "", symbolLevel = "", symbolFormat = "";
            public string apkFile = "", apkSha256 = "", symbolFile = "", symbolSha256 = "";
            public int versionCode = 0;
            public long apkBytes = 0, symbolBytes = 0;
            public bool nativeIdentitiesMatch = false, releaseAccepted = false, deviceAccepted = false, humanAccepted = false;
            public NativeIdentity[] nativeLibraries = Array.Empty<NativeIdentity>();
        }
        public static string Capture(BuildReport report, string runId, DateTime startedUtc)
        {
            if (report.summary.platform != BuildTarget.Android || report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Native-symbol capture requires a successful Android build.");
            string apk = Path.GetFullPath(report.summary.outputPath);
            string folder = Path.GetDirectoryName(apk);
            string[] candidates = Directory.GetFiles(folder, "*.symbols.zip", SearchOption.TopDirectoryOnly)
                .Where(path => Path.GetFileName(path).StartsWith(Path.GetFileNameWithoutExtension(apk) + "-", StringComparison.Ordinal)
                    && File.GetLastWriteTimeUtc(path) >= startedUtc.AddSeconds(-2)).ToArray();
            if (candidates.Length != 1) throw new IOException("The successful APK must have exactly one freshly generated matching symbols ZIP.");
            string archive = Path.Combine(folder, "NativeSymbols");
            if (Directory.Exists(archive)) throw new IOException("Refusing to replace an existing per-build symbol archive.");
            Directory.CreateDirectory(archive);
            string copy = Path.Combine(archive, Path.GetFileName(candidates[0]));
            File.Copy(candidates[0], copy, false);
            var identities = new List<NativeIdentity>();
            using (var player = ZipFile.OpenRead(apk)) using (var symbols = ZipFile.OpenRead(copy))
            {
                foreach (var entry in player.Entries.Where(entry => entry.FullName.StartsWith("lib/", StringComparison.Ordinal) && entry.FullName.EndsWith(".so", StringComparison.Ordinal)))
                {
                    string library = Path.GetFileName(entry.FullName);
                    if (library != "libmain.so" && library != "libunity.so" && library != "libil2cpp.so") continue;
                    string abi = entry.FullName.Split('/')[1];
                    var matches = symbols.Entries.Where(symbol => symbol.FullName.Split('/').Contains(abi)
                        && (Path.GetFileName(symbol.FullName) == library || Path.GetFileName(symbol.FullName) == library + ".sym" || Path.GetFileName(symbol.FullName) == library + ".dbg")).ToArray();
                    var identity = new NativeIdentity { library = library, abi = abi, apkEntry = entry.FullName, apkBytes = entry.Length };
                    using (var stream = entry.Open()) identity.apkBuildId = ReadElfBuildId(stream);
                    using (var stream = entry.Open()) identity.apkSha256 = HashStream(stream);
                    if (matches.Length == 1)
                    {
                        identity.symbolEntry = matches[0].FullName;
                        identity.symbolBytes = matches[0].Length;
                        using (var stream = matches[0].Open()) identity.symbolBuildId = ReadElfBuildId(stream);
                        using (var stream = matches[0].Open()) identity.symbolSha256 = HashStream(stream);
                    }
                    identity.matched = identity.apkBuildId.Length > 0 && identity.apkBuildId == identity.symbolBuildId;
                    identities.Add(identity);
                }
            }
            bool matched = identities.Count > 0 && identities.All(identity => identity.matched)
                && identities.Any(identity => identity.library == "libmain.so") && identities.Any(identity => identity.library == "libunity.so")
                && (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP || identities.Any(identity => identity.library == "libil2cpp.so"));
            var manifest = new Manifest
            {
                status = matched ? "passed" : "failed_native_identity", utc = DateTime.UtcNow.ToString("O"), runId = runId,
                buildGuid = report.summary.guid.ToString(), unity = UnityEngine.Application.unityVersion,
                packageName = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android), version = PlayerSettings.bundleVersion,
                versionCode = PlayerSettings.Android.bundleVersionCode, backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android).ToString(),
                architectures = PlayerSettings.Android.targetArchitectures.ToString(), buildOptions = report.summary.options.ToString(),
                symbolLevel = UserBuildSettings.DebugSymbols.level.ToString(), symbolFormat = UserBuildSettings.DebugSymbols.format.ToString(),
                apkFile = Path.GetFileName(apk), apkSha256 = HashFile(apk), apkBytes = new FileInfo(apk).Length,
                symbolFile = Path.GetFileName(copy), symbolSha256 = HashFile(copy), symbolBytes = new FileInfo(copy).Length,
                nativeLibraries = identities.ToArray(), nativeIdentitiesMatch = matched
            };
            string result = Path.Combine(archive, "symbols.json");
            File.WriteAllText(result, JsonUtility.ToJson(manifest, true));
            if (!matched) throw new IOException("Native symbols do not match the exact APK ELF build IDs; inspect " + result);
            return result;
        }
        private static string HashFile(string path)
        {
            using (var stream = File.OpenRead(path)) return HashStream(stream);
        }
        private static string HashStream(Stream stream) { using (var hash = SHA256.Create()) return Hex(hash.ComputeHash(stream)); }
        private static string Hex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        /// <summary>Reads GNU build IDs from bounded ELF PT_NOTE metadata without loading entire native libraries.</summary>
        public static string ReadElfBuildId(Stream stream)
        {
            var data = new byte[128 * 1024]; int length = 0, read;
            while (length < data.Length && (read = stream.Read(data, length, data.Length - length)) > 0) length += read;
            if (length < 64 || data[0] != 0x7f || data[1] != 'E' || data[2] != 'L' || data[3] != 'F' || data[5] != 1) return "";
            bool elf64 = data[4] == 2;
            if (!elf64 && data[4] != 1) return "";
            ulong offset = elf64 ? BitConverter.ToUInt64(data, 32) : BitConverter.ToUInt32(data, 28);
            int size = BitConverter.ToUInt16(data, elf64 ? 54 : 42), count = BitConverter.ToUInt16(data, elf64 ? 56 : 44);
            if (size < (elf64 ? 56 : 32) || offset > (ulong)length || (ulong)size * (ulong)count > (ulong)length - offset) return "";
            for (int i = 0; i < count; i++)
            {
                int header = (int)offset + i * size;
                if (BitConverter.ToUInt32(data, header) != 4) continue;
                ulong noteOffset = elf64 ? BitConverter.ToUInt64(data, header + 8) : BitConverter.ToUInt32(data, header + 4);
                ulong noteBytes = elf64 ? BitConverter.ToUInt64(data, header + 32) : BitConverter.ToUInt32(data, header + 16);
                if (noteOffset > (ulong)length || noteBytes > (ulong)length - noteOffset) continue;
                int cursor = (int)noteOffset, end = cursor + (int)noteBytes;
                while (cursor <= end - 12)
                {
                    uint names = BitConverter.ToUInt32(data, cursor), bytes = BitConverter.ToUInt32(data, cursor + 4), type = BitConverter.ToUInt32(data, cursor + 8);
                    if (names > (uint)(end - cursor - 12) || bytes > (uint)(end - cursor - 12)) break;
                    int name = cursor + 12; long desc = name + (((long)names + 3) & ~3L), next = desc + (((long)bytes + 3) & ~3L);
                    if (next > end) break;
                    if (type == 3 && names == 4 && data[name] == 'G' && data[name + 1] == 'N' && data[name + 2] == 'U' && data[name + 3] == 0 && bytes > 0)
                        return Hex(data.Skip((int)desc).Take((int)bytes).ToArray());
                    cursor = (int)next;
                }
            }
            return "";
        }
    }
}
