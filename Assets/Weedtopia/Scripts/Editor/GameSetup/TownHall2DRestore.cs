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
    /// <summary>Explicit restoration of the original authored sprite; retains the 3D experiment in an archive.</summary>
    public static class TownHall2DRestore
    {
        public const string ReportFolder="Docs/ArtProduction/Buildings/town_hall/T_FIXED/Meshy_Highpoly_2026-09-28/Restore2D";
        public const string ArchiveRoot="Assets/Weedtopia/Art/Buildings/Archive/TownHall3D20260928";
        public const string OriginalResource="WeedtopiaBuildings/town_hall/T_FIXED";
        [Serializable] private sealed class Receipt
        {
            public string utc, wrapperGuid, resource, archive, anchorsBefore, anchorsAfter;
            public bool originalSpriteActive, anchorsPreserved, spriteSettingsPreserved, noMeshDependencies;
        }

        [MenuItem("Weedtopia/Art Pipeline/Town Hall/Restore Original 2D Sprite")]
        public static void Apply()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            string path=TownHall3DIntegration.WrapperPath;
            Directory.CreateDirectory(ReportFolder+"/Backup");
            if(!File.Exists(ReportFolder+"/Backup/WT_town_hall_T_FIXED.prefab"))
                File.Copy(path,ReportFolder+"/Backup/WT_town_hall_T_FIXED.prefab");
            var prefab=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var wrapper=prefab.GetComponent<BuildingVisualWrapper>();
                var sprite=wrapper.VisualRoot.Find("ProductionSpriteVisual");
                var visual=sprite!=null?sprite.GetComponent<BuildingSpriteVisual>():null;
                if(visual==null||visual.ResourcePath!=OriginalResource||
                   AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Weedtopia/Production/Resources/"+OriginalResource+".png")==null)
                    throw new InvalidDataException("The original authored town hall sprite is required.");
                string before=ProductionBuildingIntegration.AnchorState(wrapper);
                string settings=EditorJsonUtility.ToJson(visual);
                var position=sprite.localPosition;var rotation=sprite.localRotation;var scale=sprite.localScale;
                var model=wrapper.VisualRoot.Find("ApprovedTownHall3D");
                bool changed=model!=null||!sprite.gameObject.activeSelf||wrapper.SourceModel!=null||wrapper.ManagedModel!=null;
                if(model!=null)Object.DestroyImmediate(model.gameObject);
                sprite.gameObject.SetActive(true);
                wrapper.EditorSetModel(null,null);
                string after=ProductionBuildingIntegration.AnchorState(wrapper);
                bool preserved=settings==EditorJsonUtility.ToJson(visual)&&position==sprite.localPosition&&rotation==sprite.localRotation&&scale==sprite.localScale;
                if(before!=after||!preserved)throw new InvalidOperationException("Original sprite settings or gameplay anchors changed.");
                if(prefab.GetComponentsInChildren<MeshRenderer>(true).Length!=0||prefab.GetComponentsInChildren<LODGroup>(true).Length!=0)
                    throw new InvalidDataException("Unexpected mesh remains in the production wrapper.");
                if(changed)PrefabUtility.SaveAsPrefabAsset(prefab,path);
                Archive("Assets/Weedtopia/Art/Buildings/Models/town_hall/T_FIXED/Approved20260928",ArchiveRoot+"/Model");
                Archive("Assets/Weedtopia/Art/Buildings/Prototypes3D/TownHall20260928",ArchiveRoot+"/Review");
                AssetDatabase.SaveAssets();
                bool clean=!AssetDatabase.GetDependencies(path,true).Any(p=>p.StartsWith(ArchiveRoot,StringComparison.Ordinal)||p.EndsWith(".fbx",StringComparison.OrdinalIgnoreCase));
                if(!clean)throw new InvalidDataException("The production wrapper still depends on the 3D experiment.");
                File.WriteAllText(ReportFolder+"/restoration.json",JsonUtility.ToJson(new Receipt{
                    utc=DateTime.UtcNow.ToString("O"),wrapperGuid=AssetDatabase.AssetPathToGUID(path),resource=visual.ResourcePath,
                    archive=ArchiveRoot,anchorsBefore=before,anchorsAfter=after,originalSpriteActive=true,
                    anchorsPreserved=true,spriteSettingsPreserved=preserved,noMeshDependencies=clean},true));
            }
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
        }

        private static void Archive(string source,string destination)
        {
            if(!AssetDatabase.IsValidFolder(source))
            {
                if(!AssetDatabase.IsValidFolder(destination))throw new DirectoryNotFoundException(source);
                return;
            }
            if(AssetDatabase.IsValidFolder(destination))throw new IOException("Archive already exists: "+destination);
            BuildingIntakeService.EnsureAssetFolder(ArchiveRoot);
            string error=AssetDatabase.MoveAsset(source,destination);
            if(!string.IsNullOrEmpty(error))throw new IOException(error);
        }

        public static void PrepareEvidence(GameRoot root,Action<string,Action> shot)
        {
            var site=root.Site("town_hall");
            if(!site.TryGetCurrentSpriteBounds(out _)||site.GetComponentInChildren<LODGroup>()!=null)
                throw new InvalidOperationException("Expected the restored town hall sprite in the real city.");
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
        }
    }
}
