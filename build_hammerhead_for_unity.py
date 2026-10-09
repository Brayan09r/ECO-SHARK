"""
EcoShark: Modelador 3D del Tiburón Martillo orientado nativamente para Unity 2.5D (+Z Forward, +Y Up).
Exporta directamente a D:\ECO SHARK\Assets\EcoShark\Models\Sharks\ y a Downloads.
"""

import bpy
import bmesh
import math
import os
import shutil

TARGET_DIR = r"D:\ECO SHARK\Assets\EcoShark\Models\Sharks"
DOWNLOADS_DIR = r"C:\Users\capan\Downloads"

os.makedirs(TARGET_DIR, exist_ok=True)
os.makedirs(DOWNLOADS_DIR, exist_ok=True)

def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.meshes):
        bpy.data.meshes.remove(m, do_unlink=True)
    for a in list(bpy.data.armatures):
        bpy.data.armatures.remove(a, do_unlink=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat, do_unlink=True)
    for act in list(bpy.data.actions):
        bpy.data.actions.remove(act, do_unlink=True)

    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE_NEXT' if hasattr(bpy.types, 'RenderSettings') and 'BLENDER_EEVEE_NEXT' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE'
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = 'PNG'

def setup_studio_lighting():
    key_data = bpy.data.lights.new('KeyLight', type='SUN')
    key_data.energy = 4.5
    key_data.color = (1.0, 0.98, 0.92)
    key_obj = bpy.data.objects.new('KeyLight', key_data)
    bpy.context.collection.objects.link(key_obj)
    key_obj.location = (2, 6, 4)
    key_obj.rotation_euler = (math.radians(-35), math.radians(25), 0)

    fill_data = bpy.data.lights.new('FillLight', type='POINT')
    fill_data.energy = 420.0
    fill_data.color = (0.2, 0.75, 0.95)
    fill_obj = bpy.data.objects.new('FillLight', fill_data)
    bpy.context.collection.objects.link(fill_obj)
    fill_obj.location = (-3, 4, -1)

    rim_data = bpy.data.lights.new('RimLight', type='POINT')
    rim_data.energy = 600.0
    rim_data.color = (0.5, 0.95, 1.0)
    rim_obj = bpy.data.objects.new('RimLight', rim_data)
    bpy.context.collection.objects.link(rim_obj)
    rim_obj.location = (3, -4, 0)

