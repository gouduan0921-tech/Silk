# 角色外形（docs/16、docs/17 §2）：共用一具体型（与灰盒胶囊同位，布料碰撞仍用胶囊），按角色换发式与饰物。
# 角色在本地朝 Unity +Z（与灰盒的手臂前伸一致），舞台上整体转 180° 面向观众。静态网格，转身由时间轴转根节点。
exec(open("/Users/liweng/Downloads/3D/Silk to Divinity/tools/blender/hs_art.py").read())

def body_mesh(name, height_scale=1.0, legs=True, material=None):
    """皮肤修改器做的修长人体，求值后转成普通网格。"""
    S = height_scale
    J = {
        "pelvis": ((0, 0.95, 0), 0.135), "waist": ((0, 1.12, 0), 0.092), "chest": ((0, 1.3, 0), 0.112),
        "neck": ((0, 1.46, 0), 0.04), "neckTop": ((0, 1.5, 0.005), 0.038),
    }
    edges = [("pelvis", "waist"), ("waist", "chest"), ("chest", "neck"), ("neck", "neckTop")]
    for side, tag in ((-1, "L"), (1, "R")):
        J["sh" + tag] = ((side * 0.16, 1.4, 0), 0.05)
        J["el" + tag] = ((side * 0.27, 1.13, 0.02), 0.034)
        J["wr" + tag] = ((side * 0.17, 0.93, 0.17), 0.026)
        J["hd" + tag] = ((side * 0.15, 0.87, 0.22), 0.02)
        edges += [("chest", "sh" + tag), ("sh" + tag, "el" + tag), ("el" + tag, "wr" + tag), ("wr" + tag, "hd" + tag)]
        if legs:
            J["hip" + tag] = ((side * 0.08, 0.9, 0), 0.075)
            J["kn" + tag] = ((side * 0.08, 0.5, 0.01), 0.048)
            J["an" + tag] = ((side * 0.075, 0.09, 0), 0.032)
            J["to" + tag] = ((side * 0.075, 0.03, 0.11), 0.028)
            edges += [("pelvis", "hip" + tag), ("hip" + tag, "kn" + tag), ("kn" + tag, "an" + tag), ("an" + tag, "to" + tag)]
    if not legs:
        J["hipLow"] = ((0, 0.84, 0), 0.12)
        edges.append(("pelvis", "hipLow"))
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
    m2.materials.clear(); m2.materials.append(material or SKIN())
    for p in m2.polygons: p.use_smooth = True
    return body

def hands_and_feet():
    """手：掌加五指；脚：云头履。手的位置跟灰盒的小臂一致（肘 → 腕向前下）。"""
    S_ = SKIN()
    for side in (-1, 1):
        el = Vector((side * 0.27, 1.13, 0.02)); wr = Vector((side * 0.17, 0.93, 0.17))
        d = (wr - el).normalized()
        sideway = d.cross(Vector((0, 1, 0))).normalized()
        palm_end = wr + d * 0.06
        rod("Palm", wr + d * 0.005, palm_end, 0.017, S_, 12)
        ellipsoid("PalmCap", palm_end, (0.017, 0.017, 0.017), S_, 10, 6)
        for k, off in enumerate((-1.5, -0.5, 0.5, 1.5)):
            base = palm_end + sideway * off * 0.0085
            length = (0.042, 0.05, 0.047, 0.036)[k]
            tip = base + (d * 0.9 + Vector((0, -0.25, 0))).normalized() * length
            rod("Finger", base, tip, 0.0048, S_, 8)
            ellipsoid("FingerTip", tip, (0.0048, 0.0048, 0.0048), S_, 8, 4)
        tb = wr + d * 0.025 - sideway * side * 0.012
        rod("Thumb", tb, tb + (d * 0.6 - sideway * side * 0.6 + Vector((0, -0.2, 0))).normalized() * 0.035, 0.0055, S_, 8)
    shoe = mat("M_lacquer_red", (0.5, 0.12, 0.08))
    for side in (-1, 1):
        x = side * 0.075
        ellipsoid("Shoe", (x, 0.04, 0.045), (0.035, 0.04, 0.1), shoe, 16, 10)
        ellipsoid("ShoeTip", (x, 0.07, 0.135), (0.02, 0.025, 0.02), shoe, 10, 6)  # 云头上翘

