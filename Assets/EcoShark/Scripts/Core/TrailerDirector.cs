#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor.Media;
using UnityEngine;

namespace EcoShark
{
    /// <summary>
    /// (Claude) Graba los clips del tráiler SIN manos: si existe "Capturas/grabar_trailer.txt" al entrar en Play,
    /// un piloto automático maneja al tiburón (por EcoInput.VirtualAxis) y cada toma se guarda como MP4 en
    /// "Capturas/trailer/". Al terminar sale de Play solo. Usa Time.captureFramerate = 30, así el video sale
    /// fluido aunque el editor vaya lento. Pon la vista Game en "Full HD (1920x1080)" antes de grabar.
    /// Todo el archivo está dentro de #if UNITY_EDITOR: no entra en la build.
    /// </summary>
    public class TrailerDirector : MonoBehaviour
    {
        const int Fps = 30;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            string flag = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Capturas", "grabar_trailer.txt"));
            if (!File.Exists(flag)) return;
            File.Delete(flag);
            new GameObject("~DirectorTrailer").AddComponent<TrailerDirector>();
        }

        string dir;
        Texture2D frame;
        SharkMovement shark;
        SharkHealth health;
        Rigidbody rb;
        CameraFollow follow;
        TouchControls touch;
        bool rescued;
        readonly System.Text.StringBuilder log = new System.Text.StringBuilder();

        Vector2 Pos => rb != null ? (Vector2)rb.position : Vector2.zero;

        void OnEnable() => GameEvents.AnimalRescued += OnRescued;
        void OnDisable() => GameEvents.AnimalRescued -= OnRescued;
        void OnRescued(TrappedAnimal a) => rescued = true;

