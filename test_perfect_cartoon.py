"""
Script definitivo para perfeccionar el modelo, texturas, ojos, boca y renderizado del tiburón martillo cartoon.
"""

import bpy
import bmesh
import numpy as np
import os
import math

BLEND_PATH = r"D:\ECO SHARK\Assets\EcoShark\Models\Sharks\Shark_Hammerhead_Cartoon.blend"
TEXTURE_DIR = r"D:\ECO SHARK\Assets\EcoShark\Models\Sharks\Textures"
DESIGNS_DIR = r"D:\ECO SHARK\Assets\EcoShark\Art\Designs"

ALBEDO_PATH = os.path.join(TEXTURE_DIR, "Shark_Albedo.png")
EMISSION_PATH = os.path.join(TEXTURE_DIR, "Shark_Emission.png")

# Colores según diseño de referencia (Diseno_Tiburon_Martillo_Perfil_2.5D.png)
LOMO_BASE    = np.array([0.369, 0.478, 0.549], dtype=np.float32) # #5E7A8C azul pizarra
LOMO_DARK    = np.array([0.200, 0.280, 0.350], dtype=np.float32) # Sombra y pliegues
VIENTRE_BASE = np.array([0.953, 0.925, 0.855], dtype=np.float32) # #F3ECDA crema cálido
CYAN_GLOW    = np.array([0.250, 0.920, 1.000], dtype=np.float32) # #59E7FF cian neón vibrante

# Ojos Pixar/Disney cálidos y expresivos
SCLERA       = np.array([0.985, 0.975, 0.950], dtype=np.float32) # Blanco marfil
EYE_BORDER   = np.array([0.150, 0.180, 0.220], dtype=np.float32) # Marco oscuro de cuenca
IRIS_BORDER  = np.array([0.550, 0.180, 0.020], dtype=np.float32) # Anillo limbal ámbar profundo
IRIS_AMBER   = np.array([0.950, 0.420, 0.040], dtype=np.float32) # Ámbar cálido
IRIS_GOLD    = np.array([1.000, 0.720, 0.120], dtype=np.float32) # Oro brillante interior
PUPIL        = np.array([0.060, 0.060, 0.080], dtype=np.float32) # Pupila negra
WHITE_SPEC   = np.array([1.000, 1.000, 1.000], dtype=np.float32) # Brillo especular

# Boca y sonrisa
MOUTH_LINE   = np.array([0.160, 0.100, 0.100], dtype=np.float32) # Comisura oscura
MOUTH_INNER  = np.array([0.380, 0.160, 0.160], dtype=np.float32) # Interior cálido sonriente
TEETH_COL    = np.array([0.985, 0.985, 0.960], dtype=np.float32) # Dientes redondeados perla

