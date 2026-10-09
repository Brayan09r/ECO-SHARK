using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace EcoShark.EditorScripts
{
    /// <summary>
    /// Menú "ECO SHARK" en la barra superior de Unity.
    /// "1. Crear escena MVP (greybox)" arma en un clic una escena jugable con figuras básicas:
    /// tiburón, basura, peligros, redes con animales, barco ilegal, cámara, HUD y tienda.
    /// Luego reemplazas las figuras por tus modelos 3D sin tocar los scripts.
    /// </summary>
    public static class EcoSharkSceneBuilder
    {
        const string Root = "Assets/EcoShark";
        const string GeneratedDir = Root + "/Generated";
        const string MatDir = GeneratedDir + "/Materials";
        const string PrefabDir = GeneratedDir + "/Prefabs";
        const string DataDir = GeneratedDir + "/Data";
        const string SceneDir = Root + "/Scenes";
        const string ScenePath = SceneDir + "/EcoShark_MVP.unity";

        static Shader cachedShader;

        // =====================================================================================
        //  MENÚ
        // =====================================================================================

        [MenuItem("ECO SHARK/2. Equipar Modelo Tiburón Martillo 3D", false, 1)]
        public static void EquipHammerheadModel()
        {
            GameObject shark = GameObject.Find("Tiburon_Guardian");
            if (shark == null)
            {
                var movement = Object.FindAnyObjectByType<SharkMovement>();
                if (movement != null) shark = movement.gameObject;
            }

            if (shark == null)
            {
                EditorUtility.DisplayDialog("ECO SHARK", "No se encontró el objeto 'Tiburon_Guardian' en la escena activa.\nAsegúrate de abrir la escena EcoShark_MVP.", "Aceptar");
                return;
            }

            // Eliminar modelo provisional anterior si existe
            Transform oldModel = shark.transform.Find("Modelo (reemplazar por tu 3D)");
            if (oldModel != null) Object.DestroyImmediate(oldModel.gameObject);

            Transform existing3D = shark.transform.Find("Tiburon_Martillo_3D");
            if (existing3D != null) Object.DestroyImmediate(existing3D.gameObject);

            // Cargar el FBX del Tiburón Martillo
            GameObject hammerheadAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EcoShark/Models/Sharks/Shark_Hammerhead.fbx");
            if (hammerheadAsset == null)
            {
                AssetDatabase.Refresh();
                hammerheadAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EcoShark/Models/Sharks/Shark_Hammerhead.fbx");
            }

            if (hammerheadAsset != null)
            {
                GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(hammerheadAsset);
                modelInstance.name = "Tiburon_Martillo_3D";
                modelInstance.transform.SetParent(shark.transform, false);
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
                modelInstance.transform.localScale = Vector3.one;

                Transform mouth = shark.transform.Find("Boca");
                if (mouth == null)
                {
                    mouth = new GameObject("Boca").transform;
                    mouth.SetParent(shark.transform, false);
                }
                mouth.localPosition = new Vector3(0f, -0.1f, 1.45f);

                SharkCollector collector = shark.GetComponent<SharkCollector>();
                if (collector != null)
                {
                    SetRef(collector, "mouth", mouth);
                }

                CapsuleCollider col = shark.GetComponent<CapsuleCollider>();
                if (col != null)
                {
                    col.direction = 2; // Eje Z
                    col.radius = 0.55f;
                    col.height = 3.2f;
                    col.center = new Vector3(0f, 0f, 0f);
                }

                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                Selection.activeGameObject = shark;

                EditorUtility.DisplayDialog("ECO SHARK", "¡Modelo 3D del Tiburón Martillo integrado con éxito en la escena!", "¡Genial!");
            }
            else
            {
                EditorUtility.DisplayDialog("ECO SHARK", "No se encontró el asset en 'Assets/EcoShark/Models/Sharks/Shark_Hammerhead.fbx'.", "Aceptar");
            }
        }

        [MenuItem("ECO SHARK/1. Crear escena MVP (greybox)", false, 0)]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("ECO SHARK", "La escena EcoShark_MVP ya existe.\n¿Quieres reemplazarla (y regenerar prefabs y materiales)?", "Sí, reemplazar", "Cancelar"))
                return;

            EnsureFolder(MatDir);
            EnsureFolder(PrefabDir);
            EnsureFolder(DataDir);
            EnsureFolder(SceneDir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---------- Materiales ----------
            Material mShark = Mat("Tiburon", new Color(0.36f, 0.47f, 0.58f), 0.55f);
            Material mBelly = Mat("TiburonVientre", new Color(0.88f, 0.9f, 0.92f), 0.4f);
            Material mEye = Mat("Ojo", new Color(0.02f, 0.02f, 0.02f), 0.9f);
            Material mBottle = Mat("PlasticoBotella", new Color(0.55f, 0.85f, 0.95f), 0.85f);
            Material mCap = Mat("PlasticoTapa", new Color(0.1f, 0.4f, 0.95f), 0.4f);
            Material mBag = Mat("PlasticoBolsa", new Color(0.95f, 0.95f, 0.9f), 0.2f);
            Material mCan = Mat("Lata", new Color(0.85f, 0.15f, 0.15f), 0.75f);
            Material mMetal = Mat("Metal", new Color(0.75f, 0.75f, 0.78f), 0.8f);
            Material mOil = Mat("Petroleo", new Color(0.03f, 0.03f, 0.04f), 0.95f);
            Material mBarrel = Mat("Barril", new Color(0.95f, 0.8f, 0.1f), 0.4f);
            Material mRadio = Mat("BrilloRadiactivo", new Color(0.3f, 1f, 0.2f), 0.5f, true);
            Material mNet = Mat("Red", new Color(0.95f, 0.3f, 0.2f), 0.2f);
            Material mTurtle = Mat("TortugaCaparazon", new Color(0.25f, 0.5f, 0.28f), 0.3f);
            Material mTurtleSkin = Mat("TortugaPiel", new Color(0.55f, 0.72f, 0.45f), 0.2f);
            Material mDolphin = Mat("Delfin", new Color(0.55f, 0.62f, 0.72f), 0.6f);
            Material mProgress = Mat("BarraRescate", new Color(0.3f, 1f, 0.6f), 0.2f, true);
            Material mBoat = Mat("BarcoCasco", new Color(0.45f, 0.22f, 0.14f), 0.3f);
            Material mBoatCabin = Mat("BarcoCabina", new Color(0.9f, 0.9f, 0.85f), 0.3f);
            Material mSand = Mat("Arena", new Color(0.76f, 0.68f, 0.5f), 0.1f);
            Material mRock = Mat("Roca", new Color(0.3f, 0.33f, 0.36f), 0.1f);
            Material mCoral = Mat("Coral", new Color(1f, 0.45f, 0.4f), 0.2f);
            Material mKelp = Mat("Alga", new Color(0.2f, 0.5f, 0.25f), 0.2f);
            Material mSurface = Mat("Superficie", new Color(0.55f, 0.85f, 1f), 0.9f, true);
            Material mSky = Mat("Cielo", new Color(0.65f, 0.85f, 1f), 0f);

            // ---------- Datos de basura (ScriptableObjects) ----------
            TrashData dBottle = Trash("Basura_Botella", "Botella de plástico", 10f, 5,
                "Una botella de plástico puede tardar alrededor de 450 años en degradarse en el mar.");
            TrashData dBag = Trash("Basura_Bolsa", "Bolsa plástica", 6f, 3,
                "Las tortugas marinas pueden confundir las bolsas plásticas con medusas y tragarlas.");
            TrashData dCan = Trash("Basura_Lata", "Lata de aluminio", 8f, 4,
                "Una lata puede tardar unos 200 años en degradarse en el mar, pero el aluminio se puede reciclar una y otra vez.");

            // ---------- Prefabs ----------
            GameObject pBottle = SavePrefab(BuildTrash("Basura_Botella", dBottle, 0.6f, v =>
            {
                Primitive(PrimitiveType.Capsule, "Cuerpo", v, Vector3.zero, new Vector3(0.35f, 0.45f, 0.35f), mBottle);
                Primitive(PrimitiveType.Cylinder, "Tapa", v, new Vector3(0f, 0.5f, 0f), new Vector3(0.16f, 0.08f, 0.16f), mCap);
            }));

            GameObject pBag = SavePrefab(BuildTrash("Basura_Bolsa", dBag, 0.6f, v =>
            {
                Primitive(PrimitiveType.Sphere, "Bolsa", v, Vector3.zero, new Vector3(0.8f, 0.55f, 0.3f), mBag);
                Primitive(PrimitiveType.Cube, "AsaIzq", v, new Vector3(-0.2f, 0.33f, 0f), new Vector3(0.08f, 0.2f, 0.05f), mBag);
                Primitive(PrimitiveType.Cube, "AsaDer", v, new Vector3(0.2f, 0.33f, 0f), new Vector3(0.08f, 0.2f, 0.05f), mBag);
            }));

            GameObject pCan = SavePrefab(BuildTrash("Basura_Lata", dCan, 0.5f, v =>
            {
                Primitive(PrimitiveType.Cylinder, "Lata", v, Vector3.zero, new Vector3(0.3f, 0.35f, 0.3f), mCan);
                Primitive(PrimitiveType.Cylinder, "Tapa", v, new Vector3(0f, 0.36f, 0f), new Vector3(0.28f, 0.02f, 0.28f), mMetal);
            }));

            GameObject pOil = SavePrefab(BuildOil(mOil));
            GameObject pBarrel = SavePrefab(BuildBarrel(mBarrel, mRadio));

            GameObject pTurtle = SavePrefab(BuildTrappedAnimal("Rescate_Tortuga", "Tortuga marina",
                "Muchas tortugas marinas mueren enredadas en redes abandonadas (\"redes fantasma\") o por tragar plásticos.",
                25, 2.2f, 2.2f, mNet, mProgress, a =>
                {
                    Primitive(PrimitiveType.Sphere, "Caparazon", a, Vector3.zero, new Vector3(1.6f, 0.7f, 1.1f), mTurtle);
                    Primitive(PrimitiveType.Sphere, "Cabeza", a, new Vector3(0.95f, 0.05f, 0f), new Vector3(0.45f, 0.4f, 0.4f), mTurtleSkin);
                    Primitive(PrimitiveType.Cube, "AletaDelIzq", a, new Vector3(0.45f, -0.25f, -0.5f), new Vector3(0.7f, 0.08f, 0.35f), new Vector3(0f, 30f, 0f), mTurtleSkin);
                    Primitive(PrimitiveType.Cube, "AletaDelDer", a, new Vector3(0.45f, -0.25f, 0.5f), new Vector3(0.7f, 0.08f, 0.35f), new Vector3(0f, -30f, 0f), mTurtleSkin);
                    Primitive(PrimitiveType.Cube, "AletaTrasIzq", a, new Vector3(-0.6f, -0.25f, -0.4f), new Vector3(0.4f, 0.08f, 0.25f), mTurtleSkin);
                    Primitive(PrimitiveType.Cube, "AletaTrasDer", a, new Vector3(-0.6f, -0.25f, 0.4f), new Vector3(0.4f, 0.08f, 0.25f), mTurtleSkin);
                }));

            GameObject pDolphin = SavePrefab(BuildTrappedAnimal("Rescate_Delfin", "Delfín",
                "La captura incidental en redes de pesca es una de las mayores amenazas para delfines y marsopas.",
                35, 2.5f, 2.8f, mNet, mProgress, a =>
                {
                    Primitive(PrimitiveType.Capsule, "Cuerpo", a, Vector3.zero, new Vector3(0.6f, 1.1f, 0.5f), new Vector3(0f, 0f, 90f), mDolphin);
                    Primitive(PrimitiveType.Capsule, "Hocico", a, new Vector3(1.2f, -0.08f, 0f), new Vector3(0.2f, 0.3f, 0.2f), new Vector3(0f, 0f, 90f), mDolphin);
                    Primitive(PrimitiveType.Cube, "AletaDorsal", a, new Vector3(-0.1f, 0.4f, 0f), new Vector3(0.35f, 0.4f, 0.08f), new Vector3(0f, 0f, 30f), mDolphin);
                    Primitive(PrimitiveType.Cube, "Cola", a, new Vector3(-1.2f, 0f, 0f), new Vector3(0.12f, 0.12f, 0.8f), mDolphin);
                    Primitive(PrimitiveType.Sphere, "Ojo", a, new Vector3(0.85f, 0.08f, -0.22f), Vector3.one * 0.1f, mEye);
                }));

            GameObject pBoat = SavePrefab(BuildBoat(new[] { pTurtle, pDolphin }, mBoat, mBoatCabin));

            // ---------- Ambiente ----------
            Color waterColor = new Color(0.04f, 0.26f, 0.4f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = waterColor;
            RenderSettings.fogDensity = 0.022f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.5f, 0.6f);
            RenderSettings.skybox = null;

            var lightGO = new GameObject("Luz Solar");
            Light sun = lightGO.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.color = new Color(0.8f, 0.95f, 1f);
            sun.shadows = LightShadows.Soft;
            lightGO.transform.rotation = Quaternion.Euler(60f, -20f, 0f);

            var env = new GameObject("Entorno").transform;
            Primitive(PrimitiveType.Cube, "FondoMarino", env, new Vector3(0f, -23.5f, 4f), new Vector3(160f, 3f, 30f), mSand);
            Primitive(PrimitiveType.Cube, "Superficie", env, new Vector3(0f, 6.05f, 2f), new Vector3(160f, 0.1f, 26f), mSurface);
            Primitive(PrimitiveType.Cube, "Cielo", env, new Vector3(0f, 20f, 30f), new Vector3(200f, 28f, 1f), mSky);
            BuildDecoration(env, mRock, mCoral, mKelp);

            // ---------- Managers ----------
            var gmGO = new GameObject("GameManager");
            gmGO.AddComponent<OceanArea>(); // límites por defecto: X -40..40, Y -22..6 (superficie)
            OceanSpawner spawner = gmGO.AddComponent<OceanSpawner>();
            GameManager gm = gmGO.AddComponent<GameManager>();

            // ---------- Tiburón ----------
            GameObject shark = BuildShark(mShark, mBelly, mEye, out SharkMovement movement, out SharkHealth health);

            // ---------- Cámara ----------
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            Camera cam = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = waterColor;
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 200f;
            camGO.transform.position = shark.transform.position + new Vector3(0f, 1f, -18f);
            CameraFollow follow = camGO.AddComponent<CameraFollow>();
            SetRef(follow, "target", shark.transform);

            // ---------- Objetos iniciales del nivel ----------
            var boat = (GameObject)PrefabUtility.InstantiatePrefab(pBoat);
            boat.transform.position = new Vector3(-10f, 6.6f, 0f);
            var turtle = (GameObject)PrefabUtility.InstantiatePrefab(pTurtle);
            turtle.transform.position = new Vector3(14f, -13f, 0f);
            var dolphin = (GameObject)PrefabUtility.InstantiatePrefab(pDolphin);
            dolphin.transform.position = new Vector3(-22f, -9f, 0f);

            // ---------- Spawner ----------
            SetRef(spawner, "player", shark.transform);
            SetSpawnEntries(spawner, new[]
            {
                new EntryDef("Botellas", pBottle, 14, 2.5f, new Vector2(0.05f, 0.95f), true),
                new EntryDef("Bolsas", pBag, 10, 3f, new Vector2(0.1f, 0.95f), true),
                new EntryDef("Latas", pCan, 10, 3.5f, new Vector2(0f, 0.9f), true),
                new EntryDef("Petróleo", pOil, 3, 14f, new Vector2(0.65f, 0.95f), false),
                new EntryDef("Barriles radiactivos", pBarrel, 4, 10f, new Vector2(0.9f, 1f), false),
            });

            // ---------- HUD + EventSystem ----------
            HUDController hud = BuildHUD(health);
            BuildEventSystem();

            // ---------- Conectar GameManager ----------
            SetRef(gm, "shark", health);
            SetRef(gm, "sharkMovement", movement);
            SetRef(gm, "spawner", spawner);
            SetRef(gm, "hud", hud);

            // ---------- Guardar ----------
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = shark;

            EditorUtility.DisplayDialog("ECO SHARK",
                "¡Escena MVP creada!\n\nPresiona Play.\n\n" +
                "Controles: WASD / Flechas, o mantén clic izquierdo para nadar hacia el puntero.\n" +
                "Shift o Espacio: impulso.  P / Esc: pausa.  R: reiniciar al perder.",
                "¡A nadar!");
        }

        [MenuItem("ECO SHARK/Pruebas/Dar 500 de biomasa", false, 20)]
        static void GiveBiomass()
        {
            Progression.AddBiomass(500);
            Debug.Log("[ECO SHARK] Biomasa total: " + Progression.Biomass);
        }

        [MenuItem("ECO SHARK/Pruebas/Borrar progreso guardado", false, 21)]
        static void ResetProgress()
        {
            if (EditorUtility.DisplayDialog("ECO SHARK", "¿Borrar biomasa, mejoras y récord guardados?", "Borrar", "Cancelar"))
            {
                Progression.ResetAll();
                Debug.Log("[ECO SHARK] Progreso borrado.");
            }
        }

        // =====================================================================================
        //  CONSTRUCCIÓN DE OBJETOS
        // =====================================================================================

        static GameObject BuildShark(Material body, Material belly, Material eye, out SharkMovement movement, out SharkHealth health)
        {
            var shark = new GameObject("Tiburon_Guardian");
            shark.tag = "Player";
            shark.transform.position = new Vector3(0f, -6f, 0f);

            Rigidbody rb = shark.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.mass = 1f;

            CapsuleCollider col = shark.AddComponent<CapsuleCollider>();
            col.direction = 2; // eje Z: el tiburón es largo hacia adelante
            col.radius = 0.5f;
            col.height = 2.6f;

            movement = shark.AddComponent<SharkMovement>();
            health = shark.AddComponent<SharkHealth>();
            SharkCollector collector = shark.AddComponent<SharkCollector>();

            // Modelo 3D del Tiburón Martillo
            GameObject hammerheadAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EcoShark/Models/Sharks/Shark_Hammerhead.fbx");
            if (hammerheadAsset != null)
            {
                GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(hammerheadAsset);
                modelInstance.name = "Tiburon_Martillo_3D";
                modelInstance.transform.SetParent(shark.transform, false);
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
                modelInstance.transform.localScale = Vector3.one;

                col.radius = 0.55f;
                col.height = 3.2f;
            }
            else
            {
                // Modelo provisional: la nariz apunta a +Z (igual que debe hacerlo tu modelo 3D final).
                Transform model = new GameObject("Modelo (reemplazar por tu 3D)").transform;
                model.SetParent(shark.transform, false);
                Primitive(PrimitiveType.Capsule, "Cuerpo", model, Vector3.zero, new Vector3(0.9f, 1.3f, 1f), new Vector3(90f, 0f, 0f), body);
                Primitive(PrimitiveType.Capsule, "Vientre", model, new Vector3(0f, -0.18f, 0.05f), new Vector3(0.75f, 1.15f, 0.75f), new Vector3(90f, 0f, 0f), belly);
                Primitive(PrimitiveType.Cube, "AletaDorsal", model, new Vector3(0f, 0.65f, 0f), new Vector3(0.1f, 0.65f, 0.55f), new Vector3(-20f, 0f, 0f), body);
                Primitive(PrimitiveType.Cube, "Cola", model, new Vector3(0f, 0.25f, -1.45f), new Vector3(0.08f, 1f, 0.4f), new Vector3(-35f, 0f, 0f), body);
                Primitive(PrimitiveType.Cube, "AletaIzq", model, new Vector3(-0.5f, -0.3f, 0.3f), new Vector3(0.7f, 0.06f, 0.35f), new Vector3(0f, 0f, 20f), body);
                Primitive(PrimitiveType.Cube, "AletaDer", model, new Vector3(0.5f, -0.3f, 0.3f), new Vector3(0.7f, 0.06f, 0.35f), new Vector3(0f, 0f, -20f), body);
                Primitive(PrimitiveType.Sphere, "OjoIzq", model, new Vector3(-0.36f, 0.15f, 0.85f), Vector3.one * 0.12f, eye);
                Primitive(PrimitiveType.Sphere, "OjoDer", model, new Vector3(0.36f, 0.15f, 0.85f), Vector3.one * 0.12f, eye);
            }

            Transform mouth = new GameObject("Boca").transform;
            mouth.SetParent(shark.transform, false);
            mouth.localPosition = new Vector3(0f, -0.1f, 1.45f);
            SetRef(collector, "mouth", mouth);

            return shark;
        }

        static GameObject BuildTrash(string name, TrashData data, float radius, System.Action<Transform> buildVisual)
        {
            var root = new GameObject(name);
            SphereCollider col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = radius;
            AddKinematicBody(root);
            TrashCollectible trash = root.AddComponent<TrashCollectible>();
            SetRef(trash, "data", data);

            Transform visual = new GameObject("Modelo").transform;
            visual.SetParent(root.transform, false);
            buildVisual(visual);
            return root;
        }

        static GameObject BuildOil(Material oil)
        {
            var root = new GameObject("Peligro_Petroleo");
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(4.2f, 1.4f, 2f);
            AddKinematicBody(root);
            root.AddComponent<OilSlick>();

            Transform v = new GameObject("Modelo").transform;
            v.SetParent(root.transform, false);
            Primitive(PrimitiveType.Sphere, "Mancha", v, Vector3.zero, new Vector3(4.2f, 1.3f, 0.6f), oil);
            Primitive(PrimitiveType.Sphere, "Gota1", v, new Vector3(-1.7f, 0.35f, 0f), new Vector3(1.6f, 1f, 0.5f), oil);
            Primitive(PrimitiveType.Sphere, "Gota2", v, new Vector3(1.8f, -0.25f, 0f), new Vector3(1.4f, 0.9f, 0.5f), oil);
            return root;
        }

        static GameObject BuildBarrel(Material barrel, Material radio)
        {
            var root = new GameObject("Peligro_BarrilRadiactivo");
            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
            col.radius = 0.55f;
            col.height = 1.7f;
            AddKinematicBody(root);
            RadioactiveBarrel logic = root.AddComponent<RadioactiveBarrel>();

            Transform v = new GameObject("Modelo").transform;
            v.SetParent(root.transform, false);
            Primitive(PrimitiveType.Cylinder, "Barril", v, Vector3.zero, new Vector3(0.9f, 0.8f, 0.9f), barrel);
            GameObject glow = Primitive(PrimitiveType.Cylinder, "FranjaBrillante", v, Vector3.zero, new Vector3(0.95f, 0.1f, 0.95f), radio);
            Primitive(PrimitiveType.Cylinder, "FranjaSup", v, new Vector3(0f, 0.5f, 0f), new Vector3(0.93f, 0.04f, 0.93f), radio);
            Primitive(PrimitiveType.Cylinder, "FranjaInf", v, new Vector3(0f, -0.5f, 0f), new Vector3(0.93f, 0.04f, 0.93f), radio);
            SetRef(logic, "glow", glow.transform);
            return root;
        }

        static GameObject BuildTrappedAnimal(string name, string animalName, string fact, int reward, float triggerRadius,
                                             float width, Material net, Material progress, System.Action<Transform> buildAnimal)
        {
            var root = new GameObject(name);
            SphereCollider col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = triggerRadius;
            AddKinematicBody(root);
            TrappedAnimal logic = root.AddComponent<TrappedAnimal>();

            Transform animal = new GameObject("Animal").transform;
            animal.SetParent(root.transform, false);
            buildAnimal(animal);

            // Red: rejilla de barras rojas delante del animal (la cámara mira hacia +Z).
            Transform netRoot = new GameObject("Red").transform;
            netRoot.SetParent(root.transform, false);
            float half = width * 0.5f;
            for (int i = 0; i <= 4; i++)
            {
                float x = Mathf.Lerp(-half, half, i / 4f);
                Primitive(PrimitiveType.Cube, "BarraV" + i, netRoot, new Vector3(x, 0f, -0.75f), new Vector3(0.05f, 1.7f, 0.05f), net);
            }
            for (int j = 0; j <= 2; j++)
            {
                float y = Mathf.Lerp(-0.7f, 0.7f, j / 2f);
                Primitive(PrimitiveType.Cube, "BarraH" + j, netRoot, new Vector3(0f, y, -0.75f), new Vector3(width, 0.05f, 0.05f), net);
            }

            // Barra de progreso: el "pivote" está a la izquierda para que crezca hacia la derecha.
            Transform bar = new GameObject("BarraRescate").transform;
            bar.SetParent(root.transform, false);
            bar.localPosition = new Vector3(-half, 1.3f, -0.8f);
            Primitive(PrimitiveType.Cube, "Relleno", bar, new Vector3(half, 0f, 0f), new Vector3(width, 0.18f, 0.18f), progress);

            SetRef(logic, "animalVisual", animal);
            SetRef(logic, "netVisual", netRoot.gameObject);
            SetRef(logic, "progressBar", bar);
            SetString(logic, "animalName", animalName);
            SetString(logic, "educationalFact", fact);
            SetInt(logic, "biomassReward", reward);
            return root;
        }

        static GameObject BuildBoat(GameObject[] drops, Material hull, Material cabin)
        {
            var root = new GameObject("Barco_PescaIlegal");
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, -0.3f, 0f);
            col.size = new Vector3(6.5f, 1.8f, 2.6f);
            AddKinematicBody(root);
            IllegalFishingBoat logic = root.AddComponent<IllegalFishingBoat>();

            Transform v = new GameObject("Modelo").transform;
            v.SetParent(root.transform, false);
            Primitive(PrimitiveType.Cube, "Casco", v, new Vector3(0f, -0.2f, 0f), new Vector3(6f, 1.1f, 2.2f), hull);
            Primitive(PrimitiveType.Cube, "Cabina", v, new Vector3(-1.2f, 0.85f, 0f), new Vector3(2f, 1.1f, 1.6f), cabin);
            Primitive(PrimitiveType.Cylinder, "Mastil", v, new Vector3(1.5f, 1.6f, 0f), new Vector3(0.1f, 1.4f, 0.1f), hull);
            Primitive(PrimitiveType.Cube, "Grua", v, new Vector3(2.3f, 2.4f, 0f), new Vector3(1.8f, 0.08f, 0.08f), new Vector3(0f, 0f, -25f), hull);

            Transform dropPoint = new GameObject("PuntoDeRed").transform;
            dropPoint.SetParent(root.transform, false);
            dropPoint.localPosition = new Vector3(2.5f, -1.4f, 0f);

            SetRefArray(logic, "dropPrefabs", drops);
            SetRef(logic, "dropPoint", dropPoint);
            SetRef(logic, "visual", v);
            return root;
        }

        static void BuildDecoration(Transform parent, Material rock, Material coral, Material kelp)
        {
            Transform deco = new GameObject("Decoracion (fondo con paralaje)").transform;
            deco.SetParent(parent, false);
            Random.State previous = Random.state;
            Random.InitState(14);
            for (int i = 0; i < 45; i++)
            {
                float x = Random.Range(-60f, 60f);
                float z = Random.Range(3f, 16f);
                int kind = Random.Range(0, 3);
                if (kind == 0)
                {
                    Vector3 s = new Vector3(Random.Range(1.5f, 4f), Random.Range(1f, 3f), Random.Range(1.5f, 4f));
                    Primitive(PrimitiveType.Sphere, "Roca", deco, new Vector3(x, -22f, z), s, rock);
                }
                else if (kind == 1)
                {
                    float h = Random.Range(1f, 2.5f);
                    Primitive(PrimitiveType.Cylinder, "Coral", deco, new Vector3(x, -22f + h, z), new Vector3(Random.Range(0.4f, 0.8f), h, Random.Range(0.4f, 0.8f)), coral);
                }
                else
                {
                    float h = Random.Range(4f, 12f);
                    Primitive(PrimitiveType.Cube, "Alga", deco, new Vector3(x, -22f + h * 0.5f, z), new Vector3(0.25f, h, 0.25f), kelp);
                }
            }
            Random.state = previous;
        }

        // =====================================================================================
        //  HUD (UI provisional; reemplázala luego con tu diseño de Figma)
        // =====================================================================================

        static HUDController BuildHUD(SharkHealth health)
        {
            var canvasGO = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Transform root = canvasGO.transform;
            HUDController hud = canvasGO.AddComponent<HUDController>();

            Color white = Color.white;
            Color green = new Color(0.6f, 1f, 0.7f);
            Vector2 topLeft = new Vector2(0f, 1f), topRight = new Vector2(1f, 1f), topCenter = new Vector2(0.5f, 1f);
            Vector2 center = new Vector2(0.5f, 0.5f);

            // Parpadeo rojo al recibir daño (pantalla completa, detrás de todo).
            Image flash = Img("ParpadeoDanio", root, new Color(1f, 0.1f, 0.1f, 0f));
            Stretch(flash.rectTransform, 0f);

            // Fondos oscuros para que el HUD se lea sobre el cielo o el agua clara.
            Image topLeftBg = Img("FondoEnergia", root, new Color(0f, 0.05f, 0.1f, 0.6f));
            Place(topLeftBg.rectTransform, topLeft, topLeft, new Vector2(24f, -14f), new Vector2(600f, 110f));
            Image topRightBg = Img("FondoContadores", root, new Color(0f, 0.05f, 0.1f, 0.6f));
            Place(topRightBg.rectTransform, topRight, topRight, new Vector2(-24f, -14f), new Vector2(340f, 160f));
            Image hintBg = Img("FondoControles", root, new Color(0f, 0.05f, 0.1f, 0.5f));
            Place(hintBg.rectTransform, Vector2.zero, Vector2.zero, new Vector2(24f, 18f), new Vector2(860f, 50f));

            // Barra de energía.
            Text barLabel = Txt("EtiquetaEnergia", root, "ENERGÍA  (la toxicidad la drena)", 26, TextAnchor.UpperLeft, white);
            Place(barLabel.rectTransform, topLeft, topLeft, new Vector2(40f, -26f), new Vector2(900f, 40f));
            Image barBg = Img("BarraFondo", root, new Color(0f, 0f, 0f, 0.55f));
            Place(barBg.rectTransform, topLeft, topLeft, new Vector2(40f, -70f), new Vector2(560f, 38f));
            Image fill = Img("BarraRelleno", barBg.transform, new Color(0.2f, 0.9f, 0.6f));
            Stretch(fill.rectTransform, 4f);

            // Contadores.
            Text biomass = Txt("Biomasa", root, "Biomasa: 0", 34, TextAnchor.UpperRight, green);
            Place(biomass.rectTransform, topRight, topRight, new Vector2(-40f, -26f), new Vector2(600f, 46f));
            Text rescued = Txt("Rescates", root, "Rescates: 0", 34, TextAnchor.UpperRight, white);
            Place(rescued.rectTransform, topRight, topRight, new Vector2(-40f, -74f), new Vector2(600f, 46f));
            Text time = Txt("Tiempo", root, "Tiempo: 00:00", 34, TextAnchor.UpperRight, white);
            Place(time.rectTransform, topRight, topRight, new Vector2(-40f, -122f), new Vector2(600f, 46f));

            // Ayuda de controles.
            Text hint = Txt("Controles", root, "Mover: WASD / Flechas o mantén clic  ·  Impulso: Shift  ·  Pausa: P", 22, TextAnchor.LowerLeft, new Color(1f, 1f, 1f, 0.7f));
            Place(hint.rectTransform, Vector2.zero, Vector2.zero, new Vector2(40f, 26f), new Vector2(840f, 34f));

            // Panel de dato educativo.
            Image factPanel = Img("PanelDatoEducativo", root, new Color(0.02f, 0.1f, 0.15f, 0.88f));
            Place(factPanel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(1150f, 150f));
            Text factText = Txt("TextoDato", factPanel.transform, "", 28, TextAnchor.MiddleCenter, white);
            Stretch(factText.rectTransform, 22f);

            // Pausa.
            Image pause = Img("PanelPausa", root, new Color(0f, 0f, 0f, 0.6f));
            Stretch(pause.rectTransform, 0f);
            Text pauseText = Txt("TextoPausa", pause.transform, "PAUSA\n<size=30>Presiona P o Esc para continuar</size>", 72, TextAnchor.MiddleCenter, white);
            Stretch(pauseText.rectTransform, 0f);

            // Fin de partida + tienda.
            Image over = Img("PanelFinDePartida", root, new Color(0f, 0.04f, 0.08f, 0.8f));
            over.raycastTarget = true; // bloquea clics al juego
            Stretch(over.rectTransform, 0f);
            Image box = Img("Caja", over.transform, new Color(0.03f, 0.16f, 0.24f, 0.97f));
            Place(box.rectTransform, center, center, Vector2.zero, new Vector2(1000f, 860f));

            Text title = Txt("Titulo", box.transform, "EL OCÉANO NECESITA OTRO GUARDIÁN", 46, TextAnchor.UpperCenter, new Color(1f, 0.65f, 0.5f));
            Place(title.rectTransform, topCenter, topCenter, new Vector2(0f, -36f), new Vector2(940f, 60f));
            Text stats = Txt("Estadisticas", box.transform, "", 30, TextAnchor.UpperCenter, white);
            Place(stats.rectTransform, topCenter, topCenter, new Vector2(0f, -115f), new Vector2(900f, 240f));
            Text wallet = Txt("BiomasaDisponible", box.transform, "", 32, TextAnchor.MiddleCenter, green);
            Place(wallet.rectTransform, topCenter, center, new Vector2(0f, -390f), new Vector2(900f, 44f));
            Text shopTitle = Txt("TituloTienda", box.transform, "TIENDA DE MEJORAS  (se aplican en la próxima partida)", 24, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.8f));
            Place(shopTitle.rectTransform, topCenter, center, new Vector2(0f, -440f), new Vector2(900f, 34f));

            var buttons = new Button[3];
            var labels = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                buttons[i] = Btn("Mejora_" + (UpgradeType)i, box.transform, "", new Vector2(-310f + 310f * i, -555f), new Vector2(290f, 130f),
                                 new Color(0.1f, 0.45f, 0.4f), 24, out labels[i]);
            }
            Button restart = Btn("BotonReiniciar", box.transform, "JUGAR DE NUEVO  (R)", new Vector2(0f, -730f), new Vector2(460f, 84f),
                                 new Color(0.95f, 0.55f, 0.2f), 32, out _);

            // Conectar referencias del HUD.
            SetRef(hud, "shark", health);
            SetRef(hud, "healthFill", fill.rectTransform);
            SetRef(hud, "healthFillImage", fill);
            SetRef(hud, "biomassText", biomass);
            SetRef(hud, "rescuedText", rescued);
            SetRef(hud, "timeText", time);
            SetRef(hud, "factPanel", factPanel.gameObject);
            SetRef(hud, "factText", factText);
            SetRef(hud, "damageFlash", flash);
            SetRef(hud, "pausePanel", pause.gameObject);
            SetRef(hud, "gameOverPanel", over.gameObject);
            SetRef(hud, "gameOverStatsText", stats);
            SetRef(hud, "walletText", wallet);
            SetRef(hud, "restartButton", restart);
            SetRefArray(hud, "upgradeButtons", buttons);
            SetRefArray(hud, "upgradeLabels", labels);

            factPanel.gameObject.SetActive(false);
            pause.gameObject.SetActive(false);
            over.gameObject.SetActive(false);
            return hud;
        }

        static void BuildEventSystem()
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        // =====================================================================================
        //  UTILIDADES
        // =====================================================================================

        struct EntryDef
        {
            public string label; public GameObject prefab; public int max; public float interval; public Vector2 height; public bool randomRotation;
            public EntryDef(string label, GameObject prefab, int max, float interval, Vector2 height, bool randomRotation)
            {
                this.label = label; this.prefab = prefab; this.max = max; this.interval = interval; this.height = height; this.randomRotation = randomRotation;
            }
        }

        static void SetSpawnEntries(OceanSpawner spawner, EntryDef[] defs)
        {
            var so = new SerializedObject(spawner);
            SerializedProperty arr = so.FindProperty("entries");
            arr.arraySize = defs.Length;
            for (int i = 0; i < defs.Length; i++)
            {
                SerializedProperty el = arr.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("label").stringValue = defs[i].label;
                el.FindPropertyRelative("prefab").objectReferenceValue = defs[i].prefab;
                el.FindPropertyRelative("maxAlive").intValue = defs[i].max;
                el.FindPropertyRelative("interval").floatValue = defs[i].interval;
                el.FindPropertyRelative("heightRange").vector2Value = defs[i].height;
                el.FindPropertyRelative("randomRotation").boolValue = defs[i].randomRotation;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Shader LitShader()
        {
            if (cachedShader != null) return cachedShader;
            cachedShader = Shader.Find("Universal Render Pipeline/Lit");   // URP (plantilla Universal 3D)
            if (cachedShader == null) cachedShader = Shader.Find("Standard"); // Built-in
            return cachedShader;
        }

        static Material Mat(string name, Color color, float smoothness, bool emissive = false)
        {
            string path = MatDir + "/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = m == null;
            if (isNew) m = new Material(LitShader());

            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (emissive && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * 1.5f);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            if (isNew) AssetDatabase.CreateAsset(m, path);
            else EditorUtility.SetDirty(m);
            return m;
        }

        static TrashData Trash(string file, string displayName, float heal, int biomass, string fact)
        {
            string path = DataDir + "/" + file + ".asset";
            TrashData d = AssetDatabase.LoadAssetAtPath<TrashData>(path);
            bool isNew = d == null;
            if (isNew) d = ScriptableObject.CreateInstance<TrashData>();
            d.displayName = displayName;
            d.healAmount = heal;
            d.biomass = biomass;
            d.educationalFact = fact;
            if (isNew) AssetDatabase.CreateAsset(d, path);
            else EditorUtility.SetDirty(d);
            return d;
        }

        static GameObject SavePrefab(GameObject go)
        {
            string path = PrefabDir + "/" + go.name + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            return Primitive(type, name, parent, localPos, localScale, Vector3.zero, mat);
        }

        static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Vector3 localEuler, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = localScale;
            Object.DestroyImmediate(go.GetComponent<Collider>()); // solo visual; los colliders van en la raíz
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static void AddKinematicBody(GameObject go)
        {
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        static void AddSceneToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // ---------- Asignar campos privados [SerializeField] desde el editor ----------

        static void SetRef(Object target, string prop, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(prop);
            if (p == null) { Debug.LogWarning("[ECO SHARK] No existe el campo '" + prop + "' en " + target); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetRefArray(Object target, string prop, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(prop);
            if (p == null) { Debug.LogWarning("[ECO SHARK] No existe el campo '" + prop + "' en " + target); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetString(Object target, string prop, string value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(prop);
            if (p == null) return;
            p.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetInt(Object target, string prop, int value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(prop);
            if (p == null) return;
            p.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- UI ----------

        static Font uiFont;
        static Font UIFont()
        {
            if (uiFont != null) return uiFont;
            try { uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { uiFont = null; }
            if (uiFont == null)
            {
                try { uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { uiFont = null; }
            }
            return uiFont;
        }

        static Image Img(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Text Txt(string name, Transform parent, string text, int size, TextAnchor align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            t.font = UIFont();
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static Button Btn(string name, Transform parent, string label, Vector2 pos, Vector2 size, Color color, int fontSize, out Text labelText)
        {
            Image img = Img(name, parent, color);
            img.raycastTarget = true;
            Place(img.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), pos, size);
            Button b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            ColorBlock colors = b.colors;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            b.colors = colors;
            labelText = Txt("Texto", img.transform, label, fontSize, TextAnchor.MiddleCenter, Color.white);
            Stretch(labelText.rectTransform, 8f);
            return b;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

        static void Stretch(RectTransform rt, float padding)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }
    }
}
