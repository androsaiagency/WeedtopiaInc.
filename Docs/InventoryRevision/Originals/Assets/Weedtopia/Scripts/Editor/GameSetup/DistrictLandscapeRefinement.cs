using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Unity;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit editor authoring of the approved district composition, never runtime world generation.</summary>
    public static class DistrictLandscapeRefinement
    {
        private const string Folder=BurningHillsLayoutInstaller.Folder;
        [MenuItem("Weedtopia/Production/Refine Approved District Landscape")]
        public static void Apply()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();var setup=EditorSceneManager.GetSceneManagerSetup();
            try {var scene=EditorSceneManager.OpenScene(GameScaffold.GameScene);ApplyToScene(Object.FindFirstObjectByType<GameRoot>());EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
            finally {if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        public static void ApplyToScene(GameRoot root)
        {
            var world=GameObject.Find("BurningHills_Districts").transform;
            var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/ParcelGround.mat");
            if(material==null)
            {
                var original=AssetDatabase.LoadAssetAtPath<Material>(ProductionArtImporter.Root+"/Materials/ReclaimedPath.mat");
                material=new Material(original){name="ParcelGround",shader=Shader.Find("Weedtopia/FeatheredGround")};
                AssetDatabase.CreateAsset(material,Folder+"/ParcelGround.mat");
            }
            foreach(var filter in world.GetComponentsInChildren<MeshFilter>())
            {
                if(!filter.name.StartsWith("Parcel_"))continue;
                filter.sharedMesh=Parcel();var ground=filter.GetComponent<MeshRenderer>();ground.sharedMaterial=material;
                // Transparent feathering must be below even the northernmost negative-order sprite.
                ground.sortingOrder=-32000;
            }
            var props=world.Find("AuthoredDistrictProps");
            foreach(Transform prop in props)
            {
                if(prop.name.Contains("lantern")||prop.name.Contains("planter"))prop.localScale=Vector3.one*1.8f;
                else if(prop.name.Contains("flowers"))prop.localScale=Vector3.one*1.6f;
                else if(prop.name.Contains("crates"))prop.localScale=Vector3.one*1.3f;
            }
            foreach(var actor in root.Presentation.characters)
            {
                actor.transform.localScale=Vector3.one*1.45f;
                var definition=root.Definitions??AssetDatabase.LoadAssetAtPath<Weedtopia.Content.GameContent>(GameScaffold.ContentPath).Load();
                var site=definition.instances.Single(x=>x.id==actor.workplaceId);
                var point=DistrictMap.ToPlan(new Vector3(site.x,0,site.z));
                // Keep the resident on the inner sidewalk, clear of the fixed quest/details panels.
                float side=(site.zone==Weedtopia.Domain.Zone.City||site.zone==Weedtopia.Domain.Zone.Settlement)&&point.x<34?10:-11;
                actor.transform.position=DistrictMap.ToWorld(point+new Vector2(side,-13));
                PrefabUtility.RecordPrefabInstancePropertyModifications(actor.transform);
            }
            var navigation=root.Ui.GetComponentInChildren<DistrictNavigator>(true);
            var handle=navigation.position.handleRect;
            handle.anchorMin=Vector2.zero;handle.anchorMax=new Vector2(navigation.position.size,1);handle.sizeDelta=Vector2.zero;handle.anchoredPosition=Vector2.zero;
            var handleImage=handle.GetComponent<UnityEngine.UI.Image>();handleImage.sprite=root.Presentation.art.Get("star");handleImage.type=UnityEngine.UI.Image.Type.Simple;handleImage.preserveAspect=true;
            var plan=JsonUtility.FromJson<BurningHillsLayoutInstaller.LayoutPlan>(File.ReadAllText(Folder+"/BurningHillsLayout.json"));
            if(world.Find("DistrictGardens")==null)
            {
                var garden=new GameObject("DistrictGardens").transform;garden.SetParent(world,false);
                foreach(var site in plan.sites)
                {
                    if(site.id.StartsWith("farm_grow"))continue;
                    Place(garden,"tree",site.Point+new Vector2(-14,12),1.1f);
                    Place(garden,"flowers",site.Point+new Vector2(-12,-12),1.6f);
                    Place(garden,"planter",site.Point+new Vector2(9,-13),1.5f);
                }
                // Trees mark the long countryside journey without hiding its road.
                for(int i=0;i<11;i++)Place(garden,i%2==0?"pine":"tree",new Vector2(135+i*9,-105+(i%3)*5),1.6f);
            }
            // Replace the narrow triangle bends with constant-width mitered ribbons.
            foreach(var route in root.Presentation.routes)
                route.GetComponent<MeshFilter>().sharedMesh=Road(route.routeId,route.points,route.conveyor?1.3f:4);
            var river=world.Find("River").GetComponent<MeshFilter>();
            string path=Folder+"/Meshes/RiverLongRipples.asset";var water=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(water==null){water=Object.Instantiate(river.sharedMesh);water.name="RiverLongRipples";water.uv=water.uv.Select(x=>new Vector2(x.x,x.y*.125f)).ToArray();AssetDatabase.CreateAsset(water,path);}river.sharedMesh=water;
        }
        private static void Place(Transform parent,string id,Vector2 point,float scale)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ProductionArtImporter.Root+"/Prefabs/Prop_"+id+".prefab");
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);obj.transform.position=DistrictMap.ToWorld(point);obj.transform.localScale=Vector3.one*scale;
            foreach(var renderer in obj.GetComponentsInChildren<SpriteRenderer>())renderer.sortingOrder=WorldSort.At(obj.transform.position);
        }
        private static Mesh Parcel()
        {
            string path=Folder+"/Meshes/SoftParcel.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing!=null)return existing;
            const int count=64;var v=new List<Vector3>{new Vector3(0,.028f,0)};var colors=new List<Color>{Color.white};var triangles=new List<int>();
            for(int ring=0;ring<2;ring++)for(int i=0;i<count;i++)
            {
                float angle=i*Mathf.PI*2/count,c=Mathf.Cos(angle),s=Mathf.Sin(angle);
                float irregular=1+.018f*Mathf.Sin(angle*7)+.012f*Mathf.Cos(angle*11);
                float size=ring==0?.82f:1f;
                v.Add(new Vector3(Mathf.Sign(c)*Mathf.Pow(Mathf.Abs(c),.34f)*14*size*irregular,.028f,Mathf.Sign(s)*Mathf.Pow(Mathf.Abs(s),.34f)*12*size*irregular));
                colors.Add(new Color(1,1,1,ring==0?1:0));
            }
            for(int i=0;i<count;i++)
            {
                int a=1+i,b=1+(i+1)%count;triangles.AddRange(new[]{0,b,a,a,b,a+count,b,b+count,a+count});
            }
            var mesh=new Mesh{name="SoftParcel"};mesh.SetVertices(v);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);mesh.SetUVs(0,v.Select(p=>new Vector2(p.x/11,p.z/11)).ToList());mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        private static Mesh Road(string id,Vector3[] points,float width)
        {
            string path=Folder+"/Meshes/Miter_"+id+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing!=null)return existing;
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();float distance=0;
            for(int i=0;i<points.Length;i++)
            {
                Vector3 before=(points[i]-points[Mathf.Max(0,i-1)]).normalized,after=(points[Mathf.Min(i+1,points.Length-1)]-points[i]).normalized;
                if(i==0)before=after;if(i==points.Length-1)after=before;
                Vector3 normal=Vector3.Cross(Vector3.up,after),miter=Vector3.Cross(Vector3.up,(before+after).normalized);
                Vector3 offset=miter*(width*.5f/Mathf.Max(.5f,Vector3.Dot(miter,normal)));
                if(i>0)distance+=Vector3.Distance(points[i-1],points[i]);vertices.Add(points[i]-offset);vertices.Add(points[i]+offset);
                uv.Add(new Vector2(0,distance/3));uv.Add(new Vector2(1,distance/3));if(i>0){int a=(i-1)*2;triangles.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
            }
            var mesh=new Mesh{name="Miter_"+id};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.SetColors(vertices.Select(_=>Color.white).ToList());mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
    }
}
