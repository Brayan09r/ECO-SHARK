using UnityEngine;

namespace EcoShark
{
    /// <summary>
    /// Niebla submarina que cambia con la profundidad: turquesa cerca de la superficie y azul petróleo (#0B2B47) abajo.
    /// Así lo lejano se funde con el agua y, al bajar, todo se vuelve más oscuro y profundo.
    /// Va en "FondoMarinoVivo". Solo actúa en Play (en el editor queda la niebla base que puso el instalador).
    /// </summary>
    [DefaultExecutionOrder(150)]
    public class UnderwaterAtmosphere : MonoBehaviour
    {
        [SerializeField] Color deepFog = new Color32(0x0B, 0x2B, 0x47, 0xFF);
        [SerializeField] Color shallowFog = new Color(0.07f, 0.34f, 0.46f);
        [Tooltip("Densidad de la niebla (exponencial). Suave = 0.015 a 0.025.")]
        [SerializeField] float density = 0.02f;
        [Tooltip("Qué tan rápido pasa de turquesa a azul profundo al bajar (1 = lineal).")]
        [SerializeField] float curve = 1.4f;

        Camera cam;

        public float Density => density;

        void LateUpdate()
        {
            if (!Application.isPlaying) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            Apply(cam.transform.position.y, cam);
        }

        /// <summary>Color de la niebla con la cámara a esa altura.</summary>
        public Color FogFor(float cameraY)
        {
            float top = 6f, bottom = -22f;
            if (OceanArea.Current != null) { top = OceanArea.Current.Max.y; bottom = OceanArea.Current.Min.y; }
            else
            {
                OceanArea area = FindFirstObjectByType<OceanArea>();
                if (area != null) { top = area.Max.y; bottom = area.Min.y; }
            }
            float t = Mathf.Clamp01((cameraY - bottom) / Mathf.Max(0.01f, top - bottom));
            return Color.Lerp(deepFog, shallowFog, Mathf.Pow(t, curve));
        }

        public void Apply(float cameraY, Camera target)
        {
            Color c = FogFor(cameraY);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = c;
            RenderSettings.fogDensity = density;
            if (target != null) target.backgroundColor = c;
        }
    }
}
