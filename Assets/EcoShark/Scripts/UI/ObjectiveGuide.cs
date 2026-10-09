using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EcoShark
{
    /// <summary>
    /// (Claude) Guía para el océano grande: flechas en el borde de la pantalla que apuntan a los animales
    /// atrapados más cercanos, con la distancia. El color pasa de coral a rojo (y late) cuando al animal
    /// le queda poco tiempo. Cuando el animal ya se ve en pantalla, la flecha desaparece.
    /// Va en el objeto "HUD" (el Canvas). Menú del editor: ECO SHARK > 11.
    /// </summary>
    public class ObjectiveGuide : MonoBehaviour
    {
        [SerializeField] int maxMarkers = 3;
        [Tooltip("Margen desde el borde de la pantalla (unidades del lienzo).")]
        [SerializeField] Vector2 edgeMargin = new Vector2(120f, 170f);
        [Tooltip("Más lejos que esto no se señala (0 = sin límite).")]
        [SerializeField] float maxDistance = 0f;

        class Marker
        {
            public RectTransform root, arrow;
            public Image circle;
            public Text distance;
        }

        readonly List<Marker> markers = new List<Marker>();
        readonly List<TrappedAnimal> targets = new List<TrappedAnimal>();
        RectTransform layer;
        Transform shark;
        Camera cam;
        float refreshTimer;
        bool ordered;

        void Start()
        {
            RectTransform canvas = (RectTransform)transform;
            layer = UiKit.Rect("GuiaRescates", canvas);
            UiKit.Stretch(layer, 0f);
            Transform flash = transform.Find("ParpadeoDanio");
            layer.SetSiblingIndex(flash != null ? flash.GetSiblingIndex() + 1 : 0);
            for (int i = 0; i < maxMarkers; i++) markers.Add(BuildMarker(i));

            SharkMovement sm = FindFirstObjectByType<SharkMovement>(FindObjectsInactive.Include);
            if (sm != null) shark = sm.transform;
        }

        Marker BuildMarker(int index)
        {
            var m = new Marker();
            m.root = UiKit.Rect("Flecha_" + index, layer);
            UiKit.Place(m.root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96f, 96f));

            Image border = UiKit.Image("Borde", m.root, UiKit.Circle, UiKit.Outline);
            UiKit.Stretch(border.rectTransform, 0f);
            m.circle = UiKit.Image("Circulo", m.root, UiKit.Circle, UiKit.Coral);
            UiKit.Stretch(m.circle.rectTransform, 7f);
            Image heart = UiKit.Image("Corazon", m.root, UiKit.Icon(UiIcon.Heart), Color.white);
            UiKit.Stretch(heart.rectTransform, 24f);

            // La punta gira alrededor del círculo y apunta al animal.
            m.arrow = UiKit.Rect("Punta", m.root);
            UiKit.Place(m.arrow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1f, 1f));
            Image tipBorder = UiKit.Image("Triangulo", m.arrow, UiKit.Icon(UiIcon.Play), UiKit.Outline);
            UiKit.Place(tipBorder.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(66f, 0f), new Vector2(46f, 46f));
            Image tip = UiKit.Image("Relleno", tipBorder.transform, UiKit.Icon(UiIcon.Play), Color.white);
            UiKit.Stretch(tip.rectTransform, 9f);

            m.distance = UiKit.Label("Distancia", m.root, "", 26, Color.white, TextAnchor.MiddleCenter, true, 3f);
            UiKit.Place(m.distance.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(160f, 36f));

            m.root.gameObject.SetActive(false);
            return m;
        }

        void LateUpdate()
        {
            if (layer == null) return;
            GameManager gm = GameManager.Instance;
            bool playing = gm == null || gm.State == GameState.Playing;
            if (!ordered)
            {
                // Después de que todo el HUD se construyó: las flechas quedan DETRÁS de los botones y paneles.
                ordered = true;
                Transform flash = transform.Find("ParpadeoDanio");
                layer.SetSiblingIndex(flash != null ? flash.GetSiblingIndex() + 1 : 0);
            }
            if (cam == null) cam = Camera.main;
            if (!playing || shark == null || cam == null)
            {
                HideFrom(0);
                return;
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = 0.4f;
                RefreshTargets();
            }

            Vector2 size = layer.rect.size;
            Vector2 half = new Vector2(Mathf.Max(10f, size.x * 0.5f - edgeMargin.x), Mathf.Max(10f, size.y * 0.5f - edgeMargin.y));
            int used = 0;
            for (int i = 0; i < targets.Count && used < markers.Count; i++)
            {
                TrappedAnimal animal = targets[i];
                if (animal == null || animal.IsFinished) continue;

                Vector3 world = animal.transform.position;
                Vector3 vp = cam.WorldToViewportPoint(world);
                // En unidades del lienzo, con el centro de la pantalla en (0,0).
                Vector2 p = new Vector2((vp.x - 0.5f) * size.x, (vp.y - 0.5f) * size.y);
                bool onScreen = vp.z > 0f && Mathf.Abs(p.x) < size.x * 0.5f - 60f && Mathf.Abs(p.y) < size.y * 0.5f - 60f;
                if (onScreen) continue;

                Vector2 dir = p.sqrMagnitude > 0.001f ? p.normalized : Vector2.right;
                float k = Mathf.Min(half.x / Mathf.Max(0.0001f, Mathf.Abs(dir.x)), half.y / Mathf.Max(0.0001f, Mathf.Abs(dir.y)));
                Marker m = markers[used++];
                if (!m.root.gameObject.activeSelf) m.root.gameObject.SetActive(true);
                m.root.anchoredPosition = dir * k;
                m.arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

                float meters = Vector2.Distance(world, shark.position);
                m.distance.text = Mathf.RoundToInt(meters) + " m";

                // Urgencia: poco tiempo = rojo y latiendo.
                float left = animal.TimeLeft01;
                bool urgent = left < 0.35f;
                Color c = Color.Lerp(new Color(1f, 0.15f, 0.2f), UiKit.Coral, Mathf.InverseLerp(0.2f, 0.6f, left));
                if (urgent) c = Color.Lerp(c, Color.white, (Mathf.Sin(Time.unscaledTime * 9f) * 0.5f + 0.5f) * 0.4f);
                m.circle.color = c;
                float pulse = urgent ? 1f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)) * 0.14f : 1f;
                m.root.localScale = Vector3.one * pulse;
            }
            HideFrom(used);
        }

        void HideFrom(int index)
        {
            for (int i = index; i < markers.Count; i++)
                if (markers[i].root.gameObject.activeSelf) markers[i].root.gameObject.SetActive(false);
        }

        /// <summary>Los animales atrapados, del más cercano al más lejano.</summary>
        void RefreshTargets()
        {
            targets.Clear();
            Vector3 origin = shark.position;
            foreach (TrappedAnimal a in FindObjectsByType<TrappedAnimal>(FindObjectsSortMode.None))
            {
                if (a == null || a.IsFinished) continue;
                if (maxDistance > 0f && Vector2.Distance(a.transform.position, origin) > maxDistance) continue;
                targets.Add(a);
            }
            targets.Sort((a, b) => ((Vector2)(a.transform.position - origin)).sqrMagnitude.CompareTo(((Vector2)(b.transform.position - origin)).sqrMagnitude));
        }
    }
}
