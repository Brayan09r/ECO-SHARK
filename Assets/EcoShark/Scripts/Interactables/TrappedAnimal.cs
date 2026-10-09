using System.Collections;
using UnityEngine;

namespace EcoShark
{
    /// <summary>
    /// Animal atrapado en una red o aro de plástico.
    /// Mecánica: el tiburón debe quedarse cerca ("mordiendo la red") hasta llenar la barra.
    /// Si se aleja, el progreso baja poco a poco. Si pasa demasiado tiempo, el animal no sobrevive.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TrappedAnimal : MonoBehaviour
    {
        [Header("Datos")]
        [SerializeField] string animalName = "Tortuga marina";
        [TextArea(2, 4)]
        [SerializeField] string educationalFact = "Muchas tortugas marinas mueren enredadas en redes abandonadas (\"redes fantasma\").";
        [SerializeField] int biomassReward = 25;
        [SerializeField] float healReward = 15f;

        [Header("Rescate")]
        [Tooltip("Segundos que el tiburón debe quedarse cerca para liberarlo.")]
        [SerializeField] float rescueTime = 1.5f;
        [Tooltip("Progreso que se pierde por segundo si el tiburón se aleja (1 = toda la barra).")]
        [SerializeField] float progressDecayPerSecond = 0.35f;
        [Tooltip("Segundos antes de que el animal no sobreviva. 0 = sin límite.")]
        [SerializeField] float timeToLose = 60f;

        [Header("Visuales")]
        [SerializeField] Transform animalVisual;
        [SerializeField] GameObject netVisual;
        [Tooltip("Objeto que se escala en X de 0 a 1 según el progreso.")]
        [SerializeField] Transform progressBar;
        [SerializeField] float sinkSpeed = 1.5f;

        public string AnimalName => animalName;
        public string EducationalFact => educationalFact;
        public int BiomassReward => biomassReward;
        public float Progress01 => progress;
        /// <summary>True si ya fue rescatado o se perdió (ObjectiveGuide deja de señalarlo). (Claude)</summary>
        public bool IsFinished => finished;
        public float TimeLeft01 => timeToLose > 0f ? 1f - lifeTimer / timeToLose : 1f;

        float progress;
        float lifeTimer;
        bool finished;
        bool sinking;
        float sinkTargetY;
        SharkHealth sharkNearby;
        Quaternion animalBaseRotation = Quaternion.identity;

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (animalVisual != null) animalBaseRotation = animalVisual.localRotation;
            UpdateBar();
        }

        /// <summary>Lo usa el barco pesquero: la red cae hasta esta altura.</summary>
        public void BeginSinking(float targetY)
        {
            sinkTargetY = targetY;
            sinking = true;
        }

        void Update()
        {
            if (finished) return;
            float dt = Time.deltaTime;

            if (sinking)
            {
                Vector3 p = transform.position;
                p.y = Mathf.MoveTowards(p.y, sinkTargetY, sinkSpeed * dt);
                transform.position = p;
                if (Mathf.Approximately(p.y, sinkTargetY)) sinking = false;
            }

            bool helping = sharkNearby != null && !sharkNearby.IsDead && sharkNearby.isActiveAndEnabled; // (Claude) en el menú no rescata
            if (helping) progress += dt / Mathf.Max(0.05f, rescueTime);
            else progress = Mathf.Max(0f, progress - progressDecayPerSecond * dt);
            UpdateBar();

            if (progress >= 1f)
            {
                Rescue();
                return;
            }

            if (timeToLose > 0f)
            {
                lifeTimer += dt;
                if (lifeTimer >= timeToLose)
                {
                    Lose();
                    return;
                }
            }

            // El animal forcejea (más fuerte cuando lo están ayudando).
            if (animalVisual != null)
            {
                float wiggle = Mathf.Sin(Time.time * (helping ? 22f : 8f)) * (helping ? 14f : 7f);
                animalVisual.localRotation = animalBaseRotation * Quaternion.Euler(0f, 0f, wiggle);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            SharkHealth shark = other.GetComponentInParent<SharkHealth>();
            if (shark != null) sharkNearby = shark;
        }

        void OnTriggerExit(Collider other)
        {
            SharkHealth shark = other.GetComponentInParent<SharkHealth>();
            if (shark != null && shark == sharkNearby) sharkNearby = null;
        }

        void UpdateBar()
        {
            if (progressBar == null) return;
            bool show = progress > 0.001f && !finished;
            if (progressBar.gameObject.activeSelf != show) progressBar.gameObject.SetActive(show);
            progressBar.localScale = new Vector3(Mathf.Clamp01(progress), 1f, 1f);
        }

        void Rescue()
        {
            finished = true;
            progress = 1f;
            GetComponent<Collider>().enabled = false;
            if (netVisual != null) netVisual.SetActive(false);
            UpdateBar();
            if (sharkNearby != null) sharkNearby.Heal(healReward);
            GameEvents.RaiseAnimalRescued(this);
            StartCoroutine(SwimAway());
        }

        void Lose()
        {
            finished = true;
            GetComponent<Collider>().enabled = false;
            UpdateBar();
            GameEvents.RaiseAnimalLost(this);
            StartCoroutine(SinkAndFade());
        }

        IEnumerator SwimAway()
        {
            Vector3 dir = new Vector3(Random.value < 0.5f ? -1f : 1f, 0.5f, 0f).normalized;
            if (animalVisual != null && dir.x < 0f)
                animalVisual.localRotation = animalBaseRotation * Quaternion.Euler(0f, 180f, 0f);

            Vector3 startScale = transform.localScale;
            const float duration = 2.5f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                transform.position += dir * 5f * Time.deltaTime;
                if (t > duration - 0.7f)
                    transform.localScale = Vector3.Lerp(startScale, Vector3.zero, (t - (duration - 0.7f)) / 0.7f);
                yield return null;
            }
            Destroy(gameObject);
        }

        IEnumerator SinkAndFade()
        {
            Vector3 startScale = transform.localScale;
            const float duration = 3f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                transform.position += Vector3.down * Time.deltaTime;
                if (animalVisual != null)
                    animalVisual.localRotation = animalBaseRotation * Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 180f, t / duration));
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t / duration);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
