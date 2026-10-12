using UnityEngine;

namespace Weedtopia.Buildings.Presentation
{
    /// <summary>Authored, serialized presentation structure. Does not create objects at runtime.</summary>
    [DisallowMultipleComponent]
    public sealed class BuildingVisualWrapper : MonoBehaviour
    {
        [SerializeField] private string buildingId;
        [SerializeField] private string tierId;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform interactionAnchor;
        [SerializeField] private Transform workerAnchor;
        [SerializeField] private Transform deliveryInputAnchor;
        [SerializeField] private Transform deliveryOutputAnchor;
        [SerializeField] private Transform vfxAnchor;
        [SerializeField] private Transform colliderRoot;
        [SerializeField] private GameObject sourceModel;
        [SerializeField] private Transform managedModel;

        public string BuildingId => buildingId;
        public string TierId => tierId;
        public Transform VisualRoot => visualRoot;
        public Transform InteractionAnchor => interactionAnchor;
        public Transform WorkerAnchor => workerAnchor;
        public Transform DeliveryInputAnchor => deliveryInputAnchor;
        public Transform DeliveryOutputAnchor => deliveryOutputAnchor;
        public Transform VfxAnchor => vfxAnchor;
        public Transform ColliderRoot => colliderRoot;
        public GameObject SourceModel => sourceModel;
        public Transform ManagedModel => managedModel;

#if UNITY_EDITOR
        public void EditorSetStructure(string building, string tier, Transform visual, Transform interaction,
            Transform worker, Transform input, Transform output, Transform vfx, Transform colliders)
        {
            buildingId = building;
            tierId = tier;
            visualRoot = visual;
            interactionAnchor = interaction;
            workerAnchor = worker;
            deliveryInputAnchor = input;
            deliveryOutputAnchor = output;
            vfxAnchor = vfx;
            colliderRoot = colliders;
        }

        public void EditorSetModel(GameObject source, Transform instance)
        {
            sourceModel = source;
            managedModel = instance;
        }
#endif
    }
}
