# 衣片网格（docs/17 §3、§7）：每个部件一张网格，在人体根节点空间（Unity 坐标），按绘制约定写 uv：
# uv.v = 0.999 固定（肩线、袖山、裙腰、领缘、腰带），≤ 0.9 可动；uv.u 沿一圈 0–0.999。
# 人体本地朝 +Z（手臂前伸）；穿着者的右手在 +X。只做单层面，不加厚度，供 MagicaCloth MeshCloth 解算。
exec(open("/Users/liweng/Downloads/3D/Silk to Divinity/tools/blender/hs_art.py").read())

FIXED, FREE = 0.999, 0.9

def arm(side):
    return [Vector((side * 0.19, 1.40, 0.0)), Vector((side * 0.27, 1.13, 0.02)), Vector((side * 0.17, 0.93, 0.17))]

class G:
    """一件衣片的几何：多块壳合成一张网格。坐标都是 Unity 的。"""
    def __init__(self):
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.new("UVMap")

    def _face(self, vs, uvs):
        try:
            f = self.bm.faces.new(vs)
        except ValueError:
            return
        for l, (u, v) in zip(f.loops, uvs):
            l[self.uv].uv = (u, v)

    def sweep(self, ctrl, radii, around, ellipse=(1, 1), fixed_rings=1, mod=None, step=0.035, all_fixed=False):
        path, rad = [], []
        for i in range(len(ctrl) - 1):
            a, b = Vector(ctrl[i]), Vector(ctrl[i + 1])
            n = max(1, math.ceil((b - a).length / step))
            for k in range(n):
                f = k / n
                path.append(a.lerp(b, f)); rad.append(radii[i] + (radii[i + 1] - radii[i]) * f)
        path.append(Vector(ctrl[-1])); rad.append(radii[-1])
        rings = len(path); cols = around + 1
        grid = []
        normal = None
        for r in range(rings):
            t = (path[min(r + 1, rings - 1)] - path[max(r - 1, 0)]).normalized()
            if normal is None:
                normal = Vector((1, 0, 0)) - t * t.dot(Vector((1, 0, 0)))
                if normal.length < 1e-3: normal = Vector((0, 0, 1)) - t * t.dot(Vector((0, 0, 1)))
            else:
                normal = normal - t * t.dot(normal)
            normal.normalize()
            binv = t.cross(normal).normalized()
            down = r / (rings - 1)
            vv = FIXED if (all_fixed or r < fixed_rings) else FREE * (1 - down)
            row = []
            for c in range(cols):
                a = c / around * math.tau
                m = mod(a, down) if (mod and r >= fixed_rings) else 1.0
                off = normal * (math.cos(a) * ellipse[0]) + binv * (math.sin(a) * ellipse[1])
                p = path[r] + off * rad[r] * m
                row.append((self.bm.verts.new(B(p)), (min(c / around, FIXED), vv)))
            grid.append(row)
        for r in range(rings - 1):
            for c in range(around):
                a0, a1 = grid[r][c], grid[r][c + 1]
                b0, b1 = grid[r + 1][c], grid[r + 1][c + 1]
                self._face([a0[0], b0[0], b1[0], a1[0]], [a0[1], b0[1], b1[1], a1[1]])

    def ribbon(self, ctrl, width, v_of, surface_n=None, step=0.03):
        """带子：surface_n(p) 给出贴着的表面法线（宽度沿表面），否则宽度取水平。"""
        path = smooth_path(ctrl, step)
        prev = None
        rows = []
        for i, p in enumerate(path):
            t = (path[min(i + 1, len(path) - 1)] - path[max(i - 1, 0)]).normalized()
            if surface_n:
                w = t.cross(surface_n(p))
            else:
                w = t.cross(Vector((0, 1, 0)))
                if w.length < 0.3: w = t.cross(Vector((0, 0, 1)))
            w.normalize()
            if prev is not None and w.dot(prev) < 0: w = -w
            prev = w
            s = i / (len(path) - 1)
            vv = v_of(s)
            rows.append(((self.bm.verts.new(B(p - w * width * 0.5)), (0.0, vv)), (self.bm.verts.new(B(p + w * width * 0.5)), (FIXED, vv))))
        for i in range(len(rows) - 1):
            (a, b), (c, d) = rows[i], rows[i + 1]
            self._face([a[0], c[0], d[0], b[0]], [a[1], c[1], d[1], b[1]])

    def finish(self, name):
        bmesh.ops.remove_doubles(self.bm, verts=self.bm.verts, dist=1e-6)
        self.bm.normal_update()
        me = bpy.data.meshes.new(name)
        self.bm.to_mesh(me); self.bm.free()
        for p in me.polygons: p.use_smooth = True
        ob = bpy.data.objects.new(name, me)
        scene().collection.objects.link(ob)
        me.materials.append(mat("M_cloth_prop", (0.8, 0.75, 0.68)))
        return ob

