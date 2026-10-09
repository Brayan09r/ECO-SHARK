"""
Script para descargar, recortar silencios, normalizar y exportar a .OGG
todos los 11 audios de ECO SHARK utilizando Audaspace (aud) de Blender.
"""

import os
import urllib.request
import numpy as np
import aud

BASE_DIR = r"D:\ECO SHARK"
TEMP_DIR = os.path.join(BASE_DIR, "temp_audio")
AUDIO_DIR = os.path.join(BASE_DIR, r"Assets\EcoShark\Resources\Audio")

os.makedirs(TEMP_DIR, exist_ok=True)
os.makedirs(os.path.join(AUDIO_DIR, "Musica"), exist_ok=True)
os.makedirs(os.path.join(AUDIO_DIR, "Ambiente"), exist_ok=True)
os.makedirs(os.path.join(AUDIO_DIR, "SFX"), exist_ok=True)

TRACKS = [
    {
        "id": "musica_fondo",
        "category": "Musica",
        "filename": "musica_fondo.ogg",
        "url": "https://opengameart.org/sites/default/files/Cleyton%20RX%20-%20Underwater_0.mp3",
        "title": "Underwater Theme (Cleyton RX - Underwater)",
        "author": "CleytonKauffman",
        "source_page": "https://opengameart.org/content/underwater-theme",
        "license": "CC0 1.0 Universal",
        "target_max_sec": 180.0,
        "is_loop": True,
        "target_peak_db": -1.2, # ~0.87
    },
    {
        "id": "ambiente_submarino",
        "category": "Ambiente",
        "filename": "ambiente_submarino.ogg",
        "url": "https://cdn.pixabay.com/download/audio/2026/02/10/audio_b1345942ff.mp3?filename=dragon-studio-deep-sea-underwater-ambience-482888.mp3",
        "title": "Deep-Sea Underwater Ambience",
        "author": "DRAGON-STUDIO",
        "source_page": "https://pixabay.com/sound-effects/nature-deep-sea-underwater-ambience-482888/",
        "license": "Pixabay Content License",
        "target_max_sec": 90.0,
        "is_loop": True,
        "target_peak_db": -3.0, # ~0.71
    },
    {
        "id": "nado_loop",
        "category": "SFX",
        "filename": "nado_loop.ogg",
        "url": "https://cdn.pixabay.com/download/audio/2026/03/10/audio_63bd5f2640.mp3?filename=dragon-studio-gentle-underwater-swish-499658.mp3",
        "title": "Gentle Underwater Swish",
        "author": "DRAGON-STUDIO",
        "source_page": "https://pixabay.com/sound-effects/film-special-effects-gentle-underwater-swish-499658/",
        "license": "Pixabay Content License",
        "target_max_sec": 5.0,
        "is_loop": True,
        "target_peak_db": -2.0, # ~0.79
    },
    {
        "id": "impulso",
        "category": "SFX",
        "filename": "impulso.ogg",
        "url": "https://cdn.pixabay.com/download/audio/2025/09/16/audio_6839dcc6b7.mp3?filename=dragon-studio-quick-whoosh-405448.mp3",
        "title": "Quick Whoosh",
        "author": "DRAGON-STUDIO",
        "source_page": "https://pixabay.com/sound-effects/film-special-effects-quick-whoosh-405448/",
        "license": "Pixabay Content License",
        "target_max_sec": 0.95,
        "is_loop": False,
        "target_peak_db": -1.5,
    },
    {
        "id": "recoger_basura",
        "category": "SFX",
        "filename": "recoger_basura.ogg",
        "url": "https://cdn.pixabay.com/download/audio/2025/01/27/audio_68e78552c6.mp3?filename=universfield-bubble-pop-293342.mp3",
        "title": "Bubble Pop",
        "author": "Universfield",
        "source_page": "https://pixabay.com/sound-effects/film-special-effects-bubble-pop-293342/",
        "license": "Pixabay Content License",
        "target_max_sec": 0.85,
        "is_loop": False,
        "target_peak_db": -1.5,
    },
    {
        "id": "rescate_animal",
        "category": "SFX",
        "filename": "rescate_animal.ogg",
        "url": "https://cdn.pixabay.com/download/audio/2023/04/02/audio_1f9f98a5a8.mp3?filename=universfield-reward-chime-144757.mp3",
        "title": "Reward Chime",
        "author": "Universfield",
        "source_page": "https://pixabay.com/sound-effects/film-special-effects-reward-chime-144757/",
        "license": "Pixabay Content License",
        "target_max_sec": 2.0,
        "is_loop": False,
        "target_peak_db": -1.5,
    },
    {
        "id": "animal_perdido",
        "category": "SFX",
        "filename": "animal_perdido.ogg",
        "url": "https://cdn.pixabay.com/download/audio/2024/12/17/audio_b89a48615f.mp3?filename=universfield-funny-fail-02-277575.mp3",
        "title": "Funny Fail 02",
        "author": "Universfield",
        "source_page": "https://pixabay.com/sound-effects/musical-funny-fail-02-277575/",
        "license": "Pixabay Content License",
        "target_max_sec": 2.2,
        "is_loop": False,
        "target_peak_db": -1.5,
    },
    {
        "id": "golpe",
        "category": "SFX",
        "filename": "golpe.ogg",
        "url": "https://cdn.pixabay.com/download/audio/2025/09/08/audio_8466f0df16.mp3?filename=dragon-studio-deep-sea-impact-401722.mp3",
        "title": "Deep Sea Impact",
        "author": "DRAGON-STUDIO",
        "source_page": "https://pixabay.com/sound-effects/film-special-effects-deep-sea-impact-401722/",
        "license": "Pixabay Content License",
        "target_max_sec": 0.95,
        "is_loop": False,
        "target_peak_db": -1.5,
    },
    {
        "id": "fin_partida",
        "category": "SFX",
        "filename": "fin_partida.ogg",
        "url": "https://cdn.pixabay.com/download/audio/2025/10/08/audio_836ffccdef.mp3?filename=alphix-game-over-417465.mp3",
        "title": "Game Over",
        "author": "Alphix",
        "source_page": "https://pixabay.com/sound-effects/musical-game-over-417465/",
        "license": "Pixabay Content License",
        "target_max_sec": 3.8,
        "is_loop": False,
        "target_peak_db": -1.5,
    },
    {
        "id": "burbujas",
        "category": "SFX",
        "filename": "burbujas.ogg",
        "url": "https://cdn.pixabay.com/download/audio/2025/09/18/audio_65babecca6.mp3?filename=dragon-studio-popping-bubbles-406647.mp3",
        "title": "Popping Bubbles",
        "author": "DRAGON-STUDIO",
        "source_page": "https://pixabay.com/sound-effects/film-special-effects-popping-bubbles-406647/",
        "license": "Pixabay Content License",
        "target_max_sec": 1.9,
        "is_loop": False,
        "target_peak_db": -1.5,
    },
    {
        "id": "chapoteo",
        "category": "SFX",
        "filename": "chapoteo.ogg",
        "url": "https://cdn.pixabay.com/download/audio/2024/08/03/audio_4361da9fce.mp3?filename=universfield-water-splash-229315.mp3",
        "title": "Water Splash",
        "author": "Universfield",
        "source_page": "https://pixabay.com/sound-effects/film-special-effects-water-splash-229315/",
        "license": "Pixabay Content License",
        "target_max_sec": 1.45,
        "is_loop": False,
        "target_peak_db": -1.5,
    }
]

