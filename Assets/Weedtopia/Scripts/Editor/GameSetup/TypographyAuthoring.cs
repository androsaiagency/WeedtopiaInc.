using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Content;
using Weedtopia.Unity;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit font-reference migration. No import callbacks or runtime replacement.</summary>
    public static class TypographyAuthoring
    {
        public const string Folder = "Assets/Weedtopia/Production/Fonts/WeedtopiaGrove";
        public const string BodyPath = Folder + "/WeedtopiaGrove-Regular.ttf";
        public const string HeadingPath = Folder + "/WeedtopiaGrove-Bold.ttf";
        private const string LegacyBody = "Assets/Weedtopia/Production/Fonts/Lato-Regular.ttf";
        private const string LegacyHeading = "Assets/Weedtopia/Production/Fonts/Lato-Bold.ttf";
        private const string CatalogPath = "Assets/Weedtopia/Game/Localization/LanguageCatalog.asset";

        public static Font LoadBody() => Required(BodyPath);
        public static Font LoadHeading() => Required(HeadingPath);
        public static Font ResolveBody(Font current) => Resolve(current, LegacyBody, BodyPath);
        public static Font ResolveHeading(Font current) => Resolve(current, LegacyHeading, HeadingPath);
        private static Font Resolve(Font current, string legacy, string replacement) =>
            current == null || AssetDatabase.GetAssetPath(current) == legacy ? Required(replacement) : current;
        private static Font Required(string path) => AssetDatabase.LoadAssetAtPath<Font>(path)
            ?? throw new InvalidOperationException("Missing authored Weedtopia font: " + path);

        [MenuItem("Weedtopia/Typography/Install or Repair")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before installing typography");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save manual scene changes first");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            LoadBody(); LoadHeading();
            var substitutions = new Dictionary<string, string>
            {
                { "guid: " + AssetDatabase.AssetPathToGUID(LegacyBody), "guid: " + AssetDatabase.AssetPathToGUID(BodyPath) },
                { "guid: " + AssetDatabase.AssetPathToGUID(LegacyHeading), "guid: " + AssetDatabase.AssetPathToGUID(HeadingPath) }
            };
            if (substitutions.Any(pair => pair.Key.Length != 38 || pair.Value.Length != 38))
                throw new InvalidOperationException("Missing font GUID; migration refused");
            var changed = new List<string>();
            try
            {
                // Keep original serialized bytes/IDs/layouts. Only the known font GUIDs change.
                // Their generated materials share the same stable Unity subasset file IDs.
                foreach (var file in Directory.GetFiles("Assets/Weedtopia", "*", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
                {
                    string path = file.Replace('\\', '/');
                    string extension = Path.GetExtension(path);
                    if (extension != ".unity" && extension != ".prefab" && extension != ".asset") continue;
                    if (path.StartsWith("Assets/Weedtopia/Production/Review/", StringComparison.Ordinal)) continue;
                    byte[] bytes = File.ReadAllBytes(path);
                    string original = Encoding.UTF8.GetString(bytes), updated = original;
                    foreach (var pair in substitutions) updated = updated.Replace(pair.Key, pair.Value);
                    if (updated == original) continue;
                    File.WriteAllBytes(path, Encoding.UTF8.GetBytes(updated));
                    changed.Add(path);
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var art = AssetDatabase.LoadAssetAtPath<PresentationArt>(ProductionArtImporter.ArtPath);
                if (art == null) throw new InvalidOperationException("Missing presentation art");
                if (art.bodyFont == null || art.headingFont == null)
                {
                    art.bodyFont = ResolveBody(art.bodyFont); art.headingFont = ResolveHeading(art.headingFont);
                    EditorUtility.SetDirty(art);
                }
                var catalog = AssetDatabase.LoadAssetAtPath<LanguageCatalog>(CatalogPath);
                if (catalog == null) throw new InvalidOperationException("Missing language catalog");
                if (catalog.bodyFont == null || catalog.headingFont == null)
                {
                    catalog.bodyFont = ResolveBody(catalog.bodyFont); catalog.headingFont = ResolveHeading(catalog.headingFont);
                    EditorUtility.SetDirty(catalog);
                }
                AssetDatabase.SaveAssets();
                Validate();
                Directory.CreateDirectory("Docs/Typography");
                File.WriteAllText("Docs/Typography/authoring_receipt.json", JsonUtility.ToJson(new Receipt
                { utc = DateTime.UtcNow.ToString("O"), changedAssets = changed.ToArray(), bodyFont = BodyPath, headingFont = HeadingPath }, true));
                Debug.Log("Weedtopia Grove typography installed: " + changed.Count + " authored assets");
            }
            finally
            {
                if (!UnityEngine.Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        [MenuItem("Weedtopia/Typography/Validate Characters")]
        public static void Validate()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LanguageCatalog>(CatalogPath);
            if (catalog == null) throw new InvalidOperationException("Missing language catalog");
            var characters = new HashSet<char>("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzÄäÖöÜüßẞÉéÈèÊêËëÇçŒœÑñ¿¡АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя0123456789✓✔₽€");
            foreach (var language in catalog.languages)
                foreach (var entry in JsonUtility.FromJson<Weedtopia.Domain.LocaleDocument>(language.catalog.text).entries)
                    foreach (char character in entry.value)
                        if (!char.IsControl(character) && !char.IsWhiteSpace(character)) characters.Add(character);
            foreach (var font in new[] { LoadBody(), LoadHeading() })
            {
                var importer = (TrueTypeFontImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(font));
                if (!importer.includeFontData || !font.dynamic)
                    throw new InvalidOperationException("Weedtopia font must contain dynamic offline glyph data");
                string missing = new string(characters.Where(c => !font.HasCharacter(c)).OrderBy(c => c).ToArray());
                if (missing.Length > 0) throw new InvalidOperationException(font.name + " missing characters: " + missing);
            }
            Directory.CreateDirectory("Docs/Typography");
            File.WriteAllText("Docs/Typography/unity_glyph_validation.json", JsonUtility.ToJson(new GlyphReceipt
            { status = "passed", testedCharacters = characters.Count, languages = catalog.languages.Select(l => l.code).ToArray() }, true));
        }

        [Serializable] private sealed class Receipt
        { public string utc, bodyFont, headingFont; public string[] changedAssets; }
        [Serializable] private sealed class GlyphReceipt
        { public string status; public int testedCharacters; public string[] languages; }
    }
}