def smooth_path(ctrl, step):
    c = [Vector(p) for p in ctrl]
    out = []
    for i in range(len(c) - 1):
        p0, p1, p2, p3 = c[max(i - 1, 0)], c[i], c[i + 1], c[min(i + 2, len(c) - 1)]
        n = max(1, math.ceil((p2 - p1).length / step))
        for k in range(n):
            t = k / n; t2 = t * t; t3 = t2 * t
            out.append(0.5 * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3))
    out.append(c[-1])
    return out

def ell_point(x, y, radius_at, ex, ez, out=0.008):
    """衣身（椭圆截面）正面上 x 处的点，向外让出 out。"""
    r = radius_at(y)
    w, d = r * ex, r * ez
    xx = max(-w * 0.98, min(w * 0.98, x))
    z = d * math.sqrt(max(0.0, 1 - (xx / w) ** 2)) + out
    return Vector((xx, y, z))

def ell_normal(p, ex, ez):
    n = Vector((p.x / (ex * ex), 0, p.z / (ez * ez)))
    return n.normalized() if n.length > 1e-6 else Vector((0, 0, 1))

def interp(ys, rs):
    def f(y):
        for i in range(len(ys) - 1):
            if ys[i] >= y >= ys[i + 1]:
                t = (ys[i] - y) / (ys[i] - ys[i + 1])
                return rs[i] + (rs[i + 1] - rs[i]) * t
        return rs[0] if y > ys[0] else rs[-1]
    return f

# ---------- 部件 ----------

def torso(g, ys, rs, around, ex, ez, mod=None):
    """衣身从领口起：先收到颈根（固定），再沿肩斜落到肩线（固定），往下可动。"""
    neck_y, neck_r = ys[0] + 0.035, 0.075
    g.sweep([(0, neck_y, 0)] + [(0, y, 0) for y in ys], [neck_r] + rs, around, (ex, ez), 3, mod, step=0.03)
    return interp(ys, rs)

def sleeves(g, radii, end_extra=0.0, drop=None, around=20, cuff=None):
    for side in (-1, 1):
        a = arm(side)
        path = [a[0] + Vector((side * 0.02, 0.02, 0)), a[1], a[2]]
        if end_extra: path.append(a[2] + (a[2] - a[1]).normalized() * end_extra)
        if drop: path.append(path[-1] + Vector((0, -drop, 0.02)))
        rr = radii[:len(path)]
        g.sweep(path, rr, around, (1, 1), 1)

