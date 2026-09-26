using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Weedtopia.Buildings.Presentation;

namespace Weedtopia.Editor.BuildingArtPipeline.Tests
{
    public sealed class BuildingSpritePrototypeTests
    {
        [Test]
        public void TownHallPrototype_UsesBottomPivotSpriteAndNoMeshRenderer()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildingSpritePrototypeUtility.PrototypePath);
            Assert.That(prefab, Is.Not.Null, "Generate the isolated town-hall prototype first.");
            BuildingVisualWrapper wrapper = prefab.GetComponent<BuildingVisualWrapper>();
            Assert.That(wrapper, Is.Not.Null);
            Assert.That(wrapper.BuildingId, Is.EqualTo("town_hall"));
            Assert.That(wrapper.TierId, Is.EqualTo("T_FIXED"));
            BuildingSpriteVisual visual = prefab.GetComponentInChildren<BuildingSpriteVisual>(true);
            Assert.That(visual, Is.Not.Null);
            Assert.That(visual.SpriteRenderer, Is.Not.Null);
            Assert.That(visual.SpriteRenderer.sprite, Is.Not.Null);
            Assert.That(visual.FaceRenderCamera, Is.True);
            Assert.That(visual.AuthoredWorldHeight, Is.EqualTo(22f).Within(0.001f));
            Assert.That(prefab.GetComponentInChildren<MeshRenderer>(true), Is.Null);

            TextureImporter importer = AssetImporter.GetAtPath(BuildingSpritePrototypeUtility.SourcePath) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            var textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(textureSettings.spriteAlignment, Is.EqualTo((int)SpriteAlignment.Custom));
            Assert.That(importer.spritePivot, Is.EqualTo(new Vector2(0.5f, 0f)));
            Assert.That(importer.alphaIsTransparency, Is.True);
            Assert.That(importer.mipmapEnabled, Is.True);
        }
    }
}
