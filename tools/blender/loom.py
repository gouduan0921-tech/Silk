# 织机四台（docs/07 §1）：平纹机、缎机、花楼机、罗机。斜织机的骨架，按 LoomStation 灰盒的位置对齐：
# 胸梁 (0,0.9,-0.55)、经轴 (0,1.6,0.55)、经面中心 (0,1.25,0) 斜 31°；会动的梭 y=1.0 z=-0.25、布 (0,0.93,-0.42) 不做。
exec(open("/Users/liweng/Downloads/3D/Silk to Divinity/tools/blender/hs_art.py").read())

def build_loom(kind):
    clear()
    W, Wl, Wd, L, T = WOOD(), WOOD_LIGHT(), WOOD_DARK(), LACQUER(), THREAD()
    # 机座：两侧长枋与四足
    for x in (-0.78, 0.78):
        box("Rail", (x, 0.12, 0), (0.08, 0.08, 1.5), Wd)
        box("SideLow", (x, 0.5, -0.55), (0.07, 0.8, 0.07), W)     # 前柱
        box("SideHigh", (x, 0.85, 0.55), (0.07, 1.5, 0.07), W)    # 后柱
        rod("Slant", (x, 0.88, -0.6), (x, 1.62, 0.6), 0.032, Wl)  # 斜机身
        box("Brace", (x, 0.42, 0), (0.06, 0.06, 1.1), W)
    box("Cross", (0, 0.12, 0.65), (1.62, 0.07, 0.07), Wd)
    box("Cross", (0, 0.12, -0.65), (1.62, 0.07, 0.07), Wd)
    # 胸梁与经轴（滚筒）
    rod("BreastBeam", (-0.82, 0.9, -0.55), (0.82, 0.9, -0.55), 0.045, Wl, 24)
    rod("WarpBeam", (-0.82, 1.6, 0.55), (0.82, 1.6, 0.55), 0.065, Wl, 24)
    for x in (-0.86, 0.86):
        lathe("BeamEnd", (x, 1.6, 0.55), [(0.0, -0.02), (0.09, -0.02), (0.09, 0.02), (0.0, 0.02)], Wd, 6)
    # 经线：从胸梁到经轴，排成一片
    n = 44
    for i in range(n):
        x = -0.6 + i * 1.2 / (n - 1)
        rod("Warp", (x, 0.93, -0.53), (x, 1.56, 0.52), 0.0025, T, 4, cap=False)
    # 筘框（打纬）
    rod("Reed", (-0.7, 1.05, -0.32), (0.7, 1.05, -0.32), 0.018, W)
    rod("Reed", (-0.7, 1.32, -0.18), (0.7, 1.32, -0.18), 0.018, W)
    for x in (-0.7, 0.7):
        rod("ReedSide", (x, 1.05, -0.32), (x, 1.32, -0.18), 0.016, W)
    # 综框：平纹 2 片，缎机 5 片
    frames = 5 if kind == "satin" else 2
    for k in range(frames):
        z = 0.02 + k * 0.07
        rod("HeddleTop", (-0.68, 1.5 + k * 0.02, z), (0.68, 1.5 + k * 0.02, z), 0.012, Wl)
        rod("HeddleBot", (-0.68, 1.2 + k * 0.02, z), (0.68, 1.2 + k * 0.02, z), 0.012, Wl)
    # 马头：机顶的一对摆杆，牵综
    for x in (-0.62, 0.62):
        box("HorseHead", (x, 1.82, 0.12), (0.05, 0.08, 0.5), L, rot=(18, 0, 0))
        rod("HorseCord", (x, 1.78, -0.05), (x, 1.5, 0.05), 0.004, T, 4, cap=False)
    rod("HorseAxle", (-0.8, 1.74, 0.2), (0.8, 1.74, 0.2), 0.02, Wd)
    # 踏板
    pedals = 5 if kind == "satin" else 2
    for k in range(pedals):
        x = -0.25 + k * (0.5 / max(1, pedals - 1))
        box("Treadle", (x, 0.1, -0.35), (0.07, 0.035, 0.75), W, rot=(-6, 0, 0))
    # 坐凳
    box("BenchTop", (0, 0.5, -1.1), (1.0, 0.06, 0.34), Wl)
    for x in (-0.42, 0.42):
        for z in (-1.22, -0.98):
            box("BenchLeg", (x, 0.24, z), (0.05, 0.48, 0.05), W)
    if kind == "draw":
        # 花楼：机身上加楼，拽花的人坐在楼上，衢线垂下
        for x in (-0.7, 0.7):
            for z in (0.05, 0.75):
                box("TowerPost", (x, 2.2, z), (0.08, 1.5, 0.08), W)
        box("TowerFloor", (0, 2.85, 0.4), (1.55, 0.06, 0.85), Wl)
        for x in (-0.72, 0.72):
            box("Rail", (x, 3.1, 0.4), (0.04, 0.04, 0.85), W)
            for z in (0.05, 0.4, 0.75):
                box("Baluster", (x, 2.98, z), (0.03, 0.22, 0.03), W)
        box("DrawSeat", (0, 3.0, 0.68), (0.5, 0.06, 0.28), L)
        box("PatternFrame", (0, 3.15, 0.25), (1.2, 0.5, 0.04), Wd)
        for i in range(30):
            x = -0.58 + i * 1.16 / 29
            rod("DrawCord", (x, 2.83, 0.3), (x, 1.55, 0.18), 0.003, T, 4, cap=False)
    if kind == "leno":
        # 罗机：绞综，成对细杆交叉
        for i in range(14):
            x = -0.62 + i * 1.24 / 13
            rod("Doup", (x - 0.03, 1.12, -0.05), (x + 0.03, 1.42, -0.05), 0.005, Wl, 6)
            rod("Doup", (x + 0.03, 1.12, -0.05), (x - 0.03, 1.42, -0.05), 0.005, Wl, 6)
        rod("DoupBar", (-0.7, 1.44, -0.05), (0.7, 1.44, -0.05), 0.016, Wd)
        rod("DoupBar", (-0.7, 1.1, -0.05), (0.7, 1.1, -0.05), 0.016, Wd)

out = {}
for kind, key in [("plain", "station_loom"), ("satin", "station_loom_satin"), ("draw", "station_loom_draw"), ("leno", "station_loom_leno")]:
    build_loom(kind)
    st = stats()
    export("Props/SM_" + key)
    out[key] = st
result = out
