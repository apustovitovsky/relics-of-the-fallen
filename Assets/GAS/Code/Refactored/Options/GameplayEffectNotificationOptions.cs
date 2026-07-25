namespace GAS
{
    public readonly struct GameplayEffectNotificationOptions
    {
        public bool NotifyEffectEvents { get; }

        public bool NotifyCueEvents { get; }

        public bool NotifyTagEvents { get; }

        private GameplayEffectNotificationOptions(
            bool notifyEffectEvents,
            bool notifyCueEvents,
            bool notifyTagEvents)
        {
            NotifyEffectEvents = notifyEffectEvents;
            NotifyCueEvents = notifyCueEvents;
            NotifyTagEvents = notifyTagEvents;
        }

        public static GameplayEffectNotificationOptions All =>
            new(
                notifyEffectEvents: true,
                notifyCueEvents: true,
                notifyTagEvents: true);

        public static GameplayEffectNotificationOptions None =>
            new(
                notifyEffectEvents: false,
                notifyCueEvents: false,
                notifyTagEvents: false);

        public static GameplayEffectNotificationOptions CuesOnly =>
            new(
                notifyEffectEvents: false,
                notifyCueEvents: true,
                notifyTagEvents: false);
    }
}