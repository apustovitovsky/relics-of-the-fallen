namespace GAS
{
    public readonly struct GameplayEffectApplicationOptions
    {
        public bool IgnoreTagRequirements { get; }

        public bool IgnoreChanceRoll { get; }

        public GameplayEffectNotificationOptions NotificationOptions { get; }

        private GameplayEffectApplicationOptions(
            bool ignoreTagRequirements,
            bool ignoreChanceRoll,
            GameplayEffectNotificationOptions notificationOptions)
        {
            IgnoreTagRequirements = ignoreTagRequirements;
            IgnoreChanceRoll = ignoreChanceRoll;
            NotificationOptions = notificationOptions;
        }

        public static GameplayEffectApplicationOptions Normal =>
            new(
                ignoreTagRequirements: false,
                ignoreChanceRoll: false,
                notificationOptions:
                    GameplayEffectNotificationOptions.All);

        public static GameplayEffectApplicationOptions Silent =>
            new(
                ignoreTagRequirements: true,
                ignoreChanceRoll: true,
                notificationOptions:
                    GameplayEffectNotificationOptions.None);
    }
}