using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Weedtopia.Buildings.Presentation;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.BuildingArtPipeline
{
    public sealed class BuildingScaffoldResult
    {
        public int wrappersCreated;
        public int wrappersRepaired;
        public int wrappersUnchanged;
        public bool catalogChanged;
    }

    public static class BuildingIntakeService
    {
        [MenuItem("Weedtopia/Art Pipeline/Create or Repair Building Intake Scaffold")]
        public static void CreateOrRepairScaffoldMenu()
        {
            try
            {
                BuildingScaffoldResult result = CreateOrRepairScaffold(BuildingManifestReader.Load());
                Debug.Log($"Building scaffold: {result.wrappersCreated} created, {result.wrappersRepaired} repaired, " +
                          $"{result.wrappersUnchanged} unchanged. Catalog changed: {result.catalogChanged}.");
            }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        public static BuildingScaffoldResult CreateOrRepairScaffold(BuildingArtManifest manifest, string isolatedDestinationRoot = null)
        {
            List<string> errors = BuildingManifestReader.ValidateSchema(manifest);
            if (errors.Count != 0) throw new InvalidDataException(string.Join("\n", errors));
            string catalogPath = BuildingManifestReader.CatalogPath;
            // A separate destination supports safe staging and isolated integration tests.
            // Validate the canonical manifest before translating only Unity output paths.
            if (isolatedDestinationRoot != null)
            {
                EnsureAssetFolder(isolatedDestinationRoot);
                if (isolatedDestinationRoot == "Assets/Weedtopia") throw new ArgumentException("Use null for the canonical destination.");
                manifest = JsonUtility.FromJson<BuildingArtManifest>(JsonUtility.ToJson(manifest));
                foreach (BuildingArtSlot slot in manifest.slots)
                {
                    slot.unity_incoming_folder = isolatedDestinationRoot + "/Art/Buildings/Incoming/" + slot.Key;
                    slot.unity_prefab_target = isolatedDestinationRoot + "/Art/Buildings/Prefabs/" + slot.building_id + "/WT_" + slot.building_id + "_" + slot.tier_id + ".prefab";
                }
                catalogPath = isolatedDestinationRoot + "/Data/Buildings/BuildingVisualCatalog.asset";
            }
            var result = new BuildingScaffoldResult();
            foreach (BuildingArtSlot slot in manifest.slots)
            {
                EnsureAssetFolder(slot.unity_incoming_folder);
                EnsureAssetFolder(Path.GetDirectoryName(slot.unity_prefab_target).Replace('\\', '/'));
                bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(slot.unity_prefab_target) != null;
                bool changed = EnsureWrapper(slot);
                if (!existed) result.wrappersCreated++;
                else if (changed) result.wrappersRepaired++;
                else result.wrappersUnchanged++;
            }
            result.catalogChanged = RepairCatalog(manifest, catalogPath);
            return result;
        }

        public static void EnsureAssetFolder(string path)
        {
            BuildingManifestReader.AbsolutePath(path);
            if (!path.StartsWith("Assets/Weedtopia/", StringComparison.Ordinal) && path != "Assets/Weedtopia")
                throw new InvalidDataException("Intake may only create folders under Assets/Weedtopia: " + path);
            string[] parts = path.Split('/');
            string parent = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                string next = parent + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    if (File.Exists(BuildingManifestReader.AbsolutePath(next))) throw new IOException("Folder blocked by file: " + next);
                    string guid = AssetDatabase.CreateFolder(parent, parts[i]);
                    if (string.IsNullOrEmpty(guid)) throw new IOException("Could not create folder: " + next);
                }
                parent = next;
            }
        }

        public static bool EnsureWrapper(BuildingArtSlot slot)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(slot.unity_prefab_target);
            if (asset == null && File.Exists(BuildingManifestReader.AbsolutePath(slot.unity_prefab_target)))
                throw new IOException("Existing non-prefab target is protected: " + slot.unity_prefab_target);
            bool isNew = asset == null;
            GameObject root = isNew ? new GameObject("WT_" + slot.building_id + "_" + slot.tier_id) :
                PrefabUtility.LoadPrefabContents(slot.unity_prefab_target);
            try
            {
                bool changed = EnsureStructure(root, slot);
                if (changed || isNew) SavePrefab(root, slot.unity_prefab_target);
                return changed || isNew;
            }
            finally
            {
                if (isNew) Object.DestroyImmediate(root);
                else PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool EnsureStructure(GameObject root, BuildingArtSlot slot)
        {
            BuildingVisualWrapper wrapper = root.GetComponent<BuildingVisualWrapper>();
            bool changed = wrapper == null;
            if (wrapper == null) wrapper = root.AddComponent<BuildingVisualWrapper>();
            if ((!string.IsNullOrEmpty(wrapper.BuildingId) && wrapper.BuildingId != slot.building_id) ||
                (!string.IsNullOrEmpty(wrapper.TierId) && wrapper.TierId != slot.tier_id))
                throw new InvalidDataException("Existing wrapper identity mismatch; preserved without saving: " + slot.Key);
            Transform visual = EnsureTransform(root.transform, wrapper.VisualRoot, "VisualRoot", ref changed);
            Transform interaction = EnsureTransform(root.transform, wrapper.InteractionAnchor, "InteractionAnchor", ref changed);
            Transform worker = EnsureTransform(root.transform, wrapper.WorkerAnchor, "WorkerAnchor", ref changed);
            Transform input = EnsureTransform(root.transform, wrapper.DeliveryInputAnchor, "DeliveryInputAnchor", ref changed);
            Transform output = EnsureTransform(root.transform, wrapper.DeliveryOutputAnchor, "DeliveryOutputAnchor", ref changed);
            Transform vfx = EnsureTransform(root.transform, wrapper.VfxAnchor, "VfxAnchor", ref changed);
            Transform colliders = EnsureTransform(root.transform, wrapper.ColliderRoot, "ColliderRoot", ref changed);
            if (wrapper.BuildingId != slot.building_id || wrapper.TierId != slot.tier_id) changed = true;
            if (changed) wrapper.EditorSetStructure(slot.building_id, slot.tier_id, visual, interaction, worker, input, output, vfx, colliders);
            return changed;
        }

        private static Transform EnsureTransform(Transform root, Transform existing, string name, ref bool changed)
        {
            if (existing != null)
            {
                if (!existing.IsChildOf(root) || existing == root) throw new InvalidDataException("Wrapper anchor outside root: " + name);
                return existing;
            }
            Transform[] matches = root.Cast<Transform>().Where(t => t.name == name).ToArray();
            if (matches.Length > 1) throw new InvalidDataException("Ambiguous existing child: " + name);
            changed = true;
            if (matches.Length == 1) return matches[0];
            var child = new GameObject(name);
            child.transform.SetParent(root, false);
            return child.transform;
        }

        public static void AssignModel(BuildingArtSlot slot, GameObject source)
        {
            string sourcePath = AssetDatabase.GetAssetPath(source);
            if (source == null || !EditorUtility.IsPersistent(source) || !PrefabUtility.IsPartOfPrefabAsset(source) ||
                !sourcePath.StartsWith("Assets/", StringComparison.Ordinal) ||
                AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath) != source)
                throw new ArgumentException("Choose the root of an imported model/prefab asset under Assets, not a scene object or model child.");
            if (sourcePath.StartsWith("Assets/Weedtopia/Art/Buildings/Prefabs/", StringComparison.Ordinal))
                throw new ArgumentException("A building wrapper cannot serve as a source model.");
            EnsureAssetFolder(Path.GetDirectoryName(slot.unity_prefab_target).Replace('\\', '/'));
            EnsureWrapper(slot);
            GameObject root = PrefabUtility.LoadPrefabContents(slot.unity_prefab_target);
            try
            {
                BuildingVisualWrapper wrapper = root.GetComponent<BuildingVisualWrapper>();
                if (wrapper.SourceModel == source && wrapper.ManagedModel != null) return;
                if (wrapper.ManagedModel == null && wrapper.VisualRoot.childCount > 0)
                    throw new InvalidOperationException("VisualRoot already contains untracked authored children. Preserve/adopt them manually before assigning a managed model.");
                ReplaceManagedModel(wrapper, source);
                SavePrefab(root, slot.unity_prefab_target);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private sealed class LocalTransform
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            public bool positionOverride = true;
            public bool rotationOverride = true;
            public bool scaleOverride = true;
            public bool HasOverride => positionOverride || rotationOverride || scaleOverride;
            public LocalTransform(Transform t, Transform source = null)
            {
                position = t.localPosition; rotation = t.localRotation; scale = t.localScale;
                if (source == null) return;
                positionOverride = position != source.localPosition;
                rotationOverride = Quaternion.Angle(rotation, source.localRotation) > 0.0001f;
                scaleOverride = scale != source.localScale;
            }
            public void Apply(Transform t)
            {
                if (positionOverride) t.localPosition = position;
                if (rotationOverride) t.localRotation = rotation;
                if (scaleOverride) t.localScale = scale;
            }
        }

        private sealed class AuthoredChild
        {
            public Transform transform;
            public string parentPath;
            public LocalTransform local;
        }

        // Indexed paths preserve transforms even when source meshes use duplicate sibling names.
        private static string RelativePath(Transform root, Transform target)
        {
            if (target == root) return "";
            var segments = new List<string>();
            while (target != root)
            {
                int index = 0;
                for (int i = 0; i < target.GetSiblingIndex(); i++) if (target.parent.GetChild(i).name == target.name) index++;
                segments.Add(Uri.EscapeDataString(target.name) + "#" + index);
                target = target.parent;
            }
            segments.Reverse();
            return string.Join("/", segments);
        }

        private static void ReplaceManagedModel(BuildingVisualWrapper wrapper, GameObject source)
        {
            Transform oldModel = wrapper.ManagedModel;
            if (oldModel != null && oldModel.parent != wrapper.VisualRoot)
                throw new InvalidOperationException("Managed model must remain directly under VisualRoot; no content was replaced.");
            var transforms = new Dictionary<string, LocalTransform>(StringComparer.Ordinal);
            var authoredChildren = new List<AuthoredChild>();
            if (oldModel != null)
            {
                foreach (Transform child in oldModel.GetComponentsInChildren<Transform>(true))
                {
                    Transform sourceTransform = PrefabUtility.GetCorrespondingObjectFromSource(child);
                    if (child == oldModel) transforms[""] = new LocalTransform(child);
                    else if (sourceTransform != null)
                    {
                        var overrides = new LocalTransform(child, sourceTransform);
                        if (overrides.HasOverride) transforms[RelativePath(oldModel, child)] = overrides;
                    }
                    if (child != oldModel && sourceTransform == null &&
                        (child.parent == oldModel || PrefabUtility.GetCorrespondingObjectFromSource(child.parent.gameObject) != null))
                        authoredChildren.Add(new AuthoredChild { transform = child, parentPath = RelativePath(oldModel, child.parent), local = new LocalTransform(child) });
                }
            }
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source, wrapper.VisualRoot);
            if (instance == null) throw new InvalidOperationException("Could not instantiate source model into wrapper.");
            try
            {
                var newTransforms = instance.GetComponentsInChildren<Transform>(true)
                    .ToDictionary(t => RelativePath(instance.transform, t), t => t, StringComparer.Ordinal);
                foreach (AuthoredChild child in authoredChildren)
                    if (!newTransforms.ContainsKey(child.parentPath))
                        throw new InvalidOperationException("Replacement lacks parent for a manually added child. Original wrapper preserved; review child: " + child.transform.name);
                foreach (string path in transforms.Keys)
                    if (!newTransforms.ContainsKey(path))
                        throw new InvalidOperationException("Replacement lacks a child with manual transform overrides. Original wrapper preserved; review path: " + path);
                foreach (KeyValuePair<string, LocalTransform> pair in transforms)
                    if (newTransforms.TryGetValue(pair.Key, out Transform target)) pair.Value.Apply(target);
                foreach (AuthoredChild child in authoredChildren)
                {
                    child.transform.SetParent(newTransforms[child.parentPath], false);
                    child.local.Apply(child.transform);
                }
                if (oldModel != null) Object.DestroyImmediate(oldModel.gameObject);
                wrapper.EditorSetModel(source, instance.transform);
            }
            catch
            {
                // Contents are unloaded by the caller without saving. The asset remains untouched.
                Object.DestroyImmediate(instance);
                throw;
            }
        }

        private static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved);
            if (!saved) throw new IOException("Could not save wrapper prefab: " + path);
        }

        public static bool RepairCatalog(BuildingArtManifest manifest, string catalogPath = BuildingManifestReader.CatalogPath)
        {
            EnsureAssetFolder(Path.GetDirectoryName(catalogPath).Replace('\\', '/'));
            BuildingVisualCatalog catalog = AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(catalogPath);
            bool created = catalog == null;
            if (created && File.Exists(BuildingManifestReader.AbsolutePath(catalogPath)))
                throw new IOException("Catalog target is occupied by another asset; preserved.");
            if (created) catalog = ScriptableObject.CreateInstance<BuildingVisualCatalog>();
            var families = catalog.Families.ToList();
            bool changed = created;
            foreach (IGrouping<string, BuildingArtSlot> group in manifest.slots.GroupBy(s => s.building_id))
            {
                int familyIndex = families.FindIndex(f => f != null && f.BuildingId == group.Key);
                var bands = familyIndex < 0 ? new List<BuildingVisualBand>() : families[familyIndex].Bands.ToList();
                bool familyChanged = familyIndex < 0;
                foreach (BuildingArtSlot slot in group.OrderBy(s => s.min_level))
                {
                    int index = bands.FindIndex(b => b != null && b.TierId == slot.tier_id);
                    BuildingVisualBand existing = index < 0 ? null : bands[index];
                    GameObject prefab = existing?.WrapperPrefab != null ? existing.WrapperPrefab :
                        AssetDatabase.LoadAssetAtPath<GameObject>(slot.unity_prefab_target);
                    if (existing != null && existing.MinLevel == slot.min_level && existing.MaxLevel == slot.max_level && existing.WrapperPrefab == prefab) continue;
                    var band = new BuildingVisualBand(slot.tier_id, slot.min_level, slot.max_level, prefab);
                    if (index < 0) bands.Add(band); else bands[index] = band;
                    familyChanged = true;
                }
                if (!familyChanged) continue;
                var family = new BuildingVisualFamily(group.Key, bands.OrderBy(b => b?.MinLevel ?? int.MaxValue).ToList());
                if (familyIndex < 0) families.Add(family); else families[familyIndex] = family;
                changed = true;
            }
            if (!changed) return false;
            catalog.EditorSetFamilies(families);
            if (created) AssetDatabase.CreateAsset(catalog, catalogPath);
            else { EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssetIfDirty(catalog); }
            return true;
        }
    }

    public static class TownHallMaterialRepairUtility
    {
        public const string ShaderName = "Weedtopia/Buildings/Palette Clean";
        public const string MaterialFolder = "Assets/Weedtopia/Art/Buildings/Materials/TownHall";
        public const string MaterialPath = MaterialFolder + "/WT_TownHall_PaletteClean.mat";
        public const string IncomingFolder = "Assets/Weedtopia/Art/Buildings/Incoming/town_hall/T_FIXED/Meshy_v3_Base";
        public const string WrapperPath = "Assets/Weedtopia/Art/Buildings/Prefabs/town_hall/WT_town_hall_T_FIXED.prefab";
        public const string SourceModelPath = IncomingFolder + "/Meshy_AI_Solar_Forge_Citadel_0917201315_texture_fbx/Meshy_AI_Solar_Forge_Citadel_0917201315_texture.fbx";

        [MenuItem("Weedtopia/Art Pipeline/Town Hall/Create or Refresh Clean Palette Material")]
        public static void CreateOrRefreshMaterialMenu()
        {
            try
            {
                Material material = CreateOrRefreshMaterial();
                EditorGUIUtility.PingObject(material);
                Debug.Log("Town hall clean palette material ready: " + MaterialPath);
            }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        [MenuItem("Weedtopia/Art Pipeline/Town Hall/Apply Clean Palette Material to Wrapper")]
        public static void ApplyToWrapperMenu()
        {
            try
            {
                int rendererCount = ApplyToWrapper();
                Debug.Log($"Applied clean town hall palette material to {rendererCount} renderer(s) in {WrapperPath}.");
            }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        public static Material CreateOrRefreshMaterial()
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null) throw new InvalidOperationException("Shader not imported or failed to compile: " + ShaderName);
            if (!shader.isSupported) throw new InvalidOperationException("Shader was imported but is unsupported by the active render pipeline: " + ShaderName);

            BuildingIntakeService.EnsureAssetFolder(MaterialFolder);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                if (File.Exists(BuildingManifestReader.AbsolutePath(MaterialPath)))
                    throw new IOException("Material target is occupied by a non-material asset: " + MaterialPath);
                material = new Material(shader) { name = "WT_TownHall_PaletteClean" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else
            {
                var reset = new Material(shader) { name = "WT_TownHall_PaletteClean" };
                EditorUtility.CopySerialized(reset, material);
                Object.DestroyImmediate(reset);
            }

            material.SetColor("_Cream", new Color(0.78f, 0.72f, 0.61f, 1f));
            material.SetColor("_Sage", new Color(0.21f, 0.34f, 0.25f, 1f));
            material.SetColor("_DarkSage", new Color(0.11f, 0.20f, 0.15f, 1f));
            material.SetColor("_Copper", new Color(0.55f, 0.28f, 0.10f, 1f));
            material.SetFloat("_BuildingHeight", 1f);
            material.SetFloat("_GroundOffset", 0f);
            material.SetFloat("_CrownStartFraction", 0.76f);
            material.SetFloat("_LowerTrimFraction", 0.035f);
            material.SetFloat("_RoofNormalThreshold", 0.68f);
            material.SetFloat("_Ambient", 0.62f);
            material.SetFloat("_Directional", 0.38f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        public static int ApplyToWrapper()
        {
            Material material = CreateOrRefreshMaterial();
            GameObject wrapperAsset = AssetDatabase.LoadAssetAtPath<GameObject>(WrapperPath);
            if (wrapperAsset == null) throw new FileNotFoundException("Town hall wrapper is missing: " + WrapperPath);

            GameObject root = PrefabUtility.LoadPrefabContents(WrapperPath);
            try
            {
                BuildingVisualWrapper wrapper = root.GetComponent<BuildingVisualWrapper>();
                if (wrapper == null || wrapper.ManagedModel == null)
                    throw new InvalidOperationException("Assign the imported FBX through Building Tier Intake before applying the clean palette material.");

                Renderer[] renderers = wrapper.ManagedModel.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) throw new InvalidOperationException("The assigned town hall model has no renderers.");
                foreach (Renderer renderer in renderers)
                {
                    Material[] shared = renderer.sharedMaterials;
                    if (shared.Length == 0) shared = new Material[1];
                    for (int i = 0; i < shared.Length; i++) shared[i] = material;
                    renderer.sharedMaterials = shared;
                }
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                material.SetFloat("_BuildingHeight", Mathf.Max(bounds.size.y, 0.001f));
                material.SetFloat("_GroundOffset", bounds.min.y - renderers[0].transform.position.y);
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssetIfDirty(material);
                PrefabUtility.SaveAsPrefabAsset(root, WrapperPath, out bool saved);
                if (!saved) throw new IOException("Could not save town hall wrapper: " + WrapperPath);
                return renderers.Length;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

    }

    /// <summary>
    /// Executes only when the explicit Temp request marker exists. This lets an external production
    /// handoff finish after Unity notices the downloaded FBX, while keeping normal editor starts inert.
    /// </summary>
    [InitializeOnLoad]
    public static class TownHallDownloadedModelIntakeRequest
    {
        private const string RequestPath = "Temp/WeedtopiaTownHallIntake.request";
        private const string ReceiptPath = "Docs/ArtProduction/Buildings/town_hall/T_FIXED/unity_intake_receipt.json";
        private const string SessionAttemptKey = "Weedtopia.TownHallDownloadedModelIntakeRequest.Attempted";

        static TownHallDownloadedModelIntakeRequest()
        {
            EditorApplication.delayCall += TryExecute;
        }

        private static void TryExecute()
        {
            string request = BuildingManifestReader.AbsolutePath(RequestPath);
            if (!File.Exists(request) || SessionState.GetBool(SessionAttemptKey, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryExecute;
                return;
            }

            SessionState.SetBool(SessionAttemptKey, true);
            try
            {
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(TownHallMaterialRepairUtility.SourceModelPath);
                if (source == null) throw new FileNotFoundException("Imported Meshy FBX is not available as a Unity model asset.", TownHallMaterialRepairUtility.SourceModelPath);

                Shader shader = Shader.Find(TownHallMaterialRepairUtility.ShaderName);
                if (shader == null || !shader.isSupported)
                    throw new InvalidOperationException("Clean town hall palette shader is missing or unsupported.");

                BuildingArtManifest manifest = BuildingManifestReader.Load();
                BuildingArtSlot slot = manifest.slots.Single(s => s.building_id == "town_hall" && s.tier_id == "T_FIXED");
                BuildingIntakeService.AssignModel(slot, source);
                int rendererCount = TownHallMaterialRepairUtility.ApplyToWrapper();
                BuildingIntakeService.RepairCatalog(manifest);

                BuildingVisualCatalog catalog = AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(BuildingManifestReader.CatalogPath);
                var report = new BuildingValidationReport
                {
                    generatedUtc = DateTime.UtcNow.ToString("O"),
                    expectedSlots = 1,
                    manifestSlots = 1
                };
                BuildingIntakeValidator.ValidateSlot(slot, catalog, report);
                File.WriteAllText(BuildingManifestReader.AbsolutePath(ReceiptPath), JsonUtility.ToJson(report, true));
                File.Delete(request);
                AssetDatabase.SaveAssets();

                GameObject wrapper = AssetDatabase.LoadAssetAtPath<GameObject>(TownHallMaterialRepairUtility.WrapperPath);
                Selection.activeObject = wrapper;
                EditorGUIUtility.PingObject(wrapper);
                Debug.Log($"Town hall FBX integrated with clean palette material on {rendererCount} renderer(s). " +
                          $"Validation: {report.errors} error(s), {report.warnings} warning(s). Receipt: {ReceiptPath}");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError("Town hall intake request was preserved for a retry after the reported issue is fixed: " + RequestPath);
            }
        }
    }

    public static class TownHallPreviewCaptureUtility
    {
        public const string FrontPreviewPath = "Docs/ArtProduction/Buildings/town_hall/T_FIXED/unity_preview_front_3q.png";
        public const string RearPreviewPath = "Docs/ArtProduction/Buildings/town_hall/T_FIXED/unity_preview_rear_3q.png";

        [MenuItem("Weedtopia/Art Pipeline/Town Hall/Capture Unity Material Previews")]
        public static void CaptureMenu()
        {
            Capture();
            Debug.Log("Town hall Unity previews captured: " + FrontPreviewPath + " and " + RearPreviewPath);
        }

        public static void Capture()
        {
            GameObject wrapper = AssetDatabase.LoadAssetAtPath<GameObject>(TownHallMaterialRepairUtility.WrapperPath);
            if (wrapper == null) throw new FileNotFoundException("Town hall wrapper is missing.", TownHallMaterialRepairUtility.WrapperPath);
            CaptureView(wrapper, new Vector3(1f, 0.72f, 1f), FrontPreviewPath);
            CaptureView(wrapper, new Vector3(-1f, 0.72f, -1f), RearPreviewPath);
        }

        private static void CaptureView(GameObject wrapper, Vector3 viewDirection, string outputPath)
        {
            const int size = 1024;
            var preview = new PreviewRenderUtility();
            try
            {
                GameObject instance = Object.Instantiate(wrapper);
                instance.hideFlags = HideFlags.HideAndDontSave;
                preview.AddSingleGO(instance);

                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) throw new InvalidOperationException("Town hall wrapper contains no renderers to preview.");
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

                float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                float distance = Mathf.Max(1f, largest * 2f);
                Camera camera = preview.camera;
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(bounds.extents.y * 1.18f, Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.05f);
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = distance * 4f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.83f, 0.81f, 0.76f, 1f);
                camera.transform.position = bounds.center + viewDirection.normalized * distance;
                camera.transform.LookAt(bounds.center, Vector3.up);
                preview.ambientColor = new Color(0.65f, 0.65f, 0.65f, 1f);
                preview.lights[0].intensity = 1.1f;
                preview.lights[0].transform.rotation = Quaternion.Euler(35f, 35f, 0f);
                preview.lights[1].intensity = 0.45f;
                preview.lights[1].transform.rotation = Quaternion.Euler(340f, 210f, 0f);

                preview.BeginStaticPreview(new Rect(0f, 0f, size, size));
                camera.Render();
                Texture2D image = preview.EndStaticPreview();
                if (image == null) throw new InvalidOperationException("Unity returned no town hall preview texture.");
                try { File.WriteAllBytes(BuildingManifestReader.AbsolutePath(outputPath), image.EncodeToPNG()); }
                finally { Object.DestroyImmediate(image); }
            }
            finally { preview.Cleanup(); }
        }
    }

    [InitializeOnLoad]
    public static class TownHallPreviewCaptureRequest
    {
        private const string RequestPath = "Temp/WeedtopiaTownHallPreview.request";
        private const string SessionAttemptKey = "Weedtopia.TownHallPreviewCaptureRequest.Attempted.V3";

        static TownHallPreviewCaptureRequest()
        {
            EditorApplication.delayCall += TryExecute;
        }

        private static void TryExecute()
        {
            string request = BuildingManifestReader.AbsolutePath(RequestPath);
            if (!File.Exists(request) || SessionState.GetBool(SessionAttemptKey, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryExecute;
                return;
            }

            SessionState.SetBool(SessionAttemptKey, true);
            try
            {
                TownHallMaterialRepairUtility.ApplyToWrapper();
                TownHallPreviewCaptureUtility.Capture();
                File.Delete(request);
                Debug.Log("Town hall Unity material previews captured for visual QA.");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError("Town hall preview request was preserved for a retry: " + RequestPath);
            }
        }
    }
}
