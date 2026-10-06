# 角色外形（docs/16、docs/17 §2）：共用一具体型（与灰盒胶囊同位，布料碰撞仍用胶囊），按角色换发式与饰物。
# 角色在本地朝 Unity +Z（与灰盒的手臂前伸一致），舞台上整体转 180° 面向观众。静态网格，转身由时间轴转根节点。
exec(open("/Users/liweng/Downloads/3D/Silk to Divinity/tools/blender/hs_art.py").read())

def body_mesh(name, height_scale=1.0):
    """皮肤修改器做的修长人体，求值后转成普通网格。"""
    S = height_scale
    J = {
        "pelvis": ((0, 0.95, 0), 0.13), "waist": ((0, 1.12, 0), 0.095), "chest": ((0, 1.3, 0), 0.115),
        "neck": ((0, 1.46, 0), 0.04), "neckTop": ((0, 1.5, 0.005), 0.038),
    }
    edges = [("pelvis", "waist"), ("waist", "chest"), ("chest", "neck"), ("neck", "neckTop")]
    for side, tag in ((-1, "L"), (1, "R")):
        J["sh" + tag] = ((side * 0.16, 1.4, 0), 0.05)
        J["el" + tag] = ((side * 0.27, 1.13, 0.02), 0.034)
        J["wr" + tag] = ((side * 0.17, 0.93, 0.17), 0.026)
        J["hd" + tag] = ((side * 0.15, 0.87, 0.22), 0.02)
        J["hip" + tag] = ((side * 0.08, 0.9, 0), 0.075)
        J["kn" + tag] = ((side * 0.08, 0.5, 0.01), 0.048)
        J["an" + tag] = ((side * 0.075, 0.09, 0), 0.032)
        J["to" + tag] = ((side * 0.075, 0.03, 0.11), 0.028)
        edges += [("chest", "sh" + tag), ("sh" + tag, "el" + tag), ("el" + tag, "wr" + tag), ("wr" + tag, "hd" + tag),
                  ("pelvis", "hip" + tag), ("hip" + tag, "kn" + tag), ("kn" + tag, "an" + tag), ("an" + tag, "to" + tag)]
    keys = list(J.keys())
    me = bpy.data.meshes.new(name + "_skel")
    me.from_pydata([B((p[0], p[1] * S, p[2])) for (p, r) in (J[k] for k in keys)], [(keys.index(a), keys.index(b)) for a, b in edges], [])
    ob = bpy.data.objects.new(name + "_skel", me)
    scene().collection.objects.link(ob)
    mod = ob.modifiers.new("Skin", "SKIN")
    mod.use_smooth_shade = True
    for i, k in enumerate(keys):
        r = J[k][1]
        ob.data.skin_vertices[0].data[i].radius = (r, r)
    ob.data.skin_vertices[0].data[keys.index("pelvis")].use_root = True
    sub = ob.modifiers.new("Sub", "SUBSURF"); sub.levels = 2
    dg = bpy.context.evaluated_depsgraph_get()
    ev = ob.evaluated_get(dg)
    m2 = bpy.data.meshes.new_from_object(ev)
    bpy.data.objects.remove(ob, do_unlink=True)
    body = bpy.data.objects.new(name, m2)
    scene().collection.objects.link(body)
    m2.materials.clear(); m2.materials.append(SKIN())
    for p in m2.polygons: p.use_smooth = True
    return body

