from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

folder = Path(__file__).resolve().parents[1] / "Unity/Assets/CourtX/Art/FiveNumbers"
folder.mkdir(exist_ok=True)
font = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 168)
for number in [f"0{i}" for i in range(1, 6)] + [f"X{i}" for i in range(1, 6)]:
    image = Image.new("RGBA", (344, 256), (248, 245, 219, 0))
    ImageDraw.Draw(image).text((172, 128), number, font=font, anchor="mm", fill=(248, 245, 219, 255))
    image.save(folder / f"{number}.png")