def cross_collar(g, radius_at, ex, ez, top=1.47, bottom=1.14, width=0.05):
    """交领右衽：外襟领缘从穿着者左侧颈根斜到右腋下，内襟只露颈下一小段。领缘全固定。"""
    sn = lambda p: ell_normal(p, ex, ez)
    outer = [ell_point(-0.055, top - 0.01, radius_at, ex, ez), ell_point(-0.02, top - 0.08, radius_at, ex, ez), ell_point(0.06, top - 0.2, radius_at, ex, ez), ell_point(0.15, bottom, radius_at, ex, ez)]
    g.ribbon(outer, width, lambda s: FIXED, sn)
    inner = [ell_point(0.055, top - 0.01, radius_at, ex, ez, 0.006), ell_point(0.02, top - 0.07, radius_at, ex, ez, 0.006)]
    g.ribbon(inner, width * 0.8, lambda s: FIXED, sn)
    # 后领：绕过后颈
    back = [Vector((-0.06, top, -0.02)), Vector((0, top + 0.005, -0.075)), Vector((0.06, top, -0.02))]
    g.ribbon(back, width * 0.8, lambda s: FIXED, None)

def front_bands(g, radius_at, ex, ez, top=1.46, bottom=0.6, width=0.045):
    """对襟：两条领缘从颈侧直下到衣摆。"""
    sn = lambda p: ell_normal(p, ex, ez)
    for x in (-0.05, 0.05):
        pts = [ell_point(x, y, radius_at, ex, ez) for y in (top, top - 0.1, (top + bottom) / 2, bottom)]
        g.ribbon(pts, width, lambda s: FIXED if s < 0.35 else FREE * (1 - s) + 0.05, sn)

def waistband(g, y, r, ex=1.12, ez=1.0, h=0.06):
    g.sweep([(0, y + h / 2, 0), (0, y - h / 2, 0)], [r, r], 48, (ex, ez), 1, all_fixed=True, step=0.03)

def sashes(g, y, z, length=0.5, spread=0.05, width=0.04):
    """腰前垂下的两条飘带：结处固定，往下可动。"""
    for dx in (-spread, spread):
        pts = [Vector((dx * 0.3, y, z)), Vector((dx, y - 0.1, z + 0.01)), Vector((dx * 1.4, y - length * 0.6, z + 0.03)), Vector((dx * 1.2, y - length, z + 0.02))]
        g.ribbon(pts, width, lambda s: FIXED if s < 0.08 else FREE * (1 - s), None)

def drape(g, low=0.55, width=0.24):
    la, ra = arm(-1), arm(1)
    ctrl = [Vector((la[2].x - 0.03, low, la[2].z + 0.04)), la[2] + Vector((-0.02, 0.06, 0.02)), la[1] + Vector((-0.06, 0.04, -0.06)),
            Vector((-0.16, 1.36, -0.2)), Vector((0, 1.38, -0.22)), Vector((0.16, 1.36, -0.2)),
            ra[1] + Vector((0.06, 0.04, -0.06)), ra[2] + Vector((0.02, 0.06, 0.02)), Vector((ra[2].x + 0.03, low, ra[2].z + 0.04))]
    g.ribbon(ctrl, width, lambda s: FIXED if abs(s - 0.5) < 0.1 else FREE * (1 - abs(s - 0.5) * 1.6), None, 0.035)

pleat = lambda n, amp: (lambda a, down: 1 + amp * down * math.sin(a * n))

# ---------- 形制 ----------

def part(name, build):
    clear()
    g = G(); build(g)
    ob = g.finish("SK_part_" + name)
    nverts = len(ob.data.vertices)
    export("Garments/SK_part_" + name)
    return nverts

def upper_ruQun(g):
    ys, rs = [1.47, 1.30, 1.12, 0.98], [0.16, 0.185, 0.21, 0.25]
    ra = torso(g, ys, rs, 36, 1.25, 0.9)
    sleeves(g, [0.075, 0.11, 0.17, 0.21], end_extra=0.09)
    cross_collar(g, ra, 1.25, 0.9)

def skirt_ruQun(g):
    g.sweep([(0, 1.12, 0), (0, 0.85, 0), (0, 0.5, 0), (0, 0.08, 0)], [0.175, 0.24, 0.32, 0.42], 64, (1.1, 1.0), 1, pleat(16, 0.07))
    waistband(g, 1.1, 0.185)
    sashes(g, 1.08, 0.2)

def inner_any(g):
    ra = torso(g, [1.45, 1.25, 1.02], [0.15, 0.17, 0.2], 28, 1.2, 0.9)