def build_hammerhead_mesh():
    mesh = bpy.data.meshes.new("Mesh_Shark_Hammerhead")
    bm = bmesh.new()

    # Escala general
    scale_z = 1.25  # Longitudinal (Forward: +Z)
    scale_y = 1.15  # Vertical (Up: +Y)
    scale_x = 1.10  # Lateral (Right/Left: X)

    # 1. Anillos fusiformes del cuerpo (Z = posición a lo largo del cuerpo)
    rings_def = [
        # (z_pos, radius_x, radius_y, y_offset)
        ( 1.10 * scale_z, 0.38 * scale_x, 0.22 * scale_y, -0.04), # Branquias y base del martillo
        ( 0.75 * scale_z, 0.52 * scale_x, 0.44 * scale_y, -0.02), # Pecho anterior
        ( 0.25 * scale_z, 0.58 * scale_x, 0.52 * scale_y,  0.00), # Espalda / Aleta dorsal
        (-0.35 * scale_z, 0.48 * scale_x, 0.44 * scale_y,  0.00), # Vientre medio
        (-0.90 * scale_z, 0.35 * scale_x, 0.32 * scale_y,  0.01), # Pre-cola
        (-1.45 * scale_z, 0.22 * scale_x, 0.20 * scale_y,  0.01), # Pedúnculo caudal
        (-1.85 * scale_z, 0.09 * scale_x, 0.12 * scale_y,  0.00), # Base de cola
    ]

    segments = 16
    ring_verts = []
    for rz, rx, ry, yoff in rings_def:
        cur_ring = []
        for s in range(segments):
            angle = (s / segments) * 2 * math.pi
            x = math.sin(angle) * rx
            y = (math.cos(angle) * ry) + yoff
            v = bm.verts.new((x, y, rz))
            cur_ring.append(v)
        ring_verts.append(cur_ring)

    # Conectar anillos
    for r in range(len(rings_def) - 1):
        r1 = ring_verts[r]
        r2 = ring_verts[r + 1]
        for s in range(segments):
            s_next = (s + 1) % segments
            bm.faces.new((r1[s], r2[s], r2[s_next], r1[s_next]))

    # Cierre de cola
    tail_tip = bm.verts.new((0.0, 0.0, -1.92 * scale_z))
    for s in range(segments):
        s_next = (s + 1) % segments
        bm.faces.new((tail_tip, ring_verts[-1][s], ring_verts[-1][s_next]))

    # 2. Cefalofoil en T (Martillo en +Z)
    cf_center_front = bm.verts.new((0.0, -0.02 * scale_y,  1.65 * scale_z))
    cf_center_top   = bm.verts.new((0.0,  0.15 * scale_y,  1.45 * scale_z))
    cf_center_bot   = bm.verts.new((0.0, -0.18 * scale_y,  1.35 * scale_z))

    # Lóbulo Izquierdo (+X)
    cf_tip_l_f = bm.verts.new(( 1.32 * scale_x, -0.02 * scale_y,  1.48 * scale_z))
    cf_tip_l_b = bm.verts.new(( 1.38 * scale_x, -0.02 * scale_y,  1.22 * scale_z))
    cf_tip_l_t = bm.verts.new(( 1.30 * scale_x,  0.12 * scale_y,  1.35 * scale_z))
    cf_tip_l_u = bm.verts.new(( 1.30 * scale_x, -0.16 * scale_y,  1.32 * scale_z))

    # Lóbulo Derecho (-X)
    cf_tip_r_f = bm.verts.new((-1.32 * scale_x, -0.02 * scale_y,  1.48 * scale_z))
    cf_tip_r_b = bm.verts.new((-1.38 * scale_x, -0.02 * scale_y,  1.22 * scale_z))
    cf_tip_r_t = bm.verts.new((-1.30 * scale_x,  0.12 * scale_y,  1.35 * scale_z))
    cf_tip_r_u = bm.verts.new((-1.30 * scale_x, -0.16 * scale_y,  1.32 * scale_z))

    neck_ring = ring_verts[0]
    # Caras frontales y dorsales del martillo
    bm.faces.new((cf_center_top, cf_tip_l_t, cf_tip_l_f, cf_center_front))
    bm.faces.new((cf_center_top, cf_center_front, cf_tip_r_f, cf_tip_r_t))
    bm.faces.new((cf_center_front, cf_tip_l_f, cf_tip_l_u, cf_center_bot))
    bm.faces.new((cf_center_front, cf_center_bot, cf_tip_r_u, cf_tip_r_f))
    # Tapas laterales lóbulos
    bm.faces.new((cf_tip_l_f, cf_tip_l_t, cf_tip_l_b, cf_tip_l_u))
    bm.faces.new((cf_tip_r_f, cf_tip_r_u, cf_tip_r_b, cf_tip_r_t))
    # Caras posteriores hacia el cuello
    bm.faces.new((cf_center_top, neck_ring[0], neck_ring[4], cf_tip_l_t))
    bm.faces.new((cf_center_top, cf_tip_r_t, neck_ring[12], neck_ring[0]))
    bm.faces.new((cf_tip_l_t, neck_ring[4], neck_ring[6], cf_tip_l_b))
    bm.faces.new((cf_tip_r_t, cf_tip_r_b, neck_ring[10], neck_ring[12]))
    bm.faces.new((cf_tip_l_u, cf_tip_l_b, neck_ring[6], neck_ring[8]))
    bm.faces.new((cf_tip_r_u, neck_ring[8], neck_ring[10], cf_tip_r_b))
    bm.faces.new((cf_center_bot, cf_tip_l_u, neck_ring[8]))
    bm.faces.new((cf_center_bot, neck_ring[8], cf_tip_r_u))

    # 3. Ojos ambarinos en los extremos laterales del martillo
    eye_l = bm.verts.new(( 1.39 * scale_x, 0.0, 1.36 * scale_z))
    eye_r = bm.verts.new((-1.39 * scale_x, 0.0, 1.36 * scale_z))

    # 4. Aleta Dorsal Alta (+Y)
    d_base_f = bm.verts.new((0.0,  0.52 * scale_y,  0.50 * scale_z))
    d_tip    = bm.verts.new((0.0,  1.55 * scale_y, -0.15 * scale_z))
    d_base_b = bm.verts.new((0.0,  0.44 * scale_y, -0.45 * scale_z))
    d_notch  = bm.verts.new((0.0,  0.62 * scale_y, -0.30 * scale_z))
    d_l      = bm.verts.new(( 0.04 * scale_x,  0.85 * scale_y, -0.05 * scale_z))
    d_r      = bm.verts.new((-0.04 * scale_x,  0.85 * scale_y, -0.05 * scale_z))

    bm.faces.new((d_base_f, d_tip, d_l))
    bm.faces.new((d_tip, d_notch, d_l))
    bm.faces.new((d_notch, d_base_b, d_l))
    bm.faces.new((d_base_f, d_l, d_base_b))
    bm.faces.new((d_base_f, d_r, d_tip))
    bm.faces.new((d_tip, d_r, d_notch))
    bm.faces.new((d_notch, d_r, d_base_b))
    bm.faces.new((d_base_f, d_base_b, d_r))

    # 5. Aleta Caudal Heterocerca en -Z
    c_root_t = bm.verts.new((0.0,  0.12 * scale_y, -1.80 * scale_z))
    c_root_b = bm.verts.new((0.0, -0.12 * scale_y, -1.80 * scale_z))
    c_top    = bm.verts.new((0.0,  1.20 * scale_y, -2.65 * scale_z))
    c_notch  = bm.verts.new((0.0,  0.88 * scale_y, -2.45 * scale_z))
    c_mid    = bm.verts.new((0.0,  0.08 * scale_y, -2.15 * scale_z))
    c_bot    = bm.verts.new((0.0, -0.65 * scale_y, -2.42 * scale_z))
    c_l      = bm.verts.new(( 0.03 * scale_x,  0.15 * scale_y, -2.05 * scale_z))
    c_r      = bm.verts.new((-0.03 * scale_x,  0.15 * scale_y, -2.05 * scale_z))

    bm.faces.new((c_root_t, c_top, c_l))
    bm.faces.new((c_top, c_notch, c_l))
    bm.faces.new((c_notch, c_mid, c_l))
    bm.faces.new((c_mid, c_bot, c_l))
    bm.faces.new((c_bot, c_root_b, c_l))
    bm.faces.new((c_root_t, c_l, c_root_b))

    bm.faces.new((c_root_t, c_r, c_top))
    bm.faces.new((c_top, c_r, c_notch))
    bm.faces.new((c_notch, c_r, c_mid))
    bm.faces.new((c_mid, c_r, c_bot))
    bm.faces.new((c_bot, c_r, c_root_b))
    bm.faces.new((c_root_t, c_root_b, c_r))

    # 6. Aletas Pectorales (Izquierda +X, Derecha -X)
    p_l_root_f = bm.verts.new(( 0.50 * scale_x, -0.22 * scale_y,  0.55 * scale_z))
    p_l_root_b = bm.verts.new(( 0.52 * scale_x, -0.25 * scale_y,  0.15 * scale_z))
    p_l_tip    = bm.verts.new(( 1.45 * scale_x, -0.75 * scale_y, -0.10 * scale_z))
    p_l_mid    = bm.verts.new(( 1.05 * scale_x, -0.45 * scale_y,  0.25 * scale_z))
    bm.faces.new((p_l_root_f, p_l_mid, p_l_root_b))
    bm.faces.new((p_l_root_f, p_l_tip, p_l_mid))
    bm.faces.new((p_l_mid, p_l_tip, p_l_root_b))

    p_r_root_f = bm.verts.new((-0.50 * scale_x, -0.22 * scale_y,  0.55 * scale_z))
    p_r_root_b = bm.verts.new((-0.52 * scale_x, -0.25 * scale_y,  0.15 * scale_z))
    p_r_tip    = bm.verts.new((-1.45 * scale_x, -0.75 * scale_y, -0.10 * scale_z))
    p_r_mid    = bm.verts.new((-1.05 * scale_x, -0.45 * scale_y,  0.25 * scale_z))
    bm.faces.new((p_r_root_f, p_r_root_b, p_r_mid))
    bm.faces.new((p_r_root_f, p_r_mid, p_r_tip))
    bm.faces.new((p_r_mid, p_r_root_b, p_r_tip))

    # 7. Mandíbula Voraz Inferior con Dientes
    jaw_root = bm.verts.new((0.0, -0.28 * scale_y,  0.95 * scale_z))
    jaw_tip  = bm.verts.new((0.0, -0.25 * scale_y,  1.30 * scale_z))
    jaw_l    = bm.verts.new(( 0.30 * scale_x, -0.26 * scale_y,  1.15 * scale_z))
    jaw_r    = bm.verts.new((-0.30 * scale_x, -0.26 * scale_y,  1.15 * scale_z))
    bm.faces.new((jaw_root, jaw_l, jaw_tip))
    bm.faces.new((jaw_root, jaw_tip, jaw_r))
    bm.faces.new((jaw_l, jaw_root, jaw_r))

    # Dientes en arco
    for i in range(5):
        t_ang = (i - 2) * 0.12
        tx = math.sin(t_ang) * 0.26 * scale_x
        tz = (1.28 - abs(t_ang) * 0.15) * scale_z
        t_base = bm.verts.new((tx, -0.25 * scale_y, tz))
        t_apex = bm.verts.new((tx, -0.16 * scale_y, tz + 0.05))
        t_side = bm.verts.new((tx + 0.03, -0.25 * scale_y, tz))
        bm.faces.new((t_base, t_apex, t_side))

    bm.verts.ensure_lookup_table()
    bm.faces.ensure_lookup_table()
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)

    bm.to_mesh(mesh)
    bm.free()

    obj = bpy.data.objects.new("Shark_hammerhead", mesh)
    bpy.context.collection.objects.link(obj)

    for poly in mesh.polygons:
        poly.use_smooth = True

    return obj

