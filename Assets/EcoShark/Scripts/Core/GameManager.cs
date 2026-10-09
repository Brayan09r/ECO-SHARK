using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoShark
{
    public enum GameState { Menu, Playing, Paused, GameOver }

    /// <summary>
    /// Director de la partida: cuenta estadísticas, sube la dificultad con el tiempo,
    /// maneja pausa, fin de partida y reinicio. Escucha los GameEvents; no hace física ni UI directamente.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Referencias")]
        [SerializeField] SharkHealth shark;
        [SerializeField] SharkMovement sharkMovement;
        [SerializeField] OceanSpawner spawner;
        [SerializeField] HUDController hud;

        [Header("Dificultad")]
        [Tooltip("Aumento del drenaje de toxicidad por minuto (0.35 = +35 % cada minuto).")]
        [SerializeField] float difficultyRampPerMinute = 0.35f;
        [SerializeField] float maxDifficulty = 3f;

        [Header("Fin de partida")]
        [SerializeField] float gameOverDelay = 1.5f;

        [Header("Menú principal (Claude)")]
        [Tooltip("Si hay un MainMenuController en la escena, la partida empieza en el menú.")]
        [SerializeField] bool startInMenu = true;

        /// <summary>"Jugar de nuevo" recarga la escena y salta el menú una vez.</summary>
        public static bool SkipMenuOnce;

        /// <summary>Se dispara al pasar del menú a la partida.</summary>
        public static event System.Action RunStarted;

        public GameState State { get; private set; } = GameState.Playing;
        public float RunTime { get; private set; }
        public int RunBiomass { get; private set; }
        public int TrashCollected { get; private set; }
        public int AnimalsRescued { get; private set; }
        public int AnimalsLost { get; private set; }
        public bool NewRecord { get; private set; }

        bool canRestart;

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            bool hasMenu = FindFirstObjectByType<MainMenuController>() != null;
            State = startInMenu && hasMenu && !SkipMenuOnce ? GameState.Menu : GameState.Playing;
            SkipMenuOnce = false;
        }

        void Start()
        {
            if (State == GameState.Menu) SetGameplayActive(false);
        }

        /// <summary>Del menú a la partida: activa al tiburón, la toxicidad, el spawner y el HUD.</summary>
        public void StartRun()
        {
            if (State != GameState.Menu) return;
            State = GameState.Playing;
            RunTime = 0f;
            RunBiomass = 0; TrashCollected = 0; AnimalsRescued = 0; AnimalsLost = 0;
            SetGameplayActive(true);
            RunStarted?.Invoke();
        }

        /// <summary>En el menú el tiburón posa: sin física, sin toxicidad, sin recoger basura y sin HUD.</summary>
        void SetGameplayActive(bool on)
        {
            if (sharkMovement != null)
            {
                Rigidbody rb = sharkMovement.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    if (!on) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                    rb.isKinematic = !on;
                    if (on) rb.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
                }
                sharkMovement.enabled = on;
                SharkCollector collector = sharkMovement.GetComponent<SharkCollector>();
                if (collector != null) collector.enabled = on;
            }
            if (shark != null) shark.enabled = on;
            if (spawner != null) spawner.Spawning = on;
            if (hud != null)
            {
                Canvas c = hud.GetComponent<Canvas>();
                if (c != null) c.enabled = on;
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            GameEvents.TrashCollected += HandleTrashCollected;
            GameEvents.AnimalRescued += HandleAnimalRescued;
            GameEvents.AnimalLost += HandleAnimalLost;
            GameEvents.SharkDied += HandleSharkDied;
        }

        void OnDisable()
        {
            GameEvents.TrashCollected -= HandleTrashCollected;
            GameEvents.AnimalRescued -= HandleAnimalRescued;
            GameEvents.AnimalLost -= HandleAnimalLost;
            GameEvents.SharkDied -= HandleSharkDied;
        }

        void Update()
        {
            if (State == GameState.Menu) return;
            if (State != GameState.GameOver && EcoInput.PausePressed())
                TogglePause();

            if (State == GameState.Playing)
            {
                RunTime += Time.deltaTime;
                if (shark != null)
                    shark.DifficultyMultiplier = Mathf.Min(maxDifficulty, 1f + difficultyRampPerMinute * RunTime / 60f);
            }
            else if (State == GameState.GameOver && canRestart && EcoInput.RestartPressed())
            {
                Restart();
            }
        }

        public void TogglePause()
        {
            if (State == GameState.Playing)
            {
                State = GameState.Paused;
                Time.timeScale = 0f;
            }
            else if (State == GameState.Paused)
            {
                State = GameState.Playing;
                Time.timeScale = 1f;
            }
            if (hud != null) hud.ShowPause(State == GameState.Paused);
        }

        void HandleTrashCollected(TrashData data)
        {
            if (State != GameState.Playing || data == null) return;
            TrashCollected++;
            RunBiomass += data.biomass;
        }

        /// <summary>Biomasa extra de la partida: combos y misiones (RunGoals). (Claude)</summary>
        public void AddBonusBiomass(int amount)
        {
            if (State != GameState.Playing || amount <= 0) return;
            RunBiomass += amount;
        }

        void HandleAnimalRescued(TrappedAnimal animal)
        {
            if (State != GameState.Playing || animal == null) return;
            AnimalsRescued++;
            RunBiomass += animal.BiomassReward;
        }

        void HandleAnimalLost(TrappedAnimal animal)
        {
            if (State != GameState.Playing) return;
            AnimalsLost++;
        }

        void HandleSharkDied()
        {
            if (State == GameState.GameOver) return;
            State = GameState.GameOver;
            Time.timeScale = 1f;

            if (sharkMovement != null) sharkMovement.Die();
            if (spawner != null) spawner.Spawning = false;

            // La biomasa de la partida se deposita en la "billetera" persistente.
            Progression.AddBiomass(RunBiomass);
            Progression.AddLifetimeStats(TrashCollected, AnimalsRescued);
            NewRecord = Progression.SubmitRunTime(RunTime);

            StartCoroutine(ShowGameOverAfterDelay());
        }

        IEnumerator ShowGameOverAfterDelay()
        {
            yield return new WaitForSeconds(gameOverDelay);
            canRestart = true;
            if (hud != null) hud.ShowGameOver();
        }

        /// <summary>Jugar de nuevo directamente (sin pasar por el menú).</summary>
        public void Restart()
        {
            SkipMenuOnce = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>Volver al menú principal.</summary>
        public void ReturnToMenu()
        {
            SkipMenuOnce = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
