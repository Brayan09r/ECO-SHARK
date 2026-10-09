"""
Generador completo y definitivo del Tiburón Martillo Cartoon para ECO SHARK.
Genera la malla 100% estanca, texturas Albedo y Emission (2048x2048),
exporta el FBX para Unity y renderiza las 3 vistas de revisión con encuadre completo.
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
    scene.cycles.samples = 32
    scene.render.film_transparent = False
    scene.render.image_settings.file_format = 'PNG'

def capsule_ring(W, H, z_off, N=32):
    pts = []
    p = 2.0 + 2.0 * max(0.0, (W - H) / W)
    for i in range(N):
        ang = (i / N) * 2.0 * np.pi
        ca = np.cos(ang)
        sa = np.sin(ang)
        sgn_c = np.sign(ca)
        sgn_s = np.sign(sa)
        x = W * sgn_c * (abs(ca) ** (2.0 / p))
        z = H * sgn_s * (abs(sa) ** (2.0 / p)) + z_off
        pts.append((float(x), float(z)))
    return pts

def build_cartoon_shark():
    mesh = bpy.data.meshes.new("Mesh_Shark_Hammerhead_Cartoon")
    bm = bmesh.new()

    # 1. CUERPO Y CABEZA CONTINUOS Y ESTANCOS (100% Watertight, pure quads)
    # Nariz mirando hacia -Y, Cola hacia +Y, Lomo hacia +Z.
    rings_config = [
        # (y, W, H, z_off)
        (-1.35, 0.35, 0.18,  0.06), # 0. Frente del hocico
        (-1.25, 0.88, 0.24,  0.06), # 1. Ensanchamiento frontal del martillo
        (-1.10, 1.28, 0.26,  0.06), # 2. Centro del martillo / Lóbulos de los ojos
        (-0.92, 1.20, 0.25,  0.05), # 3. Posterior de lóbulos
        (-0.72, 0.82, 0.38,  0.03), # 4. Cuello / transición suave al cuerpo
        (-0.45, 0.70, 0.50,  0.02), # 5. Pecho anterior
        (-0.12, 0.74, 0.58,  0.01), # 6. Vientre gordito (máximo ancho)
        ( 0.25, 0.68, 0.52, -0.01), # 7. Espalda
        ( 0.62, 0.54, 0.42, -0.02), # 8. Región pélvica
        ( 0.98, 0.38, 0.32, -0.01), # 9. Pre-cola
        ( 1.28, 0.20, 0.22,  0.00), # 10. Pedúnculo
        ( 1.44, 0.08, 0.16,  0.00), # 11. Raíz caudal
    ]

    N = 32
    ring_verts = []
    for y, W, H, z_off in rings_config:
        coords = capsule_ring(W, H, z_off, N=N)
        cur_ring = [bm.verts.new((x, y, z)) for x, z in coords]
        ring_verts.append(cur_ring)

    for r in range(len(rings_config) - 1):
        r1 = ring_verts[r]
        r2 = ring_verts[r + 1]
        for s in range(N):
            s_next = (s + 1) % N
            bm.faces.new((r1[s], r1[s_next], r2[s_next], r2[s]))

    snout_center = bm.verts.new((0.0, -1.38, 0.06))
    for s in range(N):
        s_next = (s + 1) % N
        bm.faces.new((ring_verts[0][s_next], ring_verts[0][s], snout_center))

    tail_center = bm.verts.new((0.0, 1.48, 0.00))
    for s in range(N):
        s_next = (s + 1) % N
        bm.faces.new((ring_verts[-1][s], ring_verts[-1][s_next], tail_center))

    # 2. ALETA DORSAL ALTA Y CURVA (+Z, profundamente anclada en el lomo)
    d_base_f = bm.verts.new(( 0.00, -0.22,  0.35)) # Anclado dentro del cuerpo
    d_base_b = bm.verts.new(( 0.00,  0.42,  0.32)) # Anclado dentro del cuerpo
    d_tip    = bm.verts.new(( 0.00,  0.14,  1.42)) # Punta alta, curvada y redondeada
    d_notch  = bm.verts.new(( 0.00,  0.34,  0.68)) # Hendidura posterior curva
    d_r      = bm.verts.new(( 0.07,  0.08,  0.72)) # Grosor infladito
    d_l      = bm.verts.new((-0.07,  0.08,  0.72))

    bm.faces.new((d_base_f, d_tip, d_r))
    bm.faces.new((d_tip, d_notch, d_r))
    bm.faces.new((d_notch, d_base_b, d_r))
    bm.faces.new((d_base_f, d_r, d_base_b))

    bm.faces.new((d_base_f, d_l, d_tip))
    bm.faces.new((d_tip, d_l, d_notch))
    bm.faces.new((d_notch, d_l, d_base_b))
    bm.faces.new((d_base_f, d_base_b, d_l))

    # 3. ALETA CAUDAL VERTICAL EN FORMA DE MEDIA LUNA (+Y)
    c_root_t = bm.verts.new(( 0.00,  1.38,  0.22))
    c_root_b = bm.verts.new(( 0.00,  1.38, -0.22))
    c_top    = bm.verts.new(( 0.00,  1.82,  1.05)) # Lóbulo superior alto y curvado
    c_notch  = bm.verts.new(( 0.00,  1.60,  0.12)) # Muesca suave
    c_bot    = bm.verts.new(( 0.00,  1.74, -0.68)) # Lóbulo inferior
    c_r      = bm.verts.new(( 0.05,  1.50,  0.12)) # Grosor
    c_l      = bm.verts.new((-0.05,  1.50,  0.12))

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

    # 4. ALETAS PECTORALES (Paddle wings bien ancladas en los costados)
    p_r_f = bm.verts.new(( 0.50, -0.38, -0.05))
    p_r_b = bm.verts.new(( 0.55,  0.10, -0.10))
    p_r_tip = bm.verts.new(( 1.42, -0.05, -0.42))
    p_r_mid = bm.verts.new(( 1.08, -0.24, -0.24))
    bm.faces.new((p_r_f, p_r_mid, p_r_b))
    bm.faces.new((p_r_f, p_r_tip, p_r_mid))
    bm.faces.new((p_r_mid, p_r_tip, p_r_b))

    p_l_f = bm.verts.new((-0.50, -0.38, -0.05))
    p_l_b = bm.verts.new((-0.60,  0.10, -0.10))
    p_l_tip = bm.verts.new((-1.42, -0.05, -0.42))
    p_l_mid = bm.verts.new((-1.08, -0.24, -0.24))
    bm.faces.new((p_l_f, p_l_b, p_l_mid))
    bm.faces.new((p_l_f, p_l_mid, p_l_tip))
    bm.faces.new((p_l_mid, p_l_b, p_l_tip))

    bm.verts.ensure_lookup_table()
    bm.faces.ensure_lookup_table()
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()

    obj = bpy.data.objects.new("Shark_Hammerhead_Cartoon", mesh)
    bpy.context.collection.objects.link(obj)

    # Subdivisión suave: Level 2 para formas perfectamente redondeadas y gorditas (≈5500 triángulos)
    subsurf = obj.modifiers.new(name="Subsurf", type='SUBSURF')
    subsurf.levels = 2
    subsurf.render_levels = 2

    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier="Subsurf")

    for poly in mesh.polygons:
        poly.use_smooth = True

    mesh.calc_loop_triangles()
    print(f"Malla creada con {len(mesh.vertices)} vértices y {len(mesh.loop_triangles)} triángulos (3000-8000 WebGL target).")

    # Escalar largo a exactamente 3.0 unidades
    bb = obj.bound_box
    ys = [p[1] for p in bb]
    cur_len = max(ys) - min(ys)
    scale_factor = 3.0 / cur_len
    obj.scale = (scale_factor, scale_factor, scale_factor)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    return obj

def generate_textures(obj):
    print("Iniciando generación de texturas 2048x2048...")
    RES = 2048

    # 1. UV Unwrap con margen
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(island_margin=0.02)
    bpy.ops.object.mode_set(mode='OBJECT')

    # 2. Hornear Position Map
    mat_bake = bpy.data.materials.new("Mat_Bake_Engine")
    mat_bake.use_nodes = True
    nodes = mat_bake.node_tree.nodes
    nodes.clear()
    out_node = nodes.new('ShaderNodeOutputMaterial')
    emit_node = nodes.new('ShaderNodeEmission')
    tex_coord = nodes.new('ShaderNodeTexCoord')
    mat_bake.node_tree.links.new(emit_node.outputs['Emission'], out_node.inputs['Surface'])
    mat_bake.node_tree.links.new(tex_coord.outputs['Object'], emit_node.inputs['Color'])

    img_pos = bpy.data.images.new("Bake_Pos", width=RES, height=RES, float_buffer=True)
    tex_pos = nodes.new('ShaderNodeTexImage')
    tex_pos.image = img_pos
    nodes.active = tex_pos

    obj.data.materials.append(mat_bake)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = 1

    print("Horneando Position Map...")
    bpy.ops.object.bake(type='EMIT')

    # 3. Hornear Normal Map
    mat_norm = bpy.data.materials.new("Mat_Norm_Engine")
    mat_norm.use_nodes = True
    nodes_n = mat_norm.node_tree.nodes
    nodes_n.clear()
    out_n = nodes_n.new('ShaderNodeOutputMaterial')
    emit_n = nodes_n.new('ShaderNodeEmission')
    geom_n = nodes_n.new('ShaderNodeNewGeometry')
    mat_norm.node_tree.links.new(emit_n.outputs['Emission'], out_n.inputs['Surface'])
    mat_norm.node_tree.links.new(geom_n.outputs['Normal'], emit_n.inputs['Color'])

    img_norm = bpy.data.images.new("Bake_Norm", width=RES, height=RES, float_buffer=True)
    tex_norm = nodes_n.new('ShaderNodeTexImage')
    tex_norm.image = img_norm
    nodes_n.active = tex_norm

    obj.data.materials[0] = mat_norm
    print("Horneando Normal Map...")
    bpy.ops.object.bake(type='EMIT')

    # 4. Hornear AO
    img_ao = bpy.data.images.new("Bake_AO", width=RES, height=RES)
    nodes.active = tex_pos
    tex_pos.image = img_ao
    obj.data.materials[0] = mat_bake
    scene.cycles.samples = 8
    print("Horneando Ambient Occlusion...")
    bpy.ops.object.bake(type='AO')

    # 5. Síntesis matemática y pintura con NumPy
    print("Procesando canales y síntesis de color...")
    pos_data = np.array(img_pos.pixels[:], dtype=np.float32).reshape((RES, RES, 4))
    norm_data = np.array(img_norm.pixels[:], dtype=np.float32).reshape((RES, RES, 4))
    ao_data = np.array(img_ao.pixels[:], dtype=np.float32).reshape((RES, RES, 4))

    X = pos_data[:, :, 0]
    Y = pos_data[:, :, 1]
    Z = pos_data[:, :, 2]
    Alpha = pos_data[:, :, 3]

    Nz = norm_data[:, :, 2]
    AO = ao_data[:, :, 0]

    mask = Alpha > 0.01

    # Paleta del diseño:
    LOMO = np.array([0.369, 0.478, 0.549], dtype=np.float32)       # Gris azulado #5E7A8C
    BELLY = np.array([0.953, 0.925, 0.855], dtype=np.float32)      # Crema #F3ECDA
    CYAN = np.array([0.349, 0.890, 1.000], dtype=np.float32)       # Cian bioluminiscente #59E7FF
    ORANGE_IRIS = np.array([1.000, 0.580, 0.080], dtype=np.float32)# Naranja cálido
    AMBER_IRIS = np.array([0.880, 0.380, 0.040], dtype=np.float32)
    PUPIL = np.array([0.050, 0.050, 0.060], dtype=np.float32)      # Negro pupila
    SCLERA = np.array([0.985, 0.975, 0.960], dtype=np.float32)     # Esclerótica blanco cálido
    WHITE_SPEC = np.array([1.000, 1.000, 1.000], dtype=np.float32) # Brillo especular

    # 5.1 Línea de separación limpia y ondulada entre lomo y vientre
    wave = np.sin(Y * 6.5) * 0.08 + np.sin(Y * 13.0 + 0.4) * 0.03 + np.cos(X * 4.0) * 0.02
    h_sep = Z + 0.35 * Nz + wave
    t_sep = np.clip((h_sep - (-0.04)) / 0.16, 0.0, 1.0)
    t_smooth = t_sep * t_sep * (3.0 - 2.0 * t_sep)

    albedo = np.zeros((RES, RES, 3), dtype=np.float32)
    emission = np.zeros((RES, RES, 3), dtype=np.float32)

    for c in range(3):
        albedo[:, :, c] = BELLY[c] * (1.0 - t_smooth) + LOMO[c] * t_smooth

    # 5.2 Sombreado suave pintado (AO bajo aleta y martillo)
    ao_factor = 0.74 + 0.26 * np.clip(AO, 0.0, 1.0)
    for c in range(3):
        albedo[:, :, c] *= ao_factor

    # 5.3 6 Marcas bioluminiscentes cian en lomo y costados
    streaks = [
        # (y_min, y_max, x_base, x_freq, z_pos, width)
        (-0.58, -0.22, 0.28, 10.0, 0.48, 0.040),
        (-0.38,  0.02, 0.52,  8.0, 0.24, 0.038),
        (-0.08,  0.32, 0.32,  8.0, 0.50, 0.042),
        ( 0.12,  0.50, 0.48,  7.0, 0.26, 0.038),
        ( 0.42,  0.78, 0.24,  7.0, 0.38, 0.036),
        ( 0.62,  0.96, 0.28,  6.0, 0.18, 0.032),
    ]

    abs_X = np.abs(X)
    streak_mask = np.zeros((RES, RES), dtype=np.float32)

    for y_min, y_max, x_b, x_f, z_p, sw in streaks:
        in_y = (Y >= y_min) & (Y <= y_max)
        t_y = np.clip((Y - y_min) / (y_max - y_min), 0.0, 1.0)
        env = 4.0 * t_y * (1.0 - t_y)
        x_target = x_b + 0.035 * np.sin(x_f * Y)
        d = np.sqrt((abs_X - x_target)**2 + (Z - z_p)**2)
        cur_w = sw * np.sqrt(np.maximum(env, 1e-4))
        intensity = np.clip(1.0 - d / cur_w, 0.0, 1.0) * env * in_y
        streak_mask = np.maximum(streak_mask, intensity)

    for c in range(3):
        albedo[:, :, c] = albedo[:, :, c] * (1.0 - streak_mask) + CYAN[c] * streak_mask
        emission[:, :, c] += CYAN[c] * streak_mask

    # 5.4 Ojos GRANDES y EXPRESIVOS en los extremos del martillo
    def paint_eye(is_right):
        cx = 1.25 if is_right else -1.25
        cy = -1.08
        cz = 0.06

        dx = X - cx
        dy = Y - cy
        dz = Z - cz
        dist = np.sqrt(dx*dx + dy*dy + dz*dz)

        # Región del lóbulo ocular (radio ~0.26)
        in_eye = (dist < 0.26) & ((X > 0.90) if is_right else (X < -0.90)) & (Y < -0.85)

        vx = dx / np.maximum(dist, 1e-4)
        vy = dy / np.maximum(dist, 1e-4)
        vz = dz / np.maximum(dist, 1e-4)

        # Vector de mirada: hacia adelante (-Y) y ligeramente hacia afuera (±X)
        lx = 0.65 if is_right else -0.65
        ly = -0.72
        lz = 0.15
        l_len = math.sqrt(lx*lx + ly*ly + lz*lz)
        lx /= l_len; ly /= l_len; lz /= l_len

        dot_look = vx * lx + vy * ly + vz * lz

        # Destellos de brillo (sparkle reflection)
        hx = 0.50 if is_right else -0.50
        hy = -0.78
        hz = 0.38
        h_len = math.sqrt(hx*hx + hy*hy + hz*hz)
        hx /= h_len; hy /= h_len; hz /= h_len
        dot_h1 = vx * hx + vy * hy + vz * hz

        h2x = 0.78 if is_right else -0.78
        h2y = -0.52
        h2z = -0.18
        h2_len = math.sqrt(h2x*h2x + h2y*h2y + h2z*h2z)
        h2x /= h2_len; h2y /= h2_len; h2z /= h2_len
        dot_h2 = vx * h2x + vy * h2y + vz * h2z

        # 1. Esclerótica (blanco cálido)
        eye_col = np.zeros((*dx.shape, 3), dtype=np.float32)
        eye_emit = np.zeros((*dx.shape, 3), dtype=np.float32)
        for c in range(3):
            eye_col[:, :, c] = SCLERA[c]

        # 2. Iris (grande, cálido y luminoso)
        iris_mask = dot_look > 0.55
        t_iris = np.clip((dot_look - 0.55) / (0.84 - 0.55), 0.0, 1.0)
        for c in range(3):
            cur_iris = AMBER_IRIS[c] * (1.0 - t_iris) + ORANGE_IRIS[c] * t_iris
            eye_col[:, :, c] = np.where(iris_mask, cur_iris, eye_col[:, :, c])
            eye_emit[:, :, c] = np.where(iris_mask, ORANGE_IRIS[c] * 0.95, eye_emit[:, :, c])

        # 3. Pupila (negro profundo amigable)
        pupil_mask = dot_look > 0.84
        for c in range(3):
            eye_col[:, :, c] = np.where(pupil_mask, PUPIL[c], eye_col[:, :, c])
            eye_emit[:, :, c] = np.where(pupil_mask, 0.0, eye_emit[:, :, c])

        # 4. Destellos blancos (brillo y vida en la mirada)
        spec_mask = (dot_h1 > 0.94) | (dot_h2 > 0.975)
        for c in range(3):
            eye_col[:, :, c] = np.where(spec_mask, WHITE_SPEC[c], eye_col[:, :, c])
            eye_emit[:, :, c] = np.where(spec_mask, 0.0, eye_emit[:, :, c])

        # 5. Párpado superior relajado (mirada amable, sin ceño fruncido)
        eyelid_mask = vz > 0.48
        t_lid = np.clip((vz - 0.48) / 0.22, 0.0, 1.0)
        for c in range(3):
            eye_col[:, :, c] = np.where(eyelid_mask, eye_col[:, :, c] * (1.0 - t_lid) + LOMO[c] * t_lid, eye_col[:, :, c])
            eye_emit[:, :, c] = np.where(eyelid_mask, eye_emit[:, :, c] * (1.0 - t_lid), eye_emit[:, :, c])

        return eye_col, eye_emit, in_eye

    eye_col_r, eye_emit_r, in_r = paint_eye(True)
    eye_col_l, eye_emit_l, in_l = paint_eye(False)

    for c in range(3):
        albedo[:, :, c] = np.where(in_r, eye_col_r[:, :, c], albedo[:, :, c])
        albedo[:, :, c] = np.where(in_l, eye_col_l[:, :, c], albedo[:, :, c])
        emission[:, :, c] = np.where(in_r, eye_emit_r[:, :, c], emission[:, :, c])
        emission[:, :, c] = np.where(in_l, eye_emit_l[:, :, c], emission[:, :, c])

    # 5.5 Sonrisa ligera y dientitos redondeados
    y_smile = -0.88 + 0.35 * ((abs_X / 0.28) ** 2)
    dy_smile = Y - y_smile
    # Línea oscura de la comisura de la sonrisa
    smile_crease = (np.abs(dy_smile) < 0.025) & (abs_X < 0.28) & (Z < -0.12)
    crease_col = np.array([0.22, 0.12, 0.10], dtype=np.float32)

    # Boca ligeramente abierta (interior cálido)
    mouth_open = (dy_smile > -0.065) & (dy_smile < 0.0) & (abs_X < 0.22) & (Z < -0.12)
    mouth_col = np.array([0.38, 0.18, 0.16], dtype=np.float32)

    # Dientitos blancos pequeños y redondeados
    teeth_mask = mouth_open & (dy_smile > -0.028) & (abs_X < 0.18) & (abs_X > 0.02)
    teeth_round = np.cos(abs_X * 60.0) > 0.3 # crea 4 dientitos perlados
    teeth_final = teeth_mask & teeth_round

    for c in range(3):
        albedo[:, :, c] = np.where(mouth_open, mouth_col[c], albedo[:, :, c])
        albedo[:, :, c] = np.where(teeth_final, WHITE_SPEC[c], albedo[:, :, c])
        albedo[:, :, c] = np.where(smile_crease, crease_col[c], albedo[:, :, c])

    # 6. Dilatación de bordes UV (Padding)
    print("Aplicando dilatación de bordes UV...")
    bg_mask = ~mask
    for c in range(3):
        albedo[:, :, c] = np.where(bg_mask, BELLY[c] if c != 0 else LOMO[c], albedo[:, :, c])
        emission[:, :, c] = np.where(bg_mask, 0.0, emission[:, :, c])

    # 7. Guardar Shark_Albedo.png y Shark_Emission.png
    albedo_path = os.path.join(TEXTURE_DIR, "Shark_Albedo.png")
    emission_path = os.path.join(TEXTURE_DIR, "Shark_Emission.png")

    img_albedo_out = bpy.data.images.new("Shark_Albedo", width=RES, height=RES)
    albedo_rgba = np.dstack([albedo, np.ones((RES, RES), dtype=np.float32)]).flatten()
    img_albedo_out.pixels.foreach_set(albedo_rgba)
    img_albedo_out.filepath_raw = albedo_path
    img_albedo_out.file_format = 'PNG'
    img_albedo_out.save()
    print("Guardado Shark_Albedo.png en:", albedo_path)

    img_emission_out = bpy.data.images.new("Shark_Emission", width=RES, height=RES)
    emission_rgba = np.dstack([emission, np.ones((RES, RES), dtype=np.float32)]).flatten()
    img_emission_out.pixels.foreach_set(emission_rgba)
    img_emission_out.filepath_raw = emission_path
    img_emission_out.file_format = 'PNG'
    img_emission_out.save()
    print("Guardado Shark_Emission.png en:", emission_path)

    # 8. Asignar el Material final Cartoon
    mat_final = bpy.data.materials.new("Mat_Shark_Cartoon")
    mat_final.use_nodes = True
    fnodes = mat_final.node_tree.nodes
    fnodes.clear()
    fout = fnodes.new('ShaderNodeOutputMaterial')
    fbsdf = fnodes.new('ShaderNodeBsdfPrincipled')
    mat_final.node_tree.links.new(fbsdf.outputs['BSDF'], fout.inputs['Surface'])

    ftex_alb = fnodes.new('ShaderNodeTexImage')
    ftex_alb.image = img_albedo_out
    mat_final.node_tree.links.new(ftex_alb.outputs['Color'], fbsdf.inputs['Base Color'])

    ftex_emit = fnodes.new('ShaderNodeTexImage')
    ftex_emit.image = img_emission_out
    if 'Emission Color' in fbsdf.inputs:
        mat_final.node_tree.links.new(ftex_emit.outputs['Color'], fbsdf.inputs['Emission Color'])
        fbsdf.inputs['Emission Strength'].default_value = 1.6
    elif 'Emission' in fbsdf.inputs:
        mat_final.node_tree.links.new(ftex_emit.outputs['Color'], fbsdf.inputs['Emission'])

    fbsdf.inputs['Roughness'].default_value = 0.32
    if 'Specular IOR Level' in fbsdf.inputs:
        fbsdf.inputs['Specular IOR Level'].default_value = 0.55
    elif 'Specular' in fbsdf.inputs:
        fbsdf.inputs['Specular'].default_value = 0.55

    obj.data.materials.clear()
    obj.data.materials.append(mat_final)
    print("Material final asignado exitosamente al modelo.")

    return mat_final

def render_review_images(obj):
    print("Renderizando 3 imágenes de revisión (perfil, 3/4 y frente) con encuadre completo...")
    scene = bpy.context.scene
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 64
    scene.render.film_transparent = False

    world = bpy.data.worlds.new("World_Ocean")
    scene.world = world
    world.use_nodes = True
    wnodes = world.node_tree.nodes
    wnodes.clear()
    wout = wnodes.new('ShaderNodeOutputWorld')
    wbg = wnodes.new('ShaderNodeBackground')
    wbg.inputs['Color'].default_value = (0.04, 0.20, 0.35, 1.0)
    wbg.inputs['Strength'].default_value = 1.0
    world.node_tree.links.new(wbg.outputs['Background'], wout.inputs['Surface'])

    # Iluminación de estudio
    key_light = bpy.data.lights.new('KeyLight', type='SUN')
    key_light.energy = 4.2
    key_light.color = (1.0, 0.98, 0.95)
    key_obj = bpy.data.objects.new('KeyLight', key_light)
    bpy.context.collection.objects.link(key_obj)
    key_obj.location = (2.0, -5.0, 5.0)
    key_obj.rotation_euler = (math.radians(45), math.radians(15), math.radians(-30))

    fill_light = bpy.data.lights.new('FillLight', type='SUN')
    fill_light.energy = 2.4
    fill_light.color = (0.45, 0.80, 1.0)
    fill_obj = bpy.data.objects.new('FillLight', fill_light)
    bpy.context.collection.objects.link(fill_obj)
    fill_obj.location = (-4.0, 4.0, -2.0)
    fill_obj.rotation_euler = (math.radians(-45), math.radians(-30), math.radians(120))

    rim_light = bpy.data.lights.new('RimLight', type='POINT')
    rim_light.energy = 500.0
    rim_light.color = (0.50, 0.95, 1.0)
    rim_obj = bpy.data.objects.new('RimLight', rim_light)
    bpy.context.collection.objects.link(rim_obj)
    rim_obj.location = (0.0, 4.5, 2.0)

    # Cámara profesional 45mm con encuadre completo
    cam_data = bpy.data.cameras.new('ReviewCam')
    cam_data.lens = 45
    cam_obj = bpy.data.objects.new('ReviewCam', cam_data)
    bpy.context.collection.objects.link(cam_obj)
    scene.camera = cam_obj

    # 1. Render Perfil: costado (+X mirando hacia el tiburón, cabeza en -Y a la izquierda)
    cam_obj.location = (7.8, 0.15, 0.25)
    cam_obj.rotation_euler = (math.radians(90), 0, math.radians(90))
    perfil_path = os.path.join(DESIGNS_DIR, "Render_Cartoon_Perfil.png")
    scene.render.filepath = perfil_path
    bpy.ops.render.render(write_still=True)
    print("Guardado:", perfil_path)

    # 2. Render 3/4: adelante-derecha-arriba
    cam_obj.location = (5.2, -5.2, 2.6)
    cam_obj.rotation_euler = (math.radians(66), math.radians(0), math.radians(45))
    tres_path = os.path.join(DESIGNS_DIR, "Render_Cartoon_TresCuartos.png")
    scene.render.filepath = tres_path
    bpy.ops.render.render(write_still=True)
    print("Guardado:", tres_path)

    # 3. Render Frente: mirando de frente a la cara (-Y hacia +Y)
    cam_obj.location = (0.0, -6.0, 0.25)
    cam_obj.rotation_euler = (math.radians(90), 0, math.radians(0))
    frente_path = os.path.join(DESIGNS_DIR, "Render_Cartoon_Frente.png")
    scene.render.filepath = frente_path
    bpy.ops.render.render(write_still=True)
    print("Guardado:", frente_path)

def export_fbx_and_blend(obj):
    print("Exportando Shark_Hammerhead_Cartoon.fbx y .blend...")
    fbx_target = os.path.join(OUTPUT_DIR, "Shark_Hammerhead_Cartoon.fbx")
    blend_target = os.path.join(OUTPUT_DIR, "Shark_Hammerhead_Cartoon.blend")

    bpy.ops.wm.save_as_mainfile(filepath=blend_target)
    print("Guardado BLEND:", blend_target)

    # Exportar FBX
    # Requisitos: Apply Scalings = FBX All, Forward = -Z, Up = Y, Apply Transform activado, sin Armature ni animación.
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

    bpy.ops.export_scene.fbx(
        filepath=fbx_target,
        use_selection=True,
        apply_scale_options='FBX_SCALE_ALL',
        use_space_transform=True,
        axis_forward='-Z',
        axis_up='Y',
        bake_anim=False,
        object_types={'MESH'}
    )
    print("Guardado FBX:", fbx_target)

def main():
    t_start = time.time()
    print("=== INICIANDO CREACIÓN DEFINITIVA DEL TIBURÓN MARTILLO CARTOON ===")
    reset_scene()
    obj = build_cartoon_shark()
    generate_textures(obj)
    render_review_images(obj)
    export_fbx_and_blend(obj)
    print(f"=== PROCESO COMPLETADO EXITOSAMENTE EN {time.time() - t_start:.2f}s ===")

if __name__ == "__main__":
    main()
