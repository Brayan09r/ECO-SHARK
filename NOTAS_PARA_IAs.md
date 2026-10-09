# Notas de coordinación (Claude ↔ Antigravity)

Archivo compartido para que las IAs que trabajan en ECO SHARK sepan qué hizo la otra.
**Antes de cambiar algo, lee esto. Después de cambiar algo, agrega una entrada abajo.**

## Reglas de convivencia
- No borrar ni reescribir scripts de la otra IA: extenderlos y marcar el cambio con un comentario `// (Claude)` o `// (Antigravity)`.
- El `.fbx` del tiburón es la fuente original: no se modifica por código. Las versiones procesadas van a `Assets/EcoShark/Generated/`.
- Pipeline de render del proyecto: **Built-in** (no URP). Espacio de color: **Gamma**.
- Orientación del tiburón en Unity: nariz hacia **+Z**, lomo hacia **+Y** (lo requiere `SharkMovement`).

## Registro

### 2026-09-26: Claude, colores del tiburón martillo
**Problema encontrado**
1. `SharkVCol.mat` usaba `Sprites/Default`, un shader 2D sin luz, sin profundidad (ZWrite Off, Cull Off) y sin niebla. Se veían caras internas y el tiburón se veía plano.
2. Los vertex colors del FBX eran casi blancos (60–97 % de luminosidad), lejos del diseño `Diseno_Tiburon_Martillo_Perfil_2.5D.png`.

**Cambios**
- NUEVO `Assets/EcoShark/Shaders/EcoShark_VertexColorLit.shader`: usa vertex colors, tiene luz, sombras y niebla. El canal **alfa del vértice indica qué zona brilla** (marcas cian). Agrega un borde luminoso (rim).
- NUEVO `Assets/EcoShark/Editor/SharkColorizer.cs`, menú **ECO SHARK > 3. Pintar tiburón 3D**:
  - Crea `Generated/Meshes/Shark_Hammerhead_Pintado.asset` (copia del mesh del FBX con contrasombreado: lomo `#4E6474`, costados `#8196A4`, vientre `#EEE7D4`).
  - Conserva las marcas cian que venían del FBX (g>0.8, b>0.8, r<0.55) y las hace brillar.
  - Cambia el shader de `SharkVCol.mat` a `EcoShark/VertexColorLit`.
  - Crea los ojos naranjas `Ojo_Izq` y `Ojo_Der` en las puntas del martillo, como hijos del hueso `Head`.
- EDITADO `AutoFixShark.cs` (de Antigravity), 2 cambios marcados con `// (Claude)`:
  - ya no asigna `SharkVCol` a los renderers `Ojo_*`;
  - si el tiburón no está pintado, llama a `SharkColorizer.Apply`.

**Si re-exportas el FBX desde Blender:** vuelve a ejecutar *ECO SHARK > 3. Pintar tiburón 3D*. Para que las marcas se conserven, píntalas en cian (≈ `#59E7FF`).

### 2026-09-26 (2): Claude, ajuste final de colores y ojos
- Paleta final: lomo `#52687A`, costados `#7E919E`, vientre `#F3ECDA`. La línea lomo/vientre es más marcada.
- Shader: nuevo parámetro `_SelfLight` (0.35). Aclara el vientre, que casi no recibe luz del sol.
- **Cuidado con la escala:** el FBX se importa con escala 100 en el renderer. `SkinnedMeshRenderer.BakeMesh` devuelve los vértices SIN esa escala, así que hay que usar `TRS(pos, rot, 1)` y no `localToWorldMatrix`. Ya está corregido en `SharkColorizer`.
- Ojos: 0.16 u de diámetro, medio incrustados en las puntas del martillo y colgados del hueso `Head`.
- NUEVO `SharkSnapshot.cs`: menú **ECO SHARK > 4. Fotografiar tiburón**. Guarda 3 fotos y un `reporte.txt` en `D:\ECO SHARK\Capturas`.
  - Si al recargar Unity existe `Capturas/pedir_fotos.txt`, toma las fotos solo. Si el archivo contiene la palabra `pintar`, primero vuelve a pintar el tiburón y guarda la escena.
  - Sirve para que cualquier IA revise el resultado sin usar menús.
- Pendiente para quien modele en Blender: la aleta caudal tiene un vértice muy alargado hacia atrás. En las fotos se ve como una "aguja" en la cola.

### 2026-09-26 (3): Claude, diagnóstico de la animación "Swim" (pendiente para Antigravity)
Lo que se ve en Unity: la cola gira como una hélice.
Leí las curvas del FBX (`Armature_hammerhead|Swim`, 30 claves):
| Hueso | Eje animado (local) | Amplitud | Rotación en reposo (Y) |
|---|---|---|---|
| Spine_01 | Y | ±8.6° | -3.3° |
| Spine_02 | Y | ±14.3° | -10.3° |
| Tail_01 | Y | ±22.9° | -21.4° |
| Tail_02 | Y | ±34.4° | -34.4° |
| CaudalFin | Y | ±45.8° | -41.7° |
| Head | Y | ±2.9° | 0° |

**Causa:** en los huesos de Blender, el eje **Y local va a lo largo del hueso**. Rotar en Y es *torcer* el hueso sobre sí mismo (roll), y eso produce el efecto hélice. Además, la pose de reposo ya trae una torsión acumulada de hasta -41.7°. Por eso la aleta caudal se ve de canto, como una "aguja".
**Corrección esperada:** torsión Y = 0 en reposo y en la animación. El vaivén lateral va en el eje perpendicular al hueso y al lomo (normalmente Z local; confirmar con el roll del hueso), con desfase progresivo hacia la cola.

### 2026-09-26 (4): Antigravity, corrección de vaivén lateral y pose de reposo en Blender

**Problemas resueltos**
1. **Torsión / Efecto hélice eliminado:** Se eliminó toda rotación en el eje Y local de la columna y cola (`pbone.rotation_euler.y = 0.0`).
2. **Pose de reposo limpia:** Se dejaron todos los huesos en rotación 0° en reposo. La aleta caudal queda 100% vertical en el plano Y/Z ($x = 0$, $y$ de $-0.75$ a $+1.38$). La forma de "aguja" que se observaba de perfil se debía a la aleta caudal vista de canto por los $-41.7^\circ$ de torsión acumulada; al estar vertical, recupera su silueta heterocerca completa.
3. **Vaivén lateral en ola (eje Z local):** Se animó el movimiento lateral izquierda-derecha en el eje local Z (perpendicular al eje del hueso Y y al plano del lomo/vientre):
   - `Spine_01`: $\pm 4.0^\circ$, retraso 0 frames.
   - `Spine_02`: $\pm 8.0^\circ$, retraso 3 frames.
   - `Tail_01`: $\pm 14.0^\circ$, retraso 6 frames.
   - `Tail_02`: $\pm 20.0^\circ$, retraso 9 frames.
   - `CaudalFin`: $\pm 28.0^\circ$, retraso 12 frames.
   - `Head`: $\pm 3.0^\circ$, contrapeso en sentido contrario (signo opuesto).
   - `Pectoral_L` y `Pectoral_R`: aleteo suave ($\pm 2.0^\circ$) sincronizado en Z.
4. **Loop perfecto a 30 fps (30 frames):** Frecuencia $\omega = \frac{2\pi}{29}$ rad/frame; el frame 30 coincide de forma exacta (diferencia $< 10^{-15}$ grados) con el frame 1 tanto en rotación como en derivada/velocidad, eliminando tirones en Unity.
5. **Exportación FBX y Blend:**
   - Sobrescrito en `Assets/EcoShark/Models/Sharks/Shark_Hammerhead.fbx` y `.blend`.
   - Parámetros: `primary_bone_axis='Y'`, `secondary_bone_axis='X'`, `add_leaf_bones=False`, `bake_anim=True`, `bake_anim_step=1.0`, `bake_anim_simplify_factor=0.0`.
   - Vértices cian fijados en `#59E7FF` (`RGB: 0.35, 0.91, 1.0`) con `random.seed(42)` para que `SharkColorizer.cs` reconozca y preserve el brillo de las marcas.

### 2026-09-26 (5): Antigravity, nuevo modelo 3D protagonista estilo cartoon (Shark_Hammerhead_Cartoon)

**Archivos generados (no sobrescribe el original):**
- Modelo FBX: `Assets/EcoShark/Models/Sharks/Shark_Hammerhead_Cartoon.fbx`
- Archivo Blender fuente: `Assets/EcoShark/Models/Sharks/Shark_Hammerhead_Cartoon.blend`
- Texturas 2048x2048:
  * `Assets/EcoShark/Models/Sharks/Textures/Shark_Albedo.png`
  * `Assets/EcoShark/Models/Sharks/Textures/Shark_Emission.png`
- Renders de revisión (1920x1080):
  * `Assets/EcoShark/Art/Designs/Render_Cartoon_Perfil.png`
  * `Assets/EcoShark/Art/Designs/Render_Cartoon_TresCuartos.png`
  * `Assets/EcoShark/Art/Designs/Render_Cartoon_Frente.png`

