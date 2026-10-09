using UnityEngine;

namespace EcoShark
{
    /// <summary>
    /// "Boca + imán" del tiburón.
    ///  - Cada paso de física busca coleccionables dentro del radio de atracción y los jala hacia la boca.
    ///  - Al tocar el cuerpo del tiburón, el coleccionable se "traga": cura y avisa al juego.
    /// El radio crece con la mejora "Radio de atracción" de la tienda.
    /// </summary>
    [RequireComponent(typeof(SharkHealth))]
    public class SharkCollector : MonoBehaviour
    {
        [SerializeField] float baseMagnetRadius = 2.5f;
        [SerializeField] float magnetPullSpeed = 11f;
        [Tooltip("Punto hacia donde vuela la basura. Si está vacío, usa un punto frente al tiburón.")]
        [SerializeField] Transform mouth;
        [SerializeField] LayerMask collectibleMask = ~0;

        readonly Collider[] buffer = new Collider[64];
        SharkHealth health;

        public float MagnetRadius { get; private set; }
        public SharkHealth Health => health;
        public Vector3 MouthPosition => mouth != null ? mouth.position : transform.position + transform.forward * 1.2f;

        void Awake()
        {
            health = GetComponent<SharkHealth>();
            MagnetRadius = baseMagnetRadius + Progression.MagnetRadiusBonus;
        }

        void FixedUpdate()
        {
            if (health.IsDead) return;

            Vector3 center = MouthPosition;
            int count = Physics.OverlapSphereNonAlloc(center, MagnetRadius, buffer, collectibleMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                ICollectible item = buffer[i].GetComponentInParent<ICollectible>();
                if (item != null && item.CanBeCollected)
                    item.Attract(center, magnetPullSpeed);
            }
        }

        void OnTriggerEnter(Collider other) => TryCollect(other);
        void OnTriggerStay(Collider other) => TryCollect(other);

        void TryCollect(Collider other)
        {
            if (!isActiveAndEnabled || health.IsDead) return; // (Claude) Unity avisa choques aunque el componente esté apagado (menú)
            ICollectible item = other.GetComponentInParent<ICollectible>();
            if (item != null && item.CanBeCollected)
                item.Collect(this);
        }

        /// <summary>Lo llama la basura al ser tragada.</summary>
        public void Consume(TrashData data)
        {
            if (data == null) return;
            health.Heal(data.healAmount);
            GameEvents.RaiseTrashCollected(data);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.6f);
            float radius = Application.isPlaying ? MagnetRadius : baseMagnetRadius;
            Gizmos.DrawWireSphere(MouthPosition, radius);
        }
    }
}
