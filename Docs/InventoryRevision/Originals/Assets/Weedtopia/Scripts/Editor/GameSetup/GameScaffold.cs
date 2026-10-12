using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using Weedtopia.Content;
using Weedtopia.Domain;
using Weedtopia.Unity;
using Weedtopia.Buildings.Presentation;
using Weedtopia.Editor.BuildingArtPipeline;
using Object=UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    public static class GameScaffold
    {
        public const string Root="Assets/Weedtopia/Game",Prefabs=Root+"/Prefabs",Scenes=Root+"/Scenes";
        public const string GameScene=Scenes+"/BurningHills.unity",BootstrapScene=Scenes+"/Bootstrap.unity";
        public const string ContentPath=Root+"/Content/BurningHillsContent.asset",VisualsPath=Root+"/Content/DevelopmentVisuals.asset";
        [Serializable] private sealed class Receipt {public string utc,unity;public int sites,events,missingScripts,errors;public string[] issues;}
        [MenuItem("Weedtopia/Setup/Create or Repair Game Scaffold")]
        public static void CreateOrRepair()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before explicit setup");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save the authored scene before setup; unsaved changes are protected.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(string folder in new[]{Root,Prefabs,Scenes,Root+"/Materials",Root+"/Content"})BuildingIntakeService.EnsureAssetFolder(folder);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var text=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Content/BurningHills.json");
                if(text==null)throw new FileNotFoundException("Authored content JSON missing");
                var content=AssetDatabase.LoadAssetAtPath<GameContent>(ContentPath);
                if(content==null){content=ScriptableObject.CreateInstance<GameContent>();content.EditorConfigure(text);AssetDatabase.CreateAsset(content,ContentPath);}
                else if(content.DefinitionsAsset==null){content.EditorConfigure(text);EditorUtility.SetDirty(content);AssetDatabase.SaveAssetIfDirty(content);}
                var defs=content.Load();var visuals=CreateVisuals(defs);
                var sitePrefab=CreateSitePrefab();var vehiclePrefab=CreateVehiclePrefab();var residentPrefab=CreateResidentPrefab();
                if(!File.Exists(GameScene))CreateWorld(content,visuals,defs,sitePrefab,vehiclePrefab,residentPrefab);
                else RepairReferences();
                if(!File.Exists(BootstrapScene))
                {
                    var bootstrap=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Bootstrap",typeof(BootstrapLoader));EditorSceneManager.SaveScene(bootstrap,BootstrapScene);
                }
                var required=new[]{new EditorBuildSettingsScene(BootstrapScene,true),new EditorBuildSettingsScene(GameScene,true)};
                var current=EditorBuildSettings.scenes;
                var desired=required.Concat(current.Where(x=>x.path!=BootstrapScene&&x.path!=GameScene)).ToArray();
                if(!current.Select(x=>x.path+":"+x.enabled).SequenceEqual(desired.Select(x=>x.path+":"+x.enabled)))EditorBuildSettings.scenes=desired;
                AssetDatabase.SaveAssets();ValidateBatch();
            }
            finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        private static Material Material(string name,Color color)
        {
            string path=Root+"/Materials/"+name+".mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing!=null)return existing;
            var shader=Shader.Find("Universal Render Pipeline/Unlit");if(shader==null)throw new InvalidOperationException("URP Unlit shader unavailable");
            var material=new Material(shader){name=name};material.SetColor("_BaseColor",color);AssetDatabase.CreateAsset(material,path);return material;
        }
        private static GameObject Shape(string name,PrimitiveType type,Transform parent,Vector3 position,Vector3 scale,Material material)
        {
            var obj=GameObject.CreatePrimitive(type);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=scale;
            Object.DestroyImmediate(obj.GetComponent<Collider>());obj.GetComponent<Renderer>().sharedMaterial=material;return obj;
        }
        private static GameObject SaveTemporary(GameObject root,string path)
        {try{return PrefabUtility.SaveAsPrefabAsset(root,path);}finally{Object.DestroyImmediate(root);}}
        private static GameVisualSet CreateVisuals(GameDefinitions defs)
        {
            var existing=AssetDatabase.LoadAssetAtPath<GameVisualSet>(VisualsPath);if(existing!=null)return existing;
            var stone=Material("ReclaimedStone",new Color(.5f,.46f,.4f));var cream=Material("WarmPlaster",new Color(.78f,.72f,.53f));
            var sage=Material("SageRoof",new Color(.23f,.42f,.31f));var copper=Material("Copper",new Color(.67f,.34f,.16f));
            var gold=Material("WorklightGold",new Color(.98f,.72f,.23f));var foliage=Material("Foliage",new Color(.32f,.58f,.34f));
            string ruinPath=Prefabs+"/Development_Ruin.prefab";var ruin=AssetDatabase.LoadAssetAtPath<GameObject>(ruinPath);
            if(ruin==null){var r=new GameObject("Development_Ruin");Shape("BrokenWall",PrimitiveType.Cube,r.transform,new Vector3(-1,1,0),new Vector3(3,2,1),stone);Shape("ReclaimedRoof",PrimitiveType.Cube,r.transform,new Vector3(1,.5f,1),new Vector3(2,1,3),copper);ruin=SaveTemporary(r,ruinPath);}
            var mappings=new List<DevelopmentVisual>();
            foreach(var d in defs.buildings)
            {
                string path=Prefabs+"/Development_"+d.id+".prefab";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(prefab==null)
                {
                    var root=new GameObject("Development_"+d.id);
                    if(d.kind==BuildingKind.Plot) {Shape("PlotMarker",PrimitiveType.Cube,root.transform,new Vector3(0,.1f,0),new Vector3(5,.2f,5),stone);}
                    else if(d.kind==BuildingKind.Grow||d.kind==BuildingKind.Park)
                    {
                        for(int i=0;i<4;i++){Vector3 p=new Vector3(i%2*3-1.5f,.5f,i/2*3-1.5f);Shape("Planter",PrimitiveType.Cylinder,root.transform,p,new Vector3(1.8f,.5f,1.8f),copper);Shape("Canopy",PrimitiveType.Sphere,root.transform,p+Vector3.up*1.6f,new Vector3(2,2.6f,2),foliage);}
                    }
                    else
                    {
                        float height=d.kind==BuildingKind.TownHall?5:d.kind==BuildingKind.House?4:2.5f;
                        Shape("Hall",PrimitiveType.Cube,root.transform,new Vector3(0,height/2,0),new Vector3(5,height,4),cream);
                        Shape("Roof",PrimitiveType.Cube,root.transform,new Vector3(0,height+.2f,0),new Vector3(5.6f,.6f,4.6f),sage);
                        Shape("Door",PrimitiveType.Cube,root.transform,new Vector3(0,.8f,-2.06f),new Vector3(1.2f,1.6f,.1f),copper);
                        Shape("Window",PrimitiveType.Cube,root.transform,new Vector3(-1.7f,1.5f,-2.08f),new Vector3(.9f,1,.1f),gold);
                        if(d.kind==BuildingKind.Processing||d.kind==BuildingKind.Butter||d.kind==BuildingKind.Warehouse)
                            for(int i=0;i<2;i++)Shape("Tank",PrimitiveType.Cylinder,root.transform,new Vector3(3.1f,1.5f,i*2-1),new Vector3(1.8f,1.5f,1.8f),copper);
                    }
                    prefab=SaveTemporary(root,path);
                }
                mappings.Add(new DevelopmentVisual{family=d.id,ruin=d.kind==BuildingKind.Plot?prefab:ruin,active=prefab});
            }
            var set=ScriptableObject.CreateInstance<GameVisualSet>();set.visuals=mappings.ToArray();AssetDatabase.CreateAsset(set,VisualsPath);return set;
        }
        private static GameObject CreateSitePrefab()
        {
            string path=Prefabs+"/BuildingSite.prefab";var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(existing!=null)return existing;
            var root=new GameObject("BuildingSite",typeof(BuildingView),typeof(BoxCollider));var collider=root.GetComponent<BoxCollider>();collider.center=new Vector3(0,2,0);collider.size=new Vector3(8,4,8);
            var anchor=new GameObject("VisualRoot").transform;anchor.SetParent(root.transform,false);
            var ring=Shape("Selection",PrimitiveType.Cube,root.transform,new Vector3(0,.09f,0),new Vector3(8.3f,.12f,8.3f),Material("SelectionGold",new Color(.8f,.6f,.18f)));ring.SetActive(false);
            Shape("PlotFoundation",PrimitiveType.Cube,root.transform,new Vector3(0,.17f,0),new Vector3(7.9f,.15f,7.9f),Material("PlotFoundation",new Color(.18f,.29f,.29f)));
            var lamp=Shape("FlowStatus",PrimitiveType.Sphere,root.transform,new Vector3(-3,1,-3),new Vector3(.5f,.5f,.5f),Material("FlowLamp",Color.green)).GetComponent<Renderer>();
            var labelObject=new GameObject("Label",typeof(TextMesh));labelObject.transform.SetParent(root.transform,false);labelObject.transform.localPosition=new Vector3(0,7,0);labelObject.transform.rotation=Quaternion.Euler(35.264f,45,0);
            var label=labelObject.GetComponent<TextMesh>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=48;label.characterSize=.3f;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=new Color(.96f,.91f,.75f);label.GetComponent<MeshRenderer>().sharedMaterial=label.font.material;
            root.GetComponent<BuildingView>().EditorConfigure("",anchor,ring,label,lamp);return SaveTemporary(root,path);
        }
        private static GameObject CreateVehiclePrefab()
        {
            var production=AssetDatabase.LoadAssetAtPath<GameObject>(ProductionArtImporter.Root+"/Prefabs/DeliveryTruck.prefab");if(production!=null)return production;
            string path=Prefabs+"/DeliveryCart.prefab";var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(existing!=null)return existing;
            var root=new GameObject("DeliveryCart",typeof(DeliveryView));var body=Shape("Body",PrimitiveType.Cube,root.transform,Vector3.zero,new Vector3(1.2f,.9f,2),Material("DeliveryGold",new Color(.95f,.6f,.18f)));root.GetComponent<DeliveryView>().EditorConfigure(body);return SaveTemporary(root,path);
        }
        private static GameObject CreateResidentPrefab()
        {
            var production=AssetDatabase.LoadAssetAtPath<GameObject>(ProductionArtImporter.Root+"/Prefabs/Character_rocko.prefab");if(production!=null)return production;
            string path=Prefabs+"/Resident.prefab";var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(existing!=null)return existing;
            var root=new GameObject("Resident");Shape("Coat",PrimitiveType.Capsule,root.transform,new Vector3(0,1,0),new Vector3(.8f,1,.8f),Material("ResidentCoat",new Color(.22f,.47f,.51f)));Shape("Head",PrimitiveType.Sphere,root.transform,new Vector3(0,2.2f,0),Vector3.one*.7f,Material("ResidentSkin",new Color(.5f,.3f,.19f)));return SaveTemporary(root,path);
        }
        private static void CreateWorld(GameContent content,GameVisualSet visuals,GameDefinitions defs,GameObject sitePrefab,GameObject vehiclePrefab,GameObject residentPrefab)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientLight=new Color(.4f,.5f,.65f);RenderSettings.fog=false;
            var light=new GameObject("BlueHourKey",typeof(Light));light.GetComponent<Light>().type=LightType.Directional;light.GetComponent<Light>().color=new Color(.7f,.81f,1);light.transform.rotation=Quaternion.Euler(40,-30,0);
            var cameraRoot=new GameObject("IsometricCameraRig",typeof(IsometricCameraRig));cameraRoot.transform.position=new Vector3(-20,0,0);
            var cameraObject=new GameObject("MainCamera",typeof(Camera),typeof(AudioListener));cameraObject.tag="MainCamera";cameraObject.transform.SetParent(cameraRoot.transform,false);cameraObject.transform.rotation=Quaternion.Euler(35.264f,45,0);cameraObject.transform.localPosition=-cameraObject.transform.forward*90;
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=28;camera.farClipPlane=300;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.07f,.12f);
            cameraRoot.GetComponent<IsometricCameraRig>().EditorConfigure(camera);
            var ground=new GameObject("BurningHillsWorld");var zones=new Dictionary<Zone,Transform>();
            foreach(Zone zone in Enum.GetValues(typeof(Zone)))
            {
                var zoneRoot=new GameObject(zone.ToString());zoneRoot.transform.SetParent(ground.transform,false);zones.Add(zone,zoneRoot.transform);
                var positions=defs.instances.Where(x=>x.zone==zone).ToArray();float minX=positions.Min(x=>x.x)-7,maxX=positions.Max(x=>x.x)+7,minZ=positions.Min(x=>x.z)-8,maxZ=positions.Max(x=>x.z)+8;
                Shape("DistrictGround",PrimitiveType.Cube,zoneRoot.transform,new Vector3((minX+maxX)/2,-.25f,(minZ+maxZ)/2),new Vector3(maxX-minX,.4f,maxZ-minZ),Material("Ground_"+zone,new Color(.105f+(int)zone*.012f,.2f+(int)zone*.009f,.18f+(int)zone*.02f)));
            }
            var sites=new List<BuildingView>();
            foreach(var instance in defs.instances)
            {
                var obj=(GameObject)PrefabUtility.InstantiatePrefab(sitePrefab,zones[instance.zone]);obj.name=instance.id;obj.transform.localPosition=new Vector3(instance.x,0,instance.z);
                var view=obj.GetComponent<BuildingView>();view.EditorSetId(instance.id);PrefabUtility.RecordPrefabInstancePropertyModifications(view);sites.Add(view);
            }
            foreach(var route in defs.routes)
            {
                var from=defs.instances.Single(x=>x.id==route.from);var to=defs.instances.Single(x=>x.id==route.to);var a=new Vector3(from.x,.04f,from.z);var b=new Vector3(to.x,.04f,to.z);
                var path=Shape(route.id,PrimitiveType.Cube,ground.transform,(a+b)*.5f,new Vector3(route.vehicle?2:1,.08f,Vector3.Distance(a,b)),Material(route.vehicle?"Road":"Conveyor",route.vehicle?new Color(.27f,.29f,.27f):new Color(.5f,.39f,.2f)));path.transform.rotation=Quaternion.LookRotation(b-a);
            }
            foreach(string id in new[]{"farm_grow_01","farm_processing","farm_warehouse","town_hall"})
            {
                var resident=(GameObject)PrefabUtility.InstantiatePrefab(residentPrefab,ground.transform);resident.name=id=="town_hall"?"Rocko":"Resident_"+id;resident.transform.position=sites.Single(x=>x.InstanceId==id).transform.position+new Vector3(-4.5f,0,-3.5f);
            }
            var pool=new GameObject("DeliveryVisualPool");pool.transform.SetParent(ground.transform,false);var vehicles=new List<DeliveryView>();
            for(int i=0;i<48;i++){var obj=(GameObject)PrefabUtility.InstantiatePrefab(vehiclePrefab,pool.transform);obj.name="Cart_"+i;vehicles.Add(obj.GetComponent<DeliveryView>());}
            var ui=new UiAuthoring(defs).Create();
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));var module=events.GetComponent<InputSystemUIInputModule>();
            module.actionsAsset=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            module.AssignDefaultActions();
            var root=new GameObject("GameRoot",typeof(GameRoot));root.GetComponent<GameRoot>().EditorConfigure(content,visuals,AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(BuildingManifestReader.CatalogPath),ui,cameraRoot.GetComponent<IsometricCameraRig>(),sites.ToArray(),vehicles.ToArray());
            EnsureFeedback(root.GetComponent<GameRoot>());
            EditorSceneManager.SaveScene(scene,GameScene);
        }
        private static bool EnsureFeedback(GameRoot root)
        {
            var serialized=new SerializedObject(root);
            if(serialized.FindProperty("feedback").objectReferenceValue!=null)return false;
            var feedback=root.GetComponent<GameFeedback>();if(feedback==null)feedback=root.gameObject.AddComponent<GameFeedback>();
            var audio=root.GetComponent<AudioSource>();if(audio==null){audio=root.gameObject.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=0;audio.volume=.25f;}
            var confirm=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Weedtopia/Game/Audio/Approved/BurningHills/confirm.wav");var unavailable=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Weedtopia/Game/Audio/Approved/BurningHills/unavailable.wav");
            feedback.EditorConfigure(audio,confirm!=null?confirm:AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/development_accept.wav"),unavailable!=null?unavailable:AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/development_blocked.wav"));
            serialized.FindProperty("feedback").objectReferenceValue=feedback;serialized.ApplyModifiedPropertiesWithoutUndo();return true;
        }
        private static void RepairReferences()
        {
            var scene=EditorSceneManager.OpenScene(GameScene,OpenSceneMode.Single);var root=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<GameRoot>(true)).SingleOrDefault();
            if(root==null)throw new InvalidOperationException("Existing scene has no GameRoot. It was preserved for explicit review.");
            var serialized=new SerializedObject(root);bool changed=false;
            void Fill(string property,Object value){var p=serialized.FindProperty(property);if(p.objectReferenceValue==null&&value!=null){p.objectReferenceValue=value;changed=true;}}
            Fill("content",AssetDatabase.LoadAssetAtPath<GameContent>(ContentPath));if(!serialized.FindProperty("productionVisualsApproved").boolValue)Fill("developmentVisuals",AssetDatabase.LoadAssetAtPath<GameVisualSet>(VisualsPath));Fill("productionCatalog",AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(BuildingManifestReader.CatalogPath));
            Fill("ui",Object.FindFirstObjectByType<GameUi>(FindObjectsInactive.Include));Fill("cameraRig",Object.FindFirstObjectByType<IsometricCameraRig>(FindObjectsInactive.Include));
            if(changed)serialized.ApplyModifiedPropertiesWithoutUndo();
            changed=EnsureFeedback(root)||changed;
            if(changed){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
        }
        [MenuItem("Weedtopia/Setup/Validate Game Assets")]
        public static void ValidateBatch()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup();var issues=new List<string>();int missing=0,sites=0,events=0;
            try
            {
                var scene=EditorSceneManager.OpenScene(GameScene,OpenSceneMode.Single);var objects=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();
                missing=objects.Sum(x=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(x.gameObject));if(missing>0)issues.Add("Missing scripts: "+missing);
                sites=objects.Count(x=>x.GetComponent<BuildingView>()!=null);events=objects.Count(x=>x.GetComponent<EventSystem>()!=null);if(events!=1)issues.Add("Expected one EventSystem");
                var content=AssetDatabase.LoadAssetAtPath<GameContent>(ContentPath);var d=content.Load();if(sites!=d.instances.Length)issues.Add("Site count mismatch");
                var views=objects.Select(x=>x.GetComponent<BuildingView>()).Where(x=>x!=null).ToArray();if(views.Select(x=>x.InstanceId).Distinct().Count()!=sites)issues.Add("Duplicate site IDs");
                foreach(var id in d.instances.Select(x=>x.id))if(!views.Any(x=>x.InstanceId==id))issues.Add("Missing site "+id);
                var game=objects.Select(x=>x.GetComponent<GameRoot>()).Single(x=>x!=null);var so=new SerializedObject(game);
                foreach(string key in new[]{"content","productionCatalog","ui","cameraRig","feedback"})if(so.FindProperty(key).objectReferenceValue==null)issues.Add("Missing root reference "+key);
                bool approved=so.FindProperty("productionVisualsApproved").boolValue;
                if(!approved&&so.FindProperty("developmentVisuals").objectReferenceValue==null)issues.Add("Missing development provider before production approval");
                if(approved&&!ProductionBuildingIntegration.HasApproval())issues.Add("Production visual gate was enabled without recorded approval");
                foreach(string path in new[]{BootstrapScene,GameScene})if(!EditorBuildSettings.scenes.Any(x=>x.path==path&&x.enabled))issues.Add("Missing build scene "+path);
                var art=BuildingIntakeValidator.Validate(BuildingManifestReader.Load(),false);if(art.errors>0)issues.Add("Art contract errors: "+art.errors);
            }
            finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
            Directory.CreateDirectory("Docs/Implementation/Validation");
            File.WriteAllText("Docs/Implementation/Validation/game_asset_validation.json",JsonUtility.ToJson(new Receipt{utc=DateTime.UtcNow.ToString("O"),unity=UnityEngine.Application.unityVersion,sites=sites,events=events,missingScripts=missing,errors=issues.Count,issues=issues.ToArray()},true));
            if(issues.Count>0)throw new InvalidOperationException(string.Join("\n",issues));
        }
    }
}
