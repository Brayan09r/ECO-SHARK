using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EcoShark
{
    public enum UiIcon { Fin, Leaf, Heart, Clock, Gear, SoundOn, SoundOff, Star, Play, Bottle, Bolt, Magnet, Shield, Wave, Question, Close, ArrowUp, Home, Trophy, Pause }

    /// <summary>
    /// Kit de interfaz estilo juego móvil, sin imágenes externas:
    ///  - Fuentes: Lilita One (títulos) y Baloo 2 (textos), en Resources/Fonts (licencia SIL OFL).
    ///  - Sprites generados por código: rectángulos redondeados (9-slice), círculos, degradados e íconos.
    ///  - Atajos para crear textos con contorno, imágenes y botones que "rebotan" al tocarlos.
    /// Lo usan MainMenuController y HudTopBar.
    /// </summary>
    public static class UiKit
    {
        // ---------------------------------------------------------------- Colores de la marca ECO SHARK
        public static readonly Color Navy = new Color(0.02f, 0.1f, 0.18f);
        public static readonly Color NavyGlass = new Color(0.02f, 0.1f, 0.18f, 0.82f);
        public static readonly Color Teal = new Color(0.09f, 0.66f, 0.8f);
        public static readonly Color TealDark = new Color(0.04f, 0.38f, 0.52f);
        public static readonly Color EcoGreen = new Color(0.47f, 0.93f, 0.4f);
        public static readonly Color Orange = new Color(1f, 0.6f, 0.13f);
        public static readonly Color Yellow = new Color(1f, 0.84f, 0.2f);
        public static readonly Color Coral = new Color(1f, 0.38f, 0.42f);
        public static readonly Color Outline = new Color(0.02f, 0.07f, 0.14f);

        // ---------------------------------------------------------------- Fuentes
        static Font title, body;

        public static Font TitleFont
        {
            get
            {
                if (title == null) title = Resources.Load<Font>("Fonts/LilitaOne-Regular");
                if (title == null) title = BodyFont;
                return title;
            }
        }

        public static Font BodyFont
        {
            get
            {
                if (body == null) body = Resources.Load<Font>("Fonts/Baloo2-Bold");
                if (body == null) body = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return body;
            }
        }

        // ---------------------------------------------------------------- Sprites generados
        const int RoundRadius = 48;
        static Sprite rounded, circle, ring, gradientV, shine;
        static readonly Dictionary<UiIcon, Sprite> icons = new Dictionary<UiIcon, Sprite>();

        /// <summary>Rectángulo redondeado blanco (9-slice). Usa SetCorner para elegir el radio en pantalla.</summary>
        public static Sprite Rounded
        {
            get
            {
                if (rounded != null) return rounded;
                int r = RoundRadius, size = r * 2 + 4;
                var tex = NewTexture(size, size);
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, r, size - r), cy = Mathf.Clamp(y + 0.5f, r, size - r);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) - r;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(0.5f - d) * 255));
                }
                tex.SetPixels32(px);
                tex.Apply();
                rounded = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                                        new Vector4(r + 1, r + 1, r + 1, r + 1));
                return rounded;
            }
        }

        public static Sprite Circle => circle != null ? circle : (circle = SdfSprite(128, p => p.magnitude - 0.96f));
        public static Sprite Ring => ring != null ? ring : (ring = SdfSprite(128, p => Mathf.Abs(p.magnitude - 0.86f) - 0.1f));

        /// <summary>Degradado vertical: opaco abajo, transparente arriba.</summary>
        public static Sprite GradientV
        {
            get
            {
                if (gradientV != null) return gradientV;
                var tex = NewTexture(4, 64);
                for (int y = 0; y < 64; y++)
                {
                    float a = 1f - y / 63f;
                    a = a * a * (3f - 2f * a);
                    for (int x = 0; x < 4; x++) tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                tex.Apply();
                gradientV = Sprite.Create(tex, new Rect(0, 0, 4, 64), new Vector2(0.5f, 0.5f));
                return gradientV;
            }
        }

        /// <summary>Brillo para barras y botones: blanco arriba que se desvanece hacia el centro.</summary>
        public static Sprite Shine
        {
            get
            {
                if (shine != null) return shine;
                int r = RoundRadius, size = r * 2 + 4;
                var tex = NewTexture(size, size);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, r, size - r), cy = Mathf.Clamp(y + 0.5f, r, size - r);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) - r;
                    float edge = Mathf.Clamp01(0.5f - d);
                    float v = Mathf.Clamp01((y / (float)size - 0.5f) * 2.2f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, edge * v * 0.55f));
                }
                tex.Apply();
                shine = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                                      new Vector4(r + 1, r + 1, r + 1, r + 1));
                return shine;
            }
        }

        /// <summary>Ícono blanco sobre transparente (colorealo con Image.color).</summary>
        public static Sprite Icon(UiIcon icon)
        {
            if (icons.TryGetValue(icon, out Sprite s) && s != null) return s;
            s = SdfSprite(128, IconShape(icon));
            icons[icon] = s;
            return s;
        }

        // ---------------------------------------------------------------- Creación de UI

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, Color color, float corner = 24f)
        {
            RectTransform rt = Rect(name, parent);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (sprite == Rounded || sprite == Shine)
            {
                img.type = UnityEngine.UI.Image.Type.Sliced;
                SetCorner(img, corner);
            }
            else img.preserveAspect = sprite != null && sprite != GradientV;
            return img;
        }

        /// <summary>Radio de las esquinas (en unidades del lienzo de 1920×1080).</summary>
        public static void SetCorner(Image img, float corner)
        {
            img.pixelsPerUnitMultiplier = (RoundRadius + 1) / Mathf.Max(1f, corner);
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color,
                                 TextAnchor anchor = TextAnchor.MiddleCenter, bool titleFont = false, float outline = 3f)
        {
            RectTransform rt = Rect(name, parent);
            Text t = rt.gameObject.AddComponent<Text>();
            t.font = titleFont ? TitleFont : BodyFont;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.text = text;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            if (outline > 0f)
            {
                var o = rt.gameObject.AddComponent<UnityEngine.UI.Outline>();
                o.effectColor = new Color(Outline.r, Outline.g, Outline.b, 0.9f);
                o.effectDistance = new Vector2(outline, -outline);
                var sh = rt.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0f, 0f, 0f, 0.35f);
                sh.effectDistance = new Vector2(0f, -outline - 2f);
            }
            return t;
        }

        /// <summary>Botón redondeado con contorno oscuro, brillo arriba, ícono opcional y animación al tocarlo.</summary>
        public static Button Button(string name, Transform parent, string text, Color color, Vector2 size, int fontSize,
                                    UiIcon? icon = null, float corner = 30f, Color? textColor = null)
        {
            // Contorno (sombra dura abajo, como los botones de juegos móviles).
            Image border = Image(name, parent, Rounded, Outline, corner + 4f);
            border.raycastTarget = true;
            border.rectTransform.sizeDelta = size;

            Image face = Image("Cara", border.transform, Rounded, color, corner);
            Stretch(face.rectTransform, 5f);
            face.rectTransform.offsetMin = new Vector2(5f, 11f);

            Image gloss = Image("Brillo", face.transform, Shine, Color.white, corner);
            Stretch(gloss.rectTransform, 0f);

            float textLeft = 10f;
            if (icon.HasValue)
            {
                Image ic = Image("Icono", face.transform, Icon(icon.Value), textColor ?? Color.white);
                float s = Mathf.Min(size.y - 40f, 64f);
                Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(string.IsNullOrEmpty(text) ? (size.x - s) * 0.5f - 5f : 22f, 0f), new Vector2(s, s));
                textLeft = 22f + s;
            }
            if (!string.IsNullOrEmpty(text))
            {
                Text t = Label("Texto", face.transform, text, fontSize, textColor ?? Color.white, TextAnchor.MiddleCenter, true, 3f);
                Stretch(t.rectTransform, 0f);
                t.rectTransform.offsetMin = new Vector2(textLeft, 0f);
                t.rectTransform.offsetMax = new Vector2(-10f, 0f);
            }

            Button b = border.gameObject.AddComponent<Button>();
            b.targetGraphic = face;
            ColorBlock cb = b.colors;
            cb.highlightedColor = new Color(1.08f, 1.08f, 1.08f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            cb.disabledColor = new Color(0.55f, 0.55f, 0.6f, 0.9f);
            cb.fadeDuration = 0.08f;
            b.colors = cb;
            border.gameObject.AddComponent<UiBounce>();
            return b;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        public static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>1250 -> "1.250" (sin depender de culturas: en WebGL pueden no existir).</summary>
        public static string Thousands(int n)
        {
            string s = Mathf.Abs(n).ToString();
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (i > 0 && (s.Length - i) % 3 == 0) sb.Append('.');
                sb.Append(s[i]);
            }
            return (n < 0 ? "-" : "") + sb;
        }

        public static string FormatTime(float seconds)
        {
            int s = Mathf.FloorToInt(seconds);
            return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
        }

        // ---------------------------------------------------------------- Dibujo de íconos (campos de distancia)

        delegate float Sdf(Vector2 p);

        static Texture2D NewTexture(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
        }

        static Sprite SdfSprite(int size, Sdf sdf)
        {
            var tex = NewTexture(size, size);
            var px = new Color32[size * size];
            float pixel = 2f / size;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                float d = sdf(p);
                float a = Mathf.Clamp01(0.5f - d / pixel);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sdf IconShape(UiIcon icon)
        {
            switch (icon)
            {
                case UiIcon.Fin:
                    return p => Poly(p, new[] { V(-0.85f, -0.62f), V(0.85f, -0.62f), V(0.55f, -0.35f), V(0.38f, 0.05f), V(0.3f, 0.45f), V(0.34f, 0.85f),
                                               V(0.1f, 0.72f), V(-0.2f, 0.45f), V(-0.5f, 0.05f), V(-0.72f, -0.35f) });
                case UiIcon.Leaf:
                    return p =>
                    {
                        Vector2 q = Rot(p, 45f);
                        float leaf = Mathf.Max(Vector2.Distance(q, V(0.62f, 0f)) - 1.05f, Vector2.Distance(q, V(-0.62f, 0f)) - 1.05f);
                        leaf = Mathf.Max(leaf, Mathf.Abs(q.y) - 0.92f);
                        float stem = Seg(q, V(0f, -0.95f), V(0f, -0.7f)) - 0.08f;
                        float vein = Seg(q, V(0f, -0.55f), V(0f, 0.5f)) - 0.035f;
                        return Mathf.Min(Mathf.Max(leaf, -vein), stem);
                    };
                case UiIcon.Heart:
                    return p =>
                    {
                        Vector2 q = new Vector2(Mathf.Abs(p.x), p.y + 0.12f);
                        float c = Vector2.Distance(q, V(0.42f, 0.3f)) - 0.46f;
                        float t = Poly(new Vector2(p.x, p.y + 0.12f), new[] { V(-0.86f, 0.22f), V(0.86f, 0.22f), V(0f, -0.82f) });
                        return Mathf.Min(c, t);
                    };
                case UiIcon.Clock:
                    return p =>
                    {
                        float r = Mathf.Abs(p.magnitude - 0.78f) - 0.12f;
                        float h = Seg(p, Vector2.zero, V(0f, 0.5f)) - 0.09f;
                        float m = Seg(p, Vector2.zero, V(0.38f, -0.1f)) - 0.09f;
                        return Mathf.Min(r, Mathf.Min(h, m));
                    };
                case UiIcon.Gear:
                    return p =>
                    {
                        float a = Mathf.Atan2(p.y, p.x);
                        float teeth = Mathf.Clamp(Mathf.Cos(a * 8f) * 3f, -1f, 1f);
                        float outer = p.magnitude - (0.72f + 0.14f * teeth);
                        return Mathf.Max(outer, -(p.magnitude - 0.3f));
                    };
                case UiIcon.SoundOn:
                case UiIcon.SoundOff:
                {
                    bool on = icon == UiIcon.SoundOn;
                    return p =>
                    {
                        float body = Poly(p, new[] { V(-0.85f, -0.28f), V(-0.45f, -0.28f), V(0.05f, -0.7f), V(0.05f, 0.7f), V(-0.45f, 0.28f), V(-0.85f, 0.28f) });
                        if (on)
                        {
                            float w1 = Mathf.Max(Mathf.Abs(Vector2.Distance(p, V(0.05f, 0f)) - 0.42f) - 0.07f, -(p.x - 0.28f));
                            float w2 = Mathf.Max(Mathf.Abs(Vector2.Distance(p, V(0.05f, 0f)) - 0.72f) - 0.07f, -(p.x - 0.42f));
                            return Mathf.Min(body, Mathf.Min(w1, w2));
                        }
                        float x1 = Seg(p, V(0.3f, -0.35f), V(0.9f, 0.35f)) - 0.08f;
                        float x2 = Seg(p, V(0.3f, 0.35f), V(0.9f, -0.35f)) - 0.08f;
                        return Mathf.Min(body, Mathf.Min(x1, x2));
                    };
                }
                case UiIcon.Star:
                    return p => Star(p, 0.9f, 0.42f);
                case UiIcon.Play:
                    return p => Poly(p, new[] { V(-0.5f, -0.72f), V(0.72f, 0f), V(-0.5f, 0.72f) }) - 0.06f;
                case UiIcon.Bottle:
                    return p =>
                    {
                        float body = Box(p - V(0f, -0.25f), V(0.36f, 0.62f)) - 0.08f;
                        float neck = Box(p - V(0f, 0.52f), V(0.14f, 0.24f));
                        float cap = Box(p - V(0f, 0.82f), V(0.19f, 0.1f));
                        float label = Box(p - V(0f, -0.25f), V(0.5f, 0.12f));
                        return Mathf.Max(Mathf.Min(body, Mathf.Min(neck, cap)), -label);
                    };
                case UiIcon.Bolt:
                    return p => Poly(p, new[] { V(0.12f, 0.95f), V(-0.55f, -0.08f), V(-0.05f, -0.08f), V(-0.2f, -0.95f), V(0.55f, 0.15f), V(0.05f, 0.15f) });
                case UiIcon.Magnet:
                    return p =>
                    {
                        float arc = Mathf.Max(Mathf.Abs(Vector2.Distance(p, V(0f, 0.05f)) - 0.55f) - 0.2f, p.y - 0.05f);
                        float left = Box(p - V(-0.55f, 0.45f), V(0.2f, 0.4f));
                        float right = Box(p - V(0.55f, 0.45f), V(0.2f, 0.4f));
                        float gapL = Box(p - V(-0.55f, 0.72f), V(0.3f, 0.06f));
                        float gapR = Box(p - V(0.55f, 0.72f), V(0.3f, 0.06f));
                        return Mathf.Max(Mathf.Min(arc, Mathf.Min(left, right)), -Mathf.Min(gapL, gapR));
                    };
                case UiIcon.Shield:
                    return p => Poly(p, new[] { V(0f, 0.92f), V(0.75f, 0.62f), V(0.68f, -0.05f), V(0.35f, -0.6f), V(0f, -0.92f), V(-0.35f, -0.6f), V(-0.68f, -0.05f), V(-0.75f, 0.62f) });
                case UiIcon.Wave:
                    return p =>
                    {
                        float d = 9f;
                        for (int k = 0; k < 2; k++)
                        {
                            float yOff = k == 0 ? 0.28f : -0.3f;
                            float y = yOff + 0.18f * Mathf.Sin(p.x * 4.2f + k);
                            d = Mathf.Min(d, Mathf.Abs(p.y - y) * 0.85f - 0.11f);
                        }
                        return Mathf.Max(d, Mathf.Abs(p.x) - 0.88f);
                    };
                case UiIcon.Question:
                    return p =>
                    {
                        float ring = Mathf.Abs(Vector2.Distance(p, V(0f, 0.33f)) - 0.36f) - 0.13f;
                        float arc = Mathf.Max(ring, -Mathf.Max(p.x, p.y - 0.33f)); // sin el cuarto inferior izquierdo
                        float stem = Seg(p, V(0f, -0.03f), V(0f, -0.3f)) - 0.13f;
                        float dot = Vector2.Distance(p, V(0f, -0.7f)) - 0.15f;
                        return Mathf.Min(Mathf.Min(arc, stem), dot);
                    };
                case UiIcon.Close:
                    return p => Mathf.Min(Seg(p, V(-0.6f, -0.6f), V(0.6f, 0.6f)), Seg(p, V(-0.6f, 0.6f), V(0.6f, -0.6f))) - 0.16f;
                case UiIcon.ArrowUp:
                    return p => Mathf.Min(Poly(p, new[] { V(0f, 0.9f), V(0.7f, 0.1f), V(-0.7f, 0.1f) }), Box(p - V(0f, -0.4f), V(0.26f, 0.52f)));
                case UiIcon.Home:
                    return p => Mathf.Max(Mathf.Min(Poly(p, new[] { V(0f, 0.85f), V(0.85f, -0.02f), V(-0.85f, -0.02f) }), Box(p - V(0f, -0.42f), V(0.6f, 0.46f))),
                                          -Box(p - V(0f, -0.57f), V(0.18f, 0.32f)));
                case UiIcon.Trophy:
                    return p =>
                    {
                        float cup = Mathf.Max(Vector2.Distance(p, V(0f, 0.35f)) - 0.6f, p.y - 0.75f);
                        float handles = Mathf.Abs(Vector2.Distance(new Vector2(Mathf.Abs(p.x), p.y), V(0.55f, 0.45f)) - 0.25f) - 0.07f;
                        float stem = Box(p - V(0f, -0.4f), V(0.1f, 0.25f));
                        float foot = Box(p - V(0f, -0.75f), V(0.42f, 0.1f));
                        return Mathf.Min(Mathf.Min(cup, handles), Mathf.Min(stem, foot));
                    };
                case UiIcon.Pause: // (Claude) botón de pausa táctil
                    return p => Mathf.Min(Box(p - V(-0.34f, 0f), V(0.17f, 0.62f)), Box(p - V(0.34f, 0f), V(0.17f, 0.62f))) - 0.05f;
                default:
                    return p => p.magnitude - 0.8f;
            }
        }

        static Vector2 V(float x, float y) => new Vector2(x, y);

        static Vector2 Rot(Vector2 p, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(p.x * c - p.y * s, p.x * s + p.y * c);
        }

        static float Seg(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }

        static float Box(Vector2 p, Vector2 half)
        {
            Vector2 d = new Vector2(Mathf.Abs(p.x) - half.x, Mathf.Abs(p.y) - half.y);
            return new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f);
        }

        /// <summary>Distancia con signo a un polígono (negativa dentro).</summary>
        static float Poly(Vector2 p, Vector2[] v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                Vector2 e = v[j] - v[i];
                Vector2 w = p - v[i];
                Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        static float Star(Vector2 p, float r, float inner)
        {
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                float rr = i % 2 == 0 ? r : inner;
                pts[i] = new Vector2(Mathf.Cos(a) * rr, Mathf.Sin(a) * rr - 0.05f);
            }
            return Poly(p, pts);
        }
    }

    /// <summary>Los botones crecen un poco al pasar el mouse y se hunden al presionarlos.</summary>
    public class UiBounce : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public float idlePulse;   // > 0: late suavemente (para el botón JUGAR)
        float target = 1f, current = 1f;
        Selectable selectable;

        void Awake() => selectable = GetComponent<Selectable>();

        public void OnPointerEnter(PointerEventData e) { if (Interactable) target = 1.06f; }
        public void OnPointerExit(PointerEventData e) => target = 1f;
        public void OnPointerDown(PointerEventData e) { if (Interactable) target = 0.93f; }
        public void OnPointerUp(PointerEventData e) => target = 1f;

        bool Interactable => selectable == null || selectable.IsInteractable();

        void Update()
        {
            current = Mathf.Lerp(current, target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            float pulse = idlePulse > 0f ? 1f + Mathf.Sin(Time.unscaledTime * 3.2f) * idlePulse : 1f;
            transform.localScale = Vector3.one * current * pulse;
        }
    }
}
