"""Describe atlas slices without modifying a generated pixel; provenance and alpha QA."""
import hashlib,json
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
ART=ROOT/'Assets/Weedtopia/Production/Art'
entries=[]
def slices(file,names,cols,rows,boxes=None,border=0):
    im=Image.open(ART/file).convert('RGBA');w,h=im.size
    for i,name in enumerate(names):
        bounds=boxes[i] if boxes else (i%cols/cols,i//cols/rows,(i%cols+1)/cols,(i//cols+1)/rows)
        x0,y0,x1,y1=[int(v*s) for v,s in zip(bounds,(w,h,w,h))]
        # Inspection only: locate nontransparent content; the original image stays byte-identical.
        alpha=im.getchannel('A').crop((x0,y0,x1,y1))
        box=alpha.point(lambda a:255 if a>40 else 0).getbbox()
        if box is None:raise ValueError('Empty sprite '+name)
        a,b,c,d=box;a=max(x0,x0+a-2);b=max(y0,y0+b-2);c=min(x1,x0+c+2);d=min(y1,y0+d+2)
        entries.append(dict(file=file,name=name,x=a,y=h-d,width=c-a,height=d-b,border=border,ppu=100,pivotX=.5,pivotY=0 if file in ('Characters.png','Props.png') else .5))
        if file=='Characters.png':
            # Additional sprite region for a consistent head-and-shoulders portrait component.
            ph=int((d-b)*.55)
            entries.append(dict(file=file,name='portrait_'+name,x=a,y=h-b-ph,width=c-a,height=ph,border=0,ppu=100,pivotX=.5,pivotY=.5))
slices('Characters.png',['rocko','mila','jaro','nia','city_host','gardener','baker','driver'],4,2)
slices('Props.png',['pine','tree','flowers','planter','lantern','crates','salvage','barrels','pump','truck_left','truck_right','sign'],4,3,
       [(0,0,.25,.34),(.25,0,.5,.34),(.5,0,.75,.34),(.75,0,1,.37),(0,.34,.25,.67),(.25,.35,.5,.67),(.5,.35,.75,.67),(.75,.37,1,.67),(0,.67,.25,1),(.25,.67,.5,1),(.5,.67,.77,1),(.79,.66,1,1)])
icon_names='money grinder cards raw refined butter pastry residents quest farm city settlement industry save settings upgrade harvest work talk close confirm back lock star cargo truck time volume zoom play pause laurel gift person down warning'.split()
# Actual generated atlas gutters differ from an ideal six-by-six grid; use inspected row boundaries.
xs=[0,.173,.338,.507,.674,.837,1];ys=[0,.198,.36,.535,.684,.83,1]
slices('Icons.png',icon_names,6,6,[(xs[i%6],ys[i//6],xs[i%6+1],ys[i//6+1]) for i in range(36)])
slices('Skin.png',['panel','paper','button','primary','frame','banner'],2,3,[(0,0,.5,.36),(.5,0,1,.36),(0,.36,.5,.59),(.5,.36,1,.59),(0,.59,.47,1),(.46,.66,1,.88)],24)
(ROOT/'Assets/Weedtopia/Production/art_slices.json').write_text(json.dumps({'entries':entries},indent=2),encoding='utf-8')
facts=[]
for p in ART.glob('*.png'):
    im=Image.open(p);facts.append(dict(path=p.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),width=im.width,height=im.height,mode=im.mode,tool='built-in image_gen',pixelsEdited=False))
(ROOT/'Docs/ProductionFinish/art_provenance.json').write_text(json.dumps(facts,indent=2),encoding='utf-8')
print('Sprite regions:',len(entries))
