using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using Weedtopia.Buildings.Presentation;

namespace Weedtopia.Editor.BuildingArtPipeline
{
    [Serializable]
    public sealed class BuildingValidationIssue
    {
        public string severity;
        public string slot;
        public string message;
    }

    [Serializable]
    public sealed class BuildingValidationReport
    {
        public string generatedUtc;
        public int expectedSlots = 251;
        public int manifestSlots;
        public int wrapperSlots;
        public int catalogSlots;
        public int assignedModels;
        public int referenceImages;
        public int errors;
        public int warnings;
        public bool readOnly = true;
        public List<BuildingValidationIssue> issues = new List<BuildingValidationIssue>();
        public void Add(string severity, string key, string message)
        {
            issues.Add(new BuildingValidationIssue { severity = severity, slot = key, message = message });
            if (severity == "error") errors++; else warnings++;
        }
    }

    /// <summary>Read-only validation: no refresh, importer changes, asset writes, scene changes or HTTP.</summary>
    public static class BuildingIntakeValidator
    {
        public static BuildingValidationReport Validate(BuildingArtManifest manifest, bool includeProductionFiles = true)
        {
            var report = new BuildingValidationReport { generatedUtc = DateTime.UtcNow.ToString("O"), manifestSlots = manifest?.slots?.Length ?? 0 };
            foreach (string error in BuildingManifestReader.ValidateSchema(manifest)) report.Add("error", "manifest", error);
            if (report.errors > 0) return report;
            BuildingVisualCatalog catalog = AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(BuildingManifestReader.CatalogPath);
            if (catalog == null) report.Add("error", "catalog", "BuildingVisualCatalog is missing. Run the scaffold explicitly.");
            var imageHashes = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (BuildingArtSlot slot in manifest.slots)
            {
                if (includeProductionFiles) ValidateProductionFiles(slot, report, imageHashes);
                ValidateSlot(slot, catalog, report);
            }
            if (catalog != null)
            {
                var families = catalog.Families.Where(f => f != null).ToArray();
                foreach (var duplicate in families.GroupBy(f => f.BuildingId).Where(g => g.Count() > 1))
                    report.Add("error", "catalog", "Duplicate family: " + duplicate.Key);
                foreach (BuildingVisualFamily family in families)
                    foreach (var duplicate in family.Bands.Where(b => b != null).GroupBy(b => b.TierId).Where(g => g.Count() > 1))
                        report.Add("error", "catalog", "Duplicate tier: " + family.BuildingId + "/" + duplicate.Key);
                int count = families.Sum(f => f.Bands.Count);
                if (count != 251) report.Add("error", "catalog", "Catalog contains " + count + " bands; expected 251. Extra/manual entries were preserved.");
            }
            return report;
        }

