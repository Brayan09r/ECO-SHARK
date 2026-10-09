using System.Collections.Generic;
using UnityEngine;

namespace EcoShark
{
    /// <summary>
    /// Sonido del juego: música de fondo, ambiente submarino y efectos del tiburón.
    /// Los audios se cargan SOLOS por nombre desde "Assets/EcoShark/Resources/Audio/" (cualquier formato: .ogg, .mp3, .wav).
    /// Si falta un archivo, ese sonido simplemente no suena (el juego no se rompe) y se avisa una vez en la consola.
    ///
    ///   Audio/Musica/musica_fondo           música en bucle
    ///   Audio/Ambiente/ambiente_submarino   ambiente en bucle (agua, burbujas lejanas)
    ///   Audio/SFX/nado_loop                 agua al nadar en bucle; el volumen sube con la velocidad
    ///   Audio/SFX/impulso                   al empezar el impulso (Shift)
    ///   Audio/SFX/recoger_basura            al recoger basura
    ///   Audio/SFX/rescate_animal            al liberar un animal
    ///   Audio/SFX/animal_perdido            cuando un animal se pierde
    ///   Audio/SFX/golpe                     al recibir daño
    ///   Audio/SFX/fin_partida               al morir
    ///   Audio/SFX/burbujas                  burbujas sueltas de vez en cuando
    ///   Audio/SFX/chapoteo                  al llegar rápido a la superficie
    ///
    /// Tecla M: silenciar / activar el sonido.
    /// </summary>
    public class EcoAudio : MonoBehaviour
    {
        [Header("Volúmenes")]
        [Range(0f, 1f)] [SerializeField] float musicVolume = 0.45f;
        [Range(0f, 1f)] [SerializeField] float ambienceVolume = 0.5f;
        [Range(0f, 1f)] [SerializeField] float effectsVolume = 0.8f;
        [Range(0f, 1f)] [SerializeField] float swimVolume = 0.35f;

        [Header("Burbujas sueltas (segundos entre una y otra)")]
        [SerializeField] Vector2 bubbleInterval = new Vector2(6f, 14f);

        [Header("Chapoteo en la superficie")]
        [Tooltip("Distancia a la superficie para que suene el chapoteo.")]
        [SerializeField] float splashDistance = 1.3f;
        [SerializeField] float splashCooldown = 2.5f;

        AudioSource music, ambience, swim, effects;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly List<string> missing = new List<string>();

        SharkMovement shark;
        SharkHealth health;
        bool wasBoosting;
        bool wasNearSurface;
        float nextBubble;
        float nextSplash;
        bool muted;

        void Awake()
        {
            music = NewSource("Musica", true);
            ambience = NewSource("Ambiente", true);
            swim = NewSource("Nado", true);
            effects = NewSource("Efectos", false);

            Play(music, "Musica/musica_fondo", musicVolume);
            Play(ambience, "Ambiente/ambiente_submarino", ambienceVolume);
            Play(swim, "SFX/nado_loop", 0f);

            // Cargar ya los efectos para que no haya pausa la primera vez.
            foreach (string n in new[] { "impulso", "recoger_basura", "rescate_animal", "animal_perdido", "golpe", "fin_partida", "burbujas", "chapoteo" })
                Clip("SFX/" + n);

            if (missing.Count > 0)
                Debug.Log("[ECO SHARK] Audio: aún faltan estos archivos en Assets/EcoShark/Resources/Audio/: " + string.Join(", ", missing));

            nextBubble = Time.time + Random.Range(bubbleInterval.x, bubbleInterval.y);
        }

        void OnEnable()
        {
            GameEvents.TrashCollected += OnTrash;
            GameEvents.AnimalRescued += OnRescued;
            GameEvents.AnimalLost += OnLost;
            GameEvents.SharkDied += OnDied;
        }

