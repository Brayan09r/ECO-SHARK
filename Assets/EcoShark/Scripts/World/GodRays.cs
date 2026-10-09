using UnityEngine;

namespace EcoShark
{
    /// <summary>
    /// Rayos de sol que bajan desde la superficie y se mecen como si el agua se moviera.
    /// Cada hijo es un "pivote" en la superficie con un rayo colgando (material EcoShark/GodRay, aditivo).
    /// Con Mathf.Sin cada rayo: se balancea unos grados, cambia de intensidad y "respira" de ancho, cada uno a su ritmo.
    /// Va en "FondoMarinoVivo/RayosDeSol".
    /// </summary>
    public class GodRays : MonoBehaviour
    {
        [SerializeField] float intensity = 0.22f;
        [Tooltip("Grados que se balancea cada rayo.")]
        [SerializeField] float swayDegrees = 4f;
        [SerializeField] float swaySpeed = 0.35f;
        [Tooltip("Cuánto varía el brillo (0 = fijo).")]
        [Range(0f, 1f)]
        [SerializeField] float pulse = 0.45f;
        [SerializeField] float pulseSpeed = 0.5f;
        [Tooltip("Cuánto cambia el ancho (0 = fijo).")]
        [Range(0f, 0.5f)]
        [SerializeField] float breathe = 0.15f;

        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int SeedId = Shader.PropertyToID("_Seed");

        struct Ray
        {
            public Transform pivot;
            public Renderer renderer;
            public float baseAngle;
            public Vector3 baseScale;
            public float seed;
        }

        Ray[] rays = new Ray[0];
        MaterialPropertyBlock block;

        void Start()
        {
            block = new MaterialPropertyBlock();
            rays = new Ray[transform.childCount];
            for (int i = 0; i < rays.Length; i++)
            {
                Transform pivot = transform.GetChild(i);
                Transform beam = pivot.childCount > 0 ? pivot.GetChild(0) : null;
                rays[i] = new Ray
                {
                    pivot = pivot,
                    renderer = beam != null ? beam.GetComponent<Renderer>() : null,
                    baseAngle = pivot.localEulerAngles.z,
                    baseScale = beam != null ? beam.localScale : Vector3.one,
                    seed = i * 1.618f + 0.37f
                };
            }
        }

        void Update()
        {
            float t = Time.time;
            foreach (Ray r in rays)
            {
                if (r.pivot == null) continue;
                float angle = r.baseAngle + Mathf.Sin(t * swaySpeed + r.seed * 2.1f) * swayDegrees;
                r.pivot.localRotation = Quaternion.Euler(0f, 0f, angle);

                if (r.renderer == null) continue;
                Transform beam = r.renderer.transform;
                float w = 1f + Mathf.Sin(t * swaySpeed * 1.7f + r.seed * 3.3f) * breathe;
                beam.localScale = new Vector3(r.baseScale.x * w, r.baseScale.y, r.baseScale.z);

                float k = 1f - pulse * 0.5f + Mathf.Sin(t * pulseSpeed + r.seed * 5.1f) * pulse * 0.5f;
                r.renderer.GetPropertyBlock(block);
                block.SetFloat(IntensityId, intensity * k);
                block.SetFloat(SeedId, r.seed);
                r.renderer.SetPropertyBlock(block);
            }
        }
    }
}
