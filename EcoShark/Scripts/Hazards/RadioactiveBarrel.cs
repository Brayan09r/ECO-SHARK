using UnityEngine;

namespace EcoShark
{
    /// <summary>
    /// Barril de desechos radiactivos: cae desde la superficie hasta el fondo.
    /// Tocarlo quita un bloque de energía y empuja al tiburón.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RadioactiveBarrel : MonoBehaviour
    {
        [SerializeField] float damage = 20f;
        [SerializeField] float knockback = 9f;
        [SerializeField] float sinkSpeed = 0.8f;
        [Tooltip("Segundos antes de desaparecer. 0 = permanente.")]
        [SerializeField] float lifetime = 40f;

        [Header("Visual")]
        [Tooltip("Parte brillante que 'late'.")]
        [SerializeField] Transform glow;
        [SerializeField] float pulseSpeed = 4f;

        Vector3 glowBaseScale = Vector3.one;
        float spin;

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (lifetime > 0f) Destroy(gameObject, lifetime);
            if (glow != null) glowBaseScale = glow.localScale;
            spin = Random.Range(-40f, 40f);
        }

        void Update()
        {
            OceanArea area = OceanArea.Current;
            float floor = area != null ? area.FloorY + 0.8f : float.NegativeInfinity;

            Vector3 p = transform.position;
            if (p.y > floor)
            {
                p.y = Mathf.Max(floor, p.y - sinkSpeed * Time.deltaTime);
                transform.position = p;
                transform.Rotate(0f, 0f, spin * Time.deltaTime, Space.World);
            }

            if (glow != null)
            {
                float s = 1f + Mathf.Sin(Time.time * pulseSpeed) * 0.08f;
                glow.localScale = glowBaseScale * s;
            }
        }

        void OnTriggerStay(Collider other)
        {
            SharkHealth health = other.GetComponentInParent<SharkHealth>();
            if (health == null) return;

            if (health.TakeDamage(damage))
            {
                SharkMovement movement = other.GetComponentInParent<SharkMovement>();
                if (movement != null)
                {
                    Vector3 away = other.transform.position - transform.position;
                    away.z = 0f;
                    if (away.sqrMagnitude < 0.001f) away = Vector3.up;
                    movement.ApplyImpulse(away.normalized * knockback);
                }
            }
        }
    }
}
