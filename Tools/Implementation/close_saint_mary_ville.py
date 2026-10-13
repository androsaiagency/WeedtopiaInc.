"""Read actual Saint Mary Ville evidence and report bounded local integration."""
import argparse,datetime,hashlib,json,struct,xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];DOC=ROOT/'Docs/SaintMaryVille'
CITY='saint_mary_ville';EXPECTED=['sm_mara','sm_elias','sm_rosa','sm_finn','sm_amira','sm_hugo']
def load(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    parser=argparse.ArgumentParser();parser.add_argument('--tests',nargs='+',required=True);args=parser.parse_args();checks=[];errors=[]
    def check(name,ok,detail=''):
        checks.append(dict(check=name,passed=bool(ok),detail=detail))
        if not ok:errors.append(name+(': '+detail if detail else ''))
    catalog=load(ROOT/'Assets/Weedtopia/Game/Content/BurningHills.json');charter=next(c for c in catalog['cities'] if c['id']==CITY)
    check('Version 11 and ten cities',catalog['version']==11 and len(catalog['cities'])==10)
    sources=[c['id'] for c in catalog['cities'] if c['id']!=CITY]
    check('Two million combined residents across nine preceding cities',charter['unlockResidents']==2000000 and charter['unlockSources']==sources)
    people=[p for p in catalog['vipStories'] if p['cityId']==CITY];projects=[p for p in catalog['vipCollaborations'] if p['cityId']==CITY]
    check('Six authored permanent residents',[p['id'] for p in people]==EXPECTED)
    check('24 personal chapters and 18 recurring variants',sum(len(p['chapters']) for p in people)==24 and sum(len(p['recurring']) for p in people)==18)
    check('Three local community projects',len(projects)==3)
    check('56 sites and 35 routes',sum(i['cityId']==CITY for i in catalog['instances'])==56 and sum(r['id'].startswith('sm_') for r in catalog['routes'])==35)
    old=load(DOC/'Preservation/Before/Assets/Weedtopia/Game/Content/BurningHills.json');house=next(b for b in catalog['buildings'] if b['id']=='residential_house');oldhouse=next(b for b in old['buildings'] if b['id']=='residential_house')
    check('Earlier housing bands preserved',house['baseResidents']==oldhouse['baseResidents'] and house['residentTierBonuses'][:-1]==oldhouse['residentTierBonuses'][:-1])
    check('Final density makes requested threshold reachable with one park per city',house['residentTierBonuses']==[0]*24+[135])
    maxhouse=(house['baseResidents']*25+house['residentTierBonuses'][-1])*(1+catalog['workerAttributeStep']*100*10)
    maximum=int(maxhouse*17*9);check('Natural maximum exceeds two million',maximum>=2000000,str(maximum))
    cases={};runs=[]
    for name in args.tests:
        p=ROOT/name;receipt=load(p);xml=Path(receipt['xml']);xml=xml if xml.is_absolute() else ROOT/xml
        tree=ET.parse(xml).getroot();counts={k:int(tree.get(k,'0')) for k in ('total','passed','failed','skipped','inconclusive')}
        check('NUnit XML agrees with receipt '+receipt['run'],counts['total']==receipt['total'] and counts['passed']==receipt['passed'] and counts['failed']==receipt['failed'])
        check('All cases passed '+receipt['run'],receipt['status']=='passed' and tree.get('result')=='Passed' and counts['total']>0 and counts['failed']==0 and counts['skipped']==0 and counts['inconclusive']==0)
        for case in tree.iter('test-case'):cases[case.get('fullname')]=case.get('result')
        runs.append(dict(receipt=name,xml=xml.relative_to(ROOT).as_posix(),**counts))
    content=load(DOC/'content_receipt.json');author=load(DOC/'authoring_receipt.json');game=load(DOC/'gameview_receipt.json');overflow=load(DOC/'ui_overflow.json');preserve=load(DOC/'Preservation/result.json')
    check('Authored content receipts',content['version']==11 and content['permanentResidents']==6 and author['status']=='passed')
    check('Actual PlayMode Game View captures',game['status']=='passed' and game['mode'].startswith('actual_playmode'))
    capture_runs=[load(path) for path in (DOC/'Validation').glob('*_Capture_*.json')]
    current_capture=max(capture_runs,key=lambda receipt:receipt.get('startedUtc',''))
    capture_start=datetime.datetime.fromisoformat(current_capture['startedUtc'].replace('Z','+00:00')).timestamp()
    check('Final owned Unity capture completed successfully',current_capture['status']=='passed' and current_capture['exitCode']==0)
    check('Every capture belongs to the final completed run',all((ROOT/path).stat().st_mtime>=capture_start for path in game['screenshots']))
    check('Four resolution UI overflow check',overflow['passed'] and not overflow['issues'])
    check('Preservation of existing scenes/content/settings',preserve['status']=='passed')
    screenshots=[];resolutions=set()
    for entry in game['dimensions']:
        p=ROOT/entry['path'];data=p.read_bytes();w,h=struct.unpack('>II',data[16:24]);resolutions.add((w,h))
        check('PNG dimensions '+p.name,data[:8]==b'\x89PNG\r\n\x1a\n' and w==entry['width'] and h==entry['height'])
        screenshots.append(dict(path=entry['path'],width=w,height=h,sha256=digest(p)))
    check('Required four resolutions',resolutions>={(1920,1080),(1366,768),(2560,1080),(1080,1920)})
    for flag in ('savedStoryRoundtrip','housingInspectionMatchesSimulation','returnedToPreviousCity','allTenCitiesVisited','tenCityOfflineRoundtrip','eliasChoiceVariantsVerified','architectureGroundAnchorsVerified'):
        check('Game View '+flag,game.get(flag) is True)
    check('All five languages rendered',set(game.get('languages',[]))=={'de','en','es','fr','ru'})
    matrix=game.get('matrix',[])
    expected_matrix={(scenario,language,w,h) for scenario in ('initial_quest','travel_final_row','elias_choice') for language in ('de','en','es','fr','ru') for w,h in ((1920,1080),(1366,768),(2560,1080),(1080,1920))}
    actual_matrix={(entry['scenario'],entry['language'],entry['width'],entry['height']) for entry in matrix}
    screenshot_paths={entry['path'] for entry in screenshots}
    check('Complete language and resolution matrix',len(matrix)==60 and actual_matrix==expected_matrix and all(entry['path'] in screenshot_paths for entry in matrix))
    check('83 distinct actual screenshots',len(screenshots)==83 and len(screenshot_paths)==83 and set(game['screenshots'])==screenshot_paths)
    idempotence=load(DOC/'content_idempotence_receipt.json')
    canonical_digest=hashlib.sha256(json.dumps(catalog,sort_keys=True,ensure_ascii=False,separators=(',', ':')).encode('utf-8')).hexdigest()
    check('Final content digest matches productive catalog',idempotence['finalContentDigest']==canonical_digest)
    check('Productive content and all locale hashes match final receipts',len(idempotence['files'])==6 and all(digest(ROOT/path)==expected for path,expected in idempotence['files'].items()))
    check('Content authoring is idempotent',idempotence['byteIdenticalOnSecondRun'] is True and idempotence['changedOnSecondRun'] is False)
    visual=load(DOC/'visual_review.json')
    check('Actual Game View visually inspected',visual.get('gameViewReviewed') is True and not visual.get('blockingGameViewIssuesObserved',[]))
    interrupted=load(DOC/'Validation/interrupted_full_domain.json')
    result=dict(utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),status='passed_local_integration' if not errors else 'failed',technicalIntegrationComplete=not errors,uniqueNUnitPassed=sum(v=='Passed' for v in cases.values()),tests=runs,population=dict(maximumPerHouse=int(maxhouse),mandatoryParksPerCity=1,housesPerCity=17,sourceCityCount=9,maximumPreviousCitiesPopulation=maximum),finalCaptureRun=current_capture,gameView=game,screenshots=screenshots,checks=checks,errors=errors,fullDomainRegressionCompleted=False,interruptedFullDomainRun=interrupted,diagnosticGrowthFixtures=True,fullHumanPlaythrough=False,physicalDeviceAcceptance=False,releaseAccepted=False,userArtApproval=False,nativeSpeakerReviewCompleted=False)
    (DOC/'validation_summary.json').write_text(json.dumps(result,indent=2,ensure_ascii=False),encoding='utf-8')
    print(json.dumps(dict(status=result['status'],uniqueNUnitPassed=result['uniqueNUnitPassed'],checks=len(checks),screenshots=len(screenshots),errors=errors),indent=2))
    return int(bool(errors))
if __name__=='__main__':raise SystemExit(main())
