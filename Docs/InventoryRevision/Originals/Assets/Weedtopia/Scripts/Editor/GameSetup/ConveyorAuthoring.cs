using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit sprite-network migration that preserves all existing logical routes and world objects.</summary>
    public static class ConveyorAuthoring
    {
        public const string Folder = "Assets/Weedtopia/Production/Conveyors";
        public const string SegmentPrefab = Folder + "/Prefabs/ConveyorSpan.prefab";
        public const string JunctionPrefab = Folder + "/Prefabs/ConveyorJunction.prefab";
        public const string CargoPrefab = Folder + "/Prefabs/HarvestTray.prefab";
        private sealed class Span
        {
            public Vector3 from, to;
            public readonly List<PresentationRoute> owners = new List<PresentationRoute>();
        }

        [MenuItem("Weedtopia/Presentation/Install Painted Conveyors")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before authoring.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save manual scene changes first.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var root = Object.FindFirstObjectByType<GameRoot>();
                if (root.Presentation.conveyors != null)
                {
                    if (ClearDecorations() > 0) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                    Validate(root); return;
                }
                EnsureAssets(root);
                var routes = root.Presentation.routes.Where(x => x.conveyor).ToArray();
                var parent = new GameObject("PaintedConveyorNetwork").transform;
                parent.SetParent(routes[0].transform.parent, false);
                var network = parent.gameObject.AddComponent<ConveyorNetwork>();
                var archive = new GameObject("ArchivedConveyorCargo").transform;
                archive.SetParent(parent, false); archive.gameObject.SetActive(false);
                var points = routes.SelectMany(x => x.points).GroupBy(Key).Select(x => x.First()).ToArray();
                var spans = new Dictionary<string, Span>();
                // Split collinear overlaps at every feeder junction, then share the resulting physical span.
                foreach (var route in routes)
                    for (int i = 1; i < route.points.Length; i++)
                    {
                        var a = route.points[i - 1]; var b = route.points[i]; var direction = (b - a).normalized;
                        var onLine = points.Where(p => OnSegment(p, a, b)).OrderBy(p => Vector3.Dot(p - a, direction)).ToArray();
                        for (int j = 1; j < onLine.Length; j++)
                        {
                            string key = Key(onLine[j - 1]) + ":" + Key(onLine[j]);
                            if (!spans.TryGetValue(key, out var span))
                            { span = new Span { from = onLine[j - 1], to = onLine[j] }; spans.Add(key, span); }
                            if (!span.owners.Contains(route)) span.owners.Add(route);
                        }
                    }
                var sections = new List<ConveyorSection>(); int index = 0;
                foreach (var span in spans.Values)
                {
                    var direction = (span.to - span.from).normalized;
                    var a = span.from + direction * 1.7f; var b = span.to - direction * 1.7f;
                    var section = Instance(SegmentPrefab, parent).GetComponent<ConveyorSection>();
                    section.name = "Span_" + index++;
                    section.transform.position = (a + b) * .5f + Vector3.up * .45f;
                    section.transform.rotation = Quaternion.LookRotation(Vector3.Cross(direction, Vector3.up), Vector3.up);
                    section.transform.localScale = new Vector3(Vector3.Distance(a, b), 1, ConveyorNetwork.BeltWidth);
                    section.owners = span.owners.ToArray();
                    section.spanUv = new Vector2(Vector3.Distance(a, b) / 18, Vector3.Dot(a, direction) / 18);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(section.transform);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(section); sections.Add(section);
                }
                index = 0;
                foreach (var point in points)
                {
                    var section = Instance(JunctionPrefab, parent).GetComponent<ConveyorSection>();
                    section.name = "Transfer_" + index++;
                    section.transform.position = point + Vector3.up * .48f;
                    section.transform.localScale = new Vector3(7.2f, 1, 7.2f);
                    section.owners = routes.Where(x => x.points.Any(p => Vector3.Distance(p, point) < .01f) ||
                        Enumerable.Range(1, x.points.Length - 1).Any(i => OnSegment(point, x.points[i - 1], x.points[i]))).ToArray();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(section.transform);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(section); sections.Add(section);
                }
                for (int r = 0; r < routes.Length; r++)
                {
                    var route = routes[r];
                    route.belt.enabled = false; EditorUtility.SetDirty(route.belt);
                    foreach (var old in route.cargoMarkers ?? Array.Empty<Transform>()) { old.SetParent(archive, true); old.gameObject.SetActive(false); }
                    route.cargoMarkers = new Transform[3];
                    for (int i = 0; i < route.cargoMarkers.Length; i++)
                    {
                        var cargo = Instance(CargoPrefab, route.transform); cargo.name = "ConveyorCargo_" + i;
                        cargo.transform.position = route.points[0] + Vector3.up * .85f; cargo.SetActive(false);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(cargo.transform);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(cargo);
                        route.cargoMarkers[i] = cargo.transform;
                    }
                    route.conveyorPath = RoundedPath(route.points, 2.5f); route.spawnDelay = r * .8f; route.cargoSpeed = 3.6f;
                    EditorUtility.SetDirty(route);
                }
                network.sections = sections.ToArray(); network.routes = routes;
                root.Presentation.conveyors = network; EditorUtility.SetDirty(root.Presentation);
                ClearDecorations();
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                Validate(root);
            }
            finally { if (!UnityEngine.Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        private static int ClearDecorations()
        {
            int moved = 0;
            void Move(string prefabName, Vector2 original, Vector2 target)
            {
                var prop = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(t =>
                    PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject) == t.gameObject &&
                    Path.GetFileNameWithoutExtension(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t)) == prefabName &&
                    Vector2.Distance(DistrictMap.ToPlan(t.position), original) < .05f);
                // Match the known authored position, so later manual placement is never overwritten.
                if (prop == null) return;
                prop.position = DistrictMap.ToWorld(target, prop.position.y);
                PrefabUtility.RecordPrefabInstancePropertyModifications(prop);
                foreach (var sprite in prop.GetComponentsInChildren<SpriteRenderer>())
                { sprite.sortingOrder = WorldSort.At(prop.position); PrefabUtility.RecordPrefabInstancePropertyModifications(sprite); }
                moved++;
            }
            Move("Prop_lantern", new Vector2(265, -14), new Vector2(275, -21));
            Move("Landscape_hedge", new Vector2(250, -55), new Vector2(241, -54));
            return moved;
        }

        public static Vector3[] RoundedPath(Vector3[] source, float radius)
        {
            var path = new List<Vector3> { source[0] };
            for (int i = 1; i < source.Length - 1; i++)
            {
                float trim = Mathf.Min(radius, Vector3.Distance(source[i - 1], source[i]) * .25f, Vector3.Distance(source[i], source[i + 1]) * .25f);
                var enter = source[i] + (source[i - 1] - source[i]).normalized * trim;
                var exit = source[i] + (source[i + 1] - source[i]).normalized * trim;
                path.Add(enter);
                for (int step = 1; step <= 12; step++)
                { float t = step / 12f; path.Add((1 - t) * (1 - t) * enter + 2 * t * (1 - t) * source[i] + t * t * exit); }
            }
            path.Add(source[source.Length - 1]); return path.ToArray();
        }
        private static string Key(Vector3 p) => p.x.ToString("F3", CultureInfo.InvariantCulture) + "," + p.z.ToString("F3", CultureInfo.InvariantCulture);
        private static bool OnSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            var delta = b - a; float t = Vector3.Dot(p - a, delta) / delta.sqrMagnitude;
            return t >= -.0001f && t <= 1.0001f && Vector3.Distance(p, a + delta * t) < .01f;
        }
        private static GameObject Instance(string prefab, Transform parent) => (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefab), parent);

        private static void EnsureAssets(GameRoot root)
        {
            foreach (string sub in new[] { "Materials", "Prefabs", "Meshes" }) Directory.CreateDirectory(Folder + "/" + sub);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string image in new[] { "ConveyorStraight", "ConveyorJunction", "HarvestTray" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + "/Art/" + image + ".png");
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 256; importer.alphaIsTransparency = true; importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 8;
                importer.maxTextureSize = 4096; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            string meshPath = Folder + "/Meshes/SharedDeck.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null)
            {
                mesh = new Mesh { name = "ConveyorDeck" };
                mesh.vertices = new[] { new Vector3(-.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(.5f,0,-.5f) };
                mesh.uv = new[] { Vector2.zero,Vector2.up,Vector2.one,Vector2.right }; mesh.triangles = new[] { 0,1,2,0,2,3 };
                mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, meshPath);
            }
            foreach (bool junction in new[] { false, true })
            {
                string name = junction ? "ConveyorJunction" : "ConveyorStraight";
                string materialPath = Folder + "/Materials/" + name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(Shader.Find("Weedtopia/PaintedConveyor"));
                    material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Art/" + name + ".png"));
                    material.SetFloat("_Junction", junction ? 1 : 0); AssetDatabase.CreateAsset(material, materialPath);
                }
                string prefab = junction ? JunctionPrefab : SegmentPrefab;
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefab) != null) continue;
                var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(ConveyorSection));
                obj.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = obj.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
                var section = obj.GetComponent<ConveyorSection>(); section.surface = renderer; section.junction = junction;
                PrefabUtility.SaveAsPrefabAsset(obj, prefab); Object.DestroyImmediate(obj);
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CargoPrefab) == null)
            {
                var obj = new GameObject("HarvestTray"); var art = new GameObject("Artwork", typeof(SpriteRenderer)); art.transform.SetParent(obj.transform, false);
                var sprite = art.GetComponent<SpriteRenderer>();
                sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Art/HarvestTray.png");
                sprite.sharedMaterial = root.Presentation.characters[0].spriteRenderer.sharedMaterial;
                art.transform.rotation = root.CameraRig.RenderCamera.transform.rotation;
                art.transform.localScale = Vector3.one * (3.8f / sprite.sprite.bounds.size.x);
                // Move the billboard toward the camera along its view axis. This preserves its
                // screen position while keeping its lower edge above the deck throughout the bob.
                art.transform.localPosition = -art.transform.forward * 2.2f;
                PrefabUtility.SaveAsPrefabAsset(obj, CargoPrefab); Object.DestroyImmediate(obj);
            }
        }
        private static void Validate(GameRoot root)
        {
            var net = root.Presentation.conveyors;
            if (net == null || net.routes.Length != 9 || net.sections.Any(s => s.surface == null || s.owners.Length == 0)) throw new InvalidOperationException("Incomplete authored conveyor network.");
            Directory.CreateDirectory("Docs/ConveyorRevision");
            File.WriteAllText("Docs/ConveyorRevision/authoring_receipt.json", JsonUtility.ToJson(new Receipt {
                utc = DateTime.UtcNow.ToString("O"), routes = net.routes.Length, spans = net.sections.Count(s => !s.junction), junctions = net.sections.Count(s => s.junction),
                cargoPool = net.routes.Sum(r => r.cargoMarkers.Length), beltWidth = ConveyorNetwork.BeltWidth }, true));
        }
        [Serializable] private sealed class Receipt { public string utc; public int routes, spans, junctions, cargoPool; public float beltWidth; }
    }
}
