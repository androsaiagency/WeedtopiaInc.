using UnityEngine;
using Weedtopia.Domain;

namespace Weedtopia.Unity
{
    public sealed class BuildingView : MonoBehaviour
    {
        [SerializeField] private string instanceId;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private GameObject selection;
        [SerializeField] private TextMesh label;
        [SerializeField] private Renderer statusLamp;
        [SerializeField] private ParticleSystem upgradeEffect = default;
        [SerializeField] private ConstructionFenceView constructionFence = default;
        private int previousLevel=-1;
        private GameRoot root;
        private GameObject visual,shownPrefab;
        private MaterialPropertyBlock properties;
        private BoxCollider parcelBox;
        private BoxCollider[] visualBoxes=System.Array.Empty<BoxCollider>();
        private BoxCollider spriteBox;
        private MeshRenderer[] visualMeshes=System.Array.Empty<MeshRenderer>();
        public string InstanceId=>instanceId;
        public Transform VisualRoot=>visualRoot;
        public ConstructionFenceView ConstructionFence=>constructionFence;
        public float CurrentFootprintWidth=>spriteBox!=null?Mathf.Abs(spriteBox.transform.lossyScale.x)*spriteBox.size.x:TryGetCurrentVisualBounds(out var bounds)?bounds.size.x:8;
        public bool TryGetCurrentSpriteBounds(out Bounds bounds)
        {
            var sprite=visual!=null?visual.GetComponentInChildren<Weedtopia.Buildings.Presentation.BuildingSpriteVisual>():null;
            bounds=sprite!=null?sprite.SpriteRenderer.bounds:default;
            return sprite!=null&&sprite.SpriteRenderer.sprite!=null;
        }
        public bool TryGetCurrentVisualBounds(out Bounds bounds)
        {
            if(TryGetCurrentSpriteBounds(out bounds))return true;
            bool found=false;
            if(visual==null)return false;
            // All authored LOD meshes contribute a stable envelope, independent of camera distance.
            foreach(var renderer in visualMeshes)
            {
                if(!found){bounds=renderer.bounds;found=true;}
                else bounds.Encapsulate(renderer.bounds);
            }
            return found;
        }
        public void Initialize(GameRoot value){root=value;properties=new MaterialPropertyBlock();parcelBox=GetComponent<BoxCollider>();}
        public bool TryPick(Ray ray,out float distance)
        {
            bool found=AuthoredBoxPicking.TryHit(parcelBox,ray,out distance);
            foreach(var box in visualBoxes)if(AuthoredBoxPicking.TryHit(box,ray,out float hit)&&hit<distance){distance=hit;found=true;}
            return found;
        }
        public void Refresh()
        {
            if(root.Session==null)return;
            var b=root.Session.State.Find(instanceId);var d=root.Definitions.Building(b.family);
            if(previousLevel>=0&&b.level>previousLevel&&upgradeEffect!=null)upgradeEffect.Play();previousLevel=b.level;
            // An unchosen residential parcel uses the accepted residential ruin until the player chooses its family.
            string family=b.family=="settlement_plot"&&root.UsesProductionBuildings?"residential_house":b.family;
            if(!root.Visuals.TryGetVisual(family,b.level,out var prefab))throw new System.InvalidOperationException("Missing building visual: "+family+"/"+b.level);
            if(prefab!=shownPrefab)
            {
                if(visual!=null)Destroy(visual);
                visual=Instantiate(prefab,visualRoot,false);shownPrefab=prefab;
                visualBoxes=visual.GetComponentsInChildren<BoxCollider>();
                visualMeshes=visual.GetComponentsInChildren<MeshRenderer>();
                var sprite=visual.GetComponentInChildren<Weedtopia.Buildings.Presentation.BuildingSpriteVisual>();
                spriteBox=sprite!=null?sprite.GetComponent<BoxCollider>():null;
            }
            if(constructionFence!=null)constructionFence.Present(b.buildingSeconds>0,CurrentFootprintWidth,root.CameraRig.RenderCamera);
            selection.SetActive(root.SelectedId==instanceId&&(root.Ui.Interaction==null||root.Ui.Interaction.BuildingOpen));
            bool locked=d.zone>root.Session.State.City.unlocked;
            // The authored information panel carries name and build time at every zoom level.
            label.gameObject.SetActive(!root.UsesProductionBuildings&&root.SelectedId==instanceId);
            label.text=root.Definitions.Text(d.nameKey)+(b.buildingSeconds>0?"\n"+BuildingInspectionMetrics.FormatDuration(b.buildingSeconds):"");
            label.transform.rotation=root.CameraRig.RenderCamera.transform.rotation;
            Color color=locked?new Color(.3f,.35f,.45f):!string.IsNullOrEmpty(b.bottleneck)?new Color(1,.55f,.15f):new Color(.35f,1,.65f);
            properties.SetColor("_BaseColor",color);properties.SetColor("_Color",color);statusLamp.SetPropertyBlock(properties);
            if(statusLamp is SpriteRenderer statusSprite){statusSprite.color=color;statusSprite.sortingOrder=WorldSort.At(transform.position)+5;}
            statusLamp.enabled=!root.UsesProductionBuildings;
        }
#if UNITY_EDITOR
        public void EditorConfigure(string id,Transform anchor,GameObject ring,TextMesh text,Renderer lamp)
        {instanceId=id;visualRoot=anchor;selection=ring;label=text;statusLamp=lamp;}
        public void EditorSetId(string id){instanceId=id;}
#endif
    }
}