**Especificaciones técnicas:**
- **Triángulos:** 6,954 (3,497 vértices), dentro del presupuesto objetivo de 3,000–8,000 triángulos para móvil/WebGL.
- **Topología:** Malla única 100% estanca (watertight), sin aristas no-manifold, sin agujeros ni normales invertidas. Mapeo UV limpio con dilatación de bordes (seam padding) para evitar costuras.
- **Pivot / Dimensiones:** Origen en el centro de masa `(0, 0, 0)`, longitud total de exactamente 3.00 unidades (`dimensions = 2.41 x 1.53 x 3.00`).
- **Orientación:** Modelado en Blender con nariz hacia `-Y` y lomo hacia `+Z`. Exportado con `Forward = -Z`, `Up = Y`, `Apply Transform = True`, de modo que en Unity queda con la nariz en **+Z** y el lomo en **+Y** (requisito estricto de `SharkMovement.cs`).
- **Pose de reposo:** 100% recta y simétrica, sin curvaturas residuales ni torsión. Aleta caudal 100% vertical en el plano Y/Z.
- **Sin esqueleto / Sin animación en FBX:** Es una malla limpia estática pensada para deformación por shader (vertex displacement / wave swim) o rigging procedural limpio.

**Diseño visual y estilo:**
- **Personaje guardián amigable:** Estilo cartoon/Pixar, proporciones gorditas y redondeadas sin rasgos amenazantes. Cabeza grande (≈38% del largo).
- **Ojos grandes y expresivos:** Ubicados en los extremos del martillo; esclerótica marfil, iris cálido degradado ámbar a oro (`#FF9500` / `#FFAE19`), pupila negra, destellos dobles de brillo y párpado superior relajado.
- **Sonrisa amigable:** Línea de sonrisa curva que recorre la mandíbula lateral con 3-4 dientitos perlados redondeados (sin colmillos afilados), hoyuelo en la mejilla e interior cálido, claramente visible tanto de frente como de perfil.
- **Colores y sombreado:** Lomo azul pizarra (`#5E7A8C`), vientre crema marfil (`#F3ECDA`) con línea de división ondulada dinámica; 6 marcas bioluminiscentes cian neón (`#59E7FF`) con mapa de emisión dedicado y oclusión ambiental (AO) horneada e integrada en la textura.

**Notas para Claude para integración en Unity:**
- Asignar un único material con shader Built-in (ej. `Standard` o shader dedicado) utilizando `Shark_Albedo.png` como Albedo y `Shark_Emission.png` como Emission Map.
- La escala de importación del FBX es estándar (1, 1, 1). No requiere `SharkColorizer` para pintar vertex colors ya que toda la paleta, contrasombreado, marcas cian y ojos están horneados en texturas de 2048x2048.


### 2026-09-26 (6): Claude, integración del tiburón cartoon en Unity
- NUEVO `Shaders/EcoShark_CartoonSwim.shader`: Albedo + Emission, sombreado toon en bandas, contorno (casco invertido), `noambient` + luz ambiente neutra propia (`_SelfLight` 0.4), `_LightTint` 0.3 (que la luz azul no tiña el vientre) y `nofog` para el protagonista.
  - **Nado sin huesos**: onda lateral en el vertex shader, casi nula en la nariz y máxima en la cola.
- NUEVO `Scripts/Player/SharkSwimWave.cs` (en `Tiburon_Cartoon`): calcula los ejes del modelo y controla la fase y la amplitud. A más velocidad del tiburón, más rápido el coletazo (0.8 → 2.2 por segundo).
- NUEVO `Editor/SharkCartoonInstaller.cs`, menú **ECO SHARK > 5. Usar tiburón cartoon**:
  - instancia `Shark_Hammerhead_Cartoon.fbx` como `Tiburon_Guardian/Tiburon_Cartoon` y lo auto-orienta (ya venía bien: nariz +Z, lomo +Y);
  - crea `Generated/Materials/SharkCartoon.mat` y ajusta el collider (3.04 u de largo) y la `Boca`.
- `Tiburon_Martillo_3D` (el modelo anterior) quedó **desactivado** como respaldo, no borrado: `AutoFixShark` lo necesita para no re-equiparlo.
- `SharkSnapshot`: si `Capturas/pedir_fotos.txt` contiene la palabra `cartoon`, reinstala el tiburón cartoon antes de tomar las fotos.
- Si Antigravity re-exporta `Shark_Hammerhead_Cartoon.fbx`: mantener la escala 1, la nariz en -Y de Blender y el cuerpo recto. Luego usar el menú 5.

### 2026-09-27: Claude, nado visible en 2.5D
- Problema: la cámara ve de costado, así que el vaivén lateral puro de la cola (el realista) va hacia la pantalla y no se ve.
- Solución:
  - NUEVO `SharkViewTilt.cs` en `Tiburon_Cartoon`: inclina el modelo 22° con el lomo hacia la cámara; solo lo visual, el Rigidbody no cambia.
  - Shader: `_SwimVertical` (0.3) suma a la onda un poco de movimiento vertical.
  - `SharkSwimWave`: amplitud 0.4, coletazos de 1.0 a 2.6 por segundo; los ejes ahora se toman del propio modelo.
- Verificado en Play: la cola se ve ondular al nadar.