def build_hammerhead_armature(shark_obj):
    arm_data = bpy.data.armatures.new("ArmatureData_hammerhead")
    arm_obj = bpy.data.objects.new("Armature_hammerhead", arm_data)
    bpy.context.collection.objects.link(arm_obj)

    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode='EDIT')

    # Cadena vertebral a lo largo de Z (Tail hacia -Z, Head hacia +Z)
    b_root = arm_data.edit_bones.new("Root")
    b_root.head = (0.0, 0.0, 0.0)
    b_root.tail = (0.0, 0.0, -0.35)

    b_spine1 = arm_data.edit_bones.new("Spine_01")
    b_spine1.head = (0.0, 0.0, 0.0)
    b_spine1.tail = (0.0, 0.0, -0.6)
    b_spine1.parent = b_root

    b_spine2 = arm_data.edit_bones.new("Spine_02")
    b_spine2.head = (0.0, 0.0, -0.6)
    b_spine2.tail = (0.0, 0.0, -1.15)
    b_spine2.parent = b_spine1

    b_tail1 = arm_data.edit_bones.new("Tail_01")
    b_tail1.head = (0.0, 0.0, -1.15)
    b_tail1.tail = (0.0, 0.0, -1.7)
    b_tail1.parent = b_spine2

    b_tail2 = arm_data.edit_bones.new("Tail_02")
    b_tail2.head = (0.0, 0.0, -1.7)
    b_tail2.tail = (0.0, 0.0, -2.2)
    b_tail2.parent = b_tail1

    b_fin = arm_data.edit_bones.new("CaudalFin")
    b_fin.head = (0.0, 0.0, -2.2)
    b_fin.tail = (0.0, 0.0, -2.9)
    b_fin.parent = b_tail2

    # Cabeza y Mandíbula hacia +Z
    b_head = arm_data.edit_bones.new("Head")
    b_head.head = (0.0, 0.0, 0.0)
    b_head.tail = (0.0, 0.0, 1.5)
    b_head.parent = b_root

    b_jaw = arm_data.edit_bones.new("Jaw_Lower")
    b_jaw.head = (0.0, -0.28, 0.9)
    b_jaw.tail = (0.0, -0.25, 1.4)
    b_jaw.parent = b_head

    # Aletas pectorales
    b_pec_l = arm_data.edit_bones.new("Pectoral_L")
    b_pec_l.head = ( 0.45, -0.25, 0.4)
    b_pec_l.tail = ( 1.35, -0.65, 0.0)
    b_pec_l.parent = b_root

    b_pec_r = arm_data.edit_bones.new("Pectoral_R")
    b_pec_r.head = (-0.45, -0.25, 0.4)
    b_pec_r.tail = (-1.35, -0.65, 0.0)
    b_pec_r.parent = b_root

    bpy.ops.object.mode_set(mode='OBJECT')

    # Grupos de vértices y asignación de pesos
    mesh = shark_obj.data
    bones = ["Root", "Spine_01", "Spine_02", "Tail_01", "Tail_02", "CaudalFin", "Head", "Jaw_Lower", "Pectoral_L", "Pectoral_R"]
    for bname in bones:
        shark_obj.vertex_groups.new(name=bname)

    vg_root = shark_obj.vertex_groups["Root"]
    vg_spine1 = shark_obj.vertex_groups["Spine_01"]
    vg_spine2 = shark_obj.vertex_groups["Spine_02"]
    vg_tail1 = shark_obj.vertex_groups["Tail_01"]
    vg_tail2 = shark_obj.vertex_groups["Tail_02"]
    vg_fin = shark_obj.vertex_groups["CaudalFin"]
    vg_head = shark_obj.vertex_groups["Head"]
    vg_jaw = shark_obj.vertex_groups["Jaw_Lower"]
    vg_pecl = shark_obj.vertex_groups["Pectoral_L"]
    vg_pecr = shark_obj.vertex_groups["Pectoral_R"]

    for v in mesh.vertices:
        x, y, z = v.co.x, v.co.y, v.co.z

        # Pectorales
        if x > 0.55 and z > -0.2 and z < 0.8:
            vg_pecl.add([v.index], 0.9, 'REPLACE')
            continue
        elif x < -0.55 and z > -0.2 and z < 0.8:
            vg_pecr.add([v.index], 0.9, 'REPLACE')
            continue

        # Cabeza y mandíbula (+Z)
        if z > 0.7:
            if y < -0.20 and z > 0.95 and abs(x) < 0.45:
                vg_jaw.add([v.index], 0.9, 'REPLACE')
            else:
                vg_head.add([v.index], 0.95, 'REPLACE')
        elif z > 0.0:
            w_h = z / 0.7
            vg_head.add([v.index], w_h, 'REPLACE')
            vg_root.add([v.index], 1.0 - w_h, 'REPLACE')
        elif z > -0.6:
            w_s1 = -z / 0.6
            vg_spine1.add([v.index], w_s1, 'REPLACE')
            vg_root.add([v.index], 1.0 - w_s1, 'REPLACE')
        elif z > -1.2:
            w_s2 = (-z - 0.6) / 0.6
            vg_spine2.add([v.index], w_s2, 'REPLACE')
            vg_spine1.add([v.index], 1.0 - w_s2, 'REPLACE')
        elif z > -1.8:
            w_t1 = (-z - 1.2) / 0.6
            vg_tail1.add([v.index], w_t1, 'REPLACE')
            vg_spine2.add([v.index], 1.0 - w_t1, 'REPLACE')
        elif z > -2.3:
            w_t2 = (-z - 1.8) / 0.5
            vg_tail2.add([v.index], w_t2, 'REPLACE')
            vg_tail1.add([v.index], 1.0 - w_t2, 'REPLACE')
        else:
            vg_fin.add([v.index], 1.0, 'REPLACE')

    mod = shark_obj.modifiers.new(name="ArmatureModifier", type='ARMATURE')
    mod.object = arm_obj
    mod.use_vertex_groups = True

    return arm_obj

