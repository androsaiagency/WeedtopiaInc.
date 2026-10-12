using UnityEngine.SceneManagement;

namespace Weedtopia.Unity
{
    /// <summary>Replaceable scene-loading boundary; the authored root retains session/handoff ownership.</summary>
    public interface ICitySceneLoader
    {
        bool Start(string scenePath);
    }

    public sealed class UnityCitySceneLoader : ICitySceneLoader
    {
        public bool Start(string scenePath) => SceneManager.LoadSceneAsync(scenePath,LoadSceneMode.Single)!=null;
    }
}
