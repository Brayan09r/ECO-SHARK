using UnityEngine;

namespace EcoShark
{
    /// <summary>
    /// Mancha de petróleo: dentro de ella el tiburón nada más lento y se intoxica más rápido.
    /// No quita vida de golpe; castiga quedarse dentro.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class OilSlick : MonoBehaviour
    {
        [Range(0.1f, 1f)]
        [SerializeField] float speedMultiplier = 0.45f;
        [SerializeField] float extraToxicityPerSecond = 4f;
        [SerializeField] Vector2 driftVelocity = new Vector2(0.4f, 0f);
        [Tooltip("Segundos antes de desaparecer. 0 = permanente.")]
        [SerializeField] float lifetime = 45f;

        SharkMovement affectedMovement;
        SharkHealth affectedHealth;

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (lifetime > 0f) Destroy(gameObject, lifetime);
        }

        void Update()
        {
            Vector3 p = transform.position + new Vector3(driftVelocity.x, driftVelocity.y, 0f) * Time.deltaTime;
            OceanArea area = OceanArea.Current;
            if (area != null)
            {
                if ((p.x < area.Min.x + 2f && driftVelocity.x < 0f) || (p.x > area.Max.x - 2f && driftVelocity.x > 0f))
                    driftVelocity.x = -driftVelocity.x;
                p = area.Clamp(p, 1f);
            }
            transform.position = p;
        }

        void OnTriggerEnter(Collider other)
        {
            SharkMovement movement = other.GetComponentInParent<SharkMovement>();
            if (movement != null)
            {
                affectedMovement = movement;
                movement.SetSpeedModifier(this, speedMultiplier);
            }

            SharkHealth health = other.GetComponentInParent<SharkHealth>();
            if (health != null)
            {
                affectedHealth = health;
                health.SetExtraDrain(this, extraToxicityPerSecond);
            }
        }

        void OnTriggerExit(Collider other)
        {
            SharkMovement movement = other.GetComponentInParent<SharkMovement>();
            if (movement != null && movement == affectedMovement) Release();
        }

        // Si la mancha desaparece con el tiburón dentro, hay que devolverle su velocidad.
        void OnDisable() => Release();

        void Release()
        {
            if (affectedMovement != null) affectedMovement.RemoveSpeedModifier(this);
            if (affectedHealth != null) affectedHealth.RemoveExtraDrain(this);
            affectedMovement = null;
            affectedHealth = null;
        }
    }
}
