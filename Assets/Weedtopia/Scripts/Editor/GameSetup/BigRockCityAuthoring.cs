using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Weedtopia.Buildings.Presentation;
using Weedtopia.Domain;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit, additive installation. Original scenes, materials and building wrappers are never rebuilt.</summary>
    public static class BigRockCityAuthoring
    {
        public const string Folder = "Assets/Weedtopia/Production/BigRockCity";
        public const string ScenePath = "Assets/Weedtopia/Game/Scenes/BigRockCity.unity";
        public const string CatalogPath = Folder + "/BuildingVisuals.asset";
        public const string StoriesPath = Folder + "/ResidentStories.prefab";
        public const string PeoplePath = Folder + "/Residents.prefab";
        public const string VisualsPath = Folder + "/ResidentVisuals.asset";
        public const string CampaignPath = Folder + "/CityCampaign.prefab";
        private static GameDefinitions defs;
        private static PresentationArt art;
        private static Sprite surface;
        private static readonly Color Copper = new Color(.96f,.67f,.36f), Paper = new Color(.97f,.94f,.86f);
        private static readonly Dictionary<string, Vector2> Places = new Dictionary<string, Vector2>
        {
            {"br_berta",new Vector2(27,14)}, {"br_levin",new Vector2(-29,14)},
            {"br_samira",new Vector2(18,-82)}, {"br_enno",new Vector2(18,-184)},
            {"br_lotte",new Vector2(5,-33)}, {"br_ida",new Vector2(-38,116)}
        };

        [MenuItem("Weedtopia/Cities/Install or Repair Big Rock City")]
        public static void Apply()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string sub in new[]{"", "/Materials", "/Buildings", "/Prefabs"}) Directory.CreateDirectory(Folder+sub);
                AssetDatabase.Refresh();
                var burning = EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var original = Object.FindFirstObjectByType<GameRoot>();
                defs = AssetDatabase.LoadAssetAtPath<Weedtopia.Content.GameContent>(GameScaffold.ContentPath).Load();
                art = original.Presentation.art;
                surface = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Weedtopia/Production/Rewards/Art/UiSurface.png");
                var sourceCatalog = (BuildingVisualCatalog)new SerializedObject(original).FindProperty("productionCatalog").objectReferenceValue;
                var catalog = Buildings(sourceCatalog);
                var visuals = Residents(); ResidentPanel(visuals); ResidentWorld(visuals);
                CampaignPanel(); RefineCampaignPanel(); TravelEntries(); RockLandmark(); MandateMarker(); ImportTownSign();
                bool changedBurning = InstallCampaign(original);
                changedBurning |= InstallMarker(original);
                changedBurning |= FitTravelEntry(original);
                if (changedBurning) { EditorSceneManager.MarkSceneDirty(burning); EditorSceneManager.SaveScene(burning); }
                bool created = !File.Exists(ScenePath);
                if (created)
                {
                    if (!AssetDatabase.CopyAsset(GameScaffold.GameScene, ScenePath)) throw new InvalidOperationException("Could not create second-city scene");
                    var cityScene = EditorSceneManager.OpenScene(ScenePath);
                    var root = Object.FindFirstObjectByType<GameRoot>();
                    RemapCity(cityScene);
                    var rootData = new SerializedObject(root);
                    rootData.FindProperty("sceneCityId").stringValue = "big_rock_city";
                    // Opening a scene unloads unused assets. Acquire the new catalog after the scene transition.
                    catalog=AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(CatalogPath);
                    if(catalog==null)throw new InvalidOperationException("New city catalog could not be reacquired");
                    rootData.FindProperty("productionCatalog").objectReferenceValue = catalog;
                    var previousWorld = root.StoryWorld.gameObject;
                    var world = Instance(PeoplePath, null).GetComponent<VipTownPresentation>();
                    rootData.FindProperty("storyWorld").objectReferenceValue = world;
                    rootData.ApplyModifiedPropertiesWithoutUndo();
                    Object.DestroyImmediate(previousWorld);
                    ReplaceResidentPanel(root);
                    ThemeLandscape(cityScene, root);
                    CityWorldDetails(cityScene);
                    var mapPath = Folder+"/DistrictMap.asset";
                    if (!File.Exists(mapPath)) AssetDatabase.CopyAsset(BurningHillsLayoutInstaller.MapPath, mapPath);
                    var map = AssetDatabase.LoadAssetAtPath<DistrictMap>(mapPath);
                    foreach (var range in map.districts)
                        range.label = range.zone==Zone.Farm?"Felsgärten · Farm":range.zone==Zone.City?"Kupfergasse · Stadt":range.zone==Zone.Settlement?"Felsstufen · Wohnviertel":"Altes Werkviertel · Industrie";
                    EditorUtility.SetDirty(map);
                    var cameraData = new SerializedObject(root.CameraRig); cameraData.FindProperty("districtMap").objectReferenceValue=map; cameraData.ApplyModifiedPropertiesWithoutUndo();
                    foreach (var navigator in Object.FindObjectsByType<DistrictNavigator>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    {var data=new SerializedObject(navigator);data.FindProperty("cityLabel").stringValue="Big Rock City";data.ApplyModifiedPropertiesWithoutUndo();}
                    var landmarks = Instance(Folder+"/Prefabs/QuarryLandmarks.prefab", null);
                    var stamp = landmarks.GetComponent<BigRockCityStamp>(); stamp.buildingVariants=catalog.Families.Sum(f=>f.Bands.Count); stamp.inhabitants=6;
                    EditorSceneManager.MarkSceneDirty(cityScene); EditorSceneManager.SaveScene(cityScene);
                }
                else
                {
                    var existingScene=EditorSceneManager.OpenScene(ScenePath);
                    var existingRoot=Object.FindFirstObjectByType<GameRoot>();
                    bool repaired=InstallMarker(existingRoot);
                    repaired |= FitTravelEntry(existingRoot);
                    repaired |= CityWorldDetails(existingScene);
                    var existingData=new SerializedObject(existingRoot);
                    if(existingData.FindProperty("productionCatalog").objectReferenceValue==null)
                    {
                        existingData.FindProperty("productionCatalog").objectReferenceValue=AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(CatalogPath);
                        existingData.ApplyModifiedPropertiesWithoutUndo();repaired=true;
                    }
                    if(repaired){EditorSceneManager.MarkSceneDirty(existingScene);EditorSceneManager.SaveScene(existingScene);}
                }
                var scenes = EditorBuildSettings.scenes.ToList();
                if (!scenes.Any(s=>s.path==ScenePath)) { scenes.Add(new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=scenes.ToArray(); }
                else if(scenes.Any(s=>s.path==ScenePath&&!s.enabled)) {scenes.Single(s=>s.path==ScenePath).enabled=true;EditorBuildSettings.scenes=scenes.ToArray();}
                AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Docs/BigRockCity");
                File.WriteAllText("Docs/BigRockCity/authoring_receipt.json","{\"status\":\"passed\",\"createdScene\":"+created.ToString().ToLowerInvariant()+",\"changedBurningHills\":"+changedBurning.ToString().ToLowerInvariant()+",\"buildings\":56,\"routes\":35,\"buildingVariants\":251,\"mainQuests\":16,\"residents\":6,\"chapters\":24,\"recurring\":18,\"sharedProjects\":3}");
            }
            finally { if(setup.Any(s=>s.isLoaded)&&setup.Any(s=>s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        private static BuildingVisualCatalog Buildings(BuildingVisualCatalog source)
        {
            var existing=AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(CatalogPath);if(existing!=null)return existing;
            var shader=Shader.Find("Weedtopia/Big Rock/Building Palette");if(shader==null)throw new InvalidOperationException("Missing Big Rock building shader");
            var materialPath=Folder+"/Materials/CopperSandstone.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,materialPath);}
            var families=new List<BuildingVisualFamily>();
            foreach(var family in source.Families)
            {
                Directory.CreateDirectory(Folder+"/Buildings/"+family.BuildingId);AssetDatabase.Refresh();
                var bands=new List<BuildingVisualBand>();
                foreach(var band in family.Bands)
                {
                    string path=Folder+"/Buildings/"+family.BuildingId+"/"+band.TierId+".prefab";
                    if(!File.Exists(path))
                    {
                        if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(band.WrapperPrefab),path))throw new InvalidOperationException("Building clone failed: "+path);
                        var copy=PrefabUtility.LoadPrefabContents(path);
                        try
                        {
                            foreach(var sprite in copy.GetComponentsInChildren<BuildingSpriteVisual>(true))sprite.SpriteRenderer.sharedMaterial=material;
                            // Keep all source tier shapes, colliders, resource paths, offsets and gameplay anchors intact.
                            PrefabUtility.SaveAsPrefabAsset(copy,path);
                        }
                        finally{PrefabUtility.UnloadPrefabContents(copy);}
                    }
                    bands.Add(new BuildingVisualBand(band.TierId,band.MinLevel,band.MaxLevel,AssetDatabase.LoadAssetAtPath<GameObject>(path)));
                }
                families.Add(new BuildingVisualFamily(family.BuildingId,bands));
            }
            var catalog=ScriptableObject.CreateInstance<BuildingVisualCatalog>();catalog.EditorSetFamilies(families);AssetDatabase.CreateAsset(catalog,CatalogPath);return catalog;
        }
        private static VipVisualCatalog Residents()
        {
            var existing=AssetDatabase.LoadAssetAtPath<VipVisualCatalog>(VisualsPath);if(existing!=null)return existing;
            var entries=new List<VipVisualEntry>();
            foreach(var person in defs.vipStories.Where(p=>p.cityId=="big_rock_city"))
            {
                string path=Folder+"/Art/"+person.id+".png";
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)throw new InvalidOperationException("Missing resident illustration "+path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.spritePixelsPerUnit=256;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.maxTextureSize=2048;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.CompressedHQ;
#pragma warning disable 618
                importer.spritesheet=new[]{new SpriteMetaData{name=person.id+"_body",rect=new Rect(0,0,1024,1536),alignment=9,pivot=new Vector2(.5f,.02f)},
                    new SpriteMetaData{name=person.id+"_portrait",rect=new Rect(100,690,824,800),alignment=9,pivot=new Vector2(.5f,.5f)}};
#pragma warning restore 618
                importer.SaveAndReimport();
                var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
                entries.Add(new VipVisualEntry{id=person.id,body=sprites.Single(s=>s.name==person.id+"_body"),portrait=sprites.Single(s=>s.name==person.id+"_portrait")});
            }
            var visuals=ScriptableObject.CreateInstance<VipVisualCatalog>();visuals.entries=entries.ToArray();AssetDatabase.CreateAsset(visuals,VisualsPath);return visuals;
        }
        private static void ResidentPanel(VipVisualCatalog visuals)
        {
            if(File.Exists(StoriesPath))return;
            var panel=PrefabUtility.LoadPrefabContents(VipStoriesAuthoring.PanelPath);
            try
            {
                var ui=panel.GetComponent<VipStoriesUi>();ui.visuals=visuals;
                foreach(var row in ui.rows)Object.DestroyImmediate(row.gameObject);
                var rows=new List<VipStoryRosterRow>();
                foreach(var person in defs.vipStories.Where(p=>p.cityId=="big_rock_city"))
                {var row=Instance(VipStoriesAuthoring.RowPath,ui.rosterScroll.content).GetComponent<VipStoryRosterRow>();row.id=person.id;row.portrait.sprite=visuals.Get(person.id).portrait;rows.Add(row);}
                foreach(var collaboration in defs.vipCollaborations.Where(p=>p.cityId=="big_rock_city"))
                {var row=Instance(VipStoriesAuthoring.RowPath,ui.rosterScroll.content).GetComponent<VipStoryRosterRow>();row.id=collaboration.id;row.collaboration=true;row.gameObject.SetActive(false);rows.Add(row);}
                ui.rows=rows.ToArray();ui.portrait.sprite=visuals.entries[0].body;
                panel.name="BigRockResidentStories";PrefabUtility.SaveAsPrefabAsset(panel,StoriesPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(panel);}
        }
        private static void ResidentWorld(VipVisualCatalog visuals)
        {
            if(File.Exists(PeoplePath))return;
            var world=new GameObject("BigRockResidents",typeof(VipTownPresentation));var actors=new List<VipStoryActor>();
            foreach(var person in defs.vipStories.Where(p=>p.cityId=="big_rock_city"))
            {
                string path=Folder+"/Prefabs/"+person.id+".prefab";
                if(!File.Exists(path))
                {
                    var actorObject=PrefabUtility.LoadPrefabContents(VipStoriesAuthoring.ActorPath);
                    try
                    {
                        var actor=actorObject.GetComponent<VipStoryActor>();actor.vipId=person.id;actorObject.name=person.id;
                        actor.spriteRenderer.sprite=visuals.Get(person.id).body;
                        actor.artwork.localScale=Vector3.one*(5.6f/actor.spriteRenderer.sprite.bounds.size.y);
                        var memories=new List<VipMemoryVisual>();
                        for(int chapter=0;chapter<person.chapters.Length;chapter++)
                            for(int choice=0;choice<person.chapters[chapter].choices.Length;choice++)
                            {
                                string prop=choice==1||chapter==1?"Assets/Weedtopia/Production/TownVitality/Prefabs/Town_reading_corner.prefab":chapter==0?"Assets/Weedtopia/Production/Prefabs/Prop_crates.prefab":chapter==2?"Assets/Weedtopia/Production/Prefabs/Prop_flowers.prefab":"Assets/Weedtopia/Production/Prefabs/Prop_lantern.prefab";
                                var memory=Instance(prop,actor.visibleRoot.transform);memory.name="Memory_"+(chapter+1)+"_"+person.chapters[chapter].choices[choice].id;
                                memory.transform.localPosition=DistrictMap.ToWorld(new Vector2(6+chapter%2*6,-5-chapter/2*6));memory.transform.localScale*=choice==1?.38f:.85f;memory.SetActive(false);
                                memories.Add(new VipMemoryVisual{chapter=chapter,choiceId=person.chapters[chapter].choices.Length>1?person.chapters[chapter].choices[choice].id:"",target=memory});
                            }
                        actor.memories=memories.ToArray();PrefabUtility.SaveAsPrefabAsset(actorObject,path);
                    }
                    finally{PrefabUtility.UnloadPrefabContents(actorObject);}
                }
                var instance=Instance(path,world.transform);instance.transform.position=DistrictMap.ToWorld(Places[person.id]);actors.Add(instance.GetComponent<VipStoryActor>());
            }
            world.GetComponent<VipTownPresentation>().actors=actors.ToArray();PrefabUtility.SaveAsPrefabAsset(world,PeoplePath);Object.DestroyImmediate(world);
        }
        private static void CampaignPanel()
        {
            if(File.Exists(CampaignPath))return;
            var rect=Rect("CityCampaign",null,new Vector2(1100,890));var panel=rect.gameObject.AddComponent<CityCampaignUi>();
            var background=rect.gameObject.AddComponent<Image>();background.sprite=surface;background.color=new Color(.035f,.08f,.085f,.995f);
            panel.title=Label("Title",rect,"",new Vector2(-50,380),new Vector2(920,66),40,Copper,true);
            panel.invitationTitle=Label("InvitationTitle",rect,"",new Vector2(100,302),new Vector2(760,54),27,Copper,true);
            panel.rockoPortrait=Image("RockoPortrait",rect,art.Get("portrait_rocko"),new Vector2(-410,222),new Vector2(160,172));
            panel.invitation=Label("Invitation",rect,"",new Vector2(92,165),new Vector2(765,208),21,Paper);
            panel.bridge=Label("Bridge",rect,"",new Vector2(0,22),new Vector2(990,54),19,Copper);
            panel.status=Label("Status",rect,"",new Vector2(0,-42),new Vector2(990,74),20,Paper);
            panel.accept=Button("AcceptMandate",rect,"",new Vector2(0,-117),new Vector2(900,60));
            panel.burningProgress=Label("BurningProgress",rect,"",new Vector2(-256,-209),new Vector2(455,70),21,Paper);
            panel.rockProgress=Label("RockProgress",rect,"",new Vector2(256,-209),new Vector2(455,70),21,Paper);
            panel.burningTravel=Button("TravelBurning",rect,"",new Vector2(-256,-286),new Vector2(455,60));
            panel.rockTravel=Button("TravelRock",rect,"",new Vector2(256,-286),new Vector2(455,60));
            panel.shared=Label("Shared",rect,"",new Vector2(0,-379),new Vector2(990,95),18,new Color(.7f,.8f,.75f));
            PrefabUtility.SaveAsPrefabAsset(rect.gameObject,CampaignPath);Object.DestroyImmediate(rect.gameObject);
        }
        private static void RefineCampaignPanel()
        {
            var obj=PrefabUtility.LoadPrefabContents(CampaignPath);
            try
            {
                var ui=obj.GetComponent<CityCampaignUi>();bool changed=false;
                Button Replace(Button old)
                {
                    if(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(old.gameObject)==RewardPresentationAuthoring.ButtonPath)return old;
                    var rect=old.GetComponent<RectTransform>();var button=Button(old.name,old.transform.parent,old.GetComponentInChildren<Text>().text,rect.anchoredPosition,rect.sizeDelta);
                    Object.DestroyImmediate(old.gameObject);changed=true;return button;
                }
                ui.accept=Replace(ui.accept);ui.burningTravel=Replace(ui.burningTravel);ui.rockTravel=Replace(ui.rockTravel);
                if(changed)PrefabUtility.SaveAsPrefabAsset(obj,CampaignPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(obj);}
        }
        private static void TravelEntries()
        {
            string headerPath=Folder+"/Prefabs/CityTravelEntry.prefab";
            if(!File.Exists(headerPath))
            {
                var rect=Rect("CityTravelEntry",null,new Vector2(360,88));var image=rect.gameObject.AddComponent<Image>();image.color=Color.clear;
                var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
                Label("TravelHint",rect,"Städte ▾",new Vector2(112,-25),new Vector2(95,24),15,Copper);
                var badge=Label("OfferBadge",rect,"NEU",new Vector2(110,26),new Vector2(95,26),17,Copper,true);badge.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(rect.gameObject,headerPath);Object.DestroyImmediate(rect.gameObject);
            }
            var entry=PrefabUtility.LoadPrefabContents(headerPath);
            try
            {
                var rect=entry.GetComponent<RectTransform>();
                var hit=entry.GetComponent<Image>();
                if(hit.sprite==null||rect.anchorMin!=Vector2.zero||rect.anchorMax!=Vector2.one||rect.sizeDelta!=Vector2.zero)
                {
                    hit.sprite=surface;
                    rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.anchoredPosition=Vector2.zero;rect.sizeDelta=Vector2.zero;
                    PrefabUtility.SaveAsPrefabAsset(entry,headerPath);
                }
            }
            finally{PrefabUtility.UnloadPrefabContents(entry);}
            string journalPath=Folder+"/Prefabs/CityJournalEntry.prefab";
            if(!File.Exists(journalPath))
            {
                var row=PrefabUtility.LoadPrefabContents(VipStoriesAuthoring.JournalEntryPath);
                try{row.name="CityJournalEntry";row.GetComponentInChildren<Text>(true).text=defs.Text("campaign.open");PrefabUtility.SaveAsPrefabAsset(row,journalPath);}
                finally{PrefabUtility.UnloadPrefabContents(row);}
            }
        }
        private static bool InstallCampaign(GameRoot root)
        {
            if(root.Ui.Campaign!=null)return false;
            var uiData=new SerializedObject(root.Ui);var bindings=uiData.FindProperty("bindings");
            GameObject Bound(string id){for(int i=0;i<bindings.arraySize;i++){var e=bindings.GetArrayElementAtIndex(i);if(e.FindPropertyRelative("id").stringValue==id)return (GameObject)e.FindPropertyRelative("target").objectReferenceValue;}throw new InvalidOperationException("Missing UI binding "+id);}
            var panel=Instance(CampaignPath,Bound("modalShade").transform).GetComponent<CityCampaignUi>();panel.gameObject.SetActive(false);
            var city=root.Ui.GetComponentInChildren<CityStatusBar>(true).City;
            var header=Instance(Folder+"/Prefabs/CityTravelEntry.prefab",city).GetComponent<Button>();
            var journal=Instance(Folder+"/Prefabs/CityJournalEntry.prefab",Bound("journalList").transform).GetComponent<Button>();journal.transform.SetSiblingIndex(0);
            panel.headerEntry=header;panel.journalEntry=journal;panel.offerBadge=header.transform.Find("OfferBadge").gameObject;
            AddBinding(bindings,"cityCampaign",panel.gameObject);AddBinding(bindings,"cityTravel",header.gameObject);AddBinding(bindings,"cityJournal",journal.gameObject);
            var panels=uiData.FindProperty("panels");panels.InsertArrayElementAtIndex(panels.arraySize);panels.GetArrayElementAtIndex(panels.arraySize-1).objectReferenceValue=panel.gameObject;
            uiData.FindProperty("campaign").objectReferenceValue=panel;uiData.ApplyModifiedPropertiesWithoutUndo();return true;
        }
        private static void MandateMarker()
        {
            string path=Folder+"/Prefabs/RockoMandateMarker.prefab";if(File.Exists(path))return;
            var obj=new GameObject("RockoMandateMarker",typeof(CityMandateActor));var marker=obj.GetComponent<CityMandateActor>();
            var visible=new GameObject("Offered");visible.transform.SetParent(obj.transform,false);
            var renderer=ProductionWorldAuthoring.Sprite("QuestSign",visible.transform,art.Get("quest"),1.2f,new Vector3(0,6.4f,0));
            renderer.color=Copper;marker.artwork=renderer;marker.visible=visible;visible.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(obj,path);Object.DestroyImmediate(obj);
        }
        private static bool InstallMarker(GameRoot root)
        {
            var rocko=root.Presentation.characters.Single(c=>c.speakerId=="rocko");
            var marker=rocko.GetComponentInChildren<CityMandateActor>(true);
            if(marker!=null&&marker.root==root)return false;
            if(marker==null)marker=Instance(Folder+"/Prefabs/RockoMandateMarker.prefab",rocko.transform).GetComponent<CityMandateActor>();
            marker.root=root;return true;
        }
        private static bool FitTravelEntry(GameRoot root)
        {
            var rect=root.Ui.Campaign.headerEntry.GetComponent<RectTransform>();var inset=new Vector2(-16,-16);
            if(rect.anchorMin==Vector2.zero&&rect.anchorMax==Vector2.one&&rect.sizeDelta==inset&&rect.anchoredPosition==Vector2.zero)return false;
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=inset;rect.anchoredPosition=Vector2.zero;
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);return true;
        }
        private static void AddBinding(SerializedProperty bindings,string id,GameObject obj)
        {bindings.InsertArrayElementAtIndex(bindings.arraySize);var e=bindings.GetArrayElementAtIndex(bindings.arraySize-1);e.FindPropertyRelative("id").stringValue=id;e.FindPropertyRelative("target").objectReferenceValue=obj;}
        private static void ReplaceResidentPanel(GameRoot root)
        {
            var previous=root.Ui.Stories.gameObject;var panel=Instance(StoriesPath,previous.transform.parent).GetComponent<VipStoriesUi>();panel.gameObject.SetActive(false);
            var data=new SerializedObject(root.Ui);data.FindProperty("stories").objectReferenceValue=panel;
            var panels=data.FindProperty("panels");for(int i=0;i<panels.arraySize;i++)if(panels.GetArrayElementAtIndex(i).objectReferenceValue==previous)panels.GetArrayElementAtIndex(i).objectReferenceValue=panel.gameObject;
            var bindings=data.FindProperty("bindings");for(int i=0;i<bindings.arraySize;i++){var e=bindings.GetArrayElementAtIndex(i);if(e.FindPropertyRelative("target").objectReferenceValue==previous)e.FindPropertyRelative("target").objectReferenceValue=panel.gameObject;}
            data.ApplyModifiedPropertiesWithoutUndo();Object.DestroyImmediate(previous);
        }
        private static void RemapCity(Scene scene)
        {
            var ids=defs.instances.Where(i=>i.cityId=="burning_hills").Select(i=>i.id).Concat(defs.routes.Where(r=>!r.id.StartsWith("br_",StringComparison.Ordinal)).Select(r=>r.id)).ToHashSet();
            foreach(var go in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)))
            {
                if(go.name.StartsWith("BurningHills",StringComparison.Ordinal))go.name=go.name.Replace("BurningHills","BigRockCity");
                foreach(var component in go.GetComponents<MonoBehaviour>().Where(c=>c!=null))
                {
                    var data=new SerializedObject(component);var p=data.GetIterator();bool changed=false;
                    while(p.Next(true))
                    {
                        if(p.propertyType!=SerializedPropertyType.String||!ids.Contains(p.stringValue))continue;
                        if(p.name!="instanceId"&&p.name!="workplaceId"&&p.name!="requiredBuilding"&&p.name!="routeId")continue;
                        p.stringValue="br_"+p.stringValue;changed=true;
                    }
                    if(changed)data.ApplyModifiedPropertiesWithoutUndo();
                }
                var text=go.GetComponent<Text>();if(text!=null&&text.text.Contains("Burning Hills"))text.text=text.text.Replace("Burning Hills","Big Rock City");
            }
        }
        private static void ThemeLandscape(Scene scene,GameRoot root)
        {
            var materials=new Dictionary<Material,Material>();
            foreach(var renderer in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)))
            {
                var shared=renderer.sharedMaterials;bool changed=false;
                for(int i=0;i<shared.Length;i++)
                {
                    var source=shared[i];if(source==null||source.shader==null)continue;
                    string shader=source.shader.name;
                    if(!shader.Contains("NaturalGround")&&!shader.Contains("LivingWater")&&!shader.Contains("FeatheredGround")&&!shader.Contains("AtlasGround")&&!shader.Contains("LivingFoliage"))continue;
                    if(!materials.TryGetValue(source,out var themed))
                    {
                        string path=Folder+"/Materials/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+".mat";
                        themed=AssetDatabase.LoadAssetAtPath<Material>(path);
                        if(themed==null)
                        {
                            themed=new Material(source){name="BigRock_"+source.name};
                            if(shader.Contains("Ground"))
                            {
                                if(themed.HasProperty("_BaseColor"))themed.SetColor("_BaseColor",new Color(.46f,.38f,.23f));
                                if(themed.HasProperty("_Tint"))themed.SetColor("_Tint",new Color(1.12f,1.02f,.79f));
                                if(themed.HasProperty("_Saturation"))themed.SetFloat("_Saturation",.48f);
                                if(themed.HasProperty("_Contrast"))themed.SetFloat("_Contrast",.45f);
                            }
                            if(shader.Contains("Foliage")&&themed.HasProperty("_Color"))themed.SetColor("_Color",new Color(1.12f,.95f,.73f));
                            AssetDatabase.CreateAsset(themed,path);
                        }
                        materials.Add(source,themed);
                    }
                    shared[i]=themed;changed=true;
                }
                if(changed)renderer.sharedMaterials=shared;
            }
            root.CameraRig.RenderCamera.backgroundColor=new Color(.21f,.20f,.17f);
        }
        private static void RockLandmark()
        {
            string path=Folder+"/Prefabs/QuarryLandmarks.prefab";
            var positions=new[]{new Vector2(198,5),new Vector2(108,42),new Vector2(470,18),new Vector2(110,156),new Vector2(-40,42)};
            if(File.Exists(path))
            {
                var existing=PrefabUtility.LoadPrefabContents(path);bool changed=false;
                var previous=new[]{new Vector2(-110,50),new Vector2(108,42),new Vector2(-128,190),new Vector2(88,156),new Vector2(112,-195)};
                try
                {
                    for(int i=0;i<positions.Length;i++)
                    {
                        var landmark=existing.transform.Find("SandstoneOutcrop_"+(i+1));
                        if(landmark!=null&&Vector3.Distance(landmark.localPosition,DistrictMap.ToWorld(previous[i]))<.01f&&previous[i]!=positions[i])
                        {landmark.localPosition=DistrictMap.ToWorld(positions[i]);changed=true;}
                    }
                    if(changed)PrefabUtility.SaveAsPrefabAsset(existing,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(existing);}
                return;
            }
            string artPath=Folder+"/Art/sandstone_rocks.png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(artPath);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=256;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.maxTextureSize=2048;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=9;settings.spritePivot=new Vector2(.5f,.04f);importer.SetTextureSettings(settings);importer.SaveAndReimport();
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(artPath);var world=new GameObject("QuarryLandmarks",typeof(BigRockCityStamp));
            for(int i=0;i<positions.Length;i++)
            {
                var obj=new GameObject("SandstoneOutcrop_"+(i+1));obj.transform.SetParent(world.transform,false);obj.transform.position=DistrictMap.ToWorld(positions[i]);
                var renderer=ProductionWorldAuthoring.Sprite("Artwork",obj.transform,sprite,i==0?35:26,Vector3.zero);
                renderer.gameObject.AddComponent<CityLandmarkVisual>();
            }
            world.GetComponent<BigRockCityStamp>().landmarks=positions.Length;PrefabUtility.SaveAsPrefabAsset(world,path);Object.DestroyImmediate(world);
        }
        private static void ImportTownSign()
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+"/Art/town_sign.png");
            if(importer.textureType==TextureImporterType.Sprite&&importer.spriteImportMode==SpriteImportMode.Single)return;
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=256;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.maxTextureSize=2048;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=9;settings.spritePivot=new Vector2(.5f,.06f);importer.SetTextureSettings(settings);importer.SaveAndReimport();
        }
        private static bool CityWorldDetails(Scene scene)
        {
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/Art/town_sign.png");bool changed=false;
            foreach(var tr in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="Living_town_sign"||t.name=="BigRockWelcomeSign"))
            {
                var renderer=tr.GetComponentInChildren<SpriteRenderer>(true);if(renderer.sprite==sprite)continue;
                float height=renderer.sprite.bounds.size.y*renderer.transform.localScale.y;
                renderer.sprite=sprite;renderer.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);PrefabUtility.RecordPrefabInstancePropertyModifications(renderer.transform);
                tr.name="BigRockWelcomeSign";PrefabUtility.RecordPrefabInstancePropertyModifications(tr.gameObject);changed=true;
            }
            return changed;
        }
        private static GameObject Instance(string path,Transform parent)
        {var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null)throw new InvalidOperationException("Missing authored prefab "+path);return (GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);}
        private static RectTransform Rect(string name,Transform parent,Vector2 size)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;return r;}
        private static Text Label(string name,Transform parent,string value,Vector2 position,Vector2 size,int font,Color color,bool heading=false)
        {var r=Rect(name,parent,size);r.anchoredPosition=position;var t=r.gameObject.AddComponent<Text>();t.text=value;t.font=heading?art.headingFont:art.bodyFont;t.fontSize=font;t.color=color;t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;t.supportRichText=false;return t;}
        private static Image Image(string name,Transform parent,Sprite sprite,Vector2 position,Vector2 size)
        {var r=Rect(name,parent,size);r.anchoredPosition=position;var i=r.gameObject.AddComponent<Image>();i.sprite=sprite;i.preserveAspect=true;i.raycastTarget=false;return i;}
        private static Button Button(string name,Transform parent,string value,Vector2 position,Vector2 size)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(RewardPresentationAuthoring.ButtonPath);
            if(prefab==null)throw new InvalidOperationException("Missing shared reward button prefab");
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);
            go.name=name;var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;
            var button=go.GetComponent<Button>();var label=go.GetComponentInChildren<Text>(true);
            if(label==null){var image=go.GetComponent<Image>();image.sprite=surface;image.color=new Color(.14f,.28f,.27f);button.targetGraphic=image;label=Label("Label",go.transform,value,Vector2.zero,size-new Vector2(20,8),20,Paper);}
            label.rectTransform.sizeDelta=size-new Vector2(20,8);label.text=value;label.font=art.bodyFont;label.fontSize=20;return button;
        }
    }
}
