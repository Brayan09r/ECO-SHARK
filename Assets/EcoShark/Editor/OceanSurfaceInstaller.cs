using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace EcoShark.EditorScripts
{
    /// <summary>
    /// Instala el cielo con nubes y las olas animadas (estilo Hungry Shark).
    /// Menú: ECO SHARK > 7. Cielo y olas. También con la línea "cielo" en Capturas/pedir_fotos.txt.
    ///  - Desactiva (no borra) "Entorno/Superficie" y "Entorno/Cielo" (los anteriores).
    ///  - Crea "Entorno/Olas" (OceanSurface) en el plano del juego, justo detrás del tiburón.
    ///  - Crea "Main Camera/FondoCielo" (SkyBackdrop): cielo arriba y agua con degradado abajo.
    ///  - Deja ver más cielo sobre la superficie (CameraFollow.extraAboveSurface).
    ///  - Hace flotar el barco con las olas (FloatOnWaves en su "Modelo").
    /// </summary>
    public static class OceanSurfaceInstaller
    {
        const string WaterMatPath = "Assets/EcoShark/Generated/Materials/Olas.mat";
        const string SkyMatPath = "Assets/EcoShark/Generated/Materials/FondoCielo.mat";
        const float WavesZ = 0.6f;          // detrás del tiburón (z = 0)
        const float ExtraSky = 4.5f;        // unidades de cielo visibles sobre la superficie

        static readonly Color WaterTop = new Color(0.1f, 0.42f, 0.6f);

        [MenuItem("ECO SHARK/7. Cielo y olas", false, 7)]
        static void InstallMenu()
        {
            if (Install(out string report))
            {
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                Debug.Log("[ECO SHARK] " + report);
            }
            else Debug.LogWarning("[ECO SHARK] " + report);
        }

        public static bool Install(out string report)
        {
            Material water = GetMaterial(WaterMatPath, "EcoShark/WaterSurface");
            Material sky = GetMaterial(SkyMatPath, "EcoShark/SkyBackdrop");
            if (water == null || sky == null)
            {
                report = "No se encontraron los shaders EcoShark/WaterSurface o EcoShark/SkyBackdrop.";
                return false;
            }
            water.SetColor("_WaterTop", WaterTop);
            sky.SetColor("_WaterTop", WaterTop);
            EditorUtility.SetDirty(water);
            EditorUtility.SetDirty(sky);

            OceanArea area = Object.FindFirstObjectByType<OceanArea>();
            float surfaceY = area != null ? area.Max.y : 6f;

            // 1) Apagar la superficie y el cielo anteriores.
            GameObject envGO = GameObject.Find("Entorno");
            Transform env = envGO != null ? envGO.transform : null;
            if (env != null)
            {
                foreach (string old in new[] { "Superficie", "Cielo" })
                {
                    Transform t = env.Find(old);
                    if (t != null) t.gameObject.SetActive(false);
                }
            }

            // 2) Olas.
            if (env != null)
            {
                Transform prev = env.Find("Olas");
                if (prev != null) Object.DestroyImmediate(prev.gameObject);
            }
            var waves = new GameObject("Olas", typeof(MeshFilter), typeof(MeshRenderer));
            if (env != null) waves.transform.SetParent(env, true);
            waves.transform.position = new Vector3(0f, surfaceY, WavesZ);
            MeshRenderer wr = waves.GetComponent<MeshRenderer>();
            wr.sharedMaterial = water;
            wr.shadowCastingMode = ShadowCastingMode.Off;
            wr.receiveShadows = false;
            waves.AddComponent<OceanSurface>();

            // 3) Fondo con cielo, pegado a la cámara.
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camGO = GameObject.Find("Main Camera");
                if (camGO != null) cam = camGO.GetComponent<Camera>();
            }
            if (cam == null)
            {
                report = "No hay cámara principal en la escena.";
                return false;
            }
            Transform oldSky = cam.transform.Find("FondoCielo");
            if (oldSky != null) Object.DestroyImmediate(oldSky.gameObject);
            var backdrop = new GameObject("FondoCielo", typeof(MeshFilter), typeof(MeshRenderer));
            backdrop.transform.SetParent(cam.transform, false);
            MeshRenderer br = backdrop.GetComponent<MeshRenderer>();
            br.sharedMaterial = sky;
            br.shadowCastingMode = ShadowCastingMode.Off;
            br.receiveShadows = false;
            backdrop.AddComponent<SkyBackdrop>();

            // 4) Más cielo visible al subir.
            CameraFollow follow = cam.GetComponent<CameraFollow>();
            if (follow != null)
            {
                var so = new SerializedObject(follow);
                SerializedProperty p = so.FindProperty("extraAboveSurface");
                if (p != null) { p.floatValue = ExtraSky; so.ApplyModifiedProperties(); }
            }

            // 5) El barco flota con las olas.
            int boats = 0;
            foreach (IllegalFishingBoat boat in Object.FindObjectsByType<IllegalFishingBoat>(FindObjectsSortMode.None))
            {
                Transform model = boat.transform.Find("Modelo");
                if (model == null) continue;
                if (model.GetComponent<FloatOnWaves>() == null) model.gameObject.AddComponent<FloatOnWaves>();
                boats++;
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            report = "Cielo y olas instalados (superficie en y=" + surfaceY + ", barcos flotando: " + boats + ").";
            return true;
        }

        static Material GetMaterial(string path, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) return null;
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader) m.shader = shader;
            return m;
        }
    }
}
