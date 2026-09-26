"""Apply the user's overview sketch to content identities and explicit editor layout data."""
from pathlib import Path
import json, math
ROOT=Path(__file__).resolve().parents[2]
CONTENT=ROOT/'Assets/Weedtopia/Game/Content/BurningHills.json'
data=json.loads(CONTENT.read_text(encoding='utf-8-sig'))
layout={'revision':1,'source':'Docs/LayoutRevision/user_layout.png','sites':[],'routes':[],'districts':[
 {'zone':0,'label':'Farm · Anbau nach rechts','start':{'x':300,'y':-45},'end':{'x':552,'y':-45},'direction':{'x':1,'y':0}},
 {'zone':1,'label':'Stadt · Shops nach unten','start':{'x':34,'y':-12},'end':{'x':34,'y':-214},'direction':{'x':0,'y':-1}},
 {'zone':2,'label':'Siedlung · Plätze nach oben','start':{'x':34,'y':96},'end':{'x':34,'y':356},'direction':{'x':0,'y':1}},
 {'zone':3,'label':'Industrie · Bäcker nach links','start':{'x':-94,'y':30},'end':{'x':-204,'y':30},'direction':{'x':-1,'y':0}}]}
original={i['id']:i for i in data['instances']}
sites=[]
def add(id,family,zone,u,v):
 i=original.get(id,{'id':id,'family':family,'zone':zone,'cityId':'burning_hills','introducedVersion':2})
 i.update(x=round((u+v)/math.sqrt(2),5),z=round((v-u)/math.sqrt(2),5))
 sites.append(i);layout['sites'].append({'id':id,'u':u,'v':v})
for n in range(1,9):add(f'farm_grow_{n:02}','cultivation_plot',0,300+(n-1)*36,-60)
add('farm_processing','farm_processing',0,250,0);add('farm_warehouse','farm_warehouse',0,250,-72)
add('city_warehouse','city_warehouse',1,62,0);add('town_hall','town_hall',1,0,0)
for n in range(1,13):add(f'coffee_{n:02}','coffee_shop',1,(n-1)%3*34,-58-(n-1)//3*52)
for n in range(1,19):add(f'settlement_{n:02}','settlement_plot',2,(n-1)%3*34,96+(n-1)//3*52)
add('butter_factory','butter_factory',3,-72,110);add('supermarket','supermarket',3,-72,-40)
for n in range(1,13):add(f'bakery_{n:02}','bakery',3,-128-(n-1)//4*38,110-(n-1)%4*52)
assert set(original).issubset({i['id'] for i in sites})
pos={s['id']:(s['u'],s['v']) for s in layout['sites']}
oldroutes={r['id']:r for r in data['routes']};routes=[]
def route(id,fr,to,resource,vehicle,points):
 points=[p for i,p in enumerate(points) if i==0 or p!=points[i-1]]
 length=sum(math.dist(a,b) for a,b in zip(points,points[1:]))
 # Distance is reflected in the deterministic trip time; preserve old in-flight save validity.
 seconds=max(oldroutes.get(id,{}).get('travelSeconds',6),math.ceil(length/14)) if vehicle else 6
 r={'id':id,'from':fr,'to':to,'resource':resource,'vehicle':vehicle,'pickupByRecipient':id.startswith('butter_bakery'),'travelSeconds':seconds,'capacity':8}
 routes.append(r);layout['routes'].append({'id':id,'points':[{'x':p[0],'y':p[1]} for p in points],'length':round(length,3),'travelSeconds':seconds})
for n in range(1,9):
 u,v=pos[f'farm_grow_{n:02}'];route(f'grow{n}_processing',f'farm_grow_{n:02}','farm_processing',0,False,[(u,v+7),(u,-7),(265,-7),(260,0)])
route('processing_farm','farm_processing','farm_warehouse',1,False,[(250,-10),(250,-59)])
route('farm_city','farm_warehouse','city_warehouse',1,True,[(236,-80),(214,-80),(170,-64),(140,-15),(108,-15),(78,-15),(72,-10)])
for n in range(1,13):
 u,v=pos[f'coffee_{n:02}'];route(f'city_coffee{n}','city_warehouse',f'coffee_{n:02}',1,True,[(72,-10),(108,-15),(108,v-18),(u,v-18),(u,v-8)])
route('city_butter','city_warehouse','butter_factory',1,True,[(72,-10),(108,-15),(108,50),(-38,50),(-38,94),(-72,98)])
for n in range(1,13):
 u,v=pos[f'bakery_{n:02}'];route(f'butter_bakery{n}','butter_factory',f'bakery_{n:02}',2,True,[(-72,98),(-108,98),(-108,v-15),(u,v-15),(u,v-7)])
data['version']=2;data['instances']=sites;data['routes']=routes
CONTENT.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
out=ROOT/'Assets/Weedtopia/Production/Layout';out.mkdir(parents=True,exist_ok=True)
(out/'BurningHillsLayout.json').write_text(json.dumps(layout,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(ROOT/'Docs/LayoutRevision/layout_contract.json').write_text(json.dumps({'sites':len(sites),'routes':len(routes),'preservedIds':list(original),'districts':layout['districts'],'farmCityTripSeconds':next(r['travelSeconds'] for r in routes if r['id']=='farm_city'),'simulationCurvesChanged':False},ensure_ascii=False,indent=2),encoding='utf-8')
print(len(sites),'sites,',len(routes),'routes; existing IDs preserved')
