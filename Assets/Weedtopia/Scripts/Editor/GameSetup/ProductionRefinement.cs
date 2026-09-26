using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Unity;
using Object=UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    public static class ProductionRefinement
    {
        [MenuItem("Weedtopia/Production/Apply Reviewed Visual Corrections")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save manual scene changes first");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var art=ProductionArtImporter.Import(true);var scene=EditorSceneManager.OpenScene(GameScaffold.GameScene);var root=Object.FindFirstObjectByType<GameRoot>();var p=root.Presentation;
                string texturePath=ProductionArtImporter.Root+"/Art/Conveyor.png";var textureImporter=(TextureImporter)AssetImporter.GetAtPath(texturePath);textureImporter.mipmapEnabled=true;textureImporter.wrapMode=TextureWrapMode.Repeat;textureImporter.filterMode=FilterMode.Trilinear;textureImporter.anisoLevel=4;textureImporter.textureCompression=TextureImporterCompression.CompressedHQ;textureImporter.SaveAndReimport();
                string beltPath=ProductionArtImporter.Root+"/Materials/ConveyorBelt.mat";var belt=AssetDatabase.LoadAssetAtPath<Material>(beltPath);if(belt==null){belt=new Material(Shader.Find("Weedtopia/AtlasGround"));belt.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));belt.SetVector("_Region",new Vector4(0,0,1,1));belt.SetColor("_Tint",new Color(.87f,.9f,.86f));AssetDatabase.CreateAsset(belt,beltPath);}
                foreach(var route in p.routes.Where(x=>x.conveyor))
                {
                    route.belt.sharedMaterial=belt;
                    foreach(var child in route.transform.Cast<Transform>().Where(x=>x.name.StartsWith("BrassSlat_")).ToArray())Object.DestroyImmediate(child.gameObject);
                    if(route.cargoMarkers==null||route.cargoMarkers.Length==0)
                    {
                        route.cargoMarkers=new Transform[3];var cargo=AssetDatabase.LoadAssetAtPath<GameObject>(ProductionArtImporter.Root+"/Prefabs/Prop_crates.prefab");
                        for(int i=0;i<3;i++){var marker=(GameObject)PrefabUtility.InstantiatePrefab(cargo,route.transform);marker.name="PooledCargo_"+i;marker.transform.localScale=Vector3.one*.45f;marker.transform.position=route.Position((i+.5f)/3);route.cargoMarkers[i]=marker.transform;marker.SetActive(false);}
                    }
                }
                var close=root.Ui.GetComponentsInChildren<Button>(true).Single(x=>x.name=="ui.close");close.GetComponent<RectTransform>().sizeDelta=new Vector2(56,56);close.GetComponentInChildren<Text>(true).enabled=false;
                p.workerName.rectTransform.anchoredPosition=new Vector2(68,-91);p.workerName.rectTransform.sizeDelta=new Vector2(278,74);p.workerName.fontSize=22;
                p.workerRoleIcon.rectTransform.anchoredPosition=new Vector2(190,-35);p.workerRoleIcon.rectTransform.sizeDelta=new Vector2(42,42);
                foreach(var actor in p.characters)
                {
                    Vector3 oldPosition=root.Site(actor.workplaceId).transform.position+new Vector3(-5,0,-3.6f);
                    if(Vector3.Distance(actor.transform.position,oldPosition)<.01f){actor.transform.position=root.Site(actor.workplaceId).transform.position+new Vector3(-6,0,0);PrefabUtility.RecordPrefabInstancePropertyModifications(actor.transform);}
                }
                var workerText=root.Ui.GetComponentsInChildren<Text>(true).Single(x=>x.name=="workerText");workerText.fontSize=18;workerText.rectTransform.sizeDelta=new Vector2(470,168);workerText.rectTransform.anchoredPosition=new Vector2(280,-300);
                foreach(var slider in root.Ui.GetComponentsInChildren<Slider>(true))
                {
                    var handle=slider.handleRect;handle.anchorMin=new Vector2(handle.anchorMin.x,.5f);handle.anchorMax=new Vector2(handle.anchorMax.x,.5f);handle.sizeDelta=new Vector2(42,42);
                    var image=handle.GetComponent<Image>();image.type=Image.Type.Simple;image.preserveAspect=true;
                }
                var nav=root.Ui.GetComponentsInChildren<Text>(true).First(x=>x.transform.parent.name=="ui.pause");nav.fontSize=18;
                foreach(string characterPath in Directory.GetFiles(ProductionArtImporter.Root+"/Prefabs","Character_*.prefab"))
                {
                    var character=PrefabUtility.LoadPrefabContents(characterPath);
                    try{if(character.GetComponent<BoxCollider>()==null){var hit=character.AddComponent<BoxCollider>();hit.center=new Vector3(0,1.5f,0);hit.size=new Vector3(1.4f,3.2f,1.4f);PrefabUtility.SaveAsPrefabAsset(character,characterPath);}}
                    finally{PrefabUtility.UnloadPrefabContents(character);}
                }
                // Keep real IDs in saves; human-readable names appear in presentation.
                var path=GameScaffold.Prefabs+"/BuildingSite.prefab";var prefab=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var old=prefab.transform.Find("Selection");if(old!=null)Object.DestroyImmediate(old.gameObject);
                    string meshPath=ProductionArtImporter.Root+"/Meshes/SelectionOutline.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if(mesh==null)
                    {
                        mesh=new Mesh{name="AuthoredSelectionOutline"};var vertices=new Vector3[128];var triangles=new int[384];
                        for(int i=0;i<64;i++)
                        {
                            float angle=i/64f*Mathf.PI*2;var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));float extent=4.6f/Mathf.Max(Mathf.Abs(direction.x),Mathf.Abs(direction.z));
                            vertices[i*2]=direction*extent+Vector3.up*.065f;vertices[i*2+1]=direction*(extent+.1f)+Vector3.up*.065f;int j=(i+1)%64;int k=i*6;triangles[k]=i*2;triangles[k+1]=j*2;triangles[k+2]=i*2+1;triangles[k+3]=i*2+1;triangles[k+4]=j*2;triangles[k+5]=j*2+1;
                        }
                        mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,meshPath);
                    }
                    string matPath=ProductionArtImporter.Root+"/Materials/SelectionOutline.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",new Color(.92f,.64f,.22f));AssetDatabase.CreateAsset(material,matPath);}
                    var ring=new GameObject("Selection",typeof(MeshFilter),typeof(MeshRenderer));ring.transform.SetParent(prefab.transform,false);ring.GetComponent<MeshFilter>().sharedMesh=mesh;ring.GetComponent<MeshRenderer>().sharedMaterial=material;ring.SetActive(false);
                    var view=new SerializedObject(prefab.GetComponent<BuildingView>());view.FindProperty("selection").objectReferenceValue=ring;view.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(prefab,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(prefab);}
                AddContactShadows();
                string archive=GameScaffold.Root+"/Archive/ReplacedPresentation";
                Weedtopia.Editor.BuildingArtPipeline.BuildingIntakeService.EnsureAssetFolder(archive);
                foreach(string replaced in new[]{GameScaffold.Prefabs+"/Resident.prefab",GameScaffold.Prefabs+"/DeliveryCart.prefab",GameScaffold.Root+"/Audio/development_accept.wav",GameScaffold.Root+"/Audio/development_blocked.wav"})
                {
                    if(!File.Exists(replaced))continue;
                    string destination=archive+"/"+Path.GetFileName(replaced);if(File.Exists(destination))throw new InvalidOperationException("Archive conflict: "+destination);
                    string error=AssetDatabase.MoveAsset(replaced,destination);if(!string.IsNullOrEmpty(error))throw new InvalidOperationException(error);
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
            finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        private static void AddContactShadows()
        {
            string meshPath=ProductionArtImporter.Root+"/Meshes/ContactShadow.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(mesh==null){mesh=new Mesh{name="ContactShadowQuad",vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f)},uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one},triangles=new[]{0,2,1,1,2,3}};mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,meshPath);}
            string materialPath=ProductionArtImporter.Root+"/Materials/ContactShadow.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(material==null){material=new Material(Shader.Find("Weedtopia/ContactShadow"));AssetDatabase.CreateAsset(material,materialPath);}
            foreach(string path in Directory.GetFiles(ProductionArtImporter.Root+"/Prefabs","*.prefab"))
            {
                if(path.EndsWith("DeliveryTruck.prefab"))continue;
                var prefab=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if(prefab.transform.Find("ContactShadow")!=null)continue;
                    var sprite=prefab.GetComponentInChildren<SpriteRenderer>();if(sprite==null)continue;
                    var shadow=new GameObject("ContactShadow",typeof(MeshFilter),typeof(MeshRenderer));shadow.transform.SetParent(prefab.transform,false);shadow.transform.localPosition=Vector3.up*.04f;
                    float width=Mathf.Clamp(sprite.bounds.size.x*.65f,.7f,4);shadow.transform.localScale=new Vector3(width,1,width*.75f);shadow.GetComponent<MeshFilter>().sharedMesh=mesh;shadow.GetComponent<MeshRenderer>().sharedMaterial=material;PrefabUtility.SaveAsPrefabAsset(prefab,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(prefab);}
            }
        }
    }
}
