#if UNITY_EDITOR
using System.IO;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EcoShark
{
    /// <summary>
    /// (Claude) Capturas para el pitch / informe: en Play, la tecla F9 guarda la vista Game en
    /// "Capturas/pitch_HHmmss.png" al triple de resolución. Todo el archivo está dentro de #if UNITY_EDITOR: no entra en la build.
    /// </summary>
    public static class PitchCapture
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            var go = new GameObject("~CapturasPitch") { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(go);
            go.AddComponent<PitchCaptureKey>();
        }
    }

    public class PitchCaptureKey : MonoBehaviour
    {
        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            bool pressed = kb != null && kb.f9Key.wasPressedThisFrame;
#else
            bool pressed = Input.GetKeyDown(KeyCode.F9);
#endif
            if (!pressed) return;
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Capturas"));
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "pitch_" + System.DateTime.Now.ToString("HHmmss") + ".png");
            ScreenCapture.CaptureScreenshot(path, 3);
            Debug.Log("[ECO SHARK] Captura para el pitch: " + path);
        }
    }
}
#endif
