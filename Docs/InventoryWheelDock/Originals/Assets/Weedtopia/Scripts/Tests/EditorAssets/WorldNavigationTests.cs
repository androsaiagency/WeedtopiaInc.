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
        [Test] public void AuthoredNavigationUsesSevenReusableIconButtonsAndAllBuildingsAreReachable()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var root=Object.FindFirstObjectByType<GameRoot>();var navigation=root.Ui.GetComponent<WorldNavigationBar>();
                Assert.That(navigation,Is.Not.Null);
                Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(navigation.Dock),Is.EqualTo(WorldNavigationAuthoring.DockPath));
                var buttons=navigation.Dock.GetComponentsInChildren<Button>(true);
                Assert.That(buttons.Select(b=>b.name),Is.EqualTo(new[]{"Overview","Farm","City","Settlement","Industry","Orders","Team"}));
                Assert.That(navigation.Dock.GetComponentsInChildren<Text>(true),Is.Empty);
                Assert.That(navigation.Dock.sizeDelta.x,Is.EqualTo(784));
                foreach(var button in buttons)
                {
                    Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(button),Is.EqualTo(WorldNavigationAuthoring.ButtonPath));
                    Assert.That(button.GetComponent<CanvasRenderer>(),Is.Not.Null);
                    Assert.That(button.GetComponent<RoundNavigationGraphic>(),Is.Not.Null);
                    Assert.That(button.GetComponentInChildren<Image>(true).sprite,Is.Not.Null);
                    var rect=button.GetComponent<RectTransform>();
                    Assert.That(Mathf.Abs(rect.anchoredPosition.x)+rect.sizeDelta.x*.5f,Is.LessThanOrEqualTo(navigation.Dock.sizeDelta.x*.5f),button.name+" must fit inside the responsive dock");
                }
                var map=AssetDatabase.LoadAssetAtPath<DistrictMap>(BurningHillsLayoutInstaller.MapPath);
                foreach(var site in root.Sites)
                {
                    var point=DistrictMap.ToPlan(site.transform.position);
                    float projection=Vector3.ProjectOnPlane(root.CameraRig.RenderCamera.transform.up,Vector3.up).magnitude;
                    Assert.That(map.ClampNavigation(point,0,16f/9f,projection),Is.EqualTo(point),site.InstanceId);
                }
                Assert.That(navigation.Dock.anchorMin,Is.EqualTo(new Vector2(.5f,0)));
                Assert.That(navigation.Dock.anchorMax,Is.EqualTo(new Vector2(.5f,0)));
                Assert.That(root.Ui.Interaction.townMenu.GetComponentsInChildren<RoundNavigationGraphic>(true),Is.Empty);
                var header=root.Ui.GetComponentInChildren<CityStatusBar>(true);
                Assert.That(header,Is.Not.Null);Assert.That(header.Settings.GetComponent<RoundNavigationGraphic>(),Is.Not.Null);
                Assert.That(header.Resources.Cast<Transform>().Select(x=>x.name),Is.EqualTo(new[]{"Money","Grinders","Residents"}));
                Assert.That(root.Presentation.currencyValues[2].transform.parent.name,Is.EqualTo("Cards"));
            }
            finally {EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}
