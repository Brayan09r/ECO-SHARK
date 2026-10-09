using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EcoShark.EditorScripts
{
    /// <summary>
    /// Prepara el sonido: crea el objeto "Audio" (EcoAudio) en la escena y las carpetas donde van los archivos.
    /// Menú: ECO SHARK > 8. Preparar sonido   (o la línea "audio" en Capturas/pedir_fotos.txt)
    /// Después solo hay que dejar los audios en Assets/EcoShark/Resources/Audio/ con los nombres de LEEME_AUDIO.txt.
    /// </summary>
    public static class EcoAudioInstaller
    {
        public const string AudioRoot = "Assets/EcoShark/Resources/Audio";

        const string Readme =
@"AUDIOS DE ECO SHARK — se cargan solos por NOMBRE (sin importar la extensión: .ogg, .mp3 o .wav).
Si falta alguno, ese sonido no suena, pero el juego funciona igual.

Musica/musica_fondo            música en bucle (tranquila, submarina, alegre tipo juego móvil)
Ambiente/ambiente_submarino    ambiente en bucle (agua, burbujas lejanas)
SFX/nado_loop                  agua moviéndose al nadar, en bucle (suena más fuerte con la velocidad)
SFX/impulso                    ""whoosh"" al usar el impulso (Shift)
SFX/recoger_basura             ""pop"" / recompensa al recoger basura
SFX/rescate_animal             sonido alegre de logro al liberar un animal
SFX/animal_perdido             sonido triste corto cuando un animal se pierde
SFX/golpe                      golpe sordo bajo el agua al recibir daño
SFX/fin_partida                fin de partida (triste, corto)
SFX/burbujas                   burbujas sueltas
SFX/chapoteo                   chapoteo al llegar a la superficie

Créditos y licencias: CREDITOS_AUDIO.md en la carpeta del proyecto.
Tecla M en el juego: silenciar / activar.";

        [MenuItem("ECO SHARK/8. Preparar sonido", false, 8)]
        static void Menu()
        {
            Install(out string report);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[ECO SHARK] " + report);
        }

        public static bool Install(out string report)
        {
            string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", AudioRoot));
            foreach (string sub in new[] { "Musica", "Ambiente", "SFX" })
                Directory.CreateDirectory(Path.Combine(full, sub));
            File.WriteAllText(Path.Combine(full, "LEEME_AUDIO.txt"), Readme);
            AssetDatabase.Refresh();

            EcoAudio audio = Object.FindFirstObjectByType<EcoAudio>();
            if (audio == null)
            {
                var go = new GameObject("Audio");
                audio = go.AddComponent<EcoAudio>();
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            report = "Sonido listo: objeto 'Audio' en la escena y carpetas en " + AudioRoot;
            return true;
        }
    }
}
