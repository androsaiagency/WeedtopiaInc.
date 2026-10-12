"""One-off, resumable OFFLINE translation. Connects only to the installed localhost Ollama service.
No downloader, remote fallback or runtime dependency. Human copy review remains separate.
"""
from pathlib import Path
import json,urllib.request,time,re,hashlib,sys
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/Localization'
CACHE=OUT/'local_translation_cache.json'
MODEL='llama3:latest'
LANGS=['en','es','fr','ru']
source=json.loads((OUT/'source_inventory.json').read_text(encoding='utf8'))
cache=json.loads(CACHE.read_text(encoding='utf8')) if CACHE.exists() else {}
unique=list(dict.fromkeys(source.values()))
# Reviewed overrides also seed identical sources, avoiding redundant generation.
manual_file=OUT/'manual_overrides.txt'
if manual_file.exists():
    for line in manual_file.read_text(encoding='utf-8-sig').splitlines():
        if not line.strip():continue
        key,*values=line.split('|')
        if key in source:cache[source[key]]={lang:value.replace('\\n','\n') for lang,value in zip(LANGS,values)}
failures={} 
placeholder=re.compile(r'\{\d+(?:,[^}:]+)?(?::[^}]+)?\}')
def valid(original,values):
    return isinstance(values,dict) and all(isinstance(values.get(lang),str) and values[lang].strip() and sorted(placeholder.findall(original))==sorted(placeholder.findall(values[lang])) and original.count('\n')==values[lang].count('\n') for lang in LANGS)
def save():CACHE.write_text(json.dumps(cache,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
def translate(batch,attempt=0):
    data={str(i):s.strip() for i,s in enumerate(batch)}
    prompt='You are a professional video game translator. Translate EVERY German value into English (en), Spanish (es), French (fr), Russian (ru). Use friendly informal singular speech. Return ONLY JSON: each numeric input key maps to {"en":"...","es":"...","fr":"...","ru":"..."}. Do not copy German into the translation. Preserve ALL numbered {0} placeholders, numbers, punctuation, symbols and exact newline count. Weedtopia is a city building game about cannabis production. Blüten means cannabis buds, not ornamental flowers. Grinder is the name of a game currency: preserve Grinder. Preserve proper names including Weedtopia, Burning Hills, Big Rock City, Rocko, Mila, Jaro, Nia, Theo, Leni, Boro, Suri, Berta Fels, Levin Quitt, Samira Kupfer, Enno Funke, Lotte Klang, Ida Stein. Worker means team member, not a computer process. Do not add any explanations. Input JSON:\n'+json.dumps(data,ensure_ascii=False)
    req=dict(model=MODEL,prompt=prompt,stream=False,format='json',keep_alive='30m',options=dict(temperature=0,num_ctx=2048,num_predict=1600,num_gpu=99))
    try:
        response=json.loads(urllib.request.urlopen(urllib.request.Request('http://localhost:11434/api/generate',data=json.dumps(req).encode(),headers={'Content-Type':'application/json'}),timeout=300).read())
        result=json.loads(response['response'])
        failed=[]
        for i,text in enumerate(batch):
            values=result.get(str(i))
            if valid(text.strip(),values):
                lead=text[:len(text)-len(text.lstrip())];trail=text[len(text.rstrip()):]
                cache[text]={lang:lead+values[lang].strip()+trail for lang in LANGS}
            else:failed.append(text)
        save()
        if failed:
            if attempt>=1:
                for text in failed:failures[text]=result
                (OUT/'translation_retry.json').write_text(json.dumps(failures,ensure_ascii=False,indent=2),encoding='utf8')
                print('DEFERRED_VALIDATION',len(failed),flush=True);return
            for text in failed:translate([text],attempt+1)
    except (TimeoutError,ValueError,urllib.error.URLError) as ex:
        if attempt>=1:
            failures.update({text:str(ex) for text in batch});(OUT/'translation_retry.json').write_text(json.dumps(failures,ensure_ascii=False,indent=2),encoding='utf8');return
        if len(batch)>1:
            middle=len(batch)//2;translate(batch[:middle],attempt+1);translate(batch[middle:],attempt+1)
        else:time.sleep(1);translate(batch,attempt+1)
    print(json.dumps(dict(translated=len(cache),unique=len(unique),model=MODEL,tokens=response.get("eval_count"),seconds=round(response.get("eval_duration",0)/1e9,1))),flush=True)
missing=[s for s in unique if not valid(s,cache.get(s))]
batch=[];length=0
for s in missing:
    if batch and (length+len(s)>650 or len(batch)>=8):translate(batch);batch=[];length=0
    batch.append(s);length+=len(s)
if batch:translate(batch)
print('LOCAL_TRANSLATION_COMPLETE',flush=True)