def apply_materials(shark_obj):
    import random
    random.seed(42)
    mesh = shark_obj.data
    
    # Create Vertex Colors for Unity export compatibility
    if not mesh.color_attributes:
        color_layer = mesh.color_attributes.new(name="Col", type='BYTE_COLOR', domain='CORNER')
    else:
        color_layer = mesh.color_attributes.active
        
    top_col = (0.35, 0.45, 0.50, 1.0)    # Gris azulado oscuro (lomo)
    belly_col = (0.92, 0.90, 0.85, 1.0)  # Crema/blanco (vientre)
    cyan_col = (0.35, 0.91, 1.0, 1.0)   # Celeste brillante (#59E7FF para marcas)

    # Assign colors to corners (loops)
    for poly in mesh.polygons:
        for loop_index in poly.loop_indices:
            v_idx = mesh.loops[loop_index].vertex_index
            v = mesh.vertices[v_idx]
            
            y_norm = (v.co.y + 0.35) / 0.85
            y_norm = max(0.0, min(1.0, y_norm))
            
            r = belly_col[0] * (1 - y_norm) + top_col[0] * y_norm
            g = belly_col[1] * (1 - y_norm) + top_col[1] * y_norm
            b = belly_col[2] * (1 - y_norm) + top_col[2] * y_norm
            
            # Simular marcas bioluminiscentes (rayas/puntos cyan) en el lomo
            if y_norm > 0.8 and random.random() > 0.85:
                r, g, b = cyan_col[0], cyan_col[1], cyan_col[2]
                
            color_layer.data[loop_index].color = (r, g, b, 1.0)

    # Create Material using Vertex Color
    mat = bpy.data.materials.new(name="Mat_Shark_Hammerhead")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()

    output = nodes.new('ShaderNodeOutputMaterial')
    bsdf = nodes.new('ShaderNodeBsdfPrincipled')
    vcol = nodes.new('ShaderNodeVertexColor')
    vcol.layer_name = "Col"

    links.new(vcol.outputs['Color'], bsdf.inputs['Base Color'])
    
    bsdf.inputs['Roughness'].default_value = 0.25
    links.new(bsdf.outputs['BSDF'], output.inputs['Surface'])
    shark_obj.data.materials.append(mat)