def download_file(url, dest_path):
    print(f"Descargando: {os.path.basename(dest_path)}...")
    req = urllib.request.Request(
        url,
        headers={'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)'}
    )
    with urllib.request.urlopen(req) as resp, open(dest_path, 'wb') as f:
        f.write(resp.read())
    print(f"  -> Descargado ({os.path.getsize(dest_path)} bytes)")

def process_audio(info):
    raw_mp3 = os.path.join(TEMP_DIR, f"{info['id']}_raw.mp3")
    out_ogg = os.path.join(AUDIO_DIR, info['category'], info['filename'])
    
    # Descargar si no existe
    if not (os.path.exists(raw_mp3) and os.path.getsize(raw_mp3) > 1000):
        download_file(info['url'], raw_mp3)
    else:
        print(f"[{info['id']}] Usando archivo raw en caché.")
    
    # Cargar en aud
    snd = aud.Sound.file(raw_mp3)
    specs = snd.specs # (rate, channels)
    rate = int(specs[0])
    channels = int(specs[1])
    
    data = snd.data()
    total_samples = len(data)
    total_sec = total_samples / rate
    print(f"[{info['id']}] specs: {rate} Hz, {channels} ch, duración orig: {total_sec:.2f}s")
    
    # Calcular amplitud por muestra para detectar silencios
    amp = np.max(np.abs(data), axis=1)
    max_amp = np.max(amp)
    
    # Umbral de silencio (-45 dB respecto al pico)
    silence_thresh = max(0.005, max_amp * 0.008)
    active_indices = np.where(amp > silence_thresh)[0]
    
    if len(active_indices) > 0:
        # Añadir 10ms de margen antes y después
        pad_samples = int(0.010 * rate)
        start_sample = max(0, active_indices[0] - pad_samples)
        end_sample = min(total_samples, active_indices[-1] + pad_samples)
    else:
        start_sample = 0
        end_sample = total_samples
        
    start_sec = start_sample / rate
    end_sec = end_sample / rate
    
    # Si excede el máximo permitido para este SFX, recortar
    max_sec = info.get("target_max_sec", total_sec)
    if (end_sec - start_sec) > max_sec:
        end_sec = start_sec + max_sec
        
    trimmed_sec = end_sec - start_sec
    print(f"  -> Recorte: {start_sec:.3f}s a {end_sec:.3f}s (duración final: {trimmed_sec:.2f}s)")
    
    # Aplicar recorte
    trimmed_snd = snd.limit(start_sec, end_sec)
    
    # Fadeout suave si fue recortado forzosamente al final
    if trimmed_sec >= max_sec and not info['is_loop']:
        fade_len = min(0.12, trimmed_sec * 0.2)
        trimmed_snd = trimmed_snd.fadeout(trimmed_sec - fade_len, fade_len)
        
    # Normalizar volumen
    trimmed_data = trimmed_snd.data()
    cur_peak = np.max(np.abs(trimmed_data))
    target_peak = 10.0 ** (info['target_peak_db'] / 20.0) # e.g. -1.5 dB = 0.841
    vol_factor = target_peak / max(cur_peak, 1e-4)
    print(f"  -> Normalización: pico actual {cur_peak:.3f}, factor {vol_factor:.3f} (target {target_peak:.3f})")
    
    final_snd = trimmed_snd.volume(vol_factor)
    
    # Bitrate OGG Vorbis
    bitrate = 128000 if (info['is_loop'] or trimmed_sec > 5.0) else 96000
    
    # Escribir OGG
    final_snd.write(
        out_ogg,
        rate,
        channels,
        aud.FORMAT_S16,
        aud.CONTAINER_OGG,
        aud.CODEC_VORBIS,
        bitrate,
        65536
    )
    
    final_size = os.path.getsize(out_ogg)
    print(f"  -> GUARDADO: {out_ogg} ({final_size / 1024:.1f} KB)")
    
    info["final_sec"] = trimmed_sec
    info["final_size_kb"] = final_size / 1024
    info["final_path"] = out_ogg

def main():
    print("=== INICIANDO DESCARGA Y PROCESAMIENTO DE AUDIOS ECO SHARK ===")
    for t in TRACKS:
        try:
            process_audio(t)
        except Exception as e:
            print(f"ERROR procesando {t['id']}: {e}")
            raise
    print("\n=== TODOS LOS 11 AUDIOS PROCESADOS EXITOSAMENTE ===")

if __name__ == '__main__':
    main()
