from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

folder = Path(__file__).resolve().parents[1] / "Unity/Assets/CourtX/Art"
font = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 112)
for number in ("04", "05", "11", "12", "13"):
    image = Image.new("RGBA", (172, 128), (248, 245, 219, 0))
    draw = ImageDraw.Draw(image)
    draw.text((86, 64), number, font=font, anchor="mm", fill=(248, 245, 219, 255))
    image.save(folder / ("Jersey" + number + ".png"))
