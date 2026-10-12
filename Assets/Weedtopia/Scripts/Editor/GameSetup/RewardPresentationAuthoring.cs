using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit authoring only. Existing installed prefabs and manual adjustments are retained on rerun.</summary>
    public static class RewardPresentationAuthoring
    {
        public const string Folder = "Assets/Weedtopia/Production/Rewards";
        public const string CardPath = Folder + "/RewardCard.prefab";
        public const string ButtonPath = Folder + "/RewardButton.prefab";
        public const string CelebrationPath = Folder + "/RewardCelebration.prefab";
        public const string CollectionPath = Folder + "/RewardCollection.prefab";
        public const string PreviewPath = Folder + "/RewardPreview.prefab";
        private static readonly Color Gold = new Color(1,.79f,.37f), Paper = new Color(.97f,.94f,.84f), Muted = new Color(.67f,.79f,.76f);
        private static PresentationArt art;
        private static Weedtopia.Domain.GameDefinitions defs;

        [MenuItem("Weedtopia/Presentation/Install Quest Rewards and Collection")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before authoring.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save manual scene changes first.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var root=Object.FindFirstObjectByType<GameRoot>();var ui=root.Ui;
                art=root.Presentation.art;
                defs=JsonUtility.FromJson<Weedtopia.Domain.GameDefinitions>(File.ReadAllText("Assets/Weedtopia/Game/Content/BurningHills.json"));
                defs.Validate();Directory.CreateDirectory(Folder);
                ImportChest();EnsureButton();EnsureCard();EnsureCelebration();EnsureCollection();EnsurePreview();
                var serialized=new SerializedObject(ui);
                var bindings=serialized.FindProperty("bindings");var map=new Dictionary<string,GameObject>();
                for(int i=0;i<bindings.arraySize;i++)
                {var b=bindings.GetArrayElementAtIndex(i);map.Add(b.FindPropertyRelative("id").stringValue,(GameObject)b.FindPropertyRelative("target").objectReferenceValue);}
                if(serialized.FindProperty("rewards").objectReferenceValue!=null){Validate(root);return;}
                void Bind(string id,GameObject go)
                {int i=bindings.arraySize++;var b=bindings.GetArrayElementAtIndex(i);b.FindPropertyRelative("id").stringValue=id;b.FindPropertyRelative("target").objectReferenceValue=go;}
                var safe=(RectTransform)serialized.FindProperty("safeArea").objectReferenceValue;
                var celebration=Instance(CelebrationPath,safe);celebration.SetActive(false);
                var collection=Instance(CollectionPath,map["journal"].transform.parent);collection.SetActive(false);
                var collectionRect=collection.GetComponent<RectTransform>();collectionRect.anchorMin=collectionRect.anchorMax=new Vector2(.5f,.5f);
                Bind("collection",collection);
                var panels=serialized.FindProperty("panels");panels.GetArrayElementAtIndex(panels.arraySize++).objectReferenceValue=collection;
                serialized.FindProperty("rewards").objectReferenceValue=celebration.GetComponent<RewardPresentation>();
                serialized.FindProperty("collection").objectReferenceValue=collection.GetComponent<RewardCollectionUi>();
                var journal=map["journal"].GetComponent<RectTransform>();journal.sizeDelta=new Vector2(1180,920);
                var preview=Instance(PreviewPath,journal).GetComponent<RectTransform>();Top(preview,new Vector2(-375,-645));
                serialized.FindProperty("rewardPreview").objectReferenceValue=preview.GetComponent<RewardPreviewView>();
                Top(map["ui.daily"].GetComponent<RectTransform>(),new Vector2(-375,-792));
                Top(map["recovery"].GetComponent<RectTransform>(),new Vector2(-375,-860));
                var scroll=map["journalList"].transform.parent.GetComponent<RectTransform>();Top(scroll,new Vector2(175,-510));scroll.sizeDelta=new Vector2(680,730);
                var journalButton=Button("Collection",journal,"reward.collection",new Vector2(315,-57),new Vector2(210,56));Top((RectTransform)journalButton.transform,new Vector2(315,-57));
                var teamTitle=map["workersTitle"].GetComponent<RectTransform>();Top(teamTitle,new Vector2(-375,-48));teamTitle.sizeDelta=new Vector2(260,60);
                var teamButton=Button("Collection",map["workers"].transform,"reward.collection",new Vector2(-95,-50),new Vector2(210,56));Top((RectTransform)teamButton.transform,new Vector2(-95,-50));
                Bind("collectionJournal",journalButton.gameObject);Bind("collectionTeam",teamButton.gameObject);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                foreach(var t in new[]{journal,preview,scroll,map["ui.daily"].GetComponent<RectTransform>(),map["recovery"].GetComponent<RectTransform>()})PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                EditorUtility.SetDirty(ui);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Validate(root);
            }
            finally {if(setup.Any(s=>s.isLoaded)&&setup.Any(s=>s.isActive))EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        [MenuItem("Weedtopia/Presentation/Refine Authored Quest Rewards")]
        public static void Refine()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save manual scene changes first.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(GameScaffold.GameScene);var root=Object.FindFirstObjectByType<GameRoot>();art=root.Presentation.art;
                var prefab=PrefabUtility.LoadPrefabContents(CelebrationPath);
                try
                {
                    var view=prefab.GetComponent<RewardPresentation>();
                    if(view.stage.Find("Atmosphere")==null)
                    {
                        var atmosphere=Image("Atmosphere",view.stage,null,Vector2.zero,new Vector2(1200,990),new Color(.012f,.04f,.045f,.985f));
                        atmosphere.transform.SetAsFirstSibling();
                    }
                    view.counter.rectTransform.anchoredPosition=new Vector2(0,325);
                    view.chest.anchoredPosition=new Vector2(0,128);view.chest.sizeDelta=new Vector2(410,380);
                    if(view.rays.GetComponent<CanvasRenderer>()==null)view.rays.gameObject.AddComponent<CanvasRenderer>();
                    view.rays.sizeDelta=new Vector2(760,680);view.rays.GetComponent<RewardBurstGraphic>().color=new Color(1,.72f,.25f,.22f);
                    view.rays.SetSiblingIndex(1);
                    PrefabUtility.SaveAsPrefabAsset(prefab,CelebrationPath);
                }
                finally{PrefabUtility.UnloadPrefabContents(prefab);}
                var serialized=new SerializedObject(root.Ui);var bindings=serialized.FindProperty("bindings");Transform modal=null;
                for(int i=0;i<bindings.arraySize;i++)
                {var b=bindings.GetArrayElementAtIndex(i);if(b.FindPropertyRelative("id").stringValue=="journal")modal=((GameObject)b.FindPropertyRelative("target").objectReferenceValue).transform.parent;}
                var collection=(RewardCollectionUi)serialized.FindProperty("collection").objectReferenceValue;
                collection.transform.SetParent(modal,false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(collection.transform);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Validate(root);
            }
            finally{if(setup.Any(s=>s.isLoaded)&&setup.Any(s=>s.isActive))EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        private static void ImportChest()
        {
            string path=Folder+"/Art/RewardChest.png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=2048;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.spritePixelsPerUnit=100;
            importer.SaveAndReimport();
        }
        private static void EnsureButton()
        {
            if(File.Exists(ButtonPath))return;
            var rect=Rect("RewardButton",null,new Vector2(270,60));
            var image=rect.gameObject.AddComponent<Image>();image.sprite=art.Get("button");image.type=UnityEngine.UI.Image.Type.Sliced;image.pixelsPerUnitMultiplier=2;
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.highlightedColor=new Color(1,.9f,.64f);colors.pressedColor=new Color(.7f,.88f,.8f);button.colors=colors;
            Label("Label",rect,"",Vector2.zero,new Vector2(250,52),23,Paper);
            Save(rect,ButtonPath);
        }
        private static void EnsureCard()
        {
            if(File.Exists(CardPath))return;
            var rect=Rect("RewardCard",null,new Vector2(332,150));var card=rect.gameObject.AddComponent<RewardCardView>();
            var bg=rect.gameObject.AddComponent<Image>();bg.color=new Color(.055f,.18f,.18f,.98f);
            card.action=rect.gameObject.AddComponent<Button>();card.action.targetGraphic=bg;
            var colors=card.action.colors;colors.disabledColor=Color.white;colors.highlightedColor=new Color(1,1,.85f);card.action.colors=colors;
            card.reveal=rect.gameObject.AddComponent<CanvasGroup>();
            card.accent=Image("Accent",rect,null,new Vector2(-162,0),new Vector2(5,150),Gold);
            card.icon=Image("Icon",rect,art.Get("gift"),new Vector2(-112,15),new Vector2(88,98));
            card.quantity=Label("Quantity",rect,"",new Vector2(44,41),new Vector2(214,42),33,Gold);
            card.title=Label("Name",rect,"",new Vector2(44,-5),new Vector2(216,56),23,Paper);card.title.alignment=TextAnchor.MiddleLeft;
            card.detail=Label("Detail",rect,"",new Vector2(0,-55),new Vector2(310,30),15,Muted);
            Save(rect,CardPath);
        }
        private static void EnsureCelebration()
        {
            if(File.Exists(CelebrationPath))return;
            var overlay=Rect("RewardCelebration",null,Vector2.zero);Stretch(overlay);
            var dim=overlay.gameObject.AddComponent<Image>();dim.color=new Color(.012f,.043f,.055f,.94f);dim.raycastTarget=true;
            var view=overlay.gameObject.AddComponent<RewardPresentation>();
            view.stage=Rect("Stage",overlay,new Vector2(1200,990));
            Image("Atmosphere",view.stage,null,Vector2.zero,new Vector2(1200,990),new Color(.012f,.04f,.045f,.985f));
            var border=Image("TopRule",view.stage,null,new Vector2(0,476),new Vector2(700,2),Gold);
            view.headline=Label("Headline",view.stage,"",new Vector2(0,427),new Vector2(1140,62),42,Gold,true);
            view.source=Label("QuestName",view.stage,"",new Vector2(0,375),new Vector2(1070,52),27,Paper);
            var rays=Rect("GoldenRays",view.stage,new Vector2(760,680));rays.anchoredPosition=new Vector2(0,136);rays.SetSiblingIndex(1);
            var burst=rays.gameObject.AddComponent<RewardBurstGraphic>();burst.color=new Color(1,.72f,.25f,.22f);burst.raycastTarget=false;view.rays=rays;
            view.sparkles=new RectTransform[24];
            for(int i=0;i<view.sparkles.Length;i++)view.sparkles[i]=Image("Sparkle_"+i,view.stage,art.Get("star"),Vector2.zero,new Vector2(14+i%3*6,14+i%3*6),new Color(1,.9f,.55f,.8f)).rectTransform;
            view.chest=Image("Chest",view.stage,AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/Art/RewardChest.png"),new Vector2(0,128),new Vector2(410,380)).rectTransform;
            view.counter=Label("Source",view.stage,"",new Vector2(0,325),new Vector2(1000,28),16,Gold);
            view.cards=Cards(view.stage,6,0);
            view.continueButton=Button("Continue",view.stage,"reward.continue",new Vector2(170,-418),new Vector2(290,64));
            view.collectionButton=Button("Collection",view.stage,"reward.open_collection",new Vector2(-170,-418),new Vector2(320,64));
            view.continueText=view.continueButton.GetComponentInChildren<Text>();view.collectionText=view.collectionButton.GetComponentInChildren<Text>();
            view.caption=Label("Credited",view.stage,"",new Vector2(0,-471),new Vector2(1130,32),18,Muted);
            view.audioSource=overlay.gameObject.AddComponent<AudioSource>();view.audioSource.playOnAwake=false;view.audioSource.spatialBlend=0;
            Save(overlay,CelebrationPath);
        }
        private static void EnsureCollection()
        {
            if(File.Exists(CollectionPath))return;
            var rect=Rect("RewardCollection",null,new Vector2(1200,900));
            var bg=rect.gameObject.AddComponent<Image>();bg.color=new Color(.025f,.09f,.105f,.99f);bg.raycastTarget=true;
            Image("TopRule",rect,null,new Vector2(0,448),new Vector2(1200,3),Gold);
            var view=rect.gameObject.AddComponent<RewardCollectionUi>();
            view.heading=Label("Heading",rect,"",new Vector2(0,392),new Vector2(990,58),39,Gold,true);
            view.inventoryTab=Button("Inventory",rect,"reward.tab.inventory",new Vector2(-300,311),new Vector2(280,58));
            view.achievementsTab=Button("Achievements",rect,"reward.tab.achievements",new Vector2(0,311),new Vector2(280,58));
            view.historyTab=Button("History",rect,"reward.tab.history",new Vector2(300,311),new Vector2(280,58));
            view.description=Label("Description",rect,"",new Vector2(0,240),new Vector2(1070,66),19,Muted);
            view.cards=Cards(rect,9,116);
            view.emptyLabel=Label("Empty",rect,"",Vector2.zero,new Vector2(1000,150),26,Paper);
            view.previousButton=Button("Previous",rect,null,new Vector2(-170,-394),new Vector2(140,54));view.previousButton.GetComponentInChildren<Text>().text="‹";
            view.nextButton=Button("Next",rect,null,new Vector2(170,-394),new Vector2(140,54));view.nextButton.GetComponentInChildren<Text>().text="›";
            view.pageLabel=Label("Page",rect,"",new Vector2(0,-394),new Vector2(160,40),23,Paper);
            Save(rect,CollectionPath);
        }
        private static void EnsurePreview()
        {
            if(File.Exists(PreviewPath))return;
            var rect=Rect("RewardPreview",null,new Vector2(350,240));
            var view=rect.gameObject.AddComponent<RewardPreviewView>();
            view.heading=Label("Heading",rect,defs.Text("reward.preview"),new Vector2(0,107),new Vector2(350,28),19,Gold,true);
            view.empty=Label("Empty",rect,"",Vector2.zero,new Vector2(320,130),19,Paper);
            view.icons=new Image[6];view.labels=new Text[6];
            for(int i=0;i<6;i++)
            {
                var slot=Rect("Reward_"+i,rect,new Vector2(168,65));slot.anchoredPosition=new Vector2(i%2==0?-89:89,62-i/2*70);
                view.icons[i]=Image("Icon",slot,art.Get("gift"),new Vector2(-61,0),new Vector2(42,48));
                view.labels[i]=Label("Label",slot,"",new Vector2(20,0),new Vector2(117,62),17,Paper);view.labels[i].alignment=TextAnchor.MiddleLeft;
            }
            Save(rect,PreviewPath);
        }
        private static RewardCardView[] Cards(Transform parent,int count,float firstY)
        {
            var result=new RewardCardView[count];
            for(int i=0;i<count;i++)
            {
                var instance=Instance(CardPath,parent);instance.name="Reward_"+(i+1);
                var rect=instance.GetComponent<RectTransform>();rect.anchoredPosition=new Vector2((i%3-1)*350,firstY-i/3*168);
                result[i]=instance.GetComponent<RewardCardView>();
            }
            return result;
        }
        private static Button Button(string name,Transform parent,string key,Vector2 position,Vector2 size)
        {
            var go=Instance(ButtonPath,parent);go.name=name;var rect=go.GetComponent<RectTransform>();rect.anchoredPosition=position;rect.sizeDelta=size;
            var text=go.GetComponentInChildren<Text>();text.rectTransform.sizeDelta=new Vector2(size.x-20,size.y-4);text.text=key==null?"":defs.Text(key);
            return go.GetComponent<Button>();
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 size)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=size;return rect;
        }
        private static Text Label(string name,Transform parent,string value,Vector2 position,Vector2 size,int fontSize,Color color,bool heading=false)
        {
            var rect=Rect(name,parent,size);rect.anchoredPosition=position;var text=rect.gameObject.AddComponent<Text>();
            text.font=heading?art.headingFont:art.bodyFont;text.fontSize=fontSize;text.text=value;text.color=color;
            text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.supportRichText=false;return text;
        }
        private static Image Image(string name,Transform parent,Sprite sprite,Vector2 position,Vector2 size,Color? color=null)
        {
            var rect=Rect(name,parent,size);rect.anchoredPosition=position;var image=rect.gameObject.AddComponent<Image>();image.sprite=sprite;
            image.color=color??Color.white;image.preserveAspect=sprite!=null;image.raycastTarget=false;return image;
        }
        private static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        private static void Top(RectTransform rect,Vector2 position){rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.anchoredPosition=position;}
        private static GameObject Instance(string path,Transform parent)=>(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),parent);
        private static void Save(RectTransform rect,string path){PrefabUtility.SaveAsPrefabAsset(rect.gameObject,path);Object.DestroyImmediate(rect.gameObject);}
        private static void Validate(GameRoot root)
        {
            var ui=new SerializedObject(root.Ui);
            var reward=(RewardPresentation)ui.FindProperty("rewards").objectReferenceValue;
            var collection=(RewardCollectionUi)ui.FindProperty("collection").objectReferenceValue;
            if(reward==null||collection==null||reward.cards.Length!=6||collection.cards.Length!=9||reward.chest.GetComponent<Image>().sprite==null||reward.rays.GetComponent<CanvasRenderer>()==null)
                throw new InvalidOperationException("Reward presentation is missing authored references");
            Directory.CreateDirectory("Docs/RewardPresentation");
            File.WriteAllText("Docs/RewardPresentation/authoring_receipt.json","{\"utc\":\""+DateTime.UtcNow.ToString("O")+"\",\"authoredPrefabs\":5,\"celebrationCards\":6,\"collectionCards\":9,\"installed\":true}");
        }
    }
}
