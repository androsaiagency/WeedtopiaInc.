using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Domain;
using Weedtopia.Unity;
using Object=UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit authoring of the third UI revision, with shared assets and no startup repair.</summary>
    public static class UiRefinementAuthoring
    {
        public const string ArtFolder="Assets/Weedtopia/Production/Art/PlayerAvatars";
        public const string CatalogPath="Assets/Weedtopia/Production/PlayerAvatars.asset";
        public const string CloseIconPath="Assets/Weedtopia/Production/Prefabs/UiExperience/GoldenCloseIcon.prefab";
        private static PresentationArt art;
        private static Sprite surface,round;
        private static GameObject closeIconPrefab;
        [MenuItem("Weedtopia/Presentation/Install UI Refinements")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save manual scene changes first.");
            var setup=EditorSceneManager.GetSceneManagerSetup();AssetDatabase.Refresh();
            try
            {
                art=AssetDatabase.LoadAssetAtPath<PresentationArt>(ProductionArtImporter.ArtPath);
                surface=AssetDatabase.LoadAssetAtPath<Sprite>(UiPolishAuthoring.Skin+"/Button.png");round=AssetDatabase.LoadAssetAtPath<Sprite>(UiPolishAuthoring.Skin+"/Round.png");
                var catalog=Avatars();
                closeIconPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(CloseIconPath);
                if(closeIconPrefab==null)
                {
                    var symbol=Rect("Icon",null);var image=symbol.gameObject.AddComponent<Image>();image.sprite=art.Get("close");image.raycastTarget=false;image.preserveAspect=true;
                    symbol.anchorMin=Vector2.zero;symbol.anchorMax=Vector2.one;symbol.offsetMin=Vector2.one*2;symbol.offsetMax=Vector2.one*-2;
                    closeIconPrefab=PrefabUtility.SaveAsPrefabAsset(symbol.gameObject,CloseIconPath);Object.DestroyImmediate(symbol.gameObject);
                }
                var files=Directory.GetFiles("Assets/Weedtopia/Production/Prefabs","*.prefab",SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(RewardPresentationAuthoring.Folder,"*.prefab",SearchOption.AllDirectories))
                    .Concat(Directory.GetFiles(LuckyWheelAuthoring.Folder,"*.prefab",SearchOption.AllDirectories))
                    .Concat(Directory.GetFiles(VipStoriesAuthoring.Folder,"*.prefab",SearchOption.AllDirectories))
                    .Concat(Directory.GetFiles(BigRockCityAuthoring.Folder,"*.prefab",SearchOption.TopDirectoryOnly));
                foreach(string path in files.Distinct())
                {
                    var go=PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        bool changed=CloseButtons(go);
                        var tile=go.GetComponent<InventoryTileView>();
                        if(tile!=null)
                        foreach(var image in new[]{tile.quantityBadge,tile.selection})
                        {if(image.sprite==surface&&image.type==Image.Type.Sliced)continue;image.sprite=surface;image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=2;EditorUtility.SetDirty(image);changed=true;}
                        if(go.GetComponent<RewardCollectionUi>()!=null)changed|=Inventory(go);
                        if(go.GetComponent<LuckyWheelUi>()!=null)changed|=Wheel(go);
                        if(changed)PrefabUtility.SaveAsPrefabAsset(go,path);
                    }
                    finally{PrefabUtility.UnloadPrefabContents(go);}
                }
                foreach(string path in new[]{GameScaffold.GameScene,BigRockCityAuthoring.ScenePath})
                {
                    var scene=EditorSceneManager.OpenScene(path);var root=Object.FindFirstObjectByType<GameRoot>();var view=root.Ui.GetComponent<UiExperience>();
                    // Opening a scene unloads assets that have not yet been referenced by that scene.
                    catalog=AssetDatabase.LoadAssetAtPath<PlayerAvatarCatalog>(CatalogPath);
                    closeIconPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(CloseIconPath);
                    bool changed=CloseButtons(root.Ui.gameObject);
                    if(view.avatars!=catalog)
                    {
                        var serialized=new SerializedObject(view);serialized.FindProperty("avatars").objectReferenceValue=catalog;serialized.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(view);PrefabUtility.RecordPrefabInstancePropertyModifications(view);changed=true;
                    }
                    if(changed){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
                    if(view.avatars==null)throw new InvalidOperationException("Avatar catalog binding missing in "+path);
                    if(!catalog.entries.Any(e=>e.portrait==view.avatar.sprite))
                    {view.avatar.sprite=catalog.entries[0].portrait;EditorUtility.SetDirty(view.avatar);PrefabUtility.RecordPrefabInstancePropertyModifications(view.avatar);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
                }
                AssetDatabase.SaveAssets();Directory.CreateDirectory("Docs/UiRefinementRevision");
                File.WriteAllText("Docs/UiRefinementRevision/authoring_receipt.json","{\"utc\":\""+DateTime.UtcNow.ToString("O")+"\",\"status\":\"passed\",\"playerAvatars\":10,\"male\":5,\"female\":5,\"goldClose\":true,\"inventoryPagination\":false,\"visibleItems\":8,\"authoredLayouts\":true}");
            }
            finally{if(setup.Any(s=>s.isLoaded)&&setup.Any(s=>s.isActive))EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        private static PlayerAvatarCatalog Avatars()
        {
            var entries=PlayerProfile.Avatars.Select((id,index)=>
            {
                string name=(index<5?"male-":"female-")+(index%5+1).ToString("00")+"-v1.png",path=ArtFolder+"/"+name;
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                if(importer==null)throw new InvalidOperationException("Missing avatar: "+path);
                if(importer.textureType!=TextureImporterType.Sprite||importer.spriteImportMode!=SpriteImportMode.Single||!importer.alphaIsTransparency||importer.mipmapEnabled)
                {importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();}
                return new ProfileAvatar{id=id,gender=index<5?ProfileAvatarGender.Male:ProfileAvatarGender.Female,portrait=AssetDatabase.LoadAssetAtPath<Sprite>(path)};
            }).ToArray();
            var catalog=AssetDatabase.LoadAssetAtPath<PlayerAvatarCatalog>(CatalogPath);
            if(catalog==null){catalog=ScriptableObject.CreateInstance<PlayerAvatarCatalog>();catalog.entries=entries;AssetDatabase.CreateAsset(catalog,CatalogPath);}
            else if(catalog.entries==null||catalog.entries.Length!=10||catalog.entries.Where((v,i)=>v.id!=entries[i].id||v.portrait!=entries[i].portrait||v.gender!=entries[i].gender).Any())
            {catalog.entries=entries;EditorUtility.SetDirty(catalog);}
            return catalog;
        }
        private static bool CloseButtons(GameObject go)
        {
            bool changed=false;
            foreach(var button in go.GetComponentsInChildren<Button>(true).Where(b=>b.name=="ui.close"||b.name.IndexOf("Close",StringComparison.OrdinalIgnoreCase)>=0))
            {
                var visual=button.GetComponent<CloseButtonVisual>();
                if(visual!=null&&PrefabUtility.GetCorrespondingObjectFromSource(visual.icon.gameObject)==AssetDatabase.LoadAssetAtPath<GameObject>(CloseIconPath))continue;
                var bg=button.GetComponent<Image>();if(bg==null)continue;
                bg.sprite=round;bg.type=Image.Type.Simple;bg.preserveAspect=true;bg.color=Color.white;
                var previous=button.transform.Find("Icon");if(previous!=null){previous.name="Archived_CloseIcon";previous.gameObject.SetActive(false);}
                var symbol=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CloseIconPath),button.transform);symbol.name="Icon";
                var icon=symbol.GetComponent<Image>();
                icon.sprite=art.Get("close");icon.color=Color.white;icon.raycastTarget=false;
                if(visual==null)visual=button.gameObject.AddComponent<CloseButtonVisual>();visual.icon=icon;visual.caption=button.GetComponentInChildren<Text>(true);visual.Fit();
                foreach(var value in new Object[]{bg,icon,visual,visual.caption})if(value!=null){EditorUtility.SetDirty(value);PrefabUtility.RecordPrefabInstancePropertyModifications(value);}
                changed=true;
            }
            return changed;
        }
        private static bool Inventory(GameObject go)
        {
            var installed=go.GetComponent<InventoryPanelLayout>();
            if(installed!=null)
            {
                bool changed=false;
                foreach(var scroll in new[]{installed.grid,installed.archive,installed.detail})
                {
                    if(scroll.verticalScrollbar==null){Scrollbar(scroll);changed=true;}
                    var handle=scroll.verticalScrollbar.handleRect;
                    if(handle.offsetMin!=Vector2.zero||handle.offsetMax!=Vector2.zero){handle.offsetMin=handle.offsetMax=Vector2.zero;EditorUtility.SetDirty(handle);changed=true;}
                }
                var catalog=AssetDatabase.LoadAssetAtPath<Weedtopia.Content.LanguageCatalog>(LocalizationAuthoring.CatalogPath);
                if(catalog!=null)
                {
                    var lookup=catalog.Create();
                    foreach(var pair in new[]{(installed.view.categoryButtons[1],"inventory.filter.team"),(installed.view.categoryButtons[2],"inventory.filter.build_time")})
                        changed|=LocalizationAuthoring.BindCaption(pair.Item1.GetComponentInChildren<Text>(true),pair.Item2,lookup)>0;
                }
                return changed;
            }
            var v=go.GetComponent<RewardCollectionUi>();var module=go.GetComponent<ModulePanelLayout>();var frame=module.frame;
            var layout=go.AddComponent<InventoryPanelLayout>();layout.frame=frame;layout.view=v;
            v.tilePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(RewardPresentationAuthoring.Folder+"/InventoryTile.prefab").GetComponent<InventoryTileView>();
            v.cardPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(RewardPresentationAuthoring.CardPath).GetComponent<RewardCardView>();
            layout.grid=Scroll("InventoryList",v.inventoryArea.transform);layout.archive=Scroll("ArchiveList",v.archiveArea.transform);
            foreach(var tile in v.tiles)tile.transform.SetParent(layout.grid.content,false);
            foreach(var card in v.cards)card.transform.SetParent(layout.archive.content,false);
            layout.detail=Scroll("ItemDetails",frame.body);Object.DestroyImmediate(layout.detail.content.gameObject);layout.detail.content=(RectTransform)v.details.transform;v.details.transform.SetParent(layout.detail.viewport,false);
            layout.back=Action("BackToInventory",frame.body,"Zur Übersicht");
            v.previousButton.gameObject.SetActive(false);v.nextButton.gameObject.SetActive(false);v.pageLabel.gameObject.SetActive(false);
            module.scroll.enabled=false;module.scroll.velocity=Vector2.zero;
            v.layoutVersion=2;EditorUtility.SetDirty(v);EditorUtility.SetDirty(module.scroll);return true;
        }
        private static bool Wheel(GameObject go)
        {
            if(go.GetComponent<WheelPanelLayout>()!=null)return false;
            var module=go.GetComponent<ModulePanelLayout>();var layout=go.AddComponent<WheelPanelLayout>();layout.view=go.GetComponent<LuckyWheelUi>();layout.frame=module.frame;layout.stage=module.wheelStage;
            module.scroll.enabled=false;module.scroll.velocity=Vector2.zero;EditorUtility.SetDirty(module.scroll);return true;
        }
        private static ScrollRect Scroll(string name,Transform parent)
        {
            var viewport=Rect(name,parent);viewport.gameObject.AddComponent<RectMask2D>();var bg=viewport.gameObject.AddComponent<Image>();bg.sprite=surface;bg.type=Image.Type.Sliced;bg.color=new Color(.02f,.12f,.13f,.16f);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=Rect("Content",viewport);scroll.content.anchorMin=scroll.content.anchorMax=scroll.content.pivot=new Vector2(.5f,1);scroll.content.anchoredPosition=Vector2.zero;
            scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            Scrollbar(scroll);
            return scroll;
        }
        private static void Scrollbar(ScrollRect scroll)
        {
            var r=Rect("Scrollbar",scroll.transform);r.anchorMin=new Vector2(1,0);r.anchorMax=Vector2.one;r.offsetMin=new Vector2(-10,0);r.offsetMax=Vector2.zero;
            var bar=r.gameObject.AddComponent<Scrollbar>();bar.direction=UnityEngine.UI.Scrollbar.Direction.BottomToTop;
            var handle=Rect("Handle",r).gameObject.AddComponent<Image>();handle.sprite=surface;handle.type=Image.Type.Sliced;handle.color=new Color(.65f,.82f,.70f,.8f);
            handle.rectTransform.anchorMin=Vector2.zero;handle.rectTransform.anchorMax=Vector2.one;handle.rectTransform.offsetMin=handle.rectTransform.offsetMax=Vector2.zero;
            bar.handleRect=handle.rectTransform;bar.targetGraphic=handle;scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
        }
        private static RectTransform Rect(string name,Transform parent)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);return r;}
        private static Button Action(string name,Transform parent,string text)
        {var r=Rect(name,parent);var image=r.gameObject.AddComponent<Image>();image.sprite=surface;image.type=Image.Type.Sliced;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;var t=Rect("Caption",r).gameObject.AddComponent<Text>();t.font=art.bodyFont;t.text=text;t.color=new Color(.97f,.96f,.85f);t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return b;}
    }
}