### 2026-09-27 (2): Claude, tiburón de Tripo + nado estilo juego móvil
- Modelo del usuario (Tripo): `Models/Sharks/cartoon_shark_3d_model.glb`. Tiene compresión meshopt y 1.9 M de triángulos (inviable para WebGL).
  - Procesado fuera de Unity con gltf-transform: decodificado, soldado y simplificado a 15,116 triángulos conservando las UV. Escalado a 3 u de largo y centrado.
  - Resultado en `Models/Sharks/Tripo/`: `Tiburon_Tripo.obj`, `Tiburon_Tripo_Albedo.png` (2048) y `Tiburon_Tripo_Emission.png` (máscara de las rayas cian).
  - El `.glb` original se puede borrar o mover fuera de `Assets` (16 MB que no se usan).
- Menú **ECO SHARK > 6. Usar tiburón de Tripo**, o `pedir_fotos.txt` con la palabra `tripo`. Reemplaza `Tiburon_Cartoon` con el modelo de Tripo; sigue siendo un solo hijo con ese nombre.
- `SharkSwimWave` reescrito: coletazo según la velocidad; con impulso (Shift) amplitud ×1.5 y frecuencia ×1.4; **curva al girar** (`_Bend` en el shader: la cola se queda atrás en los giros).
- Corrección: el primer OBJ salió sin UV (sin coordenadas de textura). Re-exportado con UV y con la orientación ya aplicada: en el GLB la cabeza estaba en -Z; se giró 180° en Y. El instalador ya no auto-orienta este modelo.
- Ajustes finales: amplitud 0.5, mezcla vertical 0.4, contorno 0.012 (sin puntitos negros). Verificado en Play: coletazo y curva al girar visibles.

### 2026-09-27 (3): Claude, tiburón "atlético" de Tripo + cola suave cerca de la superficie
- El usuario cambió el diseño: `Models/Sharks/tiburon_atletico_tripo.glb`. Se procesó igual que el anterior (15,290 triángulos). Sobrescribió los archivos de `Models/Sharks/Tripo/`, que conservan el mismo nombre, así que el menú 6 sigue sirviendo. El diseño anterior se envió a la Papelera.
- Problema: al nadar hacia la superficie, la cola daba ondas bruscas y distorsionadas. Causas y arreglos:
  1. **La curva al girar se medía alrededor del eje lateral del tiburón.** Ese eje se invierte con la voltereta izquierda/derecha. Arreglo: `SharkSwimWave` mide el giro en el plano de la pantalla (eje Z del mundo) y pasa la curva al shader como vector `_BendDir`, en espacio del objeto. La curva está limitada: `maxBendChangePerSecond` 2.5 y `maxTurnRate` 400 °/s (los giros más rápidos se ignoran).
  2. **El tiburón chocaba de punta contra el techo y se quedaba vertical, temblando.** Arreglo en `SharkMovement`:
     - Zona suave (`edgeSoftZone` 1.5, `surfaceMargin` 0.9, `floorMargin` 0.6): cerca del techo o del fondo se frena el componente vertical, y lo bloqueado se convierte en planeo horizontal.
     - `levelOutSpeed` sube a 3.
     - La rotación solo sigue la velocidad si ésta supera 0.5 u/s.
  3. **El plano del agua estaba lejos del plano de juego y parecía que el tiburón salía del agua.** `Entorno/Superficie` se movió a pos (0, 6.05, -5.25), escala (160, 0.1, 11.5).
- `SharkViewTilt`: inclinación por defecto de 20°.
- `pedir_fotos.txt` acepta nuevas órdenes, una por línea:
  - `borrar:Assets/ruta` → Papelera;
  - `transform:Ruta/Objeto|pos=x,y,z|scale=x,y,z`;
  - `Componente.campo=valor` (p. ej. `SharkSwimWave.verticalMix=0.3`).
  - Las palabras clave (`tripo`, `cartoon`, `pintar`) ahora deben ir **solas en su línea**. Antes, un nombre de archivo que contenía "cartoon" disparaba el instalador.
- Verificado en Play: al subir, el tiburón se nivela bajo la línea del agua y planea con la cola suave. También gira sin tirones.

### 2026-09-27 (4): Claude, aleteo DE LADO A LADO (no de arriba abajo)
- Queja del usuario: al nadar hacia los lados, la cola parecía aletear de arriba abajo, como un delfín. Causas:
  1. `verticalMix` estaba en 0.4: el 40 % del coletazo era sube y baja real.
  2. La onda viajaba de la cola a la cabeza (`sin(fase - along*k)`), al revés que en un pez.
  3. La cola solo se **desplazaba** hacia el costado; eso, visto de costado, apenas se nota.
- Shader `EcoShark_CartoonSwim`: `SwimOffset` se reemplazó por `SwimDeform(pos, normal)`:
  - la onda va de la nariz a la cola (`sin(fase + along*k)`);
  - cada rodaja del cuerpo **gira** alrededor del eje del lomo, según la pendiente de la curva. Por eso la aleta caudal se ve de canto y luego de frente, que es lo que delata el movimiento lateral desde la cámara 2.5D;
  - el largo se conserva: al curvarse, la cola se acerca a la nariz (integral de 8 muestras en el vertex shader);
  - las normales también giran, así cambia la luz sobre la cola;
  - `_SwimVertical` sigue existiendo, pero ahora vale 0 (solo lateral). `_BendDir` (curva al girar) no cambió.
