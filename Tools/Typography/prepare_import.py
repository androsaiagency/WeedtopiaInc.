"""Stable Unity metadata. Subsequent builds never replace existing GUIDs."""
from pathlib import Path
import uuid

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / "Assets/Weedtopia/Production/Fonts/WeedtopiaGrove"


def write_meta(path, contents):
    target = Path(str(path) + ".meta")
    if not target.exists():
        target.write_text(f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\n" + contents, encoding="utf8")


write_meta(FOLDER, "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
for path in FOLDER.glob("*.ttf"):
    template = (FOLDER.parent / "Lato-Regular.ttf.meta").read_text(encoding="utf8").split("TrueTypeFontImporter:")[1]
    template = template.replace("  - Lato", "  - Weedtopia Grove")
    write_meta(path, "TrueTypeFontImporter:" + template)
write_meta(FOLDER / "OFL.txt", "TextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
for path in (ROOT / "Assets/Weedtopia/Scripts/Editor/GameSetup/TypographyAuthoring.cs",
             ROOT / "Assets/Weedtopia/Scripts/Tests/EditorAssets/TypographyAssetTests.cs"):
    if path.exists():
        write_meta(path, "MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
print("Unity font/script metadata ready; existing GUIDs preserved")
