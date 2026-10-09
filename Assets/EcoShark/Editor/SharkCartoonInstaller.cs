using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EcoShark.EditorScripts
{
    /// <summary>
    /// Pone el tiburón cartoon de Antigravity (Shark_Hammerhead_Cartoon.fbx) en el juego:
    ///  1. Oculta el modelo anterior (queda como respaldo, no se borra).
    ///  2. Instancia el modelo nuevo como hijo de Tiburon_Guardian y lo orienta solo
    ///     (nariz hacia +Z = la cabeza es el extremo más ancho, por el martillo).
    ///  3. Crea el material con el shader EcoShark/CartoonSwim y las texturas.
    ///  4. Agrega SharkSwimWave (nado sin huesos) y ajusta el collider y la boca.
    /// Menú: ECO SHARK > 5. Usar tiburón cartoon (nuevo)
    /// </summary>
    public static class SharkCartoonInstaller
    {
        // Modelo de Antigravity (Blender).
        const string FbxPath = "Assets/EcoShark/Models/Sharks/Shark_Hammerhead_Cartoon.fbx";
        const string AlbedoPath = "Assets/EcoShark/Models/Sharks/Textures/Shark_Albedo.png";
        const string EmissionPath = "Assets/EcoShark/Models/Sharks/Textures/Shark_Emission.png";
        const string MaterialPath = "Assets/EcoShark/Generated/Materials/SharkCartoon.mat";

        // Modelo de Tripo (reducido de 1.9 M a ~15 mil triángulos por Claude).
        const string TripoPath = "Assets/EcoShark/Models/Sharks/Tripo/Tiburon_Tripo.obj";
        const string TripoAlbedoPath = "Assets/EcoShark/Models/Sharks/Tripo/Tiburon_Tripo_Albedo.png";
        const string TripoEmissionPath = "Assets/EcoShark/Models/Sharks/Tripo/Tiburon_Tripo_Emission.png";
        const string TripoMaterialPath = "Assets/EcoShark/Generated/Materials/SharkTripo.mat";
        const string ShaderName = "EcoShark/CartoonSwim";
        public const string ChildName = "Tiburon_Cartoon";

        [MenuItem("ECO SHARK/5. Usar tiburón cartoon (nuevo)", false, 4)]
        static void InstallFromMenu()
        {
            if (Install(out string report))
            {
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                EditorUtility.DisplayDialog("ECO SHARK", "¡Tiburón cartoon listo!\n\n" + report + "\n\nDale Play para verlo nadar.", "¡Genial!");
            }
            else
            {
                EditorUtility.DisplayDialog("ECO SHARK", report, "Aceptar");
            }
        }

        [MenuItem("ECO SHARK/6. Usar tiburón de Tripo", false, 5)]
        static void InstallTripoFromMenu()
        {
            if (InstallTripo(out string report))
            {
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                EditorUtility.DisplayDialog("ECO SHARK", "¡Tiburón de Tripo listo!\n\n" + report + "\n\nDale Play para verlo nadar.", "¡Genial!");
            }
            else
            {
                EditorUtility.DisplayDialog("ECO SHARK", report, "Aceptar");
            }
        }

        public static bool Install(out string report) =>
            InstallModel(FbxPath, AlbedoPath, EmissionPath, MaterialPath, true, out report);

        public static bool InstallTripo(out string report) =>
            InstallModel(TripoPath, TripoAlbedoPath, TripoEmissionPath, TripoMaterialPath, false, out report); // ya viene orientado (nariz +Z, lomo +Y)

        static bool InstallModel(string modelPath, string albedoPath, string emissionPath, string materialPath, bool autoOrient, out string report)
        {
            GameObject shark = GameObject.Find("Tiburon_Guardian");
            if (shark == null) { report = "No encontré 'Tiburon_Guardian' en la escena abierta."; return false; }

            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (fbx == null) { report = "No encontré " + modelPath; return false; }

            Shader shader = Shader.Find(ShaderName);
            if (shader == null) { report = "No encontré el shader " + ShaderName; return false; }

            Transform root = shark.transform;

            // 1) Quitar una instalación anterior y ocultar los modelos viejos (respaldo).
            Transform previous = root.Find(ChildName);
            if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
            foreach (string oldName in new[] { "Tiburon_Martillo_3D", "Modelo (reemplazar por tu 3D)" })
            {
                Transform old = root.Find(oldName);
                if (old != null) { Undo.RecordObject(old.gameObject, "Ocultar modelo anterior"); old.gameObject.SetActive(false); }
            }

            // 2) Instanciar y orientar.
            var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            Undo.RegisterCreatedObjectUndo(model, "Tiburón cartoon");
            model.name = ChildName;
            model.transform.SetParent(root, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            string orientation = autoOrient ? Orient(model.transform, root) : "definida en el archivo (nariz +Z, lomo +Y)";

            // Centrar el modelo en el tiburón.
            Bounds b = RootBounds(model, root);
            model.transform.localPosition -= b.center;
            b = RootBounds(model, root);

            // 3) Material.
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, materialPath);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }
            // Ajustes de look calibrados con las fotos (colores fieles al render de Blender).
            mat.SetColor("_ShadowColor", new Color(0.62f, 0.68f, 0.78f));
            mat.SetFloat("_SelfLight", 0.4f);
            mat.SetFloat("_LightTint", 0.3f);
            mat.SetFloat("_RimStrength", 0.2f);
            mat.SetFloat("_EmissionStrength", 1.3f);
            mat.SetFloat("_OutlineWidth", 0.012f); // más fino: evita puntitos negros donde la malla es irregular
            mat.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath));
            mat.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(emissionPath));
            EditorUtility.SetDirty(mat);

            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterial = mat;

            // 4) Nado, collider y boca.
            if (model.GetComponent<SharkSwimWave>() == null) Undo.AddComponent<SharkSwimWave>(model);
            if (model.GetComponent<SharkViewTilt>() == null) Undo.AddComponent<SharkViewTilt>(model);

            CapsuleCollider col = shark.GetComponent<CapsuleCollider>();
            if (col != null)
            {
                Undo.RecordObject(col, "Collider tiburón");
                col.direction = 2;
                col.center = Vector3.zero;
                col.height = b.size.z * 0.95f;
                col.radius = Mathf.Clamp(b.size.y * 0.35f, 0.3f, 0.8f);
            }

            Transform mouth = root.Find("Boca");
            if (mouth != null)
            {
                Undo.RecordObject(mouth, "Boca");
                mouth.localPosition = new Vector3(0f, -0.15f * b.size.y, b.max.z - 0.1f * b.size.z);
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(shark.scene);

            report = "Orientación: " + orientation + "\nTamaño (ancho x alto x largo): " + b.size.ToString("F2");
            Debug.Log("[ECO SHARK] Tiburón cartoon instalado. " + report.Replace("\n", " | "));
            return true;
        }

        // ------------------------------------------------------------------ orientación automática

        static string Orient(Transform model, Transform root)
        {
            var steps = new List<string>();

            // a) El eje más largo del modelo debe ser Z (a lo largo del cuerpo).
            Vector3 size = RootBounds(model.gameObject, root).size;
            if (size.x > size.z && size.x >= size.y) { Rotate(model, Quaternion.Euler(0f, 90f, 0f)); steps.Add("largo en X → Z"); }
            else if (size.y > size.z && size.y > size.x) { Rotate(model, Quaternion.Euler(90f, 0f, 0f)); steps.Add("largo en Y → Z"); }

            // b) En la cabeza, el martillo es más ancho (X) que alto (Y).
            SliceExtents(model, root, true, out float headW, out float headH, out _, out _);
            if (headH > headW) { Rotate(model, Quaternion.Euler(0f, 0f, 90f)); steps.Add("martillo girado 90°"); }

            // c) La cabeza (extremo más ancho) debe quedar en +Z.
            SliceExtents(model, root, true, out float frontW, out _, out _, out _);
            SliceExtents(model, root, false, out float backW, out _, out _, out _);
            if (backW > frontW) { Rotate(model, Quaternion.Euler(0f, 180f, 0f)); steps.Add("volteado 180° (cabeza adelante)"); }

            // d) Lomo arriba: la aleta dorsal hace que el cuerpo sobresalga más hacia arriba que hacia abajo en la mitad.
            MidExtents(model, root, out float up, out float down);
            if (down > up * 1.35f) { Rotate(model, Quaternion.Euler(0f, 0f, 180f)); steps.Add("volteado (lomo arriba)"); }

            return steps.Count == 0 ? "ya venía correcta (nariz +Z, lomo +Y)" : string.Join(", ", steps);
        }

        static void Rotate(Transform model, Quaternion delta)
        {
            model.localRotation = delta * model.localRotation;
        }

        static List<Vector3> RootPoints(GameObject model, Transform root)
        {
            var points = new List<Vector3>();
            Matrix4x4 toRoot = root.worldToLocalMatrix;
            foreach (MeshFilter mf in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                Matrix4x4 m = toRoot * mf.transform.localToWorldMatrix;
                foreach (Vector3 v in mf.sharedMesh.vertices) points.Add(m.MultiplyPoint3x4(v));
            }
            foreach (SkinnedMeshRenderer smr in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var baked = new Mesh();
                smr.BakeMesh(baked);
                Matrix4x4 m = toRoot * Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
                foreach (Vector3 v in baked.vertices) points.Add(m.MultiplyPoint3x4(v));
                Object.DestroyImmediate(baked);
            }
            return points;
        }

        static Bounds RootBounds(GameObject model, Transform root)
        {
            List<Vector3> pts = RootPoints(model, root);
            if (pts.Count == 0) return new Bounds(Vector3.zero, Vector3.one);
            var b = new Bounds(pts[0], Vector3.zero);
            foreach (Vector3 p in pts) b.Encapsulate(p);
            return b;
        }

        /// <summary>Ancho (X) y alto (Y) del 20 % delantero (+Z) o trasero (-Z) del cuerpo.</summary>
        static void SliceExtents(Transform model, Transform root, bool front, out float width, out float height, out float minY, out float maxY)
        {
            List<Vector3> pts = RootPoints(model.gameObject, root);
            float minZ = float.PositiveInfinity, maxZ = float.NegativeInfinity;
            foreach (Vector3 p in pts) { minZ = Mathf.Min(minZ, p.z); maxZ = Mathf.Max(maxZ, p.z); }
            float len = maxZ - minZ;
            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            minY = float.PositiveInfinity; maxY = float.NegativeInfinity;
            foreach (Vector3 p in pts)
            {
                bool inSlice = front ? p.z > maxZ - 0.2f * len : p.z < minZ + 0.2f * len;
                if (!inSlice) continue;
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            width = maxX - minX;
            height = maxY - minY;
        }

        /// <summary>Cuánto sobresale el cuerpo hacia arriba y hacia abajo en la parte central.</summary>
        static void MidExtents(Transform model, Transform root, out float up, out float down)
        {
            List<Vector3> pts = RootPoints(model.gameObject, root);
            float minZ = float.PositiveInfinity, maxZ = float.NegativeInfinity;
            foreach (Vector3 p in pts) { minZ = Mathf.Min(minZ, p.z); maxZ = Mathf.Max(maxZ, p.z); }
            float len = maxZ - minZ, mid = (minZ + maxZ) * 0.5f;
            var ys = new List<float>();
            foreach (Vector3 p in pts) if (Mathf.Abs(p.z - mid) < 0.15f * len) ys.Add(p.y);
            if (ys.Count == 0) { up = down = 1f; return; }
            ys.Sort();
            float median = ys[ys.Count / 2];
            up = ys[ys.Count - 1] - median;
            down = median - ys[0];
        }
    }
}