- `SharkSwimWave.verticalMix` = 0 (en el script y en la escena).
- `SharkViewTilt.tiltDegrees` = 12 (antes 20): con más inclinación, el vaivén lateral se proyectaba como sube y baja en pantalla.
- Verificado en Play: la aleta se abre y se cierra, y la punta casi no cambia de altura.

### 2026-09-27 (5): Claude, aleteo más calmado (pedido del usuario)
- `SharkSwimWave`: `idleBeatsPerSecond` 1.0 → 0.8, `maxBeatsPerSecond` 2.6 → 1.8, `boostBeatsMultiplier` 1.4 → 1.25 (con Shift, máximo ≈ 2.25 coletazos/s; antes ≈ 3.6).

### 2026-09-27 (6): Claude, aleteo aún más calmado (valores pedidos por el usuario)
- `SharkSwimWave`: quieto 0.5, a toda velocidad 1.2 y con Shift 1.9 coletazos/s (`idleBeatsPerSecond` 0.5, `maxBeatsPerSecond` 1.2, `boostBeatsMultiplier` 1.583).

### 2026-09-27 (7): Claude, aleteo lento (valores pedidos por el usuario)
- `SharkSwimWave`: quieto 0.2, a toda velocidad 0.8 y con Shift 1.1 coletazos/s (`idleBeatsPerSecond` 0.2, `maxBeatsPerSecond` 0.8, `boostBeatsMultiplier` 1.375).

### 2026-09-27 (8): Claude, cielo con nubes y olas animadas (estilo Hungry Shark)
- Problema: no se veía cielo. El cubo `Entorno/Cielo` estaba lejos, tapado por la niebla color agua, y la placa horizontal `Entorno/Superficie`, vista desde abajo, era una franja clara. Al llegar arriba, el tiburón parecía flotar en el aire.
- NUEVO `Scripts/World/OceanSurface.cs` + `Shaders/EcoShark_WaterSurface.shader` (objeto `Entorno/Olas`, en z = 0.6, detrás del tiburón):
  - franja vertical cuyo borde de arriba es la línea del agua, con 3 ondas sumadas que viajan, espuma blanca y una banda de luz debajo;
  - ondas extra alrededor del tiburón cuando nada cerca de la superficie, más fuertes si va rápido;
  - los parámetros van como globales `_EcoWave*` / `_EcoRipple`. `OceanSurface.HeightAt(x)` da la misma altura que el shader.
- NUEVO `Scripts/World/SkyBackdrop.cs` + `Shaders/EcoShark_SkyBackdrop.shader` (objeto `Main Camera/FondoCielo`, a 150 u de la cámara, cola Background y sin niebla):
  - arriba, cielo degradado con nubes cartoon que se mueven; abajo, agua de clara a oscura;
  - cada cuadro mueve el horizonte para que coincida con la superficie del plano de juego.
- NUEVO `Scripts/World/FloatOnWaves.cs` en `Barco_PescaIlegal/Modelo`: el barco sube, baja y se ladea con las olas (`draft` 0.45 lo hunde un poco). Solo mueve el modelo; el collider y la lógica no cambian.
- NUEVO `Editor/OceanSurfaceInstaller.cs`, menú **ECO SHARK > 7. Cielo y olas**, o la línea `cielo` en `pedir_fotos.txt`:
  - desactiva (no borra) `Entorno/Superficie` y `Entorno/Cielo`;
  - crea los objetos de arriba;
  - `CameraFollow.extraAboveSurface` = 4.5 (se ve cielo al subir).
- Los colores del agua deben coincidir entre `Olas.mat` y `FondoCielo.mat` (`_WaterTop`); el instalador los iguala.
- Verificado en Play: al subir, el tiburón queda justo bajo las olas, con cielo y nubes arriba, y el agua se agita a su alrededor.

### 2026-09-27 (9): Claude, fondo marino "vivo" + sistema de sonido
**Fondo marino** — menú **ECO SHARK > Configurar Fondo Marino** (o la línea `fondo` en `pedir_fotos.txt`). Crea `FondoMarinoVivo`; si se vuelve a ejecutar, lo reconstruye desde cero.
- Atmósfera: `UnderwaterAtmosphere` pone niebla exponencial (0.02) de #0B2B47 en lo profundo, que se aclara a turquesa cerca de la superficie según la altura de la cámara. El fondo lejano (`FondoCielo.mat`) va de turquesa a azul marino, y `Olas.mat` usa el mismo `_WaterTop`.
- Rayos de sol: `RayosDeSol` + `GodRays.cs` + shader `EcoShark/GodRay` (aditivo). Cada rayo se balancea, pulsa y "respira" con `Mathf.Sin`.
- Paralaje: `ParallaxLayer.cs` calcula cuánto debe acompañar cada capa a la cámara para que su velocidad en pantalla sea exacta:
  - montañas lejanas 15 % y abismos 20 % (shader `EcoShark/Silhouette`, mallas `Cordillera_*`);
  - rocas y arrecifes 50 %;
  - primer plano 120 % (algas oscuras y rocas en z = -7, solo en horizontal).
  - OJO: la velocidad se pasa como número. Antes se leía del nombre y "120%" contenía "20%"; ya está corregido.
