using UnityEngine;

namespace Weedtopia.Unity
{
    /// <summary>Serialized visual network. Simulation routes retain their original identity and endpoints.</summary>
    public sealed class ConveyorNetwork : MonoBehaviour
    {
        public const float BeltWidth = 6;
        public ConveyorSection[] sections;
        public PresentationRoute[] routes;
        public bool diagnosticFreeze;

        public void Animate(float delta, bool paused, Camera camera)
        {
            foreach (var section in sections) section.Present(delta, paused);
            foreach (var route in routes) route.Animate(delta, paused, camera);
        }
    }
}
