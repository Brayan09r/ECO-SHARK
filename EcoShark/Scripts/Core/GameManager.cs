using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoShark
{
    public enum GameState { Playing, Paused, GameOver }

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
            if (State == GameState.GameOver || data == null) return;
            TrashCollected++;
            RunBiomass += data.biomass;
        }

        void HandleAnimalRescued(TrappedAnimal animal)
        {
            if (State == GameState.GameOver || animal == null) return;
            AnimalsRescued++;
            RunBiomass += animal.BiomassReward;
        }

        void HandleAnimalLost(TrappedAnimal animal)
        {
            if (State == GameState.GameOver) return;
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
            NewRecord = Progression.SubmitRunTime(RunTime);

            StartCoroutine(ShowGameOverAfterDelay());
        }

        IEnumerator ShowGameOverAfterDelay()
        {
            yield return new WaitForSeconds(gameOverDelay);
            canRestart = true;
            if (hud != null) hud.ShowGameOver();
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