        public static void ValidateSlot(BuildingArtSlot slot, BuildingVisualCatalog catalog, BuildingValidationReport report)
        {
            if (!AssetDatabase.IsValidFolder(slot.unity_incoming_folder)) report.Add("error", slot.Key, "Incoming folder missing.");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(slot.unity_prefab_target);
            if (prefab == null) { report.Add("error", slot.Key, "Wrapper prefab missing."); return; }
            report.wrapperSlots++;
            BuildingVisualWrapper wrapper = prefab.GetComponent<BuildingVisualWrapper>();
            if (wrapper == null) { report.Add("error", slot.Key, "Wrapper component missing."); return; }
            if (wrapper.BuildingId != slot.building_id || wrapper.TierId != slot.tier_id) report.Add("error", slot.Key, "Wrapper identity does not match manifest.");
            if (prefab.name != "WT_" + slot.building_id + "_" + slot.tier_id) report.Add("warning", slot.Key, "Wrapper root has a noncanonical name; retained for manual review.");
            Transform[] anchors = { wrapper.VisualRoot, wrapper.InteractionAnchor, wrapper.WorkerAnchor, wrapper.DeliveryInputAnchor,
                wrapper.DeliveryOutputAnchor, wrapper.VfxAnchor, wrapper.ColliderRoot };
            if (anchors.Any(t => t == null || t == prefab.transform || !t.IsChildOf(prefab.transform))) report.Add("error", slot.Key, "Required anchor is missing or outside the wrapper.");
            if (anchors.Where(t => t != null).Distinct().Count() != anchors.Length) report.Add("error", slot.Key, "Required anchor references are not distinct.");
            BuildingVisualFamily family = catalog?.Families.FirstOrDefault(f => f != null && f.BuildingId == slot.building_id);
            BuildingVisualBand band = family?.Bands.FirstOrDefault(b => b != null && b.TierId == slot.tier_id);
            if (band == null || band.WrapperPrefab == null) report.Add("error", slot.Key, "Catalog reference missing.");
            else
            {
                report.catalogSlots++;
                if (band.WrapperPrefab != prefab) report.Add("warning", slot.Key, "Catalog points to a different manually assigned prefab; assignment preserved.");
                if (band.MinLevel != slot.min_level || band.MaxLevel != slot.max_level) report.Add("error", slot.Key, "Catalog level band differs from manifest.");
            }
            if (wrapper.SourceModel == null || wrapper.ManagedModel == null)
            {
                report.Add("warning", slot.Key, "No imported model assigned. Empty serialized slot is prepared; geometry/material/device QA is pending.");
                return;
            }
            report.assignedModels++;
            if (wrapper.ManagedModel.parent != wrapper.VisualRoot) report.Add("error", slot.Key, "Managed model is not directly beneath VisualRoot.");
            string sourcePath = AssetDatabase.GetAssetPath(wrapper.SourceModel);
            if (sourcePath.IndexOf(slot.building_id, StringComparison.OrdinalIgnoreCase) < 0 || sourcePath.IndexOf(slot.tier_id, StringComparison.OrdinalIgnoreCase) < 0)
                report.Add("warning", slot.Key, "Source path does not identify the expected family and tier: " + sourcePath);
            if (!sourcePath.StartsWith(slot.unity_incoming_folder + "/", StringComparison.Ordinal))
                report.Add("warning", slot.Key, "Source is outside this slot's Incoming folder. Check manual assignment.");
            Renderer[] renderers = wrapper.ManagedModel.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) report.Add("warning", slot.Key, "Assigned model contains no renderers.");
            var materials = new HashSet<Material>();
            foreach (Renderer renderer in renderers)
            {
                Material[] shared = renderer.sharedMaterials;
                if (shared.Length == 0 || shared.Any(m => m == null)) report.Add("warning", slot.Key, "Empty material assignment on " + renderer.name + ".");
                foreach (Material material in shared)
                {
                    if (material == null) continue;
                    materials.Add(material);
                    if (material.shader == null || material.shader.name == "Hidden/InternalErrorShader") report.Add("warning", slot.Key, "Missing/error shader: " + material.name);
                    else if (!material.shader.name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal) &&
                            !material.shader.name.StartsWith("Shader Graphs/", StringComparison.Ordinal) &&
                            !material.shader.name.StartsWith("Weedtopia/Buildings/", StringComparison.Ordinal))
                        report.Add("warning", slot.Key, "Verify shader compatibility with URP manually: " + material.shader.name);
                }
            }
            if (materials.Count > 2) report.Add("warning", slot.Key, "Model uses " + materials.Count + " distinct materials; target is one, at most two for glass.");
            if (TryGetBounds(wrapper, out Bounds bounds))
            {
                if (bounds.size.x > slot.footprint_m_x * 1.15f || bounds.size.z > slot.footprint_m_z * 1.15f || bounds.size.y > slot.height_m * 1.5f ||
                    bounds.size.x < 0.05f || bounds.size.z < 0.05f || bounds.size.y < 0.02f)
                    report.Add("warning", slot.Key, "Implausible model bounds in wrapper meters: " + bounds.size.ToString("F2") + ". Correct VisualRoot and verify scale.");
                if (Mathf.Abs(bounds.min.y) > Mathf.Max(0.1f, slot.height_m * 0.05f)) report.Add("warning", slot.Key, "Model does not rest at root ground Y=0; check VisualRoot pivot correction.");
                if (Mathf.Abs(bounds.center.x) > slot.footprint_m_x * 0.15f || Mathf.Abs(bounds.center.z) > slot.footprint_m_z * 0.15f)
                    report.Add("warning", slot.Key, "Model is not approximately centered on the ground pivot.");
            }
            else report.Add("warning", slot.Key, "No usable mesh bounds; inspect assigned source.");
            report.Add("warning", slot.Key, "Front +Z and up +Y require visual inspection; geometry cannot identify the semantic entrance reliably. No rotation was applied.");
            if (wrapper.ColliderRoot != null && wrapper.ColliderRoot.GetComponentsInChildren<Collider>(true).Length == 0)
                report.Add("warning", slot.Key, "ColliderRoot is empty. Simplified collider authoring and QA remain manual.");
        }

        private static bool TryGetBounds(BuildingVisualWrapper wrapper, out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;
            foreach (MeshFilter filter in wrapper.ManagedModel.GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh != null) Encapsulate(filter.sharedMesh.bounds, wrapper.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix, ref bounds, ref hasBounds);
            foreach (SkinnedMeshRenderer renderer in wrapper.ManagedModel.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                Encapsulate(renderer.localBounds, wrapper.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix, ref bounds, ref hasBounds);
            return hasBounds;
        }

        private static void Encapsulate(Bounds local, Matrix4x4 matrix, ref Bounds bounds, ref bool hasBounds)
        {
            for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++) for (int z = 0; z < 2; z++)
            {
                var p = new Vector3(x == 0 ? local.min.x : local.max.x, y == 0 ? local.min.y : local.max.y, z == 0 ? local.min.z : local.max.z);
                p = matrix.MultiplyPoint3x4(p);
                if (!hasBounds) { bounds = new Bounds(p, Vector3.zero); hasBounds = true; } else bounds.Encapsulate(p);
            }
        }

        private static void ValidateProductionFiles(BuildingArtSlot slot, BuildingValidationReport report, IDictionary<string, string> hashes)
        {
            foreach (string path in BuildingManifestReader.Paths(slot))
            {
                string absolute = BuildingManifestReader.AbsolutePath(path);
                if (!File.Exists(absolute) && !Directory.Exists(absolute)) report.Add("error", slot.Key, "Missing path: " + path);
            }
            CheckPrompt(slot.Key, slot.meshy_prompt_file, report);
            CheckPrompt(slot.Key, slot.texture_prompt_file, report);
            string imagePath = BuildingManifestReader.AbsolutePath(slot.reference_image);
            if (!File.Exists(imagePath)) return;
            try
            {
                byte[] bytes = File.ReadAllBytes(imagePath);
                byte[] pngSignature = { 137, 80, 78, 71, 13, 10, 26, 10 };
                if (bytes.Length < 24 || !bytes.Take(8).SequenceEqual(pngSignature)) { report.Add("error", slot.Key, "Reference is not a PNG file."); return; }
                int width = BigEndianInt(bytes, 16), height = BigEndianInt(bytes, 20);
                if (width < 1024 || height < 1024 || width != height) report.Add("error", slot.Key, "Reference must be square and at least 1024×1024, found " + width + "×" + height + ".");
                using (SHA256 sha = SHA256.Create())
                {
                    string hash = BitConverter.ToString(sha.ComputeHash(bytes));
                    if (hashes.TryGetValue(hash, out string previous)) report.Add("error", slot.Key, "Duplicate image content with " + previous);
                    else hashes.Add(hash, slot.Key);
                }
                report.referenceImages++;
            }
            catch (IOException ex) { report.Add("error", slot.Key, "Image read failed: " + ex.Message); }
        }

        private static int BigEndianInt(byte[] bytes, int offset) =>
            (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];

        private static void CheckPrompt(string key, string path, BuildingValidationReport report)
        {
            string absolute = BuildingManifestReader.AbsolutePath(path);
            if (!File.Exists(absolute)) return;
            string prompt = File.ReadAllText(absolute);
            if (string.IsNullOrWhiteSpace(prompt) || prompt.Length > 800) report.Add("error", key, "Prompt must contain 1–800 characters including whitespace: " + path + " (" + prompt.Length + ").");
        }
    }
}
