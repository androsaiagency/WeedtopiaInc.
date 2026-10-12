using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit, repeatable migration of owned UI controls; world objects and gameplay bindings retain their identity.</summary>
    public static class PhoneUiAuthoring
    {
        public const string Folder = UiExperienceAuthoring.Folder;
        public const string ScrollPath = Folder + "/BuildingDetailsScroll.prefab";
        public const string BackPath = Folder + "/StoryBackButton.prefab";
        [MenuItem("Weedtopia/Presentation/Install Phone Building and Story UI")]
        public static void Apply()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EnsurePrefabs();
                foreach (string path in new[] { VipStoriesAuthoring.PanelPath, BigRockCityAuthoring.StoriesPath })
                {
                    var panel = PrefabUtility.LoadPrefabContents(path);
                    try { if (Stories(panel.GetComponent<VipStoriesUi>())) PrefabUtility.SaveAsPrefabAsset(panel, path); }
                    finally { PrefabUtility.UnloadPrefabContents(panel); }
                }
                foreach (string path in new[] { GameScaffold.GameScene, BigRockCityAuthoring.ScenePath })
                {
                    var scene = EditorSceneManager.OpenScene(path);
                    var root = Object.FindFirstObjectByType<GameRoot>();
                    bool changed = Building(root.Ui.Interaction);
                    // Inherited prefab additions are present after load; scene-owned instances also receive the route.
                    changed |= Stories(root.Ui.Stories);
                    if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                    if (root.Ui.Interaction.inspector.GetComponent<BuildingInspectorLayout>() == null || root.Ui.Stories.GetComponent<VipStoryPanelLayout>() == null)
                        throw new InvalidOperationException("Phone UI binding missing: " + path);
                }
                AssetDatabase.SaveAssets();
                Directory.CreateDirectory("Docs/PhoneUiRevision");
                File.WriteAllText("Docs/PhoneUiRevision/authoring_receipt.json", "{\"utc\":\"" + DateTime.UtcNow.ToString("O") + "\",\"status\":\"passed\",\"scenes\":2,\"storyPrefabs\":2,\"runtimeStaticObjects\":false}");
            }
            finally { if (setup.Any(s => s.isLoaded) && setup.Any(s => s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        private static void EnsurePrefabs()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ScrollPath) == null)
            {
                var area = Rect("BuildingDetailsScroll", null);
                var scroll = area.gameObject.AddComponent<ScrollRect>();
                var viewport = Rect("Viewport", area); Stretch(viewport); viewport.gameObject.AddComponent<RectMask2D>();
                var image = viewport.gameObject.AddComponent<Image>(); image.color = Color.clear; image.raycastTarget = true;
                image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiPolishAuthoring.Skin + "/Panel.png"); image.type = Image.Type.Sliced;
                var content = Rect("Content", viewport);
                content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
                scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;
                PrefabUtility.SaveAsPrefabAsset(area.gameObject, ScrollPath); Object.DestroyImmediate(area.gameObject);
            }
            else
            {
                var prefab = PrefabUtility.LoadPrefabContents(ScrollPath);
                try
                {
                    var image = prefab.GetComponent<ScrollRect>().viewport.GetComponent<Image>();
                    if (image.sprite == null)
                    { image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiPolishAuthoring.Skin + "/Panel.png"); image.type = Image.Type.Sliced; PrefabUtility.SaveAsPrefabAsset(prefab, ScrollPath); }
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BackPath) == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/QuestRow.prefab");
                if (prefab == null) throw new InvalidOperationException("Missing shared quest button prefab.");
                var action = (GameObject)PrefabUtility.InstantiatePrefab(prefab); action.name = "BackToStories";
                var button = action.GetComponent<Button>(); button.onClick = new Button.ButtonClickedEvent();
                button.GetComponentInChildren<Text>().text = "‹  Zur Übersicht";
                PrefabUtility.SaveAsPrefabAsset(action, BackPath); Object.DestroyImmediate(action);
            }
        }
        private static bool Building(BuildingInteractionUi ui)
        {
            if (ui.inspector.GetComponent<BuildingInspectorLayout>() != null) return false;
            var layout = ui.inspector.AddComponent<BuildingInspectorLayout>(); layout.ui = ui;
            layout.scroll = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ScrollPath), ui.inspector.transform)).GetComponent<ScrollRect>();
            layout.metricsHeading = ui.inspector.transform.Find("MetricsTitle").GetComponent<Text>();
            layout.previewHeading = ui.inspector.transform.Find("UpgradePreviewLabel").GetComponent<Text>();
            var content = layout.scroll.content;
            foreach (var t in new Component[] { ui.staffPortrait, ui.staffAction, ui.staffName, ui.staffBody, ui.train, ui.rank, ui.fleet, ui.status, layout.metricsHeading, layout.previewHeading })
                t.transform.SetParent(content, false);
            foreach (var label in ui.metricLabels) label.transform.parent.SetParent(content, false);
            layout.scroll.transform.SetSiblingIndex(1);
            EditorUtility.SetDirty(layout); return true;
        }
        private static bool Stories(VipStoriesUi ui)
        {
            if (ui.GetComponent<VipStoryPanelLayout>() != null) return StoryOrder(ui);
            var layout = ui.gameObject.AddComponent<VipStoryPanelLayout>(); layout.ui = ui; layout.frame = ui.GetComponent<ModalFrame>();
            layout.back = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BackPath), layout.frame.body)).GetComponent<Button>();
            var content = ui.detailScroll.content;
            var paragraphs = new[] { ui.nameLabel, ui.role, ui.questTitle, ui.description };
            int index = 0;
            foreach (var text in paragraphs) { Flow(text, content); text.transform.SetSiblingIndex(index++); }
            foreach (var text in new[] { ui.biography, ui.greeting, ui.memories, ui.hint }) Flow(text, content);
            ui.portrait.transform.SetParent(content, false);
            if (ui.portrait.GetComponent<LayoutElement>() == null) ui.portrait.gameObject.AddComponent<LayoutElement>();
            foreach (var action in new[] { ui.speak, ui.focus, ui.remember })
            {
                action.transform.SetParent(content, false);
                if (action.GetComponent<LayoutElement>() == null) action.gameObject.AddComponent<LayoutElement>();
            }
            foreach (var action in new[] { ui.accept, ui.alternate, ui.claim, ui.speak, ui.focus, ui.remember })
                if (action.GetComponent<CanvasGroup>() == null) action.gameObject.AddComponent<CanvasGroup>();
            StoryOrder(ui); EditorUtility.SetDirty(layout); return true;
        }
        private static bool StoryOrder(VipStoriesUi ui)
        {
            // Show the actual checklist before the longer story prose, so the next action is visible immediately.
            bool changed = false; int index = 0;
            var order = new Component[] { ui.nameLabel, ui.role, ui.questTitle }.Concat(ui.goals)
                .Concat(new Component[] { ui.description, ui.reward, ui.biography, ui.greeting, ui.portrait, ui.memories, ui.speak, ui.focus, ui.remember, ui.hint });
            foreach (var item in order)
            { if (item.transform.GetSiblingIndex() != index) { item.transform.SetSiblingIndex(index); changed = true; } index++; }
            return changed;
        }
        private static void Flow(Text text, Transform parent)
        {
            text.transform.SetParent(parent, false); text.alignment = TextAnchor.UpperLeft;
            if (text.GetComponent<VipStoryParagraph>() == null) text.gameObject.AddComponent<VipStoryParagraph>().label = text;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
        }
        private static RectTransform Rect(string name, Transform parent)
        { var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); if (parent != null) r.SetParent(parent, false); return r; }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    }
}
