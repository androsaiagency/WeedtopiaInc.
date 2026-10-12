"""Compile the saved original Simulation as an isolated .NET 4.8 reference for native Unity benchmarking.

Run after Tools/Implementation/compile_sources.py. No Unity process is started, and no Assets are written.
"""
import datetime
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys
import uuid
import argparse

ROOT = Path(__file__).resolve().parents[2]
BASELINE = ROOT / "Docs/CommercialReadiness/2026-10-05/Baseline"
SOURCE = BASELINE / "Simulation.cs"
DOMAIN = ROOT / "Docs/Implementation/Validation/SourceCompile/Weedtopia.Domain.dll"
DOMAIN_RSP = DOMAIN.with_suffix(".rsp")
EDITOR = Path(r"D:\Unity Games\6000.3.9f1\Editor\Data")

def digest(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output-directory', help='Fresh project-local evidence directory; original source is preserved.')
    args = parser.parse_args()
    output_folder = (ROOT / args.output_directory).resolve() if args.output_directory else BASELINE
    evidence_root = (ROOT / 'Docs/CommercialReadiness').resolve()
    if output_folder != evidence_root and evidence_root not in output_folder.parents:
        raise ValueError('Reference output must stay inside project commercial-readiness evidence.')
    output_folder.mkdir(parents=True, exist_ok=True)
    for required in (SOURCE, DOMAIN, DOMAIN_RSP):
        if not required.is_file():
            raise FileNotFoundError(f"Missing required reference input: {required}")
    original = SOURCE.read_text(encoding="utf-8-sig")
    renamed, classes = re.subn(r"\bclass Simulation\b", "class ReferenceSimulation", original)
    renamed, constructors = re.subn(r"\bpublic Simulation\(", "public ReferenceSimulation(", renamed)
    if classes != 1 or constructors != 1:
        raise ValueError("Expected exactly one original class and constructor. Do not guess a different baseline.")
    generated = output_folder / "ReferenceSimulation.generated.cs"
    generated.write_text(renamed, encoding="utf-8")
    # Unity keeps loaded reference assemblies mapped until its next domain reload.
    # A fresh owned output never overwrites a file held by the user's open editor.
    output = output_folder / ("Weedtopia.OfflineReference-" + digest(DOMAIN)[:16] + "-" + uuid.uuid4().hex[:8] + ".dll")
    refs = [line for line in DOMAIN_RSP.read_text(encoding="utf-8-sig").splitlines() if line.startswith("-r:")]
    if not refs:
        raise ValueError("Domain response file has no .NET 4.8 references")
    options = ["-nologo", "-target:library", "-langversion:9.0", "-nostdlib+", "-deterministic+",
               f'-out:"{output}"'] + refs + [f'-r:"{DOMAIN}"', f'"{generated}"']
    rsp = output_folder / "OfflineReference.rsp"
    rsp.write_text("\n".join(options), encoding="utf-8")
    command = [str(EDITOR / "NetCoreRuntime/dotnet.exe"), str(EDITOR / "DotNetSdkRoslyn/csc.dll"), "@" + str(rsp)]
    compiled = subprocess.run(command, cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace")
    (output_folder / "reference_compile.log").write_text(compiled.stdout + compiled.stderr, encoding="utf-8")
    receipt = dict(utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
                   status="passed" if compiled.returncode == 0 else "failed", exitCode=compiled.returncode,
                   scope="reference original Simulation class compiled against current shared Domain contracts; no Unity execution",
                   framework="Unity .NET 4.8 reference assemblies", originalSource=str(SOURCE.relative_to(ROOT)),
                   originalSourceSha256=digest(SOURCE), renamedSourceSha256=digest(generated),
                   domainAssemblySha256=digest(DOMAIN), referenceAssembly=str(output.relative_to(ROOT)),
                   referenceAssemblySha256=digest(output) if compiled.returncode == 0 else "")
    (output_folder / "reference_compile.json").write_text(json.dumps(receipt, indent=2), encoding="utf-8")
    print(json.dumps(receipt, ensure_ascii=True))
    if compiled.returncode:
        print(compiled.stdout + compiled.stderr)
    return compiled.returncode

if __name__ == "__main__":
    sys.exit(main())
