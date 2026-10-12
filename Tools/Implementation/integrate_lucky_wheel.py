"""One-time integration into existing diagnostics, with stable Unity script GUIDs."""
from pathlib import Path
import uuid

p=Path('Tools/Implementation/Run-UnityValidation.ps1')
s=p.read_text(encoding='utf-8-sig')
s=s.replace("'InventoryCapture'", "'InventoryCapture','LuckyWheelCapture'")
s=s.replace("'Inventory','InventoryCapture','LuckyWheelCapture')][string]", "'Inventory','InventoryCapture','LuckyWheel','LuckyWheelCapture')][string]")
s=s.replace("'InventoryCapture','LuckyWheelCapture' {", "'LuckyWheel' {'Weedtopia.Editor.GameSetup.LuckyWheelAuthoring.Apply'}\n        'LuckyWheelCapture' {'Weedtopia.Editor.GameSetup.InteractionVisualEvidence.BeginLuckyWheel'}\n        'InventoryCapture' {")
p.write_text(s,encoding='utf-8')
p=Path('Assets/Weedtopia/Scripts/Editor/GameSetup/InteractionVisualEvidence.cs')
s=p.read_text(encoding='utf-8-sig')
s=s.replace('        public static void BeginInventory()', '        public static void BeginLuckyWheel()\n        {BeginAt("Docs/LuckyWheel");}\n        public static void BeginInventory()')
s=s.replace('EvidenceRoot=="Docs/InventoryRevision")root.Refresh()', 'EvidenceRoot=="Docs/InventoryRevision"||EvidenceRoot=="Docs/LuckyWheel")root.Refresh()')
s=s.replace('            if(EvidenceRoot=="Docs/InventoryRevision"){steps=', '            if(EvidenceRoot=="Docs/LuckyWheel"){steps=LuckyWheelEvidence.Prepare(root,clock,Capture,SetSize);return;}\n            if(EvidenceRoot=="Docs/InventoryRevision"){steps=')
s=s.replace('name.StartsWith("motion_")?.02:1.5','name.Contains("wheel_win")?5:name.StartsWith("motion_")?.02:1.5')
p.write_text(s,encoding='utf-8')
for p in Path('Assets/Weedtopia/Scripts').rglob('*Wheel*.cs'):
    meta=Path(str(p)+'.meta')
    if not meta.exists():
        meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf-8')
