using UnityEngine;

namespace EcoShark
{
    /// <summary>
    /// Hace que un objeto flote sobre las olas de OceanSurface: sube, baja y se ladea con ellas.
    /// Solo mueve el modelo visual (ponlo en el hijo "Modelo" del barco); la lógica y el collider no cambian.
    /// </summary>
    public class FloatOnWaves : MonoBehaviour
    {
        [Tooltip("Cuánto se hunde en el agua (unidades). Un barco real lleva parte del casco bajo el agua.")]
        [SerializeField] float draft = 0.45f;
        [Tooltip("Cuánto sigue la altura de la ola (1 = igual que el agua).")]
        [SerializeField] float follow = 0.85f;
        [Tooltip("Cuánto se ladea con la pendiente de la ola.")]
        [SerializeField] float tilt = 0.6f;
        [Tooltip("Separación (unidades) para medir la pendiente: parecido al largo del barco.")]
        [SerializeField] float sampleSpacing = 2.5f;

        float baseY;
        Quaternion lastWritten;
        Quaternion baseRotation;

        void Start()
        {
            baseY = transform.localPosition.y;
            baseRotation = transform.localRotation;
            lastWritten = baseRotation;
        }

        void LateUpdate()
        {
            if (OceanSurface.Current == null) return;

            float x = transform.position.x;
            float h = OceanSurface.HeightAt(x);
            float slope = (OceanSurface.HeightAt(x + sampleSpacing * 0.5f) - OceanSurface.HeightAt(x - sampleSpacing * 0.5f)) / sampleSpacing;

            Vector3 p = transform.localPosition;
            p.y = baseY - draft + h * follow;
            transform.localPosition = p;

            // Si otro script (el barco al voltearse) cambió la rotación, esa es la nueva base.
            if (transform.localRotation != lastWritten) baseRotation = transform.localRotation;
            float angle = Mathf.Atan(slope) * Mathf.Rad2Deg * tilt;
            lastWritten = Quaternion.Euler(0f, 0f, angle) * baseRotation;
            transform.localRotation = lastWritten;
        }
    }
}
