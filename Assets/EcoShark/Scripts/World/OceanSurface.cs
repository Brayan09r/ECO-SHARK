using UnityEngine;

namespace EcoShark
{
    /// <summary>
    /// Superficie del mar con olas animadas, estilo juego móvil 2.5D.
    /// Crea una franja vertical en el plano del juego (su borde de arriba es la línea del agua)
    /// y controla el shader "EcoShark/WaterSurface":
    ///  - olas que viajan continuamente (3 ondas sumadas);
    ///  - ondas extra alrededor del tiburón cuando nada cerca de la superficie (más fuertes si va rápido).
    /// HeightAt(x) da la altura de la ola en cualquier punto (lo usa FloatOnWaves para el barco).
    /// Va en el objeto "Entorno/Olas". Menú: ECO SHARK > 7. Cielo y olas.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class OceanSurface : MonoBehaviour
    {
        [Header("Franja")]
        [Tooltip("Mitad del ancho de la franja (unidades). Debe cubrir todo lo que ve la cámara.")]
        [SerializeField] float halfWidth = 100f;
        [Tooltip("Separación entre vértices: menor = olas más suaves.")]
        [SerializeField] float segment = 0.2f;
        [Tooltip("Alto de la franja bajo la línea del agua (banda de luz).")]
        [SerializeField] float depth = 2.2f;

        [Header("Olas")]
        [SerializeField] Vector3 amplitudes = new Vector3(0.18f, 0.1f, 0.05f);
        [Tooltip("Largo de cada onda (unidades).")]
        [SerializeField] Vector3 wavelengths = new Vector3(9f, 4.3f, 2.1f);
        [Tooltip("Velocidad de cada onda (radianes por segundo).")]
        [SerializeField] Vector3 speeds = new Vector3(1.1f, 1.7f, 2.6f);

        [Header("Ondas del tiburón")]
        [SerializeField] float rippleMax = 0.3f;
        [SerializeField] float rippleWidth = 2.5f;
        [SerializeField] float rippleWavelength = 2.1f;
        [Tooltip("A qué distancia bajo el límite del tiburón empiezan a notarse las ondas.")]
        [SerializeField] float rippleRange = 1.6f;

        public static OceanSurface Current { get; private set; }
        public float SurfaceY => transform.position.y;
        public float Depth => depth;

        // Copia de los valores que usa el shader (para HeightAt).
        static float waveTime;
        static Vector4 amp, freq, spd, ripple;

        static readonly int TimeId = Shader.PropertyToID("_EcoWaveTime");
        static readonly int AmpId = Shader.PropertyToID("_EcoWaveAmp");
        static readonly int FreqId = Shader.PropertyToID("_EcoWaveFreq");
        static readonly int SpeedId = Shader.PropertyToID("_EcoWaveSpeed");
        static readonly int RippleId = Shader.PropertyToID("_EcoRipple");
        static readonly int DepthId = Shader.PropertyToID("_Depth");

        Mesh mesh;
        MaterialPropertyBlock block;
        SharkMovement shark;
        float rippleStrength;
        float rippleX;

        void OnEnable()
        {
            Current = this;
            BuildMesh();
            UpdateWaves(0f);
        }

        void OnDisable()
        {
            if (Current == this) Current = null;
        }

        void OnValidate()
        {
            segment = Mathf.Max(0.05f, segment);
            depth = Mathf.Max(0.1f, depth);
        }

        void BuildMesh()
        {
            int columns = Mathf.Clamp(Mathf.CeilToInt(halfWidth * 2f / segment), 2, 20000);
            var vertices = new Vector3[(columns + 1) * 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[columns * 6];

            for (int c = 0; c <= columns; c++)
            {
                float x = -halfWidth + c * (halfWidth * 2f / columns);
                vertices[c * 2] = new Vector3(x, -depth, 0f);   // abajo
                vertices[c * 2 + 1] = new Vector3(x, 0f, 0f);   // arriba (la ola)
                uvs[c * 2] = new Vector2(x, 0f);
                uvs[c * 2 + 1] = new Vector2(x, 1f);
            }
            for (int c = 0; c < columns; c++)
            {
                int i = c * 6, v = c * 2;
                triangles[i] = v; triangles[i + 1] = v + 1; triangles[i + 2] = v + 3;
                triangles[i + 3] = v; triangles[i + 4] = v + 3; triangles[i + 5] = v + 2;
            }

            if (mesh == null)
            {
                mesh = new Mesh { name = "Olas (generado)", hideFlags = HideFlags.DontSave };
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            // Límites generosos: las olas suben por el shader y Unity no debe recortar la franja.
            mesh.bounds = new Bounds(new Vector3(0f, -depth * 0.5f, 0f), new Vector3(halfWidth * 2f, depth + 4f, 1f));
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        void Update()
        {
            UpdateWaves(Application.isPlaying ? Time.deltaTime : 0f);
        }

        void UpdateWaves(float dt)
        {
            waveTime = Application.isPlaying ? Time.time : 0f;
            amp = new Vector4(amplitudes.x, amplitudes.y, amplitudes.z, 0f);
            freq = new Vector4(K(wavelengths.x), K(wavelengths.y), K(wavelengths.z), 0f);
            spd = new Vector4(speeds.x, speeds.y, speeds.z, 0f);

            // Ondas alrededor del tiburón cuando está cerca de la superficie.
            float target = 0f;
            if (Application.isPlaying)
            {
                if (shark == null) shark = FindFirstObjectByType<SharkMovement>();
                if (shark != null)
                {
                    Vector3 p = shark.transform.position;
                    float gap = SurfaceY - p.y;                                   // en el límite del tiburón ≈ 0.9
                    float near = 1f - Mathf.Clamp01((gap - 0.9f) / Mathf.Max(0.01f, rippleRange));
                    float speed01 = shark.MaxSpeed > 0.01f ? Mathf.Clamp01(shark.Velocity.magnitude / shark.MaxSpeed) : 0f;
                    target = rippleMax * near * (0.35f + 0.65f * speed01);
                    rippleX = Mathf.Lerp(rippleX, p.x, 1f - Mathf.Exp(-6f * dt));
                }
            }
            rippleStrength = Mathf.Lerp(rippleStrength, target, 1f - Mathf.Exp(-3f * dt));
            ripple = new Vector4(rippleX, rippleStrength, rippleWidth, K(rippleWavelength));

            Shader.SetGlobalFloat(TimeId, waveTime);
            Shader.SetGlobalVector(AmpId, amp);
            Shader.SetGlobalVector(FreqId, freq);
            Shader.SetGlobalVector(SpeedId, spd);
            Shader.SetGlobalVector(RippleId, ripple);

            if (block == null) block = new MaterialPropertyBlock();
            Renderer r = GetComponent<Renderer>();
            r.GetPropertyBlock(block);
            block.SetFloat(DepthId, depth);
            r.SetPropertyBlock(block);
        }

        static float K(float wavelength) => Mathf.PI * 2f / Mathf.Max(0.01f, wavelength);

        /// <summary>Altura de la ola (respecto a la superficie) en la x del mundo. Igual que el shader.</summary>
        public static float HeightAt(float x)
        {
            float t = waveTime;
            float h = amp.x * Mathf.Sin(x * freq.x - t * spd.x)
                    + amp.y * Mathf.Sin(x * freq.y + t * spd.y + 1.3f)
                    + amp.z * Mathf.Sin(x * freq.z - t * spd.z + 2.1f);
            float d = Mathf.Abs(x - ripple.x);
            float w = Mathf.Max(ripple.z, 0.0001f);
            h += ripple.y * Mathf.Exp(-(d * d) / (w * w)) * Mathf.Sin(d * ripple.w - t * 6f);
            return h;
        }
    }
}
