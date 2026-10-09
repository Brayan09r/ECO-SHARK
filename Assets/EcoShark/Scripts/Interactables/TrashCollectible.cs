using System.Collections;
using UnityEngine;

namespace EcoShark
{
    /// <summary>
    /// Basura flotante. Deriva lentamente, se balancea, y al entrar al radio del imán vuela hacia la boca.
    /// Los valores (curación, biomasa, dato educativo) vienen de un TrashData.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TrashCollectible : MonoBehaviour, ICollectible
    {
        [SerializeField] TrashData data;

        [Header("Movimiento a la deriva")]
        [SerializeField] Vector2 driftVelocity = new Vector2(0f, -0.12f);
        [SerializeField] float bobAmplitude = 0.2f;
        [SerializeField] float bobFrequency = 1.2f;
        [SerializeField] float spinSpeed = 25f;

        [Header("Al ser tragada")]
        [SerializeField] float swallowTime = 0.15f;

        Vector3 basePosition;
        Vector3 spinAxis;
        float seed;
        bool collected;
        float attractedUntil;
        Vector3 attractTarget;
        float attractSpeed;

        public TrashData Data => data;
        public bool CanBeCollected => !collected;

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            basePosition = transform.position;
            seed = Random.value * 10f;
            spinAxis = Random.onUnitSphere;
        }

        void Update()
        {
            if (collected) return;
            float dt = Time.deltaTime;

            if (Time.time < attractedUntil)
            {
                // Atraída por el imán.
                basePosition = Vector3.MoveTowards(basePosition, attractTarget, attractSpeed * dt);
                transform.position = basePosition;
            }
            else
            {
                // A la deriva con balanceo.
                basePosition += new Vector3(driftVelocity.x, driftVelocity.y, 0f) * dt;
                OceanArea area = OceanArea.Current;
                if (area != null) basePosition = area.Clamp(basePosition, 0.4f);
                float bob = Mathf.Sin((Time.time + seed) * bobFrequency) * bobAmplitude;
                transform.position = basePosition + Vector3.up * bob;
            }

            transform.Rotate(spinAxis, spinSpeed * dt, Space.World);
        }

        public void Attract(Vector3 target, float speed)
        {
            if (collected) return;
            if (Time.time >= attractedUntil) basePosition = transform.position; // evita un salto al empezar
            attractTarget = new Vector3(target.x, target.y, 0f);
            attractSpeed = speed;
            attractedUntil = Time.time + 0.1f;
        }

        public void Collect(SharkCollector collector)
        {
            if (collected) return;
            collected = true;
            GetComponent<Collider>().enabled = false;
            collector.Consume(data);
            StartCoroutine(Swallow(collector));
        }

        IEnumerator Swallow(SharkCollector collector)
        {
            Vector3 startScale = transform.localScale;
            float t = 0f;
            while (t < swallowTime)
            {
                t += Time.deltaTime;
                float k = t / swallowTime;
                if (collector != null)
                    transform.position = Vector3.Lerp(transform.position, collector.MouthPosition, k);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, k);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
