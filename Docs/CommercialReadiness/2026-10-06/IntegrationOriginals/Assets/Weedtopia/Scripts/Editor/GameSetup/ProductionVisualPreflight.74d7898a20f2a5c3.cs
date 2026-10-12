using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Buildings.Presentation;
using Weedtopia.Unity;
using Weedtopia.Editor.BuildingArtPipeline;

namespace Weedtopia.Editor.GameSetup
{
    [Serializable] public sealed class VisualIssue {public string code,scene,objectPath,asset,detail;}
    [Serializable] public sealed class VisualPreflightReport
    {
        public string utc,unity,fingerprint,buildContext;
        public int fingerprintSchemaVersion;
        public bool productionReady,userApproved;
        public int catalogSlots;
        public VisualIssue[] issues;
    }
    public static class ProductionVisualPreflight
    {
        public const string ReportPath="Docs/ProductionFinish/Evidence/production_preflight.json";
        [Serializable] private sealed class GateReceipt {public bool passed = default;public string fingerprint = default;}
        public const string InputManifestPath = "Docs/ProductionFinish/Evidence/release_inputs.json";
        public static string[] ScenesForBuild()
        {
            var profile = BuildProfile.GetActiveBuildProfile();
            var scenes = profile != null ? profile.GetScenesForBuild() : EditorBuildSettings.scenes;
            return scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        }

        public static IEnumerable<string> FingerprintInputPaths()
        {
            var scenes = ScenesForBuild();
            var dependencies = scenes.SelectMany(path => AssetDatabase.GetDependencies(path, true));
            // Resources, StreamingAssets and build profiles can enter a player without a scene reference.
            var runtimeAssets = AssetDatabase.GetAllAssetPaths().Where(path => path.StartsWith("Assets/", StringComparison.Ordinal)
                && (path.Contains("/Resources/") || path.StartsWith("Assets/StreamingAssets/", StringComparison.Ordinal)));
            var profile = BuildProfile.GetActiveBuildProfile();
            var profileInputs = profile == null ? Enumerable.Empty<string>()
                : AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(profile), true);
            IEnumerable<string> Files(string folder) => Directory.Exists(folder)
                ? Directory.GetFiles(folder, "*", SearchOption.AllDirectories) : Array.Empty<string>();
            return Files("Assets/Weedtopia/Scripts").Concat(Files("Assets/Weedtopia/Production"))
                .Concat(Files("Assets/Settings")).Concat(Files("ProjectSettings"))
                .Concat(EmbeddedPackageInputs("Packages"))
                .Concat(dependencies.Concat(runtimeAssets).Concat(profileInputs).Where(path => path.StartsWith("Assets/", StringComparison.Ordinal))
                    .SelectMany(path => new[] { path, path + ".meta" }))
                .Concat(new[] { "ProjectSettings/ProjectSettings.asset", "ProjectSettings/ProjectVersion.txt",
                    "ProjectSettings/EditorBuildSettings.asset", "Packages/manifest.json", "Packages/packages-lock.json" })
                .Select(path => path.Replace('\\', '/')).Where(path => !AssetDatabase.IsValidFolder(path));
        }

        // Embedded package sources and metadata must invalidate release receipts just like productive assets.
        public static IEnumerable<string> EmbeddedPackageInputs(string packagesFolder)
            => Directory.Exists(packagesFolder) ? Directory.GetDirectories(packagesFolder)
                .Where(folder => File.Exists(Path.Combine(folder, "package.json")))
                .SelectMany(folder => Directory.GetFiles(folder, "*", SearchOption.AllDirectories)) : Array.Empty<string>();

