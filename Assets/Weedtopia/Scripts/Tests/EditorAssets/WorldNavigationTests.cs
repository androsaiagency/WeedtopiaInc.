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
    public sealed class WorldNavigationTests
    {
        [Test] public void AuthoredNavigationUsesFiveReusableIconButtonsAndAllBuildingsAreReachable()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var root=Object.FindFirstObjectByType<GameRoot>();var navigation=root.Ui.GetComponent<WorldNavigationBar>();
                Assert.That(navigation,Is.Not.Null);
                Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(navigation.Dock),Is.EqualTo(WorldNavigationAuthoring.DockPath));
                var buttons=navigation.Dock.GetComponentsInChildren<Button>(true);
                Assert.That(buttons.Select(b=>b.name),Is.EqualTo(new[]{"Farm","City","Settlement","Industry","Orders"}));
                Assert.That(navigation.Dock.GetComponentsInChildren<Text>(true),Is.Empty);
                foreach(var button in buttons)
                {
                    Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(button),Is.EqualTo(WorldNavigationAuthoring.ButtonPath));
                    Assert.That(button.GetComponent<CanvasRenderer>(),Is.Not.Null);
                    Assert.That(button.GetComponent<RoundNavigationGraphic>(),Is.Not.Null);
                    Assert.That(button.GetComponentInChildren<Image>(true).sprite,Is.Not.Null);
                }
                var map=AssetDatabase.LoadAssetAtPath<DistrictMap>(BurningHillsLayoutInstaller.MapPath);
                foreach(var site in root.Sites)
                {
                    var point=DistrictMap.ToPlan(site.transform.position);
                    Assert.That(map.ClampNavigation(point),Is.EqualTo(point),site.InstanceId);
                }
                Assert.That(navigation.Dock.anchorMin,Is.EqualTo(new Vector2(.5f,0)));
                Assert.That(navigation.Dock.anchorMax,Is.EqualTo(new Vector2(.5f,0)));
                Assert.That(root.Ui.Interaction.townMenu.GetComponentsInChildren<RoundNavigationGraphic>(true),Is.Empty);
            }
            finally {EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}