        IEnumerator Start()
        {
            dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Capturas", "trailer"));
            Directory.CreateDirectory(dir);
            Time.captureFramerate = Fps;
            AudioListener.volume = 0f; // el audio se pone después, en la edición

            shark = FindFirstObjectByType<SharkMovement>(FindObjectsInactive.Include);
            health = FindFirstObjectByType<SharkHealth>(FindObjectsInactive.Include);
            follow = FindFirstObjectByType<CameraFollow>();
            touch = FindFirstObjectByType<TouchControls>(FindObjectsInactive.Include);
            rb = shark != null ? shark.GetComponent<Rigidbody>() : null;
            log.AppendLine("Resolución: " + Screen.width + "x" + Screen.height);

            yield return Wait(1.2f);

            // 1. Menú principal y transición a la partida.
            MainMenuController menu = FindFirstObjectByType<MainMenuController>();
            yield return Record("01_menu", 3.5f, null);
            if (menu != null)
            {
                menu.Play();
                yield return Record("02_inicio", 2.2f, t => Axis(new Vector2(1f, -0.1f) * Mathf.Clamp01(t - 0.6f)));
            }
            else if (GameManager.Instance != null) GameManager.Instance.StartRun();
            yield return Wait(1.6f); // la cámara vuelve a su suavizado normal

            // 2. Rescate (primero, mientras a los animales les queda tiempo).
            TrappedAnimal animal = BestAnimal();
            if (animal != null)
            {
                Teleport((Vector2)animal.transform.position + new Vector2(-7.5f, 1.2f));
                yield return Wait(0.4f);
                rescued = false;
                float rescuedAt = -1f;
                yield return Record("03_rescate", 7.5f, t =>
                {
                    if (rescued && rescuedAt < 0f) rescuedAt = t;
                    if (!rescued && animal != null) SteerTo((Vector2)animal.transform.position + new Vector2(-0.6f, 0.3f), 1.2f);
                    else Axis(rescuedAt >= 0f && t - rescuedAt > 0.6f ? new Vector2(0.55f, 0.25f) : Vector2.zero);
                });
                log.AppendLine("Rescate logrado: " + rescued + " en t=" + rescuedAt.ToString("F1"));
            }
            else log.AppendLine("Sin animales para la toma de rescate.");

            // 3. Limpiar: recoger basura seguida (combo, misión, datos educativos).
            Heal();
            TrashCollectible first = NearestTrash(Pos + new Vector2(25f, -6f));
            if (first != null) Teleport((Vector2)first.transform.position + new Vector2(-5f, 1f));
            yield return Wait(0.4f);
            yield return Record("04_limpiar", 10f, t =>
            {
                TrashCollectible n = NearestTrash(Pos);
                if (n != null) SteerTo(n.transform.position, 0.3f); else Axis(Vector2.right);
            });

            // 4. Impulso atravesando un cardumen.
            Heal();
            FishSchools fish = FindFirstObjectByType<FishSchools>();
            if (fish != null && fish.TryGetNearestSchool(Pos, out Vector2 school))
            {
                Teleport(school + new Vector2(-11f, 0.3f));
                yield return Wait(0.4f);
                yield return Record("05_impulso", 4f, t => { Axis(new Vector2(1f, 0.04f)); EcoInput.VirtualBoost = t > 0.5f; });
                EcoInput.VirtualBoost = false;
            }

            // 5. Superficie: barco de pesca ilegal, olas y cielo.
            Heal();
            OceanArea area = OceanArea.Current;
            IllegalFishingBoat boat = Nearest<IllegalFishingBoat>(Pos);
            if (boat != null && area != null)
            {
                Teleport(new Vector2(boat.transform.position.x - 10f, area.SurfaceY - 3.2f));
                yield return Wait(0.5f);
                yield return Record("06_barco", 5f, t => Axis(new Vector2(1f, Mathf.Sin(t * 1.4f) * 0.18f)));
            }

            // 6. Petróleo.
            Heal();
            OilSlick oil = Nearest<OilSlick>(Pos);
            if (oil != null)
            {
                Teleport((Vector2)oil.transform.position + new Vector2(-8f, 0.4f));
                yield return Wait(0.4f);
                Vector2 through = (Vector2)oil.transform.position + new Vector2(9f, -0.5f);
                yield return Record("07_petroleo", 4.5f, t => SteerTo(through, 0.5f));
            }

            // 7. Barril tóxico: el golpe.
            Heal();
            RadioactiveBarrel barrel = Nearest<RadioactiveBarrel>(Pos);
            if (barrel != null)
            {
                Teleport((Vector2)barrel.transform.position + new Vector2(-7f, 0.8f));
                yield return Wait(0.4f);
                Vector2 past = (Vector2)barrel.transform.position + new Vector2(8f, 0f);
                yield return Record("08_barril", 3.5f, t => SteerTo(past, 0.5f));
            }

            // 8. Fondo marino: algas y corales.
            Heal();
            if (area != null)
            {
                Teleport(new Vector2(Mathf.Clamp(Pos.x, area.Min.x + 30f, area.Max.x - 60f), area.FloorY + 9f));
                yield return Wait(0.4f);
                float floor = area.FloorY + 2.6f;
                yield return Record("09_fondo", 5.5f, t => Axis(new Vector2(1f, Mathf.Clamp((floor - Pos.y) * 0.5f, -0.8f, 0.3f))));
            }

            // 9. Controles táctiles (joystick e impulso en pantalla).
            Heal();
            if (touch != null)
            {
                touch.ForceVisible = true;
                TrashCollectible start = NearestTrash(Pos + new Vector2(0f, 12f));
                if (start != null) Teleport((Vector2)start.transform.position + new Vector2(-6f, 2f));
                yield return Wait(0.8f);
                yield return Record("10_tactil", 4.5f, t =>
                {
                    TrashCollectible n = NearestTrash(Pos);
                    Vector2 d = n != null ? (Vector2)n.transform.position - Pos : Vector2.right;
                    touch.Preview(d.normalized, t > 2.2f && t < 3.6f);
                });
                touch.StopPreview();
                touch.ForceVisible = false;
            }

            Finish();
        }

        void Finish()
        {
            Time.captureFramerate = 0;
            Axis(Vector2.zero);
            EcoInput.VirtualBoost = false;
            AudioListener.volume = 1f;
            File.WriteAllText(Path.Combine(dir, "listo.txt"), log.ToString());
            Debug.Log("[ECO SHARK] Clips del tráiler guardados en: " + dir);
            UnityEditor.EditorApplication.isPlaying = false;
        }

