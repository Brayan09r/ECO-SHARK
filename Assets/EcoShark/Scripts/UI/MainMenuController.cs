using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EcoShark
{
    /// <summary>
    /// Menú principal estilo juego móvil (inspirado en la "pecera" de los juegos de tiburones), con la temática de ECO SHARK:
    ///  - El tiburón 3D posa en el centro nadando en su sitio, con el océano vivo de fondo.
    ///  - Arriba: ajustes, sonido, biomasa, récord y el banner del guardián con su nivel (suma de mejoras).
    ///  - Centro: MEJORAS y ¡JUGAR!  ·  Lados: Misión ODS, Cómo jugar y el impacto acumulado.
    ///  - Abajo: logo ECO SHARK y las 3 mejoras con sus niveles.
    /// Toda la interfaz se construye por código (no necesita imágenes). Al pulsar JUGAR, la cámara se aleja y empieza la partida.
    /// Va en el objeto "MenuPrincipal". Menú del editor: ECO SHARK > 9. Menú principal y HUD.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class MainMenuController : MonoBehaviour
    {
        [Header("Tiburón en el menú")]
        [SerializeField] string sharkName = "TIBURÓN GUARDIÁN";
        [Tooltip("Posición de la cámara respecto al tiburón en el menú (más cerca que en el juego).")]
        [SerializeField] Vector3 menuCameraOffset = new Vector3(0f, -0.85f, -8.2f);
        [Tooltip("Grados que el tiburón gira hacia la cámara para lucirse.")]
        [SerializeField] float showcaseYaw = 26f;
        [SerializeField] float bobAmplitude = 0.16f;
        [Tooltip("Ritmo del coletazo en el menú (0–1).")]
        [Range(0f, 1f)]
        [SerializeField] float menuSwimSpeed = 0.45f;

        GameManager gm;
        CameraFollow follow;
        SharkMovement shark;
        Rigidbody rb;
        SharkSwimWave wave;

        Vector3 gameOffset;
        bool gameClamp;
        float gameSmooth;
        Vector3 sharkBase;
        bool inMenu, starting;

        Canvas canvas;
        CanvasGroup rootGroup;
        RectTransform root;

        Text walletText, recordText, trashText, rescuedText, levelText;
        Image[] levelSegments;
        GameObject upgradeBadge;
        Text upgradeBadgeText;
        Button soundButton;
        Image soundIcon;
        Text[] chipLevels = new Text[3];
        Image[][] chipPips = new Image[3][];

        GameObject openPanel;
        GameObject shopPanel, odsPanel, helpPanel, settingsPanel;
        Text shopWallet;
        Button[] shopBuy = new Button[3];
        Text[] shopBuyText = new Text[3];
        Image[][] shopPips = new Image[3][];
        Text resetText;
        bool resetArmed;

        static readonly UiIcon[] UpgradeIcons = { UiIcon.Bolt, UiIcon.Magnet, UiIcon.Shield };
        static readonly Color[] UpgradeColors = { new Color(1f, 0.78f, 0.15f), new Color(1f, 0.42f, 0.45f), new Color(0.2f, 0.8f, 0.9f) };
        static readonly Color Sdg14 = new Color(0.04f, 0.59f, 0.85f);   // color oficial ODS 14
        static readonly Color Sdg12 = new Color(0.75f, 0.55f, 0.18f);   // color oficial ODS 12

        // =====================================================================================

        void Start()
        {
            gm = GameManager.Instance;
            AddMenuButtonToGameOver();
            if (gm == null || gm.State != GameState.Menu)
            {
                enabled = false;
                return;
            }

            shark = FindFirstObjectByType<SharkMovement>();
            if (shark != null)
            {
                rb = shark.GetComponent<Rigidbody>();
                wave = shark.GetComponentInChildren<SharkSwimWave>();
                sharkBase = shark.transform.position;
            }
            if (wave != null) wave.ForcedSpeed01 = menuSwimSpeed;

            Camera cam = Camera.main;
            follow = cam != null ? cam.GetComponent<CameraFollow>() : null;
            if (follow != null)
            {
                gameOffset = follow.Offset;
                gameClamp = follow.ClampToOcean;
                gameSmooth = follow.SmoothTime;
                follow.Offset = menuCameraOffset;
                follow.ClampToOcean = false;
                follow.Snap();
            }

            BuildUI();
            RefreshAll();
            inMenu = true;
        }

        void Update()
        {
            if (!inMenu || starting) return;
            if (openPanel != null)
            {
                if (EcoInput.PausePressed()) ClosePanel();
                return;
            }
            if (EcoInput.ConfirmPressed()) Play();
        }

        void FixedUpdate()
        {
            if (!inMenu || rb == null) return;
            float t = Time.time;
            rb.MovePosition(sharkBase + Vector3.up * (Mathf.Sin(t * 1.3f) * bobAmplitude));
            float yaw = showcaseYaw + Mathf.Sin(t * 0.55f) * 6f;
            rb.MoveRotation(Quaternion.LookRotation(Quaternion.Euler(0f, yaw, 0f) * Vector3.right, Vector3.up));
        }

        // =====================================================================================
        //  Jugar
        // =====================================================================================

        public void Play()
        {
            if (starting || !inMenu) return;
            starting = true;
            StartCoroutine(PlayRoutine());
        }

        IEnumerator PlayRoutine()
        {
            ClosePanel();
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
            {
                rootGroup.alpha = 1f - t;
                root.localScale = Vector3.one * (1f + t * 0.06f);
                yield return null;
            }
            rootGroup.alpha = 0f;
            canvas.enabled = false;
            inMenu = false;

            if (rb != null)
            {
                rb.position = sharkBase;
                rb.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
            }
            if (wave != null) wave.ForcedSpeed01 = -1f;
            if (follow != null)
            {
                follow.SmoothTime = 0.9f;
                follow.Offset = gameOffset;
                follow.ClampToOcean = gameClamp;
            }
            if (gm != null) gm.StartRun();

            yield return new WaitForSeconds(1.4f);
            if (follow != null) follow.SmoothTime = gameSmooth;
            gameObject.SetActive(false);
        }

        // =====================================================================================
        //  Construcción de la interfaz
        // =====================================================================================

        void BuildUI()
        {
            var go = new GameObject("MenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            rootGroup = go.GetComponent<CanvasGroup>();
            root = (RectTransform)go.transform;

            RectTransform layer = UiKit.Rect("Menu", root);
            UiKit.Stretch(layer, 0f);

            // Viñetas para que el texto se lea sobre el agua clara.
            Image top = UiKit.Image("VinetaArriba", layer, UiKit.GradientV, new Color(0.01f, 0.06f, 0.12f, 0.6f));
            top.rectTransform.anchorMin = new Vector2(0f, 1f); top.rectTransform.anchorMax = new Vector2(1f, 1f);
            top.rectTransform.pivot = new Vector2(0.5f, 1f); top.rectTransform.sizeDelta = new Vector2(0f, 250f);
            top.rectTransform.localScale = new Vector3(1f, -1f, 1f);
            top.rectTransform.anchoredPosition = new Vector2(0f, -250f);
            Image bottom = UiKit.Image("VinetaAbajo", layer, UiKit.GradientV, new Color(0.01f, 0.06f, 0.12f, 0.7f));
            bottom.rectTransform.anchorMin = Vector2.zero; bottom.rectTransform.anchorMax = new Vector2(1f, 0f);
            bottom.rectTransform.pivot = new Vector2(0.5f, 0f); bottom.rectTransform.sizeDelta = new Vector2(0f, 340f);

            BuildTopLeft(layer);
            BuildTopRight(layer);
            BuildBanner(layer);
            BuildCenterButtons(layer);
            BuildSideTiles(layer);
            BuildLogo(layer);
            BuildUpgradeStrip(layer);

            Text hint = UiKit.Label("Ayuda", layer, "ENTER: jugar   ·   M: sonido", 24, new Color(1f, 1f, 1f, 0.75f), TextAnchor.LowerRight, false, 2f);
            UiKit.Place(hint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 26f), new Vector2(520f, 40f));

            // Capa de ventanas (encima de todo).
            RectTransform panels = UiKit.Rect("Ventanas", root);
            UiKit.Stretch(panels, 0f);
            shopPanel = BuildShopPanel(panels);
            odsPanel = BuildOdsPanel(panels);
            helpPanel = BuildHelpPanel(panels);
            settingsPanel = BuildSettingsPanel(panels);
        }

        void BuildTopLeft(RectTransform layer)
        {
            Button settings = UiKit.Button("Ajustes", layer, "", UiKit.Teal, new Vector2(104f, 104f), 30, UiIcon.Gear, 50f);
            UiKit.Place((RectTransform)settings.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -30f), new Vector2(104f, 104f));
            settings.onClick.AddListener(() => OpenPanel(settingsPanel));

            soundButton = UiKit.Button("Sonido", layer, "", UiKit.Teal, new Vector2(104f, 104f), 30, UiIcon.SoundOn, 50f);
            UiKit.Place((RectTransform)soundButton.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(156f, -30f), new Vector2(104f, 104f));
            soundIcon = soundButton.transform.Find("Cara/Icono").GetComponent<Image>();
            soundButton.onClick.AddListener(ToggleSound);
        }

        void BuildTopRight(RectTransform layer)
        {
            // Billetera de biomasa.
            RectTransform wallet = Pill(layer, "Biomasa", new Vector2(-40f, -38f), new Vector2(390f, 86f), UiIcon.Leaf, UiKit.EcoGreen, out walletText, 46);
            Text caption = UiKit.Label("Titulo", wallet, "BIOMASA", 20, UiKit.EcoGreen, TextAnchor.UpperLeft, false, 2f);
            UiKit.Place(caption.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(84f, 16f), new Vector2(220f, 30f));

            // Récord.
            Pill(layer, "Record", new Vector2(-40f, -140f), new Vector2(330f, 66f), UiIcon.Trophy, UiKit.Yellow, out recordText, 30);
        }

        RectTransform Pill(RectTransform parent, string name, Vector2 pos, Vector2 size, UiIcon icon, Color iconColor, out Text value, int fontSize)
        {
            Image bg = UiKit.Image(name, parent, UiKit.Rounded, UiKit.NavyGlass, size.y * 0.5f);
            UiKit.Place(bg.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), pos, size);
            Image ring = UiKit.Image("Aro", bg.transform, UiKit.Circle, UiKit.Outline);
            UiKit.Place(ring.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(8f, 0f), new Vector2(size.y + 16f, size.y + 16f));
            Image circle = UiKit.Image("Circulo", ring.transform, UiKit.Circle, iconColor);
            UiKit.Stretch(circle.rectTransform, 6f);
            Image ic = UiKit.Image("Icono", circle.transform, UiKit.Icon(icon), UiKit.Navy);
            UiKit.Stretch(ic.rectTransform, size.y * 0.22f);
            value = UiKit.Label("Valor", bg.transform, "0", fontSize, Color.white, TextAnchor.MiddleRight, true, 3f);
            UiKit.Stretch(value.rectTransform, 0f);
            value.rectTransform.offsetMax = new Vector2(-26f, 0f);
            value.rectTransform.offsetMin = new Vector2(size.y * 0.7f, 0f);
            return bg.rectTransform;
        }

        void BuildBanner(RectTransform layer)
        {
            Image border = UiKit.Image("BannerGuardian", layer, UiKit.Rounded, UiKit.Outline, 30f);
            UiKit.Place(border.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(840f, 124f));
            Image bg = UiKit.Image("Fondo", border.transform, UiKit.Rounded, new Color(0.05f, 0.22f, 0.34f, 0.95f), 26f);
            UiKit.Stretch(bg.rectTransform, 5f);
            Image gloss = UiKit.Image("Brillo", bg.transform, UiKit.Shine, new Color(1f, 1f, 1f, 0.35f), 26f);
            UiKit.Stretch(gloss.rectTransform, 0f);

            // Insignia amarilla (rombo) con la aleta.
            Image diamondBorder = UiKit.Image("Insignia", border.transform, UiKit.Rounded, UiKit.Outline, 26f);
            UiKit.Place(diamondBorder.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), new Vector2(118f, 118f));
            diamondBorder.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
            Image diamond = UiKit.Image("Rombo", diamondBorder.transform, UiKit.Rounded, UiKit.Yellow, 22f);
            UiKit.Stretch(diamond.rectTransform, 6f);
            Image fin = UiKit.Image("Aleta", diamond.transform, UiKit.Icon(UiIcon.Fin), UiKit.Navy);
            UiKit.Stretch(fin.rectTransform, 18f);
            fin.rectTransform.localEulerAngles = new Vector3(0f, 0f, -45f);

            // Círculo derecho con la hoja (lo ecológico).
            Image ring = UiKit.Image("Eco", border.transform, UiKit.Circle, UiKit.Outline);
            UiKit.Place(ring.rectTransform, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-6f, 0f), new Vector2(112f, 112f));
            Image circle = UiKit.Image("Circulo", ring.transform, UiKit.Circle, UiKit.Teal);
            UiKit.Stretch(circle.rectTransform, 6f);
            Image leaf = UiKit.Image("Hoja", circle.transform, UiKit.Icon(UiIcon.Leaf), Color.white);
            UiKit.Stretch(leaf.rectTransform, 22f);

            Text name = UiKit.Label("Nombre", bg.transform, sharkName, 40, Color.white, TextAnchor.UpperLeft, true, 3f);
            UiKit.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(78f, -8f), new Vector2(520f, 54f));
            levelText = UiKit.Label("Nivel", bg.transform, "NVL 1", 34, UiKit.Yellow, TextAnchor.UpperRight, true, 3f);
            UiKit.Place(levelText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-72f, -10f), new Vector2(200f, 50f));

            int segments = Progression.MaxGuardianLevel;
            levelSegments = new Image[segments];
            float width = 620f, gap = 6f;
            float seg = (width - gap * (segments - 1)) / segments;
            for (int i = 0; i < segments; i++)
            {
                Image s = UiKit.Image("Segmento" + i, bg.transform, UiKit.Rounded, new Color(0.02f, 0.1f, 0.16f), 6f);
                UiKit.Place(s.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(78f + i * (seg + gap), 18f), new Vector2(seg, 24f));
                levelSegments[i] = s;
            }
        }

        void BuildCenterButtons(RectTransform layer)
        {
            Button upgrades = UiKit.Button("Mejoras", layer, "MEJORAS", new Color(0.13f, 0.55f, 0.92f), new Vector2(340f, 120f), 46, UiIcon.ArrowUp, 34f);
            UiKit.Place((RectTransform)upgrades.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-205f, -258f), new Vector2(340f, 120f));
            upgrades.onClick.AddListener(() => OpenPanel(shopPanel));

            // Globo rojo con la cantidad de mejoras que ya puedes comprar.
            Image badgeRing = UiKit.Image("Aviso", upgrades.transform, UiKit.Circle, UiKit.Outline);
            UiKit.Place(badgeRing.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(10f, -8f), new Vector2(62f, 62f));
            Image badge = UiKit.Image("Circulo", badgeRing.transform, UiKit.Circle, new Color(0.95f, 0.25f, 0.2f));
            UiKit.Stretch(badge.rectTransform, 5f);
            upgradeBadgeText = UiKit.Label("Numero", badge.transform, "1", 30, Color.white, TextAnchor.MiddleCenter, true, 2f);
            UiKit.Stretch(upgradeBadgeText.rectTransform, 0f);
            upgradeBadge = badgeRing.gameObject;

            Button play = UiKit.Button("Jugar", layer, "¡JUGAR!", UiKit.Orange, new Vector2(380f, 136f), 60, UiIcon.Play, 36f);
            UiKit.Place((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(215f, -258f), new Vector2(380f, 136f));
            play.GetComponent<UiBounce>().idlePulse = 0.035f;
            play.onClick.AddListener(Play);
        }

        void BuildSideTiles(RectTransform layer)
        {
            Tile(layer, "MisionODS", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(50f, 130f), UiIcon.Wave, Sdg14, "MISIÓN ODS", "14 · 12", out _)
                .onClick.AddListener(() => OpenPanel(odsPanel));
            Tile(layer, "ComoJugar", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(50f, -110f), UiIcon.Question, UiKit.Orange, "CÓMO JUGAR", "", out _)
                .onClick.AddListener(() => OpenPanel(helpPanel));

            Tile(layer, "BasuraRetirada", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-50f, 130f), UiIcon.Bottle, new Color(0.35f, 0.8f, 0.95f), "BASURA", "retirada", out trashText)
                .onClick.AddListener(() => OpenPanel(odsPanel));
            Tile(layer, "AnimalesSalvados", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-50f, -110f), UiIcon.Heart, UiKit.Coral, "ANIMALES", "salvados", out rescuedText)
                .onClick.AddListener(() => OpenPanel(odsPanel));
        }

        /// <summary>Botón circular con ícono y leyenda debajo (como los accesos laterales de los juegos móviles).</summary>
        Button Tile(RectTransform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, UiIcon icon, Color color,
                    string caption, string sub, out Text counter)
        {
            RectTransform r = UiKit.Rect(name, parent);
            UiKit.Place(r, anchor, pivot, pos, new Vector2(190f, 210f));
            Image hit = r.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);

            Image ring = UiKit.Image("Aro", r, UiKit.Circle, UiKit.Outline);
            UiKit.Place(ring.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(130f, 130f));
            Image circle = UiKit.Image("Circulo", ring.transform, UiKit.Circle, color);
            UiKit.Stretch(circle.rectTransform, 7f);
            Image gloss = UiKit.Image("Brillo", circle.transform, UiKit.Circle, new Color(1f, 1f, 1f, 0.18f));
            UiKit.Place(gloss.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(84f, 50f));
            Image ic = UiKit.Image("Icono", circle.transform, UiKit.Icon(icon), Color.white);
            UiKit.Stretch(ic.rectTransform, 26f);

            counter = null;
            if (name == "BasuraRetirada" || name == "AnimalesSalvados")
            {
                Image numBg = UiKit.Image("Contador", ring.transform, UiKit.Rounded, UiKit.Navy, 18f);
                UiKit.Place(numBg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(120f, 40f));
                counter = UiKit.Label("Numero", numBg.transform, "0", 28, Color.white, TextAnchor.MiddleCenter, true, 2f);
                UiKit.Stretch(counter.rectTransform, 0f);
            }

            Text cap = UiKit.Label("Leyenda", r, caption, 28, Color.white, TextAnchor.UpperCenter, true, 3f);
            UiKit.Place(cap.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(220f, 36f));
            if (!string.IsNullOrEmpty(sub))
            {
                Text s = UiKit.Label("Detalle", r, sub, 22, new Color(0.75f, 0.95f, 1f), TextAnchor.UpperCenter, false, 2f);
                UiKit.Place(s.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -174f), new Vector2(220f, 30f));
            }

            Button b = r.gameObject.AddComponent<Button>();
            b.targetGraphic = circle;
            r.gameObject.AddComponent<UiBounce>();
            return b;
        }

        void BuildLogo(RectTransform layer)
        {
            RectTransform logo = UiKit.Rect("Logo", layer);
            UiKit.Place(logo, Vector2.zero, Vector2.zero, new Vector2(46f, 34f), new Vector2(560f, 250f));
            logo.localEulerAngles = new Vector3(0f, 0f, 3f);

            Text eco = UiKit.Label("ECO", logo, "ECO", 96, UiKit.EcoGreen, TextAnchor.UpperLeft, true, 5f);
            UiKit.Place(eco.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, 10f), new Vector2(400f, 110f));
            Text shark = UiKit.Label("SHARK", logo, "SHARK", 150, Color.white, TextAnchor.UpperLeft, true, 6f);
            UiKit.Place(shark.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -62f), new Vector2(560f, 160f));
            shark.GetComponent<UnityEngine.UI.Outline>().effectColor = new Color(0.03f, 0.25f, 0.45f);

            Image ribbonBorder = UiKit.Image("Cinta", logo, UiKit.Rounded, UiKit.Outline, 14f);
            UiKit.Place(ribbonBorder.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10f, -6f), new Vector2(430f, 52f));
            Image ribbon = UiKit.Image("Fondo", ribbonBorder.transform, UiKit.Rounded, UiKit.Teal, 12f);
            UiKit.Stretch(ribbon.rectTransform, 4f);
            Text sub = UiKit.Label("Texto", ribbon.transform, "GUARDIÁN DEL OCÉANO", 28, Color.white, TextAnchor.MiddleCenter, true, 2f);
            UiKit.Stretch(sub.rectTransform, 0f);
        }

        void BuildUpgradeStrip(RectTransform layer)
        {
            Image strip = UiKit.Image("TiraMejoras", layer, UiKit.Rounded, UiKit.NavyGlass, 22f);
            UiKit.Place(strip.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(70f, 30f), new Vector2(720f, 112f));
            for (int i = 0; i < 3; i++)
            {
                UpgradeType type = (UpgradeType)i;
                RectTransform chip = UiKit.Rect("Mejora_" + type, strip.transform);
                UiKit.Place(chip, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f + i * 236f, 0f), new Vector2(222f, 92f));
                Image chipBg = chip.gameObject.AddComponent<Image>();
                chipBg.sprite = UiKit.Rounded; chipBg.type = Image.Type.Sliced; UiKit.SetCorner(chipBg, 18f);
                chipBg.color = new Color(0.07f, 0.26f, 0.38f);

                Image circle = UiKit.Image("Circulo", chip, UiKit.Circle, UpgradeColors[i]);
                UiKit.Place(circle.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(70f, 70f));
                Image ic = UiKit.Image("Icono", circle.transform, UiKit.Icon(UpgradeIcons[i]), UiKit.Navy);
                UiKit.Stretch(ic.rectTransform, 14f);

                Text nm = UiKit.Label("Nombre", chip, ShortName(type), 24, Color.white, TextAnchor.UpperLeft, true, 2f);
                UiKit.Place(nm.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(90f, -8f), new Vector2(130f, 32f));
                chipLevels[i] = UiKit.Label("Nivel", chip, "Nv 0/5", 20, UiKit.Yellow, TextAnchor.UpperLeft, false, 2f);
                UiKit.Place(chipLevels[i].rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(90f, -38f), new Vector2(130f, 28f));
                chipPips[i] = Pips(chip, new Vector2(90f, 14f), 20f, 10f, 4f);

                Button b = chip.gameObject.AddComponent<Button>();
                b.targetGraphic = chipBg;
                chip.gameObject.AddComponent<UiBounce>();
                b.onClick.AddListener(() => OpenPanel(shopPanel));
            }
        }

        Image[] Pips(RectTransform parent, Vector2 bottomLeft, float w, float h, float gap)
        {
            var pips = new Image[Progression.MaxLevel];
            for (int k = 0; k < pips.Length; k++)
            {
                Image p = UiKit.Image("Punto" + k, parent, UiKit.Rounded, new Color(0.02f, 0.08f, 0.14f), h * 0.5f);
                UiKit.Place(p.rectTransform, Vector2.zero, Vector2.zero, bottomLeft + new Vector2(k * (w + gap), 0f), new Vector2(w, h));
                pips[k] = p;
            }
            return pips;
        }

        static string ShortName(UpgradeType t)
        {
            switch (t)
            {
                case UpgradeType.Speed: return "Velocidad";
                case UpgradeType.Magnet: return "Imán";
                default: return "Resistencia";
            }
        }

        // =====================================================================================
        //  Ventanas
        // =====================================================================================

        RectTransform Window(RectTransform parent, string name, string title, Vector2 size, Color headerColor, out GameObject panel)
        {
            Image dim = UiKit.Image(name, parent, null, new Color(0.01f, 0.04f, 0.08f, 0.72f));
            UiKit.Stretch(dim.rectTransform, 0f);
            dim.raycastTarget = true;
            panel = dim.gameObject;

            Image border = UiKit.Image("Tarjeta", dim.transform, UiKit.Rounded, UiKit.Outline, 38f);
            UiKit.Place(border.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), size);
            border.raycastTarget = true;
            Image card = UiKit.Image("Fondo", border.transform, UiKit.Rounded, new Color(0.06f, 0.2f, 0.3f), 34f);
            UiKit.Stretch(card.rectTransform, 6f);

            Image header = UiKit.Image("Cabecera", card.transform, UiKit.Rounded, headerColor, 30f);
            header.rectTransform.anchorMin = new Vector2(0f, 1f); header.rectTransform.anchorMax = new Vector2(1f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f); header.rectTransform.sizeDelta = new Vector2(0f, 104f);
            header.rectTransform.anchoredPosition = Vector2.zero;
            Image hg = UiKit.Image("Brillo", header.transform, UiKit.Shine, Color.white, 30f);
            UiKit.Stretch(hg.rectTransform, 0f);
            Text t = UiKit.Label("Titulo", header.transform, title, 54, Color.white, TextAnchor.MiddleCenter, true, 4f);
            UiKit.Stretch(t.rectTransform, 0f);

            Button close = UiKit.Button("Cerrar", border.transform, "", UiKit.Coral, new Vector2(90f, 90f), 30, UiIcon.Close, 45f);
            UiKit.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-18f, -18f), new Vector2(90f, 90f));
            close.onClick.AddListener(ClosePanel);

            RectTransform content = UiKit.Rect("Contenido", card.transform);
            UiKit.Stretch(content, 0f);
            content.offsetMax = new Vector2(-30f, -120f);
            content.offsetMin = new Vector2(30f, 26f);

            panel.SetActive(false);
            return content;
        }

        GameObject BuildShopPanel(RectTransform parent)
        {
            RectTransform c = Window(parent, "VentanaMejoras", "MEJORAS DEL GUARDIÁN", new Vector2(1240f, 720f), new Color(0.13f, 0.55f, 0.92f), out GameObject panel);

            RectTransform wallet = UiKit.Rect("Billetera", c);
            UiKit.Place(wallet, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(700f, 60f));
            Image leaf = UiKit.Image("Hoja", wallet, UiKit.Icon(UiIcon.Leaf), UiKit.EcoGreen);
            UiKit.Place(leaf.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(150f, 0f), new Vector2(50f, 50f));
            shopWallet = UiKit.Label("Texto", wallet, "", 36, Color.white, TextAnchor.MiddleLeft, true, 3f);
            UiKit.Place(shopWallet.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(210f, 0f), new Vector2(480f, 60f));

            for (int i = 0; i < 3; i++)
            {
                UpgradeType type = (UpgradeType)i;
                Image cardBg = UiKit.Image("Carta_" + type, c, UiKit.Rounded, new Color(0.04f, 0.14f, 0.22f), 26f);
                UiKit.Place(cardBg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-390f + i * 390f, 0f), new Vector2(360f, 480f));

                Image ring = UiKit.Image("Aro", cardBg.transform, UiKit.Circle, UiKit.Outline);
                UiKit.Place(ring.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(132f, 132f));
                Image circle = UiKit.Image("Circulo", ring.transform, UiKit.Circle, UpgradeColors[i]);
                UiKit.Stretch(circle.rectTransform, 7f);
                Image ic = UiKit.Image("Icono", circle.transform, UiKit.Icon(UpgradeIcons[i]), UiKit.Navy);
                UiKit.Stretch(ic.rectTransform, 26f);

                Text nm = UiKit.Label("Nombre", cardBg.transform, Progression.DisplayName(type), 34, Color.white, TextAnchor.UpperCenter, true, 3f);
                UiKit.Place(nm.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(340f, 46f));
                Text desc = UiKit.Label("Descripcion", cardBg.transform, Progression.Description(type), 24, new Color(0.8f, 0.93f, 1f), TextAnchor.UpperCenter, false, 0f);
                UiKit.Place(desc.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -212f), new Vector2(310f, 70f));

                shopPips[i] = Pips((RectTransform)cardBg.transform, new Vector2(55f, 138f), 44f, 18f, 8f);

                Button buy = UiKit.Button("Comprar", cardBg.transform, "MEJORAR", new Color(0.3f, 0.78f, 0.35f), new Vector2(300f, 96f), 34, null, 26f);
                UiKit.Place((RectTransform)buy.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(300f, 96f));
                shopBuyText[i] = buy.transform.Find("Cara/Texto").GetComponent<Text>();
                shopBuy[i] = buy;
                buy.onClick.AddListener(() => { if (Progression.TryBuy(type)) RefreshAll(); });
            }
            return panel;
        }

        GameObject BuildOdsPanel(RectTransform parent)
        {
            RectTransform c = Window(parent, "VentanaODS", "MISIÓN: SALVAR EL OCÉANO", new Vector2(1500f, 800f), Sdg14, out GameObject panel);
            OdsColumn(c, new Vector2(-365f, 0f), "14", Sdg14, "Vida submarina",
                "Conservar y usar de forma sostenible los océanos, los mares y los recursos marinos.",
                "• <b>Meta 14.1:</b> prevenir y reducir la contaminación marina, sobre todo la basura y los plásticos.\n" +
                "• <b>Meta 14.4:</b> poner fin a la pesca ilegal y a las prácticas de pesca destructivas.\n" +
                "<color=#8FF08A><b>En el juego:</b></color> recoges basura, esquivas el petróleo y los barriles, y liberas animales de las redes de los barcos ilegales.");
            OdsColumn(c, new Vector2(365f, 0f), "12", Sdg12, "Producción y consumo responsables",
                "Garantizar formas de consumo y producción sostenibles.",
                "• <b>Meta 12.5:</b> reducir los desechos mediante la prevención, la reducción, el reciclaje y la reutilización.\n" +
                "• Cada basura que comes muestra un dato: cuánto tarda en degradarse y por qué importa reciclar.\n" +
                "<color=#8FF08A><b>En el juego:</b></color> la basura se convierte en biomasa: bien gestionados, los residuos se vuelven recursos.");
            return panel;
        }

        void OdsColumn(RectTransform parent, Vector2 pos, string number, Color color, string title, string goal, string body)
        {
            Image bg = UiKit.Image("ODS" + number, parent, UiKit.Rounded, new Color(0.04f, 0.14f, 0.22f), 26f);
            UiKit.Place(bg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(700f, 640f));

            Image badge = UiKit.Image("Numero", bg.transform, UiKit.Rounded, color, 20f);
            UiKit.Place(badge.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(130f, 130f));
            Text ods = UiKit.Label("ODS", badge.transform, "ODS", 28, Color.white, TextAnchor.UpperCenter, true, 2f);
            UiKit.Place(ods.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(130f, 36f));
            Text num = UiKit.Label("N", badge.transform, number, 64, Color.white, TextAnchor.LowerCenter, true, 3f);
            UiKit.Place(num.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(130f, 80f));

            Text t = UiKit.Label("Titulo", bg.transform, title, 38, Color.white, TextAnchor.UpperLeft, true, 3f);
            UiKit.Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(172f, -28f), new Vector2(500f, 96f));
            Text g = UiKit.Label("Objetivo", bg.transform, "<i>" + goal + "</i>", 25, new Color(0.8f, 0.93f, 1f), TextAnchor.UpperLeft, false, 0f);
            UiKit.Place(g.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -172f), new Vector2(644f, 90f));
            Text b = UiKit.Label("Texto", bg.transform, body, 26, Color.white, TextAnchor.UpperLeft, false, 0f);
            UiKit.Place(b.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -268f), new Vector2(644f, 360f));
            b.lineSpacing = 1.05f;
        }

        GameObject BuildHelpPanel(RectTransform parent)
        {
            RectTransform c = Window(parent, "VentanaAyuda", "CÓMO JUGAR", new Vector2(1220f, 800f), UiKit.Orange, out GameObject panel);
            var rows = new (UiIcon icon, Color color, string text)[]
            {
                (UiIcon.Fin, UiKit.Teal, "<b>Nadar:</b> WASD o flechas, o mantén presionado el clic (o el dedo) hacia donde quieras ir."),
                (UiIcon.Bolt, UpgradeColors[0], "<b>Impulso:</b> Shift o Espacio mientras nadas."),
                (UiIcon.Bottle, new Color(0.35f, 0.8f, 0.95f), "<b>Come basura:</b> recuperas energía y ganas biomasa. La toxicidad te la va quitando."),
                (UiIcon.Heart, UiKit.Coral, "<b>Rescata:</b> quédate junto a un animal atrapado en una red hasta liberarlo."),
                (UiIcon.Shield, new Color(0.62f, 0.4f, 0.95f), "<b>Cuidado:</b> el petróleo, los barriles radiactivos y los barcos de pesca ilegal te dañan."),
                (UiIcon.ArrowUp, new Color(0.13f, 0.55f, 0.92f), "<b>Mejora:</b> gasta tu biomasa en velocidad, imán y resistencia."),
                (UiIcon.SoundOn, UiKit.EcoGreen, "<b>Teclas:</b> P = pausa  ·  M = silenciar  ·  Enter = jugar"),
            };
            for (int i = 0; i < rows.Length; i++)
            {
                RectTransform row = UiKit.Rect("Fila" + i, c);
                UiKit.Place(row, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -8f - i * 90f), new Vector2(1130f, 80f));
                Image circle = UiKit.Image("Circulo", row, UiKit.Circle, rows[i].color);
                UiKit.Place(circle.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(70f, 70f));
                Image ic = UiKit.Image("Icono", circle.transform, UiKit.Icon(rows[i].icon), UiKit.Navy);
                UiKit.Stretch(ic.rectTransform, 15f);
                Text t = UiKit.Label("Texto", row, rows[i].text, 28, Color.white, TextAnchor.MiddleLeft, false, 0f);
                UiKit.Place(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(92f, 0f), new Vector2(1030f, 80f));
            }
            return panel;
        }

        GameObject BuildSettingsPanel(RectTransform parent)
        {
            RectTransform c = Window(parent, "VentanaAjustes", "AJUSTES", new Vector2(900f, 620f), UiKit.Teal, out GameObject panel);

            Button sound = UiKit.Button("SonidoOnOff", c, "SONIDO", UiKit.Teal, new Vector2(520f, 100f), 38, UiIcon.SoundOn, 28f);
            UiKit.Place((RectTransform)sound.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(520f, 100f));
            sound.onClick.AddListener(ToggleSound);

            Button full = UiKit.Button("PantallaCompleta", c, "PANTALLA COMPLETA", new Color(0.13f, 0.55f, 0.92f), new Vector2(520f, 100f), 34, null, 28f);
            UiKit.Place((RectTransform)full.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(520f, 100f));
            full.onClick.AddListener(() => Screen.fullScreen = !Screen.fullScreen);

            Button reset = UiKit.Button("BorrarProgreso", c, "BORRAR PROGRESO", UiKit.Coral, new Vector2(520f, 100f), 34, null, 28f);
            UiKit.Place((RectTransform)reset.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -260f), new Vector2(520f, 100f));
            resetText = reset.transform.Find("Cara/Texto").GetComponent<Text>();
            reset.onClick.AddListener(() =>
            {
                if (!resetArmed)
                {
                    resetArmed = true;
                    resetText.text = "¿SEGURO? TOCA OTRA VEZ";
                    return;
                }
                Progression.ResetAll();
                resetArmed = false;
                resetText.text = "PROGRESO BORRADO";
                RefreshAll();
            });

            Text credits = UiKit.Label("Creditos", c, "Fuentes: Lilita One y Baloo 2 (SIL OFL)  ·  Audios: ver CREDITOS_AUDIO.md", 20,
                                       new Color(1f, 1f, 1f, 0.6f), TextAnchor.LowerCenter, false, 0f);
            UiKit.Place(credits.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(820f, 36f));
            return panel;
        }

        void OpenPanel(GameObject panel)
        {
            if (panel == null) return;
            ClosePanel();
            RefreshAll();
            openPanel = panel;
            panel.SetActive(true);
            StartCoroutine(Pop(panel.transform.Find("Tarjeta")));
        }

        void ClosePanel()
        {
            if (openPanel != null) openPanel.SetActive(false);
            openPanel = null;
            if (resetArmed && resetText != null) { resetArmed = false; resetText.text = "BORRAR PROGRESO"; }
        }

        static IEnumerator Pop(Transform t)
        {
            if (t == null) yield break;
            for (float k = 0f; k < 1f; k += Time.unscaledDeltaTime / 0.22f)
            {
                float s = 1f - Mathf.Pow(1f - k, 3f);
                t.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, s + Mathf.Sin(k * Mathf.PI) * 0.06f);
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        // =====================================================================================
        //  Datos
        // =====================================================================================

        void RefreshAll()
        {
            if (walletText != null) walletText.text = UiKit.Thousands(Progression.Biomass);
            if (recordText != null) recordText.text = "Récord " + UiKit.FormatTime(Progression.BestTime);
            if (trashText != null) trashText.text = UiKit.Thousands(Progression.TotalTrash);
            if (rescuedText != null) rescuedText.text = UiKit.Thousands(Progression.TotalRescued);

            int level = Progression.GuardianLevel;
            if (levelText != null) levelText.text = "NVL " + (level + 1);
            if (levelSegments != null)
                for (int i = 0; i < levelSegments.Length; i++)
                    levelSegments[i].color = i < level ? UiKit.Yellow : new Color(0.02f, 0.1f, 0.16f);

            int affordable = 0;
            for (int i = 0; i < 3; i++)
            {
                UpgradeType type = (UpgradeType)i;
                int lv = Progression.GetLevel(type);
                if (Progression.CanBuy(type)) affordable++;
                if (chipLevels[i] != null) chipLevels[i].text = "Nv " + lv + "/" + Progression.MaxLevel;
                SetPips(chipPips[i], lv, UpgradeColors[i]);
                SetPips(shopPips[i], lv, UpgradeColors[i]);
                if (shopBuy[i] != null)
                {
                    bool maxed = Progression.IsMaxed(type);
                    shopBuy[i].interactable = Progression.CanBuy(type);
                    shopBuyText[i].text = maxed ? "MÁXIMO" : "MEJORAR · " + Progression.GetCost(type);
                }
            }
            if (shopWallet != null) shopWallet.text = "Tienes " + UiKit.Thousands(Progression.Biomass) + " de biomasa";
            if (upgradeBadge != null)
            {
                upgradeBadge.SetActive(affordable > 0);
                upgradeBadgeText.text = affordable.ToString();
            }
            UpdateSoundIcon();
        }

        static void SetPips(Image[] pips, int level, Color on)
        {
            if (pips == null) return;
            for (int k = 0; k < pips.Length; k++)
                if (pips[k] != null) pips[k].color = k < level ? on : new Color(0.02f, 0.08f, 0.14f);
        }

        void ToggleSound()
        {
            AudioListener.volume = AudioListener.volume > 0f ? 0f : 1f;
            UpdateSoundIcon();
        }

        void UpdateSoundIcon()
        {
            if (soundIcon != null) soundIcon.sprite = UiKit.Icon(AudioListener.volume > 0f ? UiIcon.SoundOn : UiIcon.SoundOff);
        }

        // =====================================================================================
        //  Botón "MENÚ" en la pantalla de fin de partida
        // =====================================================================================

        void AddMenuButtonToGameOver()
        {
            HUDController hud = FindFirstObjectByType<HUDController>(FindObjectsInactive.Include);
            if (hud == null) return;
            Transform box = hud.transform.Find("PanelFinDePartida/Caja");
            if (box == null || box.Find("BotonMenu") != null) return;

            Transform restart = box.Find("BotonReiniciar");
            if (restart is RectTransform rr)
            {
                rr.anchoredPosition = new Vector2(-245f, rr.anchoredPosition.y);
                rr.sizeDelta = new Vector2(440f, rr.sizeDelta.y);
            }
            float y = restart is RectTransform r2 ? r2.anchoredPosition.y : -730f;
            Vector2 anchor = restart is RectTransform r3 ? r3.anchorMin : new Vector2(0.5f, 1f);

            Button menu = UiKit.Button("BotonMenu", box, "MENÚ", UiKit.Teal, new Vector2(440f, 84f), 34, UiIcon.Home, 24f);
            RectTransform mr = (RectTransform)menu.transform;
            mr.anchorMin = anchor; mr.anchorMax = anchor; mr.pivot = new Vector2(0.5f, 0.5f);
            mr.anchoredPosition = new Vector2(245f, y);
            mr.sizeDelta = new Vector2(440f, 84f);
            menu.onClick.AddListener(() => { if (GameManager.Instance != null) GameManager.Instance.ReturnToMenu(); });
        }
    }
}
