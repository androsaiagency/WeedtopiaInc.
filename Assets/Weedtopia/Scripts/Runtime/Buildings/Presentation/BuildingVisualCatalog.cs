using System;
using System.Collections.Generic;
using UnityEngine;

namespace Weedtopia.Buildings.Presentation
{
    /// <summary>Presentation lookup only. Callers own spawning, pooling and gameplay decisions.</summary>
    public interface IBuildingVisualProvider
    {
        bool TryGetVisual(string buildingId, int level, out GameObject prefab);
    }

    [Serializable]
    public sealed class BuildingVisualBand
    {
        [SerializeField] private string tierId;
        [SerializeField] private int minLevel;
        [SerializeField] private int maxLevel;
        [SerializeField] private GameObject wrapperPrefab;

        public string TierId => tierId;
        public int MinLevel => minLevel;
        public int MaxLevel => maxLevel;
        public GameObject WrapperPrefab => wrapperPrefab;

        public BuildingVisualBand(string tierId, int minLevel, int maxLevel, GameObject wrapperPrefab)
        {
            this.tierId = tierId;
            this.minLevel = minLevel;
            this.maxLevel = maxLevel;
            this.wrapperPrefab = wrapperPrefab;
        }
    }

    [Serializable]
    public sealed class BuildingVisualFamily
    {
        [SerializeField] private string buildingId;
        [SerializeField] private List<BuildingVisualBand> bands = new List<BuildingVisualBand>();
        public string BuildingId => buildingId;
        public IReadOnlyList<BuildingVisualBand> Bands => bands;

        public BuildingVisualFamily(string buildingId, List<BuildingVisualBand> bands)
        {
            this.buildingId = buildingId;
            this.bands = bands;
        }
    }

    [CreateAssetMenu(menuName = "Weedtopia/Buildings/Visual Catalog", fileName = "BuildingVisualCatalog")]
    public sealed class BuildingVisualCatalog : ScriptableObject, IBuildingVisualProvider
    {
        [SerializeField] private List<BuildingVisualFamily> families = new List<BuildingVisualFamily>();
        public IReadOnlyList<BuildingVisualFamily> Families => families;

        public bool TryGetVisual(string buildingId, int level, out GameObject prefab)
        {
            prefab = null;
            if (string.IsNullOrEmpty(buildingId) || level < 0) return false;
            foreach (BuildingVisualFamily family in families)
            {
                if (family == null || !string.Equals(family.BuildingId, buildingId, StringComparison.Ordinal)) continue;
                foreach (BuildingVisualBand band in family.Bands)
                {
                    if (band == null || level < band.MinLevel || level > band.MaxLevel) continue;
                    prefab = band.WrapperPrefab;
                    return prefab != null;
                }
                return false;
            }
            return false;
        }

#if UNITY_EDITOR
        public void EditorSetFamilies(List<BuildingVisualFamily> value) => families = value;
#endif
    }
}
