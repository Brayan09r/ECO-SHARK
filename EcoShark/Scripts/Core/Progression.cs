using UnityEngine;

namespace EcoShark
{
    public enum UpgradeType
    {
        Speed = 0,      // Velocidad
        Magnet = 1,     // Radio de atracción
        Resistance = 2  // Resistencia a la toxicidad
    }

    /// <summary>
    /// Economía persistente: Biomasa total, niveles de mejora y récord.
    /// Usa PlayerPrefs, que en WebGL se guarda en el navegador (IndexedDB),
    /// así el progreso sobrevive aunque el profesor recargue la página.
    /// </summary>
    public static class Progression
    {
        public const int MaxLevel = 5;

        const string BiomassKey = "ecoshark_biomass";
        const string BestTimeKey = "ecoshark_best_time";
        const string UpgradePrefix = "ecoshark_upgrade_";

        // ---------- Biomasa ----------
        public static int Biomass => PlayerPrefs.GetInt(BiomassKey, 0);

        public static void AddBiomass(int amount)
        {
            if (amount <= 0) return;
            PlayerPrefs.SetInt(BiomassKey, Biomass + amount);
            PlayerPrefs.Save();
        }

        // ---------- Récord ----------
        public static float BestTime => PlayerPrefs.GetFloat(BestTimeKey, 0f);

        /// <returns>true si es un nuevo récord.</returns>
        public static bool SubmitRunTime(float seconds)
        {
            if (seconds <= BestTime) return false;
            PlayerPrefs.SetFloat(BestTimeKey, seconds);
            PlayerPrefs.Save();
            return true;
        }

        // ---------- Mejoras ----------
        public static int GetLevel(UpgradeType type) => PlayerPrefs.GetInt(UpgradePrefix + type, 0);
        public static bool IsMaxed(UpgradeType type) => GetLevel(type) >= MaxLevel;

        public static int GetCost(UpgradeType type)
        {
            int baseCost = type == UpgradeType.Resistance ? 60 : 40;
            return baseCost * (GetLevel(type) + 1); // 40, 80, 120, 160, 200...
        }

        public static bool CanBuy(UpgradeType type) => !IsMaxed(type) && Biomass >= GetCost(type);

        public static bool TryBuy(UpgradeType type)
        {
            if (!CanBuy(type)) return false;
            PlayerPrefs.SetInt(BiomassKey, Biomass - GetCost(type));
            PlayerPrefs.SetInt(UpgradePrefix + type, GetLevel(type) + 1);
            PlayerPrefs.Save();
            return true;
        }

        public static string DisplayName(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.Speed: return "Velocidad";
                case UpgradeType.Magnet: return "Radio de atracción";
                case UpgradeType.Resistance: return "Resistencia tóxica";
                default: return type.ToString();
            }
        }

        // ---------- Efecto de cada mejora (ajusta aquí el balance) ----------
        /// <summary>+10 % de velocidad por nivel.</summary>
        public static float SpeedMultiplier => 1f + 0.10f * GetLevel(UpgradeType.Speed);

        /// <summary>+0.75 unidades de radio de imán por nivel.</summary>
        public static float MagnetRadiusBonus => 0.75f * GetLevel(UpgradeType.Magnet);

        /// <summary>-8 % de drenaje de toxicidad por nivel.</summary>
        public static float DrainMultiplier => 1f - 0.08f * GetLevel(UpgradeType.Resistance);

        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(BiomassKey);
            PlayerPrefs.DeleteKey(BestTimeKey);
            foreach (UpgradeType t in System.Enum.GetValues(typeof(UpgradeType)))
                PlayerPrefs.DeleteKey(UpgradePrefix + t);
            PlayerPrefs.Save();
        }
    }
}
