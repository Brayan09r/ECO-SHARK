using System.Collections.Generic;
using UnityEngine;

namespace EcoShark
{
    /// <summary>
    /// Barco de pesca ilegal: patrulla la superficie y suelta redes con animales atrapados.
    /// Une dos mecánicas: es un obstáculo (chocar hace daño) y es la FUENTE de los rescates.
    /// Mensaje ODS 14.4: la pesca ilegal genera redes fantasma y captura incidental.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class IllegalFishingBoat : MonoBehaviour
    {
        [Header("Patrulla")]
        [SerializeField] float speed = 2.5f;
        [Tooltip("Distancia a los bordes del océano donde el barco da la vuelta.")]
        [SerializeField] float edgeMargin = 6f;
        [Tooltip("Modelo que se voltea al cambiar de dirección.")]
        [SerializeField] Transform visual;

        [Header("Redes")]
        [SerializeField] GameObject[] dropPrefabs;
        [SerializeField] Transform dropPoint;
        [SerializeField] float dropInterval = 15f;
        [SerializeField] int maxActiveDrops = 3;

        [Header("Choque")]
        [SerializeField] float contactDamage = 15f;
        [SerializeField] float knockback = 10f;

        int direction = 1;
        float dropTimer;
        readonly List<GameObject> activeDrops = new List<GameObject>();

        void Awake() => GetComponent<Collider>().isTrigger = true;

        void Start() => dropTimer = dropInterval * 0.4f;

        void Update()
        {
            Patrol();

            dropTimer -= Time.deltaTime;
            if (dropTimer <= 0f)
            {
                dropTimer = dropInterval;
                TryDropNet();
            }
        }

        void Patrol()
        {
            float minX = -30f, maxX = 30f;
            OceanArea area = OceanArea.Current;
            if (area != null)
            {
                minX = area.Min.x + edgeMargin;
                maxX = area.Max.x - edgeMargin;
            }

            Vector3 p = transform.position;
            p.x += direction * speed * Time.deltaTime;
            if (p.x > maxX) { p.x = maxX; direction = -1; }
            else if (p.x < minX) { p.x = minX; direction = 1; }
            transform.position = p;

            if (visual != null)
                visual.localRotation = Quaternion.Euler(0f, direction > 0 ? 0f : 180f, 0f);
        }

        void TryDropNet()
        {
            for (int i = activeDrops.Count - 1; i >= 0; i--)
                if (activeDrops[i] == null) activeDrops.RemoveAt(i);

            if (dropPrefabs == null || dropPrefabs.Length == 0 || activeDrops.Count >= maxActiveDrops) return;

            GameObject prefab = dropPrefabs[Random.Range(0, dropPrefabs.Length)];
            if (prefab == null) return;

            Vector3 pos = dropPoint != null ? dropPoint.position : transform.position + Vector3.down;
            pos.z = 0f;
            GameObject net = Instantiate(prefab, pos, Quaternion.identity);
            activeDrops.Add(net);

            TrappedAnimal animal = net.GetComponent<TrappedAnimal>();
            if (animal != null)
            {
                OceanArea area = OceanArea.Current;
                float lowest = area != null ? area.FloorY + 3f : pos.y - 12f;
                float highest = area != null ? area.SurfaceY - 5f : pos.y - 3f;
                animal.BeginSinking(Random.Range(lowest, Mathf.Max(lowest, highest)));
            }
        }

        void OnTriggerStay(Collider other)
        {
            SharkHealth health = other.GetComponentInParent<SharkHealth>();
            if (health == null) return;

            if (health.TakeDamage(contactDamage))
            {
                SharkMovement movement = other.GetComponentInParent<SharkMovement>();
                if (movement != null) movement.ApplyImpulse(Vector3.down * knockback);
            }
        }
    }
}
