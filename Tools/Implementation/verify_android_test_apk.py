"""Verify a built test APK with the installed Unity Android tools, without installing it on a device."""
import argparse
import datetime
import hashlib
import json
import pathlib
import re
import subprocess
import zipfile

parser = argparse.ArgumentParser()
parser.add_argument('apk', type=pathlib.Path)
parser.add_argument('--reference-icon-apk', type=pathlib.Path)
args = parser.parse_args()
apk = args.apk.resolve(strict=True)
editor = pathlib.Path(r'D:\Unity Games\6000.3.9f1\Editor\Data\PlaybackEngines\AndroidPlayer')
sdk = editor / 'SDK'
tools = sorted(p for p in (sdk / 'build-tools').iterdir() if (p / 'aapt.exe').exists())[-1]
java = editor / 'OpenJDK/bin/java.exe'
commands = {
    'signature': [str(java), '-jar', str(tools / 'lib/apksigner.jar'), 'verify', '--verbose', '--print-certs', str(apk)],
    'manifest': [str(tools / 'aapt.exe'), 'dump', 'badging', str(apk)],
    'alignment': [str(tools / 'zipalign.exe'), '-c', '-v', '4', str(apk)],
}
results = {}
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
    results['sceneIndices'] = sorted(set(int(m.group(1)) for name in names if (m := re.match(r'assets/bin/Data/level(\d+)(?:\.|$)', name))))
    if args.reference_icon_apk:
        with zipfile.ZipFile(args.reference_icon_apk.resolve(strict=True)) as reference:
            icons = [name for name in reference.namelist() if name.startswith('res/') and ('app_icon' in name or 'ic_launcher' in name)]
            results['packagedIconEntries'] = len(icons)
            results['changedIconEntries'] = [name for name in icons if name not in names or hashlib.sha256(archive.read(name)).digest() != hashlib.sha256(reference.read(name)).digest()]
            results['approvedGreenCityIconPreserved'] = bool(icons) and not results['changedIconEntries']
results.update(utc=datetime.datetime.now(datetime.timezone.utc).isoformat(), apk=str(apk), bytes=apk.stat().st_size,
               sha256=hashlib.file_digest(apk.open('rb'), 'sha256').hexdigest(), deviceTestPerformed=False)
results['passed'] = (all(results[name + 'ExitCode'] == 0 for name in commands) and results['labelWeedtopia']
                     and results['sceneIndices'] == [0, 1, 2] and all(name.startswith('lib/arm64-v8a/') for name in results['nativeLibraries'])
                     and (not args.reference_icon_apk or results['approvedGreenCityIconPreserved']))
(apk.parent / 'apk-verification.json').write_text(json.dumps(results, indent=2), encoding='utf-8')
print(json.dumps(results, indent=2))
raise SystemExit(0 if results['passed'] else 1)