def head(style):
    s = style
    ellipsoid("Head", (0, 1.57, 0.01), (0.072, 0.095, 0.082), SKIN(), 24, 16)
    # 五官：极简人偶式，只点眉眼与唇
    for x in (-0.026, 0.026):
        ellipsoid("Eye", (x, 1.58, 0.083), (0.011, 0.005, 0.004), INK(), 10, 6)
        box("Brow", (x, 1.605, 0.082), (0.022, 0.003, 0.004), INK(), bevel=0, rot=(0, 0, 8 if x > 0 else -8))
    ellipsoid("Lips", (0, 1.525, 0.083), (0.012, 0.005, 0.004), mat("M_lacquer_red", (0.5, 0.12, 0.08)), 10, 6)
    H = mat(s.get("hairMat", "M_hair"), s.get("hair", (0.05, 0.04, 0.04)))
    # 发盖住头顶与后脑
    ellipsoid("HairCap", (0, 1.6, -0.005), (0.078, 0.085, 0.085), H, 24, 14)
    kind = s.get("bun", "high")
    if kind == "high":      # 高髻
        ellipsoid("Bun", (0, 1.72, -0.02), (0.05, 0.06, 0.05), H, 16, 10)
    elif kind == "double":  # 双髻
        for x in (-0.06, 0.06): ellipsoid("Bun", (x, 1.69, -0.01), (0.04, 0.045, 0.04), H, 14, 10)
    elif kind == "low":     # 低垂髻
        ellipsoid("Bun", (0, 1.53, -0.09), (0.05, 0.05, 0.045), H, 16, 10)
    elif kind == "tall":    # 高耸的峨髻
        ellipsoid("Bun", (0, 1.76, -0.01), (0.045, 0.09, 0.04), H, 16, 10)
        ellipsoid("Bun2", (0, 1.69, -0.04), (0.06, 0.04, 0.05), H, 16, 10)
    elif kind == "long":    # 披发（篇章与跨文化）
        box("HairBack", (0, 1.42, -0.07), (0.15, 0.32, 0.04), H, bevel=0.015)
    elif kind == "greek":   # 希腊式低髻加发带
        ellipsoid("Bun", (0, 1.56, -0.1), (0.055, 0.045, 0.045), H, 16, 10)
    elif kind == "heian":   # 平安长发，垂到腰下
        box("HairBack", (0, 1.2, -0.08), (0.16, 0.75, 0.035), H, bevel=0.015)
    orn = s.get("ornament")
    G = mat("M_gold", (0.8, 0.6, 0.3))
    if orn == "pins":
        for x in (-0.05, 0.05): rod("Pin", (x, 1.66, -0.02), (x * 1.8, 1.74, 0.02), 0.004, G, 6)
    elif orn == "buyao":    # 步摇：金钗垂珠
        rod("Pin", (0.04, 1.7, -0.02), (0.1, 1.72, 0.03), 0.004, G, 6)
        for k in range(3): ellipsoid("Bead", (0.1 + k * 0.006, 1.68 - k * 0.025, 0.03), (0.007, 0.007, 0.007), mat("M_jade", (0.5, 0.7, 0.6)), 8, 6)
    elif orn == "flower":
        for k in range(5):
            a = k / 5 * math.tau
            ellipsoid("Petal", (0.07 + math.cos(a) * 0.018, 1.67 + math.sin(a) * 0.018, 0.02), (0.014, 0.014, 0.006), mat("M_lacquer_red", (0.5, 0.12, 0.08)), 8, 6)
    elif orn == "crown":
        lathe("Crown", (0, 1.66, -0.005), [(0.075, 0), (0.08, 0.03), (0.07, 0.035), (0.068, 0.0)], G, 24)
    elif orn == "band":
        lathe("Band", (0, 1.63, -0.005), [(0.081, 0), (0.081, 0.012), (0.079, 0.012), (0.079, 0.0)], G, 24)
    elif orn == "moon":
        lathe("MoonDisc", (0, 1.75, -0.08), [(0.0, 0), (0.05, 0), (0.05, 0.006), (0.0, 0.006)], mat("M_jade", (0.8, 0.85, 0.9)), 24)
    elif orn == "snake":
        rod("Pin", (-0.05, 1.7, -0.02), (0.05, 1.72, -0.02), 0.006, mat("M_jade", (0.9, 0.92, 0.9)), 8)

STYLES = {
    "xiShi": {"bun": "high", "ornament": "pins"},
    "wangZhaoJun": {"bun": "low", "ornament": "pins"},
    "zhaoFeiYan": {"bun": "double", "ornament": "flower"},
    "liQingZhao": {"bun": "low", "ornament": None},
    "yangGuiFei": {"bun": "tall", "ornament": "buyao"},
    "diaoChan": {"bun": "high", "ornament": "flower"},
    "yuJi": {"bun": "high", "ornament": "pins"},
    "liShiShi": {"bun": "double", "ornament": "buyao"},
    "hongFu": {"bun": "low", "ornament": None},
    "chenYuanYuan": {"bun": "tall", "ornament": "flower"},
    "liuRuShi": {"bun": "low", "ornament": "pins"},
    "luoShen": {"bun": "tall", "ornament": "buyao"},
    "changE": {"bun": "double", "ornament": "moon"},
    "baiSuZhen": {"bun": "high", "ornament": "snake"},
    "daJi": {"bun": "tall", "ornament": "crown"},
    "haiLun": {"bun": "greek", "ornament": "band", "hair": (0.32, 0.2, 0.1), "hairMat": "M_hair_brown"},
    "aFuLuoDiTe": {"bun": "long", "ornament": "band", "hair": (0.45, 0.32, 0.18), "hairMat": "M_hair_light"},
    "keLiAoPaTeLa": {"bun": "long", "ornament": "band"},
    "xiaoYeXiaoTing": {"bun": "heian", "ornament": None},
}

def build(cid):
    clear()
    body_mesh("Body")
    head(STYLES[cid])
    export("Characters/SK_" + cid)

only = globals().get("ONLY")
result = {}
for cid in STYLES:
    if only and cid not in only: continue
    build(cid); result[cid] = stats()
