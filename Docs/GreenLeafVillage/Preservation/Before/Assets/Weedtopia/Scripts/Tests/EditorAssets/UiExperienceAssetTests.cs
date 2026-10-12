using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Editor.GameSetup;
using Weedtopia.Unity;

namespace Weedtopia.Tests.Assets
{
    public sealed class UiExperienceAssetTests
    {
        private static readonly string[] Scenes={GameScaffold.GameScene,"Assets/Weedtopia/Game/Scenes/BigRockCity.unity",SevenHillsAuthoring.ScenePath};
        [Test] public void AllCitiesUseTheSharedAuthoredDialogueAndSinglePlayMenu()
        {
            foreach(string path in Scenes)
            {
                EditorSceneManager.OpenScene(path);var root=Object.FindFirstObjectByType<GameRoot>();var view=root.Ui.GetComponent<UiExperience>();
                Assert.That(view,Is.Not.Null,path);Assert.That(view.intro.portraits.Length,Is.EqualTo(8));Assert.That(view.gameScroll,Is.Not.Null);Assert.That(view.profileScroll,Is.Not.Null);
                var serialized=new SerializedObject(root.Ui);var bindings=serialized.FindProperty("bindings");GameObject start=null;Button play=null;
                for(int i=0;i<bindings.arraySize;i++)
                {
                    var entry=bindings.GetArrayElementAtIndex(i);string id=entry.FindPropertyRelative("id").stringValue;
                    var target=(GameObject)entry.FindPropertyRelative("target").objectReferenceValue;
                    if(id=="start")start=target;
                    if(id=="ui.start")play=target.GetComponent<Button>();
                }
                Assert.That(play,Is.Not.Null);
                CollectionAssert.AreEquivalent(new[]{play,view.startSettings},start.GetComponentsInChildren<Button>(true));Assert.That(view.inspectionVisibility,Is.Not.Null);
                Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(view.intro.gameObject),Is.EqualTo(PrefabUtility.GetCorrespondingObjectFromSource(view.conversation.gameObject)));
                var archive=root.Ui.transform.Find("ArchivedUiBeforeExperience");Assert.That(archive,Is.Not.Null);Assert.That(archive.gameObject.activeSelf,Is.False);
            }
        }
        [Test] public void PortraitCellsUseActualSourceDimensionsAndPreserveAlpha()
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(UiExperienceAuthoring.Atlas);importer.GetSourceTextureWidthAndHeight(out int width,out int height);
            Assert.That(importer.DoesSourceTextureHaveAlpha(),Is.True);Assert.That(importer.mipmapEnabled,Is.False);
            var sprites=AssetDatabase.LoadAllAssetsAtPath(UiExperienceAuthoring.Atlas).OfType<Sprite>().ToArray();Assert.That(sprites.Length,Is.EqualTo(16));
            foreach(var sprite in sprites)
            {
                Assert.That(sprite.rect.width,Is.EqualTo(width/4f*.88f).Within(.01));Assert.That(sprite.rect.height,Is.EqualTo(height/4f*.92f).Within(.01));
                Assert.That(sprite.rect.xMax,Is.LessThanOrEqualTo(width));Assert.That(sprite.rect.yMax,Is.LessThanOrEqualTo(height));
            }
        }
        [Test] public void RepeatedUiInstallerPreservesEverySceneAndSharedPrefabByte()
        {
            var paths=Scenes.Concat(Directory.GetFiles(UiExperienceAuthoring.Folder,"*",SearchOption.AllDirectories)).Append(UiExperienceAuthoring.Atlas+".meta");
            var before=paths.ToDictionary(p=>p,File.ReadAllBytes);UiExperienceAuthoring.Apply();
            foreach(var pair in before)Assert.That(pair.Value.SequenceEqual(File.ReadAllBytes(pair.Key)),Is.True,pair.Key);
            File.WriteAllText("Docs/UiExperienceRevision/idempotence.json","{\"passed\":true,\"files\":"+before.Count+"}");
        }
        [Test] public void PolishedAssetsAndInstallerAreSharedAndRepeatable()
        {
            foreach(string path in Scenes)
            {
                EditorSceneManager.OpenScene(path);var view=Object.FindFirstObjectByType<GameRoot>().Ui.GetComponent<UiExperience>();
                Assert.That(view.titleBackground.sprite,Is.Not.Null);Assert.That(view.titleWordmark.sprite,Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(view.titleBackground.transform.parent.gameObject)),Is.EqualTo(UiPolishAuthoring.TitlePrefab));
                Assert.That(AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(view.quests.preview.gameObject)),Is.EqualTo(UiPolishAuthoring.PreviewPrefab));
                Assert.That(view.conversation.portraits.All(p=>p.mirrorToFaceRight),Is.True);Assert.That(view.conversation.tapScreen,Is.Not.Null);
                var ui=view.GetComponent<GameUi>();var bindings=new SerializedObject(ui).FindProperty("bindings");
                for(int i=0;i<bindings.arraySize;i++)
                {
                    var entry=bindings.GetArrayElementAtIndex(i);string id=entry.FindPropertyRelative("id").stringValue;var target=(GameObject)entry.FindPropertyRelative("target").objectReferenceValue;
                    Assert.That(target,Is.Not.Null,id+" in "+path);
                    if(new[]{"collection","luckyWheel","vipStories","cityCampaign"}.Contains(id))Assert.That(target.GetComponent<ModulePanelLayout>().scroll.content,Is.Not.Null,id);
                }
            }
            var folders=new[]{"Assets/Weedtopia/Production/Prefabs",RewardPresentationAuthoring.Folder,LuckyWheelAuthoring.Folder,VipStoriesAuthoring.Folder,UiPolishAuthoring.Skin,UiPolishAuthoring.Title};
            var files=Scenes.Concat(folders.SelectMany(f=>Directory.GetFiles(f,"*",SearchOption.AllDirectories))).Concat(Directory.GetFiles(BigRockCityAuthoring.Folder,"*.prefab",SearchOption.TopDirectoryOnly)).Distinct().ToArray();
            var before=files.ToDictionary(f=>f,File.ReadAllBytes);UiPolishAuthoring.Apply();foreach(var pair in before)Assert.That(pair.Value.SequenceEqual(File.ReadAllBytes(pair.Key)),Is.True,pair.Key);
            File.WriteAllText("Docs/UiPolishRevision/idempotence.json","{\"passed\":true,\"files\":"+before.Count+"}");
        }
    }
}
