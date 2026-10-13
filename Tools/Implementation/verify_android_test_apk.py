"""Verify a built test APK with the installed Unity Android tools, without installing it on a device."""
import argparse
import datetime
import hashlib
import json
import pathlib
import re
import subprocess
import zipfile


def icon_resources(apk_path, aapt):
    """Map logical icon name/configuration to its packaged path, including R8 names."""
    run = subprocess.run([str(aapt), 'dump', '--values', 'resources', str(apk_path)],
                         capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=180)
    if run.returncode:
        raise ValueError('Cannot read icon resource table: ' + run.stderr.strip())
    resources, config, pending = {}, '', None
    for line in run.stdout.splitlines():
        match = re.match(r'^\s*config ([^:]+):$', line)
        if match:
            config, pending = match.group(1), None
        match = re.match(r'^\s*resource 0x[0-9a-f]+ [^:]+:(mipmap/(?:app_icon(?:_round)?|ic_launcher_(?:foreground|background))):', line)
        if match:
            pending = (match.group(1), config)
        elif re.match(r'^\s*(?:spec )?resource ', line):
            pending = None
        match = re.match(r'^\s*\(string(?:8|16)?\) "([^"]+)"$', line)
        if match and pending:
            if pending in resources:
                raise ValueError('Duplicate logical icon resource: ' + str(pending))
            resources[pending], pending = match.group(1), None
    densities = ('ldpi', 'mdpi', 'hdpi', 'xhdpi', 'xxhdpi', 'xxxhdpi')
    expected = {(name, density) for name in ('mipmap/app_icon', 'mipmap/app_icon_round',
                'mipmap/ic_launcher_foreground', 'mipmap/ic_launcher_background') for density in densities}
    expected.update((name, 'anydpi-v26') for name in ('mipmap/app_icon', 'mipmap/app_icon_round'))
    if set(resources) != expected:
        raise ValueError('Expected all 24 density PNGs and two adaptive XML icon resources')
    for (_, config), entry in resources.items():
        extension = '.xml' if config == 'anydpi-v26' else '.png'
        if not entry.startswith('res/') or not entry.endswith(extension):
            raise ValueError('Unexpected packaged icon path: ' + entry)
    return resources


def png_identity(payload):
    """Hash every alpha value and visible RGB value; only alpha=0 RGB is canonicalized."""
    import io
    from PIL import Image
    with Image.open(io.BytesIO(payload)) as image:
        rgba = image.convert('RGBA')
        pixels = rgba.tobytes()
        canonical = bytearray(pixels)
        for offset in range(0, len(canonical), 4):
            if canonical[offset + 3] == 0:
                canonical[offset:offset + 3] = b'\0\0\0'
        return {'size': list(rgba.size), 'rgbaSha256': hashlib.sha256(pixels).hexdigest(),
                'visibleRgbaSha256': hashlib.sha256(canonical).hexdigest()}


def compare_icons(current, reference, aapt):
    current_resources = icon_resources(current.filename, aapt)
    reference_resources = icon_resources(reference.filename, aapt)
    comparisons = []
    for key in sorted(reference_resources):
        current_entry, reference_entry = current_resources[key], reference_resources[key]
        current_bytes, reference_bytes = current.read(current_entry), reference.read(reference_entry)
        current_hash = hashlib.sha256(current_bytes).hexdigest()
        reference_hash = hashlib.sha256(reference_bytes).hexdigest()
        item = {'resource': key[0], 'configuration': key[1], 'currentEntry': current_entry,
                'referenceEntry': reference_entry, 'currentFileSha256': current_hash,
                'referenceFileSha256': reference_hash, 'byteIdentical': current_hash == reference_hash}
        if current_entry.endswith('.png'):
            actual, expected = png_identity(current_bytes), png_identity(reference_bytes)
            item.update(currentPng=actual, referencePng=expected,
                        exactVisiblePixelIdentity=actual['size'] == expected['size']
                        and actual['visibleRgbaSha256'] == expected['visibleRgbaSha256'])
            item['preserved'] = item['exactVisiblePixelIdentity']
        else:
            item['preserved'] = item['byteIdentical']
        comparisons.append(item)
    return comparisons


parser = argparse.ArgumentParser()
parser.add_argument('apk', type=pathlib.Path)
parser.add_argument('--reference-icon-apk', type=pathlib.Path)
parser.add_argument('--build-result', type=pathlib.Path,
                    help='Successful build receipt; defaults to build-result.json beside the APK')
args = parser.parse_args()
apk = args.apk.resolve(strict=True)
build_result_path = args.build_result or apk.parent / 'build-result.json'
try:
    build_result_path = build_result_path.resolve(strict=True)
    build = json.loads(build_result_path.read_text(encoding='utf-8-sig'))
    if not isinstance(build, dict):
        raise ValueError('Expected a JSON object')
    if build.get('status') != 'succeeded':
        raise ValueError('Build status must be succeeded')
    recorded_apk = build.get('apkPath', build.get('apk'))
    if not isinstance(recorded_apk, str) or not recorded_apk.strip():
        raise ValueError('Missing APK path')
    if pathlib.Path(recorded_apk).resolve(strict=True) != apk:
        raise ValueError('Build receipt describes a different APK path')
    build_type = build.get('buildType')
    if build_type not in ('Development', 'ReleaseDiagnostic'):
        raise ValueError('Build type must be Development or ReleaseDiagnostic')
    scenes = build.get('scenes')
    if (not isinstance(scenes, list) or not scenes
            or any(not isinstance(scene, str) or not scene.strip() for scene in scenes)):
        raise ValueError('Missing or invalid complete scenes array')
    if len(set(scenes)) != len(scenes):
        raise ValueError('Duplicate scene paths in build receipt')
    if 'bytes' in build and build['bytes'] != apk.stat().st_size:
        raise ValueError('Build receipt byte count differs from the APK')
