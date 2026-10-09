using UnityEngine;

// (Claude) Aleteo lateral: ver EcoShark_CartoonSwim.shader (SwimDeform).
namespace EcoShark
{
    /// <summary>
    /// Nado estilo juego móvil (sin huesos). Controla al shader "EcoShark/CartoonSwim":
    ///  1. Coletazo continuo DE LADO A LADO: una onda recorre el cuerpo de la nariz a la cola
    ///     y cada parte del cuerpo gira siguiendo la curva (la aleta se ve de canto o de frente).
    ///     Quieto = lento y suave; a toda velocidad = rápido y amplio.
    ///  2. Impulso (Shift): coletazos más fuertes y rápidos.
    ///  3. Curva al girar: cuando el tiburón cambia de dirección, la cola se queda atrás
    ///     y el cuerpo entero se dobla hacia la curva (lo que da la sensación de "vivo").
    /// Va en el objeto del modelo 3D (hijo de Tiburon_Guardian). Funciona con cualquier modelo:
    /// calcula solo sus ejes (nariz, lomo, lateral) y su largo.
    /// </summary>
    [ExecuteAlways]
    public class SharkSwimWave : MonoBehaviour
    {
        [Header("Coletazo")]
        [Tooltip("Coletazos por segundo cuando está quieto.")]
        [SerializeField] float idleBeatsPerSecond = 0.2f;
        [Tooltip("Coletazos por segundo a velocidad máxima.")]
        [SerializeField] float maxBeatsPerSecond = 0.8f;
        [Tooltip("Cuánto se mueve la punta de la cola hacia los lados a toda velocidad (unidades del mundo).")]
        [SerializeField] float amplitude = 0.5f;
        [Tooltip("Fracción de la amplitud cuando está quieto.")]
        [Range(0f, 1f)]
        [SerializeField] float idleAmplitude = 0.5f;
        [Tooltip("Largo de la onda que recorre el cuerpo (unidades del mundo). Menor = más 'serpenteo'.")]
        [SerializeField] float waveLength = 2.6f;
        [Tooltip("Sube y baja extra de la cola. 0 = aleteo solo de lado a lado, como un tiburón real. El shader ya gira la cola, así que se nota desde la cámara de costado.")]
        [Range(0f, 1f)]
        [SerializeField] float verticalMix = 0f;

        [Header("Impulso (Shift)")]
        [SerializeField] float boostAmplitudeMultiplier = 1.5f;
        [SerializeField] float boostBeatsMultiplier = 1.375f;

        [Header("Curva al girar")]
        [Tooltip("Cuánto se dobla la cola por cada grado/segundo de giro.")]
        [SerializeField] float bendPerDegreePerSecond = 0.0025f;
        [Tooltip("Máxima curva de la cola (unidades del mundo).")]
        [SerializeField] float maxBend = 0.6f;
        [Tooltip("Qué tan rápido responde la curva.")]
        [SerializeField] float bendSmoothing = 5f;
        [Tooltip("Velocidad máxima a la que puede cambiar la curva (unidades/segundo). Evita tirones.")]
        [SerializeField] float maxBendChangePerSecond = 2.5f;
        [Tooltip("Giros más rápidos que esto (grados/segundo) se ignoran: son volteretas o choques, no curvas.")]
        [SerializeField] float maxTurnRate = 400f;

        [Header("Editor")]
        [Tooltip("En el editor (sin Play), mostrar el cuerpo recto.")]
        [SerializeField] bool straightInEditor = true;

        static readonly int SwimAmpId = Shader.PropertyToID("_SwimAmp");
        static readonly int SwimPhaseId = Shader.PropertyToID("_SwimPhase");
        static readonly int WaveNumberId = Shader.PropertyToID("_WaveNumber");
        static readonly int HeadPosId = Shader.PropertyToID("_HeadPos");
        static readonly int BodyLengthId = Shader.PropertyToID("_BodyLength");
        static readonly int ForwardId = Shader.PropertyToID("_SwimForward");
        static readonly int SideId = Shader.PropertyToID("_SwimSide");
        static readonly int UpId = Shader.PropertyToID("_SwimUp");
        static readonly int VerticalId = Shader.PropertyToID("_SwimVertical");
        static readonly int BendDirId = Shader.PropertyToID("_BendDir");

        struct Target
        {
            public Renderer renderer;
            public Vector3 forward;   // nariz, en el espacio del objeto
            public Vector3 side;      // lateral, en el espacio del objeto
            public Vector3 up;        // lomo, en el espacio del objeto
            public float headPos;
            public float length;
            public float scale;       // para convertir unidades del mundo a unidades del objeto
        }

        /// <summary>0–1: fuerza el ritmo del coletazo (el menú lo usa para que el tiburón "nade" en su sitio). Negativo = automático. (Claude)</summary>
        public float ForcedSpeed01 { get; set; } = -1f;

        Target[] targets = new Target[0];
        MaterialPropertyBlock block;
        SharkMovement movement;
        Transform root;
        float phase;
        Vector3 bendWorld;   // hacia dónde se va la cola en la curva (mundo)
        Vector3 lastForward;

        void OnEnable()
        {
            block = new MaterialPropertyBlock();
            movement = GetComponentInParent<SharkMovement>();
            root = movement != null ? movement.transform : (transform.parent != null ? transform.parent : transform);
            lastForward = Vector3.ProjectOnPlane(root.forward, Vector3.forward).normalized;
            Setup();
            Apply(0f, Vector3.zero);
        }

