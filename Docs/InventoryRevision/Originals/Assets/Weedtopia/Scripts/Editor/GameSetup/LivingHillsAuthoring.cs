using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Domain;
using Weedtopia.Unity;
using Weedtopia.Editor.BuildingArtPipeline;
using Object=UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Additive explicit authoring: preserves all earlier landscape, road and gameplay objects.</summary>
    public static class LivingHillsAuthoring
    {
        public const string Folder="Assets/Weedtopia/Production/LivingHills";
        private static readonly Quaternion Iso=Quaternion.Euler(35.264f,45,0);
        private static readonly Dictionary<string,GameObject> prefabs=new Dictionary<string,GameObject>();
        private static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        private static readonly List<LivingTownPresentation.Actor> actors=new List<LivingTownPresentation.Actor>();
        private static LivingTownPresentation living;
        private static Transform hills,squares,people,wildlife;
        [MenuItem("Weedtopia/Production/Author Living Burning Hills")]
        public static void Apply()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(GameScaffold.GameScene);
                if(Object.FindFirstObjectByType<LivingTownPresentation>()!=null)return;
                Build();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
            finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        public static void RebuildOwned()
        {
            if(!UnityEngine.Application.isBatchMode)throw new InvalidOperationException("Explicit owned iteration only");
            BurningHillsLayoutInstaller.RequireSafeEditor();var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(GameScaffold.GameScene);var previous=Object.FindFirstObjectByType<LivingTownPresentation>();
                if(previous!=null)Object.DestroyImmediate(previous.gameObject);
                Build();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
            finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        private static Transform Group(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        private static Material Material(string name,float sway)
        {
            var path=Folder+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Weedtopia/LivingFoliage")){name=name};AssetDatabase.CreateAsset(m,path);}
            m.SetFloat("_Sway",sway);EditorUtility.SetDirty(m);return m;
        }
        private static void ImportArt()
        {
            foreach(var sub in new[]{"Prefabs","Materials","Meshes"})BuildingIntakeService.EnsureAssetFolder(Folder+"/"+sub);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var manifest=JsonUtility.FromJson<NaturalEnvironmentAuthoring.ArtManifest>(File.ReadAllText(Folder+"/art_manifest.json"));
            sprites.Clear();prefabs.Clear();
            foreach(var group in manifest.sprites.GroupBy(s=>s.file))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+"/Art/"+group.Key);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.spritePixelsPerUnit=100;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.wrapMode=TextureWrapMode.Clamp;
                importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.isReadable=false;
#pragma warning disable 618
                importer.spritesheet=group.Select(s=>new SpriteMetaData{name=s.id,rect=new Rect(s.x,s.y,s.width,s.height),alignment=9,pivot=new Vector2(.5f,s.id.StartsWith("swallow")||s.id.StartsWith("butterfly")?.5f:0)}).ToArray();
#pragma warning restore 618
                importer.SaveAndReimport();
                foreach(var sprite in AssetDatabase.LoadAllAssetsAtPath(Folder+"/Art/"+group.Key).OfType<Sprite>())sprites.Add(sprite.name,sprite);
            }
            var still=Material("PaintedTown",0);var breeze=Material("PennantBreeze",.014f);
            foreach(var item in manifest.sprites.Where(s=>!s.id.EndsWith("_1")))
            {
                var go=new GameObject("Living_"+item.id);var art=new GameObject("PaintedArtwork",typeof(SpriteRenderer));art.transform.SetParent(go.transform,false);art.transform.localRotation=Iso;
                var renderer=art.GetComponent<SpriteRenderer>();renderer.sprite=sprites[item.id];renderer.sharedMaterial=item.id=="pennants"?breeze:still;renderer.spriteSortPoint=SpriteSortPoint.Pivot;
                art.transform.localScale=Vector3.one*(item.worldHeight/renderer.sprite.bounds.size.y);
                if(item.id.StartsWith("resident")||item.id=="cyclist_0")
                {
                    var shadow=new GameObject("SoftGroundContact",typeof(MeshFilter),typeof(MeshRenderer));shadow.transform.SetParent(go.transform,false);
                    shadow.transform.localScale=new Vector3(item.id=="cyclist_0"?1.7f:1,1,.6f);
                    shadow.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(NaturalEnvironmentAuthoring.Folder+"/Meshes/CanopyContact.asset");
                    var sr=shadow.GetComponent<MeshRenderer>();sr.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(NaturalEnvironmentAuthoring.Folder+"/Materials/CanopyShadow.mat");sr.sortingOrder=-32010;
                }
                prefabs.Add(item.id,PrefabUtility.SaveAsPrefabAsset(go,Folder+"/Prefabs/"+go.name+".prefab"));Object.DestroyImmediate(go);
            }
        }
        private static GameObject Place(string id,Vector2 p,Transform parent,float scale=1,bool flip=false)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[id],parent);go.transform.position=DistrictMap.ToWorld(p);go.transform.localScale=Vector3.one*scale;
            var sr=go.GetComponentInChildren<SpriteRenderer>();sr.sortingOrder=WorldSort.At(go.transform.position);sr.flipX=flip;
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(sr);
            living.authoredLandmarks++;return go;
        }
        private static void Actor(string id,Vector2[] path,Zone zone,string building,float phase,float speed,bool flying=false,float height=0)
        {
            var go=Place(id+"_0",path[0],flying?wildlife:people);living.authoredLandmarks--;
            var sr=go.GetComponentInChildren<SpriteRenderer>();
            actors.Add(new LivingTownPresentation.Actor{anchor=go.transform,artwork=sr,frames=new[]{sprites[id+"_0"],sprites[id+"_1"]},
                path=path.Select(p=>DistrictMap.ToWorld(p,height)).ToArray(),zone=zone,requiredBuilding=building,phase=phase,speed=speed,
                closedLoop=flying,airborne=flying,restSeconds=id=="cyclist"?2:4,frameRate=flying?(id=="swallow"?4:7):2.6f,bob=flying?.13f:.055f});
        }
        private static Vector2[] Loop(Vector2 center,float x,float y)
        {return Enumerable.Range(0,33).Select(i=>center+new Vector2(Mathf.Cos(i*Mathf.PI/16)*x,Mathf.Sin(i*Mathf.PI/16)*y)).ToArray();}
        private static void GardenSurface(string name,Mesh mesh)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(living.transform,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(NaturalEnvironmentAuthoring.Folder+"/Materials/GardenEarth.mat");
            renderer.sortingOrder=-32655;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        private static void Footpath(string name,params Vector2[] points) =>
            GardenSurface(name,NaturalEnvironmentAuthoring.AuthorGardenPath(Folder+"/Meshes/"+name+".asset",points));
        private static void Build()
        {
            ImportArt();actors.Clear();var world=GameObject.Find("BurningHills_Districts").transform;
            living=Group("LivingBurningHills",world).gameObject.AddComponent<LivingTownPresentation>();living.root=Object.FindFirstObjectByType<GameRoot>();
            hills=Group("CopperHillsAndOldKilns",living.transform);squares=Group("NeighborhoodMeetingPlaces",living.transform);
            people=Group("ResidentsAndCyclists",living.transform);wildlife=Group("SwallowsAndButterflies",living.transform);
            var ridges=new[]{new Vector2(-300,102),new Vector2(-254,188),new Vector2(-142,231),new Vector2(-89,415),new Vector2(15,438),new Vector2(119,421),new Vector2(275,327),new Vector2(338,115),new Vector2(449,105),new Vector2(563,94),new Vector2(657,-31),new Vector2(650,-190),new Vector2(372,-288),new Vector2(-113,-315)};
            for(int i=0;i<ridges.Length;i++)Place(i%2==0?"copper_ridge":"walking_hill",ridges[i],hills,.8f+(i%3)*.15f,i%3==0);
            Place("old_kiln",new Vector2(-87,184),hills);Place("old_kiln",new Vector2(218,238),hills,.72f,true);
            Place("town_sign",new Vector2(94,28),squares,1.05f);Place("town_sign",new Vector2(-51,61),squares,.72f);
            foreach(var p in new[]{new Vector2(-7,-76),new Vector2(77,-128),new Vector2(9,-180),new Vector2(77,-232),new Vector2(20,23),new Vector2(-65,-94),new Vector2(273,-146)})Place("street_cafe",p,squares,1.2f);
            foreach(var p in new[]{new Vector2(49,39),new Vector2(-65,239),new Vector2(274,-136),new Vector2(90,-194)})Place("pennants",p,squares,.95f);
            foreach(var p in new[]{new Vector2(-87,181),new Vector2(218,235)})GardenSurface("KilnGarden_"+p.x,NaturalEnvironmentAuthoring.AuthorGardenClearing(Folder+"/Meshes/KilnGarden_"+p.x+".asset",p,27,22));
            Footpath("OldKilnWalk",new Vector2(-38,184),new Vector2(-55,178),new Vector2(-72,174),new Vector2(-86,177));
            Footpath("HeritageGardenWalk",new Vector2(-86,177),new Vector2(-105,192),new Vector2(-101,211),new Vector2(-85,220),new Vector2(-73,225));
            Footpath("PondKilnWalk",new Vector2(255,158),new Vector2(268,177),new Vector2(267,205),new Vector2(248,228),new Vector2(218,232));
            Footpath("WelcomeApproach",new Vector2(108,22),new Vector2(99,21),new Vector2(94,23));
            // Existing pedestrian lanes, outside freight lanes and building footprints. Two-way strolling includes rests.
            for(int row=0;row<4;row++)
            {
                float y=-80-row*52;string site="coffee_"+(row*3+1).ToString("D2");
                Actor(row%2==0?"resident_a":"resident_b",new[]{new Vector2(8,y),new Vector2(48,y),new Vector2(98,y)},Zone.City,site,row*9,1.7f);
                Actor("resident_c",new[]{new Vector2(104,y+2),new Vector2(104,y+35)},Zone.City,site,13+row*7,1.2f);
            }
            for(int row=0;row<6;row++)
            {
                float y=78+row*52;string site="settlement_"+(row*3+1).ToString("D2");
                Actor(row%2==0?"resident_b":"resident_c",new[]{new Vector2(-24,y),new Vector2(22,y-1),new Vector2(75,y+2),new Vector2(99,y)},Zone.Settlement,site,row*17,1.5f);
            }
            Actor("resident_a",new[]{new Vector2(-32,48),new Vector2(15,48),new Vector2(86,48)},Zone.City,"town_hall",20,1.8f);
            Actor("resident_b",new[]{new Vector2(-35,-68),new Vector2(-35,28)},Zone.City,"coffee_01",9,1.7f);
            Actor("resident_c",new[]{new Vector2(-38,143),new Vector2(-38,210),new Vector2(-38,275)},Zone.Settlement,"settlement_04",6,1.3f);
            Actor("resident_a",new[]{new Vector2(-234,99),new Vector2(-234,20),new Vector2(-234,-61)},Zone.Industry,"bakery_09",40,1.6f);
            Actor("resident_b",new[]{new Vector2(-86,-75),new Vector2(-86,-92),new Vector2(-67,-99)},Zone.Industry,"supermarket",8,1.5f);
            Actor("resident_c",new[]{new Vector2(254,-114),new Vector2(290,-115),new Vector2(330,-116)},Zone.Farm,"farm_processing",16,1.3f);
            Actor("resident_c",new[]{new Vector2(-38,184),new Vector2(-55,178),new Vector2(-72,174),new Vector2(-86,177)},Zone.Industry,"butter_factory",17,1.3f);
            for(int i=0;i<3;i++)Actor("cyclist",new[]{new Vector2(104,-249),new Vector2(104,-150),new Vector2(104,-50),new Vector2(104,42)},Zone.City,"coffee_01",i*28,4.2f);
            for(int i=0;i<2;i++)Actor("cyclist",new[]{new Vector2(298,-117),new Vector2(366,-115),new Vector2(430,-111),new Vector2(512,-113)},Zone.Farm,"farm_processing",i*37,3.8f);
            var flightCenters=new[]{new Vector2(155,30),new Vector2(159,173),new Vector2(226,150),new Vector2(330,-116),new Vector2(-81,183),new Vector2(32,397)};
            for(int i=0;i<flightCenters.Length;i++)for(int k=0;k<2;k++)Actor("swallow",Loop(flightCenters[i]+new Vector2(k*4,k*3),24,12),Zone.Farm,null,i*13+k*.65f,7,true,12+k*1.5f);
            var gardens=new[]{new Vector2(40,34),new Vector2(13,-69),new Vector2(81,-120),new Vector2(7,-220),new Vector2(-72,229),new Vector2(20,132),new Vector2(77,291),new Vector2(270,-148),new Vector2(305,-125),new Vector2(229,161),new Vector2(-87,-98),new Vector2(-218,114)};
            for(int i=0;i<gardens.Length;i++)Actor("butterfly",Loop(gardens[i],3.5f,2.4f),Zone.Farm,null,i*1.4f,.7f,true,1.5f);
            living.actors=actors.ToArray();EditorUtility.SetDirty(living);
            Directory.CreateDirectory("Docs/LivingHills");File.WriteAllText("Docs/LivingHills/integration.json",JsonUtility.ToJson(new Receipt{utc=DateTime.UtcNow.ToString("O"),landmarks=living.authoredLandmarks,residents=actors.Count(a=>!a.airborne),wildlife=actors.Count(a=>a.airborne),artSprites=sprites.Count},true));
        }
        [Serializable] private sealed class Receipt{public string utc;public int landmarks,residents,wildlife,artSprites;}
    }
}
