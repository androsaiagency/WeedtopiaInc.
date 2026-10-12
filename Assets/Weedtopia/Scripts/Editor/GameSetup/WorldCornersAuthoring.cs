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
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit, additive authoring. All scenery is saved in the scene as reusable prefab instances.</summary>
    public static class WorldCornersAuthoring
    {
        public const string Folder = "Assets/Weedtopia/Production/WorldCorners";
        private const string Report = "Docs/WorldCornersRevision";
        private static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        private static readonly List<LivingTownPresentation.Actor> actors = new List<LivingTownPresentation.Actor>();
        private static readonly List<WorldCornersStamp.Walk> walks = new List<WorldCornersStamp.Walk>();
        private static readonly List<Vector2> entrances = new List<Vector2>();
        private static WorldCornersStamp stamp;
        private static Transform people;
        private static int decorationIndex;
        private static Vector2 P(float x, float y) => new Vector2(x, y);
        private static Transform Group(string name, Transform parent)
        { var t = new GameObject(name).transform; t.SetParent(parent, false); return t; }

        [MenuItem("Weedtopia/Production/Author Countryside Corners")]
        public static void Apply() => Author(false);

        // For deliberate art iteration in the owned batch editor; never touches another author's hierarchy.
        public static void RefineOwned()
        {
            if (!UnityEngine.Application.isBatchMode) throw new InvalidOperationException("Owned batch editor required.");
            Author(true);
        }

        private static void Author(bool replaceOwned)
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var previous = Object.FindFirstObjectByType<WorldCornersStamp>();
                if (previous != null && !replaceOwned) return;
                if (previous != null)
                {
                    var life = Object.FindFirstObjectByType<LivingTownPresentation>();
                    life.actors = life.actors.Where(a => !a.anchor.IsChildOf(previous.transform)).ToArray();
                    Object.DestroyImmediate(previous.gameObject);
                }
                Build();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
            }
            finally { if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        private static void ImportArt()
        {
            foreach (string sub in new[] { "Prefabs", "Meshes" }) BuildingIntakeService.EnsureAssetFolder(Folder + "/" + sub);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var manifest = JsonUtility.FromJson<NaturalEnvironmentAuthoring.ArtManifest>(File.ReadAllText(Folder + "/art_manifest.json"));
            prefabs.Clear();
            foreach (var item in manifest.sprites)
            {
                string path = Folder + "/Art/" + item.file;
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = 100;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Trilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.isReadable = false;
#pragma warning disable 618
                importer.spritesheet = new[] { new SpriteMetaData { name = item.id,
                    rect = new Rect(item.x,item.y,item.width,item.height), alignment = 9, pivot = new Vector2(.5f,0) }};
#pragma warning restore 618
                importer.SaveAndReimport();
                var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single();
                var obj = new GameObject("Countryside_" + item.id);
                var artwork = Group("PaintedArtwork", obj.transform);
                artwork.localRotation = Quaternion.Euler(35.264f,45,0);
                artwork.localScale = Vector3.one * (item.worldHeight / sprite.bounds.size.y);
                var renderer = artwork.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(TownVitalityAuthoring.Folder + "/Materials/" +
                    (item.foliage ? "GardenBreeze" : "PaintedTownDetails") + ".mat");
                renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                prefabs.Add(item.id, PrefabUtility.SaveAsPrefabAsset(obj, Folder + "/Prefabs/" + obj.name + ".prefab"));
                Object.DestroyImmediate(obj);
            }
            stamp.uniqueAssets = prefabs.Count;
        }

        private static void Place(Transform parent, string id, float x, float y, float scale = 1, bool mirror = false)
        {
            if (!prefabs.TryGetValue(id, out var prefab))
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TownVitalityAuthoring.Folder + "/Prefabs/Town_" + id + ".prefab") ??
                    AssetDatabase.LoadAssetAtPath<GameObject>(NaturalEnvironmentAuthoring.Folder + "/Prefabs/Landscape_" + id + ".prefab");
                if (prefab == null) throw new InvalidOperationException("Missing scenery prefab: " + id);
                prefabs[id] = prefab;
            }
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            obj.name = (++decorationIndex).ToString("D3") + "_" + id;
            obj.transform.position = DistrictMap.ToWorld(P(x,y));
            obj.transform.localScale = Vector3.one * scale;
            var sr = obj.GetComponentInChildren<SpriteRenderer>();
            sr.sortingOrder = WorldSort.At(obj.transform.position);
            if (mirror) { var s = sr.transform.localScale; s.x = -s.x; sr.transform.localScale = s; }
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(sr);
            PrefabUtility.RecordPrefabInstancePropertyModifications(sr.transform);
        }

        private static void Surface(string name, Transform parent, Mesh mesh, int order)
        {
            var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(parent, false);
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(NaturalEnvironmentAuthoring.Folder + "/Materials/GardenEarth.mat");
            renderer.sortingOrder = order;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        private static Vector2[] Walk(string name, Transform parent, params Vector2[] points)
        {
            points = SmoothWalk(points);
            Surface(name, parent, NaturalEnvironmentAuthoring.AuthorGardenPath(Folder + "/Meshes/" + name + ".asset", points, 3.8f), -32655);
            walks.Add(new WorldCornersStamp.Walk { name = name, points = points });
            return points;
        }
        // Interpolating curves retain every authored junction; walkers use the same sampled centreline.
        private static Vector2[] SmoothWalk(Vector2[] knots)
        {
            if (knots.Length < 3) return knots;
            bool loop = knots[0] == knots[knots.Length-1];
            var points = new List<Vector2>();
            for (int i=0;i<knots.Length-1;i++)
            {
                var a=knots[i]; var b=knots[i+1];
                var previous=i>0?knots[i-1]:loop?knots[knots.Length-2]:a;
                var next=i+2<knots.Length?knots[i+2]:loop?knots[1]:b;
                var tangentA=(b-previous)*.38f; var tangentB=(next-a)*.38f;
                for(int j=0;j<10;j++)
                {
                    float t=j/10f, t2=t*t, t3=t2*t;
                    points.Add((2*t3-3*t2+1)*a+(t3-2*t2+t)*tangentA+(-2*t3+3*t2)*b+(t3-t2)*tangentB);
                }
            }
            points.Add(knots.Last()); return points.ToArray();
        }
        private static void Court(string name, Transform parent, Vector2 p, float width, float depth)
        {
            Surface(name, parent, NaturalEnvironmentAuthoring.AuthorGardenClearing(Folder + "/Meshes/" + name + ".asset", p, width, depth), -32656);
            entrances.Add(p);
        }
        private static void Actor(string id, Vector2[] points, float phase, bool airborne = false)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LivingHillsAuthoring.Folder + "/Prefabs/Living_" + id + "_0.prefab");
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, people);
            obj.name = "GardenVisitor_" + actors.Count.ToString("D2") + "_" + id;
            var animation = obj.GetComponent<DirectionalSpriteAnimator>();
            float height = airborne ? 1.5f : 0;
            obj.transform.position = DistrictMap.ToWorld(points[0], height);
            actors.Add(new LivingTownPresentation.Actor {
                anchor = obj.transform, artwork = animation.artwork, animation = animation,
                frames = animation.animationSet.views[0].frames.Take(2).ToArray(),
                path = points.Select(p => DistrictMap.ToWorld(p,height)).ToArray(), phase = phase,
                speed = airborne ? .75f : 1.25f + (actors.Count % 3) * .15f,
                restSeconds = 5, bob = 0, airborne = airborne, closedLoop = airborne,
                zone = Zone.Farm, requiredBuilding = null
            });
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
        }

        private static void Build()
        {
            decorationIndex = 0; actors.Clear(); walks.Clear(); entrances.Clear();
            stamp = Group("CountrysideCorners", GameObject.Find("BurningHills_Districts").transform).gameObject.AddComponent<WorldCornersStamp>();
            people = Group("OrchardVisitorsAndGardenWildlife", stamp.transform);
            ImportArt();
            var orchard = Group("West_OrchardAndCiderPress", stamp.transform);
            var approach = Walk("OrchardVillageApproach", orchard, P(-38,306), P(-65,306), P(-86,289), P(-116,293), P(-151,314), P(-174,329));
            var orchardLoop = Walk("OrchardDiscoveryLoop", orchard, P(-174,329), P(-199,337), P(-204,360), P(-195,380),
                P(-174,393), P(-152,390), P(-139,370), P(-146,349), P(-174,329));
            Court("CiderPressCourt",orchard,P(-170,350),32,23);
            Walk("CiderPressEntrance",orchard,P(-146,349),P(-170,350));
            Place(orchard,"cider_press",-168,353);
            Court("HoneyWagonCourt",orchard,P(-196,336),19,17);
            Walk("HoneyWagonAccess",orchard,P(-199,337),P(-196,336));
            Place(orchard,"orchard_wagon",-194,336);
            Court("ApiaryClearing",orchard,P(-182,382),28,20);
            Walk("ApiaryAccess",orchard,P(-195,380),P(-182,382));
            Place(orchard,"apiary",-181,386,1.2f);
            Place(orchard,"apiary",-166,386,.93f,true);
            Place(orchard,"picnic_nook",-216,367,1.15f);
            Place(orchard,"reading_corner",-142,382,.95f,true);
            var orchardTrees = new[] {P(-221,333),P(-224,365),P(-220,404),P(-191,415),P(-159,409),P(-138,400)};
            for(int i=0;i<orchardTrees.Length;i++)
                Place(orchard,i%3==2?"blossom_tree":"orchard_apple",orchardTrees[i].x,orchardTrees[i].y,.88f+(i%3)*.1f,i%2==0);
            Place(orchard,"rowan_tree",-215,310,.9f);
            Place(orchard,"butterfly_garden",-178,319,.85f);
            Place(orchard,"stone_wall",-210,424,1.8f);
            Place(orchard,"stone_wall",-165,430,1.5f,true);
            foreach(var p in new[]{P(-225,387),P(-214,350),P(-180,403),P(-157,366),P(-139,336),P(-195,311),P(-153,411),P(-127,306)})
            {
                Place(orchard,"poppy_meadow",p.x,p.y,1.35f);
                Place(orchard,"fern_garden",p.x+5,p.y-7,.95f,true);
            }
            Actor("resident_b",approach,7);
            Actor("resident_c",orchardLoop,41);
            Actor("resident_a",orchardLoop,111);

            var garden = Group("East_ConservatoryAndLookout", stamp.transform);
            // This trail connects the farm arrival to the existing pond path, then the conservatory.
            var pondLink = Walk("FarmToPondPromenade",garden,P(232,-75),P(225,-40),P(222,8),P(223,62),P(225,109),P(243,131),P(255,158));
            // Preserve the existing pond walk between (255,158), (268,177), (267,205).
            var gardenLink = Walk("PondToBotanicalGarden",garden,P(267,205),P(291,185),P(323,177),P(352,186),P(377,218),P(399,256),P(420,277));
            var gardenLoop = Walk("BotanicalDiscoveryLoop",garden,P(420,277),P(453,285),P(493,291),P(535,317),P(548,333),
                P(526,348),P(482,379),P(449,363),P(415,344),P(404,310),P(420,277));
            Court("ConservatoryForecourt",garden,P(430,322),45,24);
            Walk("ConservatoryEntrance",garden,P(404,310),P(417,316),P(430,322));
            Place(garden,"conservatory",433,328);
            Court("LookoutTerrace",garden,P(526,348),39,25);
            Place(garden,"lookout_pavilion",529,353);
            Court("ButterflyGardenCourt",garden,P(484,325),31,24);
            Walk("ButterflyGardenAccess",garden,P(493,291),P(488,310),P(484,325));
            Walk("ButterflyGardenReturn",garden,P(484,325),P(478,349),P(482,379));
            Place(garden,"butterfly_garden",478,331,1.45f);
            Place(garden,"herb_garden",502,343,1.5f,true);
            Place(garden,"lavender",510,348,1.35f);
            Place(garden,"birdbath",489,327,1.3f);
            Place(garden,"benches",470,314,1.1f);
            Place(garden,"benches",501,312,1.1f,true);
            Court("GardenTeaCourt",garden,P(536,301),28,23);
            Walk("GardenTeaAccess",garden,P(535,317),P(536,301));
            Place(garden,"tea_patio",536,301,1.4f);
            Place(garden,"flower_bicycle",548,307,1.15f,true);
            Place(garden,"reading_corner",515,330,1.1f);
            Place(garden,"herb_garden",409,279,1.4f);
            Place(garden,"herb_garden",447,274,1.15f,true);
            Place(garden,"butterfly_garden",400,339,1.2f);
            Place(garden,"hydrangea",454,389,1.6f);
            Place(garden,"lavender",462,394,1.6f);
            Place(garden,"fern_garden",549,357,1.7f);
            Place(garden,"poppy_meadow",553,363,1.8f);
            var gardenTrees = new[] {P(407,389),P(432,405),P(467,413),P(503,418),P(551,412),P(566,376),P(568,328),P(558,271),P(520,253),P(474,257),P(385,300)};
            for(int i=0;i<gardenTrees.Length;i++)
                Place(garden,new[]{"blossom_tree","rowan_tree","birch"}[i%3],gardenTrees[i].x,gardenTrees[i].y,1.02f+(i%3)*.12f,i%2==1);
            var gardenBorders = new[] {P(394,372),P(422,390),P(450,409),P(482,399),P(513,412),P(547,402),P(565,355),P(565,311),
                P(549,282),P(522,278),P(493,272),P(457,259),P(424,258),P(388,321),P(446,343),P(562,347)};
            for(int i=0;i<gardenBorders.Length;i++)
            {
                var p=gardenBorders[i];
                Place(garden,i%3==0?"hydrangea":i%3==1?"lavender":"fern_garden",p.x,p.y,1.35f+(i%2)*.2f,i%2==0);
                Place(garden,"poppy_meadow",p.x+7,p.y-8,1.4f,i%2==0);
            }
            // Small destinations along the approach make the longer stroll readable.
            Court("PondTrailRest",garden,P(358,195),20,17);
            Walk("PondTrailRestAccess",garden,P(352,186),P(358,195));
            Place(garden,"reading_corner",357,199,1.2f);
            Place(garden,"butterfly_garden",372,196,1.05f);
            Place(garden,"rowan_tree",387,184,1.2f);
            Place(garden,"fern_garden",378,169,1.7f);
            Place(garden,"poppy_meadow",393,218,1.7f);
            Place(garden,"poppy_meadow",400,240,1.45f,true);
            Place(garden,"flowering_shrub",218,71,1.1f);
            Place(garden,"lavender",235,41,1.5f);
            Actor("resident_a",pondLink,22);
            Actor("resident_b",gardenLink,33);
            Actor("resident_c",gardenLoop,21);
            Actor("resident_a",gardenLoop,89);
            Actor("resident_b",gardenLoop,155);

            var meadow = Group("West_MeadowPicnic", stamp.transform);
            var picnic = Walk("ParkToOrchardMeadow",meadow,P(-143,-176),P(-165,-174),P(-189,-157),P(-209,-150));
            Court("MeadowPicnicCourt",meadow,P(-209,-150),27,20);
            Place(meadow,"picnic_nook",-207,-146,1.55f);
            Place(meadow,"orchard_wagon",-193,-139,.9f,true);
            Place(meadow,"orchard_apple",-220,-122,1.1f);
            Place(meadow,"blossom_tree",-196,-119,1.2f);
            Place(meadow,"rowan_tree",-219,-186,1.18f,true);
            Place(meadow,"butterfly_garden",-208,-174,1.25f);
            Place(meadow,"butterfly_garden",-170,-148,.95f,true);
            Place(meadow,"birdbath",-225,-152,.95f);
            Place(meadow,"poppy_meadow",-224,-209,1.7f);
            Place(meadow,"fern_garden",-195,-196,1.4f);
            Actor("resident_c",picnic,5);
            Actor("resident_a",picnic,39);

            foreach(var p in new[]{P(-179,319),P(-193,384),P(478,330),P(501,343),P(400,339),P(-207,-173)})
                Actor("butterfly",new[]{p+P(-3,0),p+P(0,3),p+P(4,1),p+P(1,-3),p+P(-3,0)},actors.Count*2.7f,true);
            var living = Object.FindFirstObjectByType<LivingTownPresentation>();
            living.actors = living.actors.Concat(actors).ToArray();
            EditorUtility.SetDirty(living);
            stamp.decorations = decorationIndex;
            stamp.residents = actors.Count(a=>!a.airborne); stamp.wildlife = actors.Count(a=>a.airborne);
            stamp.walks = walks.ToArray(); stamp.destinationEntrances = entrances.ToArray();
            EditorUtility.SetDirty(stamp);
            Directory.CreateDirectory(Report);
            File.WriteAllText(Report+"/integration.json",JsonUtility.ToJson(new Receipt {
                utc=DateTime.UtcNow.ToString("O"),uniqueAssets=stamp.uniqueAssets,decorations=stamp.decorations,
                residents=stamp.residents,wildlife=stamp.wildlife,walks=walks.Count,destinations=entrances.Count,
                scene=GameScaffold.GameScene,authoredRoot=stamp.name
            },true));
        }
        [Serializable] private sealed class Receipt
        { public string utc,scene,authoredRoot; public int uniqueAssets,decorations,residents,wildlife,walks,destinations; }
    }
}
