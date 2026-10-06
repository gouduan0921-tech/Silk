# 工坊工位与环境（docs/17 §5）。位置按各工位 BuildProps 的灰盒对齐；会动的部件（蚕匾面、缸面、晾布、摊布、针、裁片）不做，留灰盒。
exec(open("/Users/liweng/Downloads/3D/Silk to Divinity/tools/blender/hs_art.py").read())
import random

def silk():
    clear(); W, Wl, Bm, P = WOOD(), WOOD_LIGHT(), BAMBOO(), POTTERY()
    # 蚕架：四根竹柱，三层竹匾（最上一层是灰盒的活动蚕匾）
    for x in (-0.75, 0.75):
        for z in (-0.4, 0.4):
            rod("Post", (x, 0, z), (x, 1.55, z), 0.025, Bm, 10)
    for y in (0.25, 0.6, 0.9):
        for z in (-0.4, 0.4):
            rod("Bar", (-0.78, y, z), (0.78, y, z), 0.018, Bm, 8)
        if y < 0.9:
            lathe("Tray", (0, y + 0.02, 0), [(0.0, 0), (0.6, 0), (0.62, 0.05), (0.6, 0.05), (0.0, 0.01)], Bm, 40)
    rod("TopBar", (-0.78, 1.55, 0), (0.78, 1.55, 0), 0.018, Bm, 8)
    # 缫丝：灶与盆、丝车
    box("Stove", (1.4, 0.2, -0.3), (0.7, 0.4, 0.7), STONE())
    lathe("Basin", (1.4, 0.4, -0.3), [(0.0, 0), (0.22, 0.0), (0.3, 0.12), (0.32, 0.2), (0.3, 0.2), (0.27, 0.13), (0.0, 0.05)], P, 32)
    cx, cy, cz = 1.4, 1.05, 0.25
    for z in (cz - 0.18, cz + 0.18):
        box("ReelPost", (cx - 0.3, 0.55, z), (0.05, 1.1, 0.05), W)
        box("ReelPost", (cx + 0.3, 0.55, z), (0.05, 1.1, 0.05), W)
    rod("ReelAxle", (cx, cy, cz - 0.22), (cx, cy, cz + 0.22), 0.02, Wl)
    for k in range(6):
        a = k / 6 * math.tau
        for z in (cz - 0.14, cz + 0.14):
            rod("Spoke", (cx, cy, z), (cx + math.cos(a) * 0.32, cy + math.sin(a) * 0.32, z), 0.01, Wl, 6)
        rod("ReelBar", (cx + math.cos(a) * 0.32, cy + math.sin(a) * 0.32, cz - 0.15), (cx + math.cos(a) * 0.32, cy + math.sin(a) * 0.32, cz + 0.15), 0.008, Wl, 6)
    # 桑叶篮
    box("Stool", (-1.2, 0.38, 0.2), (0.45, 0.06, 0.4), W)
    for x in (-1.38, -1.02):
        for z in (0.05, 0.35):
            box("StoolLeg", (x, 0.18, z), (0.04, 0.36, 0.04), W)
    lathe("Basket", (-1.2, 0.41, 0.2), [(0.0, 0), (0.16, 0), (0.24, 0.16), (0.22, 0.16), (0.14, 0.02), (0.0, 0.02)], Bm, 24)
    random.seed(3)
    for i in range(14):
        ellipsoid("Leaf", (-1.2 + random.uniform(-0.15, 0.15), 0.55 + random.uniform(0, 0.04), 0.2 + random.uniform(-0.12, 0.12)), (0.06, 0.012, 0.04), LEAF(), 8, 4)
    export("Props/SM_station_silk")

