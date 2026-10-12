using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Weedtopia.Buildings.Presentation;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.BuildingArtPipeline
{
    /// <summary>Explicit, isolated authoring and synchronous render experiment; never installs into production.</summary>
    public static class TownHall3DReview
    {
        public const string Root = "Assets/Weedtopia/Art/Buildings/Archive/TownHall3D20260928/Review";
        public const string ReportFolder = "Docs/ArtProduction/Buildings/town_hall/T_FIXED/Meshy_Highpoly_2026-09-28/UnityReview";
        static readonly int[] Targets = {50000, 20000, 12000};
        static readonly int[] ExpectedTriangles = {50000, 20000, 12000};
        [Serializable] public class TextureRecord { public string path, format; public int width, height; public long runtimeBytes; }
        [Serializable] public class MeshRecord { public int triangles, vertices; public Vector3 bounds; public long runtimeBytes; }
        [Serializable] public class Sample { public int instances, lod, triangles, visibleInstances; public double medianMs, p95Ms; }
        [Serializable] public class Receipt
        {
            public string utc, unityVersion, gpu, cpu, renderPipeline, status;
            public string measurement = "1280x720 synchronous SRP render plus 1px GPU readback; 5 warmups, 20 samples. Editor microbenchmark, NOT player FPS or target-device certification.";
            public bool productionChanged;
            public List<TextureRecord> textures = new List<TextureRecord>();
            public List<MeshRecord> meshes = new List<MeshRecord>();
            public List<Sample> samples = new List<Sample>();
        }

        [MenuItem("Weedtopia/Art Pipeline/3D Town Hall/Create and Measure Isolated Review")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit play mode first.");
            for (int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save open scene changes before running the explicit review.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            Directory.CreateDirectory(ReportFolder);
            var receipt=new Receipt {utc=DateTime.UtcNow.ToString("O"),unityVersion=Application.unityVersion,gpu=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,renderPipeline=GraphicsSettings.currentRenderPipeline?.name,status="running",productionChanged=false};
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                BuildingIntakeService.EnsureAssetFolder(Root+"/Materials");
                foreach (string file in Directory.GetFiles(Root+"/Source","*.png")) ImportTexture(file.Replace('\\','/'),receipt);
                var materials=new Material[3];
                var models=new GameObject[3];
                for(int i=0;i<3;i++)
                {
                    int target=Targets[i];
                    string path=Root+"/Source/TownHall_"+target+".fbx";
                    var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                    importer.materialImportMode=ModelImporterMaterialImportMode.None;
                    importer.importAnimation=false;
                    importer.isReadable=false;
                    importer.importNormals=ModelImporterNormals.Import;
                    importer.importTangents=ModelImporterTangents.CalculateMikk;
                    importer.SaveAndReimport();
                    models[i]=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    Mesh mesh=models[i].GetComponentInChildren<MeshFilter>().sharedMesh;
                    int triangles=(int)(mesh.GetIndexCount(0)/3);
                    if(triangles!=ExpectedTriangles[i])throw new InvalidDataException("Unexpected triangle count: "+path+" "+triangles);
                    receipt.meshes.Add(new MeshRecord{triangles=triangles,vertices=mesh.vertexCount,bounds=mesh.bounds.size,runtimeBytes=Profiler.GetRuntimeMemorySizeLong(mesh)});
                    materials[i]=MaterialFor(target,false);
                }
                var high4K=MaterialFor(50000,true);
                GameObject prefab=CreatePrefab(models,materials);
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var camera=new GameObject("Review Camera",typeof(Camera)).GetComponent<Camera>();
                camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=13;
                camera.backgroundColor=new Color(.06f,.075f,.085f);camera.clearFlags=CameraClearFlags.SolidColor;camera.farClipPlane=1500;
                var light=new GameObject("Key Light",typeof(Light)).GetComponent<Light>();
                light.type=LightType.Directional;light.intensity=2.0f;light.color=new Color(1,.94f,.86f);light.shadows=LightShadows.Soft;
                light.transform.rotation=Quaternion.Euler(40,-35,0);
                RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.48f,.52f,.56f);
                var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.name="Review Ground";ground.transform.localScale=Vector3.one*50;
                string groundPath=Root+"/Materials/ReviewGround.mat";
                var gm=AssetDatabase.LoadAssetAtPath<Material>(groundPath);
                if(gm==null){gm=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(gm,groundPath);}
                gm.SetColor("_BaseColor",new Color(.12f,.16f,.15f));ground.GetComponent<Renderer>().sharedMaterial=gm;
                var instances=new List<GameObject>();
                for(int i=0;i<100;i++)
                {
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    go.name="Review Town Hall "+(i+1).ToString("D3");
                    go.transform.position=new Vector3((i%10)*24,0,(i/10)*18);
                    instances.Add(go);go.SetActive(i==0);
                }
                Aim(camera,new Vector3(0,8,0),new Vector3(28,25,48),13);
                var renderer0=instances[0].GetComponent<LODGroup>().GetLODs()[0].renderers[0];
                instances[0].GetComponent<LODGroup>().ForceLOD(0);
                renderer0.sharedMaterial=high4K;Capture(camera,"single_4K.png");
                renderer0.sharedMaterial=materials[0];Capture(camera,"single_2K.png");
                for(int i=0;i<3;i++){instances[0].GetComponent<LODGroup>().ForceLOD(i);Capture(camera,"single_LOD"+i+".png");}
                for(int i=0;i<100;i++)instances[i].SetActive(true);
                Aim(camera,new Vector3(108,0,81),new Vector3(240,300,400),145);
                // Save an authored, inspectable scene. All objects exist in the hierarchy.
                foreach(var go in instances)go.GetComponent<LODGroup>().ForceLOD(-1);
                EditorSceneManager.SaveScene(scene,Root+"/TownHall_100_Review.unity");
                Capture(camera,"city_auto_LOD.png");
                foreach(int count in new[]{25,100})
                {
                    for(int i=0;i<100;i++)instances[i].SetActive(i<count);
                    for(int lod=0;lod<3;lod++)
                    {
                        foreach(var go in instances.Take(count))go.GetComponent<LODGroup>().ForceLOD(lod);
                        var sample=Measure(camera,count,lod);
                        var planes=GeometryUtility.CalculateFrustumPlanes(camera);
                        sample.visibleInstances=instances.Take(count).Count(g=>GeometryUtility.TestPlanesAABB(planes,g.GetComponent<LODGroup>().GetLODs()[lod].renderers[0].bounds));
                        receipt.samples.Add(sample);
                        if(count==100)Capture(camera,"city_forced_LOD"+lod+".png");
                    }
                }
                foreach(var go in instances){go.SetActive(true);go.GetComponent<LODGroup>().ForceLOD(-1);}
                AssetDatabase.SaveAssets();
                receipt.status="passed_import_prefab_and_render_experiment";
                File.WriteAllText(ReportFolder+"/unity_review.json",JsonUtility.ToJson(receipt,true));
            }
            catch(Exception e){receipt.status="failed: "+e;File.WriteAllText(ReportFolder+"/unity_review.json",JsonUtility.ToJson(receipt,true));throw;}
            finally { if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        static void ImportTexture(string path,Receipt receipt)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            bool normal=path.Contains("Normal_");
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=path.Contains("BaseColor");importer.mipmapEnabled=true;importer.isReadable=false;
            importer.maxTextureSize=path.Contains("_4K")?4096:2048;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;
            importer.alphaSource=TextureImporterAlphaSource.FromInput;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Standalone",overridden=true,maxTextureSize=importer.maxTextureSize,format=normal?TextureImporterFormat.BC5:TextureImporterFormat.BC7,compressionQuality=100});
            importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            receipt.textures.Add(new TextureRecord{path=path,format=texture.format.ToString(),width=texture.width,height=texture.height,runtimeBytes=Profiler.GetRuntimeMemorySizeLong(texture)});
        }
        static Material MaterialFor(int target,bool fourK)
        {
            string path=Root+"/Materials/TownHall_"+target+(fourK?"_4K":"_2K")+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            string prefix=Root+"/Source/TownHall_"+target;
            mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(prefix+"_BaseColor_"+(fourK?"4K":"2K")+".png"));
            mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(prefix+"_Normal_"+(fourK?"4K":"2K")+".png"));
            mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(prefix+"_MetallicSmoothness.png"));
            mat.SetFloat("_Smoothness",1);mat.SetFloat("_Metallic",1);mat.SetFloat("_BumpScale",1);
            mat.EnableKeyword("_NORMALMAP");mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.enableInstancing=true;
            EditorUtility.SetDirty(mat);return mat;
        }
        static GameObject CreatePrefab(GameObject[] models,Material[] mats)
        {
            var root=new GameObject("WT_TownHall_3D_Review_LOD");
            try
            {
                Transform Child(string n){var g=new GameObject(n);g.transform.SetParent(root.transform,false);return g.transform;}
                var visual=Child("VisualRoot");var interaction=Child("InteractionAnchor");var worker=Child("WorkerAnchor");var input=Child("DeliveryInputAnchor");var output=Child("DeliveryOutputAnchor");var vfx=Child("VfxAnchor");var colliders=Child("ColliderRoot");
                root.AddComponent<BuildingVisualWrapper>().EditorSetStructure("town_hall","T_FIXED",visual,interaction,worker,input,output,vfx,colliders);
                interaction.localPosition=new Vector3(0,0,6.2f);worker.localPosition=new Vector3(0,0,7);input.localPosition=new Vector3(-10,0,0);output.localPosition=new Vector3(10,0,0);vfx.localPosition=new Vector3(0,16,0);
                var lods=new LOD[3];float[] thresholds={.28f,.10f,.015f};
                for(int i=0;i<3;i++)
                {
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(models[i]);go.name="LOD"+i;go.transform.SetParent(visual,false);
                    var renderers=go.GetComponentsInChildren<Renderer>();foreach(var r in renderers)r.sharedMaterial=mats[i];
                    lods[i]=new LOD(thresholds[i],renderers);
                }
                var lodGroup=root.AddComponent<LODGroup>();lodGroup.fadeMode=LODFadeMode.None;lodGroup.SetLODs(lods);lodGroup.RecalculateBounds();
                var box=colliders.gameObject.AddComponent<BoxCollider>();box.center=new Vector3(0,3,0);box.size=new Vector3(20,6,11.3f);
                var tower=new GameObject("Tower Collider",typeof(BoxCollider));tower.transform.SetParent(colliders,false);
                tower.GetComponent<BoxCollider>().center=new Vector3(0,10,0);tower.GetComponent<BoxCollider>().size=new Vector3(6,13,6);
                return PrefabUtility.SaveAsPrefabAsset(root,Root+"/WT_TownHall_3D_Review_LOD.prefab");
            }
            finally{Object.DestroyImmediate(root);}
        }
        static void Aim(Camera cam,Vector3 target,Vector3 offset,float size){cam.transform.position=target+offset;cam.transform.LookAt(target);cam.orthographicSize=size;}
        static void Render(Camera cam,RenderTexture rt)
        {
            var request=new RenderPipeline.StandardRequest{destination=rt};
            if(GraphicsSettings.currentRenderPipeline==null){cam.targetTexture=rt;cam.Render();cam.targetTexture=null;}
            else RenderPipeline.SubmitRenderRequest(cam,request);
        }
        static void Capture(Camera cam,string name)
        {
            var rt=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32);
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            var old=RenderTexture.active;
            try{Render(cam,rt);RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(ReportFolder+"/"+name,image.EncodeToPNG());}
            finally{RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(image);}
        }
        static Sample Measure(Camera cam,int count,int lod)
        {
            var rt=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32);
            var readback=new Texture2D(1,1,TextureFormat.RGB24,false);var old=RenderTexture.active;var samples=new List<double>();
            try
            {
                for(int i=0;i<25;i++){var watch=Stopwatch.StartNew();Render(cam,rt);RenderTexture.active=rt;readback.ReadPixels(new Rect(0,0,1,1),0,0,false);watch.Stop();if(i>=5)samples.Add(watch.Elapsed.TotalMilliseconds);}
                samples.Sort();return new Sample{instances=count,lod=lod,triangles=count*ExpectedTriangles[lod],medianMs=(samples[9]+samples[10])/2,p95Ms=samples[18]};
            }
            finally{RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(readback);}
        }
    }
}
