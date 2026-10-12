"""Compile all Weedtopia assemblies against the installed Unity references; no editor or package restore."""
import pathlib, json, subprocess, hashlib, datetime
ROOT=pathlib.Path(__file__).resolve().parents[2]
EDITOR=pathlib.Path(r'D:\Unity Games\6000.3.9f1\Editor\Data')
OUT=ROOT/'Docs/Implementation/Validation/SourceCompile'
OUT.mkdir(parents=True,exist_ok=True)
definitions={json.loads(p.read_text(encoding='utf-8-sig'))['name']:p for p in (ROOT/'Assets/Weedtopia/Scripts').rglob('*.asmdef')}
refs=list((EDITOR/'UnityReferenceAssemblies/unity-4.8-api').glob('*.dll'))+list((EDITOR/'UnityReferenceAssemblies/unity-4.8-api/Facades').glob('*.dll'))
refs+=list((EDITOR/'Managed').glob('Unity*.dll'))+list((EDITOR/'Managed/UnityEngine').glob('*.dll'))+list((EDITOR/'Managed/UnityEditor').glob('*.dll'))
refs+=list((EDITOR/'PlaybackEngines/AndroidPlayer').glob('UnityEditor.Android.Extensions.dll'))
refs+=list((EDITOR/'PlaybackEngines/AndroidPlayer').glob('Unity.Android.Types.dll'))
refs+=list((ROOT/'Library/ScriptAssemblies').glob('Unity*.dll'))+list((ROOT/'Library/PackageCache').glob('com.unity.ext.nunit*/net40/unity-custom/nunit.framework.dll'))
refs=list({p.name:p for p in refs}.values())
remaining=dict(definitions); compiled={};results=[]
while remaining:
    ready=[(n,p) for n,p in remaining.items() if all(d not in remaining for d in json.loads(p.read_text(encoding='utf-8-sig')).get('references',[]))]
    if not ready:raise RuntimeError('Dependency cycle')
    for name,path in ready:
        nested=[p.parent for p in path.parent.rglob('*.asmdef') if p!=path]
        sources=[p for p in path.parent.rglob('*.cs') if not any(n in p.parents for n in nested)]
        config=json.loads(path.read_text(encoding='utf-8-sig'))
        dependencies=[compiled[d] for d in config.get('references',[]) if d in compiled]
        output=OUT/(name+'.dll');rsp=OUT/(name+'.rsp')
        options=['-nologo','-target:library','-langversion:9.0','-nostdlib+','-define:UNITY_EDITOR;UNITY_INCLUDE_TESTS;UNITY_6000_3_OR_NEWER;ENABLE_INPUT_SYSTEM',f'-out:"{output}"']
        options += [f'-r:"{p}"' for p in refs+dependencies]+[f'"{p}"' for p in sources]
        rsp.write_text('\n'.join(options),encoding='utf-8')
        r=subprocess.run([str(EDITOR/'NetCoreRuntime/dotnet.exe'),str(EDITOR/'DotNetSdkRoslyn/csc.dll'),'@'+str(rsp)],capture_output=True,text=True,encoding='utf-8',errors='replace',cwd=ROOT)
        (OUT/(name+'.log')).write_text(r.stdout+r.stderr,encoding='utf-8')
        print(name,r.returncode,r.stdout,r.stderr)
        results.append(dict(assembly=name,exitCode=r.returncode,sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in sources}))
        if r.returncode:
            (OUT/'result.json').write_text(json.dumps(dict(status='failed',results=results),indent=2));raise SystemExit(1)
        compiled[name]=output;del remaining[name]
(OUT/'result.json').write_text(json.dumps(dict(utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),status='passed',gate='source_compile_only',results=results),indent=2))