- Partículas (shader `EcoShark/ParticleSoft`, sin texturas):
  - `Main Camera/Plancton`: motes en espacio de mundo;
  - `Burbujas/Grieta_Burbujas`: burbujas desde el suelo que desaparecen al cruzar `LimiteSuperficie`; además, el shader recorta todo por encima de `_MaxY`.
- Lecho marino (`LechoMarino`, z de 1.8 a 7): algas con vaivén en el vertex shader (`EcoShark/ToonEnv`, `_SwayAmp`) y corales ramificados, abanico (se mece), cerebro y tubos en naranja, morado, rosa y amarillo.
- Mallas generadas en `Generated/Meshes/FondoMarino` (rocas low-poly y algas) y materiales en `Generated/Materials/FondoMarino`. Todos con GPU instancing: en Play unas 290 FPS y unos 50 batches.
- La decoración vieja (`Entorno/Decoracion (fondo con paralaje)`) quedó desactivada; `Entorno/FondoMarino` usa ahora la arena toon.
- La franja de olas ahora es transparente en su parte baja, para no cortar en línea recta lo que hay detrás.
- Capturas de validación: **ECO SHARK > Capturas del fondo marino** (o la línea `capturas fondo`) genera `Capturas/fondo_superficie.png`, `fondo_medio.png` y `fondo_profundo.png`.

**Sonido** — `Scripts/Core/EcoAudio.cs` (objeto `Audio` en la escena; menú **ECO SHARK > 8. Preparar sonido**):
- Carga los audios por NOMBRE desde `Assets/EcoShark/Resources/Audio/{Musica,Ambiente,SFX}/`. La lista está en `LEEME_AUDIO.txt`. Si falta alguno, no suena y se avisa en la consola.
- Música y ambiente en bucle; el agua al nadar sube de volumen con la velocidad.
- Efectos: impulso, recoger basura, rescate, animal perdido, golpe, fin de partida, burbujas y chapoteo.
- Tecla M: silenciar (`EcoInput.MutePressed`, agregado por Claude).
- **Para Antigravity:** descarga solo audios con licencia libre (CC0 o licencia de Pixabay), ponlos con esos nombres exactos y anota fuente y licencia en `CREDITOS_AUDIO.md`.

### 2026-09-27 (10): Antigravity, integración y procesamiento de los 11 audios del juego

**Audios descargados, procesados y colocados:**
Todos los audios fueron obtenidos de fuentes libres autorizadas (OpenGameArt y Pixabay) bajo licencias **CC0** y **Pixabay Content License** (comercial, sin atribución obligatoria). Fueron procesados mediante Audaspace (`aud`) en Blender: recorte de silencios iniciales y finales, normalización de volumen pico (SFX -1.5 dB, Música -1.2 dB, Ambiente -3.0 dB) y codificados a formato `.ogg` optimizado para WebGL/móvil:

- `Assets/EcoShark/Resources/Audio/Musica/musica_fondo.ogg` (2.7 MB, 2:50 min, bucle alegre y tranquilo submarino, CC0).
- `Assets/EcoShark/Resources/Audio/Ambiente/ambiente_submarino.ogg` (914 KB, 1:22 min, bucle subacuático con resonancia marina y burbujas lejanas, Pixabay).
- `Assets/EcoShark/Resources/Audio/SFX/nado_loop.ogg` (30 KB, 2.68 s, bucle suave de desplazamiento de agua, Pixabay).
- `Assets/EcoShark/Resources/Audio/SFX/impulso.ogg` (11 KB, 0.71 s, whoosh ágil subacuático para aceleración/boost, Pixabay).
- `Assets/EcoShark/Resources/Audio/SFX/recoger_basura.ogg` (6 KB, 0.17 s, pop brillante y satisfactorio, Pixabay).
- `Assets/EcoShark/Resources/Audio/SFX/rescate_animal.ogg` (18 KB, 1.36 s, campanitas de logro armónicas, Pixabay).
- `Assets/EcoShark/Resources/Audio/SFX/animal_perdido.ogg` (21 KB, 1.65 s, sonido cómico-triste descendente, Pixabay).
- `Assets/EcoShark/Resources/Audio/SFX/golpe.ogg` (10 KB, 0.95 s, impacto sordo bajo el agua, Pixabay).
- `Assets/EcoShark/Resources/Audio/SFX/fin_partida.ogg` (21 KB, 1.93 s, melodía corta y suave de fin de juego, Pixabay).
- `Assets/EcoShark/Resources/Audio/SFX/burbujas.ogg` (19 KB, 0.88 s, ráfaga de burbujas cristalinas, Pixabay).
- `Assets/EcoShark/Resources/Audio/SFX/chapoteo.ogg` (16 KB, 1.14 s, chapoteo en superficie, Pixabay).

