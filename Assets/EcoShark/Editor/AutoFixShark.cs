using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace EcoShark.EditorScripts
{
    public static class AutoFixShark
    {
        [InitializeOnLoadMethod]
        public static void FixNow()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                
                GameObject shark = GameObject.Find("Tiburon_Guardian");
                if (shark == null) return;
                
                Transform shark3D = shark.transform.Find("Tiburon_Martillo_3D");
                if (shark3D == null)
                {
                    // Call the builder method to equip it
                    EcoSharkSceneBuilder.EquipHammerheadModel();
                    shark3D = shark.transform.Find("Tiburon_Martillo_3D");
                    if (shark3D == null) return;
                }

                // 1. Fix Rotation: The Blender model was built with Z=Forward in Blender's space, 
                // which translates to Y=Forward in Unity after FBX conversion. We pitch it down 90 degrees.
                shark3D.localRotation = Quaternion.Euler(90f, 0f, 0f);

                // Fix material explicitly
                Renderer[] renderers = shark3D.GetComponentsInChildren<Renderer>();
                Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/EcoShark/Generated/Materials/SharkVCol.mat");
                if (mat == null)
                {
                    Shader shader = Shader.Find("Sprites/Default"); 
                    if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
                    mat = new Material(shader);
                    AssetDatabase.CreateAsset(mat, "Assets/EcoShark/Generated/Materials/SharkVCol.mat");
                }
                foreach (Renderer r in renderers)
                {
                    // (Claude) No pisar el material de los ojos que crea SharkColorizer.
                    if (r.gameObject.name.StartsWith("Ojo_")) continue;
                    r.sharedMaterial = mat;
                }

                // 2. Fix Animation
                GameObject hammerheadAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EcoShark/Models/Sharks/Shark_Hammerhead.fbx");
                if (hammerheadAsset != null)
                {
                    // Forzar el importador a Legacy para que el componente Animation lo acepte
                    ModelImporter importer = AssetImporter.GetAtPath("Assets/EcoShark/Models/Sharks/Shark_Hammerhead.fbx") as ModelImporter;
                    if (importer != null && importer.animationType != ModelImporterAnimationType.Legacy)
                    {
                        importer.animationType = ModelImporterAnimationType.Legacy;
                        importer.SaveAndReimport();
                    }

                    Animator anim = shark3D.GetComponent<Animator>();
                    if (anim != null) Object.DestroyImmediate(anim);

                    Animation legacyAnim = shark3D.GetComponent<Animation>();
                    if (legacyAnim == null) legacyAnim = shark3D.gameObject.AddComponent<Animation>();

                    AnimationClip[] clips = AnimationUtility.GetAnimationClips(hammerheadAsset);
                    foreach (var clip in clips)
                    {
                        if (clip.name.Contains("Swim"))
                        {
                            legacyAnim.AddClip(clip, "Swim");
                            legacyAnim.clip = clip;
                            legacyAnim.wrapMode = WrapMode.Loop;
                            legacyAnim.Play("Swim");
                            break;
                        }
                    }
                }
                
                // 3. (Claude) Pintar con los colores del diseño si aún no está pintado
                //    (malla con contrasombreado + shader con luz + ojos). Ver SharkColorizer.cs.
                if (!SharkColorizer.IsPainted(shark3D)) SharkColorizer.Apply(shark3D);

                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                Debug.Log("[ECO SHARK] ¡Tiburón 3D configurado automáticamente! Dale a Play.");
            };
        }
    }
}
// Trigger domain reload after FBX re-export with fixed bone wave animation.

