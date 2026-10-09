using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EcoShark
{
    /// <summary>
    /// (Claude) Controles táctiles para móvil / WebGL en teléfono:
    ///  - Joystick flotante: aparece donde apoyas el dedo en la mitad izquierda de la pantalla.
    ///  - Botón de IMPULSO abajo a la derecha (se mantiene presionado, igual que Shift).
    ///  - Botón de PAUSA arriba (en móvil no hay tecla P).
    /// Escribe en EcoInput.VirtualAxis / VirtualBoost, así SharkMovement no cambia.
    /// Se muestran solos en móvil o cuando se detecta un dedo en la pantalla. En PC siguen ocultos,
    /// salvo que se active "forceVisible" (menú ECO SHARK > 12, o la línea "tactil" en pedir_fotos.txt)
    /// para probarlos con el mouse.
    /// Va en el objeto "HUD" (el Canvas). Menú del editor: ECO SHARK > 11.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        [Tooltip("Mostrar los controles también en PC (para probarlos con el mouse).")]
        [SerializeField] bool forceVisible;
        [Tooltip("Radio del joystick en unidades del lienzo (1920×1080).")]
        [SerializeField] float stickRadius = 130f;
        [Range(0f, 0.5f)]
        [SerializeField] float deadZone = 0.12f;

        RectTransform root, stickBase, knob, boostFace;
        GameObject pauseButton, keyboardHint, keyboardHintText;
        CanvasGroup baseGroup;
        Image boostCircle;
        Vector2 restPosition;
        bool touchSeen, stickHeld, boostHeld;

        // (Claude) Para el tráiler y demostraciones: mostrar los controles y moverlos por código.
        bool previewing, previewBoost;
        Vector2 previewAxis;
        public bool ForceVisible { get => forceVisible; set => forceVisible = value; }
        public void Preview(Vector2 axis, bool boost) { previewing = true; previewAxis = Vector2.ClampMagnitude(axis, 1f); previewBoost = boost; }
        public void StopPreview() { previewing = false; previewBoost = false; EcoInput.VirtualAxis = Vector2.zero; EcoInput.VirtualBoost = false; }

        void Start()
        {
            Build();
            Apply(false);
        }

        void OnDisable() => Release();

        void Release()
        {
            stickHeld = false;
            boostHeld = false;
            EcoInput.VirtualAxis = Vector2.zero;
            EcoInput.VirtualBoost = false;
            EcoInput.VirtualControlsActive = false;
        }

        // ---------------------------------------------------------------- Construcción

        void Build()
        {
            RectTransform canvas = (RectTransform)transform;

            root = UiKit.Rect("ControlesTactiles", canvas);
            UiKit.Stretch(root, 0f);
            // Detrás de los paneles (pausa, fin de partida): justo después del parpadeo de daño.
            Transform flash = transform.Find("ParpadeoDanio");
            root.SetSiblingIndex(flash != null ? flash.GetSiblingIndex() + 1 : 0);

            // El letrero "Mover: WASD..." del HUD no aplica con controles táctiles.
            Transform hintBg = transform.Find("FondoControles"), hintText = transform.Find("Controles");
            if (hintBg != null) keyboardHint = hintBg.gameObject;
            if (hintText != null) keyboardHintText = hintText.gameObject;

            // Zona del joystick: mitad izquierda, sin tapar la barra de energía.
            Image zone = UiKit.Image("ZonaJoystick", root, null, new Color(0f, 0f, 0f, 0f));
            zone.raycastTarget = true;
            zone.rectTransform.anchorMin = new Vector2(0f, 0f);
            zone.rectTransform.anchorMax = new Vector2(0.5f, 0.8f);
            zone.rectTransform.offsetMin = zone.rectTransform.offsetMax = Vector2.zero;
            TouchZone stick = zone.gameObject.AddComponent<TouchZone>();
            stick.Down = StickDown;
            stick.Drag = StickDrag;
            stick.Up = StickUp;

            float size = stickRadius * 2f + 120f; // la palanca nunca se sale del aro
            Image ring = UiKit.Image("Joystick", root, UiKit.Circle, new Color(UiKit.Outline.r, UiKit.Outline.g, UiKit.Outline.b, 0.55f));
            stickBase = ring.rectTransform;
            UiKit.Place(stickBase, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            baseGroup = ring.gameObject.AddComponent<CanvasGroup>();
            baseGroup.blocksRaycasts = false;
            Image inner = UiKit.Image("Fondo", stickBase, UiKit.Circle, new Color(0.09f, 0.66f, 0.8f, 0.22f));
            UiKit.Stretch(inner.rectTransform, 8f);
            Image edge = UiKit.Image("Aro", stickBase, UiKit.Ring, new Color(1f, 1f, 1f, 0.35f));
            UiKit.Stretch(edge.rectTransform, 2f);

            Image knobBorder = UiKit.Image("Palanca", stickBase, UiKit.Circle, UiKit.Outline);
            knob = knobBorder.rectTransform;
            UiKit.Place(knob, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(132f, 132f));
            Image knobFace = UiKit.Image("Cara", knob, UiKit.Circle, UiKit.Teal);
            UiKit.Stretch(knobFace.rectTransform, 7f);
            Image knobShine = UiKit.Image("Brillo", knobFace.transform, UiKit.Circle, new Color(1f, 1f, 1f, 0.25f));
            UiKit.Place(knobShine.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(70f, 40f));
            Image knobIcon = UiKit.Image("Aleta", knobFace.transform, UiKit.Icon(UiIcon.Fin), new Color(1f, 1f, 1f, 0.9f));
            UiKit.Stretch(knobIcon.rectTransform, 30f);

            // Botón de impulso.
            Image boost = UiKit.Image("BotonImpulso", root, UiKit.Circle, UiKit.Outline);
            boost.raycastTarget = true;
            boostFace = boost.rectTransform;
            UiKit.Place(boostFace, new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-210f, 210f), new Vector2(220f, 220f));
            boostCircle = UiKit.Image("Cara", boostFace, UiKit.Circle, UiKit.Orange);
            UiKit.Stretch(boostCircle.rectTransform, 9f);
            Image boostShine = UiKit.Image("Brillo", boostCircle.transform, UiKit.Circle, new Color(1f, 1f, 1f, 0.25f));
            UiKit.Place(boostShine.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(120f, 64f));
            Image bolt = UiKit.Image("Rayo", boostCircle.transform, UiKit.Icon(UiIcon.Bolt), Color.white);
            UiKit.Stretch(bolt.rectTransform, 44f);
            Text boostLabel = UiKit.Label("Texto", boostFace, "IMPULSO", 26, Color.white, TextAnchor.MiddleCenter, true, 3f);
            UiKit.Place(boostLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 4f), new Vector2(260f, 40f));
            TouchZone boostZone = boost.gameObject.AddComponent<TouchZone>();
            boostZone.Down = (p, c) => boostHeld = true;
            boostZone.Up = () => boostHeld = false;

            // Botón de pausa: encima de todo, para poder reanudar con el dedo.
            Button pause = UiKit.Button("BotonPausa", canvas, "", UiKit.TealDark, new Vector2(104f, 104f), 30, UiIcon.Pause, 30f);
            UiKit.Place((RectTransform)pause.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-410f, -22f), new Vector2(104f, 104f));
            pause.onClick.AddListener(() => { if (GameManager.Instance != null) GameManager.Instance.TogglePause(); });
            pauseButton = pause.gameObject;
            pauseButton.transform.SetAsLastSibling();

        }

        // ---------------------------------------------------------------- Joystick

        void StickDown(Vector2 screen, Camera cam)
        {
            if (!ToLocal(screen, cam, out Vector2 local)) return;
            stickHeld = true;
            // El joystick aparece bajo el dedo, sin salirse de la pantalla.
            float half = stickRadius + 60f;
            Rect r = root.rect;
            local.x = Mathf.Clamp(local.x, r.xMin + half, r.xMax - half);
            local.y = Mathf.Clamp(local.y, r.yMin + half, r.yMax - half);
            stickBase.anchoredPosition = local;
            knob.anchoredPosition = Vector2.zero;
            EcoInput.VirtualAxis = Vector2.zero;
        }

        void StickDrag(Vector2 screen, Camera cam)
        {
            if (!stickHeld || !ToLocal(screen, cam, out Vector2 local)) return;
            Vector2 delta = Vector2.ClampMagnitude(local - stickBase.anchoredPosition, stickRadius);
            knob.anchoredPosition = delta;
            Vector2 axis = delta / stickRadius;
            float m = axis.magnitude;
            // Zona muerta: pequeños temblores del dedo no mueven al tiburón.
            EcoInput.VirtualAxis = m <= deadZone ? Vector2.zero : axis / m * Mathf.InverseLerp(deadZone, 1f, m);
        }

        void StickUp()
        {
            stickHeld = false;
            EcoInput.VirtualAxis = Vector2.zero;
        }

        bool ToLocal(Vector2 screen, Camera cam, out Vector2 local)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, cam, out local);
        }

        // ---------------------------------------------------------------- Estado

        void Update()
        {
            if (root == null) return;
            if (!touchSeen && EcoInput.TouchActive()) touchSeen = true;

            GameManager gm = GameManager.Instance;
            GameState state = gm != null ? gm.State : GameState.Playing;
            bool wanted = forceVisible || Application.isMobilePlatform || touchSeen;
            bool show = wanted && state == GameState.Playing;
            Apply(show);
            bool showPause = wanted && (state == GameState.Playing || state == GameState.Paused);
            if (pauseButton.activeSelf != showPause) pauseButton.SetActive(showPause);
            if (wanted && keyboardHint != null && keyboardHint.activeSelf) keyboardHint.SetActive(false);
            if (wanted && keyboardHintText != null && keyboardHintText.activeSelf) keyboardHintText.SetActive(false);
            if (!show) return;

            float dt = Time.unscaledDeltaTime;
            bool demo = previewing && !stickHeld;
            bool boostOn = boostHeld || (previewing && previewBoost);
            EcoInput.VirtualBoost = boostOn;
            if (demo) EcoInput.VirtualAxis = previewAxis;
            Rect area = root.rect;
            restPosition = new Vector2(area.xMin + 250f, area.yMin + 250f);

            // Suelto: el joystick vuelve a su sitio y se atenúa.
            if (!stickHeld)
            {
                stickBase.anchoredPosition = Vector2.Lerp(stickBase.anchoredPosition, restPosition, 1f - Mathf.Exp(-10f * dt));
                knob.anchoredPosition = Vector2.Lerp(knob.anchoredPosition, demo ? previewAxis * stickRadius : Vector2.zero, 1f - Mathf.Exp(-20f * dt));
            }
            baseGroup.alpha = Mathf.MoveTowards(baseGroup.alpha, stickHeld || demo ? 1f : 0.5f, dt * 5f);

            float scale = Mathf.Lerp(boostFace.localScale.x, boostOn ? 0.9f : 1f, 1f - Mathf.Exp(-18f * dt));
            boostFace.localScale = Vector3.one * scale;
            boostCircle.color = boostOn ? UiKit.Yellow : UiKit.Orange;
        }

        void Apply(bool show)
        {
            if (root.gameObject.activeSelf != show)
            {
                root.gameObject.SetActive(show);
                if (!show) Release();
            }
            if (show) EcoInput.VirtualControlsActive = true;
        }
    }

    /// <summary>(Claude) Zona de pantalla que avisa cuando un dedo (o el mouse) la toca, arrastra y suelta. Sigue un solo dedo.</summary>
    public class TouchZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IInitializePotentialDragHandler
    {
        public System.Action<Vector2, Camera> Down, Drag;
        public System.Action Up;
        int pointerId = int.MinValue;

        public void OnPointerDown(PointerEventData e)
        {
            if (pointerId != int.MinValue) return;
            pointerId = e.pointerId;
            Down?.Invoke(e.position, e.pressEventCamera);
            Drag?.Invoke(e.position, e.pressEventCamera);
        }

        // Sin umbral de arrastre: el joystick responde desde el primer píxel.
        public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = false;

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId == pointerId) Drag?.Invoke(e.position, e.pressEventCamera);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != pointerId) return;
            pointerId = int.MinValue;
            Up?.Invoke();
        }

        void OnDisable()
        {
            if (pointerId == int.MinValue) return;
            pointerId = int.MinValue;
            Up?.Invoke();
        }
    }
}
