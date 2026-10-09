using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EcoShark
{
    /// <summary>
    /// (Claude) Objetivos dentro de la partida, para que el océano grande tenga "algo que hacer ahora":
    ///  - COMBO: recoger basura seguida (sin dejar pasar "comboWindow" segundos) sube un multiplicador
    ///    x2, x3, x4 que da biomasa extra por cada residuo.
    ///  - MISIONES cortas, una tras otra y cada vez más exigentes: recoger N residuos, rescatar N animales,
    ///    lograr un combo de N. Al cumplirlas dan biomasa extra.
    ///  - Números flotantes "+5" junto al tiburón al recoger basura o rescatar.
    /// La biomasa extra se suma con GameManager.AddBonusBiomass. No guarda nada entre partidas.
    /// Va en el objeto "HUD" (el Canvas). Menú del editor: ECO SHARK > 11.
    /// </summary>
    public class RunGoals : MonoBehaviour
    {
        [Header("Combo")]
        [Tooltip("Segundos máximos entre un residuo y el siguiente para mantener el combo.")]
        [SerializeField] float comboWindow = 3f;
        [Tooltip("Cada cuántos residuos seguidos sube el multiplicador.")]
        [SerializeField] int comboStep = 5;
        [SerializeField] int maxMultiplier = 4;

        [Header("Misiones")]
        [SerializeField] int firstReward = 40;
        [SerializeField] int rewardGrowth = 25;

        enum GoalType { Trash, Rescue, Combo }

        // Combo.
        int comboCount;
        float comboTimer;
        public int ComboCount => comboCount;
        public int Multiplier => Mathf.Clamp(1 + comboCount / Mathf.Max(1, comboStep), 1, maxMultiplier);

        // Misión actual.
        GoalType goalType;
        int goalIndex, goalTarget, goalProgress, goalReward;
        float goalDoneTimer;

        // UI.
        RectTransform layer, missionPill, missionFill, comboRoot, comboFill;
        Image missionIconCircle, missionFillImage;
        Text missionText, missionCount, comboText, comboSub;
        float missionPunch, comboPunch;
        Transform shark;
        Camera cam;

        class Floater { public Text text; public float age, life; public Vector2 start; public Color color; }
        readonly List<Floater> floaters = new List<Floater>();
        int nextFloater;

        void Start()
        {
            Build();
            SharkMovement sm = FindFirstObjectByType<SharkMovement>(FindObjectsInactive.Include);
            if (sm != null) shark = sm.transform;
            ResetRun();
        }

        void OnEnable()
        {
            GameEvents.TrashCollected += OnTrash;
            GameEvents.AnimalRescued += OnRescued;
            GameManager.RunStarted += ResetRun;
        }

        void OnDisable()
        {
            GameEvents.TrashCollected -= OnTrash;
            GameEvents.AnimalRescued -= OnRescued;
            GameManager.RunStarted -= ResetRun;
        }

        void ResetRun()
        {
            comboCount = 0;
            comboTimer = 0f;
            goalIndex = 0;
            goalDoneTimer = 0f;
            SetGoal(0);
        }

        // ---------------------------------------------------------------- Misiones

        void SetGoal(int index)
        {
            goalIndex = index;
            goalProgress = 0;
            int round = index / 3;
            goalType = (GoalType)(index % 3);
            switch (goalType)
            {
                case GoalType.Trash: goalTarget = 15 + round * 10; break;
                case GoalType.Rescue: goalTarget = 1 + round; break;
                default: goalTarget = 8 + round * 4; break;
            }
            goalReward = firstReward + rewardGrowth * index;
            RefreshMission();
        }

        string GoalLabel()
        {
            switch (goalType)
            {
                case GoalType.Trash: return "Recoge " + goalTarget + " residuos";
                case GoalType.Rescue: return goalTarget == 1 ? "Rescata 1 animal" : "Rescata " + goalTarget + " animales";
                default: return "Logra un combo de " + goalTarget;
            }
        }

        void RefreshMission()
        {
            if (missionText == null) return;
            bool done = goalDoneTimer > 0f;
            missionText.text = done ? "¡Misión cumplida!  +" + goalReward : GoalLabel();
            missionCount.text = done ? "" : Mathf.Min(goalProgress, goalTarget) + "/" + goalTarget;
            missionFill.anchorMax = new Vector2(Mathf.Max(0.001f, done ? 1f : Mathf.Clamp01(goalProgress / (float)goalTarget)), 1f);
            missionFillImage.color = done ? UiKit.EcoGreen : UiKit.Teal;
            missionIconCircle.color = done ? UiKit.EcoGreen : UiKit.Yellow;
        }

        void Advance(GoalType type, int value, bool absolute)
        {
            if (goalDoneTimer > 0f || type != goalType) return;
            goalProgress = absolute ? Mathf.Max(goalProgress, value) : goalProgress + value;
            if (goalProgress >= goalTarget)
            {
                goalDoneTimer = 2.2f;
                missionPunch = 1f;
                GameManager gm = GameManager.Instance;
                if (gm != null) gm.AddBonusBiomass(goalReward);
            }
            RefreshMission();
        }

        // ---------------------------------------------------------------- Eventos

        bool Playing => GameManager.Instance == null || GameManager.Instance.State == GameState.Playing;

        void OnTrash(TrashData data)
        {
            if (!Playing || data == null) return;
            comboCount++;
            comboTimer = comboWindow;
            comboPunch = 1f;

            int mult = Multiplier;
            int bonus = data.biomass * (mult - 1);
            if (bonus > 0 && GameManager.Instance != null) GameManager.Instance.AddBonusBiomass(bonus);
            Float("+" + (data.biomass + bonus), mult > 1 ? UiKit.Yellow : UiKit.EcoGreen, mult > 1 ? 40 : 34);

            Advance(GoalType.Trash, 1, false);
            Advance(GoalType.Combo, comboCount, true);
        }

        void OnRescued(TrappedAnimal animal)
        {
            if (!Playing || animal == null) return;
            Float("¡Rescate!  +" + animal.BiomassReward, UiKit.Coral, 46);
            Advance(GoalType.Rescue, 1, false);
        }

        // ---------------------------------------------------------------- Construcción

        void Build()
        {
            RectTransform canvas = (RectTransform)transform;
            layer = UiKit.Rect("Objetivos", canvas);
            UiKit.Stretch(layer, 0f);
            Transform flash = transform.Find("ParpadeoDanio");
            layer.SetSiblingIndex(flash != null ? flash.GetSiblingIndex() + 1 : 0);

            // Píldora de misión (arriba, entre la barra de energía y los contadores).
            Image pill = UiKit.Image("Mision", layer, UiKit.Rounded, UiKit.NavyGlass, 34f);
            missionPill = pill.rectTransform;
            UiKit.Place(missionPill, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(60f, -30f), new Vector2(520f, 68f));

            RectTransform bar = UiKit.Rect("Barra", missionPill);
            bar.anchorMin = new Vector2(0f, 0f); bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.offsetMin = new Vector2(70f, 7f); bar.offsetMax = new Vector2(-26f, 15f);
            Image barBack = UiKit.Image("Fondo", bar, UiKit.Rounded, new Color(1f, 1f, 1f, 0.12f), 4f);
            UiKit.Stretch(barBack.rectTransform, 0f);
            missionFillImage = UiKit.Image("Relleno", bar, UiKit.Rounded, UiKit.Teal, 4f);
            missionFill = missionFillImage.rectTransform;
            missionFill.anchorMin = Vector2.zero; missionFill.anchorMax = new Vector2(0.001f, 1f);
            missionFill.offsetMin = missionFill.offsetMax = Vector2.zero;

            Image ring = UiKit.Image("Aro", missionPill, UiKit.Circle, UiKit.Outline);
            UiKit.Place(ring.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(6f, 0f), new Vector2(80f, 80f));
            missionIconCircle = UiKit.Image("Circulo", ring.transform, UiKit.Circle, UiKit.Yellow);
            UiKit.Stretch(missionIconCircle.rectTransform, 5f);
            Image star = UiKit.Image("Icono", missionIconCircle.transform, UiKit.Icon(UiIcon.Star), UiKit.Navy);
            UiKit.Stretch(star.rectTransform, 13f);

            missionText = UiKit.Label("Texto", missionPill, "", 28, Color.white, TextAnchor.MiddleLeft, true, 3f);
            UiKit.Stretch(missionText.rectTransform, 0f);
            missionText.rectTransform.offsetMin = new Vector2(62f, 12f);
            missionText.rectTransform.offsetMax = new Vector2(-10f, 0f);
            missionText.horizontalOverflow = HorizontalWrapMode.Overflow;
            missionCount = UiKit.Label("Cuenta", missionPill, "", 28, UiKit.Yellow, TextAnchor.MiddleRight, true, 3f);
            UiKit.Stretch(missionCount.rectTransform, 0f);
            missionCount.rectTransform.offsetMin = new Vector2(62f, 12f);
            missionCount.rectTransform.offsetMax = new Vector2(-24f, 0f);

            // Combo (debajo de la misión).
            comboRoot = UiKit.Rect("Combo", layer);
            UiKit.Place(comboRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(60f, -112f), new Vector2(420f, 110f));
            comboText = UiKit.Label("Texto", comboRoot, "COMBO x2", 58, UiKit.Yellow, TextAnchor.UpperCenter, true, 4f);
            UiKit.Stretch(comboText.rectTransform, 0f);
            comboSub = UiKit.Label("Detalle", comboRoot, "", 24, Color.white, TextAnchor.LowerCenter, false, 3f);
            UiKit.Stretch(comboSub.rectTransform, 0f);
            comboSub.rectTransform.offsetMin = new Vector2(0f, 12f);
            RectTransform cbar = UiKit.Rect("Tiempo", comboRoot);
            cbar.anchorMin = new Vector2(0.5f, 0f); cbar.anchorMax = new Vector2(0.5f, 0f);
            cbar.pivot = new Vector2(0.5f, 0f);
            cbar.anchoredPosition = Vector2.zero; cbar.sizeDelta = new Vector2(220f, 8f);
            Image cback = UiKit.Image("Fondo", cbar, UiKit.Rounded, new Color(0f, 0.05f, 0.1f, 0.6f), 4f);
            UiKit.Stretch(cback.rectTransform, 0f);
            Image cfill = UiKit.Image("Relleno", cbar, UiKit.Rounded, UiKit.Yellow, 4f);
            comboFill = cfill.rectTransform;
            comboFill.anchorMin = Vector2.zero; comboFill.anchorMax = Vector2.one;
            comboFill.offsetMin = comboFill.offsetMax = Vector2.zero;
            comboRoot.gameObject.SetActive(false);

            // Números flotantes.
            for (int i = 0; i < 10; i++)
            {
                Text t = UiKit.Label("Flotante_" + i, layer, "", 34, Color.white, TextAnchor.MiddleCenter, true, 3f);
                UiKit.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 60f));
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.gameObject.SetActive(false);
                floaters.Add(new Floater { text = t });
            }
        }

        // ---------------------------------------------------------------- Números flotantes

        void Float(string message, Color color, int size)
        {
            if (floaters.Count == 0 || shark == null) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            Vector3 vp = cam.WorldToViewportPoint(shark.position + Vector3.up * 1.3f);
            Vector2 canvasSize = layer.rect.size;

            Floater f = floaters[nextFloater];
            nextFloater = (nextFloater + 1) % floaters.Count;
            f.age = 0f;
            f.life = 0.9f;
            f.color = color;
            f.start = new Vector2((vp.x - 0.5f) * canvasSize.x + Random.Range(-40f, 40f), (vp.y - 0.5f) * canvasSize.y);
            f.text.text = message;
            f.text.fontSize = size;
            f.text.color = color;
            f.text.rectTransform.anchoredPosition = f.start;
            f.text.gameObject.SetActive(true);
        }

        // ---------------------------------------------------------------- Animación

        void Update()
        {
            if (layer == null) return;
            float dt = Time.deltaTime;
            float udt = Time.unscaledDeltaTime;

            // Combo: se acaba si pasa el tiempo sin recoger nada.
            if (comboCount > 0)
            {
                comboTimer -= dt;
                if (comboTimer <= 0f) comboCount = 0;
            }
            bool showCombo = comboCount >= 3 && Playing;
            if (comboRoot.gameObject.activeSelf != showCombo) comboRoot.gameObject.SetActive(showCombo);
            if (showCombo)
            {
                int mult = Multiplier;
                comboText.text = mult > 1 ? "COMBO x" + mult : "COMBO";
                comboText.color = mult >= maxMultiplier ? UiKit.Orange : mult > 1 ? UiKit.Yellow : Color.white;
                int toNext = comboStep - comboCount % comboStep;
                comboSub.text = comboCount + " seguidos" + (mult < maxMultiplier ? "  ·  faltan " + toNext + " para x" + (mult + 1) : "");
                comboFill.anchorMax = new Vector2(Mathf.Max(0.001f, Mathf.Clamp01(comboTimer / comboWindow)), 1f);
                comboPunch = Mathf.MoveTowards(comboPunch, 0f, udt * 5f);
                comboRoot.localScale = Vector3.one * (1f + comboPunch * 0.18f);
            }

            // Misión cumplida: se muestra un momento y pasa a la siguiente.
            if (goalDoneTimer > 0f)
            {
                goalDoneTimer -= dt;
                if (goalDoneTimer <= 0f) SetGoal(goalIndex + 1);
            }
            missionPunch = Mathf.MoveTowards(missionPunch, 0f, udt * 2.5f);
            missionPill.localScale = Vector3.one * (1f + Mathf.Sin(missionPunch * Mathf.PI) * 0.12f);

            // Números flotantes: suben y se desvanecen.
            for (int i = 0; i < floaters.Count; i++)
            {
                Floater f = floaters[i];
                if (!f.text.gameObject.activeSelf) continue;
                f.age += dt;
                float k = f.age / f.life;
                if (k >= 1f) { f.text.gameObject.SetActive(false); continue; }
                f.text.rectTransform.anchoredPosition = f.start + Vector2.up * (110f * (1f - (1f - k) * (1f - k)));
                Color c = f.color;
                c.a = 1f - Mathf.Clamp01((k - 0.55f) / 0.45f);
                f.text.color = c;
                f.text.rectTransform.localScale = Vector3.one * (k < 0.15f ? Mathf.Lerp(0.6f, 1.15f, k / 0.15f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((k - 0.15f) / 0.2f)));
            }
        }
    }
}
