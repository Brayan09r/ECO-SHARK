using UnityEngine;

namespace EcoShark
{
    /// <summary>
    /// Cámara 2.5D: perspectiva real (profundidad y paralaje del fondo), pero sigue al tiburón solo en X/Y.
    /// Mira un poco hacia adelante del movimiento y no muestra fuera del océano.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 offset = new Vector3(0f, 1f, -18f);
        [SerializeField] float smoothTime = 0.25f;
        [Tooltip("Cuánto se adelanta la cámara en la dirección del nado.")]
        [SerializeField] float lookAhead = 2f;

        [Header("Límites")]
        [SerializeField] bool clampToOcean = true;
        [Tooltip("Cuánto cielo se permite ver sobre la superficie.")]
        [SerializeField] float extraAboveSurface = 1.5f;

        /// <summary>Distancia/posición respecto al tiburón (el menú acerca la cámara). (Claude)</summary>
        public Vector3 Offset { get => offset; set => offset = value; }
        public bool ClampToOcean { get => clampToOcean; set => clampToOcean = value; }
        public float SmoothTime { get => smoothTime; set => smoothTime = value; }

        /// <summary>Coloca la cámara en su sitio sin suavizado.</summary>
        public void Snap()
        {
            if (target == null) return;
            velocity = Vector3.zero;
            transform.position = DesiredPosition();
        }

        Camera cam;
        SharkMovement movement;
        Vector3 velocity;

        void Awake() => cam = GetComponent<Camera>();

        void Start()
        {
            if (target == null) return;
            movement = target.GetComponent<SharkMovement>();
            transform.position = DesiredPosition();
        }

        void LateUpdate()
        {
            if (target == null) return;
            transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition(), ref velocity, smoothTime);
        }

        Vector3 DesiredPosition()
        {
            Vector3 p = target.position + offset;

            if (movement != null && movement.MaxSpeed > 0.01f)
            {
                Vector3 v = movement.Velocity;
                p += new Vector3(v.x, v.y, 0f) / movement.MaxSpeed * lookAhead;
            }

            OceanArea area = OceanArea.Current;
            if (clampToOcean && area != null && cam != null && !cam.orthographic)
            {
                float distance = Mathf.Abs(offset.z);  // (con el zoom del menú el límite se recalcula solo)
                float halfHeight = distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float halfWidth = halfHeight * cam.aspect;
                p.x = ClampOrCenter(p.x, area.Min.x + halfWidth, area.Max.x - halfWidth);
                p.y = ClampOrCenter(p.y, area.Min.y + halfHeight, area.Max.y + extraAboveSurface - halfHeight);
            }
            return p;
        }

        static float ClampOrCenter(float value, float min, float max)
        {
            return min > max ? (min + max) * 0.5f : Mathf.Clamp(value, min, max);
        }
    }
}
