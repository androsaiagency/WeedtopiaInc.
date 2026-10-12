using System.IO;
using System.Linq;
using UnityEditor.Android;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Bounds concurrent asset compression during an explicitly requested Android build or export.</summary>
    public sealed class AndroidBuildMemoryPolicy : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 100;
        public void OnPostGenerateGradleAndroidProject(string unityLibraryPath)
        {
            string file = Path.Combine(Directory.GetParent(unityLibraryPath).FullName, "gradle.properties");
            var retained = File.ReadAllLines(file).Where(line =>
                !line.TrimStart().StartsWith("org.gradle.workers.max=") &&
                !line.TrimStart().StartsWith("org.gradle.parallel="));
            File.WriteAllLines(file, retained.Concat(new[] { "org.gradle.workers.max=2", "org.gradle.parallel=false" }));
        }
    }
}
