using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Unity;
using Object=UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit, repeatable visual revision. World objects and gameplay content remain untouched.</summary>
    public static class UiPolishAuthoring
    {
        public const string Skin="Assets/Weedtopia/Production/Art/UiPolish";
        public const string Title="Assets/Weedtopia/Production/Art/Title";
        public const string Shortcut="Assets/Weedtopia/Production/Prefabs/UiExperience/QuestShortcut.prefab";
        public const string TitlePrefab="Assets/Weedtopia/Production/Prefabs/UiExperience/TitleScreen.prefab";
        public const string PreviewPrefab="Assets/Weedtopia/Production/Prefabs/UiExperience/QuestPreview.prefab";
        private static PresentationArt art;
        private static readonly Color Paper=new Color(.97f,.96f,.85f),Accent=new Color(.78f,.88f,.59f);
        private static Sprite panel,button,primary,field,round;
        [MenuItem("Weedtopia/Presentation/Install Polished UI")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save manual scene changes first.");
            var setup=EditorSceneManager.GetSceneManagerSetup();AssetDatabase.Refresh();
            try
            {
                panel=Import(Skin+"/Panel.png",24);button=Import(Skin+"/Button.png",36);primary=Import(Skin+"/Primary.png",36);field=Import(Skin+"/Field.png",24);round=Import(Skin+"/Round.png",0);
                var backdrop=Import(Title+"/WeedtopiaTown-v1.png",0);var wordmark=Import(Title+"/WeedtopiaLogo-v1.png",0);
                art=AssetDatabase.LoadAssetAtPath<PresentationArt>(ProductionArtImporter.ArtPath);
                EnsureShortcut();EnsureTitle(backdrop,wordmark);
                // Shared authored UI and row prefabs are reskinned at their source, never rebuilt at runtime.
                var prefabFiles=Directory.GetFiles("Assets/Weedtopia/Production/Prefabs","*.prefab",SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(RewardPresentationAuthoring.Folder,"*.prefab",SearchOption.AllDirectories))
                    .Concat(Directory.GetFiles(LuckyWheelAuthoring.Folder,"*.prefab",SearchOption.AllDirectories))
                    .Concat(Directory.GetFiles(VipStoriesAuthoring.Folder,"*.prefab",SearchOption.AllDirectories))
                    .Concat(Directory.GetFiles(BigRockCityAuthoring.Folder,"*.prefab",SearchOption.TopDirectoryOnly))
                    .Concat(new[]{GameScaffold.Prefabs+"/ListRow.prefab"}).Distinct();
                foreach(var path in prefabFiles)
                {
                    var contents=PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        bool changed=Reskin(contents);
                        var dialogue=contents.GetComponent<DialoguePanel>();if(dialogue!=null)changed|=BindDialogue(dialogue);
                        if((contents.GetComponent<RewardCollectionUi>()!=null||contents.GetComponent<LuckyWheelUi>()!=null||contents.GetComponent<VipStoriesUi>()!=null||contents.GetComponent<CityCampaignUi>()!=null)&&contents.GetComponent<ModalFrame>()==null){AddModalFrame(contents);changed=true;}
                        if(contents.GetComponent<ModalFrame>()!=null&&contents.GetComponent<ModulePanelLayout>()==null)changed|=InstallModuleLayout(contents);
                        if(changed)PrefabUtility.SaveAsPrefabAsset(contents,path);
                    }
                    finally{PrefabUtility.UnloadPrefabContents(contents);}
                }
                foreach(string scenePath in new[]{GameScaffold.GameScene,BigRockCityAuthoring.ScenePath})
                {
                    var scene=EditorSceneManager.OpenScene(scenePath);var root=Object.FindFirstObjectByType<GameRoot>();var view=root.Ui.GetComponent<UiExperience>();
                    var serialized=new SerializedObject(root.Ui);var bindings=serialized.FindProperty("bindings");var panels=serialized.FindProperty("panels");
                    var map=new Dictionary<string,GameObject>();for(int i=0;i<bindings.arraySize;i++){var e=bindings.GetArrayElementAtIndex(i);map[e.FindPropertyRelative("id").stringValue]=(GameObject)e.FindPropertyRelative("target").objectReferenceValue;}
                    void Bind(string id,GameObject target)
                    {for(int i=0;i<bindings.arraySize;i++){var entry=bindings.GetArrayElementAtIndex(i);if(entry.FindPropertyRelative("id").stringValue==id){entry.FindPropertyRelative("target").objectReferenceValue=target;map[id]=target;return;}}}
                    void ReplacePanel(string id,GameObject target)
                    {var old=map[id];for(int i=0;i<panels.arraySize;i++)if(panels.GetArrayElementAtIndex(i).objectReferenceValue==old)panels.GetArrayElementAtIndex(i).objectReferenceValue=target;Bind(id,target);}
                    bool changed=Reskin(root.Ui.gameObject);
                    var start=(RectTransform)map["start"].transform;
                    if(view.titleBackground==null)
                    {
                        view.titleBackground=Picture("TitleBackdrop",start,backdrop);view.titleBackground.preserveAspect=false;view.titleBackground.transform.SetAsFirstSibling();
                        view.titleWordmark=Picture("WeedtopiaWordmark",start,wordmark);view.titleWordmark.preserveAspect=true;
                        start.GetComponent<Image>().color=Color.clear;
                        foreach(var text in new[]{view.logo,view.subtitle,view.playHint})text.gameObject.SetActive(false);view.emblem.gameObject.SetActive(false);
                        var play=map["ui.start"].GetComponent<Button>();play.GetComponentInChildren<Text>(true).color=Paper;changed=true;
                    }
                    if(PrefabUtility.GetCorrespondingObjectFromSource(map["start"])!=AssetDatabase.LoadAssetAtPath<GameObject>(TitlePrefab))
                    {
                        var old=map["start"];var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(TitlePrefab),old.transform.parent);instance.name="start";instance.SetActive(old.activeSelf);
                        ReplacePanel("start",instance);Bind("ui.start",instance.transform.Find("Play").gameObject);
                        view.titleBackground=instance.transform.Find("TitleBackdrop").GetComponent<Image>();view.titleWordmark=instance.transform.Find("WeedtopiaWordmark").GetComponent<Image>();
                        old.name="Archived_TitleMenu";old.transform.SetParent(root.Ui.transform.Find("ArchivedUiBeforeExperience"),false);old.SetActive(false);changed=true;
                    }
                    changed|=BindDialogue(view.intro);changed|=BindDialogue(view.conversation);
                    if(view.quests.preview==null)
                    {
                        var tracker=view.quests;tracker.shortcutPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Shortcut).GetComponent<Button>();
                        tracker.preview=Rect("questPreview",map["modalShade"].transform);var image=tracker.preview.gameObject.AddComponent<Image>();image.sprite=panel;image.type=Image.Type.Sliced;
                        tracker.previewHeading=Label("QuestPreviewHeading",tracker.preview,"Aufgabe",34);tracker.previewHeading.color=Accent;
                        tracker.previewBody=Label("QuestPreviewBody",tracker.preview,"",30);
                        tracker.previewAction=Action("QuestPreviewAction",tracker.preview,"Zum Ziel",true);
                        tracker.previewDetails=Action("QuestPreviewDetails",tracker.preview,"Auftrag öffnen");
                        int index=bindings.arraySize++;var entry=bindings.GetArrayElementAtIndex(index);entry.FindPropertyRelative("id").stringValue="questPreview";entry.FindPropertyRelative("target").objectReferenceValue=tracker.preview.gameObject;
                        panels.GetArrayElementAtIndex(panels.arraySize++).objectReferenceValue=tracker.preview.gameObject;tracker.preview.gameObject.SetActive(false);
                        tracker.dock.GetComponent<Image>().color=Color.clear;tracker.dockScroll.GetComponent<Image>().color=Color.clear;
                        var old=tracker.dockScroll.content.GetComponent<VerticalLayoutGroup>();if(old!=null)Object.DestroyImmediate(old);
                        var layout=tracker.dockScroll.content.gameObject.AddComponent<HorizontalLayoutGroup>();layout.spacing=12;layout.childControlWidth=layout.childControlHeight=true;layout.childForceExpandWidth=layout.childForceExpandHeight=false;
                        var fitter=tracker.dockScroll.content.GetComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.Unconstrained;fitter.horizontalFit=ContentSizeFitter.FitMode.PreferredSize;
                        tracker.dockScroll.content.anchorMin=tracker.dockScroll.content.anchorMax=new Vector2(0,1);tracker.dockScroll.content.pivot=new Vector2(0,1);
                        tracker.dockScroll.horizontal=true;tracker.dockScroll.vertical=false;changed=true;
                        EditorUtility.SetDirty(tracker);
                    }
                    if(PrefabUtility.GetCorrespondingObjectFromSource(view.quests.preview.gameObject)!=AssetDatabase.LoadAssetAtPath<GameObject>(PreviewPrefab)||!File.Exists(PreviewPrefab))
                    {
                        var tracker=view.quests;var old=tracker.preview.gameObject;
                        if(!File.Exists(PreviewPrefab))PrefabUtility.SaveAsPrefabAsset(old,PreviewPrefab);
                        var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PreviewPrefab),old.transform.parent);instance.name="questPreview";instance.SetActive(false);
                        ReplacePanel("questPreview",instance);tracker.preview=(RectTransform)instance.transform;tracker.previewHeading=instance.transform.Find("QuestPreviewHeading").GetComponent<Text>();tracker.previewBody=instance.transform.Find("QuestPreviewBody").GetComponent<Text>();
                        tracker.previewAction=instance.transform.Find("QuestPreviewAction").GetComponent<Button>();tracker.previewDetails=instance.transform.Find("QuestPreviewDetails").GetComponent<Button>();
                        old.name="Archived_QuestPreview";old.transform.SetParent(root.Ui.transform.Find("ArchivedUiBeforeExperience"),false);old.SetActive(false);EditorUtility.SetDirty(tracker);changed=true;
                    }
                    for(int i=0;i<panels.arraySize;i++)
                    {
                        var target=(GameObject)panels.GetArrayElementAtIndex(i).objectReferenceValue;
                        foreach(var added in target.GetComponents<ModalFrame>().Where(f=>PrefabUtility.IsAddedComponentOverride(f)).ToArray())
                            if(target.GetComponents<ModalFrame>().Length>1){PrefabUtility.RevertAddedComponent(added,InteractionMode.AutomatedAction);changed=true;}
                        foreach(Transform child in target.transform.Cast<Transform>().ToArray())
                            if((child.name=="FrameHeading"||child.name=="FrameViewport")&&PrefabUtility.IsAddedGameObjectOverride(child.gameObject)&&target.GetComponents<ModalFrame>().All(f=>f.heading?.transform!=child&&f.viewport!=child))
                            {
                                var content=child.Find("AuthoredContent");var frame=target.GetComponent<ModalFrame>();
                                if(content!=null)foreach(var surviving in content.Cast<Transform>().ToArray())surviving.SetParent(frame.body,false);
                                PrefabUtility.RevertAddedGameObject(child.gameObject,InteractionMode.AutomatedAction);changed=true;
                            }
                        if(view.OwnsLayout(target)||target.GetComponent<ModalFrame>()!=null)continue;
                        AddModalFrame(target);changed=true;
                    }
                    if(map["workers"].GetComponent<TeamPanelLayout>()==null){InstallTeam(map,root);changed=true;}
                    if(map.TryGetValue("wheelInventory",out var wheel)&&wheel!=null&&wheel.transform.parent==map["collection"].transform)
                    {wheel.transform.SetParent(map["collection"].GetComponent<ModalFrame>().body,false);changed=true;}
                    var collectionLayout=map["collection"].GetComponent<ModulePanelLayout>();
                    if(collectionLayout.inventoryWheel==null){collectionLayout.inventoryWheel=map["wheelInventory"].GetComponent<Button>();EditorUtility.SetDirty(collectionLayout);PrefabUtility.RecordPrefabInstancePropertyModifications(collectionLayout);changed=true;}
                    foreach(var entry in map)if(entry.Value==null)throw new InvalidOperationException("Lost UI binding: "+entry.Key);
                    if(changed)
                    {
                        serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(view);PrefabUtility.RecordPrefabInstancePropertyModifications(view.intro);PrefabUtility.RecordPrefabInstancePropertyModifications(view.conversation);
                        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                    }
                }
                AssetDatabase.SaveAssets();Directory.CreateDirectory("Docs/UiPolishRevision");File.WriteAllText("Docs/UiPolishRevision/authoring_receipt.json","{\"utc\":\""+DateTime.UtcNow.ToString("O")+"\",\"scenes\":2,\"titleBackdrop\":true,\"roundQuestShortcuts\":true,\"authoredTapDialogue\":true}");
            }
            finally{if(!UnityEngine.Application.isBatchMode)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        private static Sprite Import(string path,int border)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            var edges=new Vector4(border,border,border,border);
            if(importer.textureType!=TextureImporterType.Sprite||importer.spriteImportMode!=SpriteImportMode.Single||importer.spriteBorder!=edges||importer.textureCompression!=TextureImporterCompression.Uncompressed||importer.mipmapEnabled||importer.npotScale!=TextureImporterNPOTScale.None)
            {
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spriteBorder=edges;importer.spritePixelsPerUnit=100;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;
                EditorUtility.SetDirty(importer);importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static bool Reskin(GameObject root)
        {
            bool changed=false;
            var tile=root.GetComponent<InventoryTileView>();
            if(tile!=null&&tile.button.GetComponent<Image>().sprite!=button){var image=tile.button.GetComponent<Image>();image.sprite=button;image.type=Image.Type.Sliced;image.color=Color.white;EditorUtility.SetDirty(image);changed=true;}
            var replacements=new Dictionary<Sprite,Sprite>{{art.Get("panel"),panel},{art.Get("paper"),field},{art.Get("button"),button},{art.Get("primary"),primary},{art.Get("frame"),panel},{art.Get("banner"),panel}};
            foreach(var image in root.GetComponentsInChildren<Image>(true))
            {
                if(image.sprite==null||!replacements.TryGetValue(image.sprite,out var replacement))continue;
                image.sprite=replacement;image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=replacement==panel||replacement==field?1:2;EditorUtility.SetDirty(image);PrefabUtility.RecordPrefabInstancePropertyModifications(image);changed=true;
            }
            foreach(var action in root.GetComponentsInChildren<Button>(true))
            {
                var image=action.GetComponent<Image>();if(image==null)continue;
                bool close=action.name=="ui.close"||action.name.Contains("Close")||action.name.Contains("close");
                if(close&&image.sprite!=round){image.sprite=round;image.type=Image.Type.Simple;image.preserveAspect=true;EditorUtility.SetDirty(image);changed=true;}
                var label=action.GetComponentInChildren<Text>(true);if(label==null||action.GetComponentInParent<Dropdown>()!=null)continue;
                if(label.color!=Paper&&image.sprite==primary){label.color=Paper;EditorUtility.SetDirty(label);changed=true;}
                if(image.sprite==button||image.sprite==primary)
                {
                    var rect=label.rectTransform;var min=new Vector2(24,8);var max=new Vector2(-24,-8);
                    if(rect.anchorMin!=Vector2.zero||rect.anchorMax!=Vector2.one||rect.offsetMin!=min||rect.offsetMax!=max){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=min;rect.offsetMax=max;EditorUtility.SetDirty(rect);changed=true;}
                    label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;
                }
            }
            return changed;
        }
        private static bool BindDialogue(DialoguePanel view)
        {
            bool changed=false;
            if(view.tapScreen==null)
            {
                var input=Rect("DialogueTap",view.transform);var image=input.gameObject.AddComponent<Image>();image.sprite=panel;image.color=Color.clear;image.raycastTarget=true;
                view.tapScreen=input.gameObject.AddComponent<Button>();view.tapScreen.targetGraphic=image;view.tapScreen.transition=Selectable.Transition.None;view.tapScreen.navigation=new Navigation{mode=Navigation.Mode.None};input.SetAsFirstSibling();
                view.box.GetComponent<Image>().raycastTarget=false;changed=true;
            }
            foreach(var portrait in view.portraits)if(!portrait.mirrorToFaceRight){portrait.mirrorToFaceRight=true;changed=true;}
            if(view.skip.gameObject.activeSelf){view.skip.gameObject.SetActive(false);changed=true;}
            if(changed)EditorUtility.SetDirty(view);return changed;
        }
        private static void EnsureShortcut()
        {
            if(AssetDatabase.LoadAssetAtPath<GameObject>(Shortcut)!=null)return;
            var rect=Rect("QuestShortcut",null);rect.sizeDelta=Vector2.one*104;var graphic=rect.gameObject.AddComponent<RoundNavigationGraphic>();var action=rect.gameObject.AddComponent<Button>();action.targetGraphic=graphic;
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight=104;
            var icon=Picture("QuestIcon",rect,art.Get("quest"));icon.preserveAspect=true;icon.rectTransform.anchorMin=icon.rectTransform.anchorMax=new Vector2(.5f,.5f);icon.rectTransform.anchoredPosition=new Vector2(0,8);icon.rectTransform.sizeDelta=Vector2.one*54;
            var number=Label("QuestNumber",rect,"1",22);number.alignment=TextAnchor.MiddleCenter;number.rectTransform.anchorMin=number.rectTransform.anchorMax=new Vector2(.5f,.5f);number.rectTransform.anchoredPosition=new Vector2(0,-30);number.rectTransform.sizeDelta=new Vector2(50,28);
            PrefabUtility.SaveAsPrefabAsset(rect.gameObject,Shortcut);Object.DestroyImmediate(rect.gameObject);
        }
        private static void EnsureTitle(Sprite backdrop,Sprite wordmark)
        {
            if(AssetDatabase.LoadAssetAtPath<GameObject>(TitlePrefab)!=null)return;
            var rect=Rect("TitleScreen",null);rect.sizeDelta=new Vector2(1920,1080);var background=rect.gameObject.AddComponent<Image>();background.sprite=panel;background.color=Color.clear;
            Picture("TitleBackdrop",rect,backdrop);var logo=Picture("WeedtopiaWordmark",rect,wordmark);logo.preserveAspect=true;
            var play=Action("Play",rect,"Play",true);play.GetComponent<Image>().color=Color.clear;
            var face=Rect("CapsuleFace",play.transform);face.anchorMin=Vector2.zero;face.anchorMax=Vector2.one;face.offsetMin=face.offsetMax=Vector2.zero;face.SetAsFirstSibling();
            var graphic=face.gameObject.AddComponent<CapsuleButtonGraphic>();graphic.raycastTarget=false;play.targetGraphic=graphic;
            PrefabUtility.SaveAsPrefabAsset(rect.gameObject,TitlePrefab);Object.DestroyImmediate(rect.gameObject);
        }
        private static void AddModalFrame(GameObject target)
        {
            var rect=(RectTransform)target.transform;var old=rect.Cast<Transform>().ToArray();
            var skin=target.GetComponent<Image>();if(skin!=null){skin.sprite=panel;skin.type=Image.Type.Sliced;skin.pixelsPerUnitMultiplier=1;skin.color=Color.white;EditorUtility.SetDirty(skin);}
            var frame=target.AddComponent<ModalFrame>();frame.authoredSize=rect.sizeDelta;frame.authoredPivot=rect.pivot;
            frame.headingSource=old.SelectMany(t=>t.GetComponentsInChildren<Text>(true)).FirstOrDefault();
            frame.heading=Label("FrameHeading",rect,frame.headingSource?.text??target.name,34);frame.heading.color=Accent;frame.heading.alignment=TextAnchor.MiddleLeft;
            frame.viewport=Rect("FrameViewport",rect);frame.viewport.gameObject.AddComponent<RectMask2D>();
            var backdrop=frame.viewport.gameObject.AddComponent<Image>();backdrop.sprite=panel;backdrop.type=Image.Type.Sliced;backdrop.color=Color.clear;backdrop.raycastTarget=false;
            frame.body=Rect("AuthoredContent",frame.viewport);frame.body.sizeDelta=frame.authoredSize;frame.body.pivot=frame.authoredPivot;
            foreach(var child in old)child.SetParent(frame.body,false);
            if(frame.headingSource!=null)frame.headingSource.gameObject.SetActive(false);
            EditorUtility.SetDirty(frame);
        }
        private static void InstallTeam(Dictionary<string,GameObject> map,GameRoot root)
        {
            var target=map["workers"];var view=target.AddComponent<TeamPanelLayout>();view.frame=target.GetComponent<ModalFrame>();view.frame.fillAvailable=true;
            view.roster=(RectTransform)map["workerList"].transform.parent;view.filter=map["filter"].GetComponent<Dropdown>();view.inventory=map["collectionTeam"].GetComponent<Button>();
            var viewport=Rect("TeamDetailsViewport",view.frame.body);viewport.gameObject.AddComponent<RectMask2D>();var background=viewport.gameObject.AddComponent<Image>();background.sprite=panel;background.type=Image.Type.Sliced;background.color=new Color(.04f,.15f,.14f,.2f);
            view.detailsScroll=viewport.gameObject.AddComponent<ScrollRect>();view.detailsContent=Rect("TeamDetailsContent",viewport);view.detailsContent.anchorMin=new Vector2(0,1);view.detailsContent.anchorMax=Vector2.one;view.detailsContent.pivot=new Vector2(.5f,1);view.detailsContent.sizeDelta=Vector2.zero;
            view.detailsScroll.viewport=viewport;view.detailsScroll.content=view.detailsContent;view.detailsScroll.horizontal=false;view.detailsScroll.movementType=ScrollRect.MovementType.Clamped;
            view.reveal=root.Presentation.workerReveal;view.cardsCounter=(RectTransform)root.Presentation.currencyValues[2].transform.parent;
            foreach(string id in new[]{"ui.spin","teamSpinHint","workerText","ui.remove","ui.worker_level","ui.worker_rank"})map[id].transform.SetParent(view.detailsContent,false);
            view.reveal.SetParent(view.detailsContent,false);view.cardsCounter.SetParent(view.detailsContent,false);
            view.spin=map["ui.spin"].GetComponent<Button>();view.hint=map["teamSpinHint"].GetComponent<Text>();view.detail=map["workerText"].GetComponent<Text>();view.remove=map["ui.remove"].GetComponent<Button>();view.improve=map["ui.worker_level"].GetComponent<Button>();view.rank=map["ui.worker_rank"].GetComponent<Button>();
            EditorUtility.SetDirty(view);EditorUtility.SetDirty(view.frame);
        }
        private static bool InstallModuleLayout(GameObject target)
        {
            // The shop owns its catalog and confirmation scrolls; an outer generic layout conflicts with both.
            if(target.GetComponent<ShopPanelLayout>()!=null)return false;
            var frame=target.GetComponent<ModalFrame>();frame.fillAvailable=true;var view=target.AddComponent<ModulePanelLayout>();view.frame=frame;
            view.scroll=frame.viewport.gameObject.AddComponent<ScrollRect>();view.scroll.viewport=frame.viewport;view.scroll.content=frame.body;view.scroll.horizontal=false;view.scroll.movementType=ScrollRect.MovementType.Clamped;
            frame.viewport.GetComponent<Image>().raycastTarget=true;
            if(target.GetComponent<LuckyWheelUi>()!=null)
            {
                view.wheelStage=Rect("WheelStage",frame.body);view.wheelStage.sizeDelta=new Vector2(730,730);
                foreach(string name in new[]{"GlowRays","MarqueeFrame","RotatingWheel","HubLaurel","PointerShadow","Pointer"})
                {var child=frame.body.Find(name);var rect=(RectTransform)child;var position=rect.anchoredPosition;child.SetParent(view.wheelStage,false);rect.anchoredPosition=position-new Vector2(-290,-15);}
            }
            EditorUtility.SetDirty(frame);EditorUtility.SetDirty(view);return true;
        }
        private static RectTransform Rect(string name,Transform parent)
        {var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();if(parent!=null)rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);return rect;}
        private static Image Picture(string name,Transform parent,Sprite sprite)
        {var image=Rect(name,parent).gameObject.AddComponent<Image>();image.sprite=sprite;image.raycastTarget=false;return image;}
        private static Text Label(string name,Transform parent,string value,int size)
        {var label=Rect(name,parent).gameObject.AddComponent<Text>();label.font=art.bodyFont;label.text=value;label.fontSize=size;label.color=Paper;label.raycastTarget=false;label.supportRichText=false;return label;}
        private static Button Action(string name,Transform parent,string value,bool isPrimary=false)
        {
            var rect=Rect(name,parent);var image=rect.gameObject.AddComponent<Image>();image.sprite=isPrimary?primary:button;image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=2;
            var action=rect.gameObject.AddComponent<Button>();action.targetGraphic=image;var label=Label("Caption",rect,value,30);label.alignment=TextAnchor.MiddleCenter;return action;
        }
    }
}
