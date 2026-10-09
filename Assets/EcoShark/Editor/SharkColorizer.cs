using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EcoShark.EditorScripts
{
    /// <summary>
    /// Pinta el tiburón martillo 3D con los colores del diseño
    /// (Art/Designs/Diseno_Tiburon_Martillo_Perfil_2.5D.png):
    ///   lomo gris azulado · costados gris claro · vientre crema · marcas cian brillantes · ojos naranjas.
    ///
    /// Cómo funciona (contrasombreado, como los tiburones reales):
    ///   cada vértice mira hacia arriba o hacia abajo (su normal) y está más alto o más bajo en el cuerpo;
    ///   con eso se mezcla entre el color del lomo y el del vientre.
    ///   Las marcas cian que pintó Antigravity en Blender se conservan y se hacen brillar.
    ///
    /// No toca el .fbx original: crea una copia pintada en Generated/Meshes.
    /// Menú: ECO SHARK > 3. Pintar tiburón 3D (colores del diseño)
    /// </summary>
    public static class SharkColorizer
    {
        public const string MeshPath = "Assets/EcoShark/Generated/Meshes/Shark_Hammerhead_Pintado.asset";
        public const string BodyMaterialPath = "Assets/EcoShark/Generated/Materials/SharkVCol.mat";
        const string EyeMaterialPath = "Assets/EcoShark/Generated/Materials/OjoTiburon.mat";
        const string ShaderName = "EcoShark/VertexColorLit";
        const string EyePrefix = "Ojo_";

        // ---------- Paleta (sRGB). Cambia estos valores para ajustar el look ----------
        static readonly Color Dorsal = Hex(0x52687A);   // lomo gris azulado
        static readonly Color Flank = Hex(0x7E919E);    // costados
        static readonly Color Belly = Hex(0xF3ECDA);    // vientre crema
        static readonly Color Marking = Hex(0x5BE3FF);  // marcas bioluminiscentes
        static readonly Color EyeColor = Hex(0xFF9A1F); // ojos

        [MenuItem("ECO SHARK/3. Pintar tiburón 3D (colores del diseño)", false, 2)]
        static void PaintFromMenu()
        {
            Transform model = FindModel();
            if (model == null)
            {
                EditorUtility.DisplayDialog("ECO SHARK", "No encontré 'Tiburon_Guardian/Tiburon_Martillo_3D' en la escena abierta.\nUsa primero 'ECO SHARK > 2. Equipar Modelo Tiburón Martillo 3D'.", "Aceptar");
                return;
            }

            if (Apply(model))
            {
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                EditorUtility.DisplayDialog("ECO SHARK", "¡Tiburón pintado con los colores del diseño!\n\nAjustes finos: selecciona el material 'SharkVCol' (brillo de marcas, borde, tinte).", "¡Genial!");
            }
        }

        public static Transform FindModel()
        {
            GameObject shark = GameObject.Find("Tiburon_Guardian");
            return shark != null ? shark.transform.Find("Tiburon_Martillo_3D") : null;
        }

        /// <summary>True si el modelo ya tiene la malla pintada y los ojos (lo usa AutoFixShark para no repetir trabajo).</summary>
        public static bool IsPainted(Transform model)
        {
            if (model == null) return false;
            Mesh mesh = GetMesh(model, out _, out _);
            bool meshOk = mesh != null && AssetDatabase.GetAssetPath(mesh) == MeshPath;
            bool eyesOk = FindDeep(model, EyePrefix + "Izq") != null;
            return meshOk && eyesOk;
        }

        public static bool Apply(Transform model)
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError("[ECO SHARK] No se encontró el shader '" + ShaderName + "'. Revisa que exista Assets/EcoShark/Shaders/EcoShark_VertexColorLit.shader.");
                return false;
            }

            Mesh source = GetMesh(model, out SkinnedMeshRenderer smr, out MeshFilter mf);
            if (source == null)
            {
                Debug.LogError("[ECO SHARK] El modelo no tiene SkinnedMeshRenderer ni MeshFilter.");
                return false;
            }

            Transform root = model.parent != null ? model.parent : model;
            bool alreadyPainted = AssetDatabase.GetAssetPath(source) == MeshPath;

            // 1) Posiciones y normales en el espacio del tiburón (Y = lomo, Z = hacia la cabeza).
            Vector3[] verts;
            Vector3[] norms;
            Matrix4x4 toRoot;
            if (smr != null)
            {
                var baked = new Mesh();
                smr.BakeMesh(baked);
                verts = baked.vertices;
                norms = baked.normals;
                Object.DestroyImmediate(baked);
                // BakeMesh devuelve vértices SIN la escala del renderer (el FBX viene con escala 100):
                // por eso se usa posición y rotación, pero escala 1.
                toRoot = root.worldToLocalMatrix * Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
            }
            else
            {
                verts = source.vertices;
                norms = source.normals;
                toRoot = root.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            }

            int count = verts.Length;
            if (count != source.vertexCount || norms.Length != count)
            {
                Debug.LogError("[ECO SHARK] La malla no coincide con la pose; no se pintó.");
                return false;
            }

            var p = new Vector3[count];
            var n = new Vector3[count];
            Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
            for (int i = 0; i < count; i++)
            {
                p[i] = toRoot.MultiplyPoint3x4(verts[i]);
                n[i] = toRoot.MultiplyVector(norms[i]).normalized;
                min = Vector3.Min(min, p[i]);
                max = Vector3.Max(max, p[i]);
            }

            // 2) Altura relativa por "rebanadas" a lo largo del cuerpo (-1 = vientre, +1 = lomo).
            const int bins = 12;
            float length = Mathf.Max(0.0001f, max.z - min.z);
            var binMin = new float[bins];
            var binMax = new float[bins];
            for (int b = 0; b < bins; b++) { binMin[b] = float.PositiveInfinity; binMax[b] = float.NegativeInfinity; }
            var binOf = new int[count];
            for (int i = 0; i < count; i++)
            {
                int b = Mathf.Clamp((int)((p[i].z - min.z) / length * bins), 0, bins - 1);
                binOf[i] = b;
                binMin[b] = Mathf.Min(binMin[b], p[i].y);
                binMax[b] = Mathf.Max(binMax[b], p[i].y);
            }

            // 3) Colores.
            Color[] original = source.colors;
            bool hasOriginal = original != null && original.Length == count;
            bool linear = PlayerSettings.colorSpace == ColorSpace.Linear;
            var colors = new Color[count];
            int markings = 0;

            for (int i = 0; i < count; i++)
            {
                int b = binOf[i];
                float range = binMax[b] - binMin[b];
                float h = range > 0.0001f ? (p[i].y - binMin[b]) / range * 2f - 1f : 0f;
                float s = n[i].y * 0.75f + h * 0.45f;

                // Umbrales: la mitad baja del cuerpo queda crema; el lomo, gris azulado.
                Color c = Color.Lerp(Belly, Flank, Smooth(-0.05f, 0.12f, s));
                c = Color.Lerp(c, Dorsal, Smooth(0.12f, 0.35f, s));

                bool isMarking = false;
                if (hasOriginal)
                {
                    Color o = original[i];
                    isMarking = alreadyPainted ? o.a > 0.5f : (o.g > 0.8f && o.b > 0.8f && o.r < 0.55f);
                }
                if (isMarking)
                {
                    c = Color.Lerp(c, Marking, 0.6f);
                    markings++;
                }

                if (linear) c = c.linear;
                c.a = isMarking ? 1f : 0f; // el alfa le dice al shader qué brilla
                colors[i] = c;
            }

            // 4) Guardar la malla pintada (copia; el .fbx no se toca).
            EnsureFolder(Path.GetDirectoryName(MeshPath).Replace('\\', '/'));
            Mesh painted;
            if (alreadyPainted)
            {
                painted = source;
                painted.colors = colors;
                EditorUtility.SetDirty(painted);
            }
            else
            {
                Mesh copy = Object.Instantiate(source);
                copy.name = "Shark_Hammerhead_Pintado";
                copy.colors = colors;
                painted = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
                if (painted != null)
                {
                    EditorUtility.CopySerialized(copy, painted);
                    Object.DestroyImmediate(copy);
                }
                else
                {
                    AssetDatabase.CreateAsset(copy, MeshPath);
                    painted = copy;
                }
            }

            if (smr != null) { Undo.RecordObject(smr, "Pintar tiburón"); smr.sharedMesh = painted; }
            else { Undo.RecordObject(mf, "Pintar tiburón"); mf.sharedMesh = painted; }

            // 5) Material con luz (reemplaza el Sprites/Default sin luz).
            Material body = AssetDatabase.LoadAssetAtPath<Material>(BodyMaterialPath);
            if (body == null)
            {
                body = new Material(shader);
                AssetDatabase.CreateAsset(body, BodyMaterialPath);
                SetBodyDefaults(body);
            }
            else if (body.shader != shader)
            {
                body.shader = shader;
                SetBodyDefaults(body);
            }
            EditorUtility.SetDirty(body);

            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                if (r.gameObject.name.StartsWith(EyePrefix)) continue;
                Undo.RecordObject(r, "Pintar tiburón");
                r.sharedMaterial = body;
            }

            // 6) Ojos en las puntas del martillo, pegados al hueso de la cabeza para que sigan la animación.
            CreateEyes(model, root, p, min, max);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(model.gameObject.scene);
            Debug.Log("[ECO SHARK] Tiburón pintado: " + count + " vértices, " + markings + " con marcas brillantes.");
            return true;
        }

        static void SetBodyDefaults(Material m)
        {
            m.SetColor("_Tint", Color.white);
            m.SetFloat("_Glossiness", 0.35f);
            m.SetFloat("_Metallic", 0f);
            m.SetColor("_GlowColor", Marking);
            m.SetFloat("_GlowStrength", 1.6f);
            m.SetColor("_RimColor", new Color(0.45f, 0.8f, 1f));
            m.SetFloat("_RimPower", 3f);
            m.SetFloat("_RimStrength", 0.3f);
        }

        static void CreateEyes(Transform model, Transform root, Vector3[] p, Vector3 min, Vector3 max)
        {
            // Borrar ojos anteriores.
            for (int k = 0; k < 4; k++)
            {
                Transform old = FindDeep(model, EyePrefix + "Izq") ?? FindDeep(model, EyePrefix + "Der");
                if (old == null) break;
                Undo.DestroyObjectImmediate(old.gameObject);
            }

            float length = max.z - min.z;
            float headStart = max.z - 0.3f * length;
            int left = -1, right = -1;
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i].z < headStart) continue;
                if (right < 0 || p[i].x > p[right].x) right = i;
                if (left < 0 || p[i].x < p[left].x) left = i;
            }
            if (left < 0 || right < 0) return;

            Material eyeMat = EyeMaterial();
            Transform head = FindDeep(model, "Head");
            Transform parent = head != null ? head : model;
            float diameter = 0.03f * length;
            // Un poco hacia adentro para que el ojo quede medio incrustado en la punta del martillo.
            Vector3 inwardL = new Vector3(0.35f * diameter, 0f, 0f);
            Vector3 inwardR = new Vector3(-0.35f * diameter, 0f, 0f);

            MakeEye(EyePrefix + "Izq", root.TransformPoint(p[left] + inwardL), diameter, parent, eyeMat);
            MakeEye(EyePrefix + "Der", root.TransformPoint(p[right] + inwardR), diameter, parent, eyeMat);
            Debug.Log("[ECO SHARK] Ojos en (espacio del tiburón) " + p[left].ToString("F2") + " y " + p[right].ToString("F2") + ", largo del cuerpo " + length.ToString("F2") + ", hueso: " + parent.name);
        }

        static void MakeEye(string name, Vector3 worldPos, float worldDiameter, Transform parent, Material mat)
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = name;
            Object.DestroyImmediate(eye.GetComponent<Collider>());
            eye.GetComponent<Renderer>().sharedMaterial = mat;
            eye.transform.position = worldPos;
            eye.transform.SetParent(parent, true);
            Vector3 ls = parent.lossyScale;
            float scaleFix = Mathf.Max(0.0001f, (Mathf.Abs(ls.x) + Mathf.Abs(ls.y) + Mathf.Abs(ls.z)) / 3f);
            eye.transform.localScale = Vector3.one * (worldDiameter / scaleFix);
            Undo.RegisterCreatedObjectUndo(eye, "Ojos del tiburón");
        }

        static Material EyeMaterial()
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(EyeMaterialPath);
            if (m != null) return m;

            Shader s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
            m = new Material(s);
            m.color = EyeColor;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", EyeColor);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.8f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", EyeColor * 0.5f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            AssetDatabase.CreateAsset(m, EyeMaterialPath);
            return m;
        }

        // ------------------------------------------------------------------ utilidades

        static Mesh GetMesh(Transform model, out SkinnedMeshRenderer smr, out MeshFilter mf)
        {
            smr = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            mf = null;
            if (smr != null) return smr.sharedMesh;
            foreach (MeshFilter f in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.gameObject.name.StartsWith(EyePrefix)) continue;
                mf = f;
                return f.sharedMesh;
            }
            return null;
        }

        static Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeep(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        static float Smooth(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));

        static Color Hex(int rgb) => new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
