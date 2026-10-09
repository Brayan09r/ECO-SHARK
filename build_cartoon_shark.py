"""
Generador completo del nuevo modelo 3D Cartoon del Tiburón Martillo para ECO SHARK.
"""

import bpy
import bmesh
import math
import os
import time
import numpy as np

OUTPUT_DIR = r"D:\ECO SHARK\Assets\EcoShark\Models\Sharks"
TEXTURE_DIR = os.path.join(OUTPUT_DIR, "Textures")
DESIGNS_DIR = r"D:\ECO SHARK\Assets\EcoShark\Art\Designs"

os.makedirs(TEXTURE_DIR, exist_ok=True)
os.makedirs(DESIGNS_DIR, exist_ok=True)

def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.meshes):
        bpy.data.meshes.remove(m, do_unlink=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat, do_unlink=True)
    for act in list(bpy.data.actions):
        bpy.data.actions.remove(act, do_unlink=True)
    for img in list(bpy.data.images):
        bpy.data.images.remove(img, do_unlink=True)

    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = 16
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = 'PNG'

def build_cartoon_shark():
    mesh = bpy.data.meshes.new("Mesh_Shark_Hammerhead_Cartoon")
    bm = bmesh.new()

    # 1. CUERPO FUSIFORME GORDITO Y CARTOON
    # Nose is towards -Y, Tail is towards +Y, Up is +Z, Right is +X.
    # Total length ≈ 3.0 units.
    body_rings = [
        # (y_pos, rx, rz, z_offset)
        (-0.75, 0.60, 0.44,  0.02), # Cuello
        (-0.45, 0.70, 0.54,  0.03), # Pecho anterior
        (-0.10, 0.74, 0.60,  0.02), # Vientre gordito (máxima anchura)
        ( 0.25, 0.68, 0.55, -0.01), # Espalda
        ( 0.60, 0.54, 0.45, -0.02), # Pélvica
        ( 0.95, 0.38, 0.34, -0.01), # Pre-cola
        ( 1.25, 0.20, 0.24,  0.00), # Pedúnculo
        ( 1.42, 0.08, 0.18,  0.00), # Raíz de cola
    ]

    segments = 16
    ring_verts = []
    for ry, rx, rz, zoff in body_rings:
        cur_ring = []
        for s in range(segments):
            angle = (s / segments) * 2 * math.pi
            x = math.cos(angle) * rx
            z = math.sin(angle) * rz + zoff
            v = bm.verts.new((x, ry, z))
            cur_ring.append(v)
        ring_verts.append(cur_ring)

    for r in range(len(body_rings) - 1):
        r1 = ring_verts[r]
        r2 = ring_verts[r + 1]
        for s in range(segments):
            s_next = (s + 1) % segments
            bm.faces.new((r1[s], r1[s_next], r2[s_next], r2[s]))

    peduncle_cap = bm.verts.new((0.0, 1.46, 0.0))
    for s in range(segments):
        s_next = (s + 1) % segments
        bm.faces.new((ring_verts[-1][s_next], ring_verts[-1][s], peduncle_cap))

    # 2. CEFALOFOIL (MARTILLO ANCHO Y REDONDEADO EN -Y)
    cf_center_front = bm.verts.new(( 0.00, -1.35,  0.06))
    cf_center_top   = bm.verts.new(( 0.00, -1.15,  0.28))
    cf_center_bot   = bm.verts.new(( 0.00, -1.15, -0.16))

    cf_r_f = bm.verts.new(( 1.20, -1.15,  0.06))
    cf_r_b = bm.verts.new(( 1.24, -0.92,  0.06))
    cf_r_t = bm.verts.new(( 1.16, -1.04,  0.22))
    cf_r_u = bm.verts.new(( 1.16, -1.04, -0.12))
    cf_r_tip = bm.verts.new(( 1.28, -1.04, 0.05))

    cf_l_f = bm.verts.new((-1.20, -1.15,  0.06))
    cf_l_b = bm.verts.new((-1.24, -0.92,  0.06))
    cf_l_t = bm.verts.new((-1.16, -1.04,  0.22))
    cf_l_u = bm.verts.new((-1.16, -1.04, -0.12))
    cf_l_tip = bm.verts.new((-1.28, -1.04, 0.05))

    cf_mid_rf = bm.verts.new(( 0.65, -1.28,  0.06))
    cf_mid_rt = bm.verts.new(( 0.62, -1.12,  0.25))
    cf_mid_ru = bm.verts.new(( 0.62, -1.12, -0.14))

    cf_mid_lf = bm.verts.new((-0.65, -1.28,  0.06))
    cf_mid_lt = bm.verts.new((-0.62, -1.12,  0.25))
    cf_mid_lu = bm.verts.new((-0.62, -1.12, -0.14))

    bm.faces.new((cf_center_top, cf_center_front, cf_mid_rf, cf_mid_rt))
    bm.faces.new((cf_mid_rt, cf_mid_rf, cf_r_f, cf_r_t))
    bm.faces.new((cf_center_front, cf_center_top, cf_mid_lt, cf_mid_lf))
    bm.faces.new((cf_mid_lf, cf_mid_lt, cf_l_t, cf_l_f))

    bm.faces.new((cf_center_front, cf_center_bot, cf_mid_ru, cf_mid_rf))
    bm.faces.new((cf_mid_rf, cf_mid_ru, cf_r_u, cf_r_f))
    bm.faces.new((cf_center_bot, cf_center_front, cf_mid_lf, cf_mid_lu))
    bm.faces.new((cf_mid_lu, cf_mid_lf, cf_l_f, cf_l_u))

    bm.faces.new((cf_r_t, cf_r_f, cf_r_tip))
    bm.faces.new((cf_r_f, cf_r_u, cf_r_tip))
    bm.faces.new((cf_r_u, cf_r_b, cf_r_tip))
    bm.faces.new((cf_r_b, cf_r_t, cf_r_tip))

    bm.faces.new((cf_l_f, cf_l_t, cf_l_tip))
    bm.faces.new((cf_l_u, cf_l_f, cf_l_tip))
    bm.faces.new((cf_l_b, cf_l_u, cf_l_tip))
    bm.faces.new((cf_l_t, cf_l_b, cf_l_tip))

    neck = ring_verts[0]
    bm.faces.new((cf_center_top, cf_mid_rt, neck[4], neck[0]))
    bm.faces.new((cf_mid_rt, cf_r_t, cf_r_b, neck[4]))
    bm.faces.new((cf_center_top, neck[0], neck[12], cf_mid_lt))
    bm.faces.new((cf_mid_lt, neck[12], cf_l_b, cf_l_t))

    bm.faces.new((cf_center_bot, neck[8], neck[4], cf_r_b))
    bm.faces.new((cf_center_bot, cf_r_b, cf_r_u, cf_mid_ru))
    bm.faces.new((cf_center_bot, cf_mid_lu, cf_l_u, cf_l_b))
    bm.faces.new((cf_center_bot, cf_l_b, neck[12], neck[8]))

    # 3. ALETA DORSAL ALTA Y CURVA (+Z)
    d_base_f = bm.verts.new(( 0.00, -0.15,  0.60))
    d_base_b = bm.verts.new(( 0.00,  0.40,  0.54))
    d_tip    = bm.verts.new(( 0.00,  0.15,  1.34)) # Punta alta y redondeada
    d_notch  = bm.verts.new(( 0.00,  0.32,  0.74))
    d_r      = bm.verts.new(( 0.06,  0.08,  0.86))
    d_l      = bm.verts.new((-0.06,  0.08,  0.86))

    bm.faces.new((d_base_f, d_tip, d_r))
    bm.faces.new((d_tip, d_notch, d_r))
    bm.faces.new((d_notch, d_base_b, d_r))
    bm.faces.new((d_base_f, d_r, d_base_b))

    bm.faces.new((d_base_f, d_l, d_tip))
    bm.faces.new((d_tip, d_l, d_notch))
    bm.faces.new((d_notch, d_l, d_base_b))
    bm.faces.new((d_base_f, d_base_b, d_l))

    # 4. ALETA CAUDAL VERTICAL EN FORMA DE MEDIA LUNA (+Y)
    c_root_t = bm.verts.new(( 0.00,  1.32,  0.22))
    c_root_b = bm.verts.new(( 0.00,  1.32, -0.22))
    c_top    = bm.verts.new(( 0.00,  1.68,  0.90))
    c_notch  = bm.verts.new(( 0.00,  1.52,  0.08))
    c_bot    = bm.verts.new(( 0.00,  1.62, -0.60))
    c_r      = bm.verts.new(( 0.04,  1.42,  0.08))
    c_l      = bm.verts.new((-0.04,  1.42,  0.08))

    bm.faces.new((c_root_t, c_top, c_r))
    bm.faces.new((c_top, c_notch, c_r))
    bm.faces.new((c_notch, c_bot, c_r))
    bm.faces.new((c_bot, c_root_b, c_r))
    bm.faces.new((c_root_t, c_r, c_root_b))

    bm.faces.new((c_root_t, c_l, c_top))
    bm.faces.new((c_top, c_l, c_notch))
    bm.faces.new((c_notch, c_l, c_bot))
    bm.faces.new((c_bot, c_l, c_root_b))
    bm.faces.new((c_root_t, c_root_b, c_l))

    # 5. ALETAS PECTORALES (Paddle wings)
    p_r_f = bm.verts.new(( 0.55, -0.35, -0.10))
    p_r_b = bm.verts.new(( 0.60,  0.05, -0.15))
    p_r_tip = bm.verts.new(( 1.35, -0.05, -0.42))
    p_r_mid = bm.verts.new(( 1.05, -0.22, -0.24))
    bm.faces.new((p_r_f, p_r_mid, p_r_b))
    bm.faces.new((p_r_f, p_r_tip, p_r_mid))
    bm.faces.new((p_r_mid, p_r_tip, p_r_b))

    p_l_f = bm.verts.new((-0.55, -0.35, -0.10))
    p_l_b = bm.verts.new((-0.60,  0.05, -0.15))
    p_l_tip = bm.verts.new((-1.35, -0.05, -0.42))
    p_l_mid = bm.verts.new((-1.05, -0.22, -0.24))
    bm.faces.new((p_l_f, p_l_b, p_l_mid))
    bm.faces.new((p_l_f, p_l_mid, p_l_tip))
    bm.faces.new((p_l_mid, p_l_b, p_l_tip))

    # 6. OJOS GRANDES Y EXPRESIVOS (Domes / spheres on the hammer tips)
    eye_radius = 0.17
    eye_center_r = (1.24, -1.04, 0.08)
    eye_rings = 4
    eye_segs = 8
    eye_r_verts = []
    for er in range(eye_rings + 1):
        theta = (er / eye_rings) * math.pi
        cur_ering = []
        for es in range(eye_segs):
            phi = (es / eye_segs) * 2 * math.pi
            ex = eye_center_r[0] + eye_radius * math.sin(theta) * math.cos(phi)
            ey = eye_center_r[1] + eye_radius * math.sin(theta) * math.sin(phi)
            ez = eye_center_r[2] + eye_radius * math.cos(theta)
            v = bm.verts.new((ex, ey, ez))
            cur_ering.append(v)
        eye_r_verts.append(cur_ering)

    for er in range(eye_rings):
        for es in range(eye_segs):
            es_next = (es + 1) % eye_segs
            bm.faces.new((eye_r_verts[er][es], eye_r_verts[er][es_next], eye_r_verts[er+1][es_next], eye_r_verts[er+1][es]))

    eye_center_l = (-1.24, -1.04, 0.08)
    eye_l_verts = []
    for er in range(eye_rings + 1):
        theta = (er / eye_rings) * math.pi
        cur_ering = []
        for es in range(eye_segs):
            phi = (es / eye_segs) * 2 * math.pi
            ex = eye_center_l[0] + eye_radius * math.sin(theta) * math.cos(phi)
            ey = eye_center_l[1] + eye_radius * math.sin(theta) * math.sin(phi)
            ez = eye_center_l[2] + eye_radius * math.cos(theta)
            v = bm.verts.new((ex, ey, ez))
            cur_ering.append(v)
        eye_l_verts.append(cur_ering)

    for er in range(eye_rings):
        for es in range(eye_segs):
            es_next = (es + 1) % eye_segs
            bm.faces.new((eye_l_verts[er][es], eye_l_verts[er+1][es], eye_l_verts[er+1][es_next], eye_l_verts[er][es_next]))

    # 7. SONRISA AMABLE Y DIENTES PEQUEÑOS Y REDONDEADOS
    smile_f = bm.verts.new(( 0.00, -0.98, -0.19))
    smile_b = bm.verts.new(( 0.00, -0.78, -0.21))
    smile_r = bm.verts.new(( 0.22, -0.88, -0.16))
    smile_l = bm.verts.new((-0.22, -0.88, -0.16))
    bm.faces.new((smile_f, smile_r, smile_b))
    bm.faces.new((smile_f, smile_b, smile_l))

    for dt_i in range(3):
        dt_ang = (dt_i - 1) * 0.12
        dt_x = math.sin(dt_ang) * 0.15
        dt_y = -0.96 + abs(dt_ang) * 0.05
        t_b = bm.verts.new((dt_x, dt_y, -0.20))
        t_tip = bm.verts.new((dt_x, dt_y + 0.02, -0.15))
        t_w = bm.verts.new((dt_x + 0.025, dt_y, -0.20))
        bm.faces.new((t_b, t_tip, t_w))

    bm.verts.ensure_lookup_table()
    bm.faces.ensure_lookup_table()
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()

    obj = bpy.data.objects.new("Shark_Hammerhead_Cartoon", mesh)
    bpy.context.collection.objects.link(obj)

    # Subdivisión suave: Level 2 para curvas suaves y orgánicas (≈7400 triángulos)
    subsurf = obj.modifiers.new(name="Subsurf", type='SUBSURF')
    subsurf.levels = 2
    subsurf.render_levels = 2

    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier="Subsurf")

    for poly in mesh.polygons:
        poly.use_smooth = True

    mesh.calc_loop_triangles()
    print(f"Malla creada con {len(mesh.vertices)} vértices y {len(mesh.loop_triangles)} triángulos (3000-8000 WebGL target).")

    # Escalar largo para que sea exactamente ≈ 3.0 unidades
    bb = obj.bound_box
    ys = [p[1] for p in bb]
    cur_len = max(ys) - min(ys)
    scale_factor = 3.0 / cur_len
    obj.scale = (scale_factor, scale_factor, scale_factor)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    return obj

print("build_cartoon_shark function loaded.")
