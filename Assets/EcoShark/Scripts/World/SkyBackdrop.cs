using UnityEngine;

namespace EcoShark
{
    /// <summary>
    /// Fondo lejano pegado a la cámara: cielo con nubes arriba y agua con degradado abajo.
    /// Cada cuadro mueve la línea del horizonte para que coincida con la superficie del juego
    /// (así, visto desde la cámara 2.5D, el cielo empieza justo donde terminan las olas).
    /// Va en un hijo de la cámara ("FondoCielo") con el material "EcoShark/SkyBackdrop".
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SkyBackdrop : MonoBehaviour
    {
        [Tooltip("Distancia a la cámara. Debe ser menor que el 'Far' de la cámara.")]
        [SerializeField] float distance = 150f;
        [Tooltip("Qué tan por debajo de la línea del agua empieza el agua del fondo (queda tapado por las olas).")]
        [SerializeField] float horizonBelowSurface = 0.9f;
        [Tooltip("Margen extra alrededor de lo que ve la cámara.")]
        [SerializeField] float overscan = 1.3f;

        static readonly int SplitId = Shader.PropertyToID("_SplitY");
        static readonly int GradId = Shader.PropertyToID("_GradStartY");
        static readonly int ScaleId = Shader.PropertyToID("_Scale");
        static readonly int CamXId = Shader.PropertyToID("_CamX");

        Camera cam;
        Mesh mesh;
        MaterialPropertyBlock block;
        float builtFov, builtAspect;

        void OnEnable() => builtFov = -1f;

        void LateUpdate() => Refresh();

        /// <summary>Recalcula el horizonte ya (lo usa el instalador para sacar capturas con la cámara movida).</summary>
        public void Refresh()
        {
            if (cam == null) cam = GetComponentInParent<Camera>();
            if (cam == null) return;

            float d = Mathf.Min(distance, cam.farClipPlane * 0.95f);
            transform.localPosition = new Vector3(0f, 0f, d);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            if (!Mathf.Approximately(builtFov, cam.fieldOfView) || !Mathf.Approximately(builtAspect, cam.aspect) || mesh == null)
                BuildMesh(d);

            // Superficie del juego (si no hay olas, usa el límite del océano).
            float surfaceY = 6f, surfaceZ = 0.6f, depth = 2.2f;
            OceanSurface surface = OceanSurface.Current;
            if (surface != null)
            {
                surfaceY = surface.SurfaceY;
                surfaceZ = surface.transform.position.z;
                depth = surface.Depth;
            }
            else if (OceanArea.Current != null)
            {
                surfaceY = OceanArea.Current.SurfaceY;
            }

            float x = cam.transform.position.x;
            float split = ProjectY(new Vector3(x, surfaceY - horizonBelowSurface, surfaceZ), d);
            float gradStart = ProjectY(new Vector3(x, surfaceY - depth, surfaceZ), d);

            if (block == null) block = new MaterialPropertyBlock();
            Renderer r = GetComponent<Renderer>();
            r.GetPropertyBlock(block);
            block.SetFloat(SplitId, split);
            block.SetFloat(GradId, Mathf.Min(gradStart, split));
            block.SetFloat(ScaleId, d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad));
            block.SetFloat(CamXId, x);
            r.SetPropertyBlock(block);
        }

        /// <summary>Altura (local de la cámara) en la que se ve ese punto, llevada a la distancia del fondo.</summary>
        float ProjectY(Vector3 world, float d)
        {
            Vector3 lp = cam.transform.InverseTransformPoint(world);
            if (lp.z < 0.01f) return 0f;
            return lp.y * d / lp.z;
        }

        void BuildMesh(float d)
        {
            builtFov = cam.fieldOfView;
            builtAspect = cam.aspect;
            float halfH = d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * overscan;
            float halfW = halfH * Mathf.Max(0.1f, cam.aspect);

            if (mesh == null) mesh = new Mesh { name = "Fondo cielo (generado)", hideFlags = HideFlags.DontSave };
            mesh.Clear();
            mesh.vertices = new[]
            {
                new Vector3(-halfW, -halfH, 0f), new Vector3(-halfW, halfH, 0f),
                new Vector3(halfW, halfH, 0f), new Vector3(halfW, -halfH, 0f)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }
    }
}
