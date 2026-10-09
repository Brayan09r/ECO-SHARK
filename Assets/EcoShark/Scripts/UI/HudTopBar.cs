using UnityEngine;
using UnityEngine.UI;

namespace EcoShark
{
    /// <summary>
    /// Parte de arriba del HUD, estilo juego móvil:
    ///  - Barra de ENERGÍA grande con insignia de aleta, degradado verde → amarillo → rojo, brillo,
    ///    "barra fantasma" que muestra lo que acabas de perder, sacudida al recibir daño,
    ///    pulso y aviso "¡Come basura!" cuando queda poca.
    ///  - Contadores en píldoras con íconos: biomasa, rescates y tiempo.
    /// También cambia las fuentes del resto del HUD a las del juego. Oculta la barra y los contadores anteriores
    /// (HUDController sigue funcionando igual).
    /// Va en el objeto "HUD" (el Canvas). Menú del editor: ECO SHARK > 9. Menú principal y HUD.
    /// </summary>
    public class HudTopBar : MonoBehaviour
    {
        [SerializeField] float lowThreshold = 0.28f;
        [SerializeField] float ghostDelay = 0.45f;
        [SerializeField] float ghostSpeed = 0.6f;

        static readonly string[] OldElements = { "FondoEnergia", "EtiquetaEnergia", "BarraFondo", "FondoContadores", "Biomasa", "Rescates", "Tiempo" };

        static readonly Color High = new Color(0.25f, 0.93f, 0.55f);
        static readonly Color Mid = new Color(1f, 0.82f, 0.22f);
        static readonly Color Low = new Color(1f, 0.28f, 0.3f);

        SharkHealth shark;
        RectTransform container, fillRt, ghostRt, badge;
        Image fill, badgeCircle;
        Text percent, warning, biomassText, rescuedText, timeText;
        Vector2 containerPos;
        float shownFraction = 1f, ghostFraction = 1f, ghostTimer, shake, healGlow;
        int lastBiomass = -1, lastRescued = -1, lastSeconds = -1;

        void Start()
        {
            shark = FindFirstObjectByType<SharkHealth>(FindObjectsInactive.Include);
            foreach (string n in OldElements)
            {
                Transform t = transform.Find(n);
                if (t != null) t.gameObject.SetActive(false);
            }
            RestyleFonts();
            Build();
            if (shark != null)
            {
                shark.HealthChanged += OnHealthChanged;
                shark.Damaged += OnDamaged;
                shark.Healed += OnHealed;
                float f = shark.Max > 0f ? shark.Current / shark.Max : 1f;
                shownFraction = ghostFraction = f;
            }
        }

        void OnDestroy()
        {
            if (shark == null) return;
            shark.HealthChanged -= OnHealthChanged;
            shark.Damaged -= OnDamaged;
            shark.Healed -= OnHealed;
        }

        // ---------------------------------------------------------------- Construcción

        void Build()
        {
            RectTransform root = (RectTransform)transform;

            container = UiKit.Rect("BarraEnergia", root);
            containerPos = new Vector2(34f, -26f);
            UiKit.Place(container, new Vector2(0f, 1f), new Vector2(0f, 1f), containerPos, new Vector2(700f, 130f));

            // Barra (detrás de la insignia).
            Image border = UiKit.Image("Borde", container, UiKit.Rounded, UiKit.Outline, 28f);
            UiKit.Place(border.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(76f, -44f), new Vector2(600f, 60f));
            Image back = UiKit.Image("Fondo", border.transform, UiKit.Rounded, new Color(0.03f, 0.12f, 0.2f, 0.95f), 24f);
            UiKit.Stretch(back.rectTransform, 5f);

            RectTransform area = UiKit.Rect("Area", back.transform);
            UiKit.Stretch(area, 4f);
            area.offsetMin = new Vector2(46f, 4f); // deja sitio a la insignia

            Image ghost = UiKit.Image("Fantasma", area, UiKit.Rounded, new Color(1f, 0.95f, 0.75f, 0.85f), 18f);
            ghostRt = ghost.rectTransform;
            ghostRt.anchorMin = Vector2.zero; ghostRt.anchorMax = new Vector2(1f, 1f); ghostRt.offsetMin = ghostRt.offsetMax = Vector2.zero;

            fill = UiKit.Image("Relleno", area, UiKit.Rounded, High, 18f);
            fillRt = fill.rectTransform;
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = new Vector2(1f, 1f); fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            Image gloss = UiKit.Image("Brillo", fill.transform, UiKit.Shine, Color.white, 18f);
            UiKit.Stretch(gloss.rectTransform, 0f);

            percent = UiKit.Label("Porcentaje", back.transform, "100%", 30, Color.white, TextAnchor.MiddleRight, true, 3f);
            UiKit.Stretch(percent.rectTransform, 0f);
            percent.rectTransform.offsetMax = new Vector2(-18f, 0f);

            Text label = UiKit.Label("Etiqueta", container, "ENERGÍA", 30, Color.white, TextAnchor.UpperLeft, true, 3f);
            UiKit.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(136f, 4f), new Vector2(300f, 44f));