def animate(arm_obj):
    scene = bpy.context.scene
    scene.render.fps = 30
    scene.frame_start = 1
    scene.frame_end = 30

    if not arm_obj.animation_data:
        arm_obj.animation_data_create()

    # 1. Resetear todos los pose bones a reposo limpio (rotaciones 0)
    for pb in arm_obj.pose.bones:
        pb.rotation_mode = 'XYZ'
        pb.rotation_euler = (0.0, 0.0, 0.0)

    # 2. Animación: Swim
    # Vaivén LATERAL en eje Z local (perpendicular al hueso y al lomo).
    # Torsión Y local = 0° en todos los huesos.
    # Cabeceo X local = 0° en todos los huesos.
    # Spine_01 ±4°, Spine_02 ±8°, Tail_01 ±14°, Tail_02 ±20°, CaudalFin ±28°
    # Retraso de 3 frames entre hueso y hueso.
    # Head: ±3° lateral en sentido contrario (contrapeso).
    # Ciclo de 30 frames a 30 fps, loop perfecto (frame 30 idéntico a frame 1).
    action_swim = bpy.data.actions.new(name="Swim")
    arm_obj.animation_data.action = action_swim

    bones_config = [
        # (name, amp_deg, delay_frames, sign)
        ("Spine_01",  4.0,  0.0, +1.0),
        ("Spine_02",  8.0,  3.0, +1.0),
        ("Tail_01",  14.0,  6.0, +1.0),
        ("Tail_02",  20.0,  9.0, +1.0),
        ("CaudalFin", 28.0, 12.0, +1.0),
        ("Head",      3.0,  0.0, -1.0), # Contrapeso lateral opuesto
    ]

    omega = (2.0 * math.pi) / 29.0 # 29 intervalos para que frame 30 sea exactamente igual a frame 1

    for frame in range(1, 31):
        scene.frame_set(frame)
        for bname, amp_deg, delay, sign in bones_config:
            pbone = arm_obj.pose.bones.get(bname)
            if pbone:
                amp_rad = math.radians(amp_deg)
                phase = (frame - 1.0 - delay) * omega
                angle = sign * amp_rad * math.sin(phase)
                pbone.rotation_mode = 'XYZ'
                # Eje Z local: vaivén lateral (guiñada izquierda-derecha).
                # Eje Y local: 0.0 (cero torsión/roll). Eje X local: 0.0 (cero pitch).
                pbone.rotation_euler = (0.0, 0.0, angle)
                pbone.keyframe_insert(data_path="rotation_euler", frame=frame)

        p_root = arm_obj.pose.bones.get("Root")
        if p_root:
            p_root.rotation_mode = 'XYZ'
            p_root.rotation_euler = (0.0, 0.0, 0.0)
            p_root.keyframe_insert(data_path="rotation_euler", frame=frame)

        ppec_l = arm_obj.pose.bones.get("Pectoral_L")
        ppec_r = arm_obj.pose.bones.get("Pectoral_R")
        if ppec_l and ppec_r:
            t_pec = (frame - 1.0) * omega
            pec_angle = math.sin(t_pec) * math.radians(2.0)
            ppec_l.rotation_mode = 'XYZ'
            ppec_l.rotation_euler = (0.0, 0.0, pec_angle)
            ppec_l.keyframe_insert(data_path="rotation_euler", frame=frame)
            ppec_r.rotation_mode = 'XYZ'
            ppec_r.rotation_euler = (0.0, 0.0, -pec_angle)
            ppec_r.keyframe_insert(data_path="rotation_euler", frame=frame)

    # 3. Animación: Bite (Mordisco Voraz: rotación en X)
    action_bite = bpy.data.actions.new(name="Bite")
    arm_obj.animation_data.action = action_bite
    pjaw = arm_obj.pose.bones.get("Jaw_Lower")
    phead = arm_obj.pose.bones.get("Head")

    if pjaw and phead:
        pjaw.rotation_mode = 'XYZ'
        phead.rotation_mode = 'XYZ'
        bite_keys = [
            (1,  0.0,   0.0),
            (6, -0.55,  0.15),
            (11, 0.05, -0.05),
            (18, 0.0,   0.0),
        ]
        for f, j_rot, h_rot in bite_keys:
            pjaw.rotation_euler = (j_rot, 0.0, 0.0)
            pjaw.keyframe_insert(data_path="rotation_euler", frame=f)
            phead.rotation_euler = (h_rot, 0.0, 0.0)
            phead.keyframe_insert(data_path="rotation_euler", frame=f)

    # Restablecer pose de reposo limpia (torsión Y = 0° en todos los huesos)
    arm_obj.animation_data.action = action_swim
    scene.frame_set(1)
    for pb in arm_obj.pose.bones:
        pb.rotation_mode = 'XYZ'
        pb.rotation_euler = (0.0, 0.0, 0.0)

