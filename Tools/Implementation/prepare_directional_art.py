"""Byte-copy reviewed generated atlases; author Unity slice/pivot metadata without changing image pixels."""
from pathlib import Path
from datetime import datetime, timezone
import hashlib, json, shutil
import numpy as np
from PIL import Image
from scipy.ndimage import label, find_objects

ROOT = Path(__file__).resolve().parents[2]
DOC = ROOT / 'Docs/DirectionalMotion'
ART = ROOT / 'Assets/Weedtopia/Production/DirectionalMotion/Art'
ART.mkdir(parents=True, exist_ok=True)
sources = json.loads((DOC / 'selected_art.json').read_text(encoding='utf-8'))
entries, provenance, qa = [], [], []
for source in sources:
    src = Path(source['source']); dest = ART / source['file']
    shutil.copyfile(src, dest)
    im = Image.open(dest); pixels = np.asarray(im)
    assert im.mode == 'RGBA' and (pixels[:, :, 3] == 0).mean() > .2, dest
    labels, count = label(pixels[:, :, 3] > 128)
    sizes = np.bincount(labels.ravel()); boxes = []
    for i, b in enumerate(find_objects(labels), 1):
        if sizes[i] > source.get('minimumComponent', 1000):
            boxes.append((b[1].start, b[0].start, b[1].stop, b[0].stop))
    expected = source['rows'] * 6
    assert len(boxes) == expected, (dest, len(boxes), expected)
    boxes.sort(key=lambda b: (b[1] + b[3]) / 2)
    for row in range(source['rows']):
        group = sorted(boxes[row * 6:(row + 1) * 6], key=lambda b: b[0])
        key = source['keys'][row] if source['rows'] == 2 else source['keys'][0]
        view = 0 if source['rows'] == 2 else row
        median_height = float(np.median([b[3] - b[1] for b in group]))
        baseline = float(np.median([b[3] for b in group]))
        scale = source['heights'][row] * 100 / median_height
        if 'widths' in source:
            scale = source['widths'][row] * 100 / float(np.median([b[2]-b[0] for b in group]))
        for frame, b in enumerate(group):
            x0, y0, x1, y1 = b
            if source.get('headAnchor', False):
                band = pixels[y0 + int((y1-y0)*.12):y0 + int((y1-y0)*.28), x0:x1, 3]
                _, xs = np.where(band > 128); anchor_x = x0 + float(np.mean(xs))
            else:
                anchor_x = (x0 + x1) * .5
            padding = 3
            x0, y0, x1, y1 = max(0,x0-padding), max(0,y0-padding), min(im.width,x1+padding), min(im.height,y1+padding)
            anchor_y = (y0+y1)*.5 if source['rows'] == 2 else baseline
            if 'anchorUpFraction' in source:
                anchor_y -= (y1-y0)*source['anchorUpFraction'][row]
            entry = dict(key=key, file=source['file'], row=view, frame=frame, name=f'{key}_r{view}_f{frame}',
                x=x0, y=im.height-y1, width=x1-x0, height=y1-y0,
                pivotX=(anchor_x-x0)/(x1-x0), pivotY=(y1-anchor_y)/(y1-y0), scale=scale)
            entries.append(entry)
        qa.append(dict(file=source['file'], row=row, key=key, componentBounds=group, medianHeight=median_height))
    digest = hashlib.sha256(dest.read_bytes()).hexdigest()
    # Bounding rectangles must not include an opaque fragment of another frame.
    local = [e for e in entries if e['file'] == source['file']]
    for i,e in enumerate(local):
        for f in local[i+1:]:
            x0,x1=max(e['x'],f['x']),min(e['x']+e['width'],f['x']+f['width'])
            y0,y1=max(e['y'],f['y']),min(e['y']+e['height'],f['y']+f['height'])
            if x1>x0 and y1>y0:
                assert not (pixels[im.height-y1:im.height-y0,x0:x1,3]>32).any(), (e['name'],f['name'],'opaque frame overlap')
    assert digest == hashlib.sha256(src.read_bytes()).hexdigest()
    provenance.append(dict(file=dest.relative_to(ROOT).as_posix(), source=str(src), sha256=digest,
        dimensions=list(im.size), method='Built-in ImageGen; byte-identical source copy; Unity importer slices, no pixel editing'))
(ART.parent / 'art_manifest.json').write_text(json.dumps(dict(sprites=entries),indent=2),encoding='utf-8')
(DOC / 'art_provenance.json').write_text(json.dumps(provenance,indent=2),encoding='utf-8')
(DOC / 'slice_evidence.json').write_text(json.dumps(dict(utc=datetime.now(timezone.utc).isoformat(),rows=qa,sprites=len(entries)),indent=2),encoding='utf-8')
print(json.dumps(dict(atlases=len(sources),sprites=len(entries)),indent=2))
