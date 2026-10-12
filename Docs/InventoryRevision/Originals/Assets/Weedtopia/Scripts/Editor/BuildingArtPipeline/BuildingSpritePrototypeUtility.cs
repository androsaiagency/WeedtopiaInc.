using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Weedtopia.Buildings.Presentation;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.BuildingArtPipeline
{
    public static class BuildingSpritePrototypeUtility
    {
        public const string SourcePath = "Assets/Weedtopia/Art/Buildings/Sprites/town_hall/WT_town_hall_T_FIXED_2D_v1.png";
        public const string PrototypeFolder = "Assets/Weedtopia/Art/Buildings/Prototypes2D/town_hall";
        public const string PrototypePath = PrototypeFolder + "/WT_town_hall_T_FIXED_2D_Prototype.prefab";
        public const string ReceiptPath = "Docs/ArtProduction/Buildings/town_hall/T_FIXED/2d_prototype_receipt.json";
        private const float TownHallWorldHeight = 22f;

        [Serializable]
        private sealed class PrototypeReceipt
        {
            public string generatedUtc;
            public string sourceSprite;
            public string prototypePrefab;
            public int sourceWidth;
            public int sourceHeight;
            public float pixelsPerUnit;
            public float worldHeight;
            public string pivot;
            public bool alphaTransparency;
            public bool mipMaps;
            public bool catalogChanged;
            public string decision;
        }

        [MenuItem("Weedtopia/Art Pipeline/2D Buildings/Create Town Hall Prototype")]
        public static void CreateTownHallPrototypeMenu()
        {
            GameObject prefab = CreateTownHallPrototype();
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log("Created isolated 2D town-hall prototype: " + PrototypePath);
        }

        public static GameObject CreateTownHallPrototype()
        {
            ConfigureSpriteImporter();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SourcePath);
            if (sprite == null) throw new FileNotFoundException("Town-hall sprite was not imported.", SourcePath);

            BuildingIntakeService.EnsureAssetFolder(PrototypeFolder);
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrototypePath);
            // A rerun must preserve the prefab GUID and all authored offsets/references.
            if (existing != null) return existing;

            var root = new GameObject("WT_town_hall_T_FIXED_2D_Prototype");
            try
            {
                Transform visualRoot = CreateChild(root.transform, "VisualRoot");
                Transform interaction = CreateChild(root.transform, "InteractionAnchor");
                Transform worker = CreateChild(root.transform, "WorkerAnchor");
                Transform input = CreateChild(root.transform, "DeliveryInputAnchor");
                Transform output = CreateChild(root.transform, "DeliveryOutputAnchor");
                Transform vfx = CreateChild(root.transform, "VfxAnchor");
                Transform colliderRoot = CreateChild(root.transform, "ColliderRoot");

                var spriteObject = new GameObject("SpriteVisual");
                spriteObject.transform.SetParent(visualRoot, false);
                SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = Color.white;
                renderer.drawMode = SpriteDrawMode.Simple;
                renderer.sortingOrder = 0;
                float localHeight = Mathf.Max(0.01f, sprite.bounds.size.y);
                float scale = TownHallWorldHeight / localHeight;
                spriteObject.transform.localScale = Vector3.one * scale;
                BuildingSpriteVisual spriteVisual = spriteObject.AddComponent<BuildingSpriteVisual>();
                spriteVisual.EditorConfigure(renderer, true, TownHallWorldHeight);

                BuildingVisualWrapper wrapper = root.AddComponent<BuildingVisualWrapper>();
                wrapper.EditorSetStructure("town_hall", "T_FIXED", visualRoot, interaction, worker, input, output, vfx, colliderRoot);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrototypePath);
                if (prefab == null) throw new InvalidOperationException("Unity could not save the 2D town-hall prototype.");
                WriteReceipt(sprite);
                AssetDatabase.SaveAssets();
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void ConfigureSpriteImporter()
        {
            AssetDatabase.ImportAsset(SourcePath, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(SourcePath) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Town-hall sprite texture is missing.", SourcePath);
            var textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            bool changed = importer.textureType != TextureImporterType.Sprite ||
                           importer.spriteImportMode != SpriteImportMode.Single ||
                           importer.spritePixelsPerUnit != 64f ||
                           textureSettings.spriteAlignment != (int)SpriteAlignment.Custom ||
                           importer.spritePivot != new Vector2(0.5f, 0f) ||
                           !importer.alphaIsTransparency ||
                           !importer.mipmapEnabled ||
                           importer.wrapMode != TextureWrapMode.Clamp ||
                           importer.maxTextureSize != 2048;
            if (!changed) return;
            textureSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            importer.SetTextureSettings(textureSettings);
            // SetTextureSettings restores textureType too; apply the Sprite type afterwards.
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64f;
            importer.spritePivot = new Vector2(0.5f, 0f);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void WriteReceipt(Sprite sprite)
        {
            var receipt = new PrototypeReceipt
            {
                generatedUtc = DateTime.UtcNow.ToString("O"),
                sourceSprite = SourcePath,
                prototypePrefab = PrototypePath,
                sourceWidth = (int)sprite.rect.width,
                sourceHeight = (int)sprite.rect.height,
                pixelsPerUnit = sprite.pixelsPerUnit,
                worldHeight = TownHallWorldHeight,
                pivot = "bottom_center_0.5_0.0",
                alphaTransparency = true,
                mipMaps = true,
                catalogChanged = false,
                decision = "isolated_2d_prototype_ready_for_visual_review"
            };
            File.WriteAllText(BuildingManifestReader.AbsolutePath(ReceiptPath), JsonUtility.ToJson(receipt, true));
        }
    }

    [InitializeOnLoad]
    public static class BuildingSpritePrototypeRequest
    {
        private const string RequestPath = "Temp/WeedtopiaTownHall2DPrototype.request";

        static BuildingSpritePrototypeRequest()
        {
            EditorApplication.delayCall += TryExecute;
        }

        private static void TryExecute()
        {
            string absoluteRequest = BuildingManifestReader.AbsolutePath(RequestPath);
            if (!File.Exists(absoluteRequest)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryExecute;
                return;
            }
            try
            {
                BuildingSpritePrototypeUtility.CreateTownHallPrototype();
                File.Delete(absoluteRequest);
                Debug.Log("The isolated 2D town-hall prototype is ready for review.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("The 2D prototype request remains available for retry: " + RequestPath);
            }
        }
    }
}