def main():
    print("--- Generando Tiburón Martillo 3D para D:\\ECO SHARK (+Z Forward) ---")
    reset_scene()
    setup_studio_lighting()

    shark_obj = build_hammerhead_mesh()
    arm_obj = build_hammerhead_armature(shark_obj)
    apply_materials(shark_obj)
    animate(arm_obj)

    # Exportar a Unity
    fbx_target = os.path.join(TARGET_DIR, "Shark_Hammerhead.fbx")
    blend_target = os.path.join(TARGET_DIR, "Shark_Hammerhead.blend")
    obj_target = os.path.join(TARGET_DIR, "Shark_Hammerhead.obj")

    # Guardar .blend
    bpy.ops.wm.save_as_mainfile(filepath=blend_target)
    print("Guardado BLEND en Unity:", blend_target)

    # Exportar FBX con Primary Bone Axis Y, Secondary X, Add Leaf Bones desactivado, Bake Animation activado
    bpy.ops.object.select_all(action='DESELECT')
    arm_obj.select_set(True)
    shark_obj.select_set(True)
    bpy.context.view_layer.objects.active = arm_obj

    bpy.ops.export_scene.fbx(
        filepath=fbx_target,
        use_selection=True,
        bake_anim=True,
        bake_anim_use_all_actions=True,
        bake_anim_step=1.0,
        bake_anim_simplify_factor=0.0,
        add_leaf_bones=False,
        primary_bone_axis='Y',
        secondary_bone_axis='X',
        armature_nodetype='NULL',
        axis_forward='Z',
        axis_up='Y'
    )
    print("Exportado FBX en Unity:", fbx_target)

    bpy.ops.wm.obj_export(filepath=obj_target, export_selected_objects=True)
    print("Exportado OBJ en Unity:", obj_target)

    # Copiar también a Descargas para mantener sincronizado
    shutil.copy2(fbx_target, os.path.join(DOWNLOADS_DIR, "Shark_Hammerhead_3D.fbx"))
    shutil.copy2(blend_target, os.path.join(DOWNLOADS_DIR, "Shark_Hammerhead_3D.blend"))
    shutil.copy2(obj_target, os.path.join(DOWNLOADS_DIR, "Shark_Hammerhead_3D.obj"))
    print("Copia sincronizada en Descargas.")

if __name__ == "__main__":
    main()
