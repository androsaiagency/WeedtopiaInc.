using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Weedtopia.Editor.GameSetup
{
    [Serializable] public sealed class ReleaseInputFile
    {
        public string path, sha256;
        public long bytes;
    }

    [Serializable] public sealed class ReleaseInputManifest
    {
        public int schemaVersion = 2;
        public string fingerprint, buildContext;
        public ReleaseInputFile[] files;
    }

    /// <summary>Content-addressed release inputs. Does not write assets or issue acceptance.</summary>
    public static class ReleaseFingerprint
    {
        public static ReleaseInputManifest Capture(string root, IEnumerable<string> paths, string buildContext)
        {
            if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("Project root is required", nameof(root));
            if (paths == null) throw new ArgumentNullException(nameof(paths));
            if (string.IsNullOrWhiteSpace(buildContext)) throw new ArgumentException("Build context is required", nameof(buildContext));
            var manifest = new ReleaseInputManifest { buildContext = buildContext };
            using (var hash = SHA256.Create())
            {
                manifest.files = paths.Select(p => p.Replace('\\', '/')).Distinct(StringComparer.Ordinal)
                    .OrderBy(p => p, StringComparer.Ordinal).Select(path =>
                    {
                        string absolute = Path.Combine(root, path);
                        // Missing mandatory inputs are represented, never silently removed from the identity.
                        if (!File.Exists(absolute)) return new ReleaseInputFile { path = path, sha256 = "missing", bytes = -1 };
                        using (var stream = File.OpenRead(absolute))
                            return new ReleaseInputFile { path = path, sha256 = Hex(hash.ComputeHash(stream)), bytes = stream.Length };
                    }).ToArray();
                string inputs = "weedtopia-release-v2\n" + buildContext + "\n" + string.Join("\n",
                    manifest.files.Select(file => file.path + ":" + file.sha256));
                manifest.fingerprint = Hex(hash.ComputeHash(Encoding.UTF8.GetBytes(inputs)));
            }
            return manifest;
        }

        private static string Hex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }
}
