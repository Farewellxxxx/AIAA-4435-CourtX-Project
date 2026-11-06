"""Rebuild the editable court artwork. Floor UV covers x=-9..9 and z=-16..2 metres."""
from pathlib import Path
from math import cos, sin, pi, asin
import numpy as np
from PIL import Image, ImageDraw, ImageFont

size = 2048
scale = size / 18
rng = np.random.default_rng(35)
wood = np.zeros((size, size, 3), dtype=np.uint8)
for x in range(0, size, 20):
    y = -int(rng.integers(10, 190))
    while y < size:
        end = y + int(rng.integers(130, 330))
        if end <= 0:
            y = end
            continue
        ys = slice(max(y, 0), min(end, size))
        width = min(20, size - x)
        shade = rng.normal(0, 4) + rng.normal(0, 1.5, (1, width, 1))
        grain = rng.normal(0, 1.3, (ys.stop - ys.start, width, 1))
        wood[ys, x:x+width] = np.clip(np.array([211, 173, 125]) + shade + grain, 0, 255)
        wood[ys, x] = wood[ys, x] * .92
        wood[ys.stop-1, x:x+width] = wood[ys.stop-1, x:x+width] * .94
        y = end
img = Image.fromarray(wood)
pen = ImageDraw.Draw(img, 'RGBA')
def uv(x, z): return ((x+9)*scale, (2-z)*scale)
def box(x1,z1,x2,z2,col): pen.rectangle([uv(x1,z1), uv(x2,z2)], fill=col)
def line(points, col, thick=.05):
    pen.line([uv(x,z) for x,z in points], fill=col, width=max(1,round(scale*thick)), joint="curve")
def arc(x,z,r,a,b,c):
    angles = np.linspace(a,b,160)
    line([(x+r*cos(t), z+r*sin(t)) for t in angles],c)
ink = (255,249,220,255)
deep = (21,71,74,240)
box(-9,2,9,0,deep); box(-9,-14,9,-16,deep)
box(-9,0,-7.5,-14,deep); box(7.5,0,9,-14,deep)
box(-2.45,0,2.45,-5.8,(18,102,105,185))
line([(-7.5,0),(7.5,0),(7.5,-14),(-7.5,-14),(-7.5,0)],ink)
line([(-2.45,0),(-2.45,-5.8),(2.45,-5.8),(2.45,0)],ink)
arc(0,-5.8,1.8,pi,2*pi,ink)
for i in range(8): arc(0,-5.8,1.8,i*pi/8,(i+.55)*pi/8,(255,249,220,210))
corner_x = 6.6
a = -np.arccos(corner_x/6.75)
corner_z = -1.575+6.75*sin(a)
line([(-corner_x,0),(-corner_x,corner_z)],ink)
arc(0,-1.575,6.75,-pi-a,a,ink)
line([(corner_x,corner_z),(corner_x,0)],ink)
arc(0,-1.575,1.25,pi,2*pi,ink)
for s in [-1,1]:
    line([(s*1.25,-1.575),(s*1.25,-1.2)],ink)
    for z in [-1.75,-2.65,-3.55,-4.45]:
        line([(s*2.45,z),(s*2.6,z)],ink)
arc(0,-14,1.8,0,pi,ink)
fontdir = Path("C:/Windows/Fonts")
def label(text,x,z,pixels,color,font="arialbd.ttf"):
    pen.text(uv(x,z),text,font=ImageFont.truetype(str(fontdir/font),pixels),fill=color,anchor="mm")
label("C O U R T X",0,.59,85,ink)
label("T A C T I C A L    L A B O R A T O R Y",0,-14.66,43,ink)
label("01",-6.8,-14.67,58,(238,158,76,255))
label("3  /  3",6.5,-14.65,45,ink)
img.save(Path(__file__).resolve().parents[1]/"Unity/Assets/CourtX/Art/CourtSurface.png")
