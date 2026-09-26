using UnityEngine;
namespace Weedtopia.Unity
{
    public sealed class GameFeedback : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private AudioClip success,error;
        public void Play(bool accepted){var clip=accepted?success:error;if(source!=null&&clip!=null)source.PlayOneShot(clip);}
#if UNITY_EDITOR
        public void EditorConfigure(AudioSource audio,AudioClip ok,AudioClip blocked){if(source==null)source=audio;if(success==null)success=ok;if(error==null)error=blocked;}
#endif
    }
}