def dye():
    clear(); W, Wl, P = WOOD(), WOOD_LIGHT(), POTTERY()
    lathe("Vat", (0, 0, 0), [(0.0, 0.0), (0.5, 0.0), (0.62, 0.15), (0.66, 0.55), (0.64, 0.8), (0.68, 0.84), (0.66, 0.86), (0.58, 0.84), (0.58, 0.82), (0.0, 0.3)], P, 48)
    box("Base", (0, 0.03, 0), (1.5, 0.06, 1.5), STONE())
    for x in (-1.1, 1.1):
        box("Pole", (x, 0.95, 0.6), (0.07, 1.9, 0.07), W)
        box("PoleFoot", (x, 0.04, 0.6), (0.3, 0.08, 0.3), STONE())
    rod("DryingRod", (-1.2, 1.78, 0.6), (1.2, 1.78, 0.6), 0.03, Wl, 16)
    rod("Stirrer", (0.45, 0.7, -0.35), (0.25, 1.35, -0.1), 0.02, Wl, 8)
    # 染料架与陶罐
    box("ShelfTop", (1.6, 0.9, -0.2), (0.6, 0.04, 0.4), W)
    box("ShelfMid", (1.6, 0.45, -0.2), (0.6, 0.04, 0.4), W)
    for x in (1.33, 1.87):
        for z in (-0.37, -0.03):
            box("ShelfLeg", (x, 0.45, z), (0.04, 0.9, 0.04), W)
    cols = [POTTERY(), mat("M_lacquer_red", (0.45, 0.1, 0.07)), POTTERY()]
    for i, x in enumerate((1.45, 1.6, 1.75)):
        lathe("Jar", (x, 0.92, -0.2), [(0.0, 0), (0.05, 0), (0.07, 0.07), (0.05, 0.13), (0.04, 0.15), (0.0, 0.15)], cols[i], 16)
        lathe("Jar", (x, 0.47, -0.2), [(0.0, 0), (0.06, 0), (0.06, 0.1), (0.0, 0.1)], POTTERY(), 16)
    export("Props/SM_station_dye")

def cut():
    clear(); W, Wl = WOOD(), WOOD_LIGHT()
    box("Table", (0, 0.78, 0), (2.2, 0.06, 1.2), Wl)
    box("Apron", (0, 0.71, -0.56), (2.0, 0.08, 0.03), W)
    box("Apron", (0, 0.71, 0.56), (2.0, 0.08, 0.03), W)
    for x in (-1.0, 1.0):
        for z in (-0.5, 0.5):
            box("Leg", (x, 0.38, z), (0.07, 0.76, 0.07), W)
        box("Stretcher", (x, 0.15, 0), (0.05, 0.05, 1.0), W)
    # 剪刀、尺、划粉
    B_ = BRASS()
    rod("BladeA", (0.6, 0.825, 0.33), (0.82, 0.825, 0.37), 0.008, B_, 6)
    rod("BladeB", (0.6, 0.825, 0.37), (0.82, 0.825, 0.33), 0.008, B_, 6)
    lathe("Bow", (0.55, 0.815, 0.35), [(0.0, 0), (0.035, 0), (0.035, 0.012), (0.0, 0.012)], B_, 12)
    box("Ruler", (0.2, 0.82, 0.45), (0.9, 0.012, 0.04), WOOD_DARK())
    box("Chalk", (0.85, 0.825, 0.1), (0.06, 0.02, 0.04), PAPER())
    export("Props/SM_station_cut")

def sew():
    clear(); W, Wl = WOOD(), WOOD_LIGHT()
    box("Table", (0, 0.74, 0), (1.4, 0.05, 0.9), Wl)
    for x in (-0.62, 0.62):
        for z in (-0.38, 0.38):
            box("Leg", (x, 0.36, z), (0.06, 0.72, 0.06), W)
    lathe("Spool", (0.5, 0.765, 0.3), [(0.0, 0), (0.035, 0), (0.035, 0.008), (0.025, 0.01), (0.025, 0.05), (0.035, 0.052), (0.035, 0.06), (0.0, 0.06)], mat("M_lacquer_red", (0.45, 0.1, 0.07)), 16)
    lathe("Basket", (-0.5, 0.765, 0.28), [(0.0, 0), (0.12, 0), (0.14, 0.06), (0.13, 0.06), (0.11, 0.01), (0.0, 0.01)], BAMBOO(), 24)
    ellipsoid("PinCushion", (0.35, 0.79, 0.32), (0.05, 0.03, 0.05), mat("M_cloth_prop", (0.7, 0.6, 0.5)), 12, 8)
    box("Stool", (0, 0.42, -0.75), (0.5, 0.05, 0.35), W)
    for x in (-0.2, 0.2):
        for z in (-0.88, -0.62):
            box("StoolLeg", (x, 0.2, z), (0.04, 0.4, 0.04), W)
    export("Props/SM_station_sew")

