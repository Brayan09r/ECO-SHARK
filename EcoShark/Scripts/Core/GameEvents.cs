namespace EcoShark
{
    /// <summary>
    /// "Tablón de anuncios" del juego (patrón Observer).
    /// Quien genera algo lo anuncia aquí; quien le interesa se suscribe.
    /// Así la basura no necesita conocer al HUD, ni el HUD a la basura.
    ///
    /// Regla: todo el que se suscribe (+=) debe desuscribirse (-=) en OnDisable/OnDestroy.
    /// </summary>
    public static class GameEvents
    {
        public static event System.Action<TrashData> TrashCollected;
        public static event System.Action<TrappedAnimal> AnimalRescued;
        public static event System.Action<TrappedAnimal> AnimalLost;
        public static event System.Action SharkDied;

        public static void RaiseTrashCollected(TrashData data) => TrashCollected?.Invoke(data);
        public static void RaiseAnimalRescued(TrappedAnimal animal) => AnimalRescued?.Invoke(animal);
        public static void RaiseAnimalLost(TrappedAnimal animal) => AnimalLost?.Invoke(animal);
        public static void RaiseSharkDied() => SharkDied?.Invoke();
    }
}
