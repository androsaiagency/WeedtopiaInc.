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
    /// <summary>Explicit scene authoring only. Runtime presentation reuses the existing town actor clock.</summary>
    public static class TownVitalityAuthoring
    {
        public const string Folder = "Assets/Weedtopia/Production/TownVitality";
        private static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        private static readonly List<LivingTownPresentation.Actor> residents = new List<LivingTownPresentation.Actor>();
        private static TownVitalityStamp stamp;
        private static Transform people;
        private static int decorationIndex;
        private static readonly Vector2[] ParkWalk = {
            new Vector2(-38,-146), new Vector2(-63,-146), new Vector2(-93,-154),
            new Vector2(-127,-166), new Vector2(-134,-187), new Vector2(-126,-218),
            new Vector2(-95,-235), new Vector2(-68,-229), new Vector2(-38,-224)
        };

        [MenuItem("Weedtopia/Production/Author Town Vitality")]
        public static void Apply()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(GameScaffold.GameScene);
                if (Object.FindFirstObjectByType<TownVitalityStamp>() != null) return;
                Build();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
            }
            finally { if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        // Deliberate iteration command: deletes only this tool's marked hierarchy and its actor entries.
        public static void RebuildOwned()
        {
            if (!UnityEngine.Application.isBatchMode) throw new InvalidOperationException("Owned batch editor required");
            BurningHillsLayoutInstaller.RequireSafeEditor();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var previous = Object.FindFirstObjectByType<TownVitalityStamp>();
                var living = Object.FindFirstObjectByType<LivingTownPresentation>();
                if (previous != null)
                {
                    living.actors = living.actors.Where(a => !a.anchor.IsChildOf(previous.transform)).ToArray();
                    Object.DestroyImmediate(previous.gameObject);
                }
                Build();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
            }
            finally { if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        private static Transform Group(string name, Transform parent)
        { var t = new GameObject(name).transform; t.SetParent(parent, false); return t; }

        private static Material Material(string name, float sway)
        {
            string path = Folder + "/Materials/" + name + ".mat";
            var value = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (value == null)
            {
                value = new Material(Shader.Find("Weedtopia/LivingFoliage")) { name = name };
                AssetDatabase.CreateAsset(value, path);
            }
            value.SetFloat("_Sway", sway);
            EditorUtility.SetDirty(value);
            return value;
        }

        private static void ImportArt()
        {
            foreach (string sub in new[] { "Prefabs", "Materials", "Meshes" })
                BuildingIntakeService.EnsureAssetFolder(Folder + "/" + sub);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var manifest = JsonUtility.FromJson<NaturalEnvironmentAuthoring.ArtManifest>(File.ReadAllText(Folder + "/art_manifest.json"));
            var still = Material("PaintedTownDetails", 0);
            var breeze = Material("GardenBreeze", .007f);
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
                importer.spritesheet = new[] { new SpriteMetaData {
                    name = item.id, rect = new Rect(item.x,item.y,item.width,item.height),
                    alignment = 9, pivot = new Vector2(.5f,0)
                }};
#pragma warning restore 618
                importer.SaveAndReimport();
                var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single();
                var instance = new GameObject("Town_" + item.id);
                var artwork = new GameObject("PaintedArtwork", typeof(SpriteRenderer));
                artwork.transform.SetParent(instance.transform, false);
                artwork.transform.localRotation = Quaternion.Euler(35.264f,45,0);
                artwork.transform.localScale = Vector3.one * (item.worldHeight / sprite.bounds.size.y);
                var renderer = artwork.GetComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sharedMaterial = item.foliage ? breeze : still;
                renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                prefabs.Add(item.id, PrefabUtility.SaveAsPrefabAsset(instance, Folder + "/Prefabs/" + instance.name + ".prefab"));
                Object.DestroyImmediate(instance);
            }
        }

        private static void Place(Transform group, string id, float x, float y, float scale = 1, bool mirror = false)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[id], group);
            instance.name = (++decorationIndex).ToString("D3") + "_" + id;
            instance.transform.position = DistrictMap.ToWorld(new Vector2(x,y));
            instance.transform.localScale = Vector3.one * scale;
            var artwork = instance.GetComponentInChildren<SpriteRenderer>();
            // The painted shader reflects geometry, not the SpriteRenderer flip flag.
            if (mirror) { var s = artwork.transform.localScale; s.x = -s.x; artwork.transform.localScale = s; }
            artwork.sortingOrder = WorldSort.At(instance.transform.position);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(artwork);
            PrefabUtility.RecordPrefabInstancePropertyModifications(artwork.transform);
        }

        private static void Footpath(string name, Transform parent, Vector2[] points)
        {
            var surface = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            surface.transform.SetParent(parent, false);
            surface.GetComponent<MeshFilter>().sharedMesh = NaturalEnvironmentAuthoring.AuthorGardenPath(Folder + "/Meshes/" + name + ".asset", points);
            var renderer = surface.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(NaturalEnvironmentAuthoring.Folder + "/Materials/GardenEarth.mat");
            renderer.sortingOrder = -32655;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static void Clearing(string name, Transform parent, float x, float y, float width, float depth)
        {
            var surface = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            surface.transform.SetParent(parent, false);
            surface.GetComponent<MeshFilter>().sharedMesh = NaturalEnvironmentAuthoring.AuthorGardenClearing(
                Folder + "/Meshes/" + name + ".asset", new Vector2(x,y), width, depth);
            var renderer = surface.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(NaturalEnvironmentAuthoring.Folder + "/Materials/GardenEarth.mat");
            renderer.sortingOrder = -32656;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static void Resident(string id, Zone zone, string building, float speed, params Vector2[] points)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LivingHillsAuthoring.Folder + "/Prefabs/Living_" + id + "_0.prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, people);
            instance.name = "Neighbor_" + residents.Count.ToString("D2") + "_" + id;
            instance.transform.position = DistrictMap.ToWorld(points[0]);
            var animation = instance.GetComponent<DirectionalSpriteAnimator>();
            if (animation == null) throw new InvalidOperationException("Directional resident prefab required: " + id);
            residents.Add(new LivingTownPresentation.Actor {
                anchor = instance.transform, artwork = animation.artwork, animation = animation,
                frames = animation.animationSet.views[0].frames.Take(2).ToArray(),
                path = points.Select(p => DistrictMap.ToWorld(p)).ToArray(),
                zone = zone, requiredBuilding = building, speed = speed, bob = 0,
                phase = residents.Count * 13.7f, restSeconds = 4 + residents.Count % 5
            });
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        }

        private static void VaryStreetTrees()
        {
            var landscape=Object.FindFirstObjectByType<NaturalEnvironmentStamp>();
            var originalTrees=landscape.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(r=>r.sprite!=null && (r.sprite.name=="maple" || r.sprite.name=="birch"))
                .Select(r=>r.transform.parent.gameObject).Distinct().ToArray();
            var archive=new List<GameObject>();
            var varied=Group("VariedStreetTrees",stamp.transform);
            foreach(var site in Object.FindFirstObjectByType<GameRoot>().Sites.Where(s=>
                s.InstanceId.StartsWith("coffee_",StringComparison.Ordinal) ||
                s.InstanceId.StartsWith("settlement_",StringComparison.Ordinal)))
            {
                int index=int.Parse(site.InstanceId.Substring(site.InstanceId.LastIndexOf('_')+1))-1;
                int column=index%3, row=index/3;
                if(column==0)continue;
                var point=DistrictMap.ToPlan(site.transform.position)+new Vector2(-16,13);
                var original=originalTrees.Single(t=>Vector2.Distance(DistrictMap.ToPlan(t.transform.position),point)<.1f);
                // Keep the original authored instance and its prefab reference available for restoration.
                original.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(original);
                archive.Add(original);
                Place(varied,(row+column)%3==0?"blossom_tree":"rowan_tree",point.x,point.y,.6f+(row%3)*.055f,column==2);
            }
            stamp.preservedStreetTrees=archive.ToArray();
        }

        private static void Build()
        {
            ImportArt(); residents.Clear(); decorationIndex = 0;
            stamp = Group("TownVitality", GameObject.Find("BurningHills_Districts").transform).gameObject.AddComponent<TownVitalityStamp>();
            people = Group("NeighborsOnConnectedPaths", stamp.transform);
            var park = Group("WestNeighborhoodPark", stamp.transform);
            Footpath("ParkPromenade", park, ParkWalk);
            Clearing("MarketStallCourt",park,-67,-139,24,20);
            Clearing("BreadCartCourt",park,-97,-145,18,18);
            Clearing("ReadingCourt",park,-143,-176,18,18);
            Clearing("TeaCourt",park,-111,-183,23,20);
            Clearing("PlayCourt",park,-109,-208,32,27);
            Footpath("MarketStallAccess",park,new[]{new Vector2(-67,-148),new Vector2(-67,-139)});
            Footpath("BreadCartAccess",park,new[]{new Vector2(-95,-155),new Vector2(-97,-145)});
            Footpath("ReadingAccess",park,new[]{new Vector2(-132,-177),new Vector2(-143,-176)});
            Footpath("TeaAccess",park,new[]{new Vector2(-134,-185),new Vector2(-122,-185),new Vector2(-111,-183)});
            Footpath("PlayAccess",park,new[]{new Vector2(-129,-210),new Vector2(-117,-210),new Vector2(-109,-208)});
            Place(park,"produce_stall",-67,-136);
            Place(park,"bread_cart",-97,-141,.95f,true);
            Place(park,"flower_bicycle",-83,-161,.9f);
            Place(park,"reading_corner",-143,-173,1.1f);
            Place(park,"tea_patio",-111,-181,1.15f);
            Place(park,"play_corner",-110,-207,1.3f);
            Place(park,"picnic_nook",-82,-209,1.2f);
            Place(park,"birdbath",-90,-189,.95f);
            Place(park,"herb_garden",-59,-244,1.15f);
            Place(park,"reading_corner",-72,-240,.85f,true);
            Place(park,"blossom_tree",-158,-142,1.2f);
            Place(park,"rowan_tree",-155,-217,1.12f,true);
            Place(park,"blossom_tree",-99,-265,1.1f,true);
            Place(park,"rowan_tree",-57,-179,.8f);
            // Low border plantings avoid hiding the shops or the walking route.
            var borders = new[] {
                new Vector2(-51,-135),new Vector2(-80,-129),new Vector2(-112,-139),new Vector2(-149,-158),
                new Vector2(-149,-190),new Vector2(-145,-231),new Vector2(-122,-239),new Vector2(-88,-249),
                new Vector2(-56,-235),new Vector2(-57,-211),new Vector2(-69,-190),new Vector2(-101,-195)
            };
            for (int i=0;i<borders.Length;i++)
                Place(park,i%2==0?"fern_garden":"poppy_meadow",borders[i].x,borders[i].y,.85f+(i%3)*.12f,i%2==0);

            foreach(var patch in new[]{new Vector2(-78,-136),new Vector2(-91,-130),new Vector2(-108,-133),
                new Vector2(-151,-172),new Vector2(-149,-180),new Vector2(-118,-176),new Vector2(-103,-179),
                new Vector2(-103,-222),new Vector2(-92,-219),new Vector2(-80,-200),new Vector2(-87,-204),
                new Vector2(-119,-205),new Vector2(-66,-229),new Vector2(-140,-206)})
                Place(park,decorationIndex%3==0?"fern_garden":"poppy_meadow",patch.x,patch.y,.65f+(decorationIndex%4)*.13f,decorationIndex%2==0);

            var civic = Group("CivicMarketAndShopGardens", stamp.transform);
            Place(civic,"bread_cart",-13,36,.88f);
            Place(civic,"flower_bicycle",67,36,.8f,true);
            Place(civic,"birdbath",4,28,.75f);
            Place(civic,"reading_corner",80,13,.82f);
            for (int row=0;row<4;row++)
            {
                float y=-58-row*52;
                Place(civic,row%2==0?"herb_garden":"picnic_nook",17,y-7,.75f,row%2==0);
                Place(civic,row%2==0?"poppy_meadow":"fern_garden",50,y-6,.65f);
                Place(civic,"fern_garden",89,y+22,.65f,true);
            }
            Place(civic,"flower_bicycle",90,-94,.7f);
            Place(civic,"bread_cart",89,-201,.72f,true);
            Place(civic,"tea_patio",-16,-244,.9f);
            Place(civic,"reading_corner",43,-248,.9f);

            var homes = Group("IndividualHomeGardens", stamp.transform);
            for (int row=0;row<6;row++)
            {
                float y=96+row*52;
                string[] details={"herb_garden","laundry_garden","birdbath","picnic_nook","herb_garden","reading_corner"};
                Place(homes,details[row],17,y-7,row==1?.55f:.68f,row%2==1);
                Place(homes,row%2==0?"poppy_meadow":"fern_garden",50,y-6,.7f,row%3==0);
                Place(homes,"poppy_meadow",88,y+14,.75f);
            }
            Clearing("PlayGardenCourt",homes,-63,293,23,20);
            Clearing("TeaGardenCourt",homes,-62,266,19,17);
            Clearing("LaundryCourt",homes,-65,130,21,17);
            // Pocket gardens along the west village walk, outside house and freight footprints.
            Place(homes,"play_corner",-63,299,1.1f);
            Place(homes,"tea_patio",-62,271);
            Place(homes,"laundry_garden",-63,135,.9f);
            Place(homes,"herb_garden",-79,134);
            Place(homes,"apiary",-72,348,.95f);
            Place(homes,"reading_corner",-63,375,.95f,true);
            Place(homes,"blossom_tree",-94,311,1.1f);
            Place(homes,"rowan_tree",-92,265,1.1f);
            Place(homes,"blossom_tree",-97,143,.9f);
            Footpath("PlayGardenApproach",homes,new[]{new Vector2(-38,291),new Vector2(-51,290),new Vector2(-63,290)});
            Footpath("TeaGardenApproach",homes,new[]{new Vector2(-38,266),new Vector2(-51,263),new Vector2(-62,263)});
            Footpath("LaundryGardenApproach",homes,new[]{new Vector2(-38,129),new Vector2(-50,127),new Vector2(-64,127)});

            var industry = Group("WorkshopCourts", stamp.transform);
            for (int row=0;row<4;row++)
            {
                float y=110-row*52;
                Place(industry,row%2==0?"workshop_corner":"bread_cart",-147,y-5,row%2==0?.85f:.7f,row%2==1);
                Place(industry,row%2==0?"herb_garden":"workshop_corner",-185,y-5,.75f);
                Place(industry,"poppy_meadow",-222,y+7,.8f);
            }
            Clearing("WorkshopCourt",industry,-70,16,29,23);
            Clearing("BakeryBreakCourt",industry,-71,72,27,22);
            Footpath("WorkshopAccess",industry,new[]{new Vector2(-38,16),new Vector2(-52,16),new Vector2(-70,16)});
            Footpath("BakeryBreakAccess",industry,new[]{new Vector2(-38,71),new Vector2(-52,71),new Vector2(-71,72)});
            Place(industry,"workshop_corner",-80,19,1.05f);
            Place(industry,"bread_cart",-66,22,.82f,true);
            Place(industry,"herb_garden",-56,9,.8f);
            Place(industry,"tea_patio",-74,74,1.05f);
            Place(industry,"reading_corner",-59,71,.85f,true);
            Place(industry,"flower_bicycle",-85,67,.8f);
            Place(industry,"fern_garden",-89,28,.9f);
            Place(industry,"poppy_meadow",-53,84,1.15f);
            Place(industry,"produce_stall",-73,-65,.85f);
            Place(industry,"flower_bicycle",-91,-62,.75f,true);

            var farm = Group("OrchardAndMeadowDetails", stamp.transform);
            Place(farm,"apiary",396,-149);
            Place(farm,"apiary",510,-147,.9f,true);
            Place(farm,"picnic_nook",455,-161,1.1f);
            Place(farm,"blossom_tree",422,-154,1.15f);
            Place(farm,"rowan_tree",490,-159,1.08f);
            Place(farm,"blossom_tree",543,-152,1.03f,true);
            Place(farm,"herb_garden",285,-104,.8f);
            Place(farm,"flower_bicycle",242,-97,.78f,true);
            for(int i=0;i<12;i++)
                Place(farm,i%3==0?"fern_garden":"poppy_meadow",337+i*18,-151+Mathf.Sin(i*2.1f)*11,.72f+(i%3)*.12f,i%2==0);

            // Planted verges frame the town without introducing another repeating row of identical trees.
            var trees = new[] {new Vector2(-73,-18),new Vector2(-89,57),new Vector2(-80,324),
                new Vector2(128,279),new Vector2(125,202),new Vector2(128,114),new Vector2(127,-193),
                new Vector2(-169,-248),new Vector2(-176,-121),new Vector2(-176,-185)};
            for(int i=0;i<trees.Length;i++)
                Place(stamp.transform,i%2==0?"blossom_tree":"rowan_tree",trees[i].x,trees[i].y,.8f+(i%3)*.1f,i%2==1);
            for(int i=0;i<trees.Length;i++)
                Place(stamp.transform,i%2==0?"fern_garden":"poppy_meadow",trees[i].x+(i==0?-15:8),trees[i].y-8,.8f);

            for(int row=0;row<4;row++)
            {
                float y=-80-row*52;string site="coffee_"+(row*3+1).ToString("D2");
                Resident("resident_b",Zone.City,site,1.3f,new Vector2(16,y),new Vector2(54,y),new Vector2(98,y));
                Resident("resident_a",Zone.City,site,1.7f,new Vector2(89,y-1),new Vector2(48,y-1),new Vector2(12,y-1));
            }
            for(int row=0;row<6;row++)
            {
                float y=78+row*52;string site="settlement_"+(row*3+1).ToString("D2");
                Resident(row%2==0?"resident_a":"resident_c",Zone.Settlement,site,1.25f,new Vector2(-23,y),new Vector2(22,y-1),new Vector2(75,y+2),new Vector2(99,y));
                Resident("resident_b",Zone.Settlement,site,1.6f,new Vector2(85,y+1),new Vector2(50,y),new Vector2(11,y-1));
            }
            for(int i=0;i<4;i++) Resident(new[]{"resident_a","resident_b","resident_c","resident_b"}[i],Zone.City,"coffee_01",1.2f+i*.18f,ParkWalk);
            Resident("resident_b",Zone.City,"town_hall",1.3f,new Vector2(-30,48),new Vector2(12,48),new Vector2(70,48));
            Resident("resident_c",Zone.City,"town_hall",1.5f,new Vector2(93,46),new Vector2(59,46),new Vector2(4,46));
            Resident("resident_a",Zone.Settlement,"settlement_01",1.35f,new Vector2(-38,270),new Vector2(-38,291),new Vector2(-51,290),new Vector2(-63,290));
            Resident("resident_b",Zone.Settlement,"settlement_01",1.2f,new Vector2(-38,248),new Vector2(-38,266),new Vector2(-51,263),new Vector2(-62,263));
            for(int row=0;row<4;row++)
                Resident(row%2==0?"resident_c":"resident_b",Zone.Industry,"bakery_01",1.45f,
                    new Vector2(-211,91-row*52),new Vector2(-166,91-row*52),new Vector2(-117,91-row*52));
            Resident("resident_a",Zone.Farm,"farm_processing",1.45f,new Vector2(251,-110),new Vector2(260,-127),new Vector2(280,-147));
            Resident("resident_b",Zone.Farm,"farm_processing",1.2f,new Vector2(280,-147),new Vector2(263,-133),new Vector2(251,-110));
            VaryStreetTrees();
            var living = Object.FindFirstObjectByType<LivingTownPresentation>();
            var farmWalk = living.actors.First(a => !a.airborne && a.requiredBuilding == "farm_processing" && !a.anchor.name.Contains("cyclist")).path;
            foreach(var neighbor in residents.Where(a => a.zone == Zone.Farm))
            {
                neighbor.path = farmWalk.ToArray(); neighbor.anchor.position = neighbor.path[0];
                PrefabUtility.RecordPrefabInstancePropertyModifications(neighbor.anchor);
            }
            living.actors = living.actors.Concat(residents).ToArray();
            EditorUtility.SetDirty(living);
            stamp.uniqueAssets = prefabs.Count; stamp.decorations = decorationIndex; stamp.residents = residents.Count;
            EditorUtility.SetDirty(stamp);
            Directory.CreateDirectory("Docs/TownVitalityRevision");
            File.WriteAllText("Docs/TownVitalityRevision/integration.json",JsonUtility.ToJson(new Receipt {
                utc=DateTime.UtcNow.ToString("O"),uniqueAssets=stamp.uniqueAssets,
                decorations=stamp.decorations,addedResidents=stamp.residents,totalActors=living.actors.Length,
                variedStreetTrees=stamp.preservedStreetTrees.Length,
                authoredRoot=stamp.name,scene=GameScaffold.GameScene
            },true));
        }

        [Serializable] private sealed class Receipt
        { public string utc,authoredRoot,scene; public int uniqueAssets,decorations,addedResidents,totalActors,variedStreetTrees; }
    }
}

