using System;
using System.IO;
using System.Linq;
using UnityEditor.Android;
using UnityEngine;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Bounds compression concurrency and rebuilds packaging in the owned Android cache.</summary>
    public sealed class AndroidBuildMemoryPolicy : IPostGenerateGradleAndroidProject
    {
        [Serializable] private sealed class ArchivedFolder
        {
            public string source, destination;
            public bool moved;
        }
        [Serializable] private sealed class ArchiveReceipt
        {
            public string utc, status, gradleProject;
            public ArchivedFolder[] folders;
        }
        public int callbackOrder => 100;
        public void OnPostGenerateGradleAndroidProject(string unityLibraryPath)
        {
            ArchiveOwnedPackaging(unityLibraryPath);
            string file = Path.Combine(Directory.GetParent(unityLibraryPath).FullName, "gradle.properties");
            var retained = File.ReadAllLines(file).Where(line =>
                !line.TrimStart().StartsWith("org.gradle.workers.max=") &&
                !line.TrimStart().StartsWith("org.gradle.parallel="));
            File.WriteAllLines(file, retained.Concat(new[] { "org.gradle.workers.max=2", "org.gradle.parallel=false" }));
        }
        private static string Absolute(string path) => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        private static void RequireContainedWithoutLinks(string path, string parent)
        {
            if (!path.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Packaging archive path is outside its exact owned parent: " + path);
            for (var current = new DirectoryInfo(path); current != null && !string.Equals(current.FullName, parent, StringComparison.OrdinalIgnoreCase); current = current.Parent)
                if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Packaging archive refuses redirected directory: " + current.FullName);
        }
        private static void ArchiveOwnedPackaging(string unityLibraryPath)
        {
            string workspace = Absolute(Directory.GetParent(UnityEngine.Application.dataPath).FullName);
            string gradle = Absolute(Path.Combine(workspace, "Library/Bee/Android/Prj/IL2CPP/Gradle"));
            // Exports and foreign Gradle projects retain their existing files and packaging state.
            if (UnityEditor.EditorUserBuildSettings.exportAsGoogleAndroidProject
                || !string.Equals(Absolute(unityLibraryPath), Absolute(Path.Combine(gradle, "unityLibrary")), StringComparison.OrdinalIgnoreCase)) return;
            string build = Absolute(Path.Combine(gradle, "launcher/build"));
            string[] relative = { "outputs/apk/debug", "intermediates/incremental/packageDebug", "outputs/apk/release", "intermediates/incremental/packageRelease" };
            string[] sources = relative.Select(path => Absolute(Path.Combine(build, path))).ToArray();
            // Validate every absolute source before the first move, including its workspace ancestry.
            foreach (string source in sources) { RequireContainedWithoutLinks(source, build); RequireContainedWithoutLinks(source, workspace); }
            if (!sources.Any(Directory.Exists)) return;
            string archive = Absolute(Path.Combine(workspace, ".utmp/AndroidPackagingArchive",
                DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + "-" + Guid.NewGuid().ToString("N")));
            RequireContainedWithoutLinks(archive, workspace);
            var folders = sources.Select((source, index) => new ArchivedFolder
                { source = source, destination = Absolute(Path.Combine(archive, relative[index])) }).ToArray();
            foreach (var folder in folders) RequireContainedWithoutLinks(folder.destination, archive);
            if (Directory.Exists(archive)) throw new IOException("Packaging archive already exists: " + archive);
            Directory.CreateDirectory(archive);
            var receipt = new ArchiveReceipt { utc = DateTime.UtcNow.ToString("O"), status = "archiving", gradleProject = gradle, folders = folders };
            string receiptPath = Path.Combine(archive, "archive.json");
            File.WriteAllText(receiptPath, JsonUtility.ToJson(receipt, true));
            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder.source)) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(folder.destination));
                Directory.Move(folder.source, folder.destination);
                folder.moved = true;
                File.WriteAllText(receiptPath, JsonUtility.ToJson(receipt, true));
            }
            receipt.status = "archived";
            File.WriteAllText(receiptPath, JsonUtility.ToJson(receipt, true));
            Debug.Log("Archived previous owned Android packaging for a fresh Gradle package: " + archive);
        }
    }
}
