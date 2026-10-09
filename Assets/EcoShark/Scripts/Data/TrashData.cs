using UnityEngine;

namespace EcoShark
{
    /// <summary>
    /// Ficha de un tipo de basura (ScriptableObject = archivo de datos editable desde el Inspector).
    /// Cambiar cuánto cura una botella NO requiere tocar código.
    /// Crear nuevas: clic derecho en Project > Create > ECO SHARK > Tipo de basura.
    /// </summary>
    [CreateAssetMenu(menuName = "ECO SHARK/Tipo de basura", fileName = "Basura_Nueva")]
    public class TrashData : ScriptableObject
    {
        public string displayName = "Botella de plástico";

        [Tooltip("Energía que recupera el tiburón al tragarla.")]
        public float healAmount = 8f;

        [Tooltip("Biomasa (moneda) que otorga.")]
        public int biomass = 5;

        [Tooltip("Dato educativo que aparece la primera vez que se recoge en la partida. Cita tu fuente en el documento.")]
        [TextArea(2, 4)]
        public string educationalFact;
    }
}
