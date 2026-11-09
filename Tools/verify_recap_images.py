"""Check visible court geometry in real player captures at two window sizes."""
from pathlib import Path
from PIL import Image
import json

root = Path(__file__).resolve().parents[1]
folder = root / "Preview/Playtests/Recap-20261110"
results = []
for name, scale in (("recap-first-pass.png", 1), ("recap-small-no-pass.png", .8)):
    image = Image.open(folder / name).convert("RGB")
    x = round(532 * scale)
    rows = range(round(160 * scale), round(526 * scale))
    visible = 0
    for y in rows:
        visible += any((lambda c: c[0] > 85 and c[1] > 110 and c[2] > 110)(image.getpixel((xx, y))) for xx in range(x - 2, x + 3))
    fraction = visible / len(rows)
    results.append({"image": name, "rightSidelineVisibleFraction": fraction, "passed": fraction > .9})
print(json.dumps(results, indent=2))
raise SystemExit(0 if all(r["passed"] for r in results) else 1)