            warning = UiKit.Label("Aviso", container, "¡Energía baja! Come basura", 26, new Color(1f, 0.55f, 0.55f), TextAnchor.UpperLeft, true, 3f);
            UiKit.Place(warning.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(136f, -108f), new Vector2(520f, 40f));
            warning.gameObject.SetActive(false);

            // Insignia redonda con la aleta.
            Image ring = UiKit.Image("Insignia", container, UiKit.Circle, UiKit.Outline);
            UiKit.Place(ring.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -6f), new Vector2(136f, 136f));
            badge = ring.rectTransform;
            badgeCircle = UiKit.Image("Circulo", ring.transform, UiKit.Circle, UiKit.Teal);
            UiKit.Stretch(badgeCircle.rectTransform, 8f);
            Image shineC = UiKit.Image("Brillo", badgeCircle.transform, UiKit.Circle, new Color(1f, 1f, 1f, 0.2f));
            UiKit.Place(shineC.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(80f, 46f));
            Image fin = UiKit.Image("Aleta", badgeCircle.transform, UiKit.Icon(UiIcon.Fin), Color.white);
            UiKit.Stretch(fin.rectTransform, 24f);

            // Contadores (arriba a la derecha).
            biomassText = Counter(root, "Biomasa", 0, UiIcon.Leaf, UiKit.EcoGreen);
            rescuedText = Counter(root, "Rescates", 1, UiIcon.Heart, UiKit.Coral);
            timeText = Counter(root, "Tiempo", 2, UiIcon.Clock, UiKit.Yellow);

