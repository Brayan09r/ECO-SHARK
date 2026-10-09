using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EcoShark.EditorScripts
{
    /// <summary>
    /// ECO SHARK > 9. Menú principal y HUD   (línea "menu" en Capturas/pedir_fotos.txt)
    ///   Agrega "MenuPrincipal" (MainMenuController) y la nueva barra de energía (HudTopBar) al HUD.
    /// ECO SHARK > 10. Océano grande          (línea "oceano")
    ///   Amplía el mar (X -150..150, Y -40..6), el suelo y las olas; más basura, más peligros y 3 barcos.
    ///   Después reconstruye el fondo marino para cubrir el nuevo tamaño.
    /// </summary>
    public static class GameShellInstaller
    {
        public static readonly Vector2 OceanMin = new Vector2(-150f, -40f);
        public static readonly Vector2 OceanMax = new Vector2(150f, 6f);

        [MenuItem("ECO SHARK/9. Menú principal y HUD", false, 9)]
        static void MenuInstall()
        {
            Debug.Log("[ECO SHARK] " + InstallMenu());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        [MenuItem("ECO SHARK/10. Océano grande", false, 10)]
        static void MenuOcean()
        {
            Debug.Log("[ECO SHARK] " + ExpandOcean());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[ECO SHARK] Capturas: " + SeaBackgroundInstaller.CaptureViews());
        }

        public static string InstallMenu()
        {
            if (Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include) == null)
                new GameObject("MenuPrincipal").AddComponent<MainMenuController>();

            HUDController hud = Object.FindFirstObjectByType<HUDController>(FindObjectsInactive.Include);
            if (hud != null && hud.GetComponent<HudTopBar>() == null) hud.gameObject.AddComponent<HudTopBar>();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return "Menú principal y nueva barra de energía instalados." + (hud == null ? " (No se encontró el HUD.)" : "");
        }

        public static string ExpandOcean()
        {
            OceanArea area = Object.FindFirstObjectByType<OceanArea>();
            if (area == null) return "No hay OceanArea en la escena.";
            var so = new SerializedObject(area);
            so.FindProperty("min").vector2Value = OceanMin;
            so.FindProperty("max").vector2Value = OceanMax;
            so.ApplyModifiedPropertiesWithoutUndo();

            float width = OceanMax.x - OceanMin.x;

            // Suelo de arena.
            GameObject env = GameObject.Find("Entorno");
            if (env != null)
            {
                Transform floor = env.transform.Find("FondoMarino");
                if (floor != null)
                {
                    floor.position = new Vector3(0f, OceanMin.y - 1.5f, 4f);
                    floor.localScale = new Vector3(width + 140f, 3f, 30f);
                }
            }

            // Olas: más anchas.
            OceanSurface surface = Object.FindFirstObjectByType<OceanSurface>();
            if (surface != null)
            {
                var ss = new SerializedObject(surface);
                ss.FindProperty("halfWidth").floatValue = width * 0.5f + 80f;
                ss.ApplyModifiedPropertiesWithoutUndo();
                surface.enabled = false;
                surface.enabled = true; // regenera la franja
            }

            // Más basura y peligros (el mar es ~6 veces más grande).
            OceanSpawner spawner = Object.FindFirstObjectByType<OceanSpawner>();
            if (spawner != null)
            {
                var sp = new SerializedObject(spawner);
                SerializedProperty entries = sp.FindProperty("entries");
                for (int i = 0; i < entries.arraySize; i++)
                {
                    SerializedProperty e = entries.GetArrayElementAtIndex(i);
                    string label = e.FindPropertyRelative("label").stringValue;
                    int max; float interval;
                    switch (label)
                    {
                        case "Botellas": max = 60; interval = 0.7f; break;
                        case "Bolsas": max = 45; interval = 0.9f; break;
                        case "Latas": max = 45; interval = 1f; break;
                        case "Petróleo": max = 12; interval = 5f; break;
                        case "Barriles radiactivos": max = 14; interval = 4f; break;
                        default: continue;
                    }
                    e.FindPropertyRelative("maxAlive").intValue = max;
                    e.FindPropertyRelative("interval").floatValue = interval;
                }
                sp.ApplyModifiedPropertiesWithoutUndo();
            }

            // 3 barcos de pesca ilegal (más redes con animales que rescatar).
            IllegalFishingBoat[] boats = Object.FindObjectsByType<IllegalFishingBoat>(FindObjectsSortMode.None);
            GameObject boatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EcoShark/Generated/Prefabs/Barco_PescaIlegal.prefab");
            float[] boatX = { -95f, 95f };
            for (int i = boats.Length; i < 3 && boatPrefab != null; i++)
            {
                var boat = (GameObject)PrefabUtility.InstantiatePrefab(boatPrefab);
                boat.transform.position = new Vector3(boatX[(i - 1) % 2], 6.6f, 0f);
                Transform model = boat.transform.Find("Modelo");
                if (model != null && model.GetComponent<FloatOnWaves>() == null) model.gameObject.AddComponent<FloatOnWaves>();
            }

            // Un par de animales atrapados más, lejos del centro.
            if (Object.FindObjectsByType<TrappedAnimal>(FindObjectsSortMode.None).Length < 4)
            {
                Place("Assets/EcoShark/Generated/Prefabs/Rescate_Tortuga.prefab", new Vector3(-78f, -27f, 0f));
                Place("Assets/EcoShark/Generated/Prefabs/Rescate_Delfin.prefab", new Vector3(84f, -19f, 0f));
            }

            SeaBackgroundInstaller.Install(out string sea);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return "Océano ampliado a X " + OceanMin.x + ".." + OceanMax.x + ", Y " + OceanMin.y + ".." + OceanMax.y + ". " + sea;
        }

        static void Place(string prefabPath, Vector3 position)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = position;
        }
    }
}
