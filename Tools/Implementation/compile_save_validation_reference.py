"""Compile the preserved save validators in an isolated namespace for native differential tests."""
import datetime
import hashlib
import json
import pathlib
import subprocess

ROOT = pathlib.Path(__file__).resolve().parents[2]
BASELINE = ROOT / 'Docs/CommercialReadiness/2026-10-06/Baseline'
COMPILED = ROOT / 'Docs/Implementation/Validation/SourceCompile'
EDITOR = pathlib.Path(r'D:\Unity Games\6000.3.9f1\Editor\Data')
NAMES = ('LocalSaveStore', 'RewardSaveValidator', 'ShopSaveValidator', 'VipStorySaveValidator')

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    BASELINE.mkdir(parents=True, exist_ok=True)
    inputs = {}
    generated = []
    for name in NAMES:
        source = BASELINE / (name + '.cs')
        if not source.exists():
            # The two changed sources must have been preserved before editing.
            if name in NAMES[:2]:
                raise FileNotFoundError(source)
            source.write_bytes((ROOT / 'Assets/Weedtopia/Scripts/Runtime/Persistence' / source.name).read_bytes())
        text = source.read_text(encoding='utf-8-sig')
        if text.count('namespace Weedtopia.Persistence') != 1:
            raise ValueError('Unexpected reference namespace: ' + name)
        target = BASELINE / (name + '.reference.generated.cs')
        target.write_text(text.replace('namespace Weedtopia.Persistence', 'namespace Weedtopia.Persistence.Reference'), encoding='utf-8')
        inputs[source.relative_to(ROOT).as_posix()] = digest(source)
        generated.append(target)
    output = BASELINE / ('Weedtopia.SaveValidationReference-' + digest(COMPILED / 'Weedtopia.Domain.dll')[:16] + '.dll')
    refs = [line for line in (COMPILED / 'Weedtopia.Persistence.rsp').read_text().splitlines() if line.startswith('-r:')]
    options = ['-nologo', '-target:library', '-langversion:9.0', '-nostdlib+', '-deterministic+', f'-out:"{output}"']
    options += refs + [f'"{path}"' for path in generated]
    response = BASELINE / 'SaveValidationReference.rsp'
    response.write_text('\n'.join(options), encoding='utf-8')
    result = subprocess.run([str(EDITOR/'NetCoreRuntime/dotnet.exe'), str(EDITOR/'DotNetSdkRoslyn/csc.dll'), '@'+str(response)], cwd=ROOT, capture_output=True, text=True)
    (BASELINE/'save_reference_compile.log').write_text(result.stdout+result.stderr, encoding='utf-8')
    receipt = dict(utc=datetime.datetime.now(datetime.timezone.utc).isoformat(), status='passed' if result.returncode == 0 else 'failed',
                   scope='Isolated historical save validators sharing current Domain contracts; no native execution',
                   sources=inputs, referenceAssembly=output.relative_to(ROOT).as_posix(), referenceAssemblySha256=digest(output) if output.exists() else '')
    (BASELINE/'save_reference_compile.json').write_text(json.dumps(receipt, indent=2), encoding='utf-8')
    print(json.dumps(receipt))
    return result.returncode

if __name__ == '__main__':
    raise SystemExit(main())
