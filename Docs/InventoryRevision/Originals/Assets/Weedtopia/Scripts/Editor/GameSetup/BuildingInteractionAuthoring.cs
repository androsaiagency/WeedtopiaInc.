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
    /// <summary>Explicit, additive scene migration. Existing UI objects and manual art references are retained.</summary>
    public static class BuildingInteractionAuthoring
    {
        private static PresentationArt art;
        private static readonly Color Paper = new Color(.96f,.91f,.77f), Gold = new Color(.98f,.76f,.36f), Green = new Color(.49f,.94f,.55f);
        private const string RowPath = "Assets/Weedtopia/Production/Prefabs/InteractionRow.prefab";
        [MenuItem("Weedtopia/Presentation/Install Building Interaction")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before authoring");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save manual scene changes first");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(GameScaffold.GameScene);var root=Object.FindFirstObjectByType<GameRoot>();
                if(root.Ui.GetComponent<BuildingInteractionUi>()!=null){RepairOwnedMarks(root);return;}
                art=root.Presentation.art;
                var so=new SerializedObject(root.Ui);var list=so.FindProperty("bindings");var map=new Dictionary<string,GameObject>();
                for(int i=0;i<list.arraySize;i++){var e=list.GetArrayElementAtIndex(i);map.Add(e.FindPropertyRelative("id").stringValue,(GameObject)e.FindPropertyRelative("target").objectReferenceValue);}
                var safe=(RectTransform)so.FindProperty("safeArea").objectReferenceValue;
                var view=root.Ui.gameObject.AddComponent<BuildingInteractionUi>();
                var archive=Rect("ArchivedFixedInterface",root.Ui.transform,Vector2.zero,Vector2.zero);archive.gameObject.SetActive(false);
                foreach(string id in new[]{"title","detailTitle","zone.Farm"})map[id].transform.parent.SetParent(archive,false);
                var navigator=Object.FindFirstObjectByType<DistrictNavigator>(FindObjectsInactive.Include);if(navigator!=null)navigator.transform.SetParent(archive,false);
                var inspection=Panel("BuildingInspector",safe,new Vector2(532,900));Left(inspection,28);
                view.inspector=inspection.gameObject;view.heading=Label("BuildingHeading",inspection,"Gebäude",new Vector2(-16,-67),new Vector2(426,79),30,true);
                view.closeBuilding=IconButton("CloseBuilding",inspection,new Vector2(224,-40),"close");
                view.staffPortrait=Picture("StaffPortrait",inspection,art.Get("portrait_mila"),new Vector2(-161,-209),new Vector2(138,164));
                view.staffAction=IconButton("StaffAssignment",inspection,new Vector2(-98,-135),null);
                view.staffMark=Mark("AssignmentMark",view.staffAction.transform,ActionMark.Shape.Plus,new Vector2(0,-24),new Vector2(44,44));
                view.staffName=Label("StaffName",inspection,"",new Vector2(77,-134),new Vector2(295,51),21,true);
                view.staffBody=Label("StaffAttributes",inspection,"",new Vector2(77,-232),new Vector2(295,144),18);
                view.train=Button("TrainWorker",inspection,"Level erhöhen",new Vector2(-126,-335),new Vector2(220,56),"upgrade");
                view.rank=Button("RankWorker",inspection,"Rang erhöhen",new Vector2(116,-335),new Vector2(220,56),"star");
                view.fleet=Button("Fleet",inspection,"Fuhrpark",new Vector2(0,-388),new Vector2(474,46),"truck");view.fleetLabel=view.fleet.GetComponentInChildren<Text>();
                view.status=Label("BuildingStatus",inspection,"",new Vector2(0,-438),new Vector2(470,43),19);view.status.color=Gold;
                var metricsTitle=Label("MetricsTitle",inspection,"GEBÄUDE-DETAILS",new Vector2(-133,-480),new Vector2(210,28),16,true);metricsTitle.color=Gold;
                var preview=Label("UpgradePreviewLabel",inspection,"NÄCHSTES LEVEL",new Vector2(158,-480),new Vector2(165,28),14,true);preview.color=Green;
                view.metricLabels=new Text[7];view.metricCurrent=new Text[7];view.metricNext=new Text[7];view.metricArrows=new GameObject[7];
                for(int i=0;i<7;i++)
                {
                    var row=Rect("Metric_"+i,inspection,new Vector2(0,-520-i*35),new Vector2(474,34));
                    view.metricLabels[i]=Label("Label",row,"",new Vector2(-141,-17),new Vector2(190,34),18);
                    view.metricCurrent[i]=Label("Current",row,"",new Vector2(12,-17),new Vector2(112,34),19,true);
                    view.metricNext[i]=Label("Next",row,"",new Vector2(181,-17),new Vector2(111,34),19,true);view.metricNext[i].color=Green;
                    var arrow=Mark("ImprovementArrow",row,ActionMark.Shape.Arrow,new Vector2(105,-17),new Vector2(29,29));arrow.color=Green;view.metricArrows[i]=arrow.gameObject;
                }
                view.upgrade=Button("UpgradeBuilding",inspection,"Upgrade",new Vector2(0,-790),new Vector2(474,58),"upgrade",true);view.upgradeLabel=view.upgrade.GetComponentInChildren<Text>();
                view.work=Button("BuildingAction",inspection,"Arbeiten",new Vector2(-124,-855),new Vector2(230,49),"work");
                view.talk=Button("BuildingConversation",inspection,"Ansprechen",new Vector2(124,-855),new Vector2(230,49),"talk");
                var picker=Panel("BuildingWorkerPicker",safe,new Vector2(580,900));Right(picker,28);view.picker=picker.gameObject;
                view.pickerTitle=Label("PickerTitle",picker,"Worker wählen",new Vector2(-10,-76),new Vector2(482,100),27,true);
                view.closePicker=IconButton("ClosePicker",picker,new Vector2(249,-40),"close");
                view.pickerHint=Label("AssignmentExplanation",picker,"",new Vector2(0,-817),new Vector2(516,104),20);
                var scrollObject=Rect("Candidates",picker,new Vector2(0,-430),new Vector2(522,588));view.pickerScroll=scrollObject.gameObject.AddComponent<ScrollRect>();
                var viewport=Rect("Viewport",scrollObject,Vector2.zero,Vector2.zero);Stretch(viewport);viewport.gameObject.AddComponent<RectMask2D>();
                // Transparent authored skin provides a UI raycast surface for empty lists and touch scrolling.
                var viewportImage=viewport.gameObject.AddComponent<Image>();viewportImage.sprite=art.Get("paper");viewportImage.color=new Color(1,1,1,0);viewportImage.raycastTarget=true;
                view.pickerContent=Rect("Content",viewport,Vector2.zero,new Vector2(0,0));view.pickerContent.anchorMin=new Vector2(0,1);view.pickerContent.anchorMax=new Vector2(1,1);view.pickerContent.pivot=new Vector2(.5f,1);
                var layout=view.pickerContent.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=12;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=false;
                var fitter=view.pickerContent.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
                view.pickerScroll.viewport=viewport;view.pickerScroll.content=view.pickerContent;view.pickerScroll.horizontal=false;view.pickerScroll.vertical=true;view.pickerScroll.movementType=ScrollRect.MovementType.Clamped;view.pickerScroll.scrollSensitivity=40;
                view.rowPrefab=CreateRow();
                var menu=Panel("TownMenu",safe,new Vector2(1180,850));view.townMenu=menu.gameObject;
                Label("MenuTitle",menu,"BURNING HILLS",new Vector2(-285,-61),new Vector2(500,60),32,true).color=Gold;
                view.closeTown=IconButton("CloseTownMenu",menu,new Vector2(547,-40),"close");
                for(int i=0;i<4;i++)
                {
                    float x=-440+i*290;Picture("CurrencyIcon_"+i,menu,art.Get(new[]{"money","grinder","cards","residents"}[i]),new Vector2(x-70,-144),new Vector2(48,48));
                    Label("CurrencyCaption_"+i,menu,new[]{"GELD","GRINDER","KARTEN","EINWOHNER"}[i],new Vector2(x+42,-123),new Vector2(172,22),14,true).color=Gold;
                    root.Presentation.currencyValues[i]=Label("CurrencyValue_"+i,menu,"0",new Vector2(x+42,-157),new Vector2(172,39),25,true);
                }
                var quest=map["questTitle"].transform.parent.GetComponent<RectTransform>();quest.SetParent(menu,false);Place(quest,new Vector2(-375,-211),new Vector2(350,390));quest.pivot=new Vector2(.5f,1);
                string[] navigation={"zone.Farm","zone.City","zone.Settlement","zone.Industry","ui.townhall","ui.journal","ui.save","ui.pause"};
                for(int i=0;i<navigation.Length;i++){var rt=map[navigation[i]].GetComponent<RectTransform>();rt.SetParent(menu,false);Place(rt,new Vector2(70+(i%2)*314,-252-(i/2)*78),new Vector2(288,60));}
                view.overview=Button("WorldOverview",menu,"Übersicht",new Vector2(70,-572),new Vector2(288,60),"zoom");
                view.inventory=Button("TeamInventory",menu,"Mein Team",new Vector2(384,-572),new Vector2(288,60),"person");
                var recovery=map["recovery"].GetComponent<RectTransform>();recovery.SetParent(menu,false);Place(recovery,new Vector2(-375,-663),new Vector2(350,51));
                view.menuHint=Label("MenuHint",menu,"",new Vector2(100,-740),new Vector2(990,90),23);
                view.launcher=Button("OpenTownMenu",safe,"Menü",Vector2.zero,new Vector2(150,60),"city");var launch=view.launcher.GetComponent<RectTransform>();launch.anchorMin=launch.anchorMax=new Vector2(1,1);launch.anchoredPosition=new Vector2(-100,-52);
                // Global inventory remains available for viewing and training; assignment belongs to the contextual picker.
                foreach(string id in new[]{"ui.assign","slot","assignmentTarget"})map[id].SetActive(false);
                var message=map["message"].GetComponent<RectTransform>();message.anchorMin=message.anchorMax=new Vector2(.5f,0);message.anchoredPosition=new Vector2(0,48);message.sizeDelta=new Vector2(850,50);message.SetAsLastSibling();
                map["modalShade"].transform.SetAsLastSibling();
                so.Update();so.FindProperty("interaction").objectReferenceValue=view;so.ApplyModifiedPropertiesWithoutUndo();
                view.inspector.SetActive(false);view.picker.SetActive(false);view.townMenu.SetActive(false);view.launcher.gameObject.SetActive(false);
                EditorUtility.SetDirty(root.Presentation);EditorUtility.SetDirty(view);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Docs/InteractionRevision");File.WriteAllText("Docs/InteractionRevision/install.json","{\"utc\":\""+DateTime.UtcNow.ToString("O")+"\",\"installed\":true,\"fixedUiArchived\":true,\"buildingArtChanged\":false}");
            }
            finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        private static InteractionRow CreateRow()
        {
            var existing=AssetDatabase.LoadAssetAtPath<InteractionRow>(RowPath);if(existing!=null)return existing;
            var rt=Rect("InteractionRow",null,Vector2.zero,new Vector2(518,298));var row=rt.gameObject.AddComponent<InteractionRow>();
            var bg=rt.gameObject.AddComponent<Image>();bg.sprite=art.Get("panel");bg.type=Image.Type.Sliced;bg.pixelsPerUnitMultiplier=2;
            var layout=rt.gameObject.AddComponent<LayoutElement>();layout.preferredHeight=298;
            row.portrait=Picture("Portrait",rt,art.Get("portrait_mila"),new Vector2(-185,-111),new Vector2(108,150));
            row.title=Label("WorkerName",rt,"",new Vector2(52,-39),new Vector2(350,46),20,true);
            row.body=Label("WorkerDetails",rt,"",new Vector2(52,-150),new Vector2(350,174),17);
            row.choose=Button("ChooseWorker",rt,"Zuweisen",new Vector2(-37,-264),new Vector2(378,48),"confirm");
            row.remove=IconButton("RemoveDriver",rt,new Vector2(211,-264),null);var x=Mark("RemoveMark",row.remove.transform,ActionMark.Shape.Cross,new Vector2(0,-24),new Vector2(40,40));x.color=new Color(1,.35f,.3f);
            var saved=PrefabUtility.SaveAsPrefabAsset(rt.gameObject,RowPath);Object.DestroyImmediate(rt.gameObject);return saved.GetComponent<InteractionRow>();
        }
        private static void RepairOwnedMarks(GameRoot root)
        {
            bool changed=false;
            var ui=root.Ui.GetComponent<BuildingInteractionUi>();
            foreach(var button in new[]{ui.train,ui.rank})
            {
                var rect=button.GetComponent<RectTransform>();var label=button.GetComponentInChildren<Text>(true);
                if(rect.sizeDelta.y!=56||label.fontSize!=18){rect.sizeDelta=new Vector2(rect.sizeDelta.x,56);label.fontSize=18;changed=true;}
            }
            var viewport=ui.pickerScroll.viewport.GetComponent<Image>();if(viewport.color.a!=0){viewport.color=new Color(1,1,1,0);changed=true;}
            foreach(var label in ui.metricNext){var rect=label.rectTransform;if(rect.sizeDelta.x!=111){rect.anchoredPosition=new Vector2(181,-17);rect.sizeDelta=new Vector2(111,34);changed=true;}}
            foreach(var mark in root.Ui.GetComponentsInChildren<ActionMark>(true))if(mark.GetComponent<CanvasRenderer>()==null){mark.gameObject.AddComponent<CanvasRenderer>();changed=true;}
            var contents=PrefabUtility.LoadPrefabContents(RowPath);
            try
            {
                bool prefabChanged=false;
                foreach(var mark in contents.GetComponentsInChildren<ActionMark>(true))if(mark.GetComponent<CanvasRenderer>()==null){mark.gameObject.AddComponent<CanvasRenderer>();prefabChanged=true;}
                if(prefabChanged)PrefabUtility.SaveAsPrefabAsset(contents,RowPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(contents);}
            if(changed){EditorSceneManager.MarkSceneDirty(root.gameObject.scene);EditorSceneManager.SaveScene(root.gameObject.scene);}
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 point,Vector2 size)
        {var rt=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();if(parent!=null)rt.SetParent(parent,false);Place(rt,point,size);return rt;}
        private static void Place(RectTransform rt,Vector2 point,Vector2 size){rt.anchorMin=rt.anchorMax=new Vector2(.5f,1);rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=point;rt.sizeDelta=size;}
        private static void Stretch(RectTransform rt){rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;}
        private static RectTransform Panel(string name,Transform parent,Vector2 size)
        {var rt=Rect(name,parent,Vector2.zero,size);rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);var bg=rt.gameObject.AddComponent<Image>();bg.sprite=art.Get("panel");bg.type=Image.Type.Sliced;bg.pixelsPerUnitMultiplier=2;return rt;}
        private static void Left(RectTransform rt,float margin){rt.anchorMin=rt.anchorMax=new Vector2(0,.5f);rt.anchoredPosition=new Vector2(margin+rt.sizeDelta.x/2,0);}
        private static void Right(RectTransform rt,float margin){rt.anchorMin=rt.anchorMax=new Vector2(1,.5f);rt.anchoredPosition=new Vector2(-margin-rt.sizeDelta.x/2,0);}
        private static Text Label(string name,Transform parent,string value,Vector2 point,Vector2 size,int fontSize,bool heading=false)
        {var text=Rect(name,parent,point,size).gameObject.AddComponent<Text>();text.font=heading?art.headingFont:art.bodyFont;text.fontSize=fontSize;text.text=value;text.color=Paper;text.raycastTarget=false;text.alignment=TextAnchor.MiddleLeft;text.supportRichText=true;return text;}
        private static Image Picture(string name,Transform parent,Sprite sprite,Vector2 point,Vector2 size)
        {var image=Rect(name,parent,point,size).gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;return image;}
        private static ActionMark Mark(string name,Transform parent,ActionMark.Shape shape,Vector2 point,Vector2 size)
        {var mark=Rect(name,parent,point,size).gameObject.AddComponent<ActionMark>();mark.Mark=shape;mark.raycastTarget=false;return mark;}
        private static Button IconButton(string name,Transform parent,Vector2 point,string icon)
        {return Button(name,parent,"",point,new Vector2(48,48),icon);}
        private static Button Button(string name,Transform parent,string value,Vector2 point,Vector2 size,string icon,bool primary=false)
        {
            var rt=Rect(name,parent,point,size);var image=rt.gameObject.AddComponent<Image>();image.sprite=art.Get(primary?"primary":"button");image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=2;
            var button=rt.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.highlightedColor=new Color(1,1,.84f);colors.pressedColor=new Color(.7f,.8f,.75f);colors.disabledColor=new Color(.46f,.54f,.49f,.8f);button.colors=colors;
            var text=Label("Caption",rt,value,Vector2.zero,Vector2.zero,20,true);Stretch(text.rectTransform);text.rectTransform.offsetMin=new Vector2(icon==null?10:size.x<60?0:47,4);text.rectTransform.offsetMax=new Vector2(-8,-4);text.alignment=TextAnchor.MiddleCenter;text.color=primary?new Color(.055f,.15f,.16f):Paper;
            if(icon!=null){var picture=Picture("Icon",rt,art.Get(icon),new Vector2(size.x<60?0:-size.x/2+27,-size.y/2),new Vector2(30,30));}
            return button;
        }
    }
}
