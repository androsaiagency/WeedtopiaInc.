using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit installation into the shared site prefab; existing building wrappers stay intact.</summary>
    public static class ConstructionFenceAuthoring
    {
        public const string Folder = "Assets/Weedtopia/Production/Construction";
        public const string FencePrefab = Folder + "/Prefabs/ConstructionFence.prefab";
        public const string SitePrefab = "Assets/Weedtopia/Game/Prefabs/BuildingSite.prefab";

        [MenuItem("Weedtopia/Presentation/Install Construction Fences")]
        public static void Apply()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EnsureAssets();
                var site = PrefabUtility.LoadPrefabContents(SitePrefab);
                try
                {
                    var view = site.GetComponent<BuildingView>();
                    if (view.ConstructionFence == null)
                    {
                        var fence = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FencePrefab), site.transform);
                        fence.name = "ConstructionFence";
                        var fields = new SerializedObject(view);
                        fields.FindProperty("constructionFence").objectReferenceValue = fence.GetComponent<ConstructionFenceView>();
                        fields.ApplyModifiedPropertiesWithoutUndo();
                        PrefabUtility.SaveAsPrefabAsset(site, SitePrefab);
                    }
                }
                finally { PrefabUtility.UnloadPrefabContents(site); }
                AssetDatabase.SaveAssets();
                EditorSceneManager.OpenScene(GameScaffold.GameScene);
                var root = Object.FindFirstObjectByType<GameRoot>();
                if (root.Sites.Any(s => s.ConstructionFence == null || s.ConstructionFence.Sides.Length != 4 || s.ConstructionFence.Posts.Length != 4))
                    throw new InvalidOperationException("Every building site must inherit the authored enclosure.");
                Directory.CreateDirectory("Docs/ConstructionFences");
                File.WriteAllText("Docs/ConstructionFences/authoring_receipt.json", JsonUtility.ToJson(new Receipt {
                    utc = DateTime.UtcNow.ToString("O"), sites = root.Sites.Length, sharedPrefab = FencePrefab,
                    fixedSpriteRenderersPerSite = 8, sceneSaved = false }, true));
            }
            finally { if (!UnityEngine.Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        private static void EnsureAssets()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(FencePrefab) != null) return;
            Directory.CreateDirectory(Folder + "/Prefabs");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Import("FencePanel", 3.2f, new Vector2(.5f, 0));
            Import("WarningPost", 4.4f, new Vector2(.5f, .10f));
            var character = AssetDatabase.LoadAssetAtPath<GameObject>(ProductionArtImporter.Root + "/Prefabs/Character_rocko.prefab");
            var material = character.GetComponentInChildren<SpriteRenderer>(true).sharedMaterial;
            var fence = new GameObject("ConstructionFence", typeof(ConstructionFenceView));
            try
            {
                var sides = new SpriteRenderer[4]; var posts = new SpriteRenderer[4];
                for (int i = 0; i < 4; i++)
                {
                    sides[i] = Sprite("Panel_" + i, "FencePanel", fence.transform, material);
                    sides[i].drawMode = SpriteDrawMode.Tiled; sides[i].tileMode = SpriteTileMode.Continuous;
                    sides[i].transform.localRotation = Quaternion.Euler(0, i % 2 == 0 ? 0 : 90, 0);
                    posts[i] = Sprite("WarningPost_" + i, "WarningPost", fence.transform, material);
                    posts[i].transform.localRotation = Quaternion.Euler(35.264f, 45, 0);
                }
                var view = fence.GetComponent<ConstructionFenceView>(); view.EditorConfigure(sides, posts);
                view.Present(true, 16, null); fence.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(fence, FencePrefab);
            }
            finally { Object.DestroyImmediate(fence); }
        }
        private static SpriteRenderer Sprite(string name, string art, Transform parent, Material material)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer)); obj.transform.SetParent(parent, false);
            var sprite = obj.GetComponent<SpriteRenderer>(); sprite.sharedMaterial = material;
            sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Art/" + art + ".png");
            return sprite;
        }
        private static void Import(string name, float height, Vector2 pivot)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + "/Art/" + name + ".png");
            importer.GetSourceTextureWidthAndHeight(out _, out int pixels);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixels / height; importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true; importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 8;
            importer.maxTextureSize = 4096; importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = pivot;
            importer.SetTextureSettings(settings); importer.SaveAndReimport();
        }
        [Serializable] private sealed class Receipt { public string utc, sharedPrefab; public int sites, fixedSpriteRenderersPerSite; public bool sceneSaved; }
    }
}
