using System;

namespace GAS
{
    public enum GameplayEffectSlotType : byte
    {
        None = 0,
        Cost = 1,
        Cooldown = 2,
        Effect = 3
    }

    /// <summary>
    /// Identifies the stable position of a gameplay effect within an ability.
    /// </summary>
    public readonly struct GameplayEffectSlot :
        IEquatable<GameplayEffectSlot>
    {
        public GameplayEffectSlotType Type { get; }

        public bool IsValid =>
            Type != GameplayEffectSlotType.None;

        public int Index { get; }

        private GameplayEffectSlot(
            GameplayEffectSlotType type,
            int index)
        {
            Type =
                type;

            Index =
                index;
        }

        public static GameplayEffectSlot Cost =>
            new(
                GameplayEffectSlotType.Cost,
                index: 0);

        public static GameplayEffectSlot Cooldown =>
            new(
                GameplayEffectSlotType.Cooldown,
                index: 0);

        public static GameplayEffectSlot Effect(
            int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index));
            }

            return new GameplayEffectSlot(
                GameplayEffectSlotType.Effect,
                index);
        }

        public bool Equals(
            GameplayEffectSlot other)
        {
            return
                Type == other.Type &&
                Index == other.Index;
        }

        public override bool Equals(
            object obj)
        {
            return
                obj is GameplayEffectSlot other &&
                Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Type,
                Index);
        }

        public override string ToString()
        {
            return Type switch
            {
                GameplayEffectSlotType.Cost =>
                    nameof(GameplayEffectSlotType.Cost),

                GameplayEffectSlotType.Cooldown =>
                    nameof(GameplayEffectSlotType.Cooldown),

                GameplayEffectSlotType.Effect =>
                    $"Effect:{Index}",

                _ =>
                    nameof(GameplayEffectSlotType.None)
            };
        }
    }
}