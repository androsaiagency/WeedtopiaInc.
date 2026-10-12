"""Undo only our two identifiable validation rows after fixing the isolated scene handoff."""
import hashlib,json,os,pathlib

ROOT=pathlib.Path(__file__).resolve().parents[2]
folder=pathlib.Path(os.environ['USERPROFILE'])/'AppData/LocalLow/DefaultCompany/WeedtopiaInc'
path=folder/'local_leaderboard.json'
before=path.read_bytes();data=json.loads(before)

def ours(entry):
    earned=entry.get('earned',{})
    if entry.get('cityId')!='big_rock_city' or entry.get('playSeconds')!=0:return False
    return (entry.get('player')=='Stadtchronik' and earned=={'mantissa':6.0,'exponent':2} and entry.get('freeplay') is True) or \
        (entry.get('player')=='Reisetest' and earned=={'mantissa':0.0,'exponent':0} and entry.get('freeplay') is False)

removed=[e for e in data['entries'] if ours(e)]
if not removed:print('No owned diagnostic entries remain.');raise SystemExit(0)
if len(removed)!=2:raise RuntimeError('Unexpected diagnostic rows; no write performed')
preserved=[e for e in data['entries'] if not ours(e)]
backup=ROOT/'Temp/BigRockCityLeaderboardCleanup';backup.mkdir(parents=True,exist_ok=True)
(backup/'leaderboard.before.json').write_bytes(before)
data['entries']=preserved
after=(json.dumps(data,ensure_ascii=False,indent=4)+'\n').encode('utf-8')
temporary=folder/'local_leaderboard.bigrock-cleanup.tmp'
if temporary.resolve().parent!=path.resolve().parent:raise RuntimeError('Wrong temporary path')
temporary.write_bytes(after)
if path.read_bytes()!=before:temporary.unlink();raise RuntimeError('Leaderboard changed concurrently; no replacement performed')
os.replace(temporary,path)
assert json.loads(path.read_bytes())['entries']==preserved
receipt={'status':'passed','removedOwnedDiagnosticRows':len(removed),'preservedPlayerEntries':len(preserved),
         'preservedEntriesSha256':hashlib.sha256(json.dumps(preserved,sort_keys=True).encode()).hexdigest(),
         'finalFileSha256':hashlib.sha256(path.read_bytes()).hexdigest(),'rollbackBackup':'Temp/BigRockCityLeaderboardCleanup/leaderboard.before.json',
         'realGameSaveEdited':False}
(ROOT/'Docs/BigRockCity/Preservation/score_isolation_receipt.json').write_text(json.dumps(receipt,indent=2),encoding='utf-8')
print(json.dumps(receipt,indent=2))