        public static string BuildContext(BuildTarget target, BuildOptions options)
        {
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(target));
            var profile = BuildProfile.GetActiveBuildProfile();
            // Persisted settings are hashed separately; live platform switches and profile overrides are explicit.
            var entries = new List<string>
            {
                "unity=" + UnityEngine.Application.unityVersion,
                "target=" + target,
                "options=" + (int)options,
                "textureCompressionOverride=" + EditorUserBuildSettings.overrideTextureCompression,
                "textureCompressionOverrideValue=" + (int)EditorUserBuildSettings.overrideTextureCompression,
                "maximumTextureSizeOverride=" + EditorUserBuildSettings.overrideMaxTextureSize,
                "profile=" + (profile == null ? "platform" : AssetDatabase.GetAssetPath(profile)),
                "identifier=" + PlayerSettings.GetApplicationIdentifier(namedTarget),
                "version=" + PlayerSettings.bundleVersion,
                "backend=" + PlayerSettings.GetScriptingBackend(namedTarget),
                "stripping=" + PlayerSettings.GetManagedStrippingLevel(namedTarget),
                "defines=" + PlayerSettings.GetScriptingDefineSymbols(namedTarget),
                "profileDefines=" + (profile == null ? "" : string.Join(";", profile.scriptingDefines)),
                "graphics=" + string.Join(",", PlayerSettings.GetGraphicsAPIs(target).Select(api => api.ToString())),
                "scenes=" + string.Join(";", ScenesForBuild())
            };
            if (target == BuildTarget.Android)
            {
                entries.Add("androidSymbolLevel=" + UnityEditor.Android.UserBuildSettings.DebugSymbols.level);
                entries.Add("androidSymbolFormat=" + UnityEditor.Android.UserBuildSettings.DebugSymbols.format);
                entries.Add("androidBundle=" + EditorUserBuildSettings.buildAppBundle);
                entries.Add("androidExport=" + EditorUserBuildSettings.exportAsGoogleAndroidProject);
                entries.Add("androidTextures=" + EditorUserBuildSettings.androidBuildSubtarget);
                entries.Add("androidArchitectures=" + PlayerSettings.Android.targetArchitectures);
                entries.Add("androidVersionCode=" + PlayerSettings.Android.bundleVersionCode);
                entries.Add("androidMinSdk=" + (int)PlayerSettings.Android.minSdkVersion);
                entries.Add("androidTargetSdk=" + (int)PlayerSettings.Android.targetSdkVersion);
                entries.Add("androidSplit=" + PlayerSettings.Android.splitApplicationBinary);
                entries.Add("androidPerCpu=" + PlayerSettings.Android.buildApkPerCpuArchitecture);
                entries.Add("androidCustomKeystore=" + PlayerSettings.Android.useCustomKeystore);
            }
            else if (BuildPipeline.GetBuildTargetGroup(target) == BuildTargetGroup.Standalone)
                entries.Add("standaloneSubtarget=" + EditorUserBuildSettings.standaloneBuildSubtarget);
            return string.Join("\n", entries);
        }

        public static ReleaseInputManifest CaptureInputs(BuildTarget target, BuildOptions options)
            => ReleaseFingerprint.Capture(Directory.GetCurrentDirectory(), FingerprintInputPaths(), BuildContext(target, options));
        public static string Fingerprint() => Fingerprint(EditorUserBuildSettings.activeBuildTarget, BuildOptions.None);
        public static string Fingerprint(BuildTarget target, BuildOptions options) => CaptureInputs(target, options).fingerprint;
        public static bool AcceptsReceipt(string json,string fingerprint)
        {try{var r=JsonUtility.FromJson<GateReceipt>(json);return r!=null&&r.passed&&!string.IsNullOrEmpty(r.fingerprint)&&r.fingerprint==fingerprint;}catch(ArgumentException){return false;}}
        private static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
        public static bool ForbiddenAsset(string path)
        {
            string name=Path.GetFileName(path).ToLowerInvariant();return name.StartsWith("development_")||name=="developmentvisuals.asset"||path.Contains("/VisualReview/")||path.Contains("/Prototypes2D/")||path.Contains("/Production/Review/");
        }
        [MenuItem("Weedtopia/Production/Production Visual Preflight")]
        public static void RunMenu(){var report=Inspect();if(!report.productionReady)throw new InvalidOperationException("Production Visual Preflight FAILED: "+report.issues.Length+" issues. See "+ReportPath);}
        public static VisualPreflightReport Inspect()
            => Inspect(EditorUserBuildSettings.activeBuildTarget, BuildOptions.None);
        public static VisualPreflightReport Inspect(BuildTarget target, BuildOptions options)
        {
            // Opening build scenes must never discard the user's unsaved scene work.
            if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
                .Select(UnityEngine.SceneManagement.SceneManager.GetSceneAt).Any(scene => scene.isDirty))
                throw new InvalidOperationException("Save the open scenes before running production preflight. Unsaved scene work is protected.");
            var activeProfile = BuildProfile.GetActiveBuildProfile();
            if (activeProfile != null && EditorUtility.IsDirty(activeProfile))
                throw new InvalidOperationException("Save the active build profile before running production preflight.");
            var inputs = CaptureInputs(target, options);
            var issues=new List<VisualIssue>();var setup=EditorSceneManager.GetSceneManagerSetup();bool approved=true;int presentations=0,slots=0;
            void Add(string code,string scene,string obj,string asset,string detail)=>issues.Add(new VisualIssue{code=code,scene=scene,objectPath=obj,asset=asset,detail=detail});
            foreach (var missing in inputs.files.Where(file => file.bytes < 0))
                Add("MISSING_RELEASE_INPUT", "", "", missing.path, "Required release input or import metadata is missing");
            try
            {
                foreach(var build in ScenesForBuild().Select(path => new EditorBuildSettingsScene(path, true)))
                {
                    if(build.path.Contains("Sample")||build.path.Contains("Test")||build.path.Contains("Review"))Add("NON_PRODUCT_SCENE",build.path,"",build.path,"Sample/test/review scene in product build");
                    foreach(string dependency in AssetDatabase.GetDependencies(build.path,true).Distinct())if(ForbiddenAsset(dependency))Add("FORBIDDEN_DEPENDENCY",build.path,"",dependency,"Development or review asset reachable from built scene");
                    var scene=EditorSceneManager.OpenScene(build.path);var objects=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();
                    foreach(var tr in objects)
                    {
                        string path=PathOf(tr);
                        if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject)>0)Add("MISSING_SCRIPT",build.path,path,"","Missing script");
                        // Inactive modal panels, dropdown templates and delivery pools remain reachable at runtime.
                        var uiText=tr.GetComponent<Text>();
                        if(uiText!=null&&(uiText.font==null||uiText.font.name.Contains("LegacyRuntime")))Add("DEFAULT_FONT",build.path,path,AssetDatabase.GetAssetPath(uiText.font),"Production font required in every reachable panel");
                        var uiImage=tr.GetComponent<Image>();
                        if(uiImage!=null&&uiImage.sprite==null&&uiImage.name!="ModalShade")Add("MISSING_UI_SPRITE",build.path,path,"","Reachable UI graphic has no authored sprite");
                        if(!tr.gameObject.activeInHierarchy)continue;
                        foreach(var renderer in tr.GetComponents<Renderer>())foreach(var material in renderer.sharedMaterials)
                            if(material==null||material.shader==null||material.shader.name.Contains("InternalError"))Add("MISSING_MATERIAL",build.path,path,AssetDatabase.GetAssetPath(material),"Missing/invalid material or shader");
                        var mesh=tr.GetComponent<MeshFilter>();if(mesh!=null&&mesh.sharedMesh!=null&&AssetDatabase.GetAssetPath(mesh.sharedMesh)=="Library/unity default resources")Add("BUILTIN_PRIMITIVE",build.path,path,"",mesh.sharedMesh.name);
                        var sprite=tr.GetComponent<SpriteRenderer>();if(sprite!=null&&sprite.sprite==null)Add("EMPTY_SPRITE",build.path,path,"","SpriteRenderer without sprite");
                        var text=tr.GetComponent<Text>();if(text!=null)
                        {
                            if(text.text.Contains("Entwicklungsvisual")||text.text.Contains("TODO")||text.text.Contains("Debug"))Add("DEBUG_TEXT",build.path,path,"",text.text);
                        }
                    }
                    var root=objects.Select(x=>x.GetComponent<GameRoot>()).FirstOrDefault(x=>x!=null);
                    if(root!=null)
                    {
                        var rootFields=new SerializedObject(root);
                        foreach(string field in new[]{"content","productionCatalog","ui","cameraRig","feedback","presentation"})if(rootFields.FindProperty(field).objectReferenceValue==null)Add("MISSING_ROOT_REFERENCE",build.path,"GameRoot/"+field,"","Required serialized reference missing");
                        var p=root.Presentation;
                        if(p==null){approved=false;Add("MISSING_PRESENTATION",build.path,"GameRoot","","Authored production presentation missing");}
                        else
                        {
                            presentations++;
                            if(!p.explicitBuildingApproval||!ProductionBuildingIntegration.HasApproval()){approved=false;Add("ART_APPROVAL_PENDING",build.path,"GameRoot", ProductionBuildingIntegration.ApprovalPath,"Explicit approval of town hall and coffee T01/T12/T24 required");}
                            if(p.art==null||p.art.sprites==null)Add("MISSING_ART_SET",build.path,"GameRoot","","No presentation art");
                            else foreach(var entry in p.art.sprites)if(entry.sprite==null)Add("MISSING_ICON_PORTRAIT",build.path,"GameRoot/PresentationArt",entry.id,"Missing required sprite");
                            foreach(var required in new[]{p.introPortrait,p.questPortrait,p.workerPortrait,p.workerRoleIcon})if(required==null||required.sprite==null)Add("MISSING_PORTRAIT",build.path,required==null?"GameRoot":PathOf(required.transform),"","Reachable portrait/icon missing");
                            if(p.ambience==null||p.ambience.clip==null)Add("MISSING_MUSIC",build.path,"GameRoot/AfterglowMusic","","No integrated soundtrack");
                            foreach(var actor in p.characters)if(actor==null||actor.spriteRenderer==null||actor.spriteRenderer.sprite==null||actor.GetComponent<Collider>()==null)Add("CHARACTER_REFERENCE",build.path,actor==null?"GameRoot/characters":PathOf(actor.transform),"","Character art and interaction collider required");
                            foreach(var delivery in objects.Select(x=>x.GetComponent<DeliveryView>()).Where(x=>x!=null))
                            {
                                var fields=new SerializedObject(delivery);
                                foreach(string field in new[]{"body","artwork","leftSprite","rightSprite","cargo","directionalAnimation"})if(fields.FindProperty(field).objectReferenceValue==null)Add("DELIVERY_REFERENCE",build.path,PathOf(delivery.transform),field,"Authored vehicle pool requires directional artwork");
                            }
                            foreach(var motion in objects.Select(x=>x.GetComponent<DirectionalSpriteAnimator>()).Where(x=>x!=null))
                                if(motion.artwork==null||motion.animationSet==null||motion.animationSet.views.Length!=8||motion.animationSet.views.Any(v=>v==null||v.frames==null||v.frames.Length!=6||v.frames.Any(f=>f==null)))
                                    Add("DIRECTIONAL_ANIMATION_REFERENCE",build.path,PathOf(motion.transform),"","Eight complete directions and six animation frames are required");
                            foreach(var life in objects.Select(x=>x.GetComponent<LivingTownPresentation>()).Where(x=>x!=null))
                                foreach(var actor in life.actors)if(actor.animation==null)Add("DIRECTIONAL_ANIMATION_REFERENCE",build.path,PathOf(actor.anchor),"","Moving production actors require directional animation");
                        }
                        if(root.Visuals is GameVisualSet)Add("DEVELOPMENT_PROVIDER",build.path,"GameRoot/developmentVisuals",GameScaffold.VisualsPath,"Active provider returns development buildings; release blocked");
                    }
                }
                if(presentations==0){approved=false;Add("MISSING_GAME_SCENE","","",GameScaffold.GameScene,"No authored game presentation in build scenes");}
                var catalog=AssetDatabase.LoadAssetAtPath<BuildingVisualCatalog>(BuildingManifestReader.CatalogPath);
                if(catalog==null)throw new InvalidOperationException("Missing required building catalog: "+BuildingManifestReader.CatalogPath);
                var used=new HashSet<string>();
                foreach(var family in catalog.Families)foreach(var band in family.Bands)
                {
                    slots++;var wrapper=band.WrapperPrefab.GetComponent<BuildingVisualWrapper>();string key=family.BuildingId+"/"+wrapper.TierId;
                    var visuals=wrapper.VisualRoot.GetComponentsInChildren<BuildingSpriteVisual>();
                    var sprites=visuals.Select(x=>string.IsNullOrEmpty(x.ResourcePath)?x.GetComponent<SpriteRenderer>().sprite:AssetDatabase.LoadAssetAtPath<Sprite>(ProductionBuildingIntegration.SpritePath(x.ResourcePath))).ToArray();
                    var model=wrapper.ManagedModel;
                    var lod=model!=null&&model.gameObject.activeSelf?model.GetComponent<LODGroup>():null;
                    bool meshVisual=lod!=null&&lod.GetLODs().Length>0&&lod.GetLODs().All(l=>l.renderers.Length>0&&l.renderers.All(r=>r!=null&&r.GetComponent<MeshFilter>()?.sharedMesh!=null&&r.sharedMaterials.Length>0&&r.sharedMaterials.All(m=>m!=null&&m.shader!=null)));
                    if(sprites.Any(x=>x==null)||(sprites.Length==0&&!meshVisual))Add("BUILDING_VISUAL_MISSING",GameScaffold.GameScene,key,AssetDatabase.GetAssetPath(band.WrapperPrefab),"Active accepted sprite or complete authored LOD model required under VisualRoot");
                    foreach(var sprite in sprites.Where(x=>x!=null))
                    {
                        string id=GlobalObjectId.GetGlobalObjectIdSlow(sprite).ToString();if(!used.Add(id))Add("DUPLICATE_BUILDING_SPRITE",GameScaffold.GameScene,key,AssetDatabase.GetAssetPath(sprite),"Sprite reused across building slots");
                        if(sprite.pivot.y>1)Add("GROUND_PIVOT",GameScaffold.GameScene,key,AssetDatabase.GetAssetPath(sprite),"Ground pivot must be measured and approved");
                    }
                }
                if(slots!=251)Add("CATALOG_COVERAGE",GameScaffold.GameScene,"BuildingVisualCatalog","","Expected exactly 251 slots, got "+slots);
                string fingerprint=inputs.fingerprint;
                foreach(string gate in new[]{"ui_overflow","target_profile","human_playthrough","audio_listening","audio_license"})
                {
                    string path="Docs/ProductionFinish/Evidence/"+gate+".json";
                    // Absence never counts as a pass; receipts have explicit, independent acceptance fields.
                    if(!File.Exists(path)||!AcceptsReceipt(File.ReadAllText(path),fingerprint))Add("ACCEPTANCE_GATE_OPEN",GameScaffold.GameScene,gate,path,"Current fingerprint-bound accepted receipt required");
                }
            }
            finally{if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);}
            var report=new VisualPreflightReport{utc=DateTime.UtcNow.ToString("O"),unity=UnityEngine.Application.unityVersion,fingerprint=inputs.fingerprint,buildContext=inputs.buildContext,fingerprintSchemaVersion=inputs.schemaVersion,productionReady=issues.Count==0,userApproved=approved,catalogSlots=slots,issues=issues.ToArray()};
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));File.WriteAllText(InputManifestPath,JsonUtility.ToJson(inputs,true));File.WriteAllText(ReportPath,JsonUtility.ToJson(report,true));return report;
        }
    }
    public sealed class ProductionBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder=>-1000;
        public void OnPreprocessBuild(BuildReport report)
        {
            if((report.summary.options&BuildOptions.Development)!=0)return;
            // Explicit non-Development diagnostic artifacts carry no shipping acceptance. The exact active APK path is scoped by try/finally.
            if(AndroidTestBuild.IsReleaseDiagnosticBuild(report))return;
            if(report.summary.platform==BuildTarget.Android && (UnityEditor.Android.UserBuildSettings.DebugSymbols.level!=global::Unity.Android.Types.DebugSymbolLevel.Full
                || UnityEditor.Android.UserBuildSettings.DebugSymbols.format!=global::Unity.Android.Types.DebugSymbolFormat.Zip))
                throw new BuildFailedException("Production Android builds require Full native debug symbols in ZIP format. Use Weedtopia/Build/Production Android Release.");
            var result=ProductionVisualPreflight.Inspect(report.summary.platform,report.summary.options);if(!result.productionReady)throw new BuildFailedException("Production visual preflight failed: "+result.issues.Length+" blockers. "+ProductionVisualPreflight.ReportPath);
        }
    }
}
