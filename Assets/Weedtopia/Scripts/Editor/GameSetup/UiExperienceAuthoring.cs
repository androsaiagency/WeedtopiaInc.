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
using Object=UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit migration of existing authored UI. Preserves the world, building references and archived controls.</summary>
    public static class UiExperienceAuthoring
    {
        public const string Folder="Assets/Weedtopia/Production/Prefabs/UiExperience";
        public const string Atlas="Assets/Weedtopia/Production/Art/Dialogue/Expressions-v1.png";
        private static PresentationArt art;
        private static readonly Color Paper=new Color(.98f,.94f,.84f),Gold=new Color(1,.79f,.38f);
        [MenuItem("Weedtopia/Presentation/Install UI Experience")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save manual scene changes first.");
            var setup=EditorSceneManager.GetSceneManagerSetup();Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            try
            {
                var scenes=new[]{GameScaffold.GameScene,"Assets/Weedtopia/Game/Scenes/BigRockCity.unity"}.Where(File.Exists).ToArray();
                foreach(string scenePath in scenes)
                {
                    var scene=EditorSceneManager.OpenScene(scenePath);var root=Object.FindFirstObjectByType<GameRoot>();if(root==null)throw new InvalidOperationException("Missing GameRoot in "+scenePath);
                    art=root.Presentation.art;var ui=root.Ui;
                    if(ui.GetComponent<UiExperience>()!=null)
                    {
                        RepairPortraitImport();var existing=ui.GetComponent<UiExperience>();
                        if(existing.inspectionVisibility==null)
                        {
                            var visibility=ui.Interaction.inspector.GetComponent<CanvasGroup>();if(visibility==null)visibility=ui.Interaction.inspector.AddComponent<CanvasGroup>();
                            var stored=new SerializedObject(existing);stored.FindProperty("inspectionVisibility").objectReferenceValue=visibility;stored.ApplyModifiedPropertiesWithoutUndo();
                            EditorUtility.SetDirty(existing);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                        }
                        var closeLabel=ui.GetComponentsInChildren<Button>(true).First(b=>b.name=="ui.close").GetComponentInChildren<Text>(true);
                        if(!closeLabel.enabled||closeLabel.text!="X"){closeLabel.enabled=true;closeLabel.text="X";EditorUtility.SetDirty(closeLabel);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
                        bool changedScroll=false;
                        foreach(var scroll in new[]{existing.gameScroll,existing.profileScroll,existing.quests.dockScroll,existing.quests.detailScroll})
                        {
                            var background=scroll.GetComponent<Image>();if(background.sprite!=null)continue;
                            background.sprite=art.Get("panel");background.type=Image.Type.Sliced;background.pixelsPerUnitMultiplier=2;EditorUtility.SetDirty(background);changedScroll=true;
                        }
                        if(changedScroll){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
                        Validate(root);continue;
                    }
                    var so=new SerializedObject(ui);var entries=so.FindProperty("bindings");var panels=so.FindProperty("panels");
                    var map=new Dictionary<string,GameObject>();for(int i=0;i<entries.arraySize;i++){var e=entries.GetArrayElementAtIndex(i);map.Add(e.FindPropertyRelative("id").stringValue,(GameObject)e.FindPropertyRelative("target").objectReferenceValue);}
                    void Bind(string id,GameObject target)
                    {
                        int index=-1;for(int i=0;i<entries.arraySize;i++)if(entries.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue==id){index=i;break;}
                        if(index<0)index=entries.arraySize++;
                        var e=entries.GetArrayElementAtIndex(index);e.FindPropertyRelative("id").stringValue=id;e.FindPropertyRelative("target").objectReferenceValue=target;map[id]=target;
                    }
                    void Panel(string id,GameObject target)
                    {
                        var old=map.TryGetValue(id,out var found)?found:null;int index=-1;
                        for(int i=0;i<panels.arraySize;i++)if(panels.GetArrayElementAtIndex(i).objectReferenceValue==old){index=i;break;}
                        if(index<0)index=panels.arraySize++;panels.GetArrayElementAtIndex(index).objectReferenceValue=target;Bind(id,target);
                    }
                    var safe=(RectTransform)so.FindProperty("safeArea").objectReferenceValue;var shade=map["modalShade"].transform;
                    var archive=Rect("ArchivedUiBeforeExperience",ui.transform);archive.gameObject.SetActive(false);
                    EnsureRows();EnsureDialogue(map);
                    var experience=ui.gameObject.AddComponent<UiExperience>();experience.safeArea=safe;
                    experience.inspectionVisibility=ui.Interaction.inspector.GetComponent<CanvasGroup>();if(experience.inspectionVisibility==null)experience.inspectionVisibility=ui.Interaction.inspector.AddComponent<CanvasGroup>();
                    var start=map["start"].GetComponent<RectTransform>();
                    foreach(Transform child in start.Cast<Transform>().ToArray())if(child.gameObject!=map["ui.start"]&&child.gameObject!=map["startTitle"]&&child.gameObject!=map["startSubtitle"]){child.SetParent(archive,false);child.gameObject.SetActive(false);}
                    experience.logo=map["startTitle"].GetComponent<Text>();experience.logo.text="WEEDTOPIA";experience.logo.font=art.headingFont;experience.logo.color=Gold;experience.logo.alignment=TextAnchor.MiddleCenter;
                    experience.logo.gameObject.AddComponent<Shadow>().effectColor=new Color(.01f,.06f,.04f,.9f);experience.logo.GetComponent<Shadow>().effectDistance=new Vector2(3,-4);
                    experience.subtitle=map["startSubtitle"].GetComponent<Text>();experience.subtitle.text="Eine Stadt. Dein Team. Ein neuer Anfang.";experience.subtitle.alignment=TextAnchor.MiddleCenter;
                    experience.emblem=Picture("WeedtopiaEmblem",start,art.Get("raw"));experience.playHint=Label("PlayHint",start,"",30);experience.playHint.alignment=TextAnchor.MiddleCenter;
                    Skin(map["ui.start"].GetComponent<Button>(),"Play",true);
                    foreach(string id in new[]{"intro","conversation"})
                    {
                        var old=map[id];old.transform.SetParent(archive,false);old.name="Archived_"+id;old.SetActive(false);
                        var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/DialogueStage.prefab"),shade);instance.name=id;
                        var view=instance.GetComponent<DialoguePanel>();Panel(id,instance);
                        if(id=="intro")
                        {
                            experience.intro=view;Bind("introText",view.body.gameObject);Bind("introTitle",view.speaker.gameObject);Bind("rockoIdentity",view.speaker.gameObject);Bind("name",view.nameInput.gameObject);
                            Bind("ui.next",view.next.gameObject);Bind("ui.skip",view.skip.gameObject);root.Presentation.introPortrait=view.portrait;
                        }
                        else
                        {
                            experience.conversation=view;view.nameGroup.gameObject.SetActive(false);view.skip.gameObject.SetActive(false);
                            Bind("conversationText",view.body.gameObject);Bind("conversationTitle",view.speaker.gameObject);Bind("conversationPortrait",view.portrait.gameObject);Bind("ui.dialog_continue",view.next.gameObject);
                        }
                        instance.SetActive(false);
                    }
                    var settings=map["settings"].transform;
                    experience.gameTab=Button("GameTab",settings,"Spiel");experience.profileTab=Button("ProfileTab",settings,"Profil");
                    experience.gameScroll=Scroll("GamePage",settings,out var gamePage);experience.gamePage=gamePage;
                    experience.profileScroll=Scroll("ProfilePage",settings,out var profilePage);experience.profilePage=profilePage;
                    foreach(string id in new[]{"volumeLabel","volume","scaleLabel","scale","ui.resume","ui.restart_intro","ui.reset"})map[id].transform.SetParent(gamePage,false);
                    experience.profileName=Object.Instantiate(experience.intro.nameInput,profilePage,false);experience.profileName.name="ProfileName";experience.profileName.text="";
                    experience.avatar=Picture("ProfileAvatar",profilePage,art.Get("portrait_rocko"));experience.avatarCaption=Label("AvatarCaption",profilePage,"Avatar auswählen",26);experience.avatarCaption.alignment=TextAnchor.MiddleCenter;
                    experience.previousAvatar=Button("PreviousAvatar",profilePage,"‹");experience.nextAvatar=Button("NextAvatar",profilePage,"›");
                    experience.applyProfile=Button("ApplyProfile",profilePage,"Profil speichern",true);experience.profileFeedback=Label("ProfileFeedback",profilePage,"",26);experience.profileFeedback.alignment=TextAnchor.MiddleCenter;
                    var restart=NewPanel("newGameConfirm",shade);Panel("newGameConfirm",restart.gameObject);
                    var restartTitle=Label("newGameConfirmTitle",restart,"Neues Spiel beginnen?",36);restartTitle.color=Gold;Bind("newGameConfirmTitle",restartTitle.gameObject);
                    experience.restartBody=Label("RestartBody",restart,"Du beginnst Weedtopia wieder mit Rocko und einem neuen Profil.\n\nDein bisheriger Spielstand wird vor dem Neustart als eigene Sicherungsdatei aufbewahrt.",30);
                    experience.confirmRestart=Button("ConfirmRestart",restart,"Neues Spiel starten",true);experience.cancelRestart=Button("CancelRestart",restart,"Zurück zu Einstellungen");restart.gameObject.SetActive(false);
                    var questPanel=NewPanel("questDetails",shade);Panel("questDetails",questPanel.gameObject);
                    var tracker=ui.gameObject.AddComponent<QuestTracker>();experience.quests=tracker;
                    tracker.heading=Label("QuestHeading",questPanel,"Aufgabe",36);tracker.heading.color=Gold;tracker.note=Label("QuestNote",questPanel,"",26);tracker.nextStep=Label("NextQuestStep",questPanel,"",30);tracker.nextStep.color=Gold;
                    tracker.detailScroll=Scroll("QuestSteps",questPanel,out _);tracker.claim=Button("ClaimQuest",questPanel,"Belohnung abholen",true);tracker.journal=Button("QuestJournal",questPanel,"Alle Aufträge");
                    tracker.dock=NewPanel("ActiveQuestDock",safe);tracker.dockHeading=Label("ActiveQuestHeading",tracker.dock,"AKTIVE AUFGABEN",24);tracker.dockHeading.color=Gold;
                    tracker.dockScroll=Scroll("ActiveQuests",tracker.dock,out _);tracker.rowPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/QuestRow.prefab").GetComponent<Button>();questPanel.gameObject.SetActive(false);
                    var close=map["ui.close"].GetComponent<Button>();Skin(close,"X");close.GetComponentInChildren<Text>().fontSize=48;
                    so.FindProperty("experience").objectReferenceValue=experience;so.ApplyModifiedPropertiesWithoutUndo();
                    // Settings is kept under the modal shade; all fixed navigation stays authored in its existing prefab.
                    tracker.dock.SetSiblingIndex(shade.GetSiblingIndex());shade.SetAsLastSibling();map["ui.close"].transform.SetAsLastSibling();
                    EditorUtility.SetDirty(ui);EditorUtility.SetDirty(root.Presentation);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Validate(root);
                }
                Directory.CreateDirectory("Docs/UiExperienceRevision");File.WriteAllText("Docs/UiExperienceRevision/authoring_receipt.json","{\"utc\":\""+DateTime.UtcNow.ToString("O")+"\",\"scenes\":"+scenes.Length+",\"authored\":true,\"sharedDialoguePrefab\":true,\"portraitPoses\":24,\"profileAvatars\":8}");
            }
            finally{if(!UnityEngine.Application.isBatchMode)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        private static void Validate(GameRoot root)
        {
            var view=root.Ui.GetComponent<UiExperience>();if(view==null||view.intro==null||view.conversation==null||view.quests==null)throw new InvalidOperationException("Missing authored experience reference.");
            if(view.intro.portraits.Length!=8||view.intro.portraits.Any(p=>p.greeting==null||p.explaining==null||p.pleased==null))throw new InvalidOperationException("Missing NPC expression.");
        }
        private static void EnsureRows()
        {
            string file=Folder+"/QuestRow.prefab";if(AssetDatabase.LoadAssetAtPath<GameObject>(file)!=null)return;
            var row=Button("QuestRow",null,"");var rect=(RectTransform)row.transform;rect.sizeDelta=new Vector2(700,96);row.gameObject.AddComponent<LayoutElement>().preferredHeight=96;
            row.GetComponentInChildren<Text>().alignment=TextAnchor.MiddleLeft;PrefabUtility.SaveAsPrefabAsset(row.gameObject,file);Object.DestroyImmediate(row.gameObject);
        }
        private static void EnsureDialogue(Dictionary<string,GameObject> map)
        {
            string file=Folder+"/DialogueStage.prefab";if(AssetDatabase.LoadAssetAtPath<GameObject>(file)!=null)return;
            AssetDatabase.ImportAsset(Atlas,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(Atlas);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.GetSourceTextureWidthAndHeight(out int width,out int height);
            var metadata=new List<SpriteMetaData>();for(int row=0;row<4;row++)for(int col=0;col<4;col++)metadata.Add(new SpriteMetaData{name=DialogueCharacters.Speakers[(row%2)*4+col]+(row<2?"_explaining":"_pleased"),rect=PortraitRect(row,col,width,height),pivot=new Vector2(.5f,0),alignment=(int)SpriteAlignment.BottomCenter});
#pragma warning disable CS0618
            importer.spritesheet=metadata.ToArray();
#pragma warning restore CS0618
            importer.SaveAndReimport();var sprites=AssetDatabase.LoadAllAssetsAtPath(Atlas).OfType<Sprite>().ToDictionary(s=>s.name);
            var stage=Rect("DialogueStage",null);var view=stage.gameObject.AddComponent<DialoguePanel>();view.box=NewPanel("DialogueBox",stage);view.portrait=Picture("NpcPortrait",stage,art.Get("portrait_rocko"));
            view.speaker=Label("Speaker",view.box,"Rocko",34);view.speaker.color=Gold;view.body=Label("Speech",view.box,"",30);
            view.next=Button("ContinueDialogue",view.box,"Weiter",true);view.skip=Button("SkipTutorial",view.box,"Tutorial überspringen");
            view.nameGroup=Rect("TutorialName",view.box);Label("NameCaption",view.nameGroup,"Wie möchtest du heißen?",26).color=Gold;
            view.nameInput=Object.Instantiate(map["name"].GetComponent<InputField>(),view.nameGroup,false);view.nameInput.name="TutorialNameInput";view.nameInput.text="";view.nameInput.gameObject.SetActive(true);
            view.portraits=DialogueCharacters.Speakers.Select(id=>new DialoguePortrait{speaker=id,greeting=art.Get("portrait_"+id),explaining=sprites[id+"_explaining"],pleased=sprites[id+"_pleased"]}).ToArray();
            PrefabUtility.SaveAsPrefabAsset(stage.gameObject,file);Object.DestroyImmediate(stage.gameObject);
        }
        private static void RepairPortraitImport()
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(Atlas);importer.GetSourceTextureWidthAndHeight(out int width,out int height);
#pragma warning disable CS0618
            var entries=importer.spritesheet;bool changed=false;
            for(int i=0;i<entries.Length;i++)
            {
                int row=i/4,col=i%4;var rect=PortraitRect(row,col,width,height);
                if(entries[i].rect!=rect){entries[i].rect=rect;changed=true;}
            }
            if(changed){importer.spritesheet=entries;EditorUtility.SetDirty(importer);importer.SaveAndReimport();}
#pragma warning restore CS0618
        }
        // A guard band excludes adjacent-cell paint fragments while keeping every portrait reference stable.
        private static Rect PortraitRect(int row,int col,int width,int height)=>new Rect((col+.06f)*width/4f,(3-row+.04f)*height/4f,width/4f*.88f,height/4f*.92f);
        private static RectTransform Rect(string name,Transform parent)
        {var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();if(parent!=null)rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);return rect;}
        private static RectTransform NewPanel(string name,Transform parent)
        {var rect=Rect(name,parent);var image=rect.gameObject.AddComponent<Image>();image.sprite=art.Get("panel");image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=2;rect.sizeDelta=new Vector2(1200,960);return rect;}
        private static Text Label(string name,Transform parent,string value,int size)
        {var label=Rect(name,parent).gameObject.AddComponent<Text>();label.font=art.bodyFont;label.fontSize=size;label.text=value;label.color=Paper;label.alignment=TextAnchor.MiddleLeft;label.raycastTarget=false;label.supportRichText=false;return label;}
        private static Image Picture(string name,Transform parent,Sprite sprite)
        {var image=Rect(name,parent).gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;return image;}
        private static Button Button(string name,Transform parent,string value,bool primary=false)
        {var rect=Rect(name,parent);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.gameObject.AddComponent<Image>();Label("Caption",rect,value,30);Skin(button,value,primary);return button;}
        private static void Skin(Button button,string caption,bool primary=false)
        {
            var image=button.GetComponent<Image>();image.sprite=art.Get(primary?"primary":"button");image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=2;image.raycastTarget=true;
            var label=button.GetComponentInChildren<Text>(true);label.enabled=true;label.text=caption;label.alignment=TextAnchor.MiddleCenter;label.font=art.headingFont;label.color=primary?new Color(.045f,.12f,.10f):Paper;
            var rect=label.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(16,8);rect.offsetMax=new Vector2(-16,-8);
            var icon=button.transform.Find("ActionIcon");if(icon!=null)icon.gameObject.SetActive(false);
            var colors=button.colors;colors.disabledColor=new Color(.55f,.63f,.57f);colors.pressedColor=new Color(.6f,.76f,.61f);colors.highlightedColor=new Color(1,1,.85f);button.colors=colors;
        }
        private static ScrollRect Scroll(string name,Transform parent,out RectTransform content)
        {
            var viewport=Rect(name,parent);viewport.gameObject.AddComponent<RectMask2D>();var image=viewport.gameObject.AddComponent<Image>();image.sprite=art.Get("panel");image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=2;image.color=new Color(.01f,.035f,.025f,.25f);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();content=Rect("Content",viewport);content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;
            if(name=="ActiveQuests"||name=="QuestSteps")
            {
                var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=12;layout.padding=new RectOffset(4,4,6,6);layout.childControlHeight=layout.childControlWidth=true;layout.childForceExpandHeight=false;
                content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            }
            scroll.content=content;scroll.viewport=viewport;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=40;return scroll;
        }
    }
}
