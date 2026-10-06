# 戏台两套布景（docs/15 §5）。坐标在 StageStation 根下：台面顶 y=0.5，背景 z=1.6，台口朝 -z。
exec(open("/Users/liweng/Downloads/3D/Silk to Divinity/tools/blender/hs_art.py").read())
import random

def classic():
    clear(); W, Wd, L = WOOD(), WOOD_DARK(), LACQUER()
    box("Platform", (0, 0.25, 0), (5.0, 0.5, 3.4), W)
    box("Fascia", (0, 0.25, -1.71), (5.04, 0.42, 0.04), L)
    for k in range(6):
        box("FasciaPanel", (-2.1 + k * 0.84, 0.25, -1.735), (0.7, 0.28, 0.02), Wd)
    box("Step", (0, 0.12, -1.95), (1.2, 0.24, 0.4), W)
    # 四柱、额枋、雀替
    for x in (-2.4, 2.4):
        for z in (-1.5, 1.4):
            rod("Pillar", (x, 0.5, z), (x, 4.0, z), 0.11, L, 20)
            lathe("PillarBase", (x, 0.5, z), [(0.0, 0), (0.17, 0), (0.15, 0.1), (0.12, 0.14), (0.0, 0.14)], STONE(), 20)
    for z in (-1.5, 1.4):
        box("Lintel", (0, 3.95, z), (5.2, 0.25, 0.22), Wd)
        box("LintelPaint", (0, 3.95, z - 0.12 if z < 0 else z + 0.12), (4.6, 0.16, 0.01), mat("M_jade", (0.4, 0.6, 0.5)))
        for x in (-2.15, 2.15):
            box("Bracket", (x, 3.72, z), (0.5, 0.18, 0.12), Wd, rot=(0, 0, -20 if x < 0 else 20))
    for x in (-2.4, 2.4):
        box("SideBeam", (x, 3.95, -0.05), (0.2, 0.25, 3.1), Wd)
    # 屋檐：两坡，檐角起翘
    T = TILE()
    box("RoofFront", (0, 4.45, -0.85), (5.8, 0.08, 2.0), T, rot=(-24, 0, 0))
    box("RoofBack", (0, 4.45, 0.75), (5.8, 0.08, 2.0), T, rot=(24, 0, 0))
    box("Ridge", (0, 4.88, -0.05), (5.6, 0.14, 0.14), T)
    for x in (-2.95, 2.95):
        box("EaveTip", (x, 4.12, -1.75), (0.35, 0.08, 0.3), T, rot=(0, 0, 25 if x > 0 else -25))
    # 守旧：深色屏风，左右上下场门挂帘
    box("BackScreen", (0, 2.2, 1.6), (5.2, 3.4, 0.08), Wd)
    box("ScreenBorder", (0, 3.82, 1.55), (5.0, 0.12, 0.03), L)
    box("ScreenBorder", (0, 0.62, 1.55), (5.0, 0.12, 0.03), L)
    for x in (-1.7, 1.7):
        box("DoorFrame", (x, 1.55, 1.54), (0.95, 2.1, 0.04), L)
        box("Curtain", (x, 1.5, 1.52), (0.8, 1.9, 0.02), mat("M_curtain", (0.6, 0.2, 0.15)))
    box("Plaque", (0, 3.35, 1.53), (1.4, 0.45, 0.04), mat("M_gold", (0.8, 0.6, 0.3)))
    # 两侧栏杆
    for x in (-2.48, 2.48):
        box("RailTop", (x, 1.0, -0.05), (0.06, 0.06, 2.9), W)
        for k in range(7):
            box("Baluster", (x, 0.75, -1.4 + k * 0.45), (0.04, 0.5, 0.04), W)
    export("Props/SM_stage_classic")

def mountain(seed, width, height, n=40):
    random.seed(seed)
    pts = [(-width / 2, 0)]
    peaks = [(random.uniform(-0.45, 0.45) * width, random.uniform(0.5, 1.0) * height) for _ in range(3)]
    for i in range(n + 1):
        x = -width / 2 + i * width / n
        y = 0
        for (px, ph) in peaks:
            y = max(y, ph * max(0.0, 1 - abs(x - px) / (width * 0.32)) ** 1.4)
        y += random.uniform(-0.03, 0.03) * height
        pts.append((x, max(0.02, y)))
    pts.append((width / 2, 0))
    return pts

def ink():
    clear()
    box("Platform", (0, 0.25, 0), (5.0, 0.5, 3.4), STONE())
    box("PlatformEdge", (0, 0.49, -1.68), (5.0, 0.02, 0.06), INK())
    box("Backdrop", (0, 2.2, 1.64), (5.2, 3.6, 0.04), PAPER(), bevel=0)
    tones = [mat("M_ink_light", (0.72, 0.72, 0.70)), mat("M_ink_mid", (0.52, 0.53, 0.52)), INK()]
    for layer in range(3):
        for k in range(2 if layer < 2 else 3):
            w = 3.2 - layer * 0.5
            x = (-1.4 + k * 2.6) if layer < 2 else (-1.8 + k * 1.8)
            flat_shape("Mountain", (x, 0.5 + 0.35 - layer * 0.15, 1.6 - layer * 0.03), mountain(layer * 10 + k, w, 1.9 - layer * 0.45), 0.01, tones[layer])
    # 一枝松：远景角上的墨点
    for i in range(7):
        ellipsoid("PineDot", (2.0 + i * 0.07, 2.9 - abs(i - 3) * 0.06, 1.5), (0.12, 0.05, 0.01), INK(), 8, 4)
    rod("PineBranch", (2.5, 2.6, 1.5), (1.95, 2.95, 1.5), 0.015, INK(), 6)
    export("Props/SM_stage_ink")

result = {}
for fn in (classic, ink):
    fn(); result[fn.__name__] = stats()
