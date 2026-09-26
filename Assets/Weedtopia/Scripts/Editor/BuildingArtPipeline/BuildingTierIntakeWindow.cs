using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Weedtopia.Buildings.Presentation;

namespace Weedtopia.Editor.BuildingArtPipeline
{
    public sealed class BuildingTierIntakeWindow : EditorWindow
    {
        private BuildingArtManifest manifest;
        private string loadError;
        private string search = "";
        private int familyFilter;
        private string[] familyNames = { "Alle Familien" };
        private Vector2 listScroll;
        private Vector2 detailScroll;
        private string selectedKey;
        private GameObject selectedModel;
        private BuildingValidationReport report;
        private string operationMessage;

        [MenuItem("Weedtopia/Art Pipeline/Building Tier Intake")]
        public static void Open() => GetWindow<BuildingTierIntakeWindow>("Building Tier Intake");

        private void OnEnable()
        {
            minSize = new Vector2(850, 520);
            Reload();
        }

        private void Reload()
        {
            try
            {
                manifest = BuildingManifestReader.Load();
                familyNames = new[] { "Alle Familien" }.Concat(manifest.slots.Select(s => s.building_id).Distinct()).ToArray();
                familyFilter = Mathf.Clamp(familyFilter, 0, familyNames.Length - 1);
                loadError = null;
                if (!string.IsNullOrEmpty(selectedKey)) SelectSlot(manifest.slots.FirstOrDefault(s => s.Key == selectedKey));
            }
            catch (Exception ex) { manifest = null; loadError = ex.Message; }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Manifest neu lesen", EditorStyles.toolbarButton, GUILayout.Width(140))) Reload();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Nur validieren (alle Slots)", EditorStyles.toolbarButton, GUILayout.Width(190)))
                Run(() => report = BuildingIntakeValidator.Validate(BuildingManifestReader.Load()));
            EditorGUILayout.EndHorizontal();
            if (manifest == null) { EditorGUILayout.HelpBox(loadError ?? "Manifest fehlt.", MessageType.Error); return; }
            if (!string.IsNullOrEmpty(operationMessage)) EditorGUILayout.HelpBox(operationMessage, MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            DrawSlotList();
            DrawDetails();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSlotList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(285));
            search = EditorGUILayout.TextField("Suche", search);
            familyFilter = EditorGUILayout.Popup(familyFilter, familyNames);
            IEnumerable<BuildingArtSlot> filtered = manifest.slots.Where(s =>
                (familyFilter == 0 || s.building_id == familyNames[familyFilter]) &&
                (string.IsNullOrWhiteSpace(search) || (s.Key + " " + s.building_name_de + " " + s.visual_name_de + " " + s.status)
                    .IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0));
            BuildingArtSlot[] slots = filtered.ToArray();
            EditorGUILayout.LabelField(slots.Length + " / " + manifest.slots.Length + " Slots");
            listScroll = EditorGUILayout.BeginScrollView(listScroll);
            foreach (BuildingArtSlot slot in slots)
            {
                bool selected = slot.Key == selectedKey;
                if (GUILayout.Toggle(selected, slot.building_name_de + " · " + slot.tier_id, "Button") && !selected) SelectSlot(slot);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void SelectSlot(BuildingArtSlot slot)
        {
            selectedKey = slot?.Key;
            GameObject prefab = slot == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(slot.unity_prefab_target);
            selectedModel = prefab != null ? prefab.GetComponent<BuildingVisualWrapper>()?.SourceModel : null;
            report = null;
        }

        private void DrawDetails()
        {
            detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
            BuildingArtSlot slot = manifest.slots.FirstOrDefault(s => s.Key == selectedKey);
            if (slot == null) EditorGUILayout.HelpBox("Gebäudestufe links auswählen.", MessageType.Info);
            else
            {
                EditorGUILayout.LabelField(slot.building_name_de + " / " + slot.tier_id, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Levelband", slot.min_level + "–" + slot.max_level);
                EditorGUILayout.LabelField("Epoche", slot.era);
                EditorGUILayout.LabelField("Visual", slot.visual_name_de);
                EditorGUILayout.LabelField("Fortschritt", slot.status);
                EditorGUILayout.LabelField(slot.visible_delta_from_previous, EditorStyles.wordWrappedLabel);
                EditorGUILayout.Space();
                DrawPath("Referenzbild", slot.reference_image);
                if (GUILayout.Button("Referenzbild lokal öffnen")) OpenLocal(slot.reference_image);
                DrawPath("Spezifikation", slot.asset_spec_file);
                if (GUILayout.Button("Asset-Spezifikation lokal öffnen")) OpenLocal(slot.asset_spec_file);
                EditorGUILayout.Space();
                string promptPath = BuildingManifestReader.AbsolutePath(slot.meshy_prompt_file);
                string prompt = File.Exists(promptPath) ? File.ReadAllText(promptPath) : "[Promptdatei fehlt]";
                EditorGUILayout.LabelField("Meshy-Prompt (Englisch)", EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(prompt, EditorStyles.textArea, GUILayout.MinHeight(110));
                using (new EditorGUI.DisabledScope(!File.Exists(promptPath)))
                    if (GUILayout.Button("Prompt kopieren")) EditorGUIUtility.systemCopyBuffer = prompt;
                DrawPath("FBX-Eingangsordner", slot.unity_incoming_folder);
                DrawPath("Wrapper-Ziel", slot.unity_prefab_target);
                selectedModel = (GameObject)EditorGUILayout.ObjectField("FBX / Model-Prefab", selectedModel, typeof(GameObject), false);
                EditorGUILayout.HelpBox("Den vollständigen FBX-Export inklusive Texturen im Eingangsordner importieren. Hier das Modell-Root aus dem Project-Fenster ablegen. Änderungen werden erst über den folgenden Button gespeichert.", MessageType.Info);
                using (new EditorGUI.DisabledScope(selectedModel == null))
                    if (GUILayout.Button("Wrapper mit diesem Modell erstellen / aktualisieren")) Run(() =>
                    {
                        BuildingIntakeService.AssignModel(slot, selectedModel);
                        BuildingIntakeService.RepairCatalog(manifest);
                        operationMessage = "Wrapper gespeichert: " + slot.Key + ". Quellen und Anker bleiben erhalten; Warnungen prüfen.";
                        report = new BuildingValidationReport();
                        BuildingIntakeValidator.ValidateSlot(slot, AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(BuildingManifestReader.CatalogPath), report);
                    });
                if (slot.building_id == "town_hall" && slot.tier_id == "T_FIXED")
                {
                    GameObject wrapperAsset = AssetDatabase.LoadAssetAtPath<GameObject>(slot.unity_prefab_target);
                    BuildingVisualWrapper townHallWrapper = wrapperAsset == null ? null : wrapperAsset.GetComponent<BuildingVisualWrapper>();
                    EditorGUILayout.HelpBox("Die Meshy-Base-Color bleibt als UV-Zuordnung erhalten. Das saubere Palettenmaterial glättet und quantisiert sie, ignoriert die fehlerhafte Normalmap und verändert weder FBX noch Quelltexturen.", MessageType.Info);
                    using (new EditorGUI.DisabledScope(townHallWrapper == null || townHallWrapper.ManagedModel == null))
                        if (GUILayout.Button("Rathaus: sauberes Palettenmaterial anwenden")) Run(() =>
                        {
                            int rendererCount = TownHallMaterialRepairUtility.ApplyToWrapper();
                            operationMessage = $"Sauberes Rathausmaterial auf {rendererCount} Renderer angewendet. Game View visuell prüfen.";
                        });
                }
                if (GUILayout.Button("Wrapper im Project-Fenster zeigen"))
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(slot.unity_prefab_target));
            }
            if (report != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Validierung: {report.errors} Fehler, {report.warnings} Hinweise", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Validierung verändert keine Assets oder Importsettings. Bildinhalt, Frontausrichtung und Collider benötigen zusätzlich manuelle QA.", MessageType.Info);
                foreach (BuildingValidationIssue issue in report.issues.Where(i => slot == null || i.slot == slot.Key || i.slot == "manifest" || i.slot == "catalog").Take(80))
                    EditorGUILayout.HelpBox(issue.slot + ": " + issue.message, issue.severity == "error" ? MessageType.Error : MessageType.Warning);
            }
            EditorGUILayout.EndScrollView();
        }

        private static void DrawPath(string label, string path)
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            EditorGUILayout.SelectableLabel(path, EditorStyles.wordWrappedLabel, GUILayout.Height(34));
        }

        private void OpenLocal(string relative)
        {
            Run(() =>
            {
                string absolute = BuildingManifestReader.AbsolutePath(relative);
                if (!File.Exists(absolute)) throw new FileNotFoundException("Lokale Datei fehlt: " + relative);
                EditorUtility.OpenWithDefaultApp(absolute);
            });
        }

        private void Run(Action operation)
        {
            try { operation(); }
            catch (Exception ex) { operationMessage = ex.Message; Debug.LogException(ex); }
        }
    }
}
