using System.IO;
using UnityEditor;
using UnityEngine;

namespace EcoShark.EditorScripts
{
    /// <summary>
    /// Saca fotos del tiburón (perfil, 3/4 y desde arriba) a la carpeta "Capturas" del proyecto.
    /// Útil para revisar colores y para el informe o la presentación.
    /// Menú: ECO SHARK > 4. Fotografiar tiburón (capturas)
    /// </summary>
    public static class SharkSnapshot
    {
        const string RequestFile = "Capturas/pedir_fotos.txt";

        // Si existe Capturas/pedir_fotos.txt al recargar Unity, toma las fotos y borra el archivo.
        // (Permite que una IA pida fotos sin usar los menús.)
        // Palabras extra en el archivo: "pintar" = volver a pintar el modelo anterior; "cartoon" = instalar el tiburón cartoon (Antigravity); "tripo" = instalar el tiburón de Tripo; "cielo" = instalar cielo y olas; "audio" = preparar sonido; "fondo" = configurar fondo marino (+ capturas); "capturas fondo" = solo capturas; "oceano" = océano grande; "menu" = menú principal y HUD nuevo.
        [InitializeOnLoadMethod]
        static void CheckRequest()
        {
            EditorApplication.delayCall += () =>
            {
                string req = Path.GetFullPath(Path.Combine(Application.dataPath, "..", RequestFile));
                if (!File.Exists(req) || EditorApplication.isPlayingOrWillChangePlaymode) return;
                string order = File.ReadAllText(req);
                File.Delete(req);
                if (HasLine(order, "cartoon"))
                {
                    if (SharkCartoonInstaller.Install(out string rep))
                        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                    else
                        Debug.LogWarning("[ECO SHARK] " + rep);
                }
                if (HasLine(order, "tripo"))
                {
                    if (SharkCartoonInstaller.InstallTripo(out string rep))
                        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                    else
                        Debug.LogWarning("[ECO SHARK] " + rep);
                }
                if (HasLine(order, "cielo"))
                {
                    if (OceanSurfaceInstaller.Install(out string rep))
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                        Debug.Log("[ECO SHARK] " + rep);
                    }
                    else
                        Debug.LogWarning("[ECO SHARK] " + rep);
                }
                if (HasLine(order, "audio"))
                {
                    EcoAudioInstaller.Install(out string rep);
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                    Debug.Log("[ECO SHARK] " + rep);
                }
                if (HasLine(order, "fondo"))
                {
                    if (SeaBackgroundInstaller.Install(out string rep))
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                        Debug.Log("[ECO SHARK] " + rep + " Capturas: " + SeaBackgroundInstaller.CaptureViews());
                        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                    }
                    else
                        Debug.LogWarning("[ECO SHARK] " + rep);
                }
                if (HasLine(order, "oceano"))
                {
                    Debug.Log("[ECO SHARK] " + GameShellInstaller.ExpandOcean());
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                    Debug.Log("[ECO SHARK] Capturas: " + SeaBackgroundInstaller.CaptureViews());
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                }
                if (HasLine(order, "menu"))
                {
                    Debug.Log("[ECO SHARK] " + GameShellInstaller.InstallMenu());
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                }
                if (HasLine(order, "mejoras")) // (Claude) controles táctiles, guía de rescates, misiones y peces
                {
                    Debug.Log("[ECO SHARK] " + GameplayUpgradeInstaller.Install());
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                }
                if (HasLine(order, "tactil")) // (Claude) muestra/oculta los controles táctiles en el editor
                {
                    Debug.Log("[ECO SHARK] " + GameplayUpgradeInstaller.ToggleTouchPreview());
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                }
                if (HasLine(order, "capturas fondo"))
                    Debug.Log("[ECO SHARK] Capturas: " + SeaBackgroundInstaller.CaptureViews());
                ApplyOrderLines(order);
                if (HasLine(order, "pintar"))
                {
                    Transform model = SharkColorizer.FindModel();
                    if (model != null && SharkColorizer.Apply(model))
                        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                }
                Snap();
            };
        }

        /// <summary>True si alguna línea del pedido es exactamente esa palabra (evita confundir nombres de archivo).</summary>
        static bool HasLine(string order, string word)
        {
            foreach (string raw in order.Split('\n'))
                if (raw.Trim().ToLowerInvariant() == word) return true;
            return false;
        }

        /// <summary>
        /// Líneas extra del pedido:
        ///   "Componente.campo=valor"  -> cambia un número de un componente de Tiburon_Guardian o sus hijos (ej. SharkSwimWave.amplitude=0.55)
        ///   "borrar:Assets/ruta.ext"  -> manda ese archivo a la Papelera de reciclaje (se puede recuperar)
        /// </summary>
        static void ApplyOrderLines(string order)
        {
            bool changed = false;
            foreach (string raw in order.Split('\n'))
            {
                string line = raw.Trim();
                if (line.StartsWith("borrar:"))
                {
                    string path = line.Substring(7).Trim();
                    bool ok = AssetDatabase.MoveAssetToTrash(path);
                    Debug.Log("[ECO SHARK] " + (ok ? "Enviado a la Papelera: " : "No se pudo borrar: ") + path);
                    continue;
                }

                if (line.StartsWith("transform:"))
                {
                    // transform:Ruta/Objeto|pos=x,y,z|scale=x,y,z
                    string[] parts = line.Substring(10).Split('|');
                    GameObject go = GameObject.Find(parts[0].Trim());
                    if (go == null) { Debug.LogWarning("[ECO SHARK] No existe " + parts[0]); continue; }
                    for (int k = 1; k < parts.Length; k++)
                    {
                        string[] kv = parts[k].Split('=');
                        if (kv.Length != 2) continue;
                        string[] n = kv[1].Split(',');
                        if (n.Length != 3) continue;
                        var ci = System.Globalization.CultureInfo.InvariantCulture;
                        var vec = new Vector3(float.Parse(n[0], ci), float.Parse(n[1], ci), float.Parse(n[2], ci));
                        if (kv[0].Trim() == "pos") go.transform.localPosition = vec;
                        else if (kv[0].Trim() == "scale") go.transform.localScale = vec;
                    }
                    Debug.Log("[ECO SHARK] Transform ajustado: " + parts[0]);
                    changed = true;
                    continue;
                }

                int eq = line.IndexOf('=');
                int dot = line.IndexOf('.');
                if (eq <= 0 || dot <= 0 || dot > eq) continue;

                string compName = line.Substring(0, dot);
                string field = line.Substring(dot + 1, eq - dot - 1);
                if (!float.TryParse(line.Substring(eq + 1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value)) continue;

                GameObject model = GameObject.Find("Tiburon_Guardian");
                if (model == null) continue;
                foreach (MonoBehaviour mb in model.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null || mb.GetType().Name != compName) continue;
                    var so = new SerializedObject(mb);
                    SerializedProperty prop = so.FindProperty(field);
                    if (prop == null) { Debug.LogWarning("[ECO SHARK] No existe " + compName + "." + field); break; }
                    prop.floatValue = value;
                    so.ApplyModifiedProperties();
                    Debug.Log("[ECO SHARK] " + compName + "." + field + " = " + value);
                    changed = true;
                    break;
                }
            }
            if (changed)
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        [MenuItem("ECO SHARK/4. Fotografiar tiburón (capturas)", false, 3)]
        static void Snap()
        {
            GameObject shark = GameObject.Find("Tiburon_Guardian");
            if (shark == null) { Debug.LogWarning("[ECO SHARK] No hay Tiburon_Guardian en la escena."); return; }

            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Capturas"));
            Directory.CreateDirectory(dir);

            Transform t = shark.transform;
            Vector3 c = t.position;
            // Perfil = como se ve en el juego (de costado). 3/4 = adelante-arriba. Arriba = lomo.
            Render(shark, c + t.right * 6f + t.up * 0.3f, c, Path.Combine(dir, "tiburon_perfil.png"));
            Render(shark, c + t.right * 4f + t.forward * 3.5f + t.up * 1.8f, c, Path.Combine(dir, "tiburon_tres_cuartos.png"));
            Render(shark, c + t.up * 6f - t.forward * 1.5f, c, Path.Combine(dir, "tiburon_arriba.png"));
            WriteReport(shark, Path.Combine(dir, "reporte.txt"));
            Debug.Log("[ECO SHARK] Fotos guardadas en: " + dir);
        }

        /// <summary>Texto con posición, tamaño y material de cada parte (para diagnosticar).</summary>
        static void WriteReport(GameObject shark, string path)
        {
            var sb = new System.Text.StringBuilder();
            Transform t = shark.transform;
            sb.AppendLine("Tiburon_Guardian pos=" + t.position.ToString("F2") + " rot=" + t.eulerAngles.ToString("F1"));
            foreach (Transform child in t) sb.AppendLine("  hijo " + child.name + " localRot=" + child.localEulerAngles.ToString("F1") + " localScale=" + child.localScale.ToString("F2"));
            foreach (Renderer r in shark.GetComponentsInChildren<Renderer>(true))
            {
                string mat = r.sharedMaterial != null ? r.sharedMaterial.name + " / " + r.sharedMaterial.shader.name : "sin material";
                sb.AppendLine(r.name + " | activo=" + r.gameObject.activeInHierarchy + " | centro=" + r.bounds.center.ToString("F2")
                              + " | tamaño=" + r.bounds.size.ToString("F2") + " | " + mat + " | padre=" + (r.transform.parent != null ? r.transform.parent.name : "-")
                              + " | lossyScale=" + r.transform.lossyScale.ToString("F3"));
            }
            File.WriteAllText(path, sb.ToString());
        }

        static void Render(GameObject shark, Vector3 camPos, Vector3 target, string path)
        {
            var go = new GameObject("~CamaraFoto") { hideFlags = HideFlags.HideAndDontSave };
            Camera cam = go.AddComponent<Camera>();
            cam.transform.position = camPos;
            cam.transform.LookAt(target);
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fog ? RenderSettings.fogColor : new Color(0.04f, 0.26f, 0.4f);

            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            File.WriteAllBytes(path, tex.EncodeToPNG());

            cam.targetTexture = null;
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
        }
    }
}
