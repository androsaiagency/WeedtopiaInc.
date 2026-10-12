using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Editor.BuildingArtPipeline;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit scene authoring of the farm garden. Original surfaces remain archived in the hierarchy.</summary>
    public static class FarmGardenRefinement
    {
        public const string Folder = "Assets/Weedtopia/Production/FarmGarden";
        public const string GroupName = "FarmGardenRefinement";
        private static readonly Vector2[] Approach = {
            new Vector2(236,-82),new Vector2(236,-101),new Vector2(242,-125),new Vector2(254,-149),new Vector2(269,-153)
        };
        private static readonly Vector2[] GardenLoop = {
            new Vector2(269,-153),new Vector2(279,-132),new Vector2(301,-127),new Vector2(327,-140),
            new Vector2(338,-160),new Vector2(323,-177),new Vector2(293,-180),new Vector2(273,-169),new Vector2(269,-153)
        };

        [MenuItem("Weedtopia/Production/Refine Farm River And Garden")]
        public static void Apply()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var landscape = Object.FindFirstObjectByType<NaturalEnvironmentStamp>().transform;
                if (landscape.Find(GroupName) != null) return;
                var life = Object.FindFirstObjectByType<LivingTownPresentation>();
                var route = Object.FindFirstObjectByType<GameRoot>().Presentation.Route("farm_city");
                var oldWalk = Required(landscape,"FarmOrchardWalk");
                var oldVerge = Required(landscape,"FarmOrchardWalk_SoftVerge");
                var roadVerge = Required(landscape,"CountryRoadVerge");
                var bridge = Required(landscape,"StoneBridgeDeck");
                var grove = Required(landscape,"CountrysideGroves");
                // Locate the exact original row before changing anything; preserve all unrelated planting.
                var row = new List<Transform>();
                for (int i=0;i<16;i++)
                {
                    var point = new Vector2(291+i*18,-127+Mathf.Sin(i*.6f)*5);
                    row.Add(grove.Cast<Transform>().Single(t=>Vector2.Distance(DistrictMap.ToPlan(t.position),point)<.05f));
                    row.Add(grove.Cast<Transform>().Single(t=>Vector2.Distance(DistrictMap.ToPlan(t.position),point+new Vector2(6,-7))<.05f));
                }
                var farmActors = life.actors.Where(a=>!a.airborne && a.requiredBuilding=="farm_processing").ToArray();
                if (farmActors.Length!=3) throw new InvalidOperationException("Expected the existing farm resident and two cyclists.");
                BuildingIntakeService.EnsureAssetFolder(Folder+"/Meshes");
                var garden = Group(GroupName,landscape);
                var archive = Group("InactiveOriginalPath",garden);
                oldWalk.SetParent(archive,true); oldVerge.SetParent(archive,true); archive.gameObject.SetActive(false);
                var paths = Group("ConnectedGardenPaths",garden);
                var planting = Group("FlowerMeadowAndOrchard",garden);
                var earth = Load<Material>(NaturalEnvironmentAuthoring.Folder+"/Materials/GardenEarth.mat");
                var shade = Load<Material>(NaturalEnvironmentAuthoring.Folder+"/Materials/ShadedMeadow.mat");
                var approach = Rounded(Approach,8);
                var loop = Rounded(GardenLoop,10,true);
                Surface("WarehouseToPergola",paths,PathMesh("WarehouseToPergola",approach,3.8f,.027f,1.5f),earth,-32654);
                Surface("OrchardGardenLoop",paths,PathMesh("OrchardGardenLoop",loop,3.6f,.027f,1.5f,true),earth,-32654);
                // Dense, evenly spaced tangents avoid the old long-segment/short-fillet normal discontinuity.
                var road = Rounded(route.points.Select(DistrictMap.ToPlan).ToArray(),9);
                route.GetComponent<MeshFilter>().sharedMesh=PathMesh("FarmRiverFlagstones",road,7,.045f,1.15f);
                roadVerge.GetComponent<MeshFilter>().sharedMesh=PathMesh("FarmRiverSoftVerge",road,10.5f,.03f,2);
                // The deck follows the same curve and width as the road, so no diagonal corner protrudes.
                var river=Required(landscape,"FlowingTurquoiseRiver").GetComponent<MeshFilter>().sharedMesh.vertices;
                var wet=Enumerable.Range(0,road.Length).Where(i=>CrossesRiver(road[i],river)).ToArray();
                if(wet.Length<2)throw new InvalidOperationException("Road does not cross the authored river.");
                var deck=road.Skip(wet.First()).Take(wet.Last()-wet.First()+1).ToArray();
                bridge.GetComponent<MeshFilter>().sharedMesh=PathMesh("ContinuousBridgeDeck",deck,7,.07f,1.15f);
                var riverGarden=Required(landscape,"LivingRiverbanks");
                var originalRails=riverGarden.Cast<Transform>().Where(t=>t.name=="MossyStoneBridgeParapet").ToArray();
                foreach(var rail in originalRails){rail.SetParent(archive,true);rail.gameObject.SetActive(false);}
                BuildingIntakeService.EnsureAssetFolder(Folder+"/Prefabs");
                var railTemplate=new GameObject("CurvedMossyBridgeParapet",typeof(MeshFilter),typeof(MeshRenderer));
                railTemplate.GetComponent<MeshFilter>().sharedMesh=RailMesh("BridgeRailLeft",deck,-1);
                railTemplate.GetComponent<MeshRenderer>().sharedMaterial=Load<Material>(NaturalEnvironmentAuthoring.Folder+"/Materials/MossyFlagstones.mat");
                railTemplate.GetComponent<MeshRenderer>().sortingOrder=-32480;
                var railPrefab=PrefabUtility.SaveAsPrefabAsset(railTemplate,Folder+"/Prefabs/CurvedMossyBridgeParapet.prefab");
                Object.DestroyImmediate(railTemplate);
                var rails=Group("CurvedBridgeParapets",garden);
                PrefabUtility.InstantiatePrefab(railPrefab,rails);
                var right=(GameObject)PrefabUtility.InstantiatePrefab(railPrefab,rails);
                var rightFilter=right.GetComponent<MeshFilter>();rightFilter.sharedMesh=RailMesh("BridgeRailRight",deck,1);
                PrefabUtility.RecordPrefabInstancePropertyModifications(rightFilter);

                var treePoints = new[]{new Vector2(289,-155),new Vector2(313,-156),new Vector2(356,-161),new Vector2(374,-177),
                    new Vector2(399,-151),new Vector2(417,-169),new Vector2(448,-157),new Vector2(462,-180),
                    new Vector2(489,-161),new Vector2(507,-176),new Vector2(536,-153),new Vector2(552,-173),
                    new Vector2(580,-151),new Vector2(596,-173),new Vector2(352,-195),new Vector2(547,-196)};
                for(int i=0;i<treePoints.Length;i++)
                {
                    Move(row[i*2],planting,treePoints[i],.78f+(i%4)*.055f);
                    Move(row[i*2+1],planting,treePoints[i]+new Vector2(i%2==0?8:-7,-9),1.25f);
                }
                // Low flowering islands leave a clear foreground for the cultivation plots.
                var islands=new[]{new Vector2(315,-105),new Vector2(365,-119),new Vector2(410,-110),new Vector2(468,-120),new Vector2(518,-109),new Vector2(563,-119)};
                for(int i=0;i<islands.Length;i++)
                {
                    var p=islands[i];
                    Surface("FlowerMeadow_"+i,planting,NaturalEnvironmentAuthoring.AuthorGardenClearing(Folder+"/Meshes/FlowerMeadow_"+i+".asset",p,30+i%2*6,21),shade,-32725);
                    for(int k=0;k<5;k++)
                    {
                        float angle=k*2.4f+i*.7f;
                        Place(k%3==0?"lavender":"wildflowers",p+new Vector2(Mathf.Cos(angle)*(5+k),Mathf.Sin(angle)*(3+k*.6f)),planting,.85f+k*.12f,k%2==0);
                    }
                    Place(i%2==0?"hydrangea":"flowering_shrub",p+new Vector2(3,6),planting,.9f);
                }
                Place("benches",new Vector2(319,-165),planting,1.05f);
                Place("lavender",new Vector2(327,-171),planting,1.2f);
                Place("hydrangea",new Vector2(304,-150),planting,1.1f);
                Place("wildflowers",new Vector2(296,-165),planting,1.4f);
                Place("flowering_shrub",new Vector2(284,-164),planting,1.15f);
                Place("lavender",new Vector2(247,-121),planting,1.0f);
                Place("wildflowers",new Vector2(231,-111),planting,1.3f);
                foreach(var actor in farmActors)
                {
                    bool cyclist=actor.anchor.name.Contains("cyclist");
                    actor.path=(cyclist?loop:approach).Select(p=>DistrictMap.ToWorld(p)).ToArray();
                    actor.closedLoop=cyclist;
                    actor.anchor.position=actor.path[0];
                    PrefabUtility.RecordPrefabInstancePropertyModifications(actor.anchor);
                }
                // Move the existing nearby butterflies into the flower beds; retain animation references and counts.
                var butterflies=life.actors.Where(a=>a.airborne&&a.anchor.name.Contains("butterfly")&&DistrictMap.ToPlan(a.path[0]).x>260&&DistrictMap.ToPlan(a.path[0]).x<320&&DistrictMap.ToPlan(a.path[0]).y< -100).ToArray();
                for(int i=0;i<butterflies.Length;i++)
                {
                    var target=i==0?new Vector2(286,-148):new Vector2(315,-106);
                    var oldCenter=butterflies[i].path.Take(butterflies[i].path.Length-1).Aggregate(Vector3.zero,(a,p)=>a+p)/(butterflies[i].path.Length-1);
                    var delta=DistrictMap.ToWorld(target,oldCenter.y)-oldCenter;
                    butterflies[i].path=butterflies[i].path.Select(p=>p+delta).ToArray();
                    butterflies[i].anchor.position=butterflies[i].path[0];
                    PrefabUtility.RecordPrefabInstancePropertyModifications(butterflies[i].anchor);
                }
                EditorUtility.SetDirty(life);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Docs/FarmGardenRevision");
                File.WriteAllText("Docs/FarmGardenRevision/authoring.json",JsonUtility.ToJson(new Receipt{utc=DateTime.UtcNow.ToString("O"),relocatedTrees=16,flowerIslands=islands.Length,reroutedActors=farmActors.Length,roadSamples=road.Length},true));
            }
            finally {if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }


        [MenuItem("Weedtopia/Production/Refresh Farm Garden Path Meshes")]
        public static void RefreshPathMeshes()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var landscape=Object.FindFirstObjectByType<NaturalEnvironmentStamp>().transform;
                Required(landscape,GroupName);
                var route=Object.FindFirstObjectByType<GameRoot>().Presentation.Route("farm_city");
                var road=Rounded(route.points.Select(DistrictMap.ToPlan).ToArray(),9);
                PathMesh("FarmRiverFlagstones",road,7,.045f,1.15f);
                PathMesh("FarmRiverSoftVerge",road,10.5f,.03f,2);
                var river=Required(landscape,"FlowingTurquoiseRiver").GetComponent<MeshFilter>().sharedMesh.vertices;
                var wet=Enumerable.Range(0,road.Length).Where(i=>CrossesRiver(road[i],river)).ToArray();
                if(wet.Length<2)throw new InvalidOperationException("Road does not cross the authored river.");
                var deck=road.Skip(wet.First()).Take(wet.Last()-wet.First()+1).ToArray();
                PathMesh("ContinuousBridgeDeck",deck,7,.07f,1.15f);
                RailMesh("BridgeRailLeft",deck,-1);RailMesh("BridgeRailRight",deck,1);
                PathMesh("WarehouseToPergola",Rounded(Approach,8),3.8f,.027f,1.5f);
                // Keep the serialized actor centerline aligned with the path in both first-time authoring and repair.
                var loop=Rounded(GardenLoop,10,true);
                PathMesh("OrchardGardenLoop",loop,3.6f,.027f,1.5f,true);
                var life=Object.FindFirstObjectByType<LivingTownPresentation>();
                foreach(var actor in life.actors.Where(a=>!a.airborne&&a.requiredBuilding=="farm_processing"))
                    actor.path=(actor.anchor.name.Contains("cyclist")?loop:Rounded(Approach,8)).Select(p=>DistrictMap.ToWorld(p)).ToArray();
                EditorUtility.SetDirty(life);EditorSceneManager.MarkSceneDirty(landscape.gameObject.scene);
                EditorSceneManager.SaveScene(landscape.gameObject.scene);AssetDatabase.SaveAssets();
            }
            finally {if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }

        private static Transform Required(Transform parent,string name)=>parent.Find(name)??throw new InvalidOperationException("Missing authored object: "+name);
        private static T Load<T>(string path) where T:Object=>AssetDatabase.LoadAssetAtPath<T>(path)??throw new InvalidOperationException("Missing existing asset: "+path);
        private static Transform Group(string name,Transform parent){var result=new GameObject(name).transform;result.SetParent(parent,false);return result;}
        private static void Move(Transform item,Transform parent,Vector2 point,float scale)
        {
            item.SetParent(parent,true);item.position=DistrictMap.ToWorld(point);item.localScale=Vector3.one*scale;
            foreach(var sprite in item.GetComponentsInChildren<SpriteRenderer>()){sprite.sortingOrder=WorldSort.At(item.position);PrefabUtility.RecordPrefabInstancePropertyModifications(sprite);}
            PrefabUtility.RecordPrefabInstancePropertyModifications(item);
        }
        private static void Place(string id,Vector2 point,Transform parent,float scale,bool flip=false)
        {
            var prefab=Load<GameObject>(NaturalEnvironmentAuthoring.Folder+"/Prefabs/Landscape_"+id+".prefab");
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);
            Move(instance.transform,parent,point,scale);
            var sprite=instance.GetComponentInChildren<SpriteRenderer>();sprite.flipX=flip;PrefabUtility.RecordPrefabInstancePropertyModifications(sprite);
        }
        private static void Surface(string name,Transform parent,Mesh mesh,Material material,int sorting)
        {
            var surface=Group(name,parent).gameObject;
            surface.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=surface.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=sorting;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        private static bool CrossesRiver(Vector2 point,Vector3[] vertices)
        {
            for(int row=4;row<vertices.Length;row+=4)
            {
                var a=DistrictMap.ToPlan((vertices[row-3]+vertices[row-2])*.5f);
                var b=DistrictMap.ToPlan((vertices[row+1]+vertices[row+2])*.5f);
                if(point.y<a.y||point.y>b.y)continue;
                float t=Mathf.InverseLerp(a.y,b.y,point.y);
                float half=Mathf.Lerp(Vector3.Distance(vertices[row-3],vertices[row-2]),Vector3.Distance(vertices[row+1],vertices[row+2]),t)*.5f;
                return Mathf.Abs(point.x-Mathf.Lerp(a.x,b.x,t))<=half+3.5f;
            }
            return false;
        }
        private static Mesh RailMesh(string name,Vector2[] path,float sign)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
            for(int i=0;i<path.Length;i++)
            {
                var direction=(path[Math.Min(i+1,path.Length-1)]-path[Math.Max(i-1,0)]).normalized;
                var side=new Vector2(-direction.y,direction.x);var center=path[i]+side*(sign*4);
                float top=1+Mathf.Sin(i*Mathf.PI/(path.Length-1))*.55f;
                foreach(var cross in new[]{new Vector2(-.4f,.08f),new Vector2(-.4f,top),new Vector2(.4f,top),new Vector2(.4f,.08f)})
                {var p=center+side*cross.x;vertices.Add(DistrictMap.ToWorld(p,cross.y));uv.Add(new Vector2(i*.3f,cross.y));colors.Add(Color.white);}
                if(i>0)for(int k=0;k<3;k++){int a=(i-1)*4+k;triangles.AddRange(new[]{a,a+4,a+1,a+1,a+4,a+5});}
            }
            return SaveMesh(name,vertices,triangles,uv,colors);
        }
        // Fillets use a bounded radius and sample every straight and curved segment. No distant control point
        // can dominate a vertex normal, which was responsible for the pinched road and projecting triangles.
        private static Vector2[] Rounded(Vector2[] controls,float radius,bool closed=false)
        {
            var output=new List<Vector2>();
            void Line(Vector2 end)
            {
                if(output.Count==0){output.Add(end);return;}
                var start=output.Last();int count=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(start,end)));
                for(int i=1;i<=count;i++)output.Add(Vector2.Lerp(start,end,i/(float)count));
            }
            int countPoints=closed?controls.Length-1:controls.Length;
            if(!closed)Line(controls[0]);
            for(int i=closed?0:1;i<(closed?countPoints:countPoints-1);i++)
            {
                var point=controls[i];var previous=controls[(i+countPoints-1)%countPoints];var next=controls[(i+1)%countPoints];
                float distance=Mathf.Min(radius,Mathf.Min(Vector2.Distance(previous,point)*(!closed&&i==1?.9f:.45f),Vector2.Distance(point,next)*(!closed&&i==countPoints-2?.9f:.45f)));
                var a=point+(previous-point).normalized*distance;var b=point+(next-point).normalized*distance;
                Line(a);int steps=Mathf.Max(8,Mathf.CeilToInt(distance*2));
                for(int k=1;k<=steps;k++){float t=k/(float)steps;Line((1-t)*(1-t)*a+2*(1-t)*t*point+t*t*b);}
            }
            Line(closed?output[0]:controls.Last());
            return output.Where((p,i)=>i==0||Vector2.Distance(p,output[i-1])>.0001f).ToArray();
        }
        private static Mesh PathMesh(string name,Vector2[] centerline,float width,float height,float feather,bool closed=false)
        {
            var points=closed?centerline:new[]{centerline[0]+(centerline[0]-centerline[1]).normalized*feather}.Concat(centerline)
                .Concat(new[]{centerline.Last()+(centerline.Last()-centerline[centerline.Length-2]).normalized*feather}).ToArray();
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
            for(int i=0;i<points.Length;i++)
            {
                var before=points[i==0?(closed?points.Length-2:0):i-1];
                var after=points[i==points.Length-1?(closed?1:i):i+1];
                var incoming=(points[i]-before).normalized;var outgoing=(after-points[i]).normalized;
                var tangent=(incoming+outgoing).normalized;var side=new Vector2(-tangent.y,tangent.x);
                var offsets=new[]{-width*.5f-feather,-width*.5f,width*.5f,width*.5f+feather};
                for(int k=0;k<4;k++)
                {
                    var p=points[i]+side*offsets[k];vertices.Add(DistrictMap.ToWorld(p,height));uv.Add(p/10);
                    colors.Add(new Color(1,1,1,k==0||k==3||(!closed&&(i==0||i==points.Length-1))?0:1));
                }
                if(i==0)continue;
                for(int k=0;k<3;k++){int a=(i-1)*4+k;triangles.AddRange(new[]{a,a+1,a+4,a+1,a+5,a+4});}
            }
            return SaveMesh(name,vertices,triangles,uv,colors);
        }
        private static Mesh SaveMesh(string name,List<Vector3> vertices,List<int> triangles,List<Vector2> uv,List<Color> colors)
        {
            string path=Folder+"/Meshes/"+name+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){mesh=new Mesh{name=name};AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.SetColors(colors);
            mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);return mesh;
        }
        [Serializable] private sealed class Receipt {public string utc;public int relocatedTrees,flowerIslands,reroutedActors,roadSamples;}
    }
}