def market():
    clear(); W, Wl, Wd = WOOD(), WOOD_LIGHT(), WOOD_DARK()
    box("Board", (0, 1.4, 0.3), (1.6, 1.1, 0.06), W)
    box("Frame", (0, 1.98, 0.3), (1.75, 0.08, 0.1), Wd)
    box("Frame", (0, 0.82, 0.3), (1.75, 0.08, 0.1), Wd)
    box("Roof", (0, 2.1, 0.25), (2.0, 0.05, 0.45), TILE(), rot=(-12, 0, 0))
    for x in (-0.85, 0.85):
        box("Post", (x, 1.05, 0.3), (0.09, 2.1, 0.09), Wd)
    for i in range(3):
        box("Note", (-0.5 + i * 0.5, 1.45, 0.265), (0.36, 0.5, 0.01), PAPER())
        box("Pin", (-0.5 + i * 0.5, 1.68, 0.255), (0.03, 0.03, 0.01), LACQUER())
    box("Stall", (1.4, 0.42, -0.2), (0.95, 0.06, 0.62), Wl)
    for x in (1.0, 1.8):
        for z in (-0.45, 0.05):
            box("StallLeg", (x, 0.2, z), (0.05, 0.4, 0.05), W)
    colors = [(0.75, 0.70, 0.62), (0.35, 0.42, 0.55), (0.62, 0.30, 0.25)]
    for i, c in enumerate(colors):
        rod("Bolt", (1.1, 0.5 + 0.0, -0.35 + i * 0.15), (1.7, 0.5, -0.35 + i * 0.15), 0.06, mat("M_cloth_prop", (0.78, 0.72, 0.62)), 16)
    export("Props/SM_station_market")

def workshop():
    clear(); W, Wd, Pl = WOOD(), WOOD_DARK(), PLASTER()
    box("Floor", (41, -0.05, 2), (102, 0.1, 14), W, bevel=0)
    box("BackWall", (41, 2.2, 3.25), (102, 4.4, 0.2), Pl, bevel=0)
    box("Skirting", (41, 0.15, 3.12), (102, 0.3, 0.06), Wd, bevel=0)
    box("WallBeam", (41, 3.6, 3.12), (102, 0.18, 0.08), Wd, bevel=0)
    for i in range(12):
        x = -3.5 + i * 8
        box("Column", (x, 2.2, 2.95), (0.24, 4.4, 0.24), Wd)
        lathe("ColumnBase", (x, 0, 2.95), [(0.0, 0), (0.22, 0), (0.2, 0.08), (0.16, 0.12), (0.0, 0.12)], STONE(), 16)
        box("Bracket", (x, 4.1, 2.85), (0.5, 0.14, 0.3), W)
        if i < 11:
            # 两柱之间一扇格子窗
            cx = x + 4
            box("WindowPaper", (cx, 2.3, 3.13), (1.7, 1.1, 0.01), PAPER(), bevel=0)
            box("WindowFrame", (cx, 2.88, 3.09), (1.8, 0.07, 0.07), Wd)
            box("WindowFrame", (cx, 1.72, 3.09), (1.8, 0.07, 0.07), Wd)
            box("WindowFrame", (cx - 0.9, 2.3, 3.09), (0.07, 1.2, 0.07), Wd)
            box("WindowFrame", (cx + 0.9, 2.3, 3.09), (0.07, 1.2, 0.07), Wd)
            for k in range(1, 6):
                box("Mullion", (cx - 0.85 + k * 1.7 / 6, 2.3, 3.08), (0.025, 1.1, 0.03), Wd, bevel=0)
            for k in range(1, 4):
                box("Transom", (cx, 1.75 + k * 1.1 / 4, 3.08), (1.7, 0.025, 0.03), Wd, bevel=0)
    export("Props/SM_workshop")