def run():
    print("Cargando escena...")
    bpy.ops.wm.open_mainfile(filepath=BLEND_PATH)
    
    obj = bpy.data.objects.get("Shark_Hammerhead_Cartoon")
    if not obj:
        raise RuntimeError("No se encontró el objeto Shark_Hammerhead_Cartoon")
        
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = 16
    scene.render.film_transparent = False
    
    RES = 2048
    
    # 1. Hornear Position Map
    print("Horneando Position Map...")
    img_pos = bpy.data.images.new("Bake_Pos", width=RES, height=RES, float_buffer=True)
    mat_pos = bpy.data.materials.new("Mat_Bake_Pos")
    mat_pos.use_nodes = True
    nodes = mat_pos.node_tree.nodes
    nodes.clear()
    out = nodes.new('ShaderNodeOutputMaterial')
    emit = nodes.new('ShaderNodeEmission')
    geom = nodes.new('ShaderNodeNewGeometry')
    mat_pos.node_tree.links.new(geom.outputs['Position'], emit.inputs['Color'])
    mat_pos.node_tree.links.new(emit.outputs['Emission'], out.inputs['Surface'])
    
    tex_node = nodes.new('ShaderNodeTexImage')
    tex_node.image = img_pos
    nodes.active = tex_node
    
    obj.data.materials.clear()
    obj.data.materials.append(mat_pos)
    
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.bake(type='EMIT', margin=16)
    
    pos_data = np.array(img_pos.pixels[:], dtype=np.float32).reshape((RES, RES, 4))
    X = pos_data[:, :, 0]
    Y = pos_data[:, :, 1]
    Z = pos_data[:, :, 2]
    mask = (pos_data[:, :, 3] > 0.5) & ((X != 0) | (Y != 0) | (Z != 0))
    print(f"Píxeles válidos: {np.sum(mask)}")
    
    # 2. Hornear AO
    print("Horneando Ambient Occlusion...")
    img_ao = bpy.data.images.new("Bake_AO", width=RES, height=RES)
    tex_node.image = img_ao
    bpy.ops.object.bake(type='AO', margin=16)
    ao_data = np.array(img_ao.pixels[:], dtype=np.float32).reshape((RES, RES, 4))
    AO = ao_data[:, :, 0]
    
    # Limpiar
    bpy.data.materials.remove(mat_pos, do_unlink=True)
    bpy.data.images.remove(img_pos, do_unlink=True)
    bpy.data.images.remove(img_ao, do_unlink=True)
    
    # 3. Síntesis de texturas
    print("Sintetizando Albedo y Emisión...")
    albedo = np.zeros((RES, RES, 3), dtype=np.float32)
    emission = np.zeros((RES, RES, 3), dtype=np.float32)
    
    # Línea de separación lomo/vientre ondulada y limpia
    belly_wave = 0.06 * np.sin(Y * 3.5) + 0.03 * np.cos(np.abs(X) * 4.0)
    # En el martillo, el vientre cubre la zona inferior (Z < 0)
    t_split = np.clip((Z - belly_wave + 0.04) / 0.12, 0.0, 1.0)
    
    for c in range(3):
        col_blend = VIENTRE_BASE[c] * (1.0 - t_split) + LOMO_BASE[c] * t_split
        # AO suave en cavidades
        col_blend = col_blend * (0.75 + 0.25 * AO)
        albedo[:, :, c] = np.where(mask, col_blend, 0.0)
        
    # Acento oscuro en la punta superior de la aleta dorsal
    dorsal_tip = np.clip((Z - 0.85) / 0.45, 0.0, 1.0)
    for c in range(3):
        albedo[:, :, c] = albedo[:, :, c] * (1.0 - dorsal_tip * 0.25) + LOMO_DARK[c] * (dorsal_tip * 0.25)
        
    # 4. Marcas bioluminiscentes cian (vibrantes y estilizadas)
    streaks = [
        # (y_min, y_max, x_base, x_freq, z_pos, width)
        (-0.65, -0.15, 0.32, 6.0, 0.44, 0.075),
        (-0.45,  0.08, 0.54, 5.5, 0.22, 0.070),
        (-0.15,  0.35, 0.35, 5.5, 0.46, 0.075),
        ( 0.05,  0.55, 0.48, 5.0, 0.22, 0.065),
        ( 0.35,  0.80, 0.26, 4.5, 0.36, 0.060),
        ( 0.55,  1.00, 0.26, 4.0, 0.16, 0.055),
    ]
    abs_X = np.abs(X)
    streak_mask = np.zeros((RES, RES), dtype=np.float32)
    for y_min, y_max, x_b, x_f, z_p, sw in streaks:
        in_y = (Y >= y_min) & (Y <= y_max)
        t_y = np.clip((Y - y_min) / (y_max - y_min), 0.0, 1.0)
        env = 4.0 * t_y * (1.0 - t_y)
        x_target = x_b + 0.045 * np.sin(x_f * Y)
        d = np.sqrt((abs_X - x_target)**2 + (Z - z_p)**2)
        cur_w = sw * np.sqrt(np.maximum(env, 1e-4))
        intensity = np.clip(1.0 - d / cur_w, 0.0, 1.0) * env * in_y
        streak_mask = np.maximum(streak_mask, intensity)
        
    for c in range(3):
        albedo[:, :, c] = albedo[:, :, c] * (1.0 - streak_mask) + CYAN_GLOW[c] * streak_mask
        emission[:, :, c] += CYAN_GLOW[c] * (streak_mask ** 0.7) * 2.2
        
    # 5. OJOS GRANDES Y EXPRESIVOS (Disney/Pixar Style)
    def render_eye(is_right):
        cx = 1.06 if is_right else -1.06
        cy = -1.02
        cz = 0.05
        
        dx = X - cx
        dy = Y - cy
        dz = Z - cz
        dist = np.sqrt(dx*dx + dy*dy + dz*dz)
        
        vx = dx / np.maximum(dist, 1e-4)
        vy = dy / np.maximum(dist, 1e-4)
        vz = dz / np.maximum(dist, 1e-4)
        
        # Mirada hacia afuera (+X) y hacia adelante (-Y)
        lx = 0.74 if is_right else -0.74
        ly = -0.63
        lz = 0.22
        l_len = math.sqrt(lx*lx + ly*ly + lz*lz)
        lx /= l_len; ly /= l_len; lz /= l_len
        
        dot = vx * lx + vy * ly + vz * lz
        
        # Solo en el lado correspondiente del cuerpo
        in_side = (X > 0.40) if is_right else (X < -0.40)
        # Región ocular definida puramente por el cono de mirada (redondeado orgánico)
        in_eye_cone = in_side & (dist < 0.35) & (dot > 0.06) & (Y < -0.80)
        
        # Destellos
        h1x = 0.68 if is_right else -0.68
        h1y = -0.68
        h1z = 0.38
        h1_len = math.sqrt(h1x*h1x + h1y*h1y + h1z*h1z)
        dot_h1 = vx * (h1x/h1_len) + vy * (h1y/h1_len) + vz * (h1z/h1_len)
        
        h2x = 0.82 if is_right else -0.82
        h2y = -0.45
        h2z = -0.15
        h2_len = math.sqrt(h2x*h2x + h2y*h2y + h2z*h2z)
        dot_h2 = vx * (h2x/h2_len) + vy * (h2y/h2_len) + vz * (h2z/h2_len)
        
        col = np.zeros((*dx.shape, 3), dtype=np.float32)
        emit = np.zeros((*dx.shape, 3), dtype=np.float32)
        
        # Marco exterior suavemente difuminado hacia la piel
        t_border = np.clip((dot - 0.06) / 0.12, 0.0, 1.0)
        for c in range(3):
            col[:, :, c] = EYE_BORDER[c]
            
        # Esclerótica blanco cálido
        sclera_mask = dot > 0.18
        for c in range(3):
            col[:, :, c] = np.where(sclera_mask, SCLERA[c], col[:, :, c])
            
        # Anillo limbal ámbar
        iris_rim = dot > 0.48
        for c in range(3):
            col[:, :, c] = np.where(iris_rim, IRIS_BORDER[c], col[:, :, c])
            
        # Iris ámbar a oro
        iris_main = dot > 0.54
        t_gold = np.clip((dot - 0.54) / (0.80 - 0.54), 0.0, 1.0)
        for c in range(3):
            cur_gold = IRIS_AMBER[c] * (1.0 - t_gold) + IRIS_GOLD[c] * t_gold
            col[:, :, c] = np.where(iris_main, cur_gold, col[:, :, c])
            emit[:, :, c] = np.where(iris_main, IRIS_GOLD[c] * 0.40, emit[:, :, c])
            
        # Pupila negra
        pupil_mask = dot > 0.80
        for c in range(3):
            col[:, :, c] = np.where(pupil_mask, PUPIL[c], col[:, :, c])
            emit[:, :, c] = np.where(pupil_mask, 0.0, emit[:, :, c])
            
        # Destellos
        spec1 = dot_h1 > 0.952
        spec2 = dot_h2 > 0.980
        for c in range(3):
            col[:, :, c] = np.where(spec1 | spec2, WHITE_SPEC[c], col[:, :, c])
            emit[:, :, c] = np.where(spec1 | spec2, 0.0, emit[:, :, c])
            
        # Párpado superior relajado (amable)
        # Línea de párpado curva
        lid_threshold = 0.40 + 0.10 * (vx ** 2)
        eyelid_zone = (vz > lid_threshold) & in_eye_cone
        t_lid = np.clip((vz - lid_threshold) / 0.15, 0.0, 1.0)
        crease = (np.abs(vz - lid_threshold) < 0.028) & in_eye_cone
        
        for c in range(3):
            col[:, :, c] = np.where(eyelid_zone, col[:, :, c] * (1.0 - t_lid) + LOMO_BASE[c] * t_lid, col[:, :, c])
            col[:, :, c] = np.where(crease, EYE_BORDER[c], col[:, :, c])
            emit[:, :, c] = np.where(eyelid_zone, 0.0, emit[:, :, c])
            
        return col, emit, in_eye_cone, t_border
        
    col_r, emit_r, in_r, tb_r = render_eye(True)
    col_l, emit_l, in_l, tb_l = render_eye(False)
    
    for c in range(3):
        # Mezcla suave en el borde del ojo con la piel
        albedo[:, :, c] = np.where(in_r, albedo[:, :, c] * (1.0 - tb_r) + col_r[:, :, c] * tb_r, albedo[:, :, c])
        albedo[:, :, c] = np.where(in_l, albedo[:, :, c] * (1.0 - tb_l) + col_l[:, :, c] * tb_l, albedo[:, :, c])
        emission[:, :, c] = np.where(in_r, emit_r[:, :, c], emission[:, :, c])
        emission[:, :, c] = np.where(in_l, emit_l[:, :, c], emission[:, :, c])
        
    # 6. BOCA SONRIENTE EN MANDÍBULA Y COSTADOS (VISIBLE EN FRENTE Y PERFIL)
    # Color interior rosado cálido para que se vea alegre y cartoon (no mancha negra)
    MOUTH_ROSY = np.array([0.720, 0.280, 0.320], dtype=np.float32)
    
    # Curva elegante en la mejilla lateral (Y=-0.86 a Y=-0.48)
    t_flank = np.clip((Y - (-0.85)) / 0.37, 0.0, 1.0)
    z_flank_smile = -0.12 + 0.14 * (t_flank ** 1.3)
    dz_flank = Z - z_flank_smile
    
    in_flank = (abs_X >= 0.45) & (abs_X <= 0.82) & (Y >= -0.86) & (Y <= -0.48)
    smile_line_flank = in_flank & (np.abs(dz_flank) < 0.012)
    
    # Apertura elíptica sonriente en la mejilla (centrada en Y=-0.64)
    env_mouth = np.maximum(0.0, 1.0 - ((Y - (-0.64)) / 0.12)**2)
    mouth_open_flank = in_flank & (dz_flank < 0.0) & (dz_flank > -0.038 * env_mouth) & (env_mouth > 0.0)
    
    # Dientitos perlados redondeados (3 dientitos visibles)
    teeth_flank = mouth_open_flank & (dz_flank > -0.020) & (np.cos((Y - (-0.64)) * 52.0) > 0.1)
    
    # Borde inferior de la boca abierta
    mouth_bottom_flank = in_flank & (np.abs(dz_flank - (-0.038 * env_mouth)) < 0.010) & (env_mouth > 0.1)
    
    # Hoyuelo sonriente al final de la comisura
    dimple_flank = in_flank & (np.sqrt((Y - (-0.48))**2 + (Z - 0.02)**2) < 0.018)
    
    # Conexión frontal en la barbilla
    u_chin = np.clip(abs_X / 0.46, 0.0, 1.0)
    z_chin = -0.15 + 0.03 * (u_chin ** 2)
    dz_chin = Z - z_chin
    in_chin = (abs_X < 0.46) & (Y >= -1.02) & (Y < -0.85)
    smile_line_chin = in_chin & (np.abs(dz_chin) < 0.012)
    
    smile_line_all = smile_line_flank | smile_line_chin | mouth_bottom_flank | dimple_flank
    mouth_open_all = mouth_open_flank
    teeth_all = teeth_flank
    
    for c in range(3):
        albedo[:, :, c] = np.where(mouth_open_all, MOUTH_ROSY[c], albedo[:, :, c])
        albedo[:, :, c] = np.where(teeth_all, TEETH_COL[c], albedo[:, :, c])
        albedo[:, :, c] = np.where(smile_line_all, MOUTH_LINE[c], albedo[:, :, c])
        
    # Dilatación UV
    def dilate(img_data, valid_mask, steps=6):
        out = img_data.copy()
        cur_mask = valid_mask.copy()
        for _ in range(steps):
            shift_u = np.roll(out, 1, axis=0)
            shift_d = np.roll(out, -1, axis=0)
            shift_l = np.roll(out, 1, axis=1)
            shift_r = np.roll(out, -1, axis=1)
            mask_u = np.roll(cur_mask, 1, axis=0)
            mask_d = np.roll(cur_mask, -1, axis=0)
            mask_l = np.roll(cur_mask, 1, axis=1)
            mask_r = np.roll(cur_mask, -1, axis=1)
            neigh_count = mask_u.astype(np.float32) + mask_d.astype(np.float32) + mask_l.astype(np.float32) + mask_r.astype(np.float32)
            has_neigh = (neigh_count > 0) & (~cur_mask)
            neigh_sum = (shift_u * mask_u[:, :, None] + shift_d * mask_d[:, :, None] + 
                         shift_l * mask_l[:, :, None] + shift_r * mask_r[:, :, None])
            for c in range(out.shape[2]):
                out[:, :, c] = np.where(has_neigh, neigh_sum[:, :, c] / np.maximum(neigh_count, 1.0), out[:, :, c])
            cur_mask = cur_mask | has_neigh
        return out
        
    print("Aplicando dilatación de costuras...")
    albedo_dilated = dilate(albedo, mask)
    emission_dilated = dilate(emission, mask)
    
    print("Guardando imágenes de textura...")
    img_alb_out = bpy.data.images.new("Shark_Albedo", width=RES, height=RES)
    alb_rgba = np.dstack([albedo_dilated, np.ones((RES, RES), dtype=np.float32)]).flatten()
    img_alb_out.pixels.foreach_set(alb_rgba)
    img_alb_out.filepath_raw = ALBEDO_PATH
    img_alb_out.file_format = 'PNG'
    img_alb_out.save()
    
    img_emit_out = bpy.data.images.new("Shark_Emission", width=RES, height=RES)
    emit_rgba = np.dstack([emission_dilated, np.ones((RES, RES), dtype=np.float32)]).flatten()
    img_emit_out.pixels.foreach_set(emit_rgba)
    img_emit_out.filepath_raw = EMISSION_PATH
    img_emit_out.file_format = 'PNG'
    img_emit_out.save()
    
    # Material
    mat = bpy.data.materials.get("Mat_Shark_Cartoon")
    if not mat:
        mat = bpy.data.materials.new("Mat_Shark_Cartoon")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    nodes.clear()
    out_node = nodes.new('ShaderNodeOutputMaterial')
    bsdf = nodes.new('ShaderNodeBsdfPrincipled')
    mat.node_tree.links.new(bsdf.outputs['BSDF'], out_node.inputs['Surface'])
    
    t_alb = nodes.new('ShaderNodeTexImage')
    t_alb.image = img_alb_out
    mat.node_tree.links.new(t_alb.outputs['Color'], bsdf.inputs['Base Color'])
    
    t_emit = nodes.new('ShaderNodeTexImage')
    t_emit.image = img_emit_out
    if 'Emission Color' in bsdf.inputs:
        mat.node_tree.links.new(t_emit.outputs['Color'], bsdf.inputs['Emission Color'])
        bsdf.inputs['Emission Strength'].default_value = 1.0
    elif 'Emission' in bsdf.inputs:
        mat.node_tree.links.new(t_emit.outputs['Color'], bsdf.inputs['Emission'])
        
    bsdf.inputs['Roughness'].default_value = 0.50
    if 'Specular IOR Level' in bsdf.inputs:
        bsdf.inputs['Specular IOR Level'].default_value = 0.30
    elif 'Specular' in bsdf.inputs:
        bsdf.inputs['Specular'].default_value = 0.30
        
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    
    # Renders
    print("Renderizando vistas con iluminación calibrada...")
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    scene.cycles.samples = 64
    
    # Luces
    for l in list(bpy.data.lights):
        bpy.data.lights.remove(l, do_unlink=True)
    for o in list(bpy.data.objects):
        if o.type == 'LIGHT':
            bpy.data.objects.remove(o, do_unlink=True)
            
    # Key light cálida suave
    k_data = bpy.data.lights.new('KeyLight', type='SUN')
    k_data.energy = 2.0
    k_data.color = (1.0, 0.98, 0.95)
    k_obj = bpy.data.objects.new('KeyLight', k_data)
    scene.collection.objects.link(k_obj)
    k_obj.rotation_euler = (math.radians(45), math.radians(20), math.radians(-35))
    
    # Fill light neutra suave (preserva el color crema del vientre)
    f_data = bpy.data.lights.new('FillLight', type='SUN')
    f_data.energy = 0.8
    f_data.color = (0.90, 0.93, 0.96)
    f_obj = bpy.data.objects.new('FillLight', f_data)
    scene.collection.objects.link(f_obj)
    f_obj.rotation_euler = (math.radians(-30), math.radians(-25), math.radians(140))
    
    cam_data = bpy.data.cameras.new('ReviewCam')
    cam_data.lens = 45.0
    cam_obj = bpy.data.objects.new('ReviewCam', cam_data)
    scene.collection.objects.link(cam_obj)
    scene.camera = cam_obj
    
    def track_target(cam, target_pos=(0.0, 0.0, 0.10)):
        t_obj = bpy.data.objects.new('Target', None)
        scene.collection.objects.link(t_obj)
        t_obj.location = target_pos
        c = cam.constraints.new(type='TRACK_TO')
        c.target = t_obj
        c.track_axis = 'TRACK_NEGATIVE_Z'
        c.up_axis = 'UP_Y'
        return t_obj
        
    target = track_target(cam_obj)
    
    # 1. Perfil
    cam_obj.location = (7.6, 0.0, 0.10)
    bpy.context.view_layer.update()
    scene.render.filepath = os.path.join(DESIGNS_DIR, "Render_Cartoon_Perfil.png")
    bpy.ops.render.render(write_still=True)
    print("Guardado Render_Cartoon_Perfil.png")
    
    # 2. 3/4
    cam_obj.location = (3.8, -3.8, 1.1)
    bpy.context.view_layer.update()
    scene.render.filepath = os.path.join(DESIGNS_DIR, "Render_Cartoon_TresCuartos.png")
    bpy.ops.render.render(write_still=True)
    print("Guardado Render_Cartoon_TresCuartos.png")
    
    # 3. Frente (ángulo ligeramente bajo para captar la sonrisa y los dos ojos con claridad)
    cam_obj.location = (0.0, -5.6, -0.15)
    bpy.context.view_layer.update()
    scene.render.filepath = os.path.join(DESIGNS_DIR, "Render_Cartoon_Frente.png")
    bpy.ops.render.render(write_still=True)
    print("Guardado Render_Cartoon_Frente.png")
    
    # Blend y FBX
    bpy.ops.wm.save_mainfile(filepath=BLEND_PATH)
    fbx_path = os.path.join(os.path.dirname(BLEND_PATH), "Shark_Hammerhead_Cartoon.fbx")
    
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        bake_space_transform=True,
        object_types={'MESH'},
        mesh_smooth_type='FACE',
        add_leaf_bones=False,
        bake_anim=False
    )
    print("Exportación FBX y Blend finalizada con éxito.")

if __name__ == '__main__':
    run()
