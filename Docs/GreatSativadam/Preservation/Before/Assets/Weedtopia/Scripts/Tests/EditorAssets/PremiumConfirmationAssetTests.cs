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
    public sealed class PremiumConfirmationAssetTests
    {
        [Test] public void AllCitiesBindTheExistingConfirmationControlsAndInstallerIsIdempotent()
        {
            var paths = new[] { GameScaffold.GameScene, BigRockCityAuthoring.ScenePath, SevenHillsAuthoring.ScenePath };
            var before = paths.ToDictionary(path => path, File.ReadAllBytes);
            PremiumConfirmationAuthoring.Apply();
            foreach (var path in paths) Assert.That(File.ReadAllBytes(path), Is.EqualTo(before[path]), path);
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (var path in paths)
                {
                    EditorSceneManager.OpenScene(path);
                    var root = Object.FindFirstObjectByType<GameRoot>();
                    var layout = root.Ui.GetComponentInChildren<PremiumConfirmationLayout>(true);
                    Assert.That(layout, Is.Not.Null, path);
                    Assert.That(layout.frame, Is.SameAs(layout.GetComponent<ModalFrame>()));
                    Assert.That(layout.message.name, Is.EqualTo("premiumText"));
                    Assert.That(layout.cancel.name, Is.EqualTo("ui.cancel")); Assert.That(layout.confirm.name, Is.EqualTo("ui.confirm"));
                    Assert.That(layout.frame.headingSource.GetComponent<LocalizedLabel>().Key, Is.EqualTo("ui.premium_title"));
                    Assert.That(layout.message.GetComponents<LocalizedLabel>(), Is.Empty, "An offer body belongs to its dynamic presenter.");
                }
            }
            finally { if (setup.Any(s => s.isLoaded)) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        [TestCase(1920, 1080, 1f)]
        [TestCase(844, 390, .25f)]
        [TestCase(844, 390, .75f)]
        [TestCase(390, 844, .25f)]
        [TestCase(390, 844, 1f)]
        public void WideAndCompactLayoutsKeepTextReadableAndActionsInsideTheViewport(int width, int height, float pixels)
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            RectTransform safe = null;
            try
            {
                EditorSceneManager.OpenScene(GameScaffold.GameScene);
                safe = new GameObject("PremiumGeometrySafeArea", typeof(RectTransform)).GetComponent<RectTransform>();
                var root = Object.FindFirstObjectByType<GameRoot>();
                var layout = root.Ui.GetComponentInChildren<PremiumConfirmationLayout>(true);
                Assert.That(layout, Is.Not.Null);
                layout.gameObject.SetActive(true);
                layout.message.text = "Потратить 25 гриндеров на участок выращивания? Сумма будет списана только после подтверждения.";
                layout.cancel.GetComponentInChildren<Text>(true).text = "Отмена";
                layout.confirm.GetComponentInChildren<Text>(true).text = "Подтвердить расход гриндеров";
                safe.sizeDelta = new Vector2(width, height) / pixels;
                layout.frame.Fit(safe, pixels);
                Assert.That(layout.frame.body.localScale, Is.EqualTo(Vector3.one), "Readable controls must not be shrunk with the legacy 830x450 body.");
                Assert.That(layout.message.fontSize * pixels, Is.GreaterThanOrEqualTo(22));
                Assert.That(layout.message.rectTransform.rect.height + .1f, Is.GreaterThanOrEqualTo(DialoguePanel.PreferredHeight(layout.message, layout.message.rectTransform.rect.width)));
                AssertInside(layout.frame.viewport, layout.message.rectTransform);
                foreach (var button in new[] { layout.cancel, layout.confirm })
                {
                    var rect = (RectTransform)button.transform; var caption = button.GetComponentInChildren<Text>(true);
                    Assert.That(rect.rect.height * pixels, Is.GreaterThanOrEqualTo(50));
                    Assert.That(caption.fontSize * pixels, Is.GreaterThanOrEqualTo(20));
                    Assert.That(caption.rectTransform.rect.height + .1f, Is.GreaterThanOrEqualTo(DialoguePanel.PreferredHeight(caption, caption.rectTransform.rect.width) - 8));
                    AssertInside(layout.frame.viewport, rect);
                }
            }
            finally
            {
                if (safe != null) Object.DestroyImmediate(safe.gameObject);
                if (setup.Any(s => s.isLoaded)) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }
        private static void AssertInside(RectTransform viewport, RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var point = viewport.InverseTransformPoint(corner);
                Assert.That(point.x, Is.InRange(viewport.rect.xMin - .1f, viewport.rect.xMax + .1f), rect.name);
                Assert.That(point.y, Is.InRange(viewport.rect.yMin - .1f, viewport.rect.yMax + .1f), rect.name);
            }
        }
    }
}