        void OnDisable()
        {
            GameEvents.TrashCollected -= OnTrash;
            GameEvents.AnimalRescued -= OnRescued;
            GameEvents.AnimalLost -= OnLost;
            GameEvents.SharkDied -= OnDied;
            if (health != null) health.Damaged -= OnDamaged;
        }

        void Start()
        {
            shark = FindFirstObjectByType<SharkMovement>();
            health = FindFirstObjectByType<SharkHealth>();
            if (health != null) health.Damaged += OnDamaged;
        }

        void Update()
        {
            if (EcoInput.MutePressed())
            {
                muted = !muted;
                AudioListener.volume = muted ? 0f : 1f;
            }

            // Nado: el agua suena más fuerte cuanto más rápido va el tiburón.
            float speed01 = 0f;
            if (shark != null && shark.MaxSpeed > 0.01f)
                speed01 = Mathf.Clamp01(shark.Velocity.magnitude / shark.MaxSpeed);
            if (swim.clip != null)
            {
                float target = swimVolume * speed01 * (shark != null && shark.IsBoosting ? 1.3f : 1f);
                swim.volume = Mathf.MoveTowards(swim.volume, target, Time.deltaTime * 1.5f);
                swim.pitch = Mathf.Lerp(0.9f, 1.15f, speed01);
            }

            // Impulso.
            bool boosting = shark != null && shark.IsBoosting;
            if (boosting && !wasBoosting) OneShot("SFX/impulso", 0.8f, 0.95f, 1.05f);
            wasBoosting = boosting;

            // Chapoteo al llegar rápido a la superficie.
            OceanArea area = OceanArea.Current;
            if (shark != null && area != null)
            {
                bool near = area.SurfaceY - shark.transform.position.y < splashDistance;
                if (near && !wasNearSurface && speed01 > 0.5f && Time.time >= nextSplash)
                {
                    OneShot("SFX/chapoteo", 0.7f, 0.9f, 1.1f);
                    nextSplash = Time.time + splashCooldown;
                }
                wasNearSurface = near;
            }

            // Burbujas de ambiente.
            if (Time.time >= nextBubble)
            {
                OneShot("SFX/burbujas", 0.35f, 0.85f, 1.2f);
                nextBubble = Time.time + Random.Range(bubbleInterval.x, bubbleInterval.y);
            }
        }

        void OnTrash(TrashData data) => OneShot("SFX/recoger_basura", 0.9f, 0.92f, 1.12f);
        void OnRescued(TrappedAnimal animal) => OneShot("SFX/rescate_animal", 1f, 1f, 1f);
        void OnLost(TrappedAnimal animal) => OneShot("SFX/animal_perdido", 0.8f, 1f, 1f);
        void OnDamaged(float amount) => OneShot("SFX/golpe", 0.9f, 0.9f, 1.05f);

        void OnDied()
        {
            OneShot("SFX/fin_partida", 1f, 1f, 1f);
            if (music.clip != null) music.volume = musicVolume * 0.4f;
            swim.volume = 0f;
        }

        // ---------- utilidades ----------

        AudioSource NewSource(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            AudioSource s = go.AddComponent<AudioSource>();
            s.loop = loop;
            s.playOnAwake = false;
            s.spatialBlend = 0f; // 2D: se escucha igual en toda la pantalla
            return s;
        }

        void Play(AudioSource source, string path, float volume)
        {
            AudioClip c = Clip(path);
            if (c == null) return;
            source.clip = c;
            source.volume = volume;
            source.Play();
        }

        void OneShot(string path, float volume, float minPitch, float maxPitch)
        {
            AudioClip c = Clip(path);
            if (c == null) return;
            effects.pitch = Random.Range(minPitch, maxPitch);
            effects.PlayOneShot(c, volume * effectsVolume);
        }

        AudioClip Clip(string path)
        {
            if (clips.TryGetValue(path, out AudioClip c)) return c;
            c = Resources.Load<AudioClip>("Audio/" + path);
            clips[path] = c;
            if (c == null) missing.Add(path);
            return c;
        }
    }
}
