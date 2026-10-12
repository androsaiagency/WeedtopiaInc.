using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Weedtopia.Content;
using Weedtopia.Domain;
using Weedtopia.Editor.BuildingArtPipeline;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>One explicit, idempotent layout migration. Original scenes/assets are backed up before invocation.</summary>
    public static class BurningHillsLayoutInstaller
    {
        public const string Folder = "Assets/Weedtopia/Production/Layout";
        public const string MapPath = Folder + "/DistrictMap.asset";
        [Serializable] public sealed class SitePlan { public string id; public float u,v; public Vector2 Point => new Vector2(u,v); }
        [Serializable] public sealed class RoutePlan { public string id; public Vector2[] points; public float length; public int travelSeconds; }
        [Serializable] public sealed class LayoutPlan { public int revision; public string source; public SitePlan[] sites; public RoutePlan[] routes; public DistrictRange[] districts; }
        [Serializable] private sealed class Receipt { public string utc,source; public int revision,sites,routes; public bool originalIdsPreserved,explicitBuildingApproval,releaseAccepted; }
        private static readonly Quaternion Iso=Quaternion.Euler(35.264f,45,0);
        private static readonly Color Paper=new Color(.96f,.91f,.77f);

        public static void RequireSafeEditor()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("User Play Mode is protected.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save manual scene edits first: "+SceneManager.GetSceneAt(i).path);
        }
        [MenuItem("Weedtopia/Production/Apply Approved District Layout")]
        public static void Apply()
        {
            RequireSafeEditor();
            var plan=JsonUtility.FromJson<LayoutPlan>(File.ReadAllText(Folder+"/BurningHillsLayout.json"));
            var defs=AssetDatabase.LoadAssetAtPath<GameContent>(GameScaffold.ContentPath).Load();
            if(defs.instances.Length!=plan.sites.Length||plan.routes.Length!=defs.routes.Length)throw new InvalidOperationException("Content/layout coverage mismatch");
            BuildingIntakeService.EnsureAssetFolder(Folder+"/Meshes");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(GameScaffold.GameScene);
                if(Object.FindFirstObjectByType<DistrictLayoutStamp>()!=null)return;
                var root=Object.FindFirstObjectByType<GameRoot>();var presentation=root.Presentation;var art=presentation.art;
                var oldSites=root.Sites.ToDictionary(x=>x.InstanceId);var oldIds=oldSites.Keys.ToArray();
                var world=new GameObject("BurningHills_Districts",typeof(DistrictLayoutStamp)).transform;
                var districts=new Dictionary<Zone,Transform>();
                foreach(Zone zone in Enum.GetValues(typeof(Zone))) {var t=new GameObject("District_"+zone).transform;t.SetParent(world,false);districts.Add(zone,t);}
                var sitePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(GameScaffold.Prefabs+"/BuildingSite.prefab");var views=new List<BuildingView>();
                foreach(var instance in defs.instances)
                {
                    if(!oldSites.TryGetValue(instance.id,out var view))
                    {
                        var obj=(GameObject)PrefabUtility.InstantiatePrefab(sitePrefab,districts[instance.zone]);view=obj.GetComponent<BuildingView>();view.EditorSetId(instance.id);
                    }
                    view.transform.SetParent(districts[instance.zone],true);view.transform.position=new Vector3(instance.x,0,instance.z);
                    view.name=instance.id;PrefabUtility.RecordPrefabInstancePropertyModifications(view);PrefabUtility.RecordPrefabInstancePropertyModifications(view.transform);views.Add(view);
                }
                var serialized=new SerializedObject(root);var sites=serialized.FindProperty("sites");sites.arraySize=views.Count;
                for(int i=0;i<views.Count;i++)sites.GetArrayElementAtIndex(i).objectReferenceValue=views[i];serialized.ApplyModifiedPropertiesWithoutUndo();
                foreach(var actor in presentation.characters)
                {
                    var point=plan.sites.Single(x=>x.id==actor.workplaceId).Point;
                    actor.transform.SetParent(world,true);actor.transform.position=DistrictMap.ToWorld(point+new Vector2(-11,-13));
                    PrefabUtility.RecordPrefabInstancePropertyModifications(actor.transform);
                }
                // Keep the previous authored composition accessible, but excluded from the active world.
                var previous=GameObject.Find("BurningHills_AuthoredEnvironment");if(previous!=null){previous.name="Archive_PreviousPresentationLayout";previous.SetActive(false);}
                AuthorSurfaces(world,plan);
                AuthorRoutes(world,plan,defs,presentation);
                AuthorProps(world,plan,art);
                var map=AssetDatabase.LoadAssetAtPath<DistrictMap>(MapPath);
                if(map==null){map=ScriptableObject.CreateInstance<DistrictMap>();map.districts=plan.districts;map.overviewZoom=230;AssetDatabase.CreateAsset(map,MapPath);}
                root.CameraRig.EditorSetDistrictMap(map);root.CameraRig.transform.position=DistrictMap.ToWorld(map.Range(Zone.Farm).start);
                var camera=root.CameraRig.RenderCamera;camera.transform.localPosition=-camera.transform.forward*900;camera.farClipPlane=2000;camera.orthographicSize=40;
                AuthorNavigator(root,art);
                presentation.explicitBuildingApproval=ProductionBuildingIntegration.HasApproval();
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                File.WriteAllText("Docs/LayoutRevision/layout_integration.json",JsonUtility.ToJson(new Receipt{utc=DateTime.UtcNow.ToString("O"),source=plan.source,revision=plan.revision,sites=views.Count,routes=presentation.routes.Length,originalIdsPreserved=oldIds.All(x=>views.Any(v=>v.InstanceId==x)),explicitBuildingApproval=presentation.explicitBuildingApproval,releaseAccepted=false},true));
            }
            finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        private static Material Material(string name)=>AssetDatabase.LoadAssetAtPath<Material>(ProductionArtImporter.Root+"/Materials/"+name+".mat");
        private static Mesh SaveMesh(string name,List<Vector3> vertices,List<int> triangles,List<Vector2> uv)
        {
            string path=Folder+"/Meshes/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null)return mesh;
            mesh=new Mesh{name=name};if(vertices.Count>65000)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();
            mesh.SetColors(vertices.Select(p=>{float s=.88f+.12f*Mathf.PerlinNoise(p.x*.02f+40,p.z*.02f+40);return new Color(s,s,s);}).ToList());
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        private static GameObject Surface(string name,Transform parent,Mesh mesh,Material material)
        {
            var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent,false);obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshRenderer>().sharedMaterial=material;return obj;
        }
        private static Mesh Patch(string name,float width,float depth,float height,float tile)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();int nx=Mathf.Max(1,Mathf.CeilToInt(width/12)),nz=Mathf.Max(1,Mathf.CeilToInt(depth/12));
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++){var p=new Vector3(x*width/nx-width/2,height,z*depth/nz-depth/2);v.Add(p);uv.Add(new Vector2(p.x/tile,p.z/tile));}
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){int i=z*(nx+1)+x;t.AddRange(new[]{i,i+nx+1,i+1,i+1,i+nx+1,i+nx+2});}
            return SaveMesh(name,v,t,uv);
        }
        private static Mesh Ribbon(string name,Vector3[] points,float width)
        {
            var v=new List<Vector3>();var t=new List<int>();var uv=new List<Vector2>();float distance=0;
            for(int i=0;i<points.Length;i++)
            {
                var tangent=points[Mathf.Min(points.Length-1,i+1)]-points[Mathf.Max(0,i-1)];var side=Vector3.Cross(Vector3.up,tangent.normalized)*width*.5f;
                if(i>0)distance+=Vector3.Distance(points[i-1],points[i]);v.Add(points[i]-side);v.Add(points[i]+side);uv.Add(new Vector2(0,distance/3));uv.Add(new Vector2(1,distance/3));
                if(i>0){int a=(i-1)*2;t.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
            }
            return SaveMesh(name,v,t,uv);
        }
        private static void AuthorSurfaces(Transform world,LayoutPlan plan)
        {
            Surface("ContinuousCountryside",world,Patch("Countryside",1500,1500,-.12f,28),Material("Meadow"));
            var river=Enumerable.Range(0,81).Select(i=>DistrictMap.ToWorld(new Vector2(174+Mathf.Sin(i*.15f)*8,-500+i*14),-.06f)).ToArray();
            Surface("RiverBank",world,Ribbon("RiverBank",river,16),Material("ReclaimedPath"));
            Surface("River",world,Ribbon("River",river.Select(x=>x+Vector3.up*.02f).ToArray(),10),Material("River"));
            void Road(string id,params Vector2[] points){var path=points.Select(p=>DistrictMap.ToWorld(p,.015f)).ToArray();Surface(id,world,Ribbon(id,path,5),Material("TownPaving"));}
            Road("WestDistrictRoad",new Vector2(-38,-245),new Vector2(-38,380));
            Road("NorthCityRoad",new Vector2(-38,50),new Vector2(108,50));
            Road("EastDistrictRoad",new Vector2(108,-245),new Vector2(108,380));
            for(int row=0;row<6;row++)Road("ResidentialLane_"+row,new Vector2(-38,78+row*52),new Vector2(108,78+row*52));
            foreach(var site in plan.sites)
            {
                var parcel=Surface("Parcel_"+site.id,world,Patch("Parcel",26,23,.025f,8),Material("ReclaimedPath"));
                parcel.transform.position=DistrictMap.ToWorld(site.Point);parcel.transform.rotation=Quaternion.Euler(0,45,0);
            }
        }
        private static void AuthorRoutes(Transform world,LayoutPlan plan,GameDefinitions defs,ProductionPresentation presentation)
        {
            var list=new List<PresentationRoute>();var crates=AssetDatabase.LoadAssetAtPath<GameObject>(ProductionArtImporter.Root+"/Prefabs/Prop_crates.prefab");
            foreach(var r in plan.routes)
            {
                var definition=defs.routes.Single(x=>x.id==r.id);var points=r.points.Select(p=>DistrictMap.ToWorld(p,definition.vehicle?.045f:.12f)).ToArray();
                var road=Surface("Route_"+r.id,world,Ribbon("Route_"+r.id,points,definition.vehicle?4:1.3f),Material(definition.vehicle?"TownPaving":"ConveyorBelt"));
                var route=road.AddComponent<PresentationRoute>();route.routeId=r.id;route.points=points;route.conveyor=!definition.vehicle;route.belt=road.GetComponent<Renderer>();
                if(route.conveyor)
                {
                    route.cargoMarkers=new Transform[3];
                    for(int i=0;i<3;i++){var marker=(GameObject)PrefabUtility.InstantiatePrefab(crates,road.transform);marker.name="PooledCargo_"+i;marker.transform.localScale=Vector3.one*.5f;marker.transform.position=route.Position((i+.5f)/3);route.cargoMarkers[i]=marker.transform;marker.SetActive(false);}
                }
                list.Add(route);
            }
            presentation.routes=list.ToArray();
        }
        private static void Prop(Transform world,string id,Vector2 point,float scale=1)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ProductionArtImporter.Root+"/Prefabs/Prop_"+id+".prefab");
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab,world);obj.transform.position=DistrictMap.ToWorld(point);obj.transform.localScale=Vector3.one*scale;
            foreach(var sprite in obj.GetComponentsInChildren<SpriteRenderer>())sprite.sortingOrder=WorldSort.At(obj.transform.position);
        }
        private static void AuthorProps(Transform world,LayoutPlan plan,PresentationArt art)
        {
            var props=new GameObject("AuthoredDistrictProps").transform;props.SetParent(world,false);
            foreach(var site in plan.sites)
            {
                Prop(props,"lantern",site.Point+new Vector2(12,-13));Prop(props,"planter",site.Point+new Vector2(12,10));
                Prop(props,"crates",site.Point+new Vector2(-10,-10));Prop(props,"flowers",site.Point+new Vector2(-12,11),.8f);
            }
            var random=new System.Random(420923);
            for(int i=0;i<420;i++)
            {
                var p=new Vector2(-280+(float)random.NextDouble()*920,-290+(float)random.NextDouble()*740);
                if(plan.sites.Any(s=>Vector2.Distance(p,s.Point)<28)||Mathf.Abs(p.x-174)<22||Mathf.Abs(p.x+38)<12||Mathf.Abs(p.x-108)<12)continue;
                if(plan.routes.Any(r=>Enumerable.Range(1,r.points.Length-1).Any(k=>Distance(p,r.points[k-1],r.points[k])<10)))continue;
                if(p.y>50&&p.x>-40&&p.x<110)continue;
                Prop(props,i%4==0?"tree":i%3==0?"flowers":"pine",p,.8f+(float)random.NextDouble()*.6f);
            }
            Prop(props,"sign",new Vector2(225,-89));Prop(props,"pump",new Vector2(271,-12));Prop(props,"salvage",new Vector2(-91,125));
        }
        private static float Distance(Vector2 p,Vector2 a,Vector2 b){var d=b-a;return Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.01f,d.sqrMagnitude)));}
        private static RectTransform Rect(string name,Transform parent,Vector2 size)
        {
            var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);var rt=obj.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(.5f,0);rt.sizeDelta=size;return rt;
        }
        private static Button Button(string name,Transform parent,PresentationArt art,string icon,Vector2 size,Vector2 position)
        {
            var rt=Rect(name,parent,size);rt.anchoredPosition=position;var image=rt.gameObject.AddComponent<Image>();image.sprite=art.Get("button");image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=2;
            var button=rt.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var ir=Rect("Arrow",rt,new Vector2(30,30));ir.anchorMin=ir.anchorMax=new Vector2(.5f,.5f);var graphic=ir.gameObject.AddComponent<Image>();graphic.sprite=art.Get(icon);graphic.preserveAspect=true;graphic.raycastTarget=false;
            return button;
        }
        private static void AuthorNavigator(GameRoot root,PresentationArt art)
        {
            var panel=Rect("DistrictNavigation",root.Ui.transform,new Vector2(620,96));panel.anchoredPosition=new Vector2(0,170);
            var background=panel.gameObject.AddComponent<Image>();background.sprite=art.Get("panel");background.type=Image.Type.Sliced;background.pixelsPerUnitMultiplier=3;background.raycastTarget=false;
            var navigator=panel.gameObject.AddComponent<DistrictNavigator>();navigator.rig=root.CameraRig;
            var caption=Rect("DistrictCaption",panel,new Vector2(560,28));caption.anchoredPosition=new Vector2(0,71);var text=caption.gameObject.AddComponent<Text>();text.font=art.headingFont;text.fontSize=18;text.alignment=TextAnchor.MiddleCenter;text.color=Paper;text.raycastTarget=false;navigator.caption=text;
            navigator.previous=Button("Previous",panel,art,"down",new Vector2(58,42),new Vector2(-265,28));navigator.next=Button("Next",panel,art,"down",new Vector2(58,42),new Vector2(265,28));
            var track=Rect("DistrictPosition",panel,new Vector2(300,38));track.anchoredPosition=new Vector2(-58,28);var bg=track.gameObject.AddComponent<Image>();bg.sprite=art.Get("button");bg.type=Image.Type.Sliced;
            var scrollbar=track.gameObject.AddComponent<Scrollbar>();var handle=Rect("Handle",track,new Vector2(42,36));handle.anchorMin=Vector2.zero;handle.anchorMax=Vector2.one;var handleImage=handle.gameObject.AddComponent<Image>();handleImage.sprite=art.Get("primary");handleImage.type=Image.Type.Sliced;scrollbar.handleRect=handle;scrollbar.targetGraphic=handleImage;scrollbar.size=.16f;navigator.position=scrollbar;
            navigator.overview=Button("Overview",panel,art,"zoom",new Vector2(130,42),new Vector2(168,28));navigator.overview.transform.Find("Arrow").GetComponent<RectTransform>().anchoredPosition=new Vector2(-42,0);
            var title=Rect("OverviewLabel",navigator.overview.transform,new Vector2(90,32));title.anchorMin=title.anchorMax=new Vector2(.5f,.5f);title.anchoredPosition=new Vector2(12,0);var label=title.gameObject.AddComponent<Text>();label.font=art.headingFont;label.text="Übersicht";label.fontSize=16;label.alignment=TextAnchor.MiddleCenter;label.color=Paper;label.raycastTarget=false;
            panel.SetSiblingIndex(root.Ui.transform.childCount-2);
        }
    }
}
