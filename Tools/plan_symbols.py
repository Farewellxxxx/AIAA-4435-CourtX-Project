"""Bake overhead-only role symbols and a heading arrow on a transparent atlas."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

out = Path(__file__).resolve().parents[1] / "Unity/Assets/CourtX/Art/Plan"
out.mkdir(exist_ok=True)
paper, ink = "#fff9e8", "#14333b"
for label in ("YOU", "04", "05", "11", "12", "13", "Heading"):
    img = Image.new("RGBA", (256, 256))
    draw = ImageDraw.Draw(img)
    if label == "Heading":
        draw.polygon([(128, 7), (187, 105), (128, 83), (69, 105)], fill=ink, outline=paper, width=11)
    else:
        offense = label in ("YOU", "04", "05")
        if offense:
            draw.ellipse((7, 7, 249, 249), fill=ink, outline=paper, width=8)
            draw.ellipse((19, 19, 237, 237), fill="#006663", outline="#63bbb0", width=5)
        else:
            draw.rounded_rectangle((12, 12, 244, 244), radius=38, fill="#a83c20", outline=paper, width=9)
        font = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 77 if label == "YOU" else 119)
        draw.text((128, 125), label, font=font, anchor="mm", fill=paper, stroke_width=1)
    img.save(out / (label + ".png"))
