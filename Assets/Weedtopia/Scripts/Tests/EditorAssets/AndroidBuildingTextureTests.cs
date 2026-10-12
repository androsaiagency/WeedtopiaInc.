using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Weedtopia.Editor.GameSetup;

namespace Weedtopia.Tests.Assets
{
    public sealed class AndroidBuildingTextureTests
    {
        [Test] public void ProductionResourcesCoverExactlyTheApproved251Slots()
        {
            string[] paths = AndroidBuildingTextureOptimization.ProductionPaths();
            Assert.That(paths.Length, Is.EqualTo(251));
            Assert.That(paths.Distinct().Count(), Is.EqualTo(251));
            foreach (string path in paths)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), path);
                Assert.That(importer.isReadable, Is.False, path);
                Assert.That(importer.mipmapEnabled, Is.True, path);
                Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), path);
                Assert.That(importer.spritePivot, Is.EqualTo(new Vector2(.5f, 0)), path);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(64), path);
                Assert.That(importer.GetPlatformTextureSettings("Standalone").format, Is.EqualTo(TextureImporterFormat.BC7), path);
            }
        }

        [Test] public void AndroidPolicyPreservesAlphaAndUsesTheVerified1024ProfileWithoutCrunch()
        {
            var settings = new TextureImporterPlatformSettings { name = "Android", maxTextureSize = 512, format = TextureImporterFormat.ETC_RGB4, allowsAlphaSplitting = true, crunchedCompression = true };
            AndroidBuildingTextureOptimization.Configure(settings);
            Assert.That(AndroidBuildingTextureOptimization.MatchesPolicy(settings), Is.True);
            Assert.That(settings.format, Is.EqualTo(TextureImporterFormat.ETC2_RGBA8));
            Assert.That(settings.maxTextureSize, Is.EqualTo(1024));
            Assert.That(settings.compressionQuality, Is.EqualTo(100));
            Assert.That(settings.allowsAlphaSplitting, Is.False);
            Assert.That(settings.crunchedCompression, Is.False);
            AndroidBuildingTextureOptimization.Configure(settings);
            Assert.That(AndroidBuildingTextureOptimization.MatchesPolicy(settings), Is.True);
        }
    }
}
