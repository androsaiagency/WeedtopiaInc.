using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Buildings.Presentation;
using Weedtopia.Editor.BuildingArtPipeline;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    public static class ProductionBuildingIntegration
    {
        public const string ResourceFolder = "Assets/Weedtopia/Production/Resources/WeedtopiaBuildings/";
        public const string ApprovalPath = "Docs/LayoutRevision/building_approval.json";
        [Serializable] private sealed class Approval { public bool approved; }
        [Serializable] public sealed class Cutout { public string key, sprite, spriteSha256; public float worldHeight; public int width, height; public int[] alphaBounds; }
        [Serializable] public sealed class Cutouts { public Cutout[] slots; }
        [Serializable] private sealed class IntegrationReceipt { public string utc; public bool userApproved, catalogReferencesPreserved, anchorsPreserved; public int integrated; public string[] slots; }
        public static bool HasApproval() => File.Exists(ApprovalPath) && JsonUtility.FromJson<Approval>(File.ReadAllText(ApprovalPath)).approved;
        public static string SpritePath(string resource) => "Assets/Weedtopia/Production/Resources/" + resource + ".png";

        [MenuItem("Weedtopia/Production/Integrate Approved Building Sprites")]
        public static void Apply()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            if (!HasApproval()) throw new InvalidOperationException("Explicit four-prototype approval is required.");
            var entries = JsonUtility.FromJson<Cutouts>(File.ReadAllText("Docs/LayoutRevision/Cutouts/manifest.json")).slots.ToDictionary(x => x.key);
            var manifest = BuildingManifestReader.Load();
            if(entries.Count!=251)throw new InvalidOperationException("251 individually prepared sources required.");
            // Validate the whole input set before changing any production wrapper.
            using(var sha=SHA256.Create())foreach(var slot in manifest.slots)
            {
                if(!entries.TryGetValue(slot.Key,out var e)||!File.Exists(e.sprite))throw new InvalidOperationException("Missing cutout "+slot.Key);
                string hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(e.sprite))).Replace("-","").ToLowerInvariant();
                if(hash!=e.spriteSha256)throw new InvalidOperationException("Cutout changed since preparation: "+slot.Key);
            }
            var completed=new List<string>();
            foreach(var slot in manifest.slots)
            {
                var entry=entries[slot.Key];Import(entry.sprite);
                var prefab=PrefabUtility.LoadPrefabContents(slot.unity_prefab_target);
                try
                {
                    var wrapper=prefab.GetComponent<BuildingVisualWrapper>();
                    // An explicit model installation owns this slot; sprite repair must preserve it.
                    if(wrapper.ManagedModel!=null&&wrapper.ManagedModel.gameObject.activeSelf&&wrapper.ManagedModel.GetComponent<LODGroup>()!=null)
                    {completed.Add(slot.Key);continue;}
                    string before=AnchorState(wrapper);var old=wrapper.VisualRoot.Find("ProductionSpriteVisual");
                    if(old==null || old.GetComponent<BuildingSpriteVisual>()?.ResourcePath=="WeedtopiaBuildings/"+slot.Key)
                    {
                        var child=old!=null?old.gameObject:new GameObject("ProductionSpriteVisual",typeof(SpriteRenderer),typeof(BuildingSpriteVisual));child.transform.SetParent(wrapper.VisualRoot,false);
                        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(entry.sprite);
                        float scale=entry.worldHeight/sprite.bounds.size.y;
                        child.transform.localScale=Vector3.one*scale;
                        child.transform.localRotation=Quaternion.Euler(35.264f,45,0);
                        var visual=child.GetComponent<BuildingSpriteVisual>();
                        // Place the footprint centre, rather than its nearest corner, at the parcel origin.
                        float margin=(entry.height-entry.alphaBounds[3])/(float)entry.height*entry.worldHeight;
                        child.transform.localPosition=-(Quaternion.Euler(35.264f,45,0)*Vector3.up)*(margin+visual.FootprintInset);
                        visual.EditorSetFootprintInset(FootprintInset(entry));
                        var renderer=child.GetComponent<SpriteRenderer>();renderer.color=Color.white;
                        visual.EditorConfigure(renderer,true,entry.worldHeight);
                        visual.EditorSetResource("WeedtopiaBuildings/"+slot.Key);
                        var hit=child.GetComponent<BoxCollider>();if(hit==null)hit=child.AddComponent<BoxCollider>();
                        float pixelsPerUnit=sprite.pixelsPerUnit;
                        hit.size=new Vector3((entry.alphaBounds[2]-entry.alphaBounds[0])/pixelsPerUnit,(entry.alphaBounds[3]-entry.alphaBounds[1])/pixelsPerUnit,.15f);
                        hit.center=new Vector3((entry.alphaBounds[0]+entry.alphaBounds[2]-entry.width)*.5f/pixelsPerUnit,(entry.height-(entry.alphaBounds[1]+entry.alphaBounds[3])*.5f)/pixelsPerUnit,0);
                        // No serialized texture reference: the displayed tier acquires its sprite on enable.
                        renderer.sprite=null;
                        if(wrapper.ManagedModel!=null)wrapper.ManagedModel.gameObject.SetActive(false);
                        if(before!=AnchorState(wrapper))throw new InvalidOperationException("Anchor contract changed: "+slot.Key);
                        PrefabUtility.SaveAsPrefabAsset(prefab,slot.unity_prefab_target);
                    }
                    else if(old.GetComponent<BuildingSpriteVisual>()?.ResourcePath!="WeedtopiaBuildings/"+slot.Key)
                        throw new InvalidOperationException("Existing manually edited production sprite binding preserved: "+slot.Key);
                    completed.Add(slot.Key);
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(GameScaffold.GameScene);var root=Object.FindFirstObjectByType<GameRoot>();
                var fields=new SerializedObject(root);fields.FindProperty("productionVisualsApproved").boolValue=true;fields.FindProperty("developmentVisuals").objectReferenceValue=null;fields.ApplyModifiedPropertiesWithoutUndo();
                root.Presentation.explicitBuildingApproval=true;
                DistrictLandscapeRefinement.ApplyToScene(root);
                var map=AssetDatabase.LoadAssetAtPath<DistrictMap>(BurningHillsLayoutInstaller.MapPath);
                foreach(var district in map.districts)district.zoom=district.zone==Weedtopia.Domain.Zone.Industry?78:district.zone==Weedtopia.Domain.Zone.Farm?48:44;
                map.Range(Weedtopia.Domain.Zone.Farm).start.x=265;EditorUtility.SetDirty(map);
                var message=root.Ui.GetComponentsInChildren<UnityEngine.UI.Text>(true).Single(x=>x.name=="message");
                if(message!=null){var rt=message.rectTransform;rt.anchoredPosition=new Vector2(rt.anchoredPosition.x,250);}
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            finally { if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup); }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Docs/LayoutRevision/building_integration.json",JsonUtility.ToJson(new IntegrationReceipt{utc=DateTime.UtcNow.ToString("O"),userApproved=true,catalogReferencesPreserved=true,anchorsPreserved=true,integrated=completed.Count,slots=completed.ToArray()},true));
        }
        // Half the projected footprint depth. Roof eaves and planting extend beyond the base,
        // so use 22% of the visible width rather than the whole sprite/canvas height.
        private static float FootprintInset(Cutout entry) =>
            (entry.alphaBounds[2]-entry.alphaBounds[0])/(float)entry.height*entry.worldHeight*.22f;

        [MenuItem("Weedtopia/Production/Center Building Footprints On Parcels")]
        public static void CenterFootprints()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            if(!HasApproval())throw new InvalidOperationException("Building art approval required.");
            var entries=JsonUtility.FromJson<Cutouts>(File.ReadAllText("Docs/LayoutRevision/Cutouts/manifest.json")).slots.ToDictionary(x=>x.key);
            var completed=new List<string>();
            foreach(var slot in BuildingManifestReader.Load().slots)
            {
                var prefab=PrefabUtility.LoadPrefabContents(slot.unity_prefab_target);
                try
                {
                    var wrapper=prefab.GetComponent<BuildingVisualWrapper>();
                    string anchors=AnchorState(wrapper);
                    var visual=wrapper.VisualRoot.Find("ProductionSpriteVisual")?.GetComponent<BuildingSpriteVisual>();
                    if(visual==null||visual.ResourcePath!="WeedtopiaBuildings/"+slot.Key)
                        throw new InvalidOperationException("Preserve unexpected visual binding: "+slot.Key);
                    visual.EditorSetFootprintInset(FootprintInset(entries[slot.Key]));
                    if(anchors!=AnchorState(wrapper))throw new InvalidOperationException("Anchor changed: "+slot.Key);
                    PrefabUtility.SaveAsPrefabAsset(prefab,slot.unity_prefab_target);
                    completed.Add(slot.Key);
                }
                finally{PrefabUtility.UnloadPrefabContents(prefab);}
            }
            AssetDatabase.SaveAssets();Directory.CreateDirectory("Docs/BuildingAlignment");
            File.WriteAllText("Docs/BuildingAlignment/authoring.json",JsonUtility.ToJson(new IntegrationReceipt{utc=DateTime.UtcNow.ToString("O"),userApproved=true,catalogReferencesPreserved=true,anchorsPreserved=true,integrated=completed.Count,slots=completed.ToArray()},true));
        }

        public static string AnchorState(BuildingVisualWrapper w)
        {
            return string.Join("|",new[]{w.VisualRoot,w.InteractionAnchor,w.WorkerAnchor,w.DeliveryInputAnchor,w.DeliveryOutputAnchor,w.VfxAnchor,w.ColliderRoot}
                .Select(x=>x.name+":"+x.localPosition.ToString("R")+":"+x.localRotation.ToString("R")+":"+x.localScale.ToString("R")));
        }
        private static void Import(string path)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(importer.userData=="WeedtopiaPixelPreservingCutoutV1")return;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;importer.SetTextureSettings(settings);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePivot=new Vector2(.5f,0);importer.spritePixelsPerUnit=64;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.isReadable=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;importer.maxTextureSize=2048;
            importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.npotScale=TextureImporterNPOTScale.None;
            var desktop=importer.GetPlatformTextureSettings("Standalone");desktop.name="Standalone";desktop.overridden=true;desktop.maxTextureSize=2048;desktop.format=TextureImporterFormat.BC7;desktop.compressionQuality=100;importer.SetPlatformTextureSettings(desktop);
            importer.userData="WeedtopiaPixelPreservingCutoutV1";importer.SaveAndReimport();
        }
    }
}
