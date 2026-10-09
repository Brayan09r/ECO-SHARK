# ECO SHARK: guía del proyecto (MVP)

Juego 2.5D en Unity: un tiburón guardián limpia el océano en vez de cazar.
ODS 14 (Vida submarina) + ODS 12 (Producción y consumo responsables).

---

## 1. Qué hay en esta carpeta

```
ECO SHARK/
├── LEEME_ECO_SHARK.md        ← esta guía
└── EcoShark/                 ← ARRASTRA esta carpeta dentro de "Assets" en Unity
    ├── Editor/
    │   └── EcoSharkSceneBuilder.cs   (menú "ECO SHARK": arma la escena en 1 clic)
    └── Scripts/
        ├── Core/          EcoInput, GameEvents, GameManager, Progression (economía)
        ├── Player/        SharkMovement, SharkHealth, SharkCollector
        ├── Interactables/ ICollectible, TrashCollectible, TrappedAnimal
        ├── Hazards/       OilSlick, RadioactiveBarrel, IllegalFishingBoat
        ├── World/         OceanArea, OceanSpawner, CameraFollow
        ├── Data/          TrashData (ScriptableObject)
        └── UI/            HUDController
```

---

## 2. Ponerlo a funcionar (10 minutos)

1. **Unity Hub → New project → plantilla "Universal 3D"** (Unity 6).
   Ubicación: `D:\ECO SHARK` · Nombre: `EcoSharkGame`.
   *(Unity Hub crea la subcarpeta `D:\ECO SHARK\EcoSharkGame`.)*
2. Cuando abra Unity, **arrastra la carpeta `EcoShark`** (la de esta guía) a la ventana **Project**, dentro de `Assets`.
3. Espera a que compile (la ruedita abajo a la derecha). Aparece un menú nuevo arriba: **ECO SHARK**.
4. **ECO SHARK → 1. Crear escena MVP (greybox)**.
5. Presiona **Play ▶**.

> Si Unity pregunta por el *Input System* y ofrece reiniciar, acepta. Los scripts funcionan con el sistema nuevo y con el antiguo.

**Controles**

| Acción | Teclado / Mando | Mouse / Táctil |
|---|---|---|
| Nadar 360° | WASD / Flechas / Stick | Mantener clic o dedo: nada hacia el puntero |
| Impulso | Shift o Espacio / Botón A | — |
| Pausa | P o Esc | — |
| Reiniciar (al perder) | R o Enter | Botón "Jugar de nuevo" |

**Menú de pruebas:** `ECO SHARK → Pruebas → Dar 500 de biomasa` o `Borrar progreso guardado`, para probar la tienda sin jugar 10 partidas.

---

## 3. Roadmap hacia el MVP

La regla: **primero jugable y feo, después bonito.** Cada fase termina con algo que puedes mostrar.

| Fase | Objetivo | Terminado cuando… | Estado |
|---|---|---|---|
| **0. Base** | Proyecto Unity 6 + Git (GitHub Desktop) + **build WebGL vacío subido** | Hay un link que abre en el navegador | Tú |
| **1. Sensación de nado** | Movimiento 360° en X/Y, Z bloqueada, rotación con `LookRotation` | Nadar se siente fluido y "de agua" | ✅ Incluido |
| **2. Bucle central** | Toxicidad drena → comer basura cura → muerte → reinicio | Una partida dura 1–3 min y dan ganas de repetir | ✅ Incluido |
| **3. Economía** | Basura → Biomasa → tienda (velocidad, imán, resistencia) → persistencia | Las mejoras se notan en la siguiente partida | ✅ Incluido (UI provisional) |
| **4. Rescate + obstáculos** | Redes con animales, petróleo, barriles, barco ilegal | Cada peligro se reconoce y se entiende sin explicación | ✅ Incluido |
| **5. Balance** | Ajustar números en el Inspector con 3–5 compañeros jugando | Nadie muere antes de 45 s ni sobrevive infinito | Tú |
| **6. UI desde Figma** | HUD, menú principal, tienda, pausa, tarjeta educativa | La UI final reemplaza la provisional | Tú |
| **7. Arte y feedback** | Modelos 3D, animación, partículas, sonidos ("ñam", golpe, rescate) | Se ve y se oye como juego terminado | Tú |
| **8. WebGL final** | Optimizar, publicar, probar en la PC del aula | El link funciona en Chrome y Edge sin instalar nada | Tú |
| **9. Evidencia académica** | Encuesta pre/post (5 preguntas sobre contaminación marina) | Tienes datos para la conclusión del informe | Tú |

**Por qué exportar WebGL desde la Fase 0:** los errores de WebGL (memoria, compresión, input) se detectan tarde si esperas al final. Un build temprano te ahorra el susto de la semana de entrega.

