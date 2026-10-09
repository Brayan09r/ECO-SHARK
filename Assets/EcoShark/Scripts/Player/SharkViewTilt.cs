using UnityEngine;

namespace EcoShark
{
    /// <summary>
    /// Inclina el modelo del tiburón hacia la cámara (se ve un poco del lomo, como en los juegos 2.5D móviles).
    /// Poca inclinación (12°): el aleteo lateral se lee de lado a lado, no de arriba abajo. (Claude)
    /// Solo cambia el modelo visual (hijo); el Rigidbody y el movimiento no se tocan.
    /// Va en el objeto del modelo 3D (hijo de Tiburon_Guardian).
    /// </summary>
    public class SharkViewTilt : MonoBehaviour
    {
        [Tooltip("Grados que el lomo se inclina hacia la cámara.")]
        [Range(0f, 45f)]
        [SerializeField] float tiltDegrees = 12f;
        [Tooltip("Qué tan rápido se ajusta la inclinación al girar.")]
        [SerializeField] float smoothing = 8f;

        Transform root;
        Camera cam;

        void Awake() => root = transform.parent;

        void LateUpdate()
        {
            if (root == null) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            Vector3 forward = root.forward;
            Vector3 toCamera = -cam.transform.forward;
            float rad = tiltDegrees * Mathf.Deg2Rad;
            Vector3 up = root.up * Mathf.Cos(rad) + toCamera * Mathf.Sin(rad);
            up = Vector3.ProjectOnPlane(up, forward);
            if (up.sqrMagnitude < 0.0001f) return;

            Quaternion target = Quaternion.LookRotation(forward, up.normalized);
            float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, t);
        }
    }
}
