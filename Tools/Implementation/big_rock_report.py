"""Collect fresh native gate evidence; never promotes an unrun release gate."""
import datetime, hashlib, json, pathlib, re, xml.etree.ElementTree as ET

ROOT=pathlib.Path(__file__).resolve().parents[2]
DOC=ROOT/'Docs/BigRockCity'

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def latest(folder, pattern):
    paths=sorted((ROOT/folder).glob(pattern))
    if not paths: return {'status':'not_run'}
    data=read(paths[-1]);data['report']=paths[-1].relative_to(ROOT).as_posix();return data

def references():
    sources=list((ROOT/'Assets').rglob('*.meta'))+list((ROOT/'Library/PackageCache').rglob('*.meta'))
    identifiers={}
    for path in sources:
        match=re.search(r'^guid: ([0-9a-f]{32})',path.read_text(errors='replace'),re.M)
        if match: identifiers.setdefault(match[1],[]).append(path.relative_to(ROOT).as_posix())
    assets=[ROOT/'Assets/Weedtopia/Game/Scenes/BigRockCity.unity']
    assets+=list((ROOT/'Assets/Weedtopia/Production/BigRockCity').rglob('*.prefab'))
    assets+=list((ROOT/'Assets/Weedtopia/Production/BigRockCity').rglob('*.asset'))
    missing=sorted({guid for path in assets for guid in re.findall(r'guid: ([0-9a-f]{32})',path.read_text(errors='replace'))
                    if guid not in identifiers and not guid.startswith('0000000000000000')})
    duplicates={guid:paths for guid,paths in identifiers.items() if len(paths)>1 and any('/BigRockCity/' in p for p in paths)}
    result={'status':'passed' if not missing and not duplicates else 'failed','checkedAssets':len(assets),'unresolvedGuids':missing,'duplicateNewGuids':duplicates}
    (DOC/'asset_reference_receipt.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    return result

def test_results(pattern, minimum):
    paths=sorted((ROOT/'Docs/Implementation/Validation').glob(pattern))
    if not paths:return {'status':'not_run'}
    results={};runs=[]
    for path in paths:
        data=read(path);runs.append(path.relative_to(ROOT).as_posix())
        xml=path.with_suffix('.xml')
        if not xml.exists():return {'status':'failed','missingXml':str(xml)}
        for case in ET.parse(xml).iter('test-case'):results[case.get('fullname')]=case.get('result')
    failed=[name for name,result in results.items() if result!='Passed']
    return {'status':'passed' if not failed and len(results)>=minimum else 'failed','total':len(results),
            'passed':sum(r=='Passed' for r in results.values()),'failed':len(failed),'unresolvedTests':failed,
            'method':'Full assembly followed by targeted reruns of corrected tests; last result per test', 'reports':runs}

def score_isolation():
    receipt=read(DOC/'Preservation/score_isolation_receipt.json')
    path=pathlib.Path.home()/'AppData/LocalLow/DefaultCompany/WeedtopiaInc/local_leaderboard.json'
    actual=hashlib.sha256(path.read_bytes()).hexdigest() if path.exists() else None
    return {'status':'passed' if receipt['status']=='passed' and actual==receipt['finalFileSha256'] else 'failed',
            'playerLeaderboardUnchangedAfterValidation':actual==receipt['finalFileSha256'],
            'preservedPlayerEntries':receipt['preservedPlayerEntries'],
            'removedOwnedDiagnosticRows':receipt['removedOwnedDiagnosticRows'],
            'testAndCaptureStores':'Isolated save and leaderboard paths carried across scene handoffs',
            'receipt':'Docs/BigRockCity/Preservation/score_isolation_receipt.json'}

def source_compile():
    report=read(ROOT/'Docs/Implementation/Validation/SourceCompile/result.json')
    stale=[name for assembly in report.get('results',[]) for name,expected in assembly.get('sources',{}).items()
           if not (ROOT/name).exists() or hashlib.sha256((ROOT/name).read_bytes()).hexdigest()!=expected]
    report['staleSources']=stale
    if stale:report['status']='failed'
    return report

def main():
    gates={
        'sourceCompile':source_compile(),
        'domain':latest('Docs/Implementation/Validation','*_EditMode_BigRockCityFinalDomain.json'),
        'playMode':test_results('*_PlayMode_BigRockCityFinalPlay.json',68),
        'editorAssets':test_results('*_EditMode_BigRockCityFinalAssets.json',43),
        'authoring':latest('Docs/BigRockCity/Validation','*_Install_*.json'),
        'gameView':latest('Docs/BigRockCity/Validation','*_Capture_*.json'),
        'preservation':read(DOC/'Preservation/result.json'),
        'scoreIsolation':score_isolation(),
        'assetReferences':references(),
        'uiLayout':read(DOC/'ui_overflow.json'),
        'visualReview':read(DOC/'visual_review.json') if (DOC/'visual_review.json').exists() else {'status':'not_run'}
    }
    required=['sourceCompile','domain','playMode','editorAssets','authoring','gameView','preservation','scoreIsolation','assetReferences','visualReview']
    passed=all(gates[k].get('status')=='passed' for k in required) and gates['uiLayout'].get('passed') is True
    report={'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'implementationStatus':'verified' if passed else 'incomplete',
        'contentVersion':3,'city':'big_rock_city','unlock':{'source':'burning_hills','residents':1000,'oneTimeMoney':2500,'oneTimeCards':2},
        'content':{'sites':56,'routes':35,'buildingVariants':251,'mainQuests':16,'inhabitants':6,'chapters':24,'recurringVariants':18,'collaborations':3},
        'gates':gates,'releaseAcceptance':{'userVisualApproval':False,'humanPlaythrough':'not_run','physicalTouch':'not_run','deviceAudio':'not_run','webglBuild':'not_run'},
        'diagnosticSaveOnly':True,'published':False}
    (DOC/'validation_summary.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({'implementationStatus':report['implementationStatus'],'gates':{k:v.get('status','passed' if v.get('passed') else 'failed') for k,v in gates.items()}},indent=2))
    if not passed: raise SystemExit(1)

if __name__=='__main__':main()
