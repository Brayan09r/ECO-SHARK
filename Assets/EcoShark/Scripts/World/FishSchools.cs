using UnityEngine;

namespace EcoShark
{
    /// <summary>
    /// (Claude) Vida marina: cardúmenes de peces pequeños que nadan por todo el océano y se
    /// dispersan cuando el tiburón se acerca; después se vuelven a juntar.
    ///  - Son solo decoración: nadan detrás del plano de juego (z > 0), sin colliders ni física.
    ///  - Todos los peces van en UNA sola malla que se actualiza cada cuadro (1 draw call, sin GameObjects
    ///    por pez). Funciona igual en WebGL y móvil.
    ///  - Shader "EcoShark/Fish" (Resources/Shaders): color por vértice + niebla submarina.
    /// Objeto "VidaMarina". Menú del editor: ECO SHARK > 11.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class FishSchools : MonoBehaviour
    {
        [Header("Cantidad")]
        [SerializeField] int schoolCount = 18;
        [SerializeField] int fishPerSchool = 20;

        [Header("Cardumen")]
        [SerializeField] Vector2 schoolRadius = new Vector2(2.6f, 1.3f);
        [SerializeField] Vector2 schoolSpeed = new Vector2(0.8f, 1.7f);
        [Tooltip("Profundidad (Z) de los cardúmenes: detrás del plano de juego.")]
        [SerializeField] Vector2 depthRange = new Vector2(0.9f, 2.6f);
        [Tooltip("Distancia a la superficie y al fondo que los cardúmenes respetan.")]
        [SerializeField] float topMargin = 3f, bottomMargin = 2.5f;

        [Header("Peces")]
        [SerializeField] Vector2 fishSize = new Vector2(0.45f, 0.75f);
        [SerializeField] float maxFishSpeed = 7f;

        [Header("Reacción al tiburón")]
        [SerializeField] float fleeRadius = 4.5f;
        [SerializeField] float fleeSpeed = 9f;

        static readonly Color[] Palette =
        {
            new Color(1f, 0.6f, 0.15f),    // naranja
            new Color(1f, 0.85f, 0.25f),   // amarillo
            new Color(0.55f, 0.8f, 0.95f), // plateado azul
            new Color(1f, 0.45f, 0.55f),   // rosa coral
            new Color(0.45f, 0.9f, 0.7f),  // verde agua
        };

        const int VertsPerFish = 12;
        const int IndicesPerFish = 18;

        struct School
        {
            public Vector2 center;
            public float heading, speed, z, noise;
        }

        struct Fish
        {
            public Vector2 pos, vel, dir, offset;
            public float size, phase;
        }

        School[] schools;
        Fish[] fish;
        Vector3[] verts;
        Mesh mesh;
        Transform shark;
        SharkMovement sharkMovement;

        void Awake()
        {
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            transform.localScale = Vector3.one;
        }

        void Start()
        {
            sharkMovement = FindFirstObjectByType<SharkMovement>(FindObjectsInactive.Include);
            if (sharkMovement != null) shark = sharkMovement.transform;
            Build();
        }

        void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
        }

        void GetLimits(out Vector2 min, out Vector2 max)
        {
            OceanArea area = OceanArea.Current;
            min = area != null ? area.Min : new Vector2(-40f, -22f);
            max = area != null ? area.Max : new Vector2(40f, 6f);
            min += new Vector2(3f, bottomMargin);
            max -= new Vector2(3f, topMargin);
        }

        // ---------------------------------------------------------------- Construcción

