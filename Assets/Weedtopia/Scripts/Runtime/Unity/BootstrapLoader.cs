using UnityEngine;
using UnityEngine.SceneManagement;

namespace Weedtopia.Unity
{
    public sealed class BootstrapLoader : MonoBehaviour
    {
        [SerializeField] private string gameplayScene="Assets/Weedtopia/Game/Scenes/BurningHills.unity";
        private void Start(){SceneManager.LoadSceneAsync(gameplayScene,LoadSceneMode.Single);}
    }
}
