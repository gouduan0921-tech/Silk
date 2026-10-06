# 华裳 Blender 建模工具库（docs/17 §7）
# 所有坐标、尺寸都按 Unity 写（x 右，y 上，z 向里；工位正面朝 -z），导出时换算。
# 校准：Blender (x, y, z) -> Unity (-x, z, -y)；所以 Unity (X, Y, Z) -> Blender (-X, -Z, Y)。
import bpy, bmesh, math, os
from mathutils import Vector, Matrix

PROJECT = "/Users/liweng/Downloads/3D/Silk to Divinity"
ART = PROJECT + "/Assets/HuaShang/Resources/HuaShang/Art"

def B(p):
    """Unity 坐标 -> Blender 坐标"""
    return Vector((-p[0], -p[2], p[1]))

def scene():
    return bpy.context.scene or bpy.data.scenes[0]

def clear():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.meshes):
        if m.users == 0: bpy.data.meshes.remove(m)

_mats = {}
def mat(name, rgb, rough=0.6, metal=0.0):
    """材质只用名字对上 Unity 材质库（ArtMaterials），颜色是 Blender 里看的预览色。"""
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (rgb[0], rgb[1], rgb[2], 1)
        bsdf.inputs["Roughness"].default_value = rough
        bsdf.inputs["Metallic"].default_value = metal
    m.diffuse_color = (rgb[0], rgb[1], rgb[2], 1)
    return m

WOOD = lambda: mat("M_wood", (0.36, 0.25, 0.17))
WOOD_LIGHT = lambda: mat("M_wood_light", (0.58, 0.44, 0.30))
WOOD_DARK = lambda: mat("M_wood_dark", (0.22, 0.15, 0.10))
LACQUER = lambda: mat("M_lacquer_red", (0.45, 0.10, 0.07), 0.35)
PLASTER = lambda: mat("M_plaster", (0.80, 0.76, 0.68), 0.9)
PAPER = lambda: mat("M_paper", (0.90, 0.87, 0.80), 0.85)
POTTERY = lambda: mat("M_pottery", (0.30, 0.25, 0.21), 0.5)
BAMBOO = lambda: mat("M_bamboo", (0.66, 0.58, 0.36), 0.55)
STONE = lambda: mat("M_stone", (0.50, 0.49, 0.46), 0.8)
BRASS = lambda: mat("M_brass", (0.70, 0.55, 0.30), 0.35, 0.8)
THREAD = lambda: mat("M_thread", (0.92, 0.90, 0.84), 0.7)
INK = lambda: mat("M_ink", (0.20, 0.21, 0.22), 0.9)
TILE = lambda: mat("M_tile", (0.24, 0.25, 0.27), 0.6)
SKIN = lambda: mat("M_skin", (0.86, 0.72, 0.62), 0.55)
HAIR = lambda: mat("M_hair", (0.05, 0.04, 0.04), 0.45)

def _link(name, bm, material):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me)
    if material: me.materials.append(material)
    scene().collection.objects.link(ob)
    return ob

def _box_uv(bm, scale=1.0):
    uv = bm.loops.layers.uv.verify()
    for f in bm.faces:
        n = f.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        for l in f.loops:
            c = l.vert.co
            if ax == 0: l[uv].uv = (c.y * scale, c.z * scale)
            elif ax == 1: l[uv].uv = (c.x * scale, c.z * scale)
            else: l[uv].uv = (c.x * scale, c.y * scale)

def box(name, center, size, material, bevel=0.008, rot=(0, 0, 0)):
    """center/size 是 Unity 的；rot 是 Unity 欧拉角（度）。"""
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    s = (size[0], size[2], size[1])  # Unity (x,y,z) 尺寸 -> Blender (x,y,z) 尺寸
    bmesh.ops.scale(bm, vec=s, verts=bm.verts)
    if bevel > 0:
        b = min(bevel, min(size) * 0.45)
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=b, segments=2, affect='EDGES', profile=0.5)
    _apply_rot(bm, rot)
    bmesh.ops.translate(bm, vec=B(center), verts=bm.verts)
    bm.normal_update()
    _box_uv(bm)
    return _link(name, bm, material)

def _apply_rot(bm, rot):
    if rot == (0, 0, 0): return
    # Unity 欧拉 (x,y,z) 依次绕 Unity 轴；换到 Blender 轴：Unity x -> -X，Unity y -> Z，Unity z -> -Y
    rx = Matrix.Rotation(math.radians(-rot[0]), 4, 'X')
    ry = Matrix.Rotation(math.radians(rot[1]), 4, 'Z')
    rz = Matrix.Rotation(math.radians(-rot[2]), 4, 'Y')
    # Unity 的欧拉顺序 Z, X, Y（先 z 后 x 再 y）
    bmesh.ops.transform(bm, matrix=ry @ rx @ rz, verts=bm.verts)

