using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EcoShark
{
    /// <summary>
    /// HUD provisional (greybox). Solo MUESTRA información: escucha eventos y lee al GameManager.
    /// Cuando tengas el diseño de Figma, reemplaza los elementos visuales y conserva este script.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] SharkHealth shark;

        [Header("Barra de energía / toxicidad")]
        [Tooltip("RectTransform del relleno; se ajusta su anchorMax.x de 0 a 1.")]
        [SerializeField] RectTransform healthFill;
        [SerializeField] Image healthFillImage;
        [SerializeField] Color healthyColor = new Color(0.2f, 0.9f, 0.6f);
        [SerializeField] Color toxicColor = new Color(0.7f, 0.15f, 0.55f);

        [Header("Contadores")]
        [SerializeField] Text biomassText;
        [SerializeField] Text rescuedText;
        [SerializeField] Text timeText;

        [Header("Mensajes educativos")]
        [SerializeField] GameObject factPanel;
        [SerializeField] Text factText;
        [SerializeField] float factDuration = 5f;

        [Header("Feedback de daño")]
        [SerializeField] Image damageFlash;
        [SerializeField] float flashFadeSpeed = 2.5f;

        [Header("Paneles")]
        [SerializeField] GameObject pausePanel;
        [SerializeField] GameObject gameOverPanel;
        [SerializeField] Text gameOverStatsText;
        [SerializeField] Text walletText;
        [SerializeField] Button restartButton;
        [Tooltip("Orden: Velocidad, Radio de atracción, Resistencia (igual que UpgradeType).")]
        [SerializeField] Button[] upgradeButtons = new Button[0];
        [SerializeField] Text[] upgradeLabels = new Text[0];

        readonly HashSet<TrashData> factsShown = new HashSet<TrashData>();
        float factTimer;
        int lastBiomass = -1, lastRescued = -1, lastSeconds = -1;

        void Start()
        {
            if (shark != null)
            {
                shark.HealthChanged += OnHealthChanged;
                shark.Damaged += OnDamaged;
                OnHealthChanged(shark.Current, shark.Max);
            }

            GameEvents.TrashCollected += OnTrashCollected;
            GameEvents.AnimalRescued += OnAnimalRescued;
            GameEvents.AnimalLost += OnAnimalLost;

            if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
            for (int i = 0; i < upgradeButtons.Length; i++)
            {
                if (upgradeButtons[i] == null) continue;
                UpgradeType type = (UpgradeType)i; // copia local para el lambda
                upgradeButtons[i].onClick.AddListener(() => BuyUpgrade(type));
            }

            SetActive(factPanel, false);
            SetActive(pausePanel, false);
            SetActive(gameOverPanel, false);
            if (damageFlash != null) SetAlpha(damageFlash, 0f);
        }

        void OnDestroy()
        {
            if (shark != null)
            {
                shark.HealthChanged -= OnHealthChanged;
                shark.Damaged -= OnDamaged;
            }
            GameEvents.TrashCollected -= OnTrashCollected;
            GameEvents.AnimalRescued -= OnAnimalRescued;
            GameEvents.AnimalLost -= OnAnimalLost;
        }

        void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                // Solo reescribe el texto si cambió (menos basura de memoria en WebGL).
                if (biomassText != null && gm.RunBiomass != lastBiomass)
                {
                    lastBiomass = gm.RunBiomass;
                    biomassText.text = "Biomasa: " + lastBiomass;
                }
                if (rescuedText != null && gm.AnimalsRescued != lastRescued)
                {
                    lastRescued = gm.AnimalsRescued;
                    rescuedText.text = "Rescates: " + lastRescued;
                }
                int seconds = Mathf.FloorToInt(gm.RunTime);
                if (timeText != null && seconds != lastSeconds)
                {
                    lastSeconds = seconds;
                    timeText.text = "Tiempo: " + FormatTime(seconds);
                }
            }

            if (factTimer > 0f)
            {
                factTimer -= Time.unscaledDeltaTime;
                if (factTimer <= 0f) SetActive(factPanel, false);
            }

            if (damageFlash != null && damageFlash.color.a > 0f)
                SetAlpha(damageFlash, Mathf.MoveTowards(damageFlash.color.a, 0f, flashFadeSpeed * Time.unscaledDeltaTime));
        }

        // ------------------------------------------------------------------ Reacciones a eventos

        void OnHealthChanged(float current, float max)
        {
            float t = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (healthFill != null) healthFill.anchorMax = new Vector2(t, healthFill.anchorMax.y);
            if (healthFillImage != null) healthFillImage.color = Color.Lerp(toxicColor, healthyColor, t);
        }

        void OnDamaged(float amount)
        {
            if (damageFlash != null) SetAlpha(damageFlash, 0.35f);
        }

        void OnTrashCollected(TrashData data)
        {
            if (data == null || string.IsNullOrEmpty(data.educationalFact)) return;
            if (factsShown.Add(data)) ShowMessage(data.displayName, data.educationalFact); // solo la 1.ª vez
        }

        void OnAnimalRescued(TrappedAnimal animal)
        {
            if (animal == null) return;
            ShowMessage("¡Rescataste: " + animal.AnimalName + "!", animal.EducationalFact);
        }

        void OnAnimalLost(TrappedAnimal animal)
        {
            if (animal == null) return;
            ShowMessage("Un animal no sobrevivió", animal.AnimalName + " pasó demasiado tiempo atrapado en la red.");
        }

        // ------------------------------------------------------------------ API para el GameManager

        public void ShowMessage(string title, string body)
        {
            if (factText == null) return;
            factText.text = "<b>" + title + "</b>\n" + body;
            SetActive(factPanel, true);
            factTimer = factDuration;
        }

        public void ShowPause(bool show) => SetActive(pausePanel, show);

        public void ShowGameOver()
        {
            SetActive(factPanel, false);
            SetActive(gameOverPanel, true);

            GameManager gm = GameManager.Instance;
            if (gm != null && gameOverStatsText != null)
            {
                gameOverStatsText.text =
                    "Sobreviviste: " + FormatTime(Mathf.FloorToInt(gm.RunTime)) + (gm.NewRecord ? "   ¡NUEVO RÉCORD!" : "") + "\n" +
                    "Basura recolectada: " + gm.TrashCollected + "\n" +
                    "Animales rescatados: " + gm.AnimalsRescued + "    Perdidos: " + gm.AnimalsLost + "\n" +
                    "Biomasa ganada: +" + gm.RunBiomass + "\n" +
                    "Mejor tiempo: " + FormatTime(Mathf.FloorToInt(Progression.BestTime));
            }
            RefreshShop();
        }

        // ------------------------------------------------------------------ Tienda

        void RefreshShop()
        {
            if (walletText != null) walletText.text = "Biomasa disponible: " + Progression.Biomass;

            for (int i = 0; i < upgradeButtons.Length; i++)
            {
                UpgradeType type = (UpgradeType)i;
                bool maxed = Progression.IsMaxed(type);
                int level = Progression.GetLevel(type);

                if (i < upgradeLabels.Length && upgradeLabels[i] != null)
                {
                    upgradeLabels[i].text =
                        Progression.DisplayName(type) + "\nNivel " + level + "/" + Progression.MaxLevel + "\n" +
                        (maxed ? "MÁXIMO" : Progression.GetCost(type) + " de biomasa");
                }
                if (upgradeButtons[i] != null) upgradeButtons[i].interactable = Progression.CanBuy(type);
            }
        }

        void BuyUpgrade(UpgradeType type)
        {
            if (Progression.TryBuy(type)) RefreshShop();
        }

        void OnRestartClicked()
        {
            if (GameManager.Instance != null) GameManager.Instance.Restart();
        }

        // ------------------------------------------------------------------ Utilidades

        static string FormatTime(int totalSeconds)
        {
            return (totalSeconds / 60).ToString("00") + ":" + (totalSeconds % 60).ToString("00");
        }

        static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }

        static void SetAlpha(Graphic g, float a)
        {
            Color c = g.color;
            c.a = a;
            g.color = c;
        }
    }
}