def drape_any(g): drape(g)
def drape_qiXiong(g): drape(g, low=0.32, width=0.28)
def drape_kalasiris(g): drape(g, low=0.7, width=0.42)

def robe_zhiJu(g):
    ys, rs = [1.47, 1.25, 1.0, 0.5, 0.05], [0.16, 0.19, 0.22, 0.28, 0.34]
    ra = torso(g, ys, rs, 48, 1.2, 0.95, lambda a, d: 1 + 0.03 * d * math.sin(a * 8))
    for side in (-1, 1):
        a = arm(side)
        g.sweep([a[0] + Vector((side * 0.02, 0.02, 0)), a[1], a[2] + (a[2] - a[1]).normalized() * 0.04], [0.075, 0.09, 0.1], 18, (1, 1), 1)
    cross_collar(g, ra, 1.2, 0.95, bottom=1.05)
    waistband(g, 1.0, 0.225, 1.2, 0.97, 0.07)

def robe_daXiuShan(g):
    ys, rs = [1.47, 1.2, 0.9, 0.5], [0.17, 0.22, 0.3, 0.38]
    ra = torso(g, ys, rs, 44, 1.2, 0.95)
    sleeves(g, [0.08, 0.16, 0.26, 0.32], end_extra=0.14, around=26)
    front_bands(g, ra, 1.2, 0.95, bottom=0.55)

def skirt_any(g):
    skirt_ruQun(g)

def wrap_beiZi(g):
    ys, rs = [1.47, 1.2, 0.85, 0.38], [0.165, 0.21, 0.29, 0.37]
    ra = torso(g, ys, rs, 40, 1.15, 0.95)
    for side in (-1, 1):
        a = arm(side)
        g.sweep([a[0] + Vector((side * 0.02, 0.02, 0)), a[1], a[2] + (a[2] - a[1]).normalized() * 0.03], [0.07, 0.085, 0.095], 18, (1, 1), 1)
    front_bands(g, ra, 1.15, 0.95, bottom=0.42)

def upper_aoQun(g):
    ys, rs = [1.47, 1.25, 1.0, 0.86], [0.165, 0.2, 0.25, 0.28]
    ra = torso(g, ys, rs, 36, 1.2, 0.95)
    for side in (-1, 1):
        a = arm(side)
        d = (a[2] - a[1]).normalized()
        g.sweep([a[0] + Vector((side * 0.02, 0.02, 0)), a[1], a[2] - d * 0.04, a[2] + d * 0.04], [0.08, 0.14, 0.15, 0.07], 20, (1, 1), 1)
    cross_collar(g, ra, 1.2, 0.95, bottom=1.08, width=0.06)

def skirt_aoQun(g):
    """马面裙：前后两块平整的马面，两侧打褶。"""
    def mamian(a, down):
        flat = abs(math.sin(a)) > 0.8
        return 1.0 if flat else 1 + 0.08 * down * math.sin(a * 20)
    g.sweep([(0, 1.1, 0), (0, 0.85, 0), (0, 0.5, 0), (0, 0.06, 0)], [0.18, 0.25, 0.33, 0.43], 72, (1.1, 1.0), 1, mamian)
    waistband(g, 1.08, 0.19, 1.1, 1.0, 0.08)

def upper_qiXiong(g):
    ys, rs = [1.47, 1.36, 1.28], [0.16, 0.19, 0.215]
    ra = torso(g, ys, rs, 32, 1.25, 0.9)
    sleeves(g, [0.075, 0.12, 0.18, 0.2], end_extra=0.06)
    cross_collar(g, ra, 1.25, 0.9, bottom=1.3, width=0.04)

def skirt_qiXiong(g):
    g.sweep([(0, 1.33, 0), (0, 1.0, 0), (0, 0.5, 0), (0, 0.03, 0)], [0.2, 0.27, 0.36, 0.46], 64, (1.15, 1.0), 1, pleat(14, 0.06))
    waistband(g, 1.31, 0.205, 1.15, 1.0, 0.05)
    sashes(g, 1.29, 0.21, length=0.7, spread=0.06, width=0.05)

