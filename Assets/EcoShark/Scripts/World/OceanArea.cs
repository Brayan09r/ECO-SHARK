using UnityEngine;

namespace EcoShark
{
    /// <summary>
    /// Rectángulo jugable del océano en X/Y. Un solo lugar para los límites:
    /// el tiburón, la cámara, la basura y los spawners lo leen de aquí.
    /// En la vista Scene se dibuja como un rectángulo celeste.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class OceanArea : MonoBehaviour
    {
        public static OceanArea Current { get; private set; }

        [SerializeField] Vector2 min = new Vector2(-40f, -22f);
        [Tooltip("max.y es la superficie del mar.")]
        [SerializeField] Vector2 max = new Vector2(40f, 6f);

        public Vector2 Min => min;
        public Vector2 Max => max;
        public float SurfaceY => max.y;
        public float FloorY => min.y;

        void Awake() => Current = this;

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        public Vector3 Clamp(Vector3 p, float margin = 0f)
        {
            p.x = Mathf.Clamp(p.x, min.x + margin, max.x - margin);
            p.y = Mathf.Clamp(p.y, min.y + margin, max.y - margin);
            return p;
        }

        /// <param name="heightRange01">0 = fondo, 1 = superficie.</param>
        public Vector3 RandomPoint(Vector2 heightRange01, float margin)
        {
            float bottom = min.y + margin;
            float top = max.y - margin;
            float x = Random.Range(min.x + margin, max.x - margin);
            float y = Random.Range(Mathf.Lerp(bottom, top, heightRange01.x), Mathf.Lerp(bottom, top, heightRange01.y));
            return new Vector3(x, y, 0f);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.8f);
            Vector3 center = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, 0f);
            Vector3 size = new Vector3(max.x - min.x, max.y - min.y, 0.1f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
