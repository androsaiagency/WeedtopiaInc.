"""Compile the pipeline against installed Unity assemblies without starting Unity.

This verifies C# sources only. It does not import assets, serialize prefabs, or
execute Unity tests. No restore, package installation, or network call is used.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import subprocess
from datetime import datetime, timezone


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--editor-data', type=Path, default=Path(r'D:\Unity Games\6000.3.9f1\Editor\Data'))
    args = parser.parse_args()
    project = Path(__file__).resolve().parents[3]
    destination = project / 'Docs/ArtProduction/Validation/SourceCompile'
    destination.mkdir(parents=True, exist_ok=True)
    editor = args.editor_data
    dotnet = editor / 'NetCoreRuntime/dotnet.exe'
    compiler = editor / 'DotNetSdkRoslyn/csc.dll'
    refs = sorted((editor / 'UnityReferenceAssemblies/unity-4.8-api').glob('*.dll'))
    refs += sorted((editor / 'UnityReferenceAssemblies/unity-4.8-api/Facades').glob('*.dll'))
    refs += sorted((editor / 'Managed').glob('Unity*.dll'))
    refs += sorted((editor / 'Managed/UnityEngine').glob('*.dll'))
    refs += sorted((editor / 'Managed/UnityEditor').glob('*.dll'))
    # Use one assembly per simple filename to avoid duplicate facade definitions.
    refs = list({path.name: path for path in refs}.values())
    if not dotnet.is_file() or not compiler.is_file() or not refs:
        parser.error('Installed Unity compiler/reference assemblies not found.')
    pipeline = project / 'Assets/Weedtopia/Scripts'
    definitions = sorted(pipeline.rglob('*.asmdef'))
    records = {json.loads(p.read_text(encoding='utf-8-sig'))['name']: p for p in definitions}
    if not records:
        parser.error('No pipeline assembly definitions found.')
    results = []
    compiled = {}
    remaining = dict(records)
    while remaining:
        ready = []
        for name, definition in remaining.items():
            config = json.loads(definition.read_text(encoding='utf-8-sig'))
            dependencies = config.get('references', [])
            if all(dep not in records or dep in compiled for dep in dependencies):
                ready.append((name, definition, config))
        if not ready:
            raise RuntimeError('Cyclic or failed assembly dependencies: ' + ', '.join(remaining))
        for name, definition, config in ready:
            nested_roots = {p.parent for p in definition.parent.rglob('*.asmdef') if p != definition}
            sources = sorted(p for p in definition.parent.rglob('*.cs') if not any(root in p.parents for root in nested_roots))
            assembly_refs = refs + [compiled[dep] for dep in config.get('references', []) if dep in compiled]
            if 'TestAssemblies' in config.get('optionalUnityReferences', []) or 'Test' in name:
                assembly_refs += sorted((project / 'Library/PackageCache').glob('com.unity.ext.nunit*/net40/unity-custom/nunit.framework.dll'))
                assembly_refs += sorted((project / 'Library/ScriptAssemblies').glob('Unity*TestRunner.dll'))
            output = destination / (name + '.dll')
            response = destination / (name + '.rsp')
            lines = ['-nologo', '-target:library', '-langversion:9.0', '-nostdlib+', '-define:UNITY_EDITOR;UNITY_EDITOR_WIN;UNITY_INCLUDE_TESTS;UNITY_6000_3_OR_NEWER', '-out:"' + str(output) + '"']
            lines += ['-r:"' + str(path) + '"' for path in assembly_refs]
            lines += ['"' + str(path) + '"' for path in sources]
            response.write_text('\n'.join(lines), encoding='utf-8')
            run = subprocess.run([str(dotnet), str(compiler), '@' + str(response)], cwd=project, capture_output=True, text=True, encoding='utf-8', errors='replace')
            log = run.stdout + run.stderr
            (destination / (name + '.log')).write_text(log, encoding='utf-8')
            result = {'assembly': name, 'exit_code': run.returncode, 'source_count': len(sources), 'sources': {str(p.relative_to(project)).replace('\\', '/'): hashlib.sha256(p.read_bytes()).hexdigest() for p in sources}, 'compiler_output': log}
            results.append(result)
            print(f'{name}: {len(sources)} source files; exit {run.returncode}')
            if log:
                print(log)
            if run.returncode:
                remaining.clear()
                break
            compiled[name] = output
            del remaining[name]
    passed = len(compiled) == len(records) and bool(records)
    report = {'generated_utc': datetime.now(timezone.utc).isoformat(), 'gate': 'source_compile_only', 'status': 'passed' if passed else 'failed', 'unity_import': 'not_performed', 'editmode_execution': 'not_performed', 'editor_data': str(editor), 'results': results}
    (destination / 'source_compile_report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    return 0 if passed else 1


if __name__ == '__main__':
    raise SystemExit(main())
