using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Unity;
using Weedtopia.Editor.BuildingArtPipeline;
using Object=UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Deterministic editor authoring. Every plant, path and bank is saved in the scene or a reusable prefab.</summary>
    public static class NaturalEnvironmentAuthoring
    {
        public const string Folder="Assets/Weedtopia/Production/Environment";
        [Serializable] public sealed class ArtManifest {public ArtSlice[] sprites;}
        [Serializable] public sealed class ArtSlice
        {
            public string file,id;public int x,y,width,height;public float worldHeight;public bool foliage;
        }
        private static readonly Quaternion Iso=Quaternion.Euler(35.264f,45,0);
        private static readonly Dictionary<string,GameObject> prefabs=new Dictionary<string,GameObject>();
        private static Material meadow,soil,paving,shade,water;
        private static Transform landscape,gardens,woodland,riverGarden;
        private static BurningHillsLayoutInstaller.LayoutPlan plan;
        private static NaturalEnvironmentStamp stamp;
        private static int meshIndex;
        private static Vector3 W(Vector2 p,float y=0)=>DistrictMap.ToWorld(p,y);

        [MenuItem("Weedtopia/Production/Author Natural Landscape")]
        public static void Apply()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(GameScaffold.GameScene);
                // Once installed this action is harmless; deliberate subsequent art revisions use RebuildOwned.
                if(Object.FindFirstObjectByType<NaturalEnvironmentStamp>()!=null)return;
                Build();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
            finally {if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        public static void RebuildOwned()
        {
            if(!UnityEngine.Application.isBatchMode)throw new InvalidOperationException("Iteration requires an owned editor and the recorded environment baseline.");
            BurningHillsLayoutInstaller.RequireSafeEditor();var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var previous=Object.FindFirstObjectByType<NaturalEnvironmentStamp>();if(previous!=null)Object.DestroyImmediate(previous.gameObject);
                Build();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
            finally {if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        private static void Build()
        {
            foreach(var sub in new[]{"Art","Meshes","Materials","Prefabs"})BuildingIntakeService.EnsureAssetFolder(Folder+"/"+sub);
            ImportArt();meshIndex=0;
            plan=JsonUtility.FromJson<BurningHillsLayoutInstaller.LayoutPlan>(File.ReadAllText(BurningHillsLayoutInstaller.Folder+"/BurningHillsLayout.json"));
            var world=GameObject.Find("BurningHills_Districts").transform;
            // Original surfaces and props stay in the same hierarchy with their original assets and assignments.
            foreach(Transform child in world)
                if(child.name=="ContinuousCountryside"||child.name=="RiverBank"||child.name=="River"||child.name.EndsWith("DistrictRoad")||child.name=="NorthCityRoad"||child.name.StartsWith("ResidentialLane_")||child.name.StartsWith("Parcel_")||child.name=="AuthoredDistrictProps"||child.name=="DistrictGardens")child.gameObject.SetActive(false);
            landscape=Group("NaturalLandscape",world);stamp=landscape.gameObject.AddComponent<NaturalEnvironmentStamp>();
            gardens=Group("NeighborhoodGardens",landscape);woodland=Group("CountrysideGroves",landscape);riverGarden=Group("LivingRiverbanks",landscape);
            meadow=Ground("Meadow",new Vector4(0,.5f,.5f,.5f),new Color(.88f,.92f,.86f),.11f);
            soil=Ground("GardenEarth",new Vector4(.5f,.5f,.5f,.5f),new Color(.87f,.9f,.85f),.03f);
            paving=Ground("MossyFlagstones",new Vector4(0,0,.5f,.5f),new Color(.91f,.94f,.9f),.015f);
            shade=Ground("ShadedMeadow",new Vector4(0,.5f,.5f,.5f),new Color(.68f,.84f,.65f),.08f);
            water=Mat("RiverWater","Weedtopia/LivingWater");water.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Art/Ground.png");EditorUtility.SetDirty(water);
            Surface("ContinuousPaintedMeadow",landscape,Rectangle(new Vector2(175,30),1500,1500,-.11f,34),meadow,-32750);
            AuthorRiver();AuthorVillageLanes();AuthorParcels();AuthorRoads();AuthorGardens();AuthorWoodland();
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Docs/EnvironmentRevision");
            File.WriteAllText("Docs/EnvironmentRevision/integration.json",JsonUtility.ToJson(new Receipt{utc=DateTime.UtcNow.ToString("O"),plants=stamp.authoredPlants,gardenProps=stamp.authoredGardenProps,prefabTypes=prefabs.Count,routeCount=plan.routes.Length,siteCount=plan.sites.Length},true));
        }
        [Serializable] private sealed class Receipt {public string utc;public int plants,gardenProps,prefabTypes,routeCount,siteCount;public bool userAcceptance=false;}
        private static Transform Group(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        private static Material Mat(string name,string shader)
        {
            string path=Folder+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find(shader)){name=name};AssetDatabase.CreateAsset(m,path);}return m;
        }
        private static Material Ground(string name,Vector4 region,Color tint,float variation)
        {
            var m=Mat(name,"Weedtopia/NaturalGround");m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Art/Ground.png");m.SetVector("_Region",region);m.SetColor("_Tint",tint);m.SetFloat("_Variation",variation);
            bool grass=name.Contains("Meadow"),earth=name=="GardenEarth";
            m.SetFloat("_Saturation",grass?.7f:earth?.5f:.8f);m.SetFloat("_Contrast",grass?.58f:earth?.92f:1);
            m.SetColor("_BaseColor",grass?new Color(.17f,.31f,.09f):new Color(.46f,.36f,.22f));EditorUtility.SetDirty(m);return m;
        }
        private static void ImportArt()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var art=JsonUtility.FromJson<ArtManifest>(File.ReadAllText(Folder+"/art_manifest.json"));
            foreach(var group in art.sprites.GroupBy(s=>s.file))
            {
                var imp=(TextureImporter)AssetImporter.GetAtPath(Folder+"/Art/"+group.Key);
                imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Multiple;imp.spritePixelsPerUnit=100;
                imp.alphaIsTransparency=true;imp.mipmapEnabled=true;imp.filterMode=FilterMode.Trilinear;imp.wrapMode=TextureWrapMode.Clamp;
                imp.maxTextureSize=2048;imp.textureCompression=TextureImporterCompression.CompressedHQ;imp.isReadable=false;
#pragma warning disable 618
                imp.spritesheet=group.Select(s=>new SpriteMetaData{name=s.id,rect=new Rect(s.x,s.y,s.width,s.height),alignment=9,pivot=new Vector2(.5f,0)}).ToArray();
#pragma warning restore 618
                imp.SaveAndReimport();
            }
            var ground=(TextureImporter)AssetImporter.GetAtPath(Folder+"/Art/Ground.png");ground.textureType=TextureImporterType.Default;ground.mipmapEnabled=true;ground.wrapMode=TextureWrapMode.Clamp;ground.filterMode=FilterMode.Trilinear;ground.anisoLevel=4;ground.maxTextureSize=2048;ground.textureCompression=TextureImporterCompression.CompressedHQ;ground.isReadable=false;ground.SaveAndReimport();
            var leaves=Mat("Leaves","Weedtopia/LivingFoliage");leaves.SetFloat("_Sway",.013f);
            var still=Mat("GardenObjects","Weedtopia/LivingFoliage");still.SetFloat("_Sway",0);
            var shadow=Mat("CanopyShadow","Weedtopia/ContactShadow");shadow.SetColor("_Color",new Color(.045f,.105f,.045f,.24f));
            prefabs.Clear();
            foreach(var entry in art.sprites)
            {
                var sprite=AssetDatabase.LoadAllAssetsAtPath(Folder+"/Art/"+entry.file).OfType<Sprite>().Single(s=>s.name==entry.id);
                var obj=new GameObject("Landscape_"+entry.id);var artwork=new GameObject("PaintedArtwork",typeof(SpriteRenderer));artwork.transform.SetParent(obj.transform,false);artwork.transform.localRotation=Iso;
                artwork.transform.localScale=Vector3.one*(entry.worldHeight/sprite.bounds.size.y);
                var renderer=artwork.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=entry.foliage?leaves:still;renderer.spriteSortPoint=SpriteSortPoint.Pivot;
                var contact=new GameObject("SoftGroundContact",typeof(MeshFilter),typeof(MeshRenderer));contact.transform.SetParent(obj.transform,false);
                contact.GetComponent<MeshFilter>().sharedMesh=ShadowMesh();contact.GetComponent<MeshRenderer>().sharedMaterial=shadow;
                float radius=entry.worldHeight*(entry.foliage?.4f:.28f);contact.transform.localScale=new Vector3(radius,1,radius*.7f);
                prefabs.Add(entry.id,PrefabUtility.SaveAsPrefabAsset(obj,Folder+"/Prefabs/"+obj.name+".prefab"));Object.DestroyImmediate(obj);
            }
        }
        private static Mesh ShadowMesh()
        {
            string path=Folder+"/Meshes/CanopyContact.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null)return mesh;
            mesh=new Mesh{name="CanopyContact"};mesh.vertices=new[]{new Vector3(-1,.06f,-1),new Vector3(1,.06f,-1),new Vector3(-1,.06f,1),new Vector3(1,.06f,1)};mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one};mesh.triangles=new[]{0,2,1,1,2,3};mesh.RecalculateBounds();mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        private static GameObject Place(Transform group,string id,Vector2 p,float scale=1,bool flip=false)
        {
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[id],group);obj.transform.position=W(p);obj.transform.localScale=Vector3.one*scale;
            var sr=obj.GetComponentInChildren<SpriteRenderer>();sr.flipX=flip;sr.sortingOrder=WorldSort.At(obj.transform.position);
            obj.transform.Find("SoftGroundContact").GetComponent<MeshRenderer>().sortingOrder=-32010;
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(sr);
            if(id=="oak"||id=="birch"||id=="maple"||id=="willow"||id.Contains("shrub")||id=="reeds"||id=="wildflowers"||id=="lavender"||id=="hedge")stamp.authoredPlants++;else stamp.authoredGardenProps++;
            return obj;
        }
        private static void OriginalProp(string id,Vector2 p,float scale)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(ProductionArtImporter.Root+"/Prefabs/Prop_"+id+".prefab");
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(asset,gardens);obj.transform.position=W(p);obj.transform.localScale=Vector3.one*scale;
            foreach(var sr in obj.GetComponentsInChildren<SpriteRenderer>())sr.sortingOrder=WorldSort.At(obj.transform.position);
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
        }
        private static GameObject Surface(string name,Transform parent,Mesh mesh,Material material,int order)
        {
            var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent,false);obj.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=obj.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=order;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return obj;
        }
        private static Mesh Save(List<Vector3> v,List<int> t,List<Vector2> uv,List<Color> colors,string explicitPath=null)
        {
            string name=explicitPath==null?"AuthoredLandscape_"+(meshIndex++).ToString("D4"):Path.GetFileNameWithoutExtension(explicitPath),path=explicitPath??Folder+"/Meshes/"+name+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh{name=name};AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);return mesh;
        }
        private static Mesh Rectangle(Vector2 p,float width,float depth,float height,float tile)
        {
            var corners=new[]{p+new Vector2(-width/2,-depth/2),p+new Vector2(width/2,-depth/2),p+new Vector2(-width/2,depth/2),p+new Vector2(width/2,depth/2)};
            return Save(corners.Select(x=>W(x,height)).ToList(),new List<int>{0,2,1,1,2,3},corners.Select(x=>x/tile).ToList(),corners.Select(_=>Color.white).ToList());
        }
        private static Mesh Patch(Vector2 center,float width,float depth,float seed,float height=.025f,string explicitPath=null)
        {
            const int sides=48;var v=new List<Vector3>{W(center,height)};var uv=new List<Vector2>{center/12};var colors=new List<Color>{Color.white};var t=new List<int>();
            for(int ring=0;ring<2;ring++)for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,size=(ring==0?.76f:1)*(1+.075f*Mathf.Sin(a*3+seed)+.04f*Mathf.Cos(a*7-seed));
                var p=center+new Vector2(Mathf.Cos(a)*width*.5f,Mathf.Sin(a)*depth*.5f)*size;
                v.Add(W(p,height));uv.Add(p/12);colors.Add(new Color(1,1,1,ring==0?1:0));
            }
            for(int i=0;i<sides;i++){int a=1+i,b=1+(i+1)%sides;t.AddRange(new[]{0,b,a,a,b,a+sides,b,b+sides,a+sides});}
            return Save(v,t,uv,colors,explicitPath);
        }
        private static Mesh Ribbon(Vector2[] points,Func<int,float> width,float height,float fringe=1.3f,float tile=10,string explicitPath=null)
        {
            var source=points;
            points=(new[]{source[0]+(source[0]-source[1]).normalized*fringe}).Concat(source).Concat(new[]{source.Last()+(source.Last()-source[source.Length-2]).normalized*fringe}).ToArray();
            var v=new List<Vector3>();var uv=new List<Vector2>();var color=new List<Color>();var t=new List<int>();
            for(int i=0;i<points.Length;i++)
            {
                var direction=(points[Math.Min(i+1,points.Length-1)]-points[Math.Max(0,i-1)]).normalized;var side=new Vector2(-direction.y,direction.x);
                float half=width(Mathf.Clamp(i-1,0,source.Length-1))*.5f;float[] offsets={-half-fringe,-half,half,half+fringe};
                for(int k=0;k<4;k++){var p=points[i]+side*offsets[k];v.Add(W(p,height));uv.Add(p/tile);color.Add(new Color(1,1,1,k==0||k==3||i==0||i==points.Length-1?0:1));}
                if(i==0)continue;for(int k=0;k<3;k++){int a=(i-1)*4+k;t.AddRange(new[]{a,a+4,a+1,a+1,a+4,a+5});}
            }
            return Save(v,t,uv,color,explicitPath);
        }
        public static Mesh AuthorRoadSurface(string assetPath,Vector3[] points) =>
            Ribbon(Smooth(points.Select(DistrictMap.ToPlan).ToArray()),_=>5.5f,.045f,1.2f,10,assetPath);
        public static Mesh AuthorGardenPath(string assetPath,Vector2[] points,float width=3.4f) =>
            Ribbon(Smooth(points),_=>width,.027f,1.2f,10,assetPath);
        public static Mesh AuthorGardenClearing(string assetPath,Vector2 center,float width,float depth) =>
            Patch(center,width,depth,center.x,.026f,assetPath);
        private static Vector2[] Smooth(params Vector2[] points)
        {
            var result=new List<Vector2>{points[0]};
            for(int i=1;i<points.Length-1;i++)
            {
                float r=Mathf.Min(2,Mathf.Min(Vector2.Distance(points[i-1],points[i]),Vector2.Distance(points[i],points[i+1]))*.25f);
                var a=points[i]+(points[i-1]-points[i]).normalized*r;var b=points[i]+(points[i+1]-points[i]).normalized*r;result.Add(a);
                for(int j=1;j<=6;j++){float f=j/6f;result.Add((1-f)*(1-f)*a+2*(1-f)*f*points[i]+f*f*b);}
            }
            result.Add(points.Last());return result.ToArray();
        }
        private static float RiverX(float y)=>174+11*Mathf.Sin((y+35)*.012f)+5*Mathf.Sin(y*.027f);
        private static float RiverWidth(float y)=>23+5*Mathf.Sin(y*.019f+1)+3*Mathf.Sin(y*.037f);
        private static void AuthorRiver()
        {
            var points=Enumerable.Range(0,211).Select(i=>new Vector2(RiverX(-620+i*6),-620+i*6)).ToArray();
            Surface("DampRiparianMeadow",landscape,Ribbon(points,i=>RiverWidth(points[i].y)+16,-.045f,6),shade,-32735);
            Surface("SoftSandyBanks",landscape,Ribbon(points,i=>RiverWidth(points[i].y)+6,-.03f,3),soil,-32720);
            Surface("FlowingTurquoiseRiver",landscape,Ribbon(points,i=>RiverWidth(points[i].y),-.018f,1.2f),water,-32705);
            for(int i=0;i<108;i++)
            {
                float y=-310+i*7.2f+Mathf.Sin(i*2.83f)*3.7f;float side=i%2==0?-1:1;var p=new Vector2(RiverX(y)+side*(RiverWidth(y)*.5f+2.6f+Mathf.Sin(i*1.63f)*2),y);
                if(Obstructs(p,5))continue;
                Place(riverGarden,i%3==0?"rocks":"reeds",p,i%3==0?1.25f:1.1f,i%2==0);
                if(i%7==0||i%11==0)Place(riverGarden,i%3==0?"willow":"birch",p+new Vector2(side*(6+Mathf.Sin(i)*3),4),.8f+.3f*Mathf.Abs(Mathf.Sin(i*3.17f)));
            }
        }
        private static void Lane(string name,Vector2[] p,float width,bool stone=true)
        {
            var smooth=Smooth(p);Surface(name+"_SoftVerge",landscape,Ribbon(smooth,_=>width+3,.008f,2),soil,-32690);
            Surface(name,landscape,Ribbon(smooth,_=>width,.015f,stone?.8f:1.5f),stone?paving:soil,-32680);
        }
        private static void AuthorVillageLanes()
        {
            Lane("WestVillageWalk",new[]{new Vector2(-38,-270),new Vector2(-38,50),new Vector2(-38,370)},6);
            Lane("CityGardenWalk",new[]{new Vector2(-38,50),new Vector2(30,50),new Vector2(108,50)},6);
            Lane("EastVillageWalk",new[]{new Vector2(108,-270),new Vector2(108,60),new Vector2(108,380)},6);
            for(int row=0;row<6;row++)Lane("GardenLane_"+row,new[]{new Vector2(-38,78+52*row),new Vector2(22,77+52*row),new Vector2(75,80+52*row),new Vector2(108,78+52*row)},4.5f);
            Lane("CountrysidePromenade",Enumerable.Range(0,45).Select(i=>{float y=-280+i*15;return new Vector2(RiverX(y)-RiverWidth(y)*.5f-13,y);}).ToArray(),3.8f,false);
            Lane("FarmOrchardWalk",new[]{new Vector2(242,-114),new Vector2(330,-116),new Vector2(410,-108),new Vector2(500,-114),new Vector2(578,-109)},3.5f,false);
        }
        private static void AuthorParcels()
        {
            int i=0;foreach(var site in plan.sites)
            {
                Surface("GardenGround_"+site.id,landscape,Patch(site.Point,site.id.StartsWith("farm_grow")?29:34,30,i++),soil,-32670);
                if(site.id.StartsWith("coffee"))Lane("ShopApproach_"+site.id,new[]{site.Point+new Vector2(0,-11),site.Point+new Vector2(1,-22),new Vector2(108,site.v-22)},3.4f);
                else if(site.id.StartsWith("settlement"))Lane("CottageApproach_"+site.id,new[]{site.Point+new Vector2(0,-11),site.Point+new Vector2(0,-18)},3.1f,false);
            }
        }
        private static void AuthorRoads()
        {
            // Keep logistics points, travel times and cargo pools unchanged. Rounded visual corners stay inside road width.
            foreach(var route in Object.FindFirstObjectByType<GameRoot>().Presentation.routes)
            {
                if(route.conveyor)continue;
                var p=Smooth(route.points.Select(DistrictMap.ToPlan).ToArray());
                route.GetComponent<MeshFilter>().sharedMesh=Ribbon(p,_=>route.routeId=="farm_city"?7:5.5f,.045f,1.2f);
                route.GetComponent<MeshRenderer>().sharedMaterial=paving;route.GetComponent<MeshRenderer>().sortingOrder=-32500;
                if(route.routeId=="farm_city")
                {
                    Surface("CountryRoadVerge",landscape,Ribbon(p,_=>11,.03f,2.2f),soil,-32510);
                    var samples=Enumerable.Range(0,401).Select(i=>DistrictMap.ToPlan(route.Position(i/400f))).ToArray();
                    int index=Enumerable.Range(1,399).OrderBy(i=>Mathf.Abs(samples[i].x-RiverX(samples[i].y))).First();
                    var crossing=samples[index];var direction=(samples[index+1]-samples[index-1]).normalized;
                    float span=(RiverWidth(crossing.y)+11)/Mathf.Max(.4f,Mathf.Abs(direction.x));
                    var side=new Vector2(-direction.y,direction.x);
                    Surface("StoneBridgeDeck",landscape,Ribbon(new[]{crossing-direction*span*.5f,crossing+direction*span*.5f},_=>8.7f,.07f,.35f),paving,-32490);
                    var railPrefab=BridgeRail(span);var rotation=Quaternion.LookRotation(W(direction));
                    for(int k=-1;k<=1;k+=2)
                    {
                        var rail=(GameObject)PrefabUtility.InstantiatePrefab(railPrefab,riverGarden);rail.transform.position=W(crossing+side*(k*4.4f));rail.transform.rotation=rotation;
                        Place(riverGarden,"rocks",crossing-direction*(span*.48f)+side*(k*5),1.5f,k<0);
                        Place(riverGarden,"rocks",crossing+direction*(span*.48f)+side*(k*5),1.5f,k>0);
                    }
                }
            }
        }
        private static GameObject BridgeRail(float span)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var c=new List<Color>();var t=new List<int>();
            for(int i=0;i<=12;i++)
            {
                float z=-span*.5f+span*i/12,y=1+Mathf.Sin(i*Mathf.PI/12)*.65f;
                foreach(var p in new[]{new Vector3(-.45f,.07f,z),new Vector3(-.45f,y,z),new Vector3(.45f,y,z),new Vector3(.45f,.07f,z)}){v.Add(p);uv.Add(new Vector2(p.x+p.y,z)/3);c.Add(Color.white);}
                if(i>0)for(int k=0;k<3;k++){int a=(i-1)*4+k;t.AddRange(new[]{a,a+4,a+1,a+1,a+4,a+5});}
            }
            var obj=new GameObject("MossyStoneBridgeParapet",typeof(MeshFilter),typeof(MeshRenderer));obj.GetComponent<MeshFilter>().sharedMesh=Save(v,t,uv,c);obj.GetComponent<MeshRenderer>().sharedMaterial=paving;obj.GetComponent<MeshRenderer>().sortingOrder=-32480;
            var prefab=PrefabUtility.SaveAsPrefabAsset(obj,Folder+"/Prefabs/MossyStoneBridgeParapet.prefab");Object.DestroyImmediate(obj);return prefab;
        }
        private static void AuthorGardens()
        {
            int i=0;foreach(var site in plan.sites)
            {
                bool farm=site.id.StartsWith("farm_grow"),industrial=site.id.StartsWith("bakery")||site.id=="butter_factory";
                Place(gardens,i%2==0?"flowering_shrub":"hydrangea",site.Point+new Vector2(13,8),farm?.7f:1);
                Place(gardens,i%2==0?"wildflowers":"lavender",site.Point+new Vector2(-12,-10),1.15f);
                OriginalProp("lantern",site.Point+new Vector2(15,-14),1.7f);
                if(!farm)
                {
                    Place(gardens,i%3==0?"birch":"maple",site.Point+new Vector2(-16,13),industrial?.8f:1);
                    Place(gardens,"hedge",site.Point+new Vector2(0,17),1.05f,i%2==0);
                    if(i%3==0)Place(gardens,"benches",site.Point+new Vector2(0,-16),.82f);
                }
                else Place(gardens,"hedge",site.Point+new Vector2(3,-17),1);
                i++;
            }
            GardenCourt("CivicGarden",new Vector2(34,36),true);
            GardenCourt("VillageRestGarden",new Vector2(-73,232),false);
            GardenCourt("FarmGarden",new Vector2(264,-146),false);
            GardenCourt("MillGarden",new Vector2(-77,-110),true);
            // A wooded pond sits beside the countryside walk, outside every reserved building footprint.
            var pond=new Vector2(232,152);
            Surface("WoodlandPondBank",landscape,Patch(pond,48,37,3,.018f),soil,-32670);
            Surface("WoodlandPond",landscape,Patch(pond,40,29,3,.026f),water,-32660);
            for(int k=0;k<13;k++){float a=k*Mathf.PI*2/13;Place(gardens,k%3==0?"reeds":"rocks",pond+new Vector2(Mathf.Cos(a)*22,Mathf.Sin(a)*16),1.3f);}
            Place(gardens,"willow",pond+new Vector2(-20,20),1.3f);Place(gardens,"oak",pond+new Vector2(21,21),1.2f);
        }
        private static void GardenCourt(string name,Vector2 p,bool fountain)
        {
            var group=Group(name,gardens);Surface(name+"Floor",landscape,Patch(p,30,25,p.x,.023f),paving,-32660);
            Place(group,fountain?"fountain":"pergola",p+new Vector2(0,6),fountain?1.1f:.9f);
            Place(group,"benches",p+new Vector2(-9,-5),.85f);Place(group,"lavender",p+new Vector2(11,-8),1.3f);
            Place(group,"hydrangea",p+new Vector2(-13,7),1.3f);Place(group,"flowering_shrub",p+new Vector2(13,7),1.15f);
            Place(group,"stone_wall",p+new Vector2(1,16),1.2f);
        }
        private static bool Obstructs(Vector2 p,float radius)
        {
            if(plan.sites.Any(s=>Mathf.Abs(s.u-p.x)<19+radius*.4f&&Mathf.Abs(s.v-p.y)<21+radius*.4f))return true;
            return plan.routes.Any(r=>Enumerable.Range(1,r.points.Length-1).Any(k=>Distance(p,r.points[k-1],r.points[k])<radius+5));
        }
        private static float Distance(Vector2 p,Vector2 a,Vector2 b){var d=b-a;return Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.01f,d.sqrMagnitude)));}
        private static bool CanopyHidesBuildingFront(Vector2 p,float height)
        {
            // The billboard canopy rises on screen beyond its ground footprint. Reserve that
            // projected height south of each building as well as the actual trunk clearance.
            return plan.sites.Any(s=>p.y<s.v&&p.y+height*2>s.v-22&&Mathf.Abs(p.x-s.u)<20+height*.6f);
        }
        private static void AuthorWoodland()
        {
            var random=new System.Random(420924);
            var clusters=new[]{new Vector2(-265,-215),new Vector2(-263,-80),new Vector2(-257,100),new Vector2(-160,240),new Vector2(-105,365),new Vector2(8,418),new Vector2(99,425),new Vector2(254,327),new Vector2(299,237),new Vector2(319,100),new Vector2(450,95),new Vector2(585,68),new Vector2(620,-170),new Vector2(480,-187),new Vector2(364,-208),new Vector2(225,-239),new Vector2(38,-285),new Vector2(-95,-274)};
            for(int c=0;c<clusters.Length;c++)
            {
                var center=clusters[c];Surface("WoodlandFloor_"+c,landscape,Patch(center,100,90,c,-.04f),shade,-32730);
                for(int i=0;i<30;i++)
                {
                    float a=(float)random.NextDouble()*Mathf.PI*2,r=Mathf.Sqrt((float)random.NextDouble())*43;
                    var p=center+new Vector2(Mathf.Cos(a)*r,Mathf.Sin(a)*r*.8f);
                    if(Obstructs(p,8)||Mathf.Abs(p.x-RiverX(p.y))<RiverWidth(p.y)*.5f+8)continue;
                    string id=new[]{"oak","birch","maple","willow"}[(i+c)%4];float scale=.85f+(float)random.NextDouble()*.65f;
                    if(CanopyHidesBuildingFront(p,22*scale))continue;
                    Place(woodland,id,p,scale,i%2==0);
                    if(i%3==0)Place(woodland,"flowering_shrub",p+new Vector2(6,-4),1.2f);
                }
            }
            // Deliberate groups fill the garden strips; open routes and the 56 building footprints remain legible.
            for(int i=0;i<58;i++)
            {
                float y=-248+i*10.5f;var p=new Vector2(i%2==0?-51:121,y);
                if(Obstructs(p,3))continue;Place(gardens,i%4==0?"birch":"hydrangea",p,i%4==0?.85f:1.1f);
            }
            for(int i=0;i<16;i++)
            {
                var p=new Vector2(291+i*18,-127+Mathf.Sin(i*.6f)*5);Place(woodland,i%2==0?"maple":"birch",p,.95f);Place(woodland,"wildflowers",p+new Vector2(6,-7),1.2f);
            }
        }
    }
}
