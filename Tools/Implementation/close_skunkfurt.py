"""Read productive content and completed receipts; report bounded Skunkfurt integration."""
import argparse, datetime, hashlib, json, struct, xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; DOC=ROOT/'Docs/Skunkfurt'
CONTENT='Assets/Weedtopia/Game/Content/BurningHills.json'
LANGUAGES=('de','en','es','fr','ru')
CATALOGS=[CONTENT,*[f'Assets/Weedtopia/Game/Localization/{lang}.json' for lang in LANGUAGES]]
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def stamp(s):return datetime.datetime.fromisoformat(s.replace('Z','+00:00')).timestamp()
def main(args):
    checks=[];errors=[]
    def check(name,ok,detail=''):
        checks.append(dict(check=name,passed=bool(ok),detail=detail))
        if not ok:errors.append(name)
    content=read(ROOT/CONTENT);city=next(c for c in content['cities'] if c['id']=='skunkfurt')
    sources=[c['id'] for c in content['cities'] if c['id']!='skunkfurt']
    check('Version13 and twelve authored cities',content['version']==13 and len(content['cities'])==12)
    check('Ten million contemporaneous residents across all eleven preceding cities',city['unlockResidents']==10000000 and city['unlockSources']==sources and not city['sourceCity'])
    check('One-time city charter grant',city['bridgeMoney']==15000 and city['bridgeCards']==8)
    people=[p for p in content['vipStories'] if p['cityId']=='skunkfurt'];projects=[p for p in content['vipCollaborations'] if p['cityId']=='skunkfurt']
    check('Six permanent residents,18 chapters and18 recurring tasks',len(people)==6 and sum(len(p['chapters']) for p in people)==18 and sum(len(p['recurring']) for p in people)==18)
    check('Three chapter-linked civic projects',len(projects)==3)
    sites=[b for b in content['instances'] if b['cityId']=='skunkfurt'];ids={b['id'] for b in sites};routes=[r for r in content['routes'] if r['from'] in ids]
    check('56 sites and35 local routes',len(sites)==56 and len(routes)==35 and all(r['to'] in ids for r in routes))
    check('18 main quests',len([q for q in content['quests'] if q.get('cityId')=='skunkfurt'])==18)
    jonas=next(p for p in people if p['id']=='sf_jonas')
    check('Both durable Jonas outcomes',{c['id'] for c in jonas['chapters'][2]['choices']}=={'fair_finance_desk','housing_council'})
    old=read(DOC/'Preservation/Before'/CONTENT);house=next(b for b in content['buildings'] if b['id']=='residential_house');old_house=next(b for b in old['buildings'] if b['id']=='residential_house')
    check('Housing before level5000 unchanged',house['baseResidents']==old_house['baseResidents'] and house['residentTierBonuses'][:-1]==old_house['residentTierBonuses'][:-1])
    check('Only final housing bonus475 to1000',old_house['residentTierBonuses']==[0]*24+[475] and house['residentTierBonuses']==[0]*24+[1000])
    maximum=int((house['baseResidents']*25+1000)*(1+content['workerAttributeStep']*100*10))*17*11
    check('Threshold reachable with seventeen houses and a retained park per city',maximum==10729125)
    preserve=read(DOC/'Preservation/preservation_receipt.json'); strict=read(DOC/'Preservation/prefab_result.json')
    check('Actual dirty working checkout preserved',preserve['status']=='passed')
    check('All11 old city scenes andBootstrap byte-identical; one connected travel row appended',strict['status']=='passed' and strict['sharedCampaign']['travelRowsAfter']==12)
    compile=read(ROOT/'Docs/Implementation/Validation/SourceCompile/result.json')
    check('All source assemblies compile',compile['status']=='passed' and all(x['exitCode']==0 for x in compile['results']))
    check('Compiled source hashes match final checkout',all(digest(ROOT/name)==value for assembly in compile['results'] for name,value in assembly['sources'].items()))
    author=read(DOC/'authoring_receipt.json')
    check('Explicit Unity installation succeeded',author['status']=='passed' and author['contentVersion']==13 and not author['runtimeStaticObjects'])
    idem=read(DOC/'content_idempotence_receipt.json')
    check('Content authoring is byte-idempotent',idem['byteIdenticalOnSecondRun'] and not idem['changedOnSecondRun'] and {n.replace('\\','/') for n in idem['files']}==set(CATALOGS) and all(digest(ROOT/name)==value for name,value in idem['files'].items()))
    latest=max((ROOT/name).stat().st_mtime for name in CATALOGS)
    tests=[];fixtures=set();cases={};first_failures=set()
    receipts=sorted((read(ROOT/relative) for relative in args.tests),key=lambda r:r['startedUtc'])
    check('Original122case domain run remains in audit trail',any(r['run']=='20261013_062917_EditMode_SkunkfurtDomainAndRegression' and r['total']==122 for r in receipts))
    check('Unique NUnit run receipts',len({r['run'] for r in receipts})==len(receipts))
    for receipt in receipts:
        xml=Path(receipt['xml']);tree=ET.parse(xml).getroot();test_cases=tree.findall('.//test-case')
        passed=sum(c.attrib['result']=='Passed' for c in test_cases);failed=sum(c.attrib['result']=='Failed' for c in test_cases)
        expected=('passed',0) if not failed else ('failed',2)
        root_results={'Passed'} if not failed else {'Failed','Failed(Child)'}
        check('NUnit completed with consistent actual results '+receipt['run'],(receipt['status'],receipt['exitCode'])==expected and tree.attrib['result'] in root_results and len(test_cases)==int(tree.attrib['total'])==receipt['total'] and passed==int(tree.attrib['passed'])==receipt['passed'] and failed==int(tree.attrib['failed'])==receipt['failed'] and len(test_cases)>0 and passed+failed==len(test_cases))
        check('NUnit used final catalogs '+receipt['run'],stamp(receipt['startedUtc'])>=latest)
        check('Fresh NUnit XML '+receipt['run'],xml.stat().st_mtime>=stamp(receipt['startedUtc']) and xml.stat().st_mtime<=stamp(receipt['endedUtc']))
        check('Unique test case names within run '+receipt['run'],len({c.attrib['fullname'] for c in test_cases})==len(test_cases))
        for case in test_cases:
            name=case.attrib['fullname'];cases[name]=dict(result=case.attrib['result'],run=receipt['run'],fixture=case.attrib.get('classname',''))
            if case.attrib['result']=='Failed':first_failures.add(name)
        fixtures.update(c.attrib.get('classname','') for c in test_cases)
        tests.append(dict(run=receipt['run'],status=receipt['status'],total=receipt['total'],passed=receipt['passed'],failed=receipt['failed'],xml=str(xml),sha256=digest(xml)))
    check('Every executed case has a passing latest result',len(cases)>0 and all(c['result']=='Passed' for c in cases.values()))
    check('All initial failures explicitly rerun and passed',all(cases[name]['result']=='Passed' for name in first_failures))
    check('All45 Skunkfurt domain cases covered',sum(c['fixture']=='Weedtopia.Tests.SkunkfurtCampaignTests' for c in cases.values())==45)
    for fixture in ('Weedtopia.Tests.Assets.SkunkfurtAssetTests','Weedtopia.Tests.PlayMode.SkunkfurtPlayModeTests'):
        check('All six cases covered '+fixture,sum(c['fixture']==fixture for c in cases.values())==6)
    required={'Weedtopia.Tests.SkunkfurtCampaignTests','Weedtopia.Tests.Assets.SkunkfurtAssetTests','Weedtopia.Tests.PlayMode.SkunkfurtPlayModeTests'}
    check('Skunkfurt domain,asset andPlayMode scenarios executed',required<=fixtures)
    check('Previous-city regression executed','Weedtopia.Tests.GreatKushvilleCampaignTests' in fixtures)
    captures=[read(p) for p in (DOC/'Validation').glob('*Capture*.json')];capture=max(captures,key=lambda r:r['startedUtc'])
    game=read(DOC/'gameview_receipt.json');overflow=read(DOC/'ui_overflow.json');capture_start=stamp(capture['startedUtc'])
    check('Owned Unity rendered capture completed on final catalogs',capture['status']=='passed' and capture['exitCode']==0 and capture_start>=latest)
    check('Four-resolution overflow check',overflow['passed'] and not overflow['issues'])
    for flag in ('savedStoryRoundtrip','housingInspectionMatchesSimulation','returnedToPreviousCity','allTwelveCitiesVisited','twelveCityOfflineRoundtrip','jonasChoiceVariantsVerified','architectureGroundAnchorsVerified'):
        check('GameView '+flag,game.get(flag)==True)
    check('All twelve destinations rendered',set(game['visitedCities'])==set(sources+['skunkfurt']))
    check('Both Jonas choices rendered',set(game['verifiedChoices'])=={'fair_finance_desk','housing_council'})
    screenshots=[]
    for item in game['dimensions']:
        p=ROOT/item['path'];b=p.read_bytes();w,h=struct.unpack('>II',b[16:24]);check('Fresh exact PNG '+p.name,b[:8]==b'\x89PNG\r\n\x1a\n' and (w,h)==(item['width'],item['height']) and p.stat().st_mtime>=capture_start)
        screenshots.append(dict(path=item['path'],width=w,height=h,sha256=digest(p)))
    check('85 distinct screenshots',len(screenshots)==85 and len({s['path'] for s in screenshots})==85)
    sizes={(1920,1080),(1366,768),(2560,1080),(1080,1920)}
    expected={(scenario,lang,w,h) for scenario in ('initial_quest','travel_final_row','jonas_choice') for lang in LANGUAGES for w,h in sizes}
    actual={(r['scenario'],r['language'],r['width'],r['height']) for r in game['matrix']}
    check('Full60case language andresolution matrix',actual==expected and len(game['matrix'])==60)
    review=read(DOC/'visual_review.json');check('Actual GameView samples visually inspected',review['gameViewReviewed'] and review['captureRun']==capture['run'] and not review['blockingGameViewIssuesObserved'])
    screenshot_paths={s['path'] for s in screenshots}
    check('Reviewed samples match exact final capture files',len(review['inspected'])>=15 and all(item['path'] in screenshot_paths and digest(ROOT/item['path'])==item['sha256'] for item in review['inspected']))
    counts={fixture:sum(c['fixture']==fixture for c in cases.values()) for fixture in sorted(fixtures)}
    result=dict(utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),status='passed' if not errors else 'failed',city='skunkfurt',unlockResidents=10000000,maximumPreviousCityPopulation=maximum,tests=tests,totalTests=len(cases),totalTestExecutions=sum(t['total'] for t in tests),finalCasesPerFixture=counts,sourceCompiledAssemblies=len(compile['results']),initialFailures=len(first_failures),initialFailuresRetested=sum(cases[name]['result']=='Passed' for name in first_failures),unresolvedTestFailures=[name for name,c in cases.items() if c['result']!='Passed'],gameView=dict(screenshots=len(screenshots),matrix=len(game['matrix']),visuallyInspectedSamples=len(review['inspected']),visitedCities=game['visitedCities'],captureRun=capture['run']),checks=checks,errors=errors,diagnosticGrowthFixtures=True,fullHumanPlaythrough=False,physicalDeviceAcceptance=False,releaseAccepted=False,userArtApproval=False,nativeSpeakerReviewCompleted=False)
    (DOC/'validation_summary.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps({k:v for k,v in result.items() if k!='checks'},indent=2));return not errors
if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--tests',nargs='+',required=True);raise SystemExit(not main(parser.parse_args()))
