using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Content;
using Weedtopia.Domain;
using Weedtopia.Unity;
using Weedtopia.Editor.BuildingArtPipeline;
using Object=UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit, scoped road conversion. Keeps existing sites, assets and manual scene references.</summary>
    public static class BakeryDeliveryAuthoring
    {
        public const string Folder="Assets/Weedtopia/Production/BakeryDelivery";
        [Serializable] private sealed class Receipt { public string utc; public int roads,vehicleViews; public bool conveyorsRemoved; }
        [MenuItem("Weedtopia/Production/Apply Bakery Pickup Roads")]
        public static void Apply()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            BuildingIntakeService.EnsureAssetFolder(Folder+"/Meshes");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(GameScaffold.GameScene);var root=Object.FindFirstObjectByType<GameRoot>();
                var definitions=AssetDatabase.LoadAssetAtPath<GameContent>(GameScaffold.ContentPath).Load();
                var plan=JsonUtility.FromJson<BurningHillsLayoutInstaller.LayoutPlan>(File.ReadAllText(BurningHillsLayoutInstaller.Folder+"/BurningHillsLayout.json"));
                var material=AssetDatabase.LoadAssetAtPath<Material>(NaturalEnvironmentAuthoring.Folder+"/Materials/MossyFlagstones.mat");
                if(material==null)throw new InvalidOperationException("Existing natural road material required.");
                var pickups=definitions.routes.Where(r=>r.pickupByRecipient).ToArray();
                foreach(var definition in pickups)
                {
                    var route=root.Presentation.Route(definition.id);
                    var path=plan.routes.Single(r=>r.id==definition.id);
                    if(route==null)throw new InvalidOperationException("Missing authored road: "+definition.id);
                    route.points=path.points.Select(p=>DistrictMap.ToWorld(p,.045f)).ToArray();
                    route.conveyor=false;
                    if(route.cargoMarkers!=null)foreach(var marker in route.cargoMarkers)if(marker!=null)marker.gameObject.SetActive(false);
                    route.cargoMarkers=Array.Empty<Transform>();
                    route.GetComponent<MeshFilter>().sharedMesh=NaturalEnvironmentAuthoring.AuthorRoadSurface(Folder+"/Meshes/"+definition.id+".asset",route.points);
                    var renderer=route.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=-32500;
                    renderer.SetPropertyBlock(null);EditorUtility.SetDirty(route);
                }
                // The existing pool covered two 24-driver warehouses. Every baker may now be driving too.
                int needed=definitions.instances.Sum(i=>string.IsNullOrEmpty(definitions.Building(i.family).driverRole)?0:24)+pickups.Select(r=>r.to).Distinct().Count();
                var fields=new SerializedObject(root);var vehicles=fields.FindProperty("vehicles");
                var pool=GameObject.Find("DeliveryVisualPool").transform;
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ProductionArtImporter.Root+"/Prefabs/DeliveryTruck.prefab");
                if(prefab==null)throw new InvalidOperationException("Existing delivery van prefab required.");
                while(vehicles.arraySize<needed)
                {
                    int slot=vehicles.arraySize;var van=(GameObject)PrefabUtility.InstantiatePrefab(prefab,pool);
                    van.name="Delivery_"+slot;
                    if(slot>0)van.transform.localScale=((DeliveryView)vehicles.GetArrayElementAtIndex(0).objectReferenceValue).transform.localScale;
                    vehicles.arraySize++;vehicles.GetArrayElementAtIndex(slot).objectReferenceValue=van.GetComponent<DeliveryView>();
                }
                fields.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Docs/BakeryDelivery");
                File.WriteAllText("Docs/BakeryDelivery/authoring.json",JsonUtility.ToJson(new Receipt{utc=DateTime.UtcNow.ToString("O"),roads=pickups.Length,vehicleViews=vehicles.arraySize,conveyorsRemoved=pickups.All(r=>!root.Presentation.Route(r.id).conveyor)},true));
            }
            finally {if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}
