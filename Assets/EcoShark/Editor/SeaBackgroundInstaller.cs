using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace EcoShark.EditorScripts
{
    /// <summary>
    /// Fondo marino 2.5D "vivo" estilo cartoon (Hungry Shark / Pixar), con un clic.
    /// Menú: ECO SHARK > Configurar Fondo Marino   (o la línea "fondo" en Capturas/pedir_fotos.txt)
    ///
    /// Crea "FondoMarinoVivo" con:
    ///  1. Atmósfera: niebla #0B2B47 que se aclara a turquesa cerca de la superficie (UnderwaterAtmosphere)
    ///     y el fondo lejano con degradado turquesa → azul marino (materiales del cielo y las olas).
    ///  2. Rayos de sol aditivos que se mecen (GodRays).
    ///  3. Paralaje en 4 capas (ParallaxLayer): montañas lejanas 15 % y 20 %, rocas/arrecifes 50 %, primer plano 120 %.
    ///  4. Partículas: plancton pegado a la cámara y burbujas que suben desde grietas del suelo.
    ///  5. Lecho marino: algas que se mecen (shader) y corales naranjas, morados, rosas y amarillos.
    /// Desactiva (no borra) la decoración anterior. Luego guarda 3 capturas en Capturas/fondo_*.png.
    /// </summary>
    public static class SeaBackgroundInstaller
    {
        const string RootName = "FondoMarinoVivo";
        const string MatDir = "Assets/EcoShark/Generated/Materials/FondoMarino";
        const string MeshDir = "Assets/EcoShark/Generated/Meshes/FondoMarino";

        // Colores del agua del fondo lejano (deben coincidir con la franja de olas).
        static readonly Color WaterTop = new Color(0.09f, 0.47f, 0.6f);
        static readonly Color WaterDeep = new Color(0.03f, 0.12f, 0.21f);

        static System.Random rng;
        static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static int RI(int a, int bExclusive) => rng.Next(a, bExclusive);

        [MenuItem("ECO SHARK/Configurar Fondo Marino", false, 20)]
        static void Menu()
        {
            if (Install(out string report))
            {
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                string shots = CaptureViews();
                Debug.Log("[ECO SHARK] " + report + " Capturas: " + shots);
            }
            else Debug.LogWarning("[ECO SHARK] " + report);
        }

        [MenuItem("ECO SHARK/Capturas del fondo marino", false, 21)]
        static void MenuCaptures() => Debug.Log("[ECO SHARK] Capturas: " + CaptureViews());

        // =====================================================================================

        public static bool Install(out string report)
        {
            if (Shader.Find("EcoShark/ToonEnv") == null || Shader.Find("EcoShark/GodRay") == null
                || Shader.Find("EcoShark/Silhouette") == null || Shader.Find("EcoShark/ParticleSoft") == null)
            {
                report = "Faltan shaders del fondo marino (EcoShark/ToonEnv, GodRay, Silhouette o ParticleSoft).";
                return false;
            }

            Camera cam = MainCamera();
            if (cam == null) { report = "No hay cámara principal."; return false; }
            if (cam.farClipPlane < 200f) cam.farClipPlane = 200f;

            OceanArea area = Object.FindFirstObjectByType<OceanArea>();
            float floorY = area != null ? area.Min.y : -22f;
            float surfaceY = area != null ? area.Max.y : 6f;
            float minX = area != null ? area.Min.x : -40f;
            float maxX = area != null ? area.Max.x : 40f;

            EnsureFolder(MatDir);
            EnsureFolder(MeshDir);
            rng = new System.Random(2026);

            // Quitar la versión anterior de este fondo y apagar la decoración vieja.
            GameObject old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            GameObject env = GameObject.Find("Entorno");
            if (env != null)
            {
                Transform oldDeco = env.transform.Find("Decoracion (fondo con paralaje)");
                if (oldDeco != null) oldDeco.gameObject.SetActive(false);
            }

            Transform root = new GameObject(RootName).transform;

            // ---------- Materiales ----------
            Material rock = Toon("Roca", new Color(0.26f, 0.4f, 0.48f));
            Material rockDark = Toon("RocaOscura", new Color(0.17f, 0.28f, 0.36f));
            Material sand = Toon("Arena", new Color(0.6f, 0.53f, 0.4f), rim: 0.1f);
            Material kelp = Toon("Alga", new Color(0.24f, 0.66f, 0.36f), swayAmp: 0.55f, swaySpeed: 1.1f, swayBase: 0f, swayHeight: 1f);
            Material kelpLight = Toon("AlgaClara", new Color(0.42f, 0.78f, 0.38f), swayAmp: 0.45f, swaySpeed: 1.3f, swayBase: 0f, swayHeight: 1f);
            Material kelpFront = Toon("AlgaPrimerPlano", new Color(0.05f, 0.2f, 0.19f), swayAmp: 0.7f, swaySpeed: 0.9f, swayBase: 0f, swayHeight: 1f, rim: 0.1f);
            Material rockFront = Toon("RocaPrimerPlano", new Color(0.05f, 0.13f, 0.17f), rim: 0.12f);
            Material coralOrange = Toon("Coral_Naranja", new Color(1f, 0.5f, 0.2f), emission: 0.22f);
            Material coralPurple = Toon("Coral_Morado", new Color(0.62f, 0.34f, 0.92f), emission: 0.22f);
            Material coralPink = Toon("Coral_Rosa", new Color(1f, 0.38f, 0.56f), emission: 0.22f);
            Material coralYellow = Toon("Coral_Amarillo", new Color(1f, 0.8f, 0.28f), emission: 0.2f);
            Material fanPurple = Toon("Abanico_Morado", new Color(0.7f, 0.36f, 0.95f), emission: 0.2f, swayAmp: 0.08f, swaySpeed: 1.4f, swayBase: -0.5f, swayHeight: 1f);
            Material fanOrange = Toon("Abanico_Naranja", new Color(1f, 0.55f, 0.28f), emission: 0.2f, swayAmp: 0.08f, swaySpeed: 1.2f, swayBase: -0.5f, swayHeight: 1f);
            Material[] corals = { coralOrange, coralPurple, coralPink, coralYellow };
            Material[] fans = { fanPurple, fanOrange };

            Material farA = Silhouette("Montanas_Lejanas", new Color(0.08f, 0.3f, 0.41f), new Color(0.03f, 0.11f, 0.19f));
            Material farB = Silhouette("Montanas_Medias", new Color(0.05f, 0.21f, 0.31f), new Color(0.02f, 0.08f, 0.15f));
            Material rayMat = GetMaterial(MatDir + "/RayoDeSol.mat", "EcoShark/GodRay");
            rayMat.SetColor("_Color", new Color(0.55f, 0.9f, 1f));
            Material planktonMat = GetMaterial(MatDir + "/Plancton.mat", "EcoShark/ParticleSoft");
            planktonMat.SetFloat("_Ring", 0f);
            planktonMat.SetFloat("_MaxY", surfaceY - 0.7f);
            Material bubbleMat = GetMaterial(MatDir + "/Burbuja.mat", "EcoShark/ParticleSoft");
            bubbleMat.SetFloat("_Ring", 1f);
            bubbleMat.SetFloat("_MaxY", surfaceY - 0.7f);
            EditorUtility.SetDirty(rayMat);
            EditorUtility.SetDirty(planktonMat);
            EditorUtility.SetDirty(bubbleMat);

            // ---------- Mallas ----------
            Mesh[] rocks = { SaveMesh(RockMesh(1), "Roca_A"), SaveMesh(RockMesh(2), "Roca_B"), SaveMesh(RockMesh(3), "Roca_C") };
            Mesh[] kelps = { SaveMesh(KelpMesh(0), "Alga_A"), SaveMesh(KelpMesh(1), "Alga_B"), SaveMesh(KelpMesh(2), "Alga_C") };

            // Suelo con el nuevo material cartoon.
            if (env != null)
            {
                Transform floor = env.transform.Find("FondoMarino");
                if (floor != null) floor.GetComponent<Renderer>().sharedMaterial = sand;
            }

            // ---------- 1. Atmósfera ----------
            UnderwaterAtmosphere atmosphere = root.gameObject.AddComponent<UnderwaterAtmosphere>();
            atmosphere.Apply(cam.transform.position.y, cam);
            UpdateWaterColors();

            // ---------- 3. Paralaje ----------
            Vector3 camPos = cam.transform.position;
            float spanMin = minX - 70f, spanMax = maxX + 70f;

            // Fondo lejano: montañas y abismos (dos cordilleras para dar profundidad).
            Transform layerFarA = Layer(root, "Capa_Lejana_Montanas (15%)", new Vector3(0f, floorY, 62f), 0.15f);
            Mesh ridgeA = SaveMesh(RidgeMesh(11, Mathf.Max(170f, (maxX - minX) * 0.5f + 150f), 10f, 7f), "Cordillera_Lejana");
            MeshObject(layerFarA, "Cordillera", ridgeA, farA, Vector3.zero, Vector3.one);
            farA.SetFloat("_YTop", 20f); farA.SetFloat("_YBottom", -18f);

            Transform layerFarB = Layer(root, "Capa_Lejana_Abismos (20%)", new Vector3(0f, floorY, 40f), 0.2f);
            Mesh ridgeB = SaveMesh(RidgeMesh(29, Mathf.Max(150f, (maxX - minX) * 0.5f + 120f), 7f, 5f), "Cordillera_Media");
            MeshObject(layerFarB, "Cordillera", ridgeB, farB, Vector3.zero, Vector3.one);
            farB.SetFloat("_YTop", 14f); farB.SetFloat("_YBottom", -14f);

            // Fondo medio: rocas grandes y arrecifes con corales.
            Transform layerMid = Layer(root, "Capa_Media_Arrecifes (50%)", new Vector3(0f, 0f, 12f), 0.5f);
            for (float x = spanMin; x < spanMax; x += R(11f, 16f))
            {
                Vector3 p = new Vector3(x, floorY, R(-2f, 3f));
                ReefCluster(layerMid, p, rocks, rock, rockDark, corals, fans, big: true);
            }

            // Primer plano: algas oscuras y rocas que pasan cerca de la cámara.
            Transform layerFront = Layer(root, "Capa_PrimerPlano (120%)", new Vector3(0f, 0f, -7f), 1.2f);
            for (float x = spanMin; x < spanMax; x += R(8f, 13f))
            {
                Vector3 p = new Vector3(x, floorY - 1.5f, R(-1f, 1f));
                Transform clump = new GameObject("Grupo_PrimerPlano").transform;
                clump.SetParent(layerFront, false);
                clump.localPosition = p;
                MeshObject(clump, "Roca", rocks[RI(0, 3)], rockFront, new Vector3(R(-0.5f, 0.5f), 0.4f, 0f),
                    new Vector3(R(2.2f, 3.5f), R(1.4f, 2.4f), R(1.5f, 2.2f)), R(0f, 360f));
                int n = RI(2, 5);
                for (int i = 0; i < n; i++)
                {
                    float h = R(9f, 14f);
                    MeshObject(clump, "Alga", kelps[RI(0, 3)], kelpFront, new Vector3(R(-1.6f, 1.6f), 0.5f, R(-0.4f, 0.4f)),
                        new Vector3(R(1.2f, 1.6f), h, 1f));
                }
            }

            foreach (ParallaxLayer layer in root.GetComponentsInChildren<ParallaxLayer>())
                layer.SetReference(camPos);

            // ---------- 5. Lecho marino junto al plano de juego (algas y corales) ----------
            Transform bed = new GameObject("LechoMarino").transform;
            bed.SetParent(root, false);
            for (float x = minX - 20f; x < maxX + 20f; x += R(3f, 5.5f))
            {
                float z = R(1.8f, 7f);
                float roll = R(0f, 1f);
                if (roll < 0.5f)
                {
                    // Algas en grupitos.
                    int n = RI(1, 4);
                    for (int i = 0; i < n; i++)
                    {
                        float h = R(4.5f, 12f);
                        MeshObject(bed, "Alga", kelps[RI(0, 3)], rng.NextDouble() < 0.6 ? kelp : kelpLight,
                            new Vector3(x + R(-0.7f, 0.7f), floorY - 0.2f, z + R(-0.4f, 0.4f)), new Vector3(R(0.8f, 1.2f), h, 1f));
                    }
                }
                else if (roll < 0.85f)
                {
                    Coral(bed, new Vector3(x, floorY, z), corals, fans);
                }
                else
                {
                    MeshObject(bed, "Roca", rocks[RI(0, 3)], rock, new Vector3(x, floorY + 0.2f, z),
                        new Vector3(R(0.8f, 1.8f), R(0.6f, 1.2f), R(0.8f, 1.5f)), R(0f, 360f));
                }
            }

            // ---------- 2. Rayos de sol ----------
            Transform raysRoot = new GameObject("RayosDeSol").transform;
            raysRoot.SetParent(root, false);
            for (float x = spanMin; x < spanMax; x += R(9f, 15f))
            {
                Transform pivot = new GameObject("Rayo").transform;
                pivot.SetParent(raysRoot, false);
                pivot.position = new Vector3(x, surfaceY - 0.25f, R(4f, 14f));
                pivot.localRotation = Quaternion.Euler(0f, 0f, R(-16f, -6f));
                float length = R(18f, 26f);
                GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Quad);
                beam.name = "Haz";
                Object.DestroyImmediate(beam.GetComponent<Collider>());
                beam.transform.SetParent(pivot, false);
                beam.transform.localPosition = new Vector3(0f, -length * 0.5f, 0f);
                beam.transform.localScale = new Vector3(R(2.5f, 6f), length, 1f);
                Renderer br = beam.GetComponent<Renderer>();
                br.sharedMaterial = rayMat;
                br.shadowCastingMode = ShadowCastingMode.Off;
                br.receiveShadows = false;
            }
            raysRoot.gameObject.AddComponent<GodRays>();

            // ---------- 4. Partículas ----------
            Transform oldPlankton = cam.transform.Find("Plancton");
            if (oldPlankton != null) Object.DestroyImmediate(oldPlankton.gameObject);
            Plankton(cam.transform, planktonMat);

            Transform bubbles = new GameObject("Burbujas").transform;
            bubbles.SetParent(root, false);
            Transform killPlane = new GameObject("LimiteSuperficie").transform;
            killPlane.SetParent(bubbles, false);
            killPlane.position = new Vector3(0f, surfaceY - 0.9f, 0f);
            killPlane.rotation = Quaternion.Euler(180f, 0f, 0f); // normal hacia abajo: al cruzarla, la burbuja desaparece
            for (float x = minX + 4f; x < maxX; x += R(14f, 22f))
                BubbleVent(bubbles, new Vector3(x, floorY + 0.1f, R(0.8f, 5f)), bubbleMat, killPlane);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            report = "Fondo marino configurado: " + root.GetComponentsInChildren<Renderer>().Length + " objetos visibles.";
            return true;
        }

        // =====================================================================================
        //  Capturas para validar
        // =====================================================================================

        /// <summary>Renderiza la cámara principal en 3 alturas y guarda Capturas/fondo_*.png. Devuelve la carpeta.</summary>
        public static string CaptureViews()
        {
            Camera cam = MainCamera();
            if (cam == null) return "(sin cámara)";
            OceanArea area = Object.FindFirstObjectByType<OceanArea>();
            float floorY = area != null ? area.Min.y : -22f;
            float surfaceY = area != null ? area.Max.y : 6f;
            float halfHeight = Mathf.Abs(cam.transform.position.z) * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            CameraFollow follow = cam.GetComponent<CameraFollow>();
            float extra = 4.5f;
            if (follow != null)
            {
                SerializedProperty p = new SerializedObject(follow).FindProperty("extraAboveSurface");
                if (p != null) extra = p.floatValue;
            }

            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Capturas"));
            Directory.CreateDirectory(dir);

            Vector3 original = cam.transform.position;
            Color bg = cam.backgroundColor;
            Color fog = RenderSettings.fogColor;
            float z = original.z;

            var views = new (string name, Vector3 pos)[]
            {
                ("fondo_superficie", new Vector3(-4f, surfaceY + extra - halfHeight, z)),
                ("fondo_medio", new Vector3(8f, (surfaceY + floorY) * 0.5f, z)),
                ("fondo_profundo", new Vector3(18f, floorY + halfHeight, z)),
            };

            GameObject rootGO = GameObject.Find(RootName);
            ParallaxLayer[] layers = rootGO != null ? rootGO.GetComponentsInChildren<ParallaxLayer>() : new ParallaxLayer[0];
            UnderwaterAtmosphere atmosphere = rootGO != null ? rootGO.GetComponent<UnderwaterAtmosphere>() : null;
            SkyBackdrop sky = cam.GetComponentInChildren<SkyBackdrop>();
            ParticleSystem[] particles = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);

            var rt = new RenderTexture(1600, 900, 24);
            foreach (var v in views)
            {
                cam.transform.position = v.pos;
                foreach (ParallaxLayer l in layers) l.ApplyFor(v.pos);
                if (atmosphere != null) atmosphere.Apply(v.pos.y, cam);
                if (sky != null) sky.Refresh();
                foreach (ParticleSystem ps in particles) ps.Simulate(14f, true, true);

                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = null;

                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                File.WriteAllBytes(Path.Combine(dir, v.name + ".png"), tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            Object.DestroyImmediate(rt);

            // Dejar todo como estaba.
            cam.transform.position = original;
            foreach (ParallaxLayer l in layers) l.ApplyFor(original);
            cam.backgroundColor = bg;
            RenderSettings.fogColor = fog;
            if (sky != null) sky.Refresh();
            foreach (ParticleSystem ps in particles) ps.Clear(true);
            return dir;
        }

        // =====================================================================================
        //  Piezas
        // =====================================================================================

        static Transform Layer(Transform root, string name, Vector3 position, float speed)
        {
            Transform t = new GameObject(name).transform;
            t.SetParent(root, false);
            t.position = position;
            // El primer plano solo se desplaza en horizontal: así sus algas siempre asoman desde abajo cuando la cámara está cerca del fondo.
            t.gameObject.AddComponent<ParallaxLayer>().Configure(speed, speed < 1f);
            return t;
        }

        static void ReefCluster(Transform parent, Vector3 localPos, Mesh[] rocks, Material rock, Material rockDark, Material[] corals, Material[] fans, bool big)
        {
            Transform c = new GameObject("Arrecife").transform;
            c.SetParent(parent, false);
            c.localPosition = localPos;

            int nRocks = RI(2, 5);
            float top = 0f;
            for (int i = 0; i < nRocks; i++)
            {
                float h = big ? R(1.8f, 6.5f) : R(1f, 2f);
                float w = R(2f, 4.5f);
                MeshObject(c, "Roca", rocks[RI(0, 3)], i % 2 == 0 ? rock : rockDark,
                    new Vector3(R(-3f, 3f), h * 0.35f, R(-1f, 1f)), new Vector3(w, h, R(1.8f, 3f)), R(0f, 360f));
                top = Mathf.Max(top, h * 0.8f);
            }
            // Pilar alto de roca de vez en cuando (formación de arrecife).
            if (rng.NextDouble() < 0.35)
                MeshObject(c, "Pilar", rocks[RI(0, 3)], rockDark, new Vector3(R(-2f, 2f), 3f, 0.5f), new Vector3(R(1.6f, 2.4f), R(7f, 11f), 1.8f), R(0f, 360f));

            int nCorals = RI(2, 5);
            for (int i = 0; i < nCorals; i++)
                Coral(c, new Vector3(R(-3f, 3f), R(0.2f, top * 0.7f), R(-1.4f, -0.6f)), corals, fans);
        }

        static void Coral(Transform parent, Vector3 localPos, Material[] corals, Material[] fans)
        {
            Transform c = new GameObject("Coral").transform;
            c.SetParent(parent, false);
            c.localPosition = localPos;
            int kind = RI(0, 4);
            Material m = corals[RI(0, corals.Length)];
            switch (kind)
            {
                case 0: // Ramificado
                {
                    int branches = RI(4, 8);
                    for (int i = 0; i < branches; i++)
                    {
                        float len = R(0.7f, 1.6f);
                        var rot = Quaternion.Euler(R(-35f, 35f), 0f, R(-40f, 40f));
                        Vector3 dir = rot * Vector3.up;
                        Prim(PrimitiveType.Capsule, "Rama", c, dir * len * 0.5f, new Vector3(0.22f, len * 0.5f, 0.22f), rot, m);
                        Prim(PrimitiveType.Sphere, "Punta", c, dir * len, Vector3.one * 0.3f, Quaternion.identity, m);
                    }
                    break;
                }
                case 1: // Abanico (se mece)
                {
                    Material f = fans[RI(0, fans.Length)];
                    float s = R(1.3f, 2.2f);
                    Prim(PrimitiveType.Cylinder, "Tallo", c, new Vector3(0f, 0.3f, 0f), new Vector3(0.12f, 0.3f, 0.12f), Quaternion.identity, f);
                    Prim(PrimitiveType.Sphere, "Abanico", c, new Vector3(0f, 0.55f + s * 0.45f, 0f), new Vector3(s, s * 1.1f, 0.12f),
                        Quaternion.Euler(0f, R(-25f, 25f), 0f), f);
                    break;
                }
                case 2: // Cerebro
                {
                    float s = R(0.9f, 1.6f);
                    Prim(PrimitiveType.Sphere, "Cerebro", c, new Vector3(0f, s * 0.25f, 0f), new Vector3(s * 1.3f, s * 0.8f, s * 1.1f), Quaternion.identity, m);
                    break;
                }
                default: // Tubos
                {
                    int tubes = RI(3, 7);
                    for (int i = 0; i < tubes; i++)
                    {
                        float h = R(0.5f, 1.6f);
                        float r = R(0.18f, 0.32f);
                        Prim(PrimitiveType.Cylinder, "Tubo", c, new Vector3(R(-0.6f, 0.6f), h, R(-0.4f, 0.4f)), new Vector3(r, h, r),
                            Quaternion.Euler(R(-10f, 10f), 0f, R(-12f, 12f)), m);
                    }
                    break;
                }
            }
        }

        static void Plankton(Transform cam, Material mat)
        {
            var go = new GameObject("Plancton");
            go.transform.SetParent(cam, false);
            go.transform.localPosition = new Vector3(0f, 0f, 16f);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 10f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 16f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.18f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.95f, 1f, 0.3f), new Color(0.9f, 1f, 0.95f, 0.65f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 600;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 36f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(44f, 26f, 16f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.18f;
            noise.frequency = 0.25f;
            noise.scrollSpeed = 0.1f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
        }

        static void BubbleVent(Transform parent, Vector3 position, Material mat, Transform killPlane)
        {
            var go = new GameObject("Grieta_Burbujas");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // el cono apunta hacia arriba
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 5f;
            main.startLifetime = 20f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.3f, 2.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.32f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.8f, 0.95f, 1f, 0.55f), new Color(1f, 1f, 1f, 0.85f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = R(1.2f, 2.6f);
            emission.SetBursts(new[] { new ParticleSystem.Burst(R(0f, 4f), 4, 7, 3, R(4f, 7f)) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 6f;
            shape.radius = 0.25f;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.9f;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.8f, 1f, 1.3f));

            var collision = ps.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.Planes;
            collision.SetPlane(0, killPlane);
            collision.lifetimeLoss = 1f;
            collision.bounce = 0f;
            collision.dampen = 1f;
            collision.radiusScale = 0.1f;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
        }

        // =====================================================================================
        //  Mallas generadas
        // =====================================================================================

        /// <summary>Roca low-poly: esfera deformada con caras planas (se ve cartoon con la luz toon).</summary>
        static Mesh RockMesh(int seed)
        {
            GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh sphere = tmp.GetComponent<MeshFilter>().sharedMesh;
            Vector3[] sv = sphere.vertices;
            int[] st = sphere.triangles;
            Object.DestroyImmediate(tmp);

            var verts = new Vector3[st.Length];
            var tris = new int[st.Length];
            float s = seed * 1.7f;
            for (int i = 0; i < st.Length; i++)
            {
                Vector3 d = sv[st[i]].normalized;
                // Cuantizar un poco la dirección para facetas más grandes.
                Vector3 q = new Vector3(Mathf.Round(d.x * 5f) / 5f, Mathf.Round(d.y * 5f) / 5f, Mathf.Round(d.z * 5f) / 5f);
                float n = Mathf.Sin(q.x * 3.1f + s) * Mathf.Cos(q.y * 2.7f + s * 0.7f) + 0.5f * Mathf.Sin(q.z * 4.3f + s * 1.3f);
                float r = 0.5f * (1f + 0.22f * n);
                Vector3 p = d * r;
                if (p.y < -0.2f) p.y = -0.2f + (p.y + 0.2f) * 0.3f; // base aplanada
                verts[i] = p;
                tris[i] = i;
            }
            var m = new Mesh { name = "Roca" + seed, vertices = verts, triangles = tris };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Alga: tira ondulada que se afina hacia arriba, con hojas a los lados. Alto 1 (se escala en Y).</summary>
        static Mesh KelpMesh(int seed)
        {
            const int segments = 16;
            var v = new System.Collections.Generic.List<Vector3>();
            var t = new System.Collections.Generic.List<int>();
            float phase = seed * 1.9f;

            for (int i = 0; i <= segments; i++)
            {
                float y = i / (float)segments;
                float w = Mathf.Lerp(0.34f, 0.1f, y);
                float cx = 0.1f * Mathf.Sin(y * 9f + phase);
                v.Add(new Vector3(cx - w * 0.5f, y, 0f));
                v.Add(new Vector3(cx + w * 0.5f, y, 0f));
                if (i < segments)
                {
                    int b = i * 2;
                    t.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 });
                }
            }
            // Hojas alternadas.
            for (int i = 1; i < segments; i += 2)
            {
                float y = i / (float)segments;
                float side = (i / 2) % 2 == 0 ? 1f : -1f;
                float cx = 0.1f * Mathf.Sin(y * 9f + phase);
                float w = Mathf.Lerp(0.34f, 0.1f, y) * 0.5f;
                int b = v.Count;
                v.Add(new Vector3(cx + side * w, y, 0f));
                v.Add(new Vector3(cx + side * w, y + 0.05f, 0f));
                v.Add(new Vector3(cx + side * (w + Mathf.Lerp(0.55f, 0.25f, y)), y + 0.09f, 0f));
                if (side > 0) t.AddRange(new[] { b, b + 1, b + 2 });
                else t.AddRange(new[] { b, b + 2, b + 1 });
            }

            var normals = new Vector3[v.Count];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.back; // mira a la cámara
            var m = new Mesh { name = "Alga" + seed, vertices = v.ToArray(), triangles = t.ToArray(), normals = normals };
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Cordillera submarina: perfil de montañas con cañones (abismos), rellena hasta muy abajo.</summary>
        static Mesh RidgeMesh(int seed, float halfWidth, float baseHeight, float variation)
        {
            var v = new System.Collections.Generic.List<Vector3>();
            var t = new System.Collections.Generic.List<int>();
            float s = seed * 0.73f;
            float c1 = -halfWidth * 0.35f + seed % 7 * 4f;
            float c2 = halfWidth * 0.3f - seed % 5 * 5f;
            int columns = Mathf.CeilToInt(halfWidth * 2f / 1.5f);
            for (int i = 0; i <= columns; i++)
            {
                float x = -halfWidth + i * (halfWidth * 2f / columns);
                float h = baseHeight
                          + variation * Mathf.Sin(x * 0.045f + s)
                          + variation * 0.55f * Mathf.Sin(x * 0.11f + s * 2f)
                          + variation * 0.3f * Mathf.Sin(x * 0.29f + s * 3f)
                          + variation * 0.15f * Mathf.Sin(x * 0.7f + s * 5f);
                // Cañones: el perfil baja hasta casi el fondo.
                h *= 1f - 0.85f * Mathf.Exp(-Mathf.Pow((x - c1) / 7f, 2f));
                h *= 1f - 0.85f * Mathf.Exp(-Mathf.Pow((x - c2) / 9f, 2f));
                h -= 12f * Mathf.Exp(-Mathf.Pow((x - c1) / 4f, 2f)); // abismo bajo el suelo
                v.Add(new Vector3(x, -80f, 0f));
                v.Add(new Vector3(x, h, 0f));
                if (i < columns)
                {
                    int b = i * 2;
                    t.AddRange(new[] { b, b + 1, b + 2, b + 1, b + 3, b + 2 });
                }
            }
            var m = new Mesh { name = "Cordillera" + seed, vertices = v.ToArray(), triangles = t.ToArray() };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        // =====================================================================================
        //  Utilidades
        // =====================================================================================

        static GameObject MeshObject(Transform parent, string name, Mesh mesh, Material mat, Vector3 localPos, Vector3 localScale, float yaw = 0f)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = localScale;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Quaternion localRot, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;
            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        static Material Toon(string name, Color color, float emission = 0f, float swayAmp = 0f, float swaySpeed = 1.2f,
                             float swayBase = 0f, float swayHeight = 1f, float rim = 0.25f)
        {
            Material m = GetMaterial(MatDir + "/" + name + ".mat", "EcoShark/ToonEnv");
            m.SetColor("_Color", color);
            m.SetColor("_ShadowColor", new Color(0.42f, 0.52f, 0.72f));
            m.SetFloat("_Emission", emission);
            m.SetFloat("_RimStrength", rim);
            m.SetFloat("_SwayAmp", swayAmp);
            m.SetFloat("_SwaySpeed", swaySpeed);
            m.SetFloat("_SwayBase", swayBase);
            m.SetFloat("_SwayHeight", swayHeight);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Silhouette(string name, Color top, Color bottom)
        {
            Material m = GetMaterial(MatDir + "/" + name + ".mat", "EcoShark/Silhouette");
            m.SetColor("_TopColor", top);
            m.SetColor("_BottomColor", bottom);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>El agua del fondo lejano y la franja de olas usan el mismo turquesa (si no, se ve una línea).</summary>
        static void UpdateWaterColors()
        {
            Material sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/EcoShark/Generated/Materials/FondoCielo.mat");
            Material waves = AssetDatabase.LoadAssetAtPath<Material>("Assets/EcoShark/Generated/Materials/Olas.mat");
            if (sky != null)
            {
                sky.SetColor("_WaterTop", WaterTop);
                sky.SetColor("_WaterDeep", WaterDeep);
                EditorUtility.SetDirty(sky);
            }
            if (waves != null)
            {
                waves.SetColor("_WaterTop", WaterTop);
                EditorUtility.SetDirty(waves);
            }
        }

        static Material GetMaterial(string path, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader) m.shader = shader;
            return m;
        }

        static Mesh SaveMesh(Mesh mesh, string name)
        {
            string path = MeshDir + "/" + name + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                existing.Clear();
                existing.vertices = mesh.vertices;
                existing.triangles = mesh.triangles;
                existing.normals = mesh.normals;
                existing.RecalculateBounds();
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(mesh);
                return existing;
            }
            mesh.name = name;
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        static Camera MainCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject go = GameObject.Find("Main Camera");
                if (go != null) cam = go.GetComponent<Camera>();
            }
            return cam;
        }
    }
}