            // Detrás de los paneles (dato educativo, pausa, fin de partida): justo después del parpadeo de daño.
            Transform flash = transform.Find("ParpadeoDanio");
            int index = flash != null ? flash.GetSiblingIndex() + 1 : 0;
            container.SetSiblingIndex(index);
            biomassText.transform.parent.SetSiblingIndex(index + 1);
            rescuedText.transform.parent.SetSiblingIndex(index + 2);
            timeText.transform.parent.SetSiblingIndex(index + 3);
        }

        Text Counter(RectTransform root, string name, int row, UiIcon icon, Color color)
        {
            Image bg = UiKit.Image("Contador_" + name, root, UiKit.Rounded, UiKit.NavyGlass, 34f);
            UiKit.Place(bg.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -30f - row * 84f), new Vector2(300f, 68f));
            Image ring = UiKit.Image("Aro", bg.transform, UiKit.Circle, UiKit.Outline);
            UiKit.Place(ring.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(6f, 0f), new Vector2(80f, 80f));
            Image c = UiKit.Image("Circulo", ring.transform, UiKit.Circle, color);
            UiKit.Stretch(c.rectTransform, 5f);
            Image ic = UiKit.Image("Icono", c.transform, UiKit.Icon(icon), UiKit.Navy);
            UiKit.Stretch(ic.rectTransform, 15f);
            Text t = UiKit.Label("Valor", bg.transform, "0", 38, Color.white, TextAnchor.MiddleRight, true, 3f);
            UiKit.Stretch(t.rectTransform, 0f);
            t.rectTransform.offsetMax = new Vector2(-22f, 0f);
            t.rectTransform.offsetMin = new Vector2(56f, 0f);
            return t;
        }

        static readonly string[] RoundedPanels = { "Caja", "PanelDatoEducativo", "FondoControles", "BotonReiniciar", "Mejora_Speed", "Mejora_Magnet", "Mejora_Resistance" };

        /// <summary>El resto del HUD (datos educativos, pausa, fin de partida) con las fuentes del juego y esquinas redondeadas.</summary>
        void RestyleFonts()
        {
            foreach (Image img in GetComponentsInChildren<Image>(true))
            {
                if (System.Array.IndexOf(RoundedPanels, img.name) < 0 || img.sprite != null) continue;
                img.sprite = UiKit.Rounded;
                img.type = Image.Type.Sliced;
                UiKit.SetCorner(img, img.name == "Caja" ? 36f : 22f);
            }

            foreach (Text t in GetComponentsInChildren<Text>(true))
            {
                if (t.font == UiKit.TitleFont || t.font == UiKit.BodyFont) continue;
                bool big = t.fontSize >= 40 || t.name == "Titulo" || t.name == "TextoPausa" || t.transform.parent.name.StartsWith("Boton") || t.transform.parent.name.StartsWith("Mejora_");
                t.font = big ? UiKit.TitleFont : UiKit.BodyFont;
                if (t.GetComponent<UnityEngine.UI.Outline>() == null)
                {
                    var o = t.gameObject.AddComponent<UnityEngine.UI.Outline>();
                    o.effectColor = new Color(0f, 0.05f, 0.1f, 0.8f);
                    o.effectDistance = new Vector2(2f, -2f);
                }
            }
        }

        // ---------------------------------------------------------------- Eventos

        void OnHealthChanged(float current, float max) { }

        void OnDamaged(float amount)
        {
            shake = 1f;
            ghostTimer = ghostDelay;
        }

        void OnHealed(float amount) => healGlow = 1f;

        // ---------------------------------------------------------------- Animación

        void Update()
        {
            if (container == null) return;
            float dt = Time.unscaledDeltaTime;

            float target = shark != null && shark.Max > 0f ? Mathf.Clamp01(shark.Current / shark.Max) : 1f;
            shownFraction = Mathf.MoveTowards(shownFraction, target, dt * 1.5f);
            if (target > ghostFraction) ghostFraction = target;
            if (ghostTimer > 0f) ghostTimer -= dt;
            else ghostFraction = Mathf.MoveTowards(ghostFraction, shownFraction, dt * ghostSpeed);

            fillRt.anchorMax = new Vector2(Mathf.Max(0.001f, shownFraction), 1f);
            ghostRt.anchorMax = new Vector2(Mathf.Max(0.001f, ghostFraction), 1f);
            fill.enabled = shownFraction > 0.005f;

            Color c = shownFraction > 0.55f ? Color.Lerp(Mid, High, (shownFraction - 0.55f) / 0.45f)
                                             : Color.Lerp(Low, Mid, Mathf.Clamp01((shownFraction - lowThreshold) / (0.55f - lowThreshold)));
            bool low = target <= lowThreshold && target > 0f;
            if (low) c = Color.Lerp(c, Color.white, (Mathf.Sin(Time.unscaledTime * 10f) * 0.5f + 0.5f) * 0.35f);
            fill.color = c;
            percent.text = Mathf.CeilToInt(target * 100f) + "%";
            if (warning.gameObject.activeSelf != low) warning.gameObject.SetActive(low);
            if (low) warning.color = new Color(1f, 0.55f, 0.55f, 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)));

            // Insignia: late cuando queda poca energía, brilla verde al comer.
            float pulse = low ? 1f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)) * 0.1f : 1f;
            healGlow = Mathf.MoveTowards(healGlow, 0f, dt * 2.5f);
            badge.localScale = Vector3.one * (pulse + healGlow * 0.12f);
            badgeCircle.color = Color.Lerp(low ? Color.Lerp(UiKit.Teal, Low, 0.6f) : UiKit.Teal, UiKit.EcoGreen, healGlow);

            // Sacudida al recibir daño.
            shake = Mathf.MoveTowards(shake, 0f, dt * 3f);
            Vector2 offset = shake > 0f ? new Vector2(Mathf.Sin(Time.unscaledTime * 70f), Mathf.Cos(Time.unscaledTime * 55f)) * (10f * shake) : Vector2.zero;
            container.anchoredPosition = containerPos + offset;

            // Contadores.
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.RunBiomass != lastBiomass) { lastBiomass = gm.RunBiomass; biomassText.text = UiKit.Thousands(lastBiomass); }
            if (gm.AnimalsRescued != lastRescued) { lastRescued = gm.AnimalsRescued; rescuedText.text = lastRescued.ToString(); }
            int s = Mathf.FloorToInt(gm.RunTime);
            if (s != lastSeconds) { lastSeconds = s; timeText.text = UiKit.FormatTime(s); }
        }
    }
}
