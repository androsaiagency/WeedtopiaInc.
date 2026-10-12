# Android building texture optimization

The 5 October 2026 baseline inspects the newest existing APK,
`Builds/Android/20261004-191541-languages-fonts-test/WeedtopiaInc-Android-Test.apk`.
Every one of the 251 production building resources is embedded as **RGBA32**,
with **2,105,908,052 image bytes**. The ZIP entries use 1,139,642,764 bytes out of
the APK's 1,382,680,800 bytes. This is actual serialized Texture2D evidence,
not an estimate from import settings or a measurement of resident RAM.

Source canvases are 1254×1254 (244 files) and 1270×1270 (7 files), with mipmaps.
The Android override was previously disabled. The existing Standalone BC7
override and the source pixel contract are preserved.

## Explicit authoring

In the existing editor, select Android and stop Play Mode/save manual scenes.
Use `Weedtopia > Build > Inspect Android Building Textures`, then
`Weedtopia > Build > Optimize Android Building Texture Imports`.
The existing file-backed `LayoutEditorJob` allows commands
`android-building-textures-inspect` and `android-building-textures`.
Creating the code does not automatically invoke either operation.

The verified policy selects ETC2 RGBA8, high compressor quality, an Android-only
1024 maximum, full alpha and no Crunch. The project uses Vulkan/GLES3.
ETC2 is the compatible choice for that graphics profile. The explicit test APK
build now supplies the ETC2 subtarget rather than leaving the BuildPlayerOptions
default implicit, and restores the user's previous setting afterward.

The authoring method backs up original import metas, skips matching imports and
compares source hashes/GUIDs, all 251 wrapper bytes, the catalog, both city scenes,
sprite world bounds and world pivot. Raw pixel pivot and imported pixels per
unit are recorded separately: Unity adjusts them proportionally while retaining
the sprite's world geometry. It requires a source/GUID/bundle-SHA-bound native
Android probe and checks that every import keeps the requested dimensions and
policy. A failed preservation check rolls back; a desired meta format alone
cannot produce a passing receipt.

The full-size 1254×1254 ETC2 and ASTC probes produced RGBA32. The 1024 ETC2 probe
instead proves **TextureFormat 47, ETC2_RGBA8, 1024×1024, 11 mip levels and
1,398,128 GPU image bytes**, with source pixels, GUID, original meta and world
geometry preserved. Its previous full-size image used 8,384,072 bytes. Global
development overrides were already `NoOverride`/`0`; they were not the cause of
this baseline failure.

This explicitly trades approximately 18–19% of linear Android resolution for
working GPU compression and substantially lower memory/build size. Original
PNG canvases and the Standalone 2048/BC7 import remain intact. The Android import
resamples its platform copy; it does not edit the source artwork. Fine details
and close zoom still require visual acceptance. The full APK audit separately
proves the format and reduction for all 251 resources.

## Isolated serialized probes and development overrides

The explicit commands `android-building-textures-probe`,
`android-building-textures-probe-astc4`, and
`android-building-textures-probe-1024` build one original resource as an isolated
Android AssetBundle. Each probe restores its original meta and its original
global import overrides in `finally`. The reports expose sprite bounds,
pixel pivot, pixels per unit and their derived world pivot. A smaller import is
accepted for a probe only when the world geometry remains invariant; changes to
raw pixel metrics are recorded separately and never hidden.

Unity's global `EditorUserBuildSettings.overrideTextureCompression` takes
precedence over every per-texture setting and persists in
`Library/EditorUserBuildSettings.asset`. Therefore both probes and test APK
builds temporarily use `NoOverride` and no global size limit (`0`), then restore
the previous values. Before/effective/restored contexts are recorded. This
prevents a development `ForceUncompressed` setting from silently defeating a
compressed build while preserving the user's editor preference.

Use the same tool to inspect the actual serialized probe:

```powershell
& C:/Python312/python.exe Tools/Implementation/audit_android_building_textures.py `
  --probe Docs/CommercialReadiness/AndroidTextures/probe-astc4.json
```

The result is `probe-astc4-serialized-verification.json`. Desired settings and
Editor preview formats remain separate from native Android serialization.
ASTC is supported by most modern Android GPUs, but older unsupported GPUs
decompress to RGBA32 at runtime, increasing memory and CPU work. An ASTC policy
requires the serialized proof and a suitable device profile.

## Package verification

Run the measurement against the new APK with the baseline:

```powershell
& C:/Python312/python.exe Tools/Implementation/audit_android_building_textures.py `
  Builds/Android/<new-id>/WeedtopiaInc-Android-Test.apk `
  --baseline Docs/CommercialReadiness/AndroidTextures/apk-before.json
```

The tool uses the stripped serialized-file version 22 schema from Unity
6000.3. It reads bounded metadata/object prefixes and ZIP central-directory
sizes, without decoding images or installing dependencies. It fails if the
schema, target or expected single texture identity differs. The report proves
embedded formats, bytes and unchanged source hashes/GUIDs; it does not establish
visual quality or physical device performance.

## Acceptance still required

Import: fresh `optimization.json` must say `applied`, with source/reference/world
geometry preservation and `serializedAndroidProbeVerified` true. Raw pixel
metrics may change proportionally and are reported separately. Package: a fresh APK audit must
show 251 ETC2_RGBA8 resources and measured reductions. Visual: compare cutout
edges, thin details and signage at normal and close zoom. Device: compare cold
start, resume and high-tier city rendering on a Vulkan/GLES3 phone. Human visual
acceptance and device performance remain separate from automatic checks.

Official references:
[Unity texture-format support](https://docs.unity.com/en-us/engine/6000.7/manual/materials-and-shaders/textures/textures-getting-started/texture-compression-formats/texture-formats-reference),
[BuildPlayerOptions.subtarget](https://docs.unity.com/en-us/engine/6000.7/script-reference/unityeditor/buildplayeroptions/subtarget),
[EditorUserBuildSettings.androidBuildSubtarget](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityeditor/editoruserbuildsettings/androidbuildsubtarget).
Additional checked Unity 6000.3 references:
[persistent texture import overrides](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/EditorUserBuildSettings-overrideTextureCompression.html),
[NPOT texture compression restrictions](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/EditorUtility.CompressTexture.html),
[Android GPU support and runtime fallback](https://docs.unity3d.com/6000.3/Documentation/Manual/texture-choose-format-by-platform.html).
