using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit, repeatable installation of the selected launcher artwork; never runs on import.</summary>
    public static class GameIconAuthoring
    {
        public const string PrimaryIconPath = "Assets/Weedtopia/Production/Art/AppIcon/WeedtopiaGreenCity-v1.png";
        public const string ForegroundPath = "Assets/Weedtopia/Production/Art/AppIcon/WeedtopiaGreenCity-AdaptiveForeground-v1.png";
        public const string BackgroundPath = "Assets/Weedtopia/Production/Art/AppIcon/WeedtopiaPetrol-AdaptiveBackground-v1.png";
        public const string ReceiptPath = "Docs/GameIconImplementation/20261003/icon-installation.json";
        private const float SafeRadius = 1f / 3f; // Android's visible 72 dp mask on a 108 dp layer.

        [Serializable] private sealed class Slot
        {
            public string kind;
            public int width, height, layers;
            public string[] textures;
        }
        [Serializable] private sealed class Receipt
        {
            public string status, utc, unityVersion, productName, sourceIcon, sourceGuid;
            public string foreground, background;
            public int defaultSlots, androidSlots;
            public float foregroundMaxRadius;
            public bool foregroundInsideAdaptiveSafeCircle;
            public Slot[] slots;
        }

        [MenuItem("Weedtopia/Build/Apply Green City Game Icon")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                throw new InvalidOperationException("Install the game icon before entering Play Mode or building.");
            if (!File.Exists(PrimaryIconPath) || !File.Exists(ForegroundPath))
                throw new FileNotFoundException("The selected Green City icon assets are missing.");

            float maxRadius = ValidateForeground();
            CreateBackground();
            Texture2D primary = ImportIcon(PrimaryIconPath, false);
            Texture2D foreground = ImportIcon(ForegroundPath, true);
            Texture2D background = ImportIcon(BackgroundPath, false);
            int defaultSlots = PlayerSettings.GetIconSizes(NamedBuildTarget.Unknown, IconKind.Any).Length;
            if (defaultSlots == 0) throw new InvalidOperationException("Unity has no default application icon slot.");
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown,
                Enumerable.Repeat(primary, defaultSlots).ToArray(), IconKind.Any);

            var installed = new List<Slot>();
            foreach (PlatformIconKind kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
            {
                PlatformIcon[] slots = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
                foreach (PlatformIcon slot in slots)
                {
                    Texture2D[] layers;
                    if (slot.maxLayerCount == 2) layers = new[] { background, foreground };
                    else if (slot.maxLayerCount == 1) layers = new[] { primary };
                    else throw new InvalidOperationException("Unsupported Android icon layer count: " + slot.maxLayerCount);
                    slot.SetTextures(layers);
                }
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, slots);
                foreach (PlatformIcon slot in PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind))
                {
                    Texture2D[] layers = slot.GetTextures();
                    if (layers.Length != slot.maxLayerCount || layers.Any(x => x == null))
                        throw new InvalidOperationException("Android icon assignment did not persist.");
                    installed.Add(new Slot { kind = kind.ToString(), width = slot.width, height = slot.height,
                        layers = layers.Length, textures = layers.Select(AssetDatabase.GetAssetPath).ToArray() });
                }
            }
            if (installed.Count == 0 || !installed.Any(x => x.layers == 2))
                throw new InvalidOperationException("Android adaptive icon slots are unavailable.");
            PlayerSettings.productName = "Weedtopia";
            AssetDatabase.SaveAssets();

            Directory.CreateDirectory(Path.GetDirectoryName(ReceiptPath));
            File.WriteAllText(ReceiptPath, JsonUtility.ToJson(new Receipt {
                status = "passed", utc = DateTime.UtcNow.ToString("O"),
                unityVersion = UnityEngine.Application.unityVersion, productName = PlayerSettings.productName,
                sourceIcon = PrimaryIconPath, sourceGuid = AssetDatabase.AssetPathToGUID(PrimaryIconPath),
                foreground = ForegroundPath, background = BackgroundPath,
                defaultSlots = defaultSlots, androidSlots = installed.Count, slots = installed.ToArray(),
                foregroundMaxRadius = maxRadius, foregroundInsideAdaptiveSafeCircle = maxRadius <= SafeRadius
            }, true));
            Debug.Log("Green City launcher icon installed: " + ReceiptPath);
        }

        private static float ValidateForeground()
        {
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(image, File.ReadAllBytes(ForegroundPath))
                    || image.width != image.height || image.width < 512)
                    throw new InvalidDataException("Adaptive foreground must be a square high-resolution PNG.");
                Color32[] pixels = image.GetPixels32();
                float maxRadius = 0;
                int visible = 0;
                for (int y = 0; y < image.height; y++)
                    for (int x = 0; x < image.width; x++)
                    {
                        if (pixels[y * image.width + x].a < 16) continue;
                        visible++;
                        float dx = (x + 0.5f) / image.width - 0.5f;
                        float dy = (y + 0.5f) / image.height - 0.5f;
                        maxRadius = Mathf.Max(maxRadius, Mathf.Sqrt(dx * dx + dy * dy));
                    }
                if (visible == 0 || maxRadius > SafeRadius)
                    throw new InvalidDataException("Adaptive city artwork exceeds Android's safe circle: " + maxRadius);
                return maxRadius;
            }
            finally { UnityEngine.Object.DestroyImmediate(image); }
        }

        private static void CreateBackground()
        {
            if (File.Exists(BackgroundPath)) return;
            var image = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            try
            {
                image.SetPixels32(Enumerable.Repeat(new Color32(6, 60, 70, 255), 32 * 32).ToArray());
                image.Apply();
                File.WriteAllBytes(BackgroundPath, ImageConversion.EncodeToPNG(image));
            }
            finally { UnityEngine.Object.DestroyImmediate(image); }
        }

        private static Texture2D ImportIcon(string path, bool transparent)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing icon TextureImporter: " + path);
            importer.textureType = TextureImporterType.Default;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = transparent;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.isReadable = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path)
                ?? throw new InvalidOperationException("Icon import failed: " + path);
        }
    }
}
