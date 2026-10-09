using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EcoShark.EditorScripts
{
    /// <summary>
    /// (Claude)
    /// ECO SHARK > 11. Controles táctiles, guía y peces   (línea "mejoras" en Capturas/pedir_fotos.txt)
    ///   Agrega al HUD: TouchControls (joystick + impulso + pausa), ObjectiveGuide (flechas a los animales
    ///   atrapados) y RunGoals (combo, misiones y números flotantes). Crea "VidaMarina" (FishSchools).
    ///   Se puede ejecutar varias veces: no duplica nada.
    /// ECO SHARK > 12. Probar controles táctiles en el editor   (línea "tactil")
    ///   Muestra u oculta el joystick en PC para probarlo con el mouse. En móvil aparece solo.
    /// </summary>
    public static class GameplayUpgradeInstaller
    {
        [MenuItem("ECO SHARK/11. Controles táctiles, guía y peces", false, 11)]
        static void MenuInstall()
        {
            Debug.Log("[ECO SHARK] " + Install());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        [MenuItem("ECO SHARK/12. Probar controles táctiles en el editor (mostrar u ocultar)", false, 12)]
        static void MenuTouchPreview()
        {
            Debug.Log("[ECO SHARK] " + ToggleTouchPreview());
            if (!Application.isPlaying) EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        public static string Install()
        {
            HUDController hud = Object.FindFirstObjectByType<HUDController>(FindObjectsInactive.Include);
            if (hud == null) return "No se encontró el HUD (HUDController) en la escena.";

            int added = 0;
            if (hud.GetComponent<TouchControls>() == null) { hud.gameObject.AddComponent<TouchControls>(); added++; }
            if (hud.GetComponent<ObjectiveGuide>() == null) { hud.gameObject.AddComponent<ObjectiveGuide>(); added++; }
            if (hud.GetComponent<RunGoals>() == null) { hud.gameObject.AddComponent<RunGoals>(); added++; }

            if (Object.FindFirstObjectByType<FishSchools>(FindObjectsInactive.Include) == null)
            {
                var go = new GameObject("VidaMarina");
                go.AddComponent<FishSchools>();
                added++;
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return added == 0
                ? "Controles táctiles, guía de rescates, misiones y peces: ya estaban instalados."
                : "Instalados controles táctiles, guía de rescates, misiones/combo y cardúmenes (" + added + " componentes nuevos).";
        }

        public static string ToggleTouchPreview()
        {
            TouchControls touch = Object.FindFirstObjectByType<TouchControls>(FindObjectsInactive.Include);
            if (touch == null) return "No hay TouchControls en la escena. Usa primero el menú 11.";
            var so = new SerializedObject(touch);
            SerializedProperty prop = so.FindProperty("forceVisible");
            prop.boolValue = !prop.boolValue;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return "Controles táctiles en el editor: " + (prop.boolValue ? "VISIBLES (prueba con el mouse)." : "ocultos (en móvil aparecen solos).");
        }
    }
}
