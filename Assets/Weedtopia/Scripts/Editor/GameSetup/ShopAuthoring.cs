using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Content;
using Weedtopia.Domain;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit, idempotent shop installation. Existing scene and navigation objects retain their identity.</summary>
    public static class ShopAuthoring
    {
        public const string Folder = "Assets/Weedtopia/Production/Prefabs/Shop";
        public const string PanelPath = Folder + "/ShopPanel.prefab", CardPath = Folder + "/ShopProductCard.prefab";
        private static PresentationArt art;
        private static Sprite panel, primary;
        private static GameDefinitions definitions;
        [MenuItem("Weedtopia/Shop/Install Shop")]
        public static void Apply()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                art = AssetDatabase.LoadAssetAtPath<PresentationArt>(ProductionArtImporter.ArtPath);
                definitions = AssetDatabase.LoadAssetAtPath<GameContent>(GameScaffold.ContentPath).Load();
                if (definitions.shopProducts.Length != 17) throw new InvalidOperationException("The approved catalog must contain 17 products.");
                panel = AssetDatabase.LoadAssetAtPath<Sprite>(UiPolishAuthoring.Skin + "/Panel.png");
                primary = AssetDatabase.LoadAssetAtPath<Sprite>(UiPolishAuthoring.Skin + "/Primary.png");
                Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
                if (AssetDatabase.LoadAssetAtPath<GameObject>(CardPath) == null) BuildCard();
                if (AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath) == null) BuildPanel();
                foreach (var prefabPath in new[] { CardPath, PanelPath })
                {
                    var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
                    try
                    {
                        bool captionsChanged = false;
                        foreach (var button in prefab.GetComponentsInChildren<Button>(true))
                        {
                            var caption = button.GetComponentInChildren<Text>(true);
                            if (caption == null || caption.alignment == TextAnchor.MiddleCenter) continue;
                            caption.alignment = TextAnchor.MiddleCenter; PrefabUtility.RecordPrefabInstancePropertyModifications(caption); captionsChanged = true;
                        }
                        if (captionsChanged) PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                    }
                    finally { PrefabUtility.UnloadPrefabContents(prefab); }
                }
                var dock = PrefabUtility.LoadPrefabContents(WorldNavigationAuthoring.DockPath);
                try
                {
                    if (dock.transform.Find("Shop") == null)
                    {
                        var button = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(WorldNavigationAuthoring.ButtonPath), dock.transform);
                        button.name = "Shop";
                        button.GetComponentInChildren<Image>().sprite = art.Get("gift");
                        PrefabUtility.RecordPrefabInstancePropertyModifications(button.GetComponentInChildren<Image>());
                        PrefabUtility.SaveAsPrefabAsset(dock, WorldNavigationAuthoring.DockPath);
                    }
                }
                finally { PrefabUtility.UnloadPrefabContents(dock); }
                foreach (var path in new[] { GameScaffold.GameScene, BigRockCityAuthoring.ScenePath })
                {
                    var scene = EditorSceneManager.OpenScene(path);
                    var root = Object.FindFirstObjectByType<GameRoot>();
                    var serialized = new SerializedObject(root.Ui);
                    var bindings = serialized.FindProperty("bindings"); var panels = serialized.FindProperty("panels");
                    bool changed = false;
                    Transform modalParent = null;
                    for (int i = 0; i < bindings.arraySize; i++)
                    {
                        var binding = bindings.GetArrayElementAtIndex(i);
                        if (binding.FindPropertyRelative("id").stringValue == "modalShade")
                            modalParent = ((GameObject)binding.FindPropertyRelative("target").objectReferenceValue).transform;
                    }
                    if (modalParent == null) throw new InvalidOperationException("The shared modal layer is missing.");
                    var view = root.Ui.Shop;
                    if (view == null)
                    {
                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath), modalParent);
                        instance.name = "Shop"; instance.SetActive(false); view = instance.GetComponent<ShopUi>();
                        serialized.FindProperty("shop").objectReferenceValue = view;
                        panels.InsertArrayElementAtIndex(panels.arraySize); panels.GetArrayElementAtIndex(panels.arraySize - 1).objectReferenceValue = instance;
                        changed = true;
                    }
                    else if (view.transform.parent != modalParent) { view.transform.SetParent(modalParent, false); changed = true; }
                    void Bind(string id, GameObject target)
                    {
                        for (int i = 0; i < bindings.arraySize; i++)
                        {
                            var item = bindings.GetArrayElementAtIndex(i);
                            if (item.FindPropertyRelative("id").stringValue != id) continue;
                            if (item.FindPropertyRelative("target").objectReferenceValue != target) { item.FindPropertyRelative("target").objectReferenceValue = target; changed = true; }
                            return;
                        }
                        bindings.InsertArrayElementAtIndex(bindings.arraySize); var entry = bindings.GetArrayElementAtIndex(bindings.arraySize - 1);
                        entry.FindPropertyRelative("id").stringValue = id; entry.FindPropertyRelative("target").objectReferenceValue = target; changed = true;
                    }
                    Bind("shop", view.gameObject);
                    Bind("shopDock", root.Ui.GetComponent<WorldNavigationBar>().Dock.Find("Shop").gameObject);
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                    if (view.cards.Length != 17 || root.Ui.GetComponent<WorldNavigationBar>().Dock.GetComponentsInChildren<Button>(true).Length != 10)
                        throw new InvalidOperationException("Incomplete shop references: " + path);
                }
                AssetDatabase.SaveAssets(); Directory.CreateDirectory("Docs/ShopRevision");
                File.WriteAllText("Docs/ShopRevision/authoring_receipt.json", "{\"utc\":\"" + DateTime.UtcNow.ToString("O") + "\",\"status\":\"passed\",\"products\":17,\"scenes\":2,\"navigationButtons\":10,\"runtimeStaticObjects\":false}");
            }
            finally { if (setup.Any(s => s.isLoaded) && setup.Any(s => s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        private static void BuildCard()
        {
            var rect = Rect("ShopProductCard", null); Surface(rect);
            var card = rect.gameObject.AddComponent<ShopProductCard>();
            card.ribbon = Label("Ribbon", rect, "PAKET", new Color(.82f, .86f, .55f));
            card.title = Label("Title", rect, "Paket", new Color(1, .82f, .4f), true);
            card.icon = Picture("Icon", rect, art.Get("gift"));
            card.contents = Label("Contents", rect, "", new Color(.97f, .96f, .85f));
            card.price = Label("Price", rect, "", new Color(.77f, .86f, .8f));
            card.purchase = Button("Purchase", rect, "Testkauf · kostenlos", true);
            PrefabUtility.SaveAsPrefabAsset(rect.gameObject, CardPath); Object.DestroyImmediate(rect.gameObject);
        }
        private static void BuildPanel()
        {
            var root = Rect("ShopPanel", null); root.sizeDelta = new Vector2(1500, 1100); Surface(root);
            var frame = root.gameObject.AddComponent<ModalFrame>(); frame.fillAvailable = true; frame.authoredSize = root.sizeDelta;
            frame.heading = Label("Heading", root, "Shop", new Color(.82f, .88f, .6f), true);
            frame.viewport = Rect("Viewport", root); Stretch(frame.viewport); frame.viewport.gameObject.AddComponent<RectMask2D>();
            var target = frame.viewport.gameObject.AddComponent<Image>(); target.sprite = panel; target.color = Color.clear; target.raycastTarget = true;
            frame.body = Rect("Body", frame.viewport); Stretch(frame.body);
            var view = root.gameObject.AddComponent<ShopUi>();
            var layout = root.gameObject.AddComponent<ShopPanelLayout>(); layout.frame = frame; layout.ui = view;
            view.catalog = Rect("Catalog", frame.body); Stretch(view.catalog);
            view.mode = Label("Mode", view.catalog, "TESTSHOP · Keine echten Zahlungen", new Color(1, .82f, .4f), true);
            view.balance = Label("Balance", view.catalog, "", new Color(.97f, .96f, .85f));
            view.status = Label("Status", view.catalog, "", new Color(.63f, .9f, .72f));
            string[] names = { "Pakete", "VIP", "Geld", "Grinder", "Boosts" };
            view.categories = names.Select(name => Button("Category_" + name, view.catalog, name)).ToArray();
            view.productScroll = Scroll("Products", view.catalog);
            view.cards = definitions.shopProducts.Select(product =>
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CardPath), view.productScroll.content);
                instance.name = product.id; var card = instance.GetComponent<ShopProductCard>(); card.productId = product.id;
                card.title.text = definitions.Text(product.titleKey); card.icon.sprite = art.Get(product.icon);
                PrefabUtility.RecordPrefabInstancePropertyModifications(card);
                PrefabUtility.RecordPrefabInstancePropertyModifications(card.title);
                PrefabUtility.RecordPrefabInstancePropertyModifications(card.icon);
                return card;
            }).ToArray();
            view.confirmation = Rect("Confirmation", frame.body); Stretch(view.confirmation);
            view.confirmationScroll = Scroll("Summary", view.confirmation);
            var content = view.confirmationScroll.content;
            view.confirmTitle = Label("Title", content, "", new Color(1, .82f, .4f), true);
            view.confirmIcon = Picture("Icon", content, art.Get("gift"));
            view.confirmContents = Label("Contents", content, "", new Color(.97f, .96f, .85f));
            view.confirmPrice = Label("Price", content, "", new Color(1, .82f, .4f), true);
            view.confirmNotice = Label("Notice", content, "", new Color(.77f, .86f, .8f));
            view.confirmPurchase = Button("ConfirmPurchase", view.confirmation, "Kostenlos bestätigen", true);
            view.confirmBack = Button("Back", view.confirmation, "Zurück zum Shop");
            root.gameObject.SetActive(false); PrefabUtility.SaveAsPrefabAsset(root.gameObject, PanelPath); Object.DestroyImmediate(root.gameObject);
        }
        private static ScrollRect Scroll(string name, Transform parent)
        {
            var rect = Rect(name, parent); var scroll = rect.gameObject.AddComponent<ScrollRect>();
            var viewport = Rect("Viewport", rect); Stretch(viewport); viewport.gameObject.AddComponent<RectMask2D>();
            var image = viewport.gameObject.AddComponent<Image>(); image.sprite = panel; image.color = Color.clear;
            var content = Rect("Content", viewport); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40; return scroll;
        }
        private static Button Button(string name, Transform parent, string caption, bool featured = false)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UiExperienceAuthoring.Folder + "/QuestRow.prefab"), parent);
            instance.name = name; var button = instance.GetComponent<Button>();
            if (featured) { button.GetComponent<Image>().sprite = primary; button.GetComponent<Image>().color = Color.white; }
            var label = button.GetComponentInChildren<Text>(true); label.text = caption; label.color = new Color(.97f, .96f, .85f); label.alignment = TextAnchor.MiddleCenter;
            button.navigation = new Navigation { mode = Navigation.Mode.None }; return button;
        }
        private static RectTransform Rect(string name, Transform parent)
        { var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect; }
        private static Text Label(string name, Transform parent, string value, Color color, bool bold = false)
        {
            var text = Rect(name, parent).gameObject.AddComponent<Text>(); text.font = bold ? art.headingFont : art.bodyFont;
            text.text = value; text.fontSize = 26; text.color = color; text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; return text;
        }
        private static Image Picture(string name, Transform parent, Sprite sprite)
        { var image = Rect(name, parent).gameObject.AddComponent<Image>(); image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false; return image; }
        private static void Surface(RectTransform rect)
        { var image = rect.gameObject.AddComponent<Image>(); image.sprite = panel; image.type = Image.Type.Sliced; image.color = Color.white; }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    }
}
