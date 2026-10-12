# Android release artifact audit

Run this read-only audit after a Unity Android build has finished successfully. It requires Python 3.11 or newer and the Java/Android SDK already installed with Unity; it installs no dependencies, opens no editor, builds nothing, changes no APK, uploads nothing and reads no personal save. All reports go to an explicit folder outside Unity input and cache directories.

From the project root:

```powershell
python Tools/CommercialReadiness/audit_android_release.py "Builds/Android/<fresh-run>/WeedtopiaInc-Android-ReleaseDiagnostic.apk" --output "Docs/CommercialReadiness/<date>/ReleaseApkAudit/<fresh-run>" --textures
```

Replace the bracketed parts with the actual completed build and desired report folder. The default receipts are that APK folder's `build-result.json` and `NativeSymbols/symbols.json`. `--build-result` and `--symbol-manifest` accept explicit paths. `--android-player` selects an existing Unity AndroidPlayer directory if its location differs from the supplied Unity 6000.3.9f1 default.

Add `--texture-baseline <prior-building-texture-report.json>` when original source PNG/GUID identity must be compared with an earlier approved APK texture report. This comparison reads the current 251 productive source PNGs and `.meta` GUIDs, without modifying them. The optional existing `Tools/Implementation/audit_android_building_textures.py` uses the already validated Unity 6000.3 stripped serialized-file v22 Texture2D parser and is reused directly, without a duplicate copy. Unsupported or ambiguous schemas fail instead of reporting import settings as compression proof. The release audit calls only its APK `audit` function; its preserved standalone historical `--probe` mode is unrelated to the APK audit and requires its own isolated probe receipt.

The APK checks are:

- EOCD and central/local entry metadata, exact names, CRCs, sizes, data descriptors and nonoverlapping local-entry intervals.
- Actual streamed CRC and raw size for every APK entry. Duplicate names, encryption, unknown ZIP64/multidisk layouts, overlaps and unreferenced package holes fail. Both size markers and all bounded ID/value pairs of the APK signing block are parsed before its bytes are excluded from hole accounting. The default allowance is exactly zero holes; `--max-orphan-bytes` makes any exception explicit in the invocation.
- Existing Unity SDK `apksigner verify`, `zipalign -c` and `aapt dump badging`, with log/tool hashes. ReleaseDiagnostic must use BuildOptions.None and have no debuggable manifest flag.
- Successful build receipt, exact APK hash/size, symbol ZIP hash/size, run/build identity, Full/Zip configuration and actual SHA/GNU ELF build IDs for every core library in each ABI. Current public `.so.sym`, full IL2CPP `.so.dbg` and legacy `.so` naming are understood. Missing or ambiguous libraries and wrong identities fail.
- All 16 before/restored temporary settings in `AndroidTestBuild.Build`, including APK/keystore flags, heap, texture subtarget/overrides, native symbols, managed stacktrace and separate PlayerSettings/Application Error/Exception stacktraces. Seven effective diagnostic overrides are also required. Missing fields or changed restoration values fail. A `ProductionReleaseBuild` receipt uses its five recorded pairs and explicitly leaves its two unrecorded Application restoration values unproven.
- With `--textures`, the actual 251 texture objects must use ETC2 RGBA8; GPU bytes and source identity are reported independently of the APK's ZIP size.

The report `apk-audit.json` contains exact artifact/tool SHA-256 values, recorded restoration values and layout/CRC/signature/symbol/optional-texture evidence. The APK's size and modification timestamp are checked before and after inspection. SDK logs and the optional `building-textures.json` stay in the chosen report folder. A failing check returns exit code 1 and a failed report. A successful check returns exit code 0 and `gate=actual_artifact_verification_only`.

A passed artifact audit does not certify gameplay, GPU timing, physical touch, device audio, runtime crash collection or human acceptance. Physical-device, human and release acceptance remain false.

Repeatable synthetic parser checks:

```powershell
python Tools/CommercialReadiness/audit_android_release.py --self-test --output ".utmp/AndroidReleaseAuditSelfTest"
```

These checks cover local/central metadata, corrupt CRC, orphan bytes, descriptor bounds, bounded structural signing blocks, ELF32/ELF64 notes and malformed lengths. A positive synthetic Full ZIP follows Unity's filename convention with `.so.sym` and `.so.dbg` entries for both ABIs, verifies all six actual fixture hashes/build IDs and rejects a wrong ID. Restoration checks cover all 16 pairs, reject changed keystore state and reject missing required evidence. Temporary fixtures are removed; the report explicitly records `actualApkVerified=false` and `nativeBuildExecuted=false`. Its structural signing-block fixture is not cryptographically signed.

The release auditor and the existing texture auditor use `Path(__file__).resolve().parents[2]`, so they move with the project and have no dependency on a dated staging folder. Keep the release auditor under `Tools/CommercialReadiness` and its existing optional dependency under `Tools/Implementation`.
