"""Register an owned capture route without changing existing evidence folders."""
from pathlib import Path

p=Path('Tools/Implementation/Run-UnityValidation.ps1')
s=p.read_text(encoding='utf-8-sig')
if "'InventoryWheelDockCapture'" not in s:
    s=s.replace("'MenuLayoutCapture'", "'MenuLayoutCapture','InventoryWheelDockCapture'")
    s=s.replace("'MenuLayoutCapture','InventoryWheelDockCapture' {", "'InventoryWheelDockCapture' {'Weedtopia.Editor.GameSetup.InteractionVisualEvidence.BeginInventoryWheelDock'}\n        'MenuLayoutCapture' {")
    p.write_text(s,encoding='utf-8')
