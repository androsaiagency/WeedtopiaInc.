using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Unity;
using Object=UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    public static class ProductionUiAuthoring
    {
        private static readonly Color Paper=new Color(.96f,.91f,.77f),Gold=new Color(.98f,.76f,.36f),Ink=new Color(.055f,.15f,.16f);
        private static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);var rt=obj.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(.5f,1);rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=position;rt.sizeDelta=size;return rt;}
        private static Image Image(string name,Transform parent,Sprite sprite,Vector2 position,Vector2 size,bool slice=false)
        {var rt=Rect(name,parent,position,size);var image=rt.gameObject.AddComponent<Image>();image.sprite=sprite;image.color=Color.white;image.raycastTarget=false;image.type=slice?UnityEngine.UI.Image.Type.Sliced:UnityEngine.UI.Image.Type.Simple;image.preserveAspect=!slice;return image;}
        private static Text Label(string name,Transform parent,string value,Vector2 position,Vector2 size,Font font,int fontSize=24)
        {var rt=Rect(name,parent,position,size);var text=rt.gameObject.AddComponent<Text>();text.font=font;text.text=value;text.fontSize=fontSize;text.color=Paper;text.raycastTarget=false;text.alignment=TextAnchor.MiddleLeft;return text;}
        private static void Position(GameObject obj,Vector2 p,Vector2 size){var rt=obj.GetComponent<RectTransform>();rt.anchoredPosition=p;rt.sizeDelta=size;}
        private static readonly Dictionary<string,string> ButtonIcons=new Dictionary<string,string>
        {
            {"ui.start","play"},{"ui.continue","play"},{"ui.new","back"},{"ui.next","play"},{"ui.skip","back"},{"ui.claim","gift"},{"ui.focus","zoom"},
            {"ui.upgrade","upgrade"},{"ui.finish","time"},{"ui.harvest","harvest"},{"ui.work","work"},{"ui.workers","person"},{"ui.talk","talk"},{"ui.house","settlement"},{"ui.park","farm"},
            {"zone.Farm","farm"},{"zone.City","city"},{"zone.Settlement","settlement"},{"zone.Industry","industry"},{"ui.townhall","city"},{"ui.journal","quest"},{"ui.save","save"},{"ui.pause","settings"},
            {"ui.close","close"},{"ui.spin","cards"},{"ui.daily","gift"},{"ui.assign","confirm"},{"ui.remove","back"},{"ui.worker_level","upgrade"},{"ui.worker_rank","star"},
            {"ui.resume","play"},{"ui.restart_intro","talk"},{"ui.reset","back"},{"ui.confirm","confirm"},{"ui.cancel","back"},{"recovery","money"}
        };
        private static void SkinButton(Button button,PresentationArt art,string icon)
        {
            bool primary=new[]{"ui.start","ui.next","ui.claim","ui.upgrade","ui.confirm","ui.assign","ui.spin"}.Contains(button.name);
            var image=button.GetComponent<Image>();image.sprite=art.Get(primary?"primary":"button");image.type=UnityEngine.UI.Image.Type.Sliced;image.color=Color.white;image.pixelsPerUnitMultiplier=2;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,1,.84f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.7f,.8f,.75f);colors.disabledColor=new Color(.5f,.57f,.55f,.85f);colors.fadeDuration=.1f;button.colors=colors;
            var text=button.GetComponentInChildren<Text>(true);text.font=art.headingFont;text.fontSize=20;text.color=primary?Ink:Paper;
            if(icon!=null)
            {
                var graphic=Image("ActionIcon",button.transform,art.Get(icon),Vector2.zero,new Vector2(30,30));var rt=graphic.rectTransform;rt.anchorMin=rt.anchorMax=new Vector2(0,.5f);rt.anchoredPosition=new Vector2(28,0);
                text.rectTransform.offsetMin=new Vector2(50,4);text.rectTransform.offsetMax=new Vector2(-12,-4);
            }
        }
        public static void Author(GameUi ui,ProductionPresentation p,PresentationArt art)
        {
            var so=new SerializedObject(ui);var entries=so.FindProperty("bindings");var map=new Dictionary<string,GameObject>();
            for(int i=0;i<entries.arraySize;i++){var e=entries.GetArrayElementAtIndex(i);map.Add(e.FindPropertyRelative("id").stringValue,(GameObject)e.FindPropertyRelative("target").objectReferenceValue);}
            foreach(var text in ui.GetComponentsInChildren<Text>(true))
            {text.font=text.fontSize>=28?art.headingFont:art.bodyFont;text.color=Paper;text.supportRichText=false;}
            foreach(var image in ui.GetComponentsInChildren<Image>(true))
            {
                if(image.name=="ModalShade")continue;
                image.sprite=art.Get("panel");image.type=UnityEngine.UI.Image.Type.Sliced;image.pixelsPerUnitMultiplier=2;image.color=Color.white;
            }
            foreach(var button in ui.GetComponentsInChildren<Button>(true))SkinButton(button,art,ButtonIcons.TryGetValue(button.name,out var icon)?icon:null);
            foreach(var dropdown in ui.GetComponentsInChildren<Dropdown>(true))
            {
                dropdown.GetComponent<Image>().sprite=art.Get("button");
                foreach(var image in dropdown.GetComponentsInChildren<Image>(true))
                {if(image.name=="Arrow"){image.sprite=art.Get("down");image.type=UnityEngine.UI.Image.Type.Simple;image.preserveAspect=true;image.color=Gold;}else if(image.name=="Item Checkmark"){image.sprite=art.Get("confirm");image.type=UnityEngine.UI.Image.Type.Simple;image.preserveAspect=true;}}
                foreach(var text in dropdown.GetComponentsInChildren<Text>(true)){text.color=Paper;text.font=art.bodyFont;}
            }
            foreach(var slider in ui.GetComponentsInChildren<Slider>(true))
            {
                slider.GetComponent<RectTransform>().sizeDelta=new Vector2(620,48);
                foreach(var image in slider.GetComponentsInChildren<Image>(true))image.sprite=art.Get(image.name=="Handle"?"star":image.name=="Fill"?"primary":"button");
                slider.handleRect.sizeDelta=new Vector2(38,44);
            }
            foreach(var input in ui.GetComponentsInChildren<InputField>(true))
            {
                input.GetComponent<Image>().sprite=art.Get("paper");foreach(var text in input.GetComponentsInChildren<Text>(true)){text.font=art.bodyFont;text.color=Ink;}
                if(input.placeholder is Text placeholder)placeholder.text="Dein Name";
            }
            map["development"].SetActive(false);map["hud"].SetActive(false);
            var top=map["title"].transform.parent.GetComponent<RectTransform>();top.sizeDelta=new Vector2(0,96);
            var title=map["title"].GetComponent<Text>();title.fontSize=28;title.color=Gold;title.rectTransform.anchoredPosition=new Vector2(86,-37);title.rectTransform.sizeDelta=new Vector2(360,44);
            var emblem=Image("BurningHillsSeal",top,art.Get("money"),Vector2.zero,new Vector2(62,62));emblem.rectTransform.anchorMin=emblem.rectTransform.anchorMax=new Vector2(0,1);emblem.rectTransform.anchoredPosition=new Vector2(46,-45);
            var subtitle=Label("DistrictSubtitle",top,"WIEDERAUFBAU · AFTERGLOW",new Vector2(88,-72),new Vector2(320,20),art.headingFont,14);subtitle.rectTransform.anchorMin=subtitle.rectTransform.anchorMax=new Vector2(0,1);subtitle.rectTransform.pivot=new Vector2(0,.5f);subtitle.color=new Color(.62f,.8f,.72f);
            var currencies=Rect("CurrencyCounters",top,Vector2.zero,new Vector2(980,70));currencies.anchorMin=new Vector2(.36f,0);currencies.anchorMax=new Vector2(.97f,1);currencies.offsetMin=new Vector2(0,9);currencies.offsetMax=new Vector2(0,-9);
            var layout=currencies.gameObject.AddComponent<HorizontalLayoutGroup>();layout.spacing=14;layout.childControlWidth=true;layout.childForceExpandWidth=true;layout.childControlHeight=true;
            p.currencyValues=new Text[4];string[] resources={"money","grinder","cards","residents"};string[] labels={"GELD","GRINDER","KARTEN","EINWOHNER"};
            for(int i=0;i<4;i++)
            {
                var cell=Rect(resources[i],currencies,Vector2.zero,new Vector2(200,70));var le=cell.gameObject.AddComponent<LayoutElement>();le.flexibleWidth=1;le.preferredWidth=200;
                var icon=Image("Icon",cell,art.Get(resources[i]),new Vector2(-75,-34),new Vector2(48,48));icon.rectTransform.anchorMin=icon.rectTransform.anchorMax=new Vector2(0,1);icon.rectTransform.anchoredPosition=new Vector2(26,-34);
                var value=Label("Value",cell,"0",new Vector2(59,-42),new Vector2(160,34),art.headingFont,26);value.rectTransform.anchorMin=value.rectTransform.anchorMax=new Vector2(0,1);value.rectTransform.pivot=new Vector2(0,.5f);p.currencyValues[i]=value;
                var caption=Label("Caption",cell,labels[i],new Vector2(59,-16),new Vector2(160,21),art.headingFont,13);caption.rectTransform.anchorMin=caption.rectTransform.anchorMax=new Vector2(0,1);caption.rectTransform.pivot=new Vector2(0,.5f);caption.color=Gold;
            }
            var quest=map["questTitle"].transform.parent.GetComponent<RectTransform>();quest.sizeDelta=new Vector2(350,390);quest.anchoredPosition=new Vector2(24,-116);
            Position(map["questTitle"],new Vector2(34,-47),new Vector2(236,74));map["questTitle"].GetComponent<Text>().fontSize=21;
            p.questPortrait=Image("QuestPortrait",quest,art.Get("portrait_rocko"),new Vector2(-122,-51),new Vector2(74,78));
            Position(map["questBody"],new Vector2(0,-183),new Vector2(302,182));map["questBody"].GetComponent<Text>().fontSize=20;
            Position(map["ui.claim"],new Vector2(0,-306),new Vector2(306,52));Position(map["ui.focus"],new Vector2(0,-363),new Vector2(306,48));
            var intro=map["intro"].GetComponent<RectTransform>();intro.sizeDelta=new Vector2(1080,620);
            Position(map["introTitle"],new Vector2(145,-56),new Vector2(700,65));
            p.introPortrait=Image("RockoPortrait",intro,art.Get("portrait_rocko"),new Vector2(-355,-295),new Vector2(310,350));
            Position(map["rockoIdentity"],new Vector2(163,-142),new Vector2(610,86));Position(map["introText"],new Vector2(163,-318),new Vector2(610,235));map["introText"].GetComponent<Text>().fontSize=28;
            Position(map["ui.next"],new Vector2(273,-534),new Vector2(360,60));Position(map["ui.skip"],new Vector2(-115,-534),new Vector2(340,60));
            var workers=map["workers"].transform;p.workerReveal=Rect("WorkerCard",workers,new Vector2(280,-167),new Vector2(472,153));
            p.workerRarity=Image("RarityFrame",p.workerReveal,art.Get("paper"),new Vector2(0,-76),new Vector2(472,153),true);p.workerRarity.transform.SetAsFirstSibling();
            p.workerPortrait=Image("WorkerPortrait",p.workerReveal,art.Get("portrait_mila"),new Vector2(-165,-72),new Vector2(130,140));
            p.workerRoleIcon=Image("RoleIcon",p.workerReveal,art.Get("person"),new Vector2(-49,-58),new Vector2(48,48));
            p.workerName=Label("RoleName",p.workerReveal,"Dein Team",new Vector2(95,-68),new Vector2(220,78),art.headingFont,23);p.workerName.color=Ink;
            Position(map["workerText"],new Vector2(280,-294),new Vector2(470,138));map["workerText"].GetComponent<Text>().fontSize=21;
            var hall=map["townhall"].transform;Image("MayorPortrait",hall,art.Get("portrait_rocko"),new Vector2(394,-112),new Vector2(110,110));
            Position(map["scores"],new Vector2(328,-357),new Vector2(350,300));map["stats"].GetComponent<Text>().fontSize=22;
            var start=map["start"].transform;Image("WelcomePortrait",start,art.Get("portrait_rocko"),new Vector2(-340,-270),new Vector2(110,150));
            var rowObj=PrefabUtility.LoadPrefabContents(GameScaffold.Prefabs+"/ListRow.prefab");
            try
            {
                SkinButton(rowObj.GetComponent<Button>(),art,null);var text=rowObj.GetComponentInChildren<Text>();text.fontSize=19;text.rectTransform.offsetMin=new Vector2(88,8);text.rectTransform.offsetMax=new Vector2(-18,-8);
                var icon=Image("RowIcon",rowObj.transform,art.Get("quest"),Vector2.zero,new Vector2(65,65));icon.rectTransform.anchorMin=icon.rectTransform.anchorMax=new Vector2(0,.5f);icon.rectTransform.anchoredPosition=new Vector2(43,0);
                PrefabUtility.SaveAsPrefabAsset(rowObj,GameScaffold.Prefabs+"/ListRow.prefab");
            }
            finally{PrefabUtility.UnloadPrefabContents(rowObj);}
            p.contextualActions=new[]{"ui.upgrade","ui.finish","ui.harvest","ui.work","ui.workers","ui.talk","ui.house","ui.park"}.Select(id=>map[id]).ToArray();
            foreach(var action in p.contextualActions)action.GetComponent<RectTransform>().sizeDelta=new Vector2(318,48);
            p.detailPanel=map["detailTitle"].transform.parent.GetComponent<RectTransform>();
            var body=map["detailBody"].GetComponent<Text>();body.fontSize=20;map["flow"].GetComponent<Text>().fontSize=19;
            // A real conversation panel with its own authored portrait and action.
            var conversation=Rect("conversation",map["modalShade"].transform,Vector2.zero,new Vector2(960,510));conversation.anchorMin=conversation.anchorMax=new Vector2(.5f,.5f);conversation.pivot=new Vector2(.5f,1);conversation.anchoredPosition=new Vector2(0,255);
            var background=conversation.gameObject.AddComponent<Image>();background.sprite=art.Get("panel");background.type=UnityEngine.UI.Image.Type.Sliced;background.pixelsPerUnitMultiplier=2;
            var cp=Image("conversationPortrait",conversation,art.Get("portrait_rocko"),new Vector2(-317,-240),new Vector2(240,300));
            var ct=Label("conversationTitle",conversation,"Rocko",new Vector2(125,-70),new Vector2(530,60),art.headingFont,30);ct.color=Gold;
            var cb=Label("conversationText",conversation,"",new Vector2(125,-237),new Vector2(530,234),art.bodyFont,26);
            var buttonObject=Object.Instantiate(map["ui.next"],conversation,false);buttonObject.name="ui.dialog_continue";Position(buttonObject,new Vector2(130,-426),new Vector2(490,60));buttonObject.GetComponentInChildren<Text>().text="Weiter";
            void Bind(string id,GameObject obj){int n=entries.arraySize;entries.InsertArrayElementAtIndex(n);var e=entries.GetArrayElementAtIndex(n);e.FindPropertyRelative("id").stringValue=id;e.FindPropertyRelative("target").objectReferenceValue=obj;}
            Bind("conversation",conversation.gameObject);Bind("conversationPortrait",cp.gameObject);Bind("conversationTitle",ct.gameObject);Bind("conversationText",cb.gameObject);Bind("ui.dialog_continue",buttonObject);
            var panels=so.FindProperty("panels");int count=panels.arraySize;panels.InsertArrayElementAtIndex(count);panels.GetArrayElementAtIndex(count).objectReferenceValue=conversation.gameObject;conversation.gameObject.SetActive(false);so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