**Documentación y verificación:**
- Creado `D:\ECO SHARK\CREDITOS_AUDIO.md` con la tabla detallada de archivo, título original, autor, URL fuente y licencia.
- No se modificó ningún script (`EcoAudio.cs` sin tocar) ni escenas. Todos los archivos cumplen con el peso (< 3 MB en SFX, < 5 MB en música) y con los nombres exactos requeridos por `Resources.Load<AudioClip>`.

### 2026-09-27 (10): Claude, menú principal, nueva barra de energía y océano grande
**Menú principal** (`Scripts/UI/MainMenuController.cs`, objeto `MenuPrincipal`; menú **ECO SHARK > 9. Menú principal y HUD** o la línea `menu`):
- `GameManager` tiene un nuevo estado `GameState.Menu`. Si hay un MainMenuController, la escena empieza en el menú: el tiburón posa sin física, toxicidad, recolección ni HUD, y nada de eso cuenta para la partida.
  - `StartRun()` pasa del menú a la partida.
  - `Restart()` ("Jugar de nuevo") salta el menú con `GameManager.SkipMenuOnce`; `ReturnToMenu()` vuelve a él.
- Contenido del menú:
  - cámara cercana (`CameraFollow.Offset`, `ClampToOcean`, `Snap()`) y el coletazo forzado con `SharkSwimWave.ForcedSpeed01`;
  - banner "TIBURÓN GUARDIÁN" con NVL = suma de mejoras + 1 (`Progression.GuardianLevel`, 15 segmentos);
  - botones MEJORAS (con globo de mejoras que ya puedes comprar) y ¡JUGAR! (también con Enter o Espacio);
  - accesos a Misión ODS (ODS 14 y 12, metas 14.1, 14.4 y 12.5), Cómo jugar y Ajustes (sonido, pantalla completa, borrar progreso);
  - impacto acumulado (`Progression.TotalTrash`, `TotalRescued`), logo y tira de mejoras.
- El panel de fin de partida tiene un botón **MENÚ** que se agrega al iniciar.
- IMPORTANTE: Unity envía `OnTrigger*` aunque el componente esté desactivado. Por eso `SharkCollector.TryCollect` y `TrappedAnimal` comprueban `isActiveAndEnabled`, y los contadores del `GameManager` solo suman en `Playing`.

**Interfaz**
- `Scripts/UI/UiKit.cs`:
  - fuentes Lilita One y Baloo 2 en `Resources/Fonts` (SIL OFL; ver `LICENCIA_FUENTES.txt`);
  - sprites redondeados 9-slice, degradados e íconos dibujados por código (campos de distancia);
  - botones con rebote (`UiBounce`).
- `Scripts/UI/HudTopBar.cs` (en el objeto `HUD`):
  - nueva barra de ENERGÍA con insignia de aleta, degradado verde → amarillo → rojo y barra "fantasma" del daño;
  - sacudida al recibir daño; pulso y aviso "¡Energía baja! Come basura" por debajo del 28 %;
  - contadores en píldoras con íconos.
  - Oculta la barra y los contadores viejos (HUDController no cambió). También cambia las fuentes del HUD y redondea sus paneles.

**Océano grande** (**ECO SHARK > 10. Océano grande** o la línea `oceano`):
- `OceanArea`: X de -150 a 150 e Y de -40 a 6 (antes -40..40 y -22..6).
- Suelo, olas (`halfWidth`), fondo marino y montañas ajustados al nuevo tamaño.
- Spawner: botellas 60, bolsas 45, latas 45, petróleo 12, barriles 14.
- 3 barcos de pesca ilegal y 2 animales atrapados más.

### 2026-10-02: Claude, controles táctiles, guía de rescates, combo/misiones y cardúmenes
Menú **ECO SHARK > 11. Controles táctiles, guía y peces** (o la línea `mejoras` en `pedir_fotos.txt`). Se puede repetir: no duplica nada. Agrega 3 componentes al objeto `HUD` y crea el objeto `VidaMarina`.

