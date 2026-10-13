using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Domain;
using Weedtopia.Editor.GameSetup;
using Weedtopia.Unity;

namespace Weedtopia.Tests.Assets
{
    public sealed class UiRefinementAssetTests
    {
        [Test] public void AllCitiesReferenceTheSameGoldenCloseSymbolPrefab()
        {
            foreach(string path in new[]{GameScaffold.GameScene,BigRockCityAuthoring.ScenePath,SevenHillsAuthoring.ScenePath})
            {
                EditorSceneManager.OpenScene(path);var root=Object.FindFirstObjectByType<GameRoot>();
                foreach(var visual in root.Ui.GetComponentsInChildren<CloseButtonVisual>(true))
                    Assert.That(AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(visual.icon.gameObject)),Is.EqualTo(UiRefinementAuthoring.CloseIconPath),visual.name);
                Assert.That(root.Ui.GetComponent<UiExperience>().avatars,Is.Not.Null);
            }
        }
        [Test] public void PlayerPortraitsAreIndependentAndAllTenHaveSourceAlpha()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<PlayerAvatarCatalog>(UiRefinementAuthoring.CatalogPath);
            Assert.That(catalog.entries.Select(e=>e.id),Is.EqualTo(PlayerProfile.Avatars));
            Assert.That(catalog.entries.Count(e=>e.gender==ProfileAvatarGender.Male),Is.EqualTo(5));Assert.That(catalog.entries.Count(e=>e.gender==ProfileAvatarGender.Female),Is.EqualTo(5));
            foreach(var entry in catalog.entries)
            {
                Assert.That(DialogueCharacters.Speakers,Does.Not.Contain(entry.id));
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(entry.portrait));
                Assert.That(importer.DoesSourceTextureHaveAlpha(),Is.True,entry.id);Assert.That(importer.alphaIsTransparency,Is.True);Assert.That(importer.mipmapEnabled,Is.False);
            }
            Assert.That(catalog.entries.Select(e=>e.portrait).Distinct().Count(),Is.EqualTo(10));
        }
        [Test] public void InstallerRepeatsWithoutChangingAuthoredSourcesOrScenes()
        {
            var files=Directory.GetFiles("Assets/Weedtopia/Production","*",SearchOption.AllDirectories)
                .Concat(new[]{GameScaffold.GameScene,BigRockCityAuthoring.ScenePath}).ToArray();
            var before=files.ToDictionary(p=>p,File.ReadAllBytes);UiRefinementAuthoring.Apply();
            // Array equality stays exact without NUnit's per-element boxing of large texture files.
            foreach(var pair in before)Assert.That(pair.Value.SequenceEqual(File.ReadAllBytes(pair.Key)),Is.True,pair.Key);
            File.WriteAllText("Docs/UiRefinementRevision/idempotence.json","{\"passed\":true,\"files\":"+before.Count+"}");
        }
    }
}