def rod(name, a, b, radius, material, segs=16, cap=True):
    """Unity 两点之间的圆柱。"""
    pa, pb = B(a), B(b)
    d = pb - pa
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=cap, cap_tris=False, segments=segs, radius1=radius, radius2=radius, depth=d.length)
    q = Vector((0, 0, 1)).rotation_difference(d.normalized())
    bmesh.ops.transform(bm, matrix=q.to_matrix().to_4x4(), verts=bm.verts)
    bmesh.ops.translate(bm, vec=(pa + pb) * 0.5, verts=bm.verts)
    bm.normal_update()
    _box_uv(bm, 2.0)
    return _link(name, bm, material)

def lathe(name, center, profile, material, segs=32):
    """绕 Unity y 轴旋转的轮廓。profile: [(半径, 高度)...]，从下到上。"""
    bm = bmesh.new()
    rings = []
    for (r, h) in profile:
        ring = []
        for i in range(segs):
            a = i / segs * math.tau
            ring.append(bm.verts.new(B((center[0] + math.cos(a) * r, center[1] + h, center[2] + math.sin(a) * r))))
        rings.append(ring)
    for k in range(len(rings) - 1):
        for i in range(segs):
            j = (i + 1) % segs
            bm.faces.new((rings[k][i], rings[k][j], rings[k + 1][j], rings[k + 1][i]))
    if profile[0][0] > 1e-4: bm.faces.new(list(reversed(rings[0])))
    if profile[-1][0] > 1e-4: bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.normal_update()
    _box_uv(bm, 2.0)
    ob = _link(name, bm, material)
    for p in ob.data.polygons: p.use_smooth = True
    return ob

def plane(name, center, size_xy, material, rot=(0, 0, 0)):
    """竖直平面（Unity xy 平面，朝 -z）。"""
    return box(name, center, (size_xy[0], size_xy[1], 0.004), material, bevel=0, rot=rot)

def smooth(ob, angle=40):
    for p in ob.data.polygons: p.use_smooth = True
    return ob

def export(rel_path):
    """导出到 Resources/HuaShang/Art/{rel_path}.fbx；导出前把场景里所有物体合成一个父节点下。"""
    path = ART + "/" + rel_path + ".fbx"
    os.makedirs(os.path.dirname(path), exist_ok=True)
    sc = scene()
    for o in sc.objects: o.select_set(True)
    with bpy.context.temp_override(scene=sc, view_layer=sc.view_layers[0]):
        bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_scale_options='FBX_SCALE_ALL',
                                 axis_forward='-Z', axis_up='Y', bake_space_transform=True,
                                 object_types={'MESH', 'ARMATURE', 'EMPTY'}, use_mesh_modifiers=True,
                                 mesh_smooth_type='FACE', path_mode='STRIP')
    return path

def stats():
    sc = scene()
    tris = 0
    for o in sc.objects:
        if o.type == 'MESH':
            tris += sum(len(p.vertices) - 2 for p in o.data.polygons)
    return {"objects": len(sc.objects), "tris": tris}

LEAF = lambda: mat("M_leaf", (0.30, 0.45, 0.22), 0.6)

def flat_shape(name, center, outline, depth, material):
    """竖直的薄片：outline 是 Unity xy 局部坐标的多边形，朝 -z，厚 depth。"""
    bm = bmesh.new()
    front = [bm.verts.new(B((center[0] + x, center[1] + y, center[2] - depth / 2))) for (x, y) in outline]
    back = [bm.verts.new(B((center[0] + x, center[1] + y, center[2] + depth / 2))) for (x, y) in outline]
    bm.faces.new(front)
    bm.faces.new(list(reversed(back)))
    n = len(outline)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((front[i], back[i], back[j], front[j]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.normal_update()
    _box_uv(bm, 0.5)
    return _link(name, bm, material)

def ellipsoid(name, center, radii, material, segs=16, rings=10):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=rings, radius=1.0)
    bmesh.ops.scale(bm, vec=(radii[0], radii[2], radii[1]), verts=bm.verts)
    bmesh.ops.translate(bm, vec=B(center), verts=bm.verts)
    bm.normal_update()
    _box_uv(bm, 2.0)
    ob = _link(name, bm, material)
    return smooth(ob)