def form():
    clear(); W, Wd = WOOD(), WOOD_DARK()
    lathe("Plinth", (0, 0, 0), [(0.0, 0), (0.42, 0), (0.42, 0.06), (0.4, 0.08), (0.36, 0.1), (0.0, 0.1)], Wd, 48)
    lathe("Rim", (0, 0.098, 0), [(0.37, 0), (0.38, 0), (0.38, 0.006), (0.37, 0.006)], BRASS(), 48)
    # 透光对照的格子窗与叶影（在 transRig 的位置）
    ox, oz = 2.4, 0.4
    box("WindowSill", (ox, 0.45, oz + 0.3), (1.4, 0.06, 0.2), W)
    for i in range(7):
        box("LatticeV", (ox - 0.6 + i * 0.2, 1.2, oz + 0.3), (0.03, 1.4, 0.03), Wd)
        box("LatticeH", (ox, 0.55 + i * 0.2, oz + 0.3), (1.25, 0.03, 0.03), Wd)
    box("Frame", (ox, 1.92, oz + 0.3), (1.35, 0.06, 0.06), Wd)
    rod("Rod", (ox - 0.7, 1.85, oz), (ox + 0.7, 1.85, oz), 0.015, W, 12)
    for i in range(6):
        ellipsoid("Leaf", (ox - 0.5 + i * 0.2, 1.0 + (i % 2) * 0.35, oz + 0.36), (0.06, 0.11, 0.01), LEAF(), 10, 6)
    # 镜架与针插台
    box("MirrorFrame", (-1.1, 1.0, 0.3), (0.5, 1.6, 0.05), Wd)
    box("Mirror", (-1.1, 1.05, 0.27), (0.42, 1.4, 0.01), mat("M_glass_prop", (0.75, 0.8, 0.82)), bevel=0)
    box("MirrorFoot", (-1.1, 0.03, 0.3), (0.6, 0.06, 0.3), Wd)
    export("Props/SM_station_form")

def museum():
    clear(); W, Wd, L = WOOD(), WOOD_DARK(), LACQUER()
    box("Wall", (0, 1.8, 1.2), (4.0, 3.6, 0.1), PLASTER())
    box("WallPanel", (0, 1.6, 1.14), (3.2, 2.4, 0.02), mat("M_paper", (0.9, 0.87, 0.8)))
    box("WallFrame", (0, 2.82, 1.13), (3.3, 0.06, 0.03), Wd)
    box("WallFrame", (0, 0.38, 1.13), (3.3, 0.06, 0.03), Wd)
    box("CaseBase", (0, 0.3, 0), (2.6, 0.6, 1.2), Wd)
    for x in (-0.9, 0, 0.9):
        box("BasePanel", (x, 0.3, -0.61), (0.7, 0.4, 0.02), L)
    box("Plinth", (0, 0.61, 0), (2.62, 0.03, 1.22), W)
    # 玻璃罩的木框：十二条棱
    for x in (-1.3, 1.3):
        for z in (-0.6, 0.6):
            box("Mullion", (x, 1.45, z), (0.04, 1.7, 0.04), Wd)
    for y in (0.6, 2.3):
        for z in (-0.6, 0.6):
            box("Rail", (0, y, z), (2.64, 0.04, 0.04), Wd)
        for x in (-1.3, 1.3):
            box("Rail", (x, y, 0), (0.04, 0.04, 1.24), Wd)
    box("Cornice", (0, 2.36, -0.66), (2.74, 0.08, 0.06), Wd)  # 顶面留空（玻璃），只在正面加一道檐
    # 说明牌与吊灯
    box("Label", (0, 0.45, -0.62), (0.5, 0.14, 0.01), PAPER())
    lathe("Lamp", (0, 2.62, -0.6), [(0.0, 0), (0.08, 0.0), (0.06, 0.08), (0.02, 0.1), (0.0, 0.1)], BRASS(), 16)
    rod("LampCord", (0, 2.7, -0.6), (0, 3.6, -0.6), 0.005, INK(), 6)
    export("Props/SM_station_museum")

result = {}
import sys
only = globals().get("ONLY")
for fn in (silk, dye, cut, sew, market, workshop, form, museum):
    if only and fn.__name__ not in only: continue
    fn(); result[fn.__name__] = stats()
