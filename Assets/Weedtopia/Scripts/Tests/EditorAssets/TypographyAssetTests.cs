using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Content;
using Weedtopia.Domain;
using Weedtopia.Editor.GameSetup;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Tests.Assets
{
    public sealed class TypographyAssetTests
    {
        [TestCase(TypographyAuthoring.BodyPath)]
        [TestCase(TypographyAuthoring.HeadingPath)]
        public void ShippedFontsRasterizeEveryLocalizedCharacterAtUiSizes(string path)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(path);
            Assert.That(font, Is.Not.Null);
            var importer = (TrueTypeFontImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer.includeFontData, Is.True, "Glyphs must ship offline");
            Assert.That(importer.fontNames, Is.EqualTo(new[] { "Weedtopia Grove" }));
            Assert.That(importer.fontReferences, Is.Empty, "Coverage must come from this font");
            var catalog = AssetDatabase.LoadAssetAtPath<LanguageCatalog>(LocalizationAuthoring.CatalogPath);
            string corpus = string.Concat(catalog.languages.SelectMany(language =>
                JsonUtility.FromJson<LocaleDocument>(language.catalog.text).entries.Select(e => e.value)));
            corpus += corpus.Normalize(System.Text.NormalizationForm.FormD) + "✓✔₽ẞ";
            string characters = new string(corpus.Where(c => !char.IsControl(c) && !char.IsWhiteSpace(c)).Distinct().OrderBy(c => c).ToArray());
            foreach (int size in new[] { 16, 32, 64 })
            {
                font.RequestCharactersInTexture(characters, size, FontStyle.Normal);
                foreach (char character in characters)
                {
                    Assert.That(font.HasCharacter(character), Is.True, "U+" + ((int)character).ToString("X4"));
                    Assert.That(font.GetCharacterInfo(character, out var info, size, FontStyle.Normal), Is.True, path + " / " + character + " / " + size);
                    Assert.That(info.glyphWidth, Is.GreaterThan(0), "Empty glyph: " + character);
                    if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                        Assert.That(info.advance, Is.GreaterThan(0), "Non-advancing letter: " + character);
                }
            }
        }

        [TestCase(GameScaffold.GameScene)]
        [TestCase("Assets/Weedtopia/Game/Scenes/BigRockCity.unity")]
        [TestCase("Assets/Weedtopia/Game/Scenes/SevenHills.unity")]
        public void AllCitiesAndWorldLabelsUseTheAuthoredFamily(string path)
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(path);
                var body = TypographyAuthoring.LoadBody(); var heading = TypographyAuthoring.LoadHeading();
                var allowed = new[] { body, heading };
                var texts = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Text>(true)).ToArray();
                Assert.That(texts.Length, Is.GreaterThan(100));
                foreach (var text in texts) Assert.That(allowed, Does.Contain(text.font), text.name + " in " + path);
                foreach (var text in scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<TextMesh>(true)))
                {
                    Assert.That(allowed, Does.Contain(text.font), text.name);
                    Assert.That(text.GetComponent<MeshRenderer>().sharedMaterial, Is.EqualTo(text.font.material), "Stale font atlas material: " + text.name);
                }
                var root = Object.FindFirstObjectByType<GameRoot>();
                var art = AssetDatabase.LoadAssetAtPath<PresentationArt>(ProductionArtImporter.ArtPath);
                Assert.That(art.bodyFont, Is.EqualTo(body)); Assert.That(art.headingFont, Is.EqualTo(heading));
                var catalog = AssetDatabase.LoadAssetAtPath<LanguageCatalog>(LocalizationAuthoring.CatalogPath);
                Assert.That(catalog.bodyFont, Is.EqualTo(body)); Assert.That(catalog.headingFont, Is.EqualTo(heading));
                Assert.That(root.Ui.Experience.language.GetComponentInChildren<Text>().font, Is.EqualTo(body));
            }
            finally { if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        [Test] public void ReusableTextPrefabsUseTheFamilyAndFontRepairIsIdempotent()
        {
            var allowed = new[] { TypographyAuthoring.LoadBody(), TypographyAuthoring.LoadHeading() };
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Weedtopia/Game", "Assets/Weedtopia/Production" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/Review/")) continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var text in prefab.GetComponentsInChildren<Text>(true))
                {
                    Assert.That(allowed, Does.Contain(text.font), path + " / " + text.name); count++;
                }
            }
            Assert.That(count, Is.GreaterThan(100));
            var files = Directory.GetFiles("Assets/Weedtopia", "*", SearchOption.AllDirectories)
                .Where(p => new[] { ".unity", ".prefab", ".asset", ".ttf", ".meta", ".json" }.Contains(Path.GetExtension(p)));
            var before = files.ToDictionary(p => p, File.ReadAllBytes);
            TypographyAuthoring.Apply();
            foreach (var file in before) Assert.That(file.Value.SequenceEqual(File.ReadAllBytes(file.Key)),Is.True,file.Key);
            File.WriteAllText("Docs/Typography/idempotence.json", "{\"passed\":true,\"files\":" + before.Count + ",\"prefabTextComponents\":" + count + "}");
        }
    }
}