**Controles táctiles** — `Scripts/UI/TouchControls.cs` (en `HUD`):
- Joystick flotante (aparece bajo el dedo en la mitad izquierda), botón IMPULSO abajo a la derecha y botón de PAUSA arriba.
- Escribe en `EcoInput.VirtualAxis` / `VirtualBoost`; `SharkMovement` no cambió.
- Aparecen solos en móvil o al detectar un dedo (`EcoInput.TouchActive`). En PC quedan ocultos, salvo `forceVisible`: menú **ECO SHARK > 12** o la línea `tactil` (alterna) para probarlos con el mouse.
- Mientras están visibles, `EcoInput.PointerHeld` devuelve false: "mantener el dedo para ir hacia ahí" se desactiva, porque tocar un botón llevaba al tiburón hacia ese botón. También se oculta el letrero `FondoControles` ("Mover: WASD...").
- `UiKit`: ícono nuevo `UiIcon.Pause` (al final del enum).

**Guía de rescates** — `Scripts/UI/ObjectiveGuide.cs` (en `HUD`):
- Hasta 3 flechas en el borde de la pantalla hacia los animales atrapados más cercanos, con la distancia. Se ponen rojas y laten cuando `TimeLeft01` < 0.35. Desaparecen cuando el animal ya se ve.
- `TrappedAnimal` tiene la propiedad nueva `IsFinished`.

**Combo y misiones** — `Scripts/UI/RunGoals.cs` (en `HUD`):
- Combo: recoger basura con menos de 3 s entre una y otra. Cada 5 seguidas sube el multiplicador (x2, x3, x4) y cada residuo da `biomass × (multiplicador − 1)` extra.
- Misiones en cadena: recoger N residuos → rescatar N animales → combo de N, y vuelve a empezar con metas más altas. Premio: 40 de biomasa, +25 por misión.
- Números flotantes "+5" junto al tiburón.
- La biomasa extra entra por `GameManager.AddBonusBiomass(int)` (nuevo). No se guarda nada nuevo en `Progression`.

**Cardúmenes** — `Scripts/World/FishSchools.cs` (objeto `VidaMarina`) + `Resources/Shaders/EcoShark_Fish.shader`:
- 18 cardúmenes de 20 peces repartidos por todo el `OceanArea`, en z de 0.9 a 2.6 (detrás del plano de juego). Solo decoración: sin colliders.
- Huyen del tiburón (`fleeRadius` 4.5) y se reagrupan.
- Todos los peces van en UNA malla dinámica (4,320 vértices, 1 draw call). No usé GPU instancing a propósito: el material se crea por código y, en la build, Unity descarta las variantes de instancing de materiales que no están en la escena.
- El shader está en `Resources` para que entre en la build.

**Verificado en Play (editor):** compila sin errores ni avisos; joystick con el mouse, combo x2, misión 5/15, flechas y peces visibles.
**NO verificado:** dedo real en un teléfono, multitáctil (joystick + impulso a la vez) y build WebGL. Falta hacer esa build de prueba.
**Pendiente:** jaula, animales y basura siguen con el arte anterior (no se rehicieron en estilo toon).

### 2026-10-02 (2): Claude, capturas para el pitch
- NUEVO `Scripts/Core/PitchCapture.cs` (todo dentro de `#if UNITY_EDITOR`, no entra en la build): en Play, la tecla **F9** guarda la vista Game en `Capturas/pitch_HHmmss.png` al triple de resolución. No hay que instalar nada: se crea solo al entrar en Play.
- Pitch de 3 minutos (10 diapositivas, notas del orador con tiempos): `ECO_SHARK_Pitch_Deck_3Min.pptx` en la raíz del proyecto. Falta insertar el tráiler en la diapositiva 2.
- Observado al capturar (sin corregir): el contador de 60 s de `TrappedAnimal` corre también en el menú, así que los animales de la escena se pierden aunque la partida no haya empezado; y las flechas de `ObjectiveGuide` pueden quedar debajo de los contadores de arriba a la derecha.

### 2026-10-02 (3): Claude, tráiler de 30 s grabado desde el juego
- NUEVO `Scripts/Core/TrailerDirector.cs` (todo dentro de `#if UNITY_EDITOR`): si existe `Capturas/grabar_trailer.txt` al entrar en Play, un piloto automático maneja al tiburón con `EcoInput.VirtualAxis` y graba 10 tomas en `Capturas/trailer/*.mp4` (menú, inicio, rescate, limpiar, impulso, barco, petróleo, barril, fondo, táctil). Usa `UnityEditor.Media.MediaEncoder` y `Time.captureFramerate = 30`; al terminar sale de Play solo. Poner antes la vista Game en "Full HD (1920x1080)".
- Los clips salen SIN audio. El montaje (cortes, rótulos con Lilita One, tarjeta final y `musica_fondo.ogg`) se hizo fuera de Unity con ffmpeg.
- Resultado: `ECO_SHARK_Trailer_30s.mp4` en la raíz del proyecto (29.5 s, 1920x1080, 30 fps).
- `TouchControls`: nuevos `ForceVisible`, `Preview(axis, boost)` y `StopPreview()` para mover el joystick por código. `FishSchools`: nuevo `TryGetNearestSchool`.
