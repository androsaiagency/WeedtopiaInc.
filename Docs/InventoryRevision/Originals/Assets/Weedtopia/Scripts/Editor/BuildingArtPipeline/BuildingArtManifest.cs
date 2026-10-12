using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Weedtopia.Editor.BuildingArtPipeline
{
    [Serializable]
    public sealed class BuildingArtManifest
    {
        public int schemaVersion;
        public BuildingArtSlot[] slots;
    }

    [Serializable]
    public sealed class BuildingArtSlot
    {
        public string building_id;
        public string building_name_de;
        public string tier_id;
        public int min_level;
        public int max_level;
        public string era;
        public string visual_name_de;
        public string visible_delta_from_previous;
        public float footprint_m_x;
        public float footprint_m_z;
        public float height_m;
        public int target_faces;
        public int driver_slot_delta;
        public string reference_image;
        public string meshy_prompt_file;
        public string texture_prompt_file;
        public string image_prompt_file;
        public string asset_spec_file;
        public string meshy_settings_file;
        public string unity_incoming_folder;
        public string unity_prefab_target;
        public string status;
        public string Key => building_id + "/" + tier_id;
    }

    public static class BuildingManifestReader
    {
        public const string ManifestPath = "Docs/ArtProduction/BUILDING_ASSET_MANIFEST.json";
        public const string CatalogPath = "Assets/Weedtopia/Data/Buildings/BuildingVisualCatalog.asset";
        public static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);
        public static readonly string[] Families =
        {
            "cultivation_plot", "farm_processing", "farm_warehouse", "city_warehouse", "coffee_shop",
            "residential_house", "park", "butter_factory", "supermarket", "bakery"
        };
        private static readonly int[] Levels =
            { 0, 1, 5, 10, 25, 50, 100, 150, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1250, 1500, 1750, 2000, 2500, 3000, 4000, 5000 };

        public static BuildingArtManifest Load()
        {
            string json = File.ReadAllText(AbsolutePath(ManifestPath));
            BuildingArtManifest manifest = JsonUtility.FromJson<BuildingArtManifest>(json);
            List<string> errors = ValidateSchema(manifest);
            if (errors.Count > 0) throw new InvalidDataException(string.Join("\n", errors));
            return manifest;
        }

        public static string AbsolutePath(string path)
        {
            if (!IsRelativeNormalized(path)) throw new InvalidDataException("Unsafe/non-normalized project path: " + path);
            string absolute = Path.GetFullPath(Path.Combine(ProjectRoot, path));
            string rootPrefix = Path.GetFullPath(ProjectRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!absolute.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Path leaves project root: " + path);
            return absolute;
        }

        public static bool IsRelativeNormalized(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && !Path.IsPathRooted(path) && !path.Contains('\\') &&
                   !path.Contains(':') && !path.EndsWith("/", StringComparison.Ordinal) &&
                   path.Split('/').All(p => p.Length > 0 && p != "." && p != "..");
        }

        public static List<string> ValidateSchema(BuildingArtManifest manifest)
        {
            var errors = new List<string>();
            if (manifest == null || manifest.schemaVersion != 1 || manifest.slots == null)
            {
                errors.Add("Manifest requires schemaVersion 1 and a slots array.");
                return errors;
            }
            if (manifest.slots.Length != 251) errors.Add("Expected exactly 251 slots.");
            var keys = new HashSet<string>(StringComparer.Ordinal);
            var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (BuildingArtSlot slot in manifest.slots)
            {
                if (slot == null) { errors.Add("Null slot."); continue; }
                if (!keys.Add(slot.Key)) errors.Add("Duplicate slot: " + slot.Key);
                if (!Families.Contains(slot.building_id) && slot.building_id != "town_hall") errors.Add("Unknown family: " + slot.Key);
                if (!targets.Add(slot.unity_prefab_target ?? "")) errors.Add("Duplicate target: " + slot.Key);
                if (slot.target_faces < 6000 || slot.target_faces > 15000) errors.Add("Face budget outside 6000–15000: " + slot.Key);
                if (slot.footprint_m_x <= 0 || slot.footprint_m_z <= 0 || slot.height_m <= 0) errors.Add("Invalid dimensions: " + slot.Key);
                foreach (string path in Paths(slot)) if (!IsRelativeNormalized(path)) errors.Add("Invalid path for " + slot.Key + ": " + path);
                if (slot.unity_incoming_folder != "Assets/Weedtopia/Art/Buildings/Incoming/" + slot.Key)
                    errors.Add("Incoming folder mismatch: " + slot.Key);
                if (slot.unity_prefab_target != "Assets/Weedtopia/Art/Buildings/Prefabs/" + slot.building_id + "/WT_" + slot.building_id + "_" + slot.tier_id + ".prefab")
                    errors.Add("Prefab target mismatch: " + slot.Key);
                int expectedDriver = (slot.building_id == "farm_warehouse" || slot.building_id == "city_warehouse") && slot.min_level >= 1 ? 1 : 0;
                if (slot.driver_slot_delta != expectedDriver) errors.Add("Driver milestone mismatch: " + slot.Key);
            }
            foreach (string family in Families)
            {
                BuildingArtSlot[] slots = manifest.slots.Where(s => s != null && s.building_id == family).OrderBy(s => s.min_level).ToArray();
                if (slots.Length != 25) { errors.Add("Expected 25 slots: " + family); continue; }
                for (int i = 0; i < Levels.Length; i++)
                {
                    string tier = i == 0 ? "T00_RUIN" : "T" + i.ToString("00") + "_L" + Levels[i].ToString(Levels[i] < 1000 ? "000" : "0");
                    int max = i == 24 ? 5000 : Levels[i + 1] - 1;
                    if (slots[i].min_level != Levels[i] || slots[i].max_level != max || slots[i].tier_id != tier)
                        errors.Add("Incorrect level band/tier: " + family + "/" + tier);
                }
            }
            BuildingArtSlot[] fixedSlots = manifest.slots.Where(s => s != null && s.building_id == "town_hall").ToArray();
            if (fixedSlots.Length != 1 || fixedSlots[0].tier_id != "T_FIXED" || fixedSlots[0].min_level != 0 || fixedSlots[0].max_level != 5000)
                errors.Add("town_hall requires exactly one T_FIXED band 0–5000.");
            return errors;
        }

        public static IEnumerable<string> Paths(BuildingArtSlot slot)
        {
            yield return slot.reference_image;
            yield return slot.meshy_prompt_file;
            yield return slot.texture_prompt_file;
            yield return slot.image_prompt_file;
            yield return slot.asset_spec_file;
            yield return slot.meshy_settings_file;
            yield return slot.unity_incoming_folder;
            yield return slot.unity_prefab_target;
        }
    }
}
