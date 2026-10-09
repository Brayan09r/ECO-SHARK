using UnityEngine;

namespace EcoShark
{
    /// <summary>
    /// Capa de paralaje 2.5D. "screenSpeed" es qué tan rápido se ve pasar la capa comparada con el plano de juego:
    ///   0.15–0.2 = fondo lejano (montañas), 0.5 = fondo medio (rocas y arrecifes), 1.2 = primer plano (algas cerca de la cámara).
    /// La cámara es de perspectiva, así que la distancia ya da algo de paralaje; este script calcula cuánto
    /// debe acompañar la capa a la cámara para que la velocidad en pantalla sea EXACTAMENTE la pedida.
    /// Va en el objeto padre de cada capa (hijos de "FondoMarinoVivo").
    /// </summary>
    [DefaultExecutionOrder(200)] // después de CameraFollow
    public class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("Velocidad en pantalla respecto al plano de juego (1 = igual que el tiburón).")]
        [SerializeField] float screenSpeed = 0.5f;
        [Tooltip("También acompaña a la cámara en vertical.")]
        [SerializeField] bool followY = true;
        [Tooltip("Profundidad (z) del plano de juego.")]
        [SerializeField] float playPlaneZ = 0f;

        [Header("Referencia (la pone el instalador)")]
        [SerializeField] Vector3 basePosition;
        [SerializeField] Vector3 referenceCamera = new Vector3(0f, 0f, -18f);

        Camera cam;

        public float ScreenSpeed => screenSpeed;

        /// <summary>Guarda la posición actual como base, junto con la posición de la cámara en ese momento.</summary>
        public void SetReference(Vector3 cameraPosition)
        {
            basePosition = transform.position;
            referenceCamera = cameraPosition;
        }

        public void Configure(float speed, bool vertical)
        {
            screenSpeed = speed;
            followY = vertical;
        }

        void LateUpdate()
        {
            if (!Application.isPlaying) return;
            if (cam == null) cam = Camera.main;
            if (cam != null) ApplyFor(cam.transform.position);
        }

        /// <summary>Coloca la capa como se vería con la cámara en esa posición.</summary>
        public void ApplyFor(Vector3 cameraPosition)
        {
            float layerDistance = basePosition.z - cameraPosition.z;
            float planeDistance = playPlaneZ - cameraPosition.z;
            if (planeDistance <= 0.01f || layerDistance <= 0.01f) return;

            // Velocidad natural en pantalla = planeDistance / layerDistance. Acompañar un factor f la multiplica por (1 - f).
            float follow = 1f - screenSpeed * layerDistance / planeDistance;
            Vector3 delta = cameraPosition - referenceCamera;
            transform.position = basePosition + new Vector3(delta.x * follow, followY ? delta.y * follow : 0f, 0f);
        }
    }
}