        /// <summary>Recalcula ejes y largo del modelo (llámalo si cambias el modelo).</summary>
        public void Setup()
        {
            Transform model = transform;
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            targets = new Target[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                Transform t = r.transform;
                Vector3 fwd = t.InverseTransformDirection(model.forward).normalized;
                Vector3 side = t.InverseTransformDirection(model.right).normalized;
                Vector3 up = t.InverseTransformDirection(model.up).normalized;

                Bounds local = LocalBounds(r);
                float min = float.PositiveInfinity, max = float.NegativeInfinity;
                Vector3 c = local.center, e = local.extents;
                for (int k = 0; k < 8; k++)
                {
                    Vector3 corner = c + new Vector3((k & 1) == 0 ? -e.x : e.x, (k & 2) == 0 ? -e.y : e.y, (k & 4) == 0 ? -e.z : e.z);
                    float d = Vector3.Dot(corner, fwd);
                    min = Mathf.Min(min, d);
                    max = Mathf.Max(max, d);
                }

                Vector3 ls = t.lossyScale;
                targets[i] = new Target
                {
                    renderer = r,
                    forward = fwd,
                    side = side,
                    up = up,
                    headPos = max,
                    length = Mathf.Max(0.0001f, max - min),
                    scale = Mathf.Max(0.0001f, (Mathf.Abs(ls.x) + Mathf.Abs(ls.y) + Mathf.Abs(ls.z)) / 3f)
                };
            }
        }

        static Bounds LocalBounds(Renderer r)
        {
            if (r is SkinnedMeshRenderer smr) return smr.localBounds;
            MeshFilter mf = r.GetComponent<MeshFilter>();
            return mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one);
        }

        void Update()
        {
            if (!Application.isPlaying)
            {
                if (straightInEditor) Apply(0f, Vector3.zero);
                return;
            }

            float dt = Mathf.Max(Time.deltaTime, 0.0001f);

            // --- Velocidad e impulso ---
            float speed01 = 0.3f;
            bool boosting = false;
            if (ForcedSpeed01 >= 0f)
            {
                speed01 = Mathf.Clamp01(ForcedSpeed01);
            }
            else if (movement != null && movement.MaxSpeed > 0.01f)
            {
                speed01 = Mathf.Clamp01(movement.Velocity.magnitude / movement.MaxSpeed);
                boosting = movement.IsBoosting;
            }

            float beats = Mathf.Lerp(idleBeatsPerSecond, maxBeatsPerSecond, speed01);
            float amp = amplitude * Mathf.Lerp(idleAmplitude, 1f, speed01);
            if (boosting)
            {
                beats *= boostBeatsMultiplier;
                amp *= boostAmplitudeMultiplier;
            }

            phase += beats * Mathf.PI * 2f * dt;
            if (phase > 1000f) phase -= Mathf.PI * 2f * 100f; // evita perder precisión con el tiempo

            // --- Curva al girar ---
            // Se mide el giro EN EL PLANO DE LA PANTALLA (eje Z del mundo). Antes se medía alrededor del eje
            // lateral del tiburón, que se invierte al voltearse (izquierda/derecha): al nadar hacia la superficie
            // la curva cambiaba de signo de golpe y la cola daba tirones.
            Vector3 forward = Vector3.ProjectOnPlane(root.forward, Vector3.forward);
            Vector3 targetBend = Vector3.zero;
            if (forward.sqrMagnitude > 0.0001f && lastForward.sqrMagnitude > 0.0001f)
            {
                forward.Normalize();
                float degreesPerSecond = Vector3.SignedAngle(lastForward, forward, Vector3.forward) / dt;
                if (Mathf.Abs(degreesPerSecond) <= maxTurnRate)
                {
                    // La cola se queda atrás: se desplaza al lado contrario del giro, perpendicular a la nariz.
                    Vector3 perpendicular = Vector3.Cross(Vector3.forward, forward);
                    float amount = Mathf.Clamp(degreesPerSecond * bendPerDegreePerSecond, -maxBend, maxBend);
                    targetBend = -perpendicular * amount;
                }
            }
            lastForward = forward;

            Vector3 smoothed = Vector3.Lerp(bendWorld, targetBend, 1f - Mathf.Exp(-bendSmoothing * dt));
            bendWorld = Vector3.MoveTowards(bendWorld, smoothed, maxBendChangePerSecond * dt);

            Apply(amp, bendWorld);
        }

        void Apply(float worldAmplitude, Vector3 worldBend)
        {
            if (block == null) block = new MaterialPropertyBlock();
            foreach (Target t in targets)
            {
                if (t.renderer == null) continue;
                t.renderer.GetPropertyBlock(block);
                block.SetFloat(SwimAmpId, worldAmplitude / t.scale);
                block.SetFloat(SwimPhaseId, phase);
                block.SetFloat(WaveNumberId, Mathf.PI * 2f / (waveLength / t.scale));
                block.SetFloat(HeadPosId, t.headPos);
                block.SetFloat(BodyLengthId, t.length);
                block.SetVector(ForwardId, t.forward);
                block.SetVector(SideId, t.side);
                block.SetVector(UpId, t.up);
                block.SetFloat(VerticalId, verticalMix);
                Vector3 bendLocal = t.renderer.transform.InverseTransformDirection(worldBend) / t.scale;
                block.SetVector(BendDirId, bendLocal);
                t.renderer.SetPropertyBlock(block);
            }
        }
    }
}
