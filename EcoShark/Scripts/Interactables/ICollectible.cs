using UnityEngine;

namespace EcoShark
{
    /// <summary>
    /// Contrato de "cualquier cosa que el tiburón puede tragar".
    /// Hoy: basura. Mañana: power-ups, burbujas de oxígeno, etc., sin tocar el código del tiburón.
    /// </summary>
    public interface ICollectible
    {
        bool CanBeCollected { get; }

        /// <summary>El imán del tiburón lo llama cada paso de física mientras esté en rango.</summary>
        void Attract(Vector3 target, float speed);

        /// <summary>Se ejecuta al tocar al tiburón.</summary>
        void Collect(SharkCollector collector);
    }
}