### Wireframes para Figma (Fase 6)
1. **HUD**: barra de energía/toxicidad (arriba izquierda), biomasa, rescates, tiempo (arriba derecha), tarjeta educativa (abajo al centro).
2. **Fin de partida + Tienda**: estadísticas, biomasa disponible, 3 tarjetas de mejora (nombre, nivel x/5, costo), botón "Jugar de nuevo".
3. **Menú principal**: Jugar, Cómo jugar, Créditos, ODS (pantalla que explica ODS 12 y 14).
4. **Pausa**.
5. **Tarjeta educativa**: título corto + 1 dato + ícono del residuo o animal.

---

## 4. Script de movimiento: lo esencial (`SharkMovement.cs`)

- **Z bloqueada:** `rb.constraints = FreezePositionZ` + se corrige `z = 0` cada paso de física.
- **Nado fluido:** la velocidad se acerca a la deseada con `Vector3.MoveTowards` (aceleración y frenado distintos → sensación de agua).
- **Rotación hacia el movimiento:**
  ```csharp
  Vector3 up = Vector3.Cross(Vector3.forward, dir);   // "lomo" perpendicular a la dirección, en el plano X/Y
  if (!facingRight) up = -up;                         // al ir a la izquierda, voltea para no nadar panza arriba
  Quaternion target = Quaternion.LookRotation(dir, up);
  rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, 1f - Mathf.Exp(-turnSharpness * dt)));
  ```
  Si usas `LookRotation(dir)` sin el vector `up`, el tiburón se pone de cabeza al nadar a la izquierda y "tiembla" al nadar recto hacia arriba. El `up` calculado lo evita y además genera el giro de barril al cambiar de lado.
- **Entrada en `Update`, física en `FixedUpdate`**, con `Interpolate` en el Rigidbody para que no vibre.

### Cambiar el tiburón por tu modelo 3D
1. Arrastra tu modelo como hijo de `Tiburon_Guardian` y borra el hijo `Modelo (reemplazar por tu 3D)`.
2. La **nariz debe apuntar a +Z** (flecha azul). Si no, rota el hijo (p. ej. Y = 90) **o** usa `Model Rotation Offset` en `SharkMovement`.
3. Ajusta el `CapsuleCollider` del padre al tamaño del modelo.

---

## 5. Arquitectura del sistema de vida

**Idea clave:** separar *quién guarda la vida* de *quién la cambia* y de *quién la muestra*.

```
   [Basura] --Heal()--------->  +------------------+  --HealthChanged-->  [HUD: barra]
   [Rescate] --Heal()-------->  |   SharkHealth    |  --Damaged-------->  [HUD: parpadeo rojo]
   [Barril/Barco] --TakeDamage->|  (solo números)  |  --Died---------->  [GameEvents] --> [GameManager]
   [Petróleo] --SetExtraDrain-> |  drena en Update |                                        |
   [GameManager] --Dificultad-> +------------------+                        guarda biomasa, muestra tienda
```

**Fórmula del drenaje (por segundo):**
`drenaje = drenajeBase × dificultad(tiempo) × resistencia(mejora) + extras(petróleo)`

Qué hace cada pieza:

1. **`SharkHealth` (modelo):** guarda `Current`/`Max`, drena con `Time.deltaTime` en `Update`, expone `Heal`, `TakeDamage` (con invulnerabilidad breve para que un barril no te mate en un solo roce) y eventos C#. **No conoce** la basura ni el HUD.
2. **`ICollectible` + `TrashCollectible` (fuentes de curación):** cada basura sabe cuánto cura por su **`TrashData`** (ScriptableObject). Para balancear no se toca código: se edita el asset.
3. **`SharkCollector` (boca + imán):** detecta coleccionables con `OverlapSphereNonAlloc` (sin generar basura de memoria, importante en WebGL), los atrae y al tocarlos llama `Heal`.
4. **`GameEvents` (Observer):** anuncia "se recogió basura", "se rescató un animal", "murió el tiburón". HUD y GameManager escuchan sin depender unos de otros.
5. **`HUDController` (vista):** solo dibuja. Cuando llegue el diseño de Figma se cambia la vista y la lógica queda igual.

Ventajas que puedes defender: **responsabilidad única**, **bajo acoplamiento**, **datos separados de la lógica** (balance sin programar) y **extensible** (un nuevo residuo o power-up es un asset nuevo, no un `if` más).

---

## 6. Exportar a WebGL

