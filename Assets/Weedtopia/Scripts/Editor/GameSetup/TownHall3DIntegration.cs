using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Weedtopia.Buildings.Presentation;
using Weedtopia.Editor.BuildingArtPipeline;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit single-slot installation of the user-approved town hall, preserving catalog identity.</summary>
    public static class TownHall3DIntegration
    {
        public const string ReportFolder="Docs/ArtProduction/Buildings/town_hall/T_FIXED/Meshy_Highpoly_2026-09-28/CityIntegration";
        public const string WrapperPath="Assets/Weedtopia/Art/Buildings/Prefabs/town_hall/WT_town_hall_T_FIXED.prefab";
        public const string ModelPath="Assets/Weedtopia/Art/Buildings/Archive/TownHall3D20260928/Model/WT_TownHall_LOD.prefab";
        private const string InstanceName="ApprovedTownHall3D";
        [Serializable] private sealed class Receipt
        {
            public string utc, wrapper, model, wrapperGuid, anchorsBefore, anchorsAfter;
            public bool userApproved, anchorsPreserved, sceneChanged;
            public int[] triangles;
            public Vector3 bounds;
        }

        [MenuItem("Weedtopia/Art Pipeline/3D Town Hall/Install Approved Model In City")]
        public static void Apply()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            if(!File.Exists(ReportFolder+"/approval.json")||!JsonUtility.FromJson<Approval>(File.ReadAllText(ReportFolder+"/approval.json")).approved)
                throw new InvalidOperationException("Explicit approval of the 2K and LOD review is required.");
            BuildingIntakeService.EnsureAssetFolder(Path.GetDirectoryName(ModelPath).Replace('\\','/'));
            if(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath)==null)
            {
                var source=PrefabUtility.LoadPrefabContents(TownHall3DReview.Root+"/WT_TownHall_3D_Review_LOD.prefab");
                try
                {
                    source.name="WT_TownHall_LOD";
                    var reviewWrapper=source.GetComponent<BuildingVisualWrapper>();
                    foreach(var anchor in new[]{reviewWrapper.InteractionAnchor,reviewWrapper.WorkerAnchor,reviewWrapper.DeliveryInputAnchor,reviewWrapper.DeliveryOutputAnchor,reviewWrapper.VfxAnchor})
                        Object.DestroyImmediate(anchor.gameObject);
                    Object.DestroyImmediate(reviewWrapper);
                    PrefabUtility.SaveAsPrefabAsset(source,ModelPath);
                }
                finally{PrefabUtility.UnloadPrefabContents(source);}
            }
            var prefab=PrefabUtility.LoadPrefabContents(WrapperPath);
            try
            {
                var wrapper=prefab.GetComponent<BuildingVisualWrapper>();
                string before=ProductionBuildingIntegration.AnchorState(wrapper);
                var model=wrapper.VisualRoot.Find(InstanceName);
                if(model==null)
                {
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath),wrapper.VisualRoot);
                    model=instance.transform;instance.name=InstanceName;
                    // The actual city camera looks along +Z; the approved source entrance faces +Z.
                    // Rotate only the new model, never the established wrapper or gameplay anchors.
                    model.localRotation=Quaternion.Euler(0,180,0);
                    model.localPosition=Vector3.zero;model.localScale=Vector3.one;
                }
                foreach(Transform child in wrapper.VisualRoot)child.gameObject.SetActive(child==model);
                // The original wrapper is backed up outside Assets. Remove only this identified,
                // retired mesh instance so Unity does not ship/load its textures as prefab dependencies.
                var retired=wrapper.VisualRoot.Cast<Transform>().FirstOrDefault(child=>child!=model&&
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromOriginalSource(child.gameObject)))=="b5bab00d5d931844a84e715a0a446c8a");
                if(retired!=null)Object.DestroyImmediate(retired.gameObject);
                wrapper.EditorSetModel(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath),model);
                var lods=model.GetComponent<LODGroup>().GetLODs();
                var counts=lods.Select(l=>l.renderers.Sum(r=>(int)(r.GetComponent<MeshFilter>().sharedMesh.GetIndexCount(0)/3))).ToArray();
                if(!counts.SequenceEqual(new[]{50000,20000,12000}))throw new InvalidDataException("Approved LOD geometry changed.");
                string after=ProductionBuildingIntegration.AnchorState(wrapper);
                if(before!=after)throw new InvalidOperationException("Existing anchor transforms changed.");
                PrefabUtility.SaveAsPrefabAsset(prefab,WrapperPath);
                var bounds=lods[0].renderers[0].bounds;
                Directory.CreateDirectory(ReportFolder);
                File.WriteAllText(ReportFolder+"/integration.json",JsonUtility.ToJson(new Receipt{utc=DateTime.UtcNow.ToString("O"),wrapper=WrapperPath,model=ModelPath,wrapperGuid=AssetDatabase.AssetPathToGUID(WrapperPath),anchorsBefore=before,anchorsAfter=after,userApproved=true,anchorsPreserved=true,sceneChanged=false,triangles=counts,bounds=bounds.size},true));
            }
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            AssetDatabase.SaveAssets();
        }
        [Serializable] private sealed class Approval {public bool approved = default;}

        public static void PrepareEvidence(GameRoot root,Action<string,Action> shot)
        {
            var site=root.Site("town_hall");
            var life=Object.FindFirstObjectByType<LivingTownPresentation>();
            if(life!=null){life.diagnosticFreeze=true;life.PresentAt(12);}
            void View(float zoom)
            {
                root.Ui.Close();root.Ui.Interaction.CloseBuilding();root.CameraRig.ShowOverview();
                root.CameraRig.SetDiagnosticZoom(zoom);root.CameraRig.Focus(site.transform.position);
            }
            shot("01_city_context",()=>View(48));
            shot("02_town_hall_close",()=>View(22));
            shot("03_selected",()=>root.Ui.Interaction.OpenBuilding("town_hall"));
            shot("04_town_hall_statistics",()=>root.Ui.Interaction.work.onClick.Invoke());
            shot("05_rocko_dialog",()=>{root.Ui.Close();root.Ui.Interaction.OpenBuilding("town_hall");root.Ui.Interaction.talk.onClick.Invoke();});
            shot("06_lod0_close",()=>{View(22);site.GetComponentInChildren<LODGroup>().ForceLOD(0);});
            shot("07_lod1_close",()=>site.GetComponentInChildren<LODGroup>().ForceLOD(1));
            shot("08_lod2_close",()=>site.GetComponentInChildren<LODGroup>().ForceLOD(2));
            shot("09_auto_lod",()=>{site.GetComponentInChildren<LODGroup>().ForceLOD(-1);View(32);});
        }
    }
}