except (OSError, ValueError, TypeError) as error:
    parser.error('Invalid build receipt ' + str(build_result_path) + ': ' + str(error))
expected_scene_indices = list(range(len(scenes)))
expected_debuggable = build_type == 'Development'
required_native_libraries = {'lib/arm64-v8a/libmain.so', 'lib/arm64-v8a/libunity.so',
                             'lib/arm64-v8a/libil2cpp.so'}
editor = pathlib.Path(r'D:\Unity Games\6000.3.9f1\Editor\Data\PlaybackEngines\AndroidPlayer')
sdk = editor / 'SDK'
tools = sorted(p for p in (sdk / 'build-tools').iterdir() if (p / 'aapt.exe').exists())[-1]
java = editor / 'OpenJDK/bin/java.exe'
commands = {
    'signature': [str(java), '-jar', str(tools / 'lib/apksigner.jar'), 'verify', '--verbose', '--print-certs', str(apk)],
    'manifest': [str(tools / 'aapt.exe'), 'dump', 'badging', str(apk)],
    'alignment': [str(tools / 'zipalign.exe'), '-c', '-v', '4', str(apk)],
}
results = {'buildResult': str(build_result_path), 'buildStatus': build['status'],
           'buildType': build_type, 'expectedSceneIndices': expected_scene_indices,
           'expectedScenes': scenes, 'expectedDebuggable': expected_debuggable}
for name, command in commands.items():
    run = subprocess.run(command, capture_output=True, text=True, encoding='utf-8', errors='replace')
    (apk.parent / ('apk-' + name + '.txt')).write_text(run.stdout + run.stderr, encoding='utf-8')
    results[name + 'ExitCode'] = run.returncode
    if name == 'manifest':
        results['labelWeedtopia'] = "application-label:'Weedtopia'" in run.stdout
        results['debuggable'] = 'application-debuggable' in run.stdout
with zipfile.ZipFile(apk) as archive:
    names = archive.namelist()
    results['zipEntryCount'] = len(names)
    results['nativeLibraries'] = [name for name in names if name.startswith('lib/') and name.endswith('.so')]
    results['missingCoreNativeLibraries'] = sorted(required_native_libraries - set(results['nativeLibraries']))
    results['arm64NativeLibrariesVerified'] = (bool(results['nativeLibraries'])
                                              and not results['missingCoreNativeLibraries']
                                              and all(name.startswith('lib/arm64-v8a/')
                                                      for name in results['nativeLibraries']))
    results['sceneIndices'] = sorted(set(int(m.group(1)) for name in names if (m := re.match(r'assets/bin/Data/level(\d+)(?:\.|$)', name))))
    if args.reference_icon_apk:
        results['approvedGreenCityIconPreserved'] = False
        try:
            reference_path = args.reference_icon_apk.resolve(strict=True)
            with zipfile.ZipFile(reference_path) as reference:
                icons = compare_icons(archive, reference, tools / 'aapt.exe')
            results['referenceIconApk'] = str(reference_path)
            results['packagedIconEntries'] = len(icons)
            results['iconComparisonMethod'] = ('Logical resource/configuration mapping; exact XML bytes and PNG '
                                               'dimensions/visible RGBA hashes; RGB canonicalized only at alpha=0')
            results['iconComparisons'] = icons
            results['iconBytesIdentical'] = all(item['byteIdentical'] for item in icons)
            results['exactVisiblePixelIdentity'] = all(item['exactVisiblePixelIdentity']
                                                      for item in icons if item['currentEntry'].endswith('.png'))
            results['changedIconEntries'] = [item['resource'] + '@' + item['configuration']
                                             for item in icons if not item['preserved']]
            results['approvedGreenCityIconPreserved'] = not results['changedIconEntries']
        except (OSError, ValueError, KeyError, zipfile.BadZipFile, subprocess.TimeoutExpired) as error:
            results['iconComparisonError'] = str(error)
results.update(utc=datetime.datetime.now(datetime.timezone.utc).isoformat(), apk=str(apk), bytes=apk.stat().st_size,
               sha256=hashlib.file_digest(apk.open('rb'), 'sha256').hexdigest(), deviceTestPerformed=False)
results['passed'] = (all(results[name + 'ExitCode'] == 0 for name in commands) and results['labelWeedtopia']
                     and results['sceneIndices'] == expected_scene_indices
                     and results['debuggable'] == expected_debuggable
                     and results['arm64NativeLibrariesVerified']
                     and (not args.reference_icon_apk or results['approvedGreenCityIconPreserved']))
(apk.parent / 'apk-verification.json').write_text(json.dumps(results, indent=2), encoding='utf-8')
print(json.dumps(results, indent=2))
raise SystemExit(0 if results['passed'] else 1)
