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
    /// <summary>Explicit, idempotent binding of the existing premium confirmation controls in both cities.</summary>
    public static class PremiumConfirmationAuthoring
    {
        [MenuItem("Weedtopia/Presentation/Install Premium Confirmation Layout")]
        public static void Apply()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            int changedScenes = 0;
            try
            {
                foreach (string path in new[] { GameScaffold.GameScene, BigRockCityAuthoring.ScenePath })
                {
                    var scene = EditorSceneManager.OpenScene(path);
                    var root = Object.FindFirstObjectByType<GameRoot>();
                    var bindings = new SerializedObject(root.Ui).FindProperty("bindings");
                    GameObject Bound(string id)
                    {
                        for (int i = 0; i < bindings.arraySize; i++)
                        {
                            var entry = bindings.GetArrayElementAtIndex(i);
                            if (entry.FindPropertyRelative("id").stringValue == id)
                                return (GameObject)entry.FindPropertyRelative("target").objectReferenceValue;
                        }
                        throw new InvalidOperationException("Missing premium UI binding: " + id + " in " + path);
                    }
                    var panel = Bound("premium"); var frame = panel.GetComponent<ModalFrame>();
                    if (frame == null || frame.body == null || frame.viewport == null)
                        throw new InvalidOperationException("Install the authored ModalFrame before premium layout: " + path);
                    var message = Bound("premiumText").GetComponent<Text>();
                    var cancel = Bound("ui.cancel").GetComponent<Button>(); var confirm = Bound("ui.confirm").GetComponent<Button>();
                    if (message == null || cancel == null || confirm == null)
                        throw new InvalidOperationException("Invalid authored premium confirmation controls: " + path);
                    var layout = panel.GetComponent<PremiumConfirmationLayout>(); bool changed = layout == null;
                    if (layout == null) layout = panel.AddComponent<PremiumConfirmationLayout>();
                    if (layout.frame != frame || layout.message != message || layout.cancel != cancel || layout.confirm != confirm)
                    {
                        layout.frame = frame; layout.message = message; layout.cancel = cancel; layout.confirm = confirm;
                        changed = true;
                    }
                    if (changed)
                    {
                        EditorUtility.SetDirty(layout); PrefabUtility.RecordPrefabInstancePropertyModifications(layout);
                        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); changedScenes++;
                    }
                }
                AssetDatabase.SaveAssets(); Directory.CreateDirectory("Docs/LocalizationRevision");
                File.WriteAllText("Docs/LocalizationRevision/premium_authoring_receipt.json", JsonUtility.ToJson(new Receipt
                {
                    utc = DateTime.UtcNow.ToString("O"), scenes = 2, changedScenes = changedScenes,
                    existingControlReferencesPreserved = true, runtimeStaticObjects = false
                }, true));
            }
            finally { if (setup.Any(s => s.isLoaded) && setup.Any(s => s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        [Serializable] private sealed class Receipt
        {
            public string utc;
            public int scenes, changedScenes;
            public bool existingControlReferencesPreserved, runtimeStaticObjects;
        }
    }
}