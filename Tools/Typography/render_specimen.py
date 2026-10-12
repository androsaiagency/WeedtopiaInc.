"""Render real TTF glyphs at display and UI sizes, never raster substitutes."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
FONTS = ROOT / "Assets/Weedtopia/Production/Fonts/WeedtopiaGrove"
DOCS = ROOT / "Docs/Typography"
SCALE = 2


def render():
    image = Image.new("RGB", (1680 * SCALE, 1480 * SCALE), "#f6f1e5")
    d = ImageDraw.Draw(image)

    def font(size, bold=False, original=False):
        path = (FONTS.parent / ("Lato-Bold.ttf" if bold else "Lato-Regular.ttf")) if original else FONTS / ("WeedtopiaGrove-Bold.ttf" if bold else "WeedtopiaGrove-Regular.ttf")
        return ImageFont.truetype(str(path), size * SCALE)

    def text(x, y, value, size=28, bold=False, color="#234a3c", original=False):
        d.text((x * SCALE, y * SCALE), value, font=font(size, bold, original), fill=color)

    def line(y):
        d.line((56 * SCALE, y * SCALE, 1624 * SCALE, y * SCALE), fill="#ddd6c4", width=2 * SCALE)

    d.rectangle((0, 0, 1680 * SCALE, 275 * SCALE), fill="#234a3c")
    text(56, 27, "WEEDTOPIA GROVE", 22, True, "#c4d4aa")
    text(49, 64, "Weedtopia", 132, True, "#f6f1e5")
    text(59, 229, "Regular + Bold  /  Latein + Kyrillisch  /  2.169 Unicode-Zeichen", 23, False, "#c4d4aa")
    text(56, 304, "Fünf Sprachen, eine Schriftfamilie", 34, True)
    samples = [
        ("Deutsch", "Grüne Gärten, große Pläne. Willkommen in Weedtopia!"),
        ("English", "Green gardens, big dreams. Welcome to Weedtopia!"),
        ("Español", "Jardines verdes, grandes sueños. ¡Bienvenido a Weedtopia!"),
        ("Français", "Jardins verdoyants, grands rêves. Bienvenue à Weedtopia !"),
        ("Русский", "Зелёные сады, большие мечты. Добро пожаловать в Weedtopia!"),
    ]
    for i, (language, sentence) in enumerate(samples):
        y = 365 + i * 83
        text(57, y, language, 29, True)
        text(285, y, sentence, 30)
    line(787)
    text(56, 811, "Eigene Buchstabenformen", 31, True)
    text(56, 857, "a g а i j  /  Aa Ёё Йй  /  ✓ ✔ ▣ ◆ ₽", 69, True)
    text(56, 948, "Akzente & Kyrillisch", 25, True)
    text(350, 948, "Ää Öö Üü ß ẞ  Éé Èè Êê Ëë  Çç Œœ  Ññ ¿¡  Ёё Йй Ыы Ъъ Ьь", 28)
    text(56, 1000, "Regular", 25, True)
    text(350, 1000, "ABCDEFGHIJKLMNOPQRSTUVWXYZ  abcdefghijklmnopqrstuvwxyz", 28)
    text(350, 1040, "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ", 29)
    text(350, 1080, "абвгдеёжзийклмнопрстуфхцчшщъыьэюя", 29)
    line(1140)
    text(56, 1164, "Zahlen & Oberfläche", 31, True)
    text(57, 1216, "0123456789  1.250,50 €  /  1,250.50 €  /  1 250,50 ₽", 35, True)
    for i, size in enumerate((16, 20, 24)):
        text(57, 1283 + i * 41, f"{size} px", 17, True)
        text(173, 1283 + i * 41, "Einstellungen · Settings · Ajustes · Paramètres · Настройки · ✓", size)
    text(56, 1432, "Eigene Weedtopia-Konturen, abgeleitet von Lato. SIL Open Font License 1.1.", 19, False, "#687567")
    DOCS.mkdir(parents=True, exist_ok=True)
    image.resize((1680, 1480), Image.Resampling.LANCZOS).save(DOCS / "WeedtopiaGrove-Specimen.png")

    comparison = Image.new("RGB", (1500 * SCALE, 440 * SCALE), "#f6f1e5")
    d = ImageDraw.Draw(comparison)
    text(36, 15, "Ausgangsschrift Lato", 24, True, "#687567")
    text(36, 62, "Weedtopia ag аi Ёё", 85, True, original=True)
    text(36, 218, "Weedtopia Grove", 24, True)
    text(36, 265, "Weedtopia ag аi Ёё", 85, True)
    comparison.resize((1500, 440), Image.Resampling.LANCZOS).save(DOCS / "Outline-Comparison.png")


if __name__ == "__main__":
    render()
    print("Rendered real font specimen and outline comparison in Docs/Typography")