def head(style):
    s = style
    # 鹅蛋脸：球体下半收窄，下巴略向前
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=32, v_segments=20, radius=1.0)
    for v in bm.verts:
        x, y, z = v.co.x, v.co.y, v.co.z   # Blender：z 向上，-y 向前（Unity +Z）
        if z < 0:
            t = -z
            k = 1 - 0.38 * t * t
            v.co.x = x * k
            v.co.y = y * (1 - 0.25 * t * t) - 0.12 * t * t
        v.co.x *= 0.072; v.co.y *= 0.082; v.co.z *= 0.095
    bmesh.ops.translate(bm, vec=B((0, 1.57, 0.01)), verts=bm.verts)
    bm.normal_update()
    smooth(_link("Head", bm, SKIN()))
    ellipsoid("Nose", (0, 1.556, 0.085), (0.0055, 0.012, 0.007), SKIN(), 12, 8)
    for x in (-0.072, 0.072):
        ellipsoid("Ear", (x, 1.565, 0.0), (0.008, 0.02, 0.014), SKIN(), 10, 6)
    # 五官：工笔人物式——细长的眼、弯眉、点唇
    for x in (-0.026, 0.026):
        ellipsoid("EyeWhite", (x, 1.578, 0.079), (0.015, 0.0062, 0.004), PAPER(), 12, 6)
        ellipsoid("Iris", (x, 1.578, 0.0828), (0.0062, 0.0062, 0.002), INK(), 10, 6)
        rod("Lid", (x - 0.014 * (1 if x > 0 else -1) * -1, 1.581, 0.083), (x + 0.014 * (1 if x > 0 else -1), 1.585, 0.081), 0.0015, INK(), 6)
        sgn = 1 if x > 0 else -1
        rod("Brow", (x - sgn * 0.012, 1.6, 0.082), (x + sgn * 0.004, 1.607, 0.081), 0.0016, INK(), 6)
        rod("Brow", (x + sgn * 0.004, 1.607, 0.081), (x + sgn * 0.018, 1.602, 0.077), 0.0014, INK(), 6)
        ellipsoid("Blush", (x * 1.45, 1.55, 0.072), (0.011, 0.006, 0.002), mat("M_blush", (0.9, 0.66, 0.6)), 10, 6)
    lip = mat("M_lacquer_red", (0.5, 0.12, 0.08))
    ellipsoid("LipUp", (0, 1.527, 0.083), (0.011, 0.0035, 0.004), lip, 10, 6)
    ellipsoid("LipLow", (0, 1.521, 0.082), (0.008, 0.0035, 0.004), lip, 10, 6)
    H = mat(s.get("hairMat", "M_hair"), s.get("hair", (0.05, 0.04, 0.04)))
    # 发盖住头顶与后脑
    ellipsoid("HairCap", (0, 1.6, -0.005), (0.078, 0.085, 0.085), H, 24, 14)
    for x in (-0.068, 0.068):
        ellipsoid("SideLock", (x, 1.58, 0.02), (0.014, 0.05, 0.03), H, 10, 8)  # 鬓
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
    hands_and_feet()
    head(STYLES[cid])
    export("Characters/SK_" + cid)

def build_form():
    """人台（裁缝用的立裁人台）：布面躯干带臂，木颈钮，铜杆与三脚底座。与灰盒同位，布料仍靠灰盒胶囊。"""
    clear()
    F = mat("M_form", (0.86, 0.82, 0.72))
    body_mesh("FormBody", legs=False, material=F)
    lathe("NeckKnob", (0, 1.47, 0), [(0.0, 0), (0.045, 0), (0.05, 0.03), (0.035, 0.07), (0.0, 0.08)], WOOD_DARK(), 20)
    lathe("HipCap", (0, 0.78, 0), [(0.0, 0), (0.1, 0.0), (0.12, 0.05), (0.0, 0.05)], WOOD_DARK(), 24)
    rod("Pole", (0, 0.05, 0), (0, 0.8, 0), 0.016, BRASS(), 16)
    lathe("Collar", (0, 0.42, 0), [(0.0, 0), (0.03, 0), (0.03, 0.04), (0.0, 0.04)], BRASS(), 16)
    for k in range(3):
        a = k / 3 * math.tau + 0.5
        rod("Leg", (0, 0.06, 0), (math.cos(a) * 0.28, 0.01, math.sin(a) * 0.28), 0.014, WOOD_DARK(), 10)
        ellipsoid("Foot", (math.cos(a) * 0.28, 0.012, math.sin(a) * 0.28), (0.025, 0.012, 0.025), BRASS(), 10, 6)
    export("Characters/SK_form")

only = globals().get("ONLY")
if only and "form" in only:
    build_form()
result = {}
for cid in STYLES:
    if only and cid not in only: continue
    build(cid); result[cid] = stats()
