using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Domain;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit, repeatable authoring. All static controls and ten reusable prize instances live in assets.</summary>
    public static class LuckyWheelAuthoring
    {
        public const string Folder="Assets/Weedtopia/Production/LuckyWheel";
        public const string PanelPath=Folder+"/LuckyWheel.prefab",PrizePath=Folder+"/WheelPrize.prefab";
        private static PresentationArt art;
        private static GameDefinitions defs;
        private static Sprite surface;
        private static readonly Color Gold=new Color(1,.79f,.37f),Paper=new Color(.97f,.94f,.84f),Muted=new Color(.64f,.78f,.75f);
        [MenuItem("Weedtopia/Presentation/Install Daily Lucky Wheel")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode before authoring.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save manual scene changes first.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(GameScaffold.GameScene);var root=Object.FindFirstObjectByType<GameRoot>();art=root.Presentation.art;
                defs=root.Definitions??JsonUtility.FromJson<GameDefinitions>(File.ReadAllText("Assets/Weedtopia/Game/Content/BurningHills.json"));defs.Validate();
                surface=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Weedtopia/Production/Rewards/Art/UiSurface.png");
                Directory.CreateDirectory(Folder);AssetDatabase.Refresh();CreatePrize();CreatePanel();RepairCopy();RepairRewardIcon();
                var so=new SerializedObject(root.Ui);var bindings=so.FindProperty("bindings");var map=new Dictionary<string,GameObject>();
                for(int i=0;i<bindings.arraySize;i++)
                {var b=bindings.GetArrayElementAtIndex(i);map.Add(b.FindPropertyRelative("id").stringValue,(GameObject)b.FindPropertyRelative("target").objectReferenceValue);}
                void Bind(string id,GameObject go)
                {var b=bindings.GetArrayElementAtIndex(bindings.arraySize++);b.FindPropertyRelative("id").stringValue=id;b.FindPropertyRelative("target").objectReferenceValue=go;map[id]=go;}
                bool changed=false;
                if(so.FindProperty("luckyWheel").objectReferenceValue==null)
                {
                    var panel=Instance(PanelPath,map["journal"].transform.parent);panel.SetActive(false);Bind("luckyWheel",panel);
                    so.FindProperty("luckyWheel").objectReferenceValue=panel.GetComponent<LuckyWheelUi>();
                    var panels=so.FindProperty("panels");panels.GetArrayElementAtIndex(panels.arraySize++).objectReferenceValue=panel;
                    var journal=(RectTransform)map["journal"].transform;
                    var entry=Button("LuckyWheelEntry",journal,defs.Text("wheel.entry_free"),new Vector2(66,-57),new Vector2(230,56));
                    var rect=(RectTransform)entry.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(66,-57);
                    Bind("wheelJournal",entry.gameObject);
                    var inv=Button("LuckyWheelEntry",map["collection"].transform,defs.Text("wheel.entry_free"),new Vector2(425,351),new Vector2(250,42));
                    inv.GetComponentInChildren<Text>().fontSize=17;Bind("wheelInventory",inv.gameObject);
                    changed=true;
                }
                foreach(string entryId in new[]{"wheelJournal","wheelInventory"})
                {
                    if(!map.TryGetValue(entryId,out var entry))continue;
                    var label=entry.GetComponentInChildren<Text>(true);
                    if(label.text==defs.Text("wheel.entry_free"))continue;
                    label.text=defs.Text("wheel.entry_free");PrefabUtility.RecordPrefabInstancePropertyModifications(label);changed=true;
                }
                if(changed)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(root.Ui);
                    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                }
                var view=root.Ui.LuckyWheel;
                if(view==null||view.slots.Length!=10||view.graphic==null||view.disc==null)throw new InvalidOperationException("Wheel binding failure");
                if(defs.wheelRewards.Length<10)throw new InvalidOperationException("Wheel reward pool missing");
                Directory.CreateDirectory("Docs/LuckyWheel");File.WriteAllText("Docs/LuckyWheel/authoring_receipt.json",
                    "{\"status\":\"passed\",\"authoredSlots\":10,\"prefabs\":2,\"entries\":2,\"poolSize\":"+defs.wheelRewards.Length+"}");
            }
            finally{if(setup.Any(s=>s.isLoaded)&&setup.Any(s=>s.isActive))EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        private static void RepairCopy()
        {
            var prefab=PrefabUtility.LoadPrefabContents(PanelPath);
            try
            {
                var keys=new Dictionary<string,string>{{"Heading","wheel.title"},{"Subtitle","wheel.subtitle"},
                    {"ResetHint","wheel.reset_hint"},{"CostsHeading","wheel.costs"},{"Costs","wheel.costs_body"}};
                bool changed=false;
                foreach(var label in prefab.GetComponentsInChildren<Text>(true))
                    if(keys.TryGetValue(label.name,out var key) && label.text!=defs.Text(key)){label.text=defs.Text(key);changed=true;}
                var view=prefab.GetComponent<LuckyWheelUi>();
                foreach(var pair in new[]{(view.cancel,"wheel.cancel"),(view.inventory,"wheel.inventory")})
                {var label=pair.Item1.GetComponentInChildren<Text>(true);if(label.text!=defs.Text(pair.Item2)){label.text=defs.Text(pair.Item2);changed=true;}}
                if(changed)PrefabUtility.SaveAsPrefabAsset(prefab,PanelPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
        }
        private static void RepairRewardIcon()
        {
            var prefab=PrefabUtility.LoadPrefabContents(RewardPresentationAuthoring.CardPath);
            try
            {
                var icon=prefab.GetComponent<RewardCardView>().icon;
                if(icon.transform.parent!=prefab.transform)return;
                var viewport=Rect("RewardIconViewport",prefab.transform,icon.rectTransform.sizeDelta);
                viewport.anchoredPosition=icon.rectTransform.anchoredPosition;viewport.SetSiblingIndex(icon.transform.GetSiblingIndex());
                viewport.gameObject.AddComponent<RectMask2D>();icon.transform.SetParent(viewport,false);icon.rectTransform.anchoredPosition=Vector2.zero;
                PrefabUtility.SaveAsPrefabAsset(prefab,RewardPresentationAuthoring.CardPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
        }
        private static void CreatePrize()
        {
            if(File.Exists(PrizePath))return;
            var rect=Rect("WheelPrize",null,new Vector2(108,118));var view=rect.gameObject.AddComponent<LuckyWheelPrizeView>();
            var hit=rect.gameObject.AddComponent<Image>();hit.sprite=surface;hit.color=Color.clear;
            view.button=rect.gameObject.AddComponent<Button>();view.button.targetGraphic=hit;
            view.content=Rect("Content",rect,rect.sizeDelta);
            var viewport=Rect("IconViewport",view.content,new Vector2(58,58));viewport.anchoredPosition=new Vector2(0,22);viewport.gameObject.AddComponent<RectMask2D>();
            view.icon=Image("Icon",viewport,art.Get("gift"),Vector2.zero,new Vector2(58,58));
            view.caption=Label("Caption",view.content,"",new Vector2(0,-33),new Vector2(106,50),15,Paper);view.caption.fontStyle=FontStyle.Bold;
            Save(rect,PrizePath);
        }
        private static void CreatePanel()
        {
            if(File.Exists(PanelPath))return;
            var rect=Rect("LuckyWheel",null,new Vector2(1400,970));var view=rect.gameObject.AddComponent<LuckyWheelUi>();
            var bg=rect.gameObject.AddComponent<Image>();bg.sprite=surface;bg.color=new Color(.022f,.073f,.083f,.997f);
            Image("TopRule",rect,surface,new Vector2(0,483),new Vector2(1400,3),Gold);
            Label("Heading",rect,defs.Text("wheel.title"),new Vector2(-290,420),new Vector2(640,62),46,Gold,true);
            Label("Subtitle",rect,defs.Text("wheel.subtitle"),new Vector2(-290,373),new Vector2(640,32),21,Muted);
            view.remaining=Label("Remaining",rect,"",new Vector2(-290,334),new Vector2(600,30),19,Paper);
            view.rays=Rect("GlowRays",rect,new Vector2(730,730));view.rays.anchoredPosition=new Vector2(-290,-15);
            var rays=view.rays.gameObject.AddComponent<RewardBurstGraphic>();rays.color=new Color(.48f,.81f,.57f,.15f);rays.raycastTarget=false;
            var frame=Rect("MarqueeFrame",rect,new Vector2(650,650));frame.anchoredPosition=new Vector2(-290,-15);
            var ring=frame.gameObject.AddComponent<LuckyWheelGraphic>();ring.frameOnly=true;ring.raycastTarget=false;
            view.disc=Rect("RotatingWheel",rect,new Vector2(558,558));view.disc.anchoredPosition=new Vector2(-290,-15);
            view.graphic=view.disc.gameObject.AddComponent<LuckyWheelGraphic>();view.graphic.raycastTarget=false;
            view.slots=new LuckyWheelPrizeView[10];
            for(int i=0;i<10;i++){var slot=Instance(PrizePath,view.disc);slot.name="Prize_"+(i+1);view.slots[i]=slot.GetComponent<LuckyWheelPrizeView>();}
            Image("HubLaurel",rect,art.Get("laurel"),new Vector2(-290,-15),new Vector2(66,66),Gold);
            Image("PointerShadow",rect,art.Get("down"),new Vector2(-288,293),new Vector2(65,65),new Color(.025f,.08f,.09f));
            Image("Pointer",rect,art.Get("down"),new Vector2(-290,300),new Vector2(57,57),Gold);
            view.chance=Label("Chance",rect,"",new Vector2(-290,-361),new Vector2(620,32),18,Muted);
            Label("ResetHeading",rect,"TÄGLICHE ERNEUERUNG",new Vector2(363,418),new Vector2(460,30),16,Gold);
            view.countdown=Label("Countdown",rect,"",new Vector2(363,375),new Vector2(480,58),29,Paper,true);
            Label("ResetHint",rect,defs.Text("wheel.reset_hint"),new Vector2(363,328),new Vector2(490,30),16,Muted);
            Image("DetailsSurface",rect,surface,new Vector2(363,92),new Vector2(488,410),new Color(.045f,.14f,.15f));
            view.itemAccent=Image("ItemAccent",rect,surface,new Vector2(363,296),new Vector2(488,3),Gold);
            var itemViewport=Rect("ItemIconViewport",rect,new Vector2(108,108));itemViewport.anchoredPosition=new Vector2(363,220);itemViewport.gameObject.AddComponent<RectMask2D>();
            view.itemIcon=Image("ItemIcon",itemViewport,art.Get("gift"),Vector2.zero,new Vector2(108,108));
            view.itemName=Label("ItemName",rect,"",new Vector2(363,120),new Vector2(440,74),26,Paper,true);
            view.description=Label("ItemDescription",rect,"",new Vector2(363,-5),new Vector2(430,168),18,Muted);
            view.costHeading=Label("CostsHeading",rect,defs.Text("wheel.costs"),new Vector2(363,-143),new Vector2(460,24),14,Gold);
            view.costBody=Label("Costs",rect,defs.Text("wheel.costs_body"),new Vector2(363,-192),new Vector2(480,60),17,Muted);
            view.balance=Label("Balance",rect,"",new Vector2(363,-254),new Vector2(460,32),19,Paper);
            view.spin=Button("Spin",rect,"",new Vector2(363,-313),new Vector2(426,66));view.spinLabel=view.spin.GetComponentInChildren<Text>();view.spinLabel.fontSize=23;
            view.cancel=Button("Cancel",rect,defs.Text("wheel.cancel"),new Vector2(363,-371),new Vector2(190,44));view.cancel.gameObject.SetActive(false);
            view.inventory=Button("OpenInventory",rect,defs.Text("wheel.inventory"),new Vector2(-290,-421),new Vector2(300,48));
            view.status=Label("Status",rect,"",new Vector2(363,-430),new Vector2(490,78),18,Gold);
            Save(rect,PanelPath);
        }
        private static GameObject Instance(string path,Transform parent)=>(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),parent);
        private static RectTransform Rect(string name,Transform parent,Vector2 size)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;return r;}
        private static Image Image(string name,Transform parent,Sprite sprite,Vector2 pos,Vector2 size,Color? color=null)
        {var r=Rect(name,parent,size);r.anchoredPosition=pos;var image=r.gameObject.AddComponent<Image>();image.sprite=sprite;image.color=color??Color.white;image.preserveAspect=sprite!=surface;image.raycastTarget=false;return image;}
        private static Text Label(string name,Transform parent,string value,Vector2 pos,Vector2 size,int font,Color color,bool heading=false)
        {var r=Rect(name,parent,size);r.anchoredPosition=pos;var text=r.gameObject.AddComponent<Text>();text.font=heading?art.headingFont:art.bodyFont;text.fontSize=font;text.text=value;text.color=color;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.supportRichText=false;return text;}
        private static Button Button(string name,Transform parent,string text,Vector2 pos,Vector2 size)
        {var go=Instance(RewardPresentationAuthoring.ButtonPath,parent);go.name=name;var rect=go.GetComponent<RectTransform>();rect.anchoredPosition=pos;rect.sizeDelta=size;var label=go.GetComponentInChildren<Text>();label.rectTransform.sizeDelta=new Vector2(size.x-16,size.y-4);label.text=text;label.fontSize=20;return go.GetComponent<Button>();}
        private static void Save(RectTransform rect,string path){PrefabUtility.SaveAsPrefabAsset(rect.gameObject,path);Object.DestroyImmediate(rect.gameObject);}
    }
}
