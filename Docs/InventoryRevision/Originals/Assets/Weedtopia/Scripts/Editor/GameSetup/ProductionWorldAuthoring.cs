using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Weedtopia.Domain;
using Weedtopia.Unity;
using Object=UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    public static class ProductionWorldAuthoring
    {
        private const string Root=ProductionArtImporter.Root;
        private static readonly Quaternion Iso=Quaternion.Euler(35.264f,45,0);
        private static Material Ground(string name,Vector4 region,Color tint)
        {
            string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
            m=new Material(Shader.Find("Weedtopia/AtlasGround")){name=name};m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/Terrain.png"));m.SetVector("_Region",region);m.SetColor("_Tint",tint);AssetDatabase.CreateAsset(m,path);return m;
        }
        private static Mesh Mesh(string name,List<Vector3> vertices,List<int> triangles,List<Vector2> uv,List<Color> colors)
        {
            string path=Root+"/Meshes/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null)return mesh;
            mesh=new Mesh{name=name};if(vertices.Count>65000)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        private static GameObject Surface(string name,Transform parent,Mesh mesh,Material material)
        {
            var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent,false);obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshRenderer>().sharedMaterial=material;return obj;
        }
        private static Mesh Patch(string name,float width,float depth,float tile,float height=0)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();var c=new List<Color>();int nx=Mathf.CeilToInt(width/4),nz=Mathf.CeilToInt(depth/4);
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)
            {float px=(float)x/nx*width-width/2,pz=(float)z/nz*depth-depth/2;v.Add(new Vector3(px,height,pz));uv.Add(new Vector2(px/tile,pz/tile));float shade=.83f+.17f*Mathf.PerlinNoise(px*.025f+20,pz*.025f+20);c.Add(new Color(shade,shade,shade));}
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){int i=z*(nx+1)+x;t.AddRange(new[]{i,i+nx+1,i+1,i+1,i+nx+1,i+nx+2});}
            return Mesh(name,v,t,uv,c);
        }
        private static Mesh Ribbon(string name,Vector3[] points,float width)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();var c=new List<Color>();float distance=0;
            for(int i=0;i<points.Length;i++)
            {
                Vector3 forward=(points[Mathf.Min(points.Length-1,i+1)]-points[Mathf.Max(0,i-1)]).normalized,side=Vector3.Cross(Vector3.up,forward)*width*.5f;
                if(i>0)distance+=Vector3.Distance(points[i-1],points[i]);
                v.Add(points[i]-side);v.Add(points[i]+side);uv.Add(new Vector2(0,distance/3));uv.Add(new Vector2(1,distance/3));c.Add(Color.white);c.Add(Color.white);
                if(i>0){int a=(i-1)*2;t.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
            }
            return Mesh(name,v,t,uv,c);
        }
        private static GameObject Save(GameObject obj,string name)
        {try{return PrefabUtility.SaveAsPrefabAsset(obj,Root+"/Prefabs/"+name+".prefab");}finally{Object.DestroyImmediate(obj);}}
        public static SpriteRenderer Sprite(string name,Transform parent,Sprite sprite,float height,Vector3 position)
        {
            var obj=new GameObject(name,typeof(SpriteRenderer));obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.rotation=Iso;obj.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);
            var sr=obj.GetComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=WorldSort.At(obj.transform.position);return sr;
        }
        private static GameObject PropPrefab(PresentationArt art,string id,float height)
        {
            string name="Prop_"+id;var existing=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+name+".prefab");if(existing!=null)return existing;
            var obj=new GameObject(name);Sprite("Artwork",obj.transform,art.Get(id),height,Vector3.zero);return Save(obj,name);
        }
        private static void Place(GameObject prefab,Transform parent,Vector3 position,float scale=1)
        {
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);obj.transform.localPosition=position;obj.transform.localScale=Vector3.one*scale;
            foreach(var s in obj.GetComponentsInChildren<SpriteRenderer>())s.sortingOrder=WorldSort.At(position);
        }
        public static void Author(GameRoot root,ProductionPresentation presentation,PresentationArt art)
        {
            var defs=AssetDatabase.LoadAssetAtPath<Weedtopia.Content.GameContent>(GameScaffold.ContentPath).Load();
            var world=new GameObject("BurningHills_AuthoredEnvironment",typeof(ProductionAuthoringStamp)).transform;
            var grass=Ground("Meadow",new Vector4(0,.5f,.5f,.5f),new Color(.7f,.78f,.76f));
            var dirt=Ground("ReclaimedPath",new Vector4(.5f,.5f,.5f,.5f),new Color(.79f,.78f,.75f));
            var stone=Ground("TownPaving",new Vector4(0,0,.5f,.5f),new Color(.75f,.81f,.82f));
            var water=Ground("River",new Vector4(.5f,0,.5f,.5f),new Color(.47f,.66f,.77f));
            Surface("ContinuousMeadow",world,Patch("ContinuousMeadow",440,440,18,-.12f),grass);
            // Broad river outside all authored building footprints; no isolated platform edges.
            var river=new Vector3[60];for(int i=0;i<river.Length;i++){float z=-160+i*6;river[i]=new Vector3(-72+Mathf.Sin(z*.025f)*12,-.09f,z);}
            Surface("Riverbank",world,Ribbon("Riverbank",river,13),dirt);var riverWater=river.Select(p=>p+Vector3.up*.02f).ToArray();Surface("River",world,Ribbon("River",riverWater,9),water);
            foreach(var zone in Enum.GetValues(typeof(Zone)).Cast<Zone>())
            {
                var district=new GameObject("District_"+zone).transform;district.SetParent(world,false);
                var items=defs.instances.Where(i=>i.zone==zone).ToArray();
                float minX=items.Min(i=>i.x)-5,maxX=items.Max(i=>i.x)+5,minZ=items.Min(i=>i.z)-5,maxZ=items.Max(i=>i.z)+5;
                var promenade=new[]{new Vector3(minX,.015f,minZ-3),new Vector3((minX+maxX)/2,.015f,minZ-4),new Vector3(maxX,.015f,minZ-3)};
                Surface("Promenade_"+zone,district,Ribbon("Promenade_"+zone,promenade,3.5f),zone==Zone.Farm?dirt:stone);
            }
            var oldWorld=GameObject.Find("BurningHillsWorld");
            if(oldWorld!=null)
            {
                foreach(var tr in oldWorld.GetComponentsInChildren<Transform>(true).ToArray())
                    if(tr!=null&&(tr.name=="DistrictGround"||defs.routes.Any(r=>r.id==tr.name)||tr.name=="Rocko"||tr.name.StartsWith("Resident_")))Object.DestroyImmediate(tr.gameObject);
            }
            var routeList=new List<PresentationRoute>();
            foreach(var r in defs.routes)
            {
                var a=defs.instances.Single(i=>i.id==r.from);var b=defs.instances.Single(i=>i.id==r.to);
                Vector3 start=new Vector3(a.x,.045f,a.z),end=new Vector3(b.x,.045f,b.z);
                // Retain exact simulation endpoints. Curved service roads remain independent of building footprints.
                var points=new Vector3[9];for(int i=0;i<9;i++){float p=i/8f;points[i]=Vector3.Lerp(start,end,p);if(r.vehicle)points[i]+=Vector3.forward*(-2.2f*Mathf.Sin(p*Mathf.PI));}
                var road=Surface("Route_"+r.id,world,Ribbon("Route_"+r.id,points,r.vehicle?2.3f:1.2f),r.vehicle?stone:dirt);
                var route=road.AddComponent<PresentationRoute>();route.routeId=r.id;route.points=points;route.conveyor=!r.vehicle;route.belt=road.GetComponent<Renderer>();routeList.Add(route);
                if(!r.vehicle)
                {
                    // Shared authored short slat mesh gives the conveyor a deliberate mechanical silhouette.
                    for(int i=0;i<8;i++){var p=Vector3.Lerp(start,end,(i+.5f)/8);var slat=Surface("BrassSlat_"+i,road.transform,Patch("ConveyorSlat",1.5f,.16f,1,.13f),stone);slat.transform.position=p;slat.transform.rotation=Quaternion.LookRotation(end-start);}
                }
            }
            presentation.routes=routeList.ToArray();
            var props=new Dictionary<string,GameObject>();
            foreach(var pair in new[]{("pine",9f),("tree",7f),("flowers",1.5f),("planter",2f),("lantern",4f),("crates",1.4f),("salvage",1.4f),("barrels",1.8f),("pump",2f),("sign",3f)})props[pair.Item1]=PropPrefab(art,pair.Item1,pair.Item2);
            var decor=new GameObject("LandscapeAndReclaimedProps").transform;decor.SetParent(world,false);
            var random=new System.Random(420926);
            for(int i=0;i<170;i++)
            {
                var p=new Vector3(-105+(float)random.NextDouble()*220,0,-90+(float)random.NextDouble()*220);
                if(defs.instances.Any(b=>Vector2.Distance(new Vector2(b.x,b.z),new Vector2(p.x,p.z))<11)||defs.routes.Any(r=>DistanceToRoute(p,routeList.Single(x=>x.routeId==r.id).points)<5)||p.x<-58&&p.x>-90)continue;
                string id=i%4==0?"tree":i%3==0?"flowers":"pine";Place(props[id],decor,p,.8f+(float)random.NextDouble()*.45f);
            }
            foreach(var site in root.Sites)
            {
                Vector3 p=site.transform.position;
                Place(props["lantern"],decor,p+new Vector3(-4.5f,0,-3));Place(props["flowers"],decor,p+new Vector3(4.7f,0,3.8f));
                Place(props["planter"],decor,p+new Vector3(4.7f,0,-4.3f));
                Place(props["crates"],decor,p+new Vector3(-3.7f,0,-4));
                var parcel=Surface("Parcel_"+site.InstanceId,world,Patch("ParcelPaving",9.6f,9.6f,5,.025f),dirt);parcel.transform.position=p;
            }
            Place(props["pump"],decor,new Vector3(-31,0,12));Place(props["barrels"],decor,new Vector3(-18,0,6));Place(props["salvage"],decor,new Vector3(52,0,20));Place(props["sign"],decor,new Vector3(-43,0,-12));
            var actors=new List<CharacterActor>();
            string[] ids={"rocko","mila","jaro","nia","city_host","gardener","baker","driver"};
            string[] workplaces={"town_hall","farm_grow_01","farm_processing","farm_warehouse","coffee_01","settlement_01","bakery_01","city_warehouse"};
            for(int i=0;i<ids.Length;i++)
            {
                string name="Character_"+ids[i];var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+name+".prefab");
                if(prefab==null){var obj=new GameObject(name,typeof(CharacterActor));var sprite=Sprite("Artwork",obj.transform,art.Get(ids[i]),3.2f,Vector3.zero);var actor=obj.GetComponent<CharacterActor>();actor.artwork=sprite.transform;actor.spriteRenderer=sprite;actor.speakerId=ids[i];prefab=Save(obj,name);}
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,world);instance.transform.position=root.Site(workplaces[i]).transform.position+new Vector3(-5,0,-3.6f);
                var character=instance.GetComponent<CharacterActor>();character.workplaceId=workplaces[i];PrefabUtility.RecordPrefabInstancePropertyModifications(character);actors.Add(character);
            }
            presentation.characters=actors.ToArray();
            var camera=root.CameraRig.RenderCamera;camera.backgroundColor=new Color(.18f,.26f,.29f);camera.transparencySortMode=TransparencySortMode.CustomAxis;camera.transparencySortAxis=new Vector3(1,0,1);
            var key=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(x=>x.type==LightType.Directional);if(key!=null){key.color=new Color(.83f,.83f,1);key.intensity=.8f;}
            ConfigureSites(art);ConfigureVehicles(root,art);
        }
        private static float DistanceToRoute(Vector3 p,Vector3[] points)
        {
            float result=float.MaxValue;
            for(int i=1;i<points.Length;i++){var delta=points[i]-points[i-1];float t=Mathf.Clamp01(Vector3.Dot(p-points[i-1],delta)/Mathf.Max(.001f,delta.sqrMagnitude));result=Mathf.Min(result,Vector3.Distance(p,points[i-1]+delta*t));}return result;
        }
        private static void ConfigureSites(PresentationArt art)
        {
            string path=GameScaffold.Prefabs+"/BuildingSite.prefab";var prefab=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(string name in new[]{"Selection","PlotFoundation","FlowStatus"}){var old=prefab.transform.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject);}
                var ring=Sprite("Selection",prefab.transform,art.Get("laurel"),9,Vector3.up*.08f);ring.transform.rotation=Quaternion.Euler(90,0,0);ring.color=new Color(1,.83f,.39f,.8f);ring.gameObject.SetActive(false);
                var status=Sprite("FlowStatus",prefab.transform,art.Get("work"),.7f,new Vector3(-3.7f,1,-4));
                var label=prefab.GetComponentInChildren<TextMesh>(true);label.font=art.headingFont;label.fontSize=48;label.characterSize=.2f;label.GetComponent<MeshRenderer>().sharedMaterial=art.headingFont.material;label.transform.localPosition=new Vector3(0,6.2f,0);
                var view=prefab.GetComponent<BuildingView>();var so=new SerializedObject(view);so.FindProperty("selection").objectReferenceValue=ring.gameObject;so.FindProperty("statusLamp").objectReferenceValue=status;
                var fx=new GameObject("UpgradeCelebration",typeof(ParticleSystem));fx.transform.SetParent(prefab.transform,false);fx.transform.localPosition=Vector3.up*2;
                var particles=fx.GetComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=particles.main;main.playOnAwake=false;main.loop=false;main.duration=.65f;main.startLifetime=.8f;main.startSpeed=2;main.startSize=.15f;main.startColor=new Color(1,.75f,.25f);main.maxParticles=24;
                var emission=particles.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,18)});var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.radius=2;shape.angle=25;
                var materialPath=Root+"/Materials/UpgradeSpark.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(material==null){material=new Material(Shader.Find("Sprites/Default"));material.mainTexture=art.Get("star").texture;AssetDatabase.CreateAsset(material,materialPath);}
                particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;var sheet=particles.textureSheetAnimation;sheet.enabled=true;sheet.mode=ParticleSystemAnimationMode.Sprites;sheet.AddSprite(art.Get("star"));sheet.frameOverTime=0;
                so.FindProperty("upgradeEffect").objectReferenceValue=particles;so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(prefab,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
        }
        private static void ConfigureVehicles(GameRoot root,PresentationArt art)
        {
            string name="DeliveryTruck";var obj=new GameObject(name,typeof(DeliveryView));var body=new GameObject("Body");body.transform.SetParent(obj.transform,false);
            var visual=Sprite("TruckArtwork",body.transform,art.Get("truck_left"),2.6f,Vector3.zero);
            var cargo=Sprite("Cargo",body.transform,art.Get("cargo"),.65f,new Vector3(.25f,1.45f,0));
            var so=new SerializedObject(obj.GetComponent<DeliveryView>());so.FindProperty("body").objectReferenceValue=body;so.FindProperty("artwork").objectReferenceValue=visual;so.FindProperty("leftSprite").objectReferenceValue=art.Get("truck_left");so.FindProperty("rightSprite").objectReferenceValue=art.Get("truck_right");so.FindProperty("cargo").objectReferenceValue=cargo;so.ApplyModifiedPropertiesWithoutUndo();
            var prefab=Save(obj,name);var pool=GameObject.Find("DeliveryVisualPool").transform;
            foreach(Transform child in pool.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
            var views=new List<DeliveryView>();for(int i=0;i<48;i++){var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,pool);instance.name="Delivery_"+i;views.Add(instance.GetComponent<DeliveryView>());}
            var game=new SerializedObject(root);var array=game.FindProperty("vehicles");array.arraySize=views.Count;for(int i=0;i<views.Count;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=views[i];game.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