def robe_chiton(g):
    ys, rs = [1.47, 1.3, 1.12, 1.04, 0.6, 0.03], [0.2, 0.235, 0.245, 0.2, 0.3, 0.37]
    torso(g, ys, rs, 64, 1.25, 0.95, lambda a, d: 1 + 0.06 * d * math.sin(a * 14))
    for side in (-1, 1):
        a = arm(side)
        g.sweep([a[0] + Vector((side * 0.03, 0.03, 0)), a[0].lerp(a[1], 0.5), a[1] + Vector((0, -0.02, 0))], [0.09, 0.14, 0.17], 22, (1, 1), 1)
    waistband(g, 1.04, 0.205, 1.25, 0.95, 0.03)

def robe_kalasiris(g):
    ys, rs = [1.45, 1.2, 0.95, 0.5, 0.06], [0.17, 0.19, 0.2, 0.22, 0.25]
    torso(g, ys, rs, 64, 1.2, 0.95, lambda a, d: 1 + 0.02 * math.sin(a * 30))
    for side in (-1, 1):
        a = arm(side)
        g.sweep([a[0] + Vector((side * 0.02, 0.02, 0)), a[0].lerp(a[1], 0.35)], [0.08, 0.1], 18, (1, 1), 1)
    # 宽项圈：胸前一圈固定的带子
    g.sweep([(0, 1.45, 0.0), (0, 1.36, 0.0)], [0.175, 0.205], 48, (1.22, 0.97), 1, all_fixed=True, step=0.03)

def robe_juniHitoe(g):
    ys, rs = [1.47, 1.2, 0.8, 0.3, 0.02], [0.19, 0.25, 0.33, 0.42, 0.5]
    ra = torso(g, ys, rs, 52, 1.15, 1.0, lambda a, d: 1 + 0.03 * d * math.sin(a * 6))
    for side in (-1, 1):
        a = arm(side)
        g.sweep([a[0] + Vector((side * 0.02, 0.02, 0)), a[1], a[2], a[2] + Vector((0, -0.22, 0))], [0.09, 0.2, 0.3, 0.34], 28, (1, 1), 1)
    # 层层叠领：三道领缘
    for k in range(3):
        cross_collar(g, lambda y: ra(y) + 0.008 * k, 1.15, 1.0, bottom=1.05 - k * 0.03, width=0.035)

def skirt_juniHitoe(g):
    g.sweep([(0, 1.08, 0), (0, 0.8, 0), (0, 0.4, 0), (0, 0.0, 0)], [0.19, 0.3, 0.4, 0.46], 56, (1.15, 1.0), 1, pleat(8, 0.05))
    waistband(g, 1.06, 0.2, 1.15, 1.0, 0.05)
    sashes(g, 1.04, 0.22, length=0.35, spread=0.08, width=0.035)

PARTS = {
    "upper_ruQun": upper_ruQun, "skirt_ruQun": skirt_ruQun, "inner_any": inner_any, "drape_any": drape_any,
    "robe_zhiJu": robe_zhiJu, "robe_daXiuShan": robe_daXiuShan, "skirt_any": skirt_any, "wrap_beiZi": wrap_beiZi,
    "upper_aoQun": upper_aoQun, "skirt_aoQun": skirt_aoQun, "upper_qiXiong": upper_qiXiong, "skirt_qiXiong": skirt_qiXiong,
    "drape_qiXiong": drape_qiXiong, "robe_chiton": robe_chiton, "robe_kalasiris": robe_kalasiris, "drape_kalasiris": drape_kalasiris,
    "robe_juniHitoe": robe_juniHitoe, "skirt_juniHitoe": skirt_juniHitoe,
}
only = globals().get("ONLY")
result = {}
for k, fn in PARTS.items():
    if only and k not in only: continue
    result[k] = part(k, fn)
