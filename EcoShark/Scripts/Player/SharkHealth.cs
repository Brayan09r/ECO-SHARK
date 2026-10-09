using System.Collections.Generic;
using UnityEngine;

namespace EcoShark
{
    /// <summary>
    /// SISTEMA DE VIDA / TOXICIDAD.
    ///
    /// Responsabilidad única: guardar la energía del tiburón y avisar cuando cambia.
    /// NO sabe qué es una botella, un barril o el HUD. Solo ofrece:
    ///   - Drenaje constante por toxicidad (en Update).
    ///   - Heal(cantidad)        -> lo usan la basura y los rescates.
    ///   - TakeDamage(cantidad)  -> lo usan barriles y barcos (con invulnerabilidad breve).
    ///   - SetExtraDrain(...)    -> zonas tóxicas como el petróleo.
    ///   - Eventos: HealthChanged, Damaged, Healed, Died (el HUD y el GameManager escuchan).
    ///
    /// Fórmula del drenaje por segundo:
    ///   drenaje = drenajeBase × dificultad (sube con el tiempo) × resistencia (mejora de tienda) + extras (petróleo)
    /// </summary>
    public class SharkHealth : MonoBehaviour
    {
        [Header("Energía")]
        [SerializeField] float maxHealth = 100f;

        [Header("Toxicidad")]
        [Tooltip("Energía que se pierde por segundo al inicio de la partida.")]
        [SerializeField] float baseDrainPerSecond = 2.5f;

        [Header("Daño")]
        [Tooltip("Segundos de invulnerabilidad tras recibir un golpe (evita perder toda la vida en un solo choque).")]
        [SerializeField] float invulnerabilityTime = 0.8f;

        public float Current { get; private set; }
        public float Max => maxHealth;
        public float Normalized => maxHealth > 0f ? Current / maxHealth : 0f;
        public bool IsDead { get; private set; }
        public bool IsInvulnerable => Time.time < invulnerableUntil;

        /// <summary>Lo ajusta el GameManager: 1 = normal, 2 = doble drenaje...</summary>
        public float DifficultyMultiplier { get; set; } = 1f;

        /// <summary>Permite pausar el drenaje (tutorial, cinemáticas).</summary>
        public bool DrainEnabled { get; set; } = true;

        public float CurrentDrainPerSecond
        {
            get
            {
                float extra = 0f;
                foreach (float e in extraDrains.Values) extra += e;
                return baseDrainPerSecond * DifficultyMultiplier * resistanceMultiplier + extra;
            }
        }

        /// <summary>(actual, máximo) — para la barra del HUD.</summary>
        public event System.Action<float, float> HealthChanged;
        /// <summary>Cantidad de daño recibido — para parpadeo rojo, sonido, vibración.</summary>
        public event System.Action<float> Damaged;
        /// <summary>Cantidad curada — para partículas verdes, sonido de "ñam".</summary>
        public event System.Action<float> Healed;
        public event System.Action Died;

        float resistanceMultiplier = 1f;
        float invulnerableUntil;
        readonly Dictionary<object, float> extraDrains = new Dictionary<object, float>();

        void Awake()
        {
            Current = maxHealth;
            resistanceMultiplier = Mathf.Max(0.3f, Progression.DrainMultiplier);
        }

        void Update()
        {
            if (IsDead || !DrainEnabled) return;
            ChangeHealth(-CurrentDrainPerSecond * Time.deltaTime);
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            float before = Current;
            ChangeHealth(amount);
            float healed = Current - before;
            if (healed > 0f) Healed?.Invoke(healed);
        }

        /// <returns>true si el daño se aplicó (false si estaba invulnerable o muerto).</returns>
        public bool TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f || IsInvulnerable) return false;
            invulnerableUntil = Time.time + invulnerabilityTime;
            Damaged?.Invoke(amount);
            ChangeHealth(-amount);
            return true;
        }

        /// <summary>Drenaje adicional mientras dure (ej. dentro de una mancha de petróleo).</summary>
        public void SetExtraDrain(object source, float perSecond) => extraDrains[source] = perSecond;

        public void RemoveExtraDrain(object source) => extraDrains.Remove(source);

        void ChangeHealth(float delta)
        {
            Current = Mathf.Clamp(Current + delta, 0f, maxHealth);
            HealthChanged?.Invoke(Current, maxHealth);

            if (Current <= 0f && !IsDead)
            {
                IsDead = true;
                Died?.Invoke();
                GameEvents.RaiseSharkDied();
            }
        }
    }
}
