import bpy, bmesh, os, sys, json, math
from mathutils import Vector, Matrix
argv = sys.argv[sys.argv.index("--")+1:]
out_dir = argv[0]
variants = json.loads(argv[1])
srcs = argv[2:]
def tris(o): return sum(len(p.vertices)-2 for p in o.data.polygons)
def apply_mods(o):
    dg = bpy.context.evaluated_depsgraph_get()
    new = bpy.data.meshes.new_from_object(o.evaluated_get(dg), preserve_all_data_layers=True, depsgraph=dg)
    old = o.data; o.modifiers.clear(); o.data = new
    for m in old.materials:
        if m and new.materials.find(m.name) < 0: new.materials.append(m)
def decimate(o, planar_deg=None, ratio=None, protect=()):
    me = o.data
    prot_idx = {i for i, m in enumerate(me.materials) if m and any(p in m.name.lower() for p in protect)}
    if planar_deg:
        m = o.modifiers.new("Planar", 'DECIMATE'); m.decimate_type='DISSOLVE'; m.angle_limit = math.radians(planar_deg); m.delimit={'UV','MATERIAL','SHARP','SEAM'}
    if ratio and ratio < 1:
        if prot_idx:
            vg = o.vertex_groups.new(name="keep")
            verts = set()
            for p in me.polygons:
                if p.material_index in prot_idx: verts.update(p.vertices)
            if verts: vg.add(list(verts), 1.0, 'REPLACE')
        m = o.modifiers.new("Collapse", 'DECIMATE'); m.decimate_type='COLLAPSE'; m.ratio=ratio; m.use_collapse_triangulate=True; m.use_symmetry=True; m.symmetry_axis='X'
        if prot_idx: m.vertex_group = "keep"; m.invert_vertex_group = True; m.vertex_group_factor = 10.0
    apply_mods(o)
def wheel_cyl(o, segments):
    me = o.data
    uv = me.uv_layers.active
    if not uv or len(me.vertices) < 3: return
    xs=[v.co.x for v in me.vertices]; ys=[v.co.y for v in me.vertices]; zs=[v.co.z for v in me.vertices]
    dims = [max(xs)-min(xs), max(ys)-min(ys), max(zs)-min(zs)]
    axis = dims.index(min(dims))
    center = Vector(((max(xs)+min(xs))/2, (max(ys)+min(ys))/2, (max(zs)+min(zs))/2))
    width = dims[axis]; radius = max(d for i,d in enumerate(dims) if i!=axis)/2
    tire_mat={}; rim_mat={}
    best_rim=None; best_rim_r=1e9; best_tire=None; best_tire_a=-1
    def face_uv(p):
        lst=[uv.data[l].uv.copy() for l in p.loop_indices]
        return sum(lst, Vector((0,0)))/len(lst)
    for p in me.polygons:
        n = p.normal; r = p.center - center; r[axis]=0
        if abs(n[axis]) > 0.7:
            rim_mat[p.material_index] = rim_mat.get(p.material_index,0)+1
            if r.length < best_rim_r: best_rim_r = r.length; best_rim = face_uv(p)
        elif r.length > radius*0.9:
            tire_mat[p.material_index] = tire_mat.get(p.material_index,0)+1
            if p.area > best_tire_a: best_tire_a = p.area; best_tire = face_uv(p)
    tire = best_tire if best_tire is not None else Vector((0.5,0.5))
    rim = best_rim if best_rim is not None else tire
    tire_mi = max(tire_mat, key=tire_mat.get) if tire_mat else 0
    rim_mi = max(rim_mat, key=rim_mat.get) if rim_mat else tire_mi
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segments, radius1=radius, radius2=radius, depth=width)
    if axis == 0: rot = Matrix.Rotation(math.radians(90), 4, 'Y')
    elif axis == 1: rot = Matrix.Rotation(math.radians(90), 4, 'X')
    else: rot = Matrix.Identity(4)
    bmesh.ops.transform(bm, matrix=Matrix.Translation(center) @ rot, verts=bm.verts)
    uv_layer = bm.loops.layers.uv.new(uv.name)
    for f in bm.faces:
        cap = abs(f.normal[axis]) > 0.7
        f.smooth = not cap
        f.material_index = rim_mi if cap else tire_mi
        for l in f.loops: l[uv_layer].uv = rim if cap else tire
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
    new = bpy.data.meshes.new(me.name + "_cyl"); bm.to_mesh(new); bm.free()
    for m in me.materials: new.materials.append(m)
    o.data = new
report = {}
for src in srcs:
    base = os.path.splitext(os.path.basename(src))[0]
    rep = {}
    for tag, v in variants.items():
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=src)
        meshes = [o for o in bpy.context.scene.objects if o.type=='MESH']
        before = sum(tris(o) for o in meshes)
        for o in meshes:
            if 'wheel' in o.name.lower():
                if v.get('wheel_cyl'): wheel_cyl(o, v['wheel_cyl'])
                else: decimate(o, v.get('wheel_planar'), v.get('wheel_ratio'))
            else:
                decimate(o, v.get('body_planar'), v.get('body_ratio'), protect=tuple(v.get('protect', [])))
        after = sum(tris(o) for o in bpy.context.scene.objects if o.type=='MESH')
        fp = os.path.join(out_dir, f"{base}_{tag}.fbx")
        bpy.ops.export_scene.fbx(filepath=fp, use_selection=False, apply_scale_options='FBX_SCALE_ALL', mesh_smooth_type='FACE', use_mesh_modifiers=False, add_leaf_bones=False, bake_anim=False, path_mode='STRIP', embed_textures=False)
        rep[tag] = [before, after]
    report[base] = rep
    print("CAR " + base + " " + json.dumps(rep), flush=True)
print("REPORT " + json.dumps(report))