        void Build()
        {
            schoolCount = Mathf.Clamp(schoolCount, 1, 64);
            fishPerSchool = Mathf.Clamp(fishPerSchool, 1, 80);
            int total = schoolCount * fishPerSchool; // máx. 5120 peces = 61440 vértices (< 65535)

            GetLimits(out Vector2 min, out Vector2 max);
            schools = new School[schoolCount];
            fish = new Fish[total];
            verts = new Vector3[total * VertsPerFish];
            var colors = new Color32[total * VertsPerFish];
            var tris = new int[total * IndicesPerFish];

            for (int s = 0; s < schoolCount; s++)
            {
                // Repartidos a lo ancho del mar, uno por franja.
                float x = Mathf.Lerp(min.x, max.x, (s + Random.value) / schoolCount);
                School sc = new School
                {
                    center = new Vector2(x, Random.Range(min.y, max.y)),
                    heading = Random.value < 0.5f ? 0f : Mathf.PI,
                    speed = Random.Range(schoolSpeed.x, schoolSpeed.y),
                    z = Random.Range(depthRange.x, depthRange.y),
                    noise = Random.value * 100f
                };
                schools[s] = sc;

                Color baseColor = Palette[s % Palette.Length];
                float sizeScale = Random.Range(0.85f, 1.2f);
                for (int k = 0; k < fishPerSchool; k++)
                {
                    int i = s * fishPerSchool + k;
                    Vector2 o = Random.insideUnitCircle;
                    Fish f = new Fish
                    {
                        offset = new Vector2(o.x * schoolRadius.x, o.y * schoolRadius.y),
                        size = Random.Range(fishSize.x, fishSize.y) * sizeScale,
                        phase = Random.value * 6.283f,
                        dir = new Vector2(Mathf.Cos(sc.heading), 0f)
                    };
                    f.pos = sc.center + f.offset;
                    fish[i] = f;

                    // Colores: lomo oscuro, vientre claro (contrasombreado, como los peces de verdad).
                    Color tint = Color.Lerp(baseColor, Color.white, Random.Range(0f, 0.15f));
                    Color32 back = tint * 0.78f, belly = Color.Lerp(tint, Color.white, 0.55f), mid = tint, fin = tint * 0.62f;
                    back.a = belly.a = mid.a = fin.a = 255;
                    int v = i * VertsPerFish;
                    colors[v] = mid;       // nariz
                    colors[v + 1] = back;  // lomo adelante
                    colors[v + 2] = back;  // lomo atrás
                    colors[v + 3] = mid;   // unión con la cola
                    colors[v + 4] = belly; // vientre atrás
                    colors[v + 5] = belly; // vientre adelante
                    colors[v + 6] = mid;   // cola
                    colors[v + 7] = fin;
                    colors[v + 8] = fin;
                    colors[v + 9] = fin;   // aleta dorsal
                    colors[v + 10] = fin;
                    colors[v + 11] = fin;

                    int t = i * IndicesPerFish;
                    tris[t] = v; tris[t + 1] = v + 1; tris[t + 2] = v + 2;
                    tris[t + 3] = v; tris[t + 4] = v + 2; tris[t + 5] = v + 3;
                    tris[t + 6] = v; tris[t + 7] = v + 3; tris[t + 8] = v + 4;
                    tris[t + 9] = v; tris[t + 10] = v + 4; tris[t + 11] = v + 5;
                    tris[t + 12] = v + 6; tris[t + 13] = v + 7; tris[t + 14] = v + 8;
                    tris[t + 15] = v + 9; tris[t + 16] = v + 10; tris[t + 17] = v + 11;
                }
            }

            mesh = new Mesh { name = "Cardumenes", hideFlags = HideFlags.DontSave };
            mesh.MarkDynamic();
            WriteVertices(0f);
            mesh.vertices = verts;
            mesh.colors32 = colors;
            mesh.triangles = tris;
            // Límites fijos (todo el mar): no hace falta recalcularlos cada cuadro.
            OceanArea area = OceanArea.Current;
            Vector2 a = area != null ? area.Min : new Vector2(-40f, -22f), b = area != null ? area.Max : new Vector2(40f, 6f);
            mesh.bounds = new Bounds(new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, 2f), new Vector3(b.x - a.x + 20f, b.y - a.y + 20f, 12f));
            GetComponent<MeshFilter>().sharedMesh = mesh;

            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr.sharedMaterial == null)
            {
                Shader shader = Resources.Load<Shader>("Shaders/EcoShark_Fish");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                mr.sharedMaterial = new Material(shader) { name = "Peces", hideFlags = HideFlags.DontSave };
            }
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        /// <summary>Centro del cardumen más cercano a un punto (lo usa el tráiler). (Claude)</summary>
        public bool TryGetNearestSchool(Vector2 from, out Vector2 center)
        {
            center = from;
            if (schools == null || schools.Length == 0) return false;
            float best = float.MaxValue;
            for (int i = 0; i < schools.Length; i++)
            {
                float d = (schools[i].center - from).sqrMagnitude;
                if (d < best) { best = d; center = schools[i].center; }
            }
            return true;
        }

        // ---------------------------------------------------------------- Simulación

        void Update()
        {
            if (mesh == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (dt <= 0f) return; // en pausa
            float time = Time.time;

            GetLimits(out Vector2 min, out Vector2 max);
            bool hasShark = shark != null;
            Vector2 sharkPos = hasShark ? (Vector2)shark.position : Vector2.zero;
            float sharkSpeed01 = 0f;
            if (sharkMovement != null && sharkMovement.isActiveAndEnabled && sharkMovement.MaxSpeed > 0.01f)
                sharkSpeed01 = Mathf.Clamp01(sharkMovement.Velocity.magnitude / sharkMovement.MaxSpeed);
            float fleeSq = fleeRadius * fleeRadius;
            float follow = 1f - Mathf.Exp(-4f * dt);
            float turn = 1f - Mathf.Exp(-8f * dt);

            for (int s = 0; s < schools.Length; s++)
            {
                School sc = schools[s];

                // El cardumen deambula: cambia de rumbo poco a poco y da la vuelta en los bordes.
                float wander = (Mathf.PerlinNoise(sc.noise, time * 0.07f) - 0.5f) * 1.6f;
                sc.heading += wander * dt;
                Vector2 heading = new Vector2(Mathf.Cos(sc.heading), Mathf.Sin(sc.heading) * 0.45f);
                Vector2 vel = heading * sc.speed;

                // Se aparta del tiburón (todo el grupo, más despacio que cada pez).
                if (hasShark)
                {
                    Vector2 away = sc.center - sharkPos;
                    float d = away.magnitude;
                    if (d < fleeRadius * 1.6f && d > 0.001f) vel += away / d * (2.5f * (1f - d / (fleeRadius * 1.6f)));
                }
                sc.center += vel * dt;
                if (sc.center.x < min.x) { sc.center.x = min.x; sc.heading = Mathf.PI - sc.heading; }
                else if (sc.center.x > max.x) { sc.center.x = max.x; sc.heading = Mathf.PI - sc.heading; }
                if (sc.center.y < min.y) { sc.center.y = min.y; sc.heading = -sc.heading; }
                else if (sc.center.y > max.y) { sc.center.y = max.y; sc.heading = -sc.heading; }
                schools[s] = sc;

                int first = s * fishPerSchool;
                for (int k = 0; k < fishPerSchool; k++)
                {
                    int i = first + k;
                    Fish f = fish[i];

                    // Cada pez persigue su sitio dentro del grupo (que se mece un poco).
                    Vector2 slot = sc.center + f.offset + new Vector2(Mathf.Sin(time * 0.6f + f.phase) * 0.35f, Mathf.Cos(time * 0.8f + f.phase) * 0.25f);
                    Vector2 desired = (slot - f.pos) * 2.2f + vel;

                    if (hasShark)
                    {
                        Vector2 away = f.pos - sharkPos;
                        float sq = away.sqrMagnitude;
                        if (sq < fleeSq && sq > 0.0001f)
                        {
                            float d = Mathf.Sqrt(sq);
                            desired += away / d * (fleeSpeed * (1f - d / fleeRadius) * (0.6f + sharkSpeed01));
                        }
                    }

                    f.vel = Vector2.ClampMagnitude(Vector2.Lerp(f.vel, desired, follow), maxFishSpeed);
                    f.pos += f.vel * dt;
                    if (f.pos.y > max.y + 1.5f) f.pos.y = max.y + 1.5f; // nunca fuera del agua

                    float speed = f.vel.magnitude;
                    if (speed > 0.15f) f.dir = Vector2.Lerp(f.dir, f.vel / speed, turn).normalized;
                    if (f.dir.sqrMagnitude < 0.01f) f.dir = Vector2.right;
                    f.phase += dt * (5f + speed * 3.5f); // coletea más rápido cuanto más corre
                    fish[i] = f;
                }
            }

            WriteVertices(time);
            mesh.vertices = verts;
        }

        /// <summary>Escribe los 12 vértices de cada pez según su posición, dirección y coletazo.</summary>
        void WriteVertices(float time)
        {
            for (int s = 0; s < schools.Length; s++)
            {
                float z = schools[s].z;
                int first = s * fishPerSchool;
                for (int k = 0; k < fishPerSchool; k++)
                {
                    int i = first + k;
                    Fish f = fish[i];
                    Vector2 fw = f.dir * f.size;
                    // "Arriba" del pez: perpendicular a la dirección, siempre con el lomo hacia arriba.
                    Vector2 up = new Vector2(-f.dir.y, f.dir.x) * f.size;
                    if (f.dir.x < 0f) up = -up;
                    float wag = Mathf.Sin(f.phase) * 0.12f;
                    float zz = z + (i % 7) * 0.05f; // un poco de profundidad dentro del grupo

                    int v = i * VertsPerFish;
                    verts[v] = P(f.pos, fw, up, 0.5f, 0f, zz);
                    verts[v + 1] = P(f.pos, fw, up, 0.2f, 0.2f, zz);
                    verts[v + 2] = P(f.pos, fw, up, -0.15f, 0.17f, zz);
                    verts[v + 3] = P(f.pos, fw, up, -0.42f, wag * 0.3f, zz);
                    verts[v + 4] = P(f.pos, fw, up, -0.15f, -0.17f, zz);
                    verts[v + 5] = P(f.pos, fw, up, 0.2f, -0.2f, zz);
                    // Cola (se mueve con el coletazo).
                    verts[v + 6] = P(f.pos, fw, up, -0.36f, wag * 0.3f, zz);
                    verts[v + 7] = P(f.pos, fw, up, -0.74f + Mathf.Abs(wag) * 0.3f, 0.25f + wag, zz);
                    verts[v + 8] = P(f.pos, fw, up, -0.74f + Mathf.Abs(wag) * 0.3f, -0.25f + wag, zz);
                    // Aleta dorsal.
                    verts[v + 9] = P(f.pos, fw, up, 0.12f, 0.19f, zz);
                    verts[v + 10] = P(f.pos, fw, up, -0.14f, 0.17f, zz);
                    verts[v + 11] = P(f.pos, fw, up, -0.1f, 0.37f, zz);
                }
            }
        }

        static Vector3 P(Vector2 pos, Vector2 fw, Vector2 up, float x, float y, float z)
        {
            return new Vector3(pos.x + fw.x * x + up.x * y, pos.y + fw.y * x + up.y * y, z);
        }
    }
}
