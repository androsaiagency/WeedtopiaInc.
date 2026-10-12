using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Unity;
using Weedtopia.Domain;

namespace Weedtopia.Editor.GameSetup
{
    public sealed class UiAuthoring
    {
        private readonly List<UiBinding> bindings=new List<UiBinding>();
        private readonly List<GameObject> panels=new List<GameObject>();
        private readonly Font font;
        private readonly GameDefinitions defs;
        private Transform safe,shade;
        private static Color Ink=>new Color(.045f,.095f,.12f,1);
        private static Color Paper=>new Color(.93f,.9f,.79f);
        private static Color Gold=>new Color(.94f,.68f,.28f);
        public UiAuthoring(GameDefinitions definitions){defs=definitions;font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");}
        public GameUi Create()
        {
            var canvasObject=new GameObject("BurningHillsUI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(GameUi));
            var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            safe=Rect("SafeArea",canvasObject.transform);Stretch((RectTransform)safe);
            var top=Panel("TopBar",safe,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,0),new Vector2(1920,106));
            Text("title",top,"BURNING HILLS",new Vector2(-715,-28),new Vector2(390,40),30,Gold);
            Text("hud",top,"",new Vector2(100,-32),new Vector2(1150,46),26,Paper);
            Text("development",top,"",new Vector2(110,-76),new Vector2(1540,30),16,new Color(.65f,.77f,.72f));
            var quest=Panel("QuestCard",safe,new Vector2(0,1),new Vector2(0,1),new Vector2(24,-135),new Vector2(350,350));
            Text("questTitle",quest,"",new Vector2(0,-30),new Vector2(314,56),24,Gold);
            Text("questBody",quest,"",new Vector2(0,-143),new Vector2(314,178),21,Paper);
            Button("ui.claim",quest,new Vector2(0,-264),new Vector2(306,48));Button("ui.focus",quest,new Vector2(0,-320),new Vector2(306,42));
            var detail=Panel("BuildingDetails",safe,new Vector2(1,1),new Vector2(1,1),new Vector2(-24,-135),new Vector2(360,790));
            Text("detailTitle",detail,"",new Vector2(0,-31),new Vector2(324,50),26,Gold);
            Text("detailBody",detail,"",new Vector2(0,-166),new Vector2(324,212),21,Paper);
            Text("flow",detail,"",new Vector2(0,-316),new Vector2(324,95),20,new Color(.5f,.92f,.69f));
            string[] actions={"ui.upgrade","ui.finish","ui.harvest","ui.work","ui.workers","ui.talk","ui.house","ui.park"};
            for(int i=0;i<actions.Length;i++)Button(actions[i],detail,new Vector2(0,-389-i*48),new Vector2(318,41));
            var nav=Panel("Navigation",safe,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,0),new Vector2(1920,104));
            string[] navigation={"zone.Farm","zone.City","zone.Settlement","zone.Industry","ui.townhall","ui.journal","ui.save","ui.pause"};
            for(int i=0;i<navigation.Length;i++)Button(navigation[i],nav,new Vector2(-819+i*234,-48),new Vector2(218,55));
            Text("message",safe,"",new Vector2(0,-945),new Vector2(1120,45),21,Paper);
            Button("recovery",safe,new Vector2(-734,-875),new Vector2(336,50));
            var shadeRect=Rect("ModalShade",safe);Stretch(shadeRect);var background=shadeRect.gameObject.AddComponent<Image>();background.color=new Color(.01f,.02f,.025f,.82f);shade=shadeRect;Bind("modalShade",shade.gameObject);
            BuildPanels();
            var close=Button("ui.close",shade,new Vector2(475,-192),new Vector2(135,45));close.GetComponent<RectTransform>().anchorMin=close.GetComponent<RectTransform>().anchorMax=new Vector2(.5f,1);
            string rowPath=GameScaffold.Prefabs+"/ListRow.prefab";
            var rowAsset=AssetDatabase.LoadAssetAtPath<GameObject>(rowPath);
            if(rowAsset==null)
            {
                var row=MakeButton("ListRow",null,"",new Vector2(0,0),new Vector2(400,82));row.gameObject.AddComponent<LayoutElement>().preferredHeight=82;
                row.GetComponentInChildren<Text>().alignment=TextAnchor.MiddleLeft;row.GetComponentInChildren<Text>().fontSize=20;
                rowAsset=PrefabUtility.SaveAsPrefabAsset(row.gameObject,rowPath);Object.DestroyImmediate(row.gameObject);
            }
            var ui=canvasObject.GetComponent<GameUi>();ui.EditorConfigure(bindings.ToArray(),panels.ToArray(),rowAsset.GetComponent<Button>(),(RectTransform)safe,scaler);
            GamePolish.ApplyResponsiveLayout(ui);
            shade.gameObject.SetActive(false);foreach(var panel in panels)panel.SetActive(false);
            return ui;
        }
        private void BuildPanels()
        {
            var start=Modal("start","Burning Hills",new Vector2(820,590));
            Text("startSubtitle",start,defs.Text("game.subtitle"),new Vector2(0,-110),new Vector2(740,65),25,Paper);
            Text("nameLabel",start,defs.Text("ui.name"),new Vector2(0,-176),new Vector2(680,40),23,Gold);
            var input=DefaultControls.CreateInputField(new DefaultControls.Resources());input.name="name";input.transform.SetParent(start,false);Position(input.GetComponent<RectTransform>(),new Vector2(0,-235),new Vector2(600,62));
            foreach(var t in input.GetComponentsInChildren<Text>(true)){t.font=font;t.fontSize=25;t.color=new Color(.06f,.14f,.15f);}
            input.GetComponent<InputField>().characterLimit=24;Bind("name",input);
            Button("ui.start",start,new Vector2(0,-329),new Vector2(600,64));Button("ui.continue",start,new Vector2(0,-411),new Vector2(600,60));
            Text("newNote",start,defs.Text("ui.new_note"),new Vector2(0,-500),new Vector2(720,78),20,Paper);
            // Required command binding for a visible menu action; it opens the same authored start panel.
            Button("ui.new",start,new Vector2(0,-558),new Vector2(600,42));
            var intro=Modal("intro","Rocko · Bürgermeister",new Vector2(960,570));
            Text("rockoIdentity",intro,defs.Text("ui.rocko_identity"),new Vector2(0,-132),new Vector2(860,60),23,Gold);
            Text("introText",intro,"",new Vector2(0,-284),new Vector2(830,215),30,Paper);
            Button("ui.next",intro,new Vector2(210,-480),new Vector2(350,60));Button("ui.skip",intro,new Vector2(-210,-480),new Vector2(350,60));
            var workers=Modal("workers",defs.Text("ui.workers"),new Vector2(1130,740));
            Dropdown("filter",workers,new Vector2(-280,-100),new Vector2(470,48));Scroll("workerList",workers,new Vector2(-280,-380),new Vector2(470,485));
            Text("workerText",workers,"",new Vector2(280,-207),new Vector2(470,230),23,Paper);
            Text("assignmentTarget",workers,"",new Vector2(280,-374),new Vector2(470,68),21,Gold);
            Dropdown("slot",workers,new Vector2(280,-430),new Vector2(450,46));
            Button("ui.assign",workers,new Vector2(280,-487),new Vector2(450,46));Button("ui.remove",workers,new Vector2(280,-543),new Vector2(450,46));
            Button("ui.worker_level",workers,new Vector2(280,-599),new Vector2(450,46));Button("ui.worker_rank",workers,new Vector2(280,-655),new Vector2(450,46));
            var townhall=Modal("townhall",defs.Text("building.town_hall"),new Vector2(1100,730));
            Text("stats",townhall,"",new Vector2(-215,-299),new Vector2(610,428),24,Paper);Text("scores",townhall,"",new Vector2(328,-294),new Vector2(350,390),22,Gold);
            Button("ui.spin",townhall,new Vector2(-230,-624),new Vector2(490,62));Button("ui.daily",townhall,new Vector2(290,-624),new Vector2(460,62));
            var journal=Modal("journal",defs.Text("ui.journal"),new Vector2(1040,740));Scroll("journalList",journal,new Vector2(0,-410),new Vector2(930,550));
            var settings=Modal("settings",defs.Text("ui.pause"),new Vector2(820,620));
            Text("volumeLabel",settings,defs.Text("ui.volume"),new Vector2(0,-125),new Vector2(700,40),24,Gold);Slider("volume",settings,new Vector2(0,-173),0,1,.7f);
            Text("scaleLabel",settings,defs.Text("ui.scale"),new Vector2(0,-240),new Vector2(700,40),24,Gold);Slider("scale",settings,new Vector2(0,-290),.85f,1.15f,1);
            Button("ui.resume",settings,new Vector2(0,-370),new Vector2(670,58));Button("ui.restart_intro",settings,new Vector2(0,-447),new Vector2(670,58));Button("ui.reset",settings,new Vector2(0,-524),new Vector2(670,58));
            var premium=Modal("premium",defs.Text("ui.premium_title"),new Vector2(830,450));Text("premiumText",premium,"",new Vector2(0,-190),new Vector2(730,185),28,Gold);
            Button("ui.confirm",premium,new Vector2(180,-365),new Vector2(385,64));Button("ui.cancel",premium,new Vector2(-232,-365),new Vector2(320,64));
            var offline=Modal("offline",defs.Text("ui.offline_title"),new Vector2(830,460));Text("offlineText",offline,"",new Vector2(0,-248),new Vector2(720,255),28,Paper);
        }
        private RectTransform Modal(string id,string title,Vector2 size)
        {
            var panel=Panel(id,shade,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,size);Bind(id,panel.gameObject);panels.Add(panel.gameObject);
            Text(id+"Title",panel,title,new Vector2(0,-48),new Vector2(size.x-80,60),34,Gold);return panel;
        }
        private void Slider(string id,Transform parent,Vector2 position,float min,float max,float value)
        {
            var obj=DefaultControls.CreateSlider(new DefaultControls.Resources());obj.name=id;obj.transform.SetParent(parent,false);Position(obj.GetComponent<RectTransform>(),position,new Vector2(620,35));
            var slider=obj.GetComponent<Slider>();slider.minValue=min;slider.maxValue=max;slider.value=value;Bind(id,obj);
        }
        private void Dropdown(string id,Transform parent,Vector2 position,Vector2 size)
        {
            var obj=DefaultControls.CreateDropdown(new DefaultControls.Resources());obj.name=id;obj.transform.SetParent(parent,false);Position(obj.GetComponent<RectTransform>(),position,size);
            foreach(var t in obj.GetComponentsInChildren<Text>(true)){t.font=font;t.fontSize=21;}
            Bind(id,obj);
        }
        private void Scroll(string id,Transform parent,Vector2 position,Vector2 size)
        {
            var viewport=Rect(id+"Viewport",parent);Position(viewport,position,size);viewport.gameObject.AddComponent<Image>().color=new Color(.03f,.07f,.085f);viewport.gameObject.AddComponent<Mask>().showMaskGraphic=true;
            var content=Rect(id,viewport);content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;
            var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=8;layout.childControlHeight=true;layout.childForceExpandHeight=false;layout.childControlWidth=true;layout.childForceExpandWidth=true;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.scrollSensitivity=35;Bind(id,content.gameObject);
        }
        private RectTransform Panel(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
        {
            var rect=Rect(name,parent);rect.anchorMin=rect.anchorMax=anchor;rect.pivot=pivot;rect.anchoredPosition=position;rect.sizeDelta=size;rect.gameObject.AddComponent<Image>().color=Ink;return rect;
        }
        private Text Text(string id,Transform parent,string value,Vector2 position,Vector2 size,int fontSize,Color color)
        {
            var rect=Rect(id,parent);Position(rect,position,size);var t=rect.gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=fontSize;t.color=color;t.alignment=TextAnchor.MiddleLeft;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.raycastTarget=false;Bind(id,rect.gameObject);return t;
        }
        private Button Button(string id,Transform parent,Vector2 position,Vector2 size)
        {
            var button=MakeButton(id,parent,defs.Text(id),position,size);Bind(id,button.gameObject);return button;
        }
        private Button MakeButton(string id,Transform parent,string title,Vector2 position,Vector2 size)
        {
            var rect=Rect(id,parent);Position(rect,position,size);var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.17f,.33f,.31f);
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;var colors=button.colors;colors.highlightedColor=new Color(.85f,1,.8f);colors.pressedColor=new Color(.65f,.8f,.65f);colors.disabledColor=new Color(.45f,.48f,.48f,.65f);button.colors=colors;
            var label=Rect("Label",rect);Stretch(label);label.offsetMin=new Vector2(12,3);label.offsetMax=new Vector2(-12,-3);var text=label.gameObject.AddComponent<Text>();text.font=font;text.text=title;text.color=Paper;text.fontSize=21;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;return button;
        }
        private void Bind(string id,GameObject obj){bindings.Add(new UiBinding{id=id,target=obj});}
        private static RectTransform Rect(string name,Transform parent){var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);return obj.GetComponent<RectTransform>();}
        private static void Position(RectTransform rect,Vector2 position,Vector2 size){rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;}
        private static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
    }
}