        // ---------------------------------------------------------------- Grabación

        IEnumerator Wait(float seconds)
        {
            for (int i = 0, n = Mathf.CeilToInt(seconds * Fps); i < n; i++) yield return null;
        }

        /// <summary>Graba "seconds" de juego en Capturas/trailer/name.mp4. "drive" se llama cada cuadro con el tiempo de la toma.</summary>
        IEnumerator Record(string name, float seconds, System.Action<float> drive)
        {
            int w = Screen.width & ~1, h = Screen.height & ~1;
            if (frame == null || frame.width != w || frame.height != h) frame = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var video = new VideoTrackAttributes
            {
                frameRate = new MediaRational(Fps),
                width = (uint)w,
                height = (uint)h,
                includeAlpha = false,
                bitRateMode = UnityEditor.VideoBitrateMode.High
            };
            string path = Path.Combine(dir, name + ".mp4");
            int frames = Mathf.CeilToInt(seconds * Fps);
            var end = new WaitForEndOfFrame();
            using (var encoder = new MediaEncoder(path, video))
            {
                for (int i = 0; i < frames; i++)
                {
                    drive?.Invoke(i / (float)Fps);
                    yield return end;
                    frame.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
                    frame.Apply(false);
                    encoder.AddFrame(frame);
                }
            }
            Axis(Vector2.zero);
            log.AppendLine(name + ".mp4  " + frames + " cuadros  " + w + "x" + h);
        }

        // ---------------------------------------------------------------- Piloto automático

        void Axis(Vector2 v) => EcoInput.VirtualAxis = Vector2.ClampMagnitude(v, 1f);

        void SteerTo(Vector2 target, float stopDistance)
        {
            Vector2 d = target - Pos;
            float m = d.magnitude;
            Axis(m < stopDistance ? Vector2.zero : d / m * Mathf.Clamp01(m / 1.5f + 0.25f));
        }

        void Teleport(Vector2 p)
        {
            OceanArea area = OceanArea.Current;
            if (area != null)
            {
                p.x = Mathf.Clamp(p.x, area.Min.x + 14f, area.Max.x - 14f);
                p.y = Mathf.Clamp(p.y, area.FloorY + 2f, area.SurfaceY - 2f);
            }
            if (rb == null) return;
            rb.position = p;
            rb.transform.position = p;
            rb.linearVelocity = Vector3.zero;
            rb.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
            Physics.SyncTransforms();
            if (follow != null) follow.Snap();
        }

        void Heal()
        {
            if (health != null) health.Heal(health.Max);
        }

        TrappedAnimal BestAnimal()
        {
            TrappedAnimal best = null;
            float bestScore = -1f;
            OceanArea area = OceanArea.Current;
            foreach (TrappedAnimal a in FindObjectsByType<TrappedAnimal>(FindObjectsSortMode.None))
            {
                if (a == null || a.IsFinished) continue;
                float score = a.TimeLeft01;
                // Mejor lejos de la superficie (barcos) y de los bordes.
                if (area != null && a.transform.position.y > area.SurfaceY - 7f) score -= 0.3f;
                if (score > bestScore) { bestScore = score; best = a; }
            }
            return best;
        }

        TrashCollectible NearestTrash(Vector2 from)
        {
            TrashCollectible best = null;
            float bestSq = float.MaxValue;
            foreach (TrashCollectible t in FindObjectsByType<TrashCollectible>(FindObjectsSortMode.None))
            {
                if (t == null || !t.CanBeCollected) continue;
                float sq = ((Vector2)t.transform.position - from).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = t; }
            }
            return best;
        }

        T Nearest<T>(Vector2 from) where T : Component
        {
            T best = null;
            float bestSq = float.MaxValue;
            foreach (T t in FindObjectsByType<T>(FindObjectsSortMode.None))
            {
                float sq = ((Vector2)t.transform.position - from).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = t; }
            }
            return best;
        }
    }
}
#endif
