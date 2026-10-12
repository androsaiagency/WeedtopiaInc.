using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Content;
using Weedtopia.Domain;
using Weedtopia.Unity;
using Object=UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit, repeatable migration. Authors controls in scenes/prefabs; never creates fixed runtime UI.</summary>
    public static class LocalizationAuthoring
    {
        public const string Folder="Assets/Weedtopia/Game/Localization";
        public const string CatalogPath=Folder+"/LanguageCatalog.asset";
        public const string SelectorPath=Folder+"/LanguageSelector.prefab";
        [MenuItem("Weedtopia/Localization/Install or Repair")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode before installing localization");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save manual scene changes first");
            var setup=EditorSceneManager.GetSceneManagerSetup();AssetDatabase.Refresh();
            var catalog=AssetDatabase.LoadAssetAtPath<LanguageCatalog>(CatalogPath);
            if(catalog==null){catalog=ScriptableObject.CreateInstance<LanguageCatalog>();AssetDatabase.CreateAsset(catalog,CatalogPath);}
            // Existing/custom languages remain in the registry. Required languages are installed only once.
            var required=new[]{new[]{"de","Deutsch","de-DE"},new[]{"en","English","en-GB"},new[]{"es","Español","es-ES"},new[]{"fr","Français","fr-FR"},new[]{"ru","Русский","ru-RU"}};
            var languages=(catalog.languages??Array.Empty<AuthoredLanguage>()).ToList();
            foreach(var values in required)
            {
                var entry=languages.FirstOrDefault(l=>l.code==values[0]);
                if(entry==null){entry=new AuthoredLanguage{code=values[0],nativeName=values[1],culture=values[2]};languages.Add(entry);}
                if(entry.catalog==null)entry.catalog=AssetDatabase.LoadAssetAtPath<TextAsset>(Folder+"/"+entry.code+".json");
            }
            catalog.languages=languages.ToArray();
            if(catalog.bodyFont==null)catalog.bodyFont=TypographyAuthoring.LoadBody();
            if(catalog.headingFont==null)catalog.headingFont=TypographyAuthoring.LoadHeading();
            EditorUtility.SetDirty(catalog);var lookup=catalog.Create();
            var aliases=new Dictionary<string,string>(StringComparer.Ordinal);
            const string legacyCaptions="Tools/Localization/legacy_caption_sources.json";
            if(File.Exists(legacyCaptions))
                foreach(var entry in JsonUtility.FromJson<LocaleDocument>(File.ReadAllText(legacyCaptions)).entries)
                    if(!aliases.ContainsKey(entry.value))aliases.Add(entry.value,entry.key);
            var content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Weedtopia/Game/Content/BurningHillsContent.asset");
            var contentSo=new SerializedObject(content);contentSo.FindProperty("languageCatalog").objectReferenceValue=catalog;contentSo.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(content);
            foreach(var mapPath in new[]{"Assets/Weedtopia/Production/Layout/DistrictMap.asset","Assets/Weedtopia/Production/BigRockCity/DistrictMap.asset"})
            {
                var map=AssetDatabase.LoadAssetAtPath<DistrictMap>(mapPath);if(map==null)continue;
                foreach(var range in map.districts)if(string.IsNullOrEmpty(range.labelKey))range.labelKey=lookup.KeyForSource(range.label);
                EditorUtility.SetDirty(map);
            }
            int labels=0,scenes=0;var unbound=new List<string>();
            try
            {
                // Author reusable sources first so freshly opened scenes inherit those bindings.
                foreach(var file in CaptionPrefabPaths())
                {
                    var instance=PrefabUtility.LoadPrefabContents(file);
                    try{int added=BindLabels(instance,lookup,aliases);unbound.AddRange(UnboundCaptions(instance,lookup).Select(path=>file+"/"+path));if(added>0){PrefabUtility.SaveAsPrefabAsset(instance,file);labels+=added;}}
                    finally{PrefabUtility.UnloadPrefabContents(instance);}
                }
                foreach(var scenePath in new[]{GameScaffold.GameScene,"Assets/Weedtopia/Game/Scenes/BigRockCity.unity"}.Where(File.Exists))
                {
                    var scene=EditorSceneManager.OpenScene(scenePath);var root=scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<GameRoot>(true)).Single();
                    var ui=root.Ui;var experience=ui.Experience;
                    if(experience==null)throw new InvalidOperationException("Missing authored settings experience");
                    if(experience.language==null)
                    {
                        if(AssetDatabase.LoadAssetAtPath<GameObject>(SelectorPath)==null)
                        {
                            var module=new GameObject("LanguageSelector",typeof(RectTransform));var rect=module.GetComponent<RectTransform>();
                            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                            var dropdown=Object.Instantiate(Bound<Dropdown>(ui,"filter"),module.transform,false);dropdown.name="LanguageDropdown";
                            dropdown.onValueChanged=new Dropdown.DropdownEvent();dropdown.ClearOptions();dropdown.AddOptions(languages.Select(l=>l.nativeName).ToList());
                            var caption=Object.Instantiate(Bound<Text>(ui,"volumeLabel"),module.transform,false);caption.name="LanguageLabel";caption.text=lookup.Get("settings.language");
                            BindLabels(module,lookup,aliases);PrefabUtility.SaveAsPrefabAsset(module,SelectorPath);Object.DestroyImmediate(module);
                        }
                        var selector=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SelectorPath),experience.gamePage);
                        experience.language=selector.GetComponentInChildren<Dropdown>(true);experience.languageLabel=selector.transform.Find("LanguageLabel").GetComponent<Text>();EditorUtility.SetDirty(experience);
                    }
                    labels+=BindLabels(ui.gameObject,lookup,aliases);unbound.AddRange(UnboundCaptions(ui.gameObject,lookup).Select(path=>scenePath+"/"+path));EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);scenes++;
                }
                AssetDatabase.SaveAssets();ValidateCatalogs();Directory.CreateDirectory("Docs/Localization");
                File.WriteAllText("Docs/Localization/authoring_receipt.json",JsonUtility.ToJson(new Receipt{utc=DateTime.UtcNow.ToString("O"),scenes=scenes,changedLabelBindings=labels,languages=languages.Select(l=>l.code).ToArray(),unboundAuthoredCaptionCandidates=unbound.Distinct().OrderBy(path=>path,StringComparer.Ordinal).ToArray()},true));
            }
            finally{if(!UnityEngine.Application.isBatchMode)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        [MenuItem("Weedtopia/Localization/Validate Catalogs")]
        public static void ValidateCatalogs()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<LanguageCatalog>(CatalogPath);
            if(catalog==null)throw new InvalidOperationException("Missing localization catalog");
            var service=catalog.Create();
            var baseline=JsonUtility.FromJson<LocaleDocument>(catalog.languages.Single(l=>l.code==catalog.fallback).catalog.text);
            var keys=baseline.entries.Select(e=>e.key).OrderBy(k=>k,StringComparer.Ordinal).ToArray();
            var arguments=Enumerable.Repeat((object)0,64).ToArray();
            var original=baseline.entries.ToDictionary(e=>e.key,e=>e.value);
            foreach(var language in catalog.languages)
            {
                var document=JsonUtility.FromJson<LocaleDocument>(language.catalog.text);
                if(!keys.SequenceEqual(document.entries.Select(e=>e.key).OrderBy(k=>k,StringComparer.Ordinal)))throw new InvalidOperationException("Catalog key coverage differs: "+language.code);
                foreach(var entry in document.entries)
                {
                    string.Format(System.Globalization.CultureInfo.GetCultureInfo(language.culture),entry.value,arguments);
                    if(entry.value.Count(c=>c=='\n')!=original[entry.key].Count(c=>c=='\n'))throw new InvalidOperationException("Line break mismatch: "+language.code+"/"+entry.key);
                }
            }
            Directory.CreateDirectory("Docs/Localization");
            File.WriteAllText("Docs/Localization/unity_catalog_validation.json",JsonUtility.ToJson(new CatalogReceipt{status="passed",languages=catalog.languages.Length,keysPerLanguage=keys.Length},true));
        }
        private static T Bound<T>(GameUi ui,string key)where T:Component
        {
            var bindings=new SerializedObject(ui).FindProperty("bindings");
            for(int i=0;i<bindings.arraySize;i++)
            {var entry=bindings.GetArrayElementAtIndex(i);if(entry.FindPropertyRelative("id").stringValue==key)return ((GameObject)entry.FindPropertyRelative("target").objectReferenceValue).GetComponent<T>();}
            throw new InvalidOperationException("Missing UI binding "+key);
        }
        private static IEnumerable<string> CaptionPrefabPaths()
        {
            var paths=new HashSet<string>(AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Weedtopia/Game/Prefabs","Assets/Weedtopia/Production"}).Select(AssetDatabase.GUIDToAssetPath),StringComparer.Ordinal);
            var ordered=new List<string>();var visited=new HashSet<string>(StringComparer.Ordinal);var active=new HashSet<string>(StringComparer.Ordinal);
            void Visit(string path)
            {
                if(visited.Contains(path))return;
                if(!active.Add(path))throw new InvalidOperationException("Cyclic prefab caption dependency: "+path);
                foreach(var dependency in AssetDatabase.GetDependencies(path,false).Where(value=>value!=path&&paths.Contains(value)).OrderBy(value=>value,StringComparer.Ordinal))Visit(dependency);
                active.Remove(path);visited.Add(path);ordered.Add(path);
            }
            // Nested reusable sources are repaired before every parent and scene that inherits them.
            foreach(var path in paths.OrderBy(value=>value,StringComparer.Ordinal))Visit(path);
            return ordered;
        }
        private static int BindLabels(GameObject parent,LocalizationService lookup,Dictionary<string,string> aliases)
        {
            int added=BindInventoryCaptions(parent,lookup);var dynamic=PresenterLabels(parent);
            foreach(var text in parent.GetComponentsInChildren<Text>(true))
            {
                // Player input and dropdown choices are owned by the controls, never by static bindings.
                var input=text.GetComponentInParent<InputField>(true);
                var existing=text.GetComponents<LocalizedLabel>();
                if(dynamic.Contains(text)||(input!=null&&input.textComponent==text)||text.GetComponentInParent<Dropdown>(true)!=null)
                {foreach(var obsolete in existing){Object.DestroyImmediate(obsolete);added++;}continue;}
                foreach(var duplicate in existing.Skip(1)){Object.DestroyImmediate(duplicate);added++;}
                if(existing.Length>0)continue;
                LocalizedLabel binding;
                string key=lookup.KeyForSource(text.text);
                if(key==null)aliases.TryGetValue(text.text,out key);
                if(key==null)continue;
                binding=text.gameObject.AddComponent<LocalizedLabel>();binding.EditorConfigure(key);EditorUtility.SetDirty(binding);added++;
            }
            return added;
        }
        // Short inventory filters are distinct from the full category descriptions used by item cards.
        // Bind by presenter contract instead of guessing a key from a possibly outdated German caption.
        private static int BindInventoryCaptions(GameObject parent,LocalizationService lookup)
        {
            int changed=0;
            foreach(var view in parent.GetComponentsInChildren<RewardCollectionUi>(true))
            {
                changed+=BindCaption(view.inventoryTab,"reward.tab.inventory",lookup);
                changed+=BindCaption(view.achievementsTab,"reward.tab.achievements",lookup);
                changed+=BindCaption(view.historyTab,"reward.tab.history",lookup);
                var keys=new[]{"inventory.all","inventory.filter.team","inventory.filter.build_time","inventory.category.Supplies"};
                for(int i=0;i<Math.Min(keys.Length,view.categoryButtons.Length);i++)
                    changed+=BindCaption(view.categoryButtons[i],keys[i],lookup);
            }
            foreach(var layout in parent.GetComponentsInChildren<InventoryPanelLayout>(true))
                changed+=BindCaption(layout.back,"inventory.back",lookup);
            return changed;
        }
        private static IEnumerable<string> UnboundCaptions(GameObject parent,LocalizationService lookup)
        {
            var dynamic=PresenterLabels(parent);
            foreach(var text in parent.GetComponentsInChildren<Text>(true))
            {
                var input=text.GetComponentInParent<InputField>(true);
                if(dynamic.Contains(text)||(input!=null&&input.textComponent==text)||text.GetComponentInParent<Dropdown>(true)!=null)continue;
                var binding=text.GetComponent<LocalizedLabel>();
                if(binding!=null)
                {
                    if(string.IsNullOrEmpty(binding.Key)||lookup.Get(binding.Key)=="["+binding.Key+"]")
                        throw new InvalidOperationException("Unknown authored localization key: "+binding.Key+" on "+text.name);
                    continue;
                }
                if(!Regex.IsMatch(text.text??"",@"\p{L}{2}"))continue;
                var path=text.transform;string name=path.name;
                while(path!=parent.transform&&path.parent!=null){path=path.parent;name=path.name+"/"+name;}
                yield return name+" = "+text.text.Replace("\n"," / ");
            }
        }
        private static int BindCaption(Button button,string key,LocalizationService lookup)
        {
            if(button==null)return 0;
            var text=button.GetComponentInChildren<Text>(true);if(text==null)return 0;
            return BindCaption(text,key,lookup);
        }
        internal static int BindCaption(Text text,string key,LocalizationService lookup)
        {
            if(text==null)return 0;
            int changed=0;var binding=text.GetComponent<LocalizedLabel>();
            if(binding==null){binding=text.gameObject.AddComponent<LocalizedLabel>();changed++;}
            if(binding.Key!=key){binding.EditorConfigure(key);EditorUtility.SetDirty(binding);PrefabUtility.RecordPrefabInstancePropertyModifications(binding);changed++;}
            string caption=lookup.Get(key);
            if(text.text!=caption){text.text=caption;EditorUtility.SetDirty(text);PrefabUtility.RecordPrefabInstancePropertyModifications(text);changed++;}
            return changed;
        }
        // Presenter-owned values must never receive OnEnable writes from a static caption binding.
        private static HashSet<Text> PresenterLabels(GameObject parent)
        {
            var result=new HashSet<Text>();
            var owners=new HashSet<Type>{typeof(BuildingInteractionUi),typeof(CityCampaignUi),typeof(CityStatusBar),typeof(DialoguePanel),typeof(DistrictNavigator),typeof(InteractionRow),typeof(LuckyWheelPrizeView),typeof(InventoryDetailView),typeof(InventoryTileView),typeof(LuckyWheelUi),typeof(ProductionPresentation),typeof(QuestTracker),typeof(RewardCollectionUi),typeof(RewardPreviewView),typeof(RewardPresentation),typeof(RewardCardView),typeof(ShopProductCard),typeof(ShopUi),typeof(UiExperience),typeof(VipStoriesUi),typeof(VipStoryRosterRow)};
            var captions=new HashSet<string>{"UiExperience.logo","UiExperience.subtitle","UiExperience.playHint","UiExperience.restartBody","VipStoriesUi.heading","VipStoriesUi.subtitle","LuckyWheelUi.costHeading","LuckyWheelUi.costBody","RewardPreviewView.heading","RewardPresentation.continueText","RewardPresentation.collectionText"};
            foreach(var presenter in parent.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c!=null&&owners.Contains(c.GetType())))
                foreach(var field in presenter.GetType().GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance))
                {
                    if(captions.Contains(presenter.GetType().Name+"."+field.Name))continue;
                    if(field.GetValue(presenter) is Text text)result.Add(text);
                    else if(field.GetValue(presenter) is Text[] labels)foreach(var label in labels.Where(l=>l!=null))result.Add(label);
                }
            foreach(var frame in parent.GetComponentsInChildren<ModalFrame>(true))if(frame.headingSource!=null)result.Add(frame.heading);
            foreach(var ui in parent.GetComponentsInChildren<GameUi>(true))
            {
                var bindings=new SerializedObject(ui).FindProperty("bindings");
                var dynamicIds=new HashSet<string>{"hud","questTitle","questBody","detailTitle","detailBody","flow","workerText","stats","scores","message","introText","offlineText","premiumText","conversationTitle","conversationText","assignmentTarget","teamSpinHint"};
                for(int i=0;i<bindings.arraySize;i++)
                {
                    var entry=bindings.GetArrayElementAtIndex(i);string id=entry.FindPropertyRelative("id").stringValue;
                    var target=entry.FindPropertyRelative("target").objectReferenceValue as GameObject;
                    if(target==null||!dynamicIds.Contains(id))continue;
                    var text=target.GetComponent<Text>();if(text!=null)result.Add(text);
                }
            }
            foreach(var ui in parent.GetComponentsInChildren<BuildingInteractionUi>(true))
                foreach(var button in new[]{ui.train,ui.rank,ui.work,ui.talk})if(button!=null)result.Add(button.GetComponentInChildren<Text>(true));
            foreach(var panel in parent.GetComponentsInChildren<DialoguePanel>(true))
                foreach(var button in new[]{panel.next,panel.skip})if(button!=null)result.Add(button.GetComponentInChildren<Text>(true));
            foreach(var panel in parent.GetComponentsInChildren<ShopUi>(true))
                if(panel.confirmPurchase!=null)result.Add(panel.confirmPurchase.GetComponentInChildren<Text>(true));
            foreach(var card in parent.GetComponentsInChildren<ShopProductCard>(true))
                if(card.purchase!=null)result.Add(card.purchase.GetComponentInChildren<Text>(true));
            foreach(var panel in parent.GetComponentsInChildren<QuestTracker>(true))
                if(panel.previewAction!=null)result.Add(panel.previewAction.GetComponentInChildren<Text>(true));
            foreach(var panel in parent.GetComponentsInChildren<CityCampaignUi>(true))
                foreach(var button in new[]{panel.burningTravel,panel.rockTravel})if(button!=null)result.Add(button.GetComponentInChildren<Text>(true));
            return result;
        }
        [Serializable]private sealed class CatalogReceipt{public string status;public int languages,keysPerLanguage;}
        [Serializable]private sealed class Receipt{public string utc;public int scenes,changedLabelBindings;public string[] languages,unboundAuthoredCaptionCandidates;public bool fixedUiAuthored=true;}
    }
}