1. `File → Build Profiles → Web → Switch Platform`.
2. Agrega `EcoShark_MVP` a la lista de escenas (el menú ECO SHARK ya la agrega).
3. `Player Settings → Publishing Settings`: si tu hosting no sirve archivos comprimidos correctamente (errores de "Content-Encoding"), activa **Decompression Fallback** o pon **Compression Format: Disabled**.
4. Build y súbelo a Unity Play (como ya hiciste antes) o a itch.io.
5. El progreso (biomasa, mejoras, récord) se guarda en el navegador con `PlayerPrefs`.

---

## 7. Justificación académica (para el documento)

> ECO SHARK se fundamenta en el principio de la retórica procedimental (Bogost, 2007), según el cual los videojuegos comunican y persuaden a través de las reglas que el jugador ejecuta, y no solo mediante el contenido que muestran. El bucle central de juego traduce las metas de los Objetivos de Desarrollo Sostenible en mecánicas concretas: la barra de energía, que se drena de forma constante por la toxicidad del agua, modela la dependencia entre la salud de las especies marinas y la calidad de su ecosistema (ODS 14, meta 14.1: prevenir y reducir significativamente la contaminación marina), y la única vía de supervivencia consiste en retirar residuos plásticos del océano, de modo que la acción ambientalmente responsable deja de ser un mensaje externo y se convierte en la estrategia óptima del jugador. La conversión de los residuos recolectados en "Biomasa", recurso que permite mejorar las capacidades del personaje, introduce una lógica de economía circular en la que el desecho se revaloriza como recurso (ODS 12, meta 12.5: reducir la generación de desechos mediante actividades de prevención, reducción, reciclado y reutilización). A su vez, el barco de pesca ilegal que abandona redes con animales atrapados representa la pesca ilegal, no declarada y no reglamentada y el fenómeno de las redes fantasma (ODS 14, meta 14.4), mientras que la mecánica de rescate, que exige detenerse y perseverar para liberar a tortugas y delfines, sustituye la depredación propia del género por el cuidado de la vida marina. Finalmente, la retroalimentación inmediata, las mejoras progresivas y los datos breves que aparecen en el momento exacto de cada acción aplican los principios del aprendizaje experiencial (Kolb, 1984): el jugador no se limita a memorizar información sobre la contaminación oceánica, sino que la experimenta, toma decisiones en función de ella y observa sus consecuencias, lo que favorece una comprensión más significativa y duradera de la problemática.

**Precisión conceptual útil para la sustentación:** en sentido estricto, *gamificación* es "el uso de elementos de diseño de juegos en contextos no lúdicos" (Deterding et al., 2011). ECO SHARK es un videojuego completo con propósito educativo, es decir, un **juego serio** (*serious game*). Una formulación segura es: *"juego serio que aplica mecánicas de gamificación (puntos, economía, progresión y retroalimentación) para la educación en los ODS 12 y 14"*.

### Referencias
- Bogost, I. (2007). *Persuasive Games: The Expressive Power of Videogames*. MIT Press.
- Deterding, S., Dixon, D., Khaled, R., & Nacke, L. (2011). From game design elements to gamefulness: Defining "gamification". *Proceedings of the 15th International Academic MindTrek Conference*, 9–15.
- Kolb, D. A. (1984). *Experiential Learning: Experience as the Source of Learning and Development*. Prentice Hall.
- Naciones Unidas (2015). *Transformar nuestro mundo: la Agenda 2030 para el Desarrollo Sostenible* (A/RES/70/1).
- NOAA Marine Debris Program / Woods Hole Sea Grant. Infografía de tiempos estimados de degradación de residuos marinos (fuente de los datos de botella ~450 años y lata ~200 años usados en el juego). *Verifica y cita la versión que consultes: son estimaciones.*

---

## 8. Dónde ajustar el balance (sin programar)

| Qué | Dónde (Inspector) |
|---|---|
| Velocidad, aceleración, giro | `Tiburon_Guardian → SharkMovement` |
| Vida máxima, drenaje base | `Tiburon_Guardian → SharkHealth` |
| Radio del imán | `Tiburon_Guardian → SharkCollector` |
| Cuánto cura/paga cada basura, dato educativo | `Assets/EcoShark/Generated/Data/*.asset` |
| Cantidad y frecuencia de basura y peligros | `GameManager → OceanSpawner` |
| Qué tan rápido sube la dificultad | `GameManager → GameManager` |
| Tamaño del océano | `GameManager → OceanArea` |
| Precio y efecto de mejoras | `Scripts/Core/Progression.cs` (sección "Efecto de cada mejora") |

> ⚠️ Si vuelves a usar **"1. Crear escena MVP"**, se regeneran la escena, los prefabs y los datos. Hazlo solo al inicio. Después, edita directamente la escena y los prefabs.
