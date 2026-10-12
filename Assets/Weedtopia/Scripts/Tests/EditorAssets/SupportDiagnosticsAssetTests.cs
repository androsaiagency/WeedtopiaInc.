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
using Object=UnityEngine.Object;

namespace Weedtopia.Tests.Assets
{
    public sealed class SupportDiagnosticsAssetTests
    {
        private static readonly string[] Scenes={GameScaffold.GameScene,"Assets/Weedtopia/Game/Scenes/BigRockCity.unity",SevenHillsAuthoring.ScenePath};
        [Test] public void AllCitiesAuthorSupportControlsFromExistingSharedPrefabs()
        {
            foreach(string path in Scenes)
            {
                // Drop the current scene first: these assertions must inspect serialized disk references.
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                EditorSceneManager.OpenScene(path);AssertAuthoredControls(path);
            }
        }
        private static void AssertAuthoredControls(string path)
        {
            var root=Object.FindFirstObjectByType<GameRoot>();var view=root.Ui.Experience;
            Assert.That(view.copyDiagnostics,Is.Not.Null,path);Assert.That(view.startSettings,Is.Not.Null,path);
            Assert.That(view.copyDiagnostics.transform.parent,Is.SameAs(view.gamePage));
            Assert.That(view.startSettings.transform.parent.name,Is.EqualTo("start"));
            Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(view.copyDiagnostics.gameObject),Is.EqualTo(SupportDiagnosticsAuthoring.CopyButtonPrefab));
            Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(view.startSettings.gameObject),Is.EqualTo(SupportDiagnosticsAuthoring.SettingsButtonPrefab));
            Assert.That(view.copyDiagnostics.GetComponentInChildren<Text>(true).GetComponent<LocalizedLabel>().Key,Is.EqualTo("settings.diagnostics.copy"));
            Assert.That(view.startSettings.GetComponentInChildren<Image>(true).sprite,Is.Not.Null);
        }
        [Test] public void RepeatingSupportInstallerPreservesSceneAndSharedPrefabBytesAndGuids()
        {
            var paths=Scenes.SelectMany(path=>new[]{path,path+".meta"}).Concat(new[]{SupportDiagnosticsAuthoring.CopyButtonPrefab,
                SupportDiagnosticsAuthoring.CopyButtonPrefab+".meta",SupportDiagnosticsAuthoring.SettingsButtonPrefab,SupportDiagnosticsAuthoring.SettingsButtonPrefab+".meta"});
            var before=paths.ToDictionary(path=>path,File.ReadAllBytes);
            SupportDiagnosticsAuthoring.Apply();
            foreach(var pair in before)Assert.That(pair.Value.SequenceEqual(File.ReadAllBytes(pair.Key)),Is.True,pair.Key);
            foreach(string path in Scenes)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                EditorSceneManager.OpenScene(path);AssertAuthoredControls(path);
            }
        }
        [Test] public void EverySupportedLanguageDefinesAllSupportMessagesExactlyOnce()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<LanguageCatalog>(LocalizationAuthoring.CatalogPath);
            foreach(var language in catalog.languages)
            {
                var document=JsonUtility.FromJson<LocaleDocument>(language.catalog.text);
                foreach(string key in new[]{"settings.diagnostics.copy","settings.diagnostics.copied","settings.diagnostics.empty","settings.diagnostics.failed"})
                {
                    var entries=document.entries.Where(entry=>entry.key==key).ToArray();
                    Assert.That(entries.Length,Is.EqualTo(1),language.code+":"+key);Assert.That(entries[0].value,Is.Not.Empty);
                }
            }
        }
    }
}
