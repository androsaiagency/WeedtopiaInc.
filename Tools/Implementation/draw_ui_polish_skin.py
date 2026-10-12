"""Draw original, deterministic UI primitives; no existing artwork is edited."""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Weedtopia/Production/Art/UiPolish'
OUT.mkdir(parents=True, exist_ok=True)
S = 4

def surface(name, radius, top, bottom, circular=False):
    size = 128
    image = Image.new('RGBA', (size*S, size*S))
    draw = ImageDraw.Draw(image)
    def shape(inset, color, r):
        box = tuple(int(n*S) for n in (inset, inset, size-inset, size-inset))
        if circular: draw.ellipse(box, fill=color)
        else: draw.rounded_rectangle(box, radius=int(r*S), fill=color)
    shape(1, (4, 29, 29, 220), radius+2)
    shape(3, (151, 197, 156, 255), radius)
    shape(5, (220, 228, 182, 255), max(1,radius-2))
    mask = Image.new('L', image.size)
    md = ImageDraw.Draw(mask)
    box = (7*S,7*S,(size-7)*S,(size-7)*S)
    if circular: md.ellipse(box, fill=255)
    else: md.rounded_rectangle(box, radius=max(1,radius-4)*S, fill=255)
    gradient = Image.new('RGBA', image.size)
    gd = ImageDraw.Draw(gradient)
    for y in range(size*S):
        # The center of the nine-slice stays visually flat; subtle edge shading adds depth.
        t=max(0,min(1,y/(size*S-1)))
        color=tuple(round(a+(b-a)*t) for a,b in zip(top,bottom))+(255,)
        gd.line((0,y,size*S,y),fill=color)
    image.paste(gradient,mask=mask)
    image.resize((size,size),Image.Resampling.LANCZOS).save(OUT/(name+'.png'))

surface('Panel',16,(17,67,66),(12,50,54))
surface('Button',30,(35,110,99),(20,76,79))
surface('Primary',30,(116,173,86),(57,121,86))
surface('Field',14,(246,245,218),(221,231,202))
surface('Round',60,(35,110,99),(20,76,79),True)
print('Created five scalable original UI surfaces')
