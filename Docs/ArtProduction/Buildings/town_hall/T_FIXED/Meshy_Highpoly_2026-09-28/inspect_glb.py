import json
import struct
from pathlib import Path

root = Path(__file__).resolve().parent
source = root / 'Meshy_AI_Green_Industrial_Buil_0928195308_image-to-3d-texture.glb'
with source.open('rb') as f:
    magic, version, length = struct.unpack('<4sII', f.read(12))
    size, kind = struct.unpack('<II', f.read(8))
    doc = json.loads(f.read(size))
report = {'bytes': source.stat().st_size, 'header': [magic.decode(), version, length], 'meshes': [], 'materials': doc.get('materials', []), 'images': doc.get('images', []), 'nodes': doc.get('nodes', []), 'extensionsUsed': doc.get('extensionsUsed', [])}
for mesh in doc.get('meshes', []):
    for p in mesh['primitives']:
        report['meshes'].append({'name': mesh.get('name'), 'mode': p.get('mode', 4), 'material': p.get('material'), 'attributes': {k: doc['accessors'][v] for k, v in p['attributes'].items()}, 'indices': doc['accessors'][p['indices']] if 'indices' in p else None})
out = root / 'Optimization_v1'
out.mkdir(exist_ok=True)
(out / 'glb_source_inventory.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
