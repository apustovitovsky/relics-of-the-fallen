using Mirror;
using UnityEngine;

namespace GAS.Mirror
{
    internal readonly struct GameplayCueParametersReplicationState :
        INetworkSerializable
    {
        public GameplayEffectContextReplicationState Context
        {
            get;
        }

        public float NormalizedMagnitude
        {
            get;
        }

        public float RawMagnitude
        {
            get;
        }

        public Vector3 Location
        {
            get;
        }

        public Vector3 Normal
        {
            get;
        }

        public int GameplayEffectLevel
        {
            get;
        }

        public int AbilityLevel
        {
            get;
        }

        public bool IsGameplayEffectActive
        {
            get;
        }

        /// <summary>
        /// Creates replicated gameplay cue parameters from their transport-safe values.
        /// </summary>
        public GameplayCueParametersReplicationState(
            GameplayEffectContextReplicationState context,
            float normalizedMagnitude,
            float rawMagnitude,
            Vector3 location,
            Vector3 normal,
            int gameplayEffectLevel,
            int abilityLevel,
            bool isGameplayEffectActive)
        {
            Context = context;
            NormalizedMagnitude = normalizedMagnitude;
            RawMagnitude = rawMagnitude;
            Location = location;
            Normal = normal;
            GameplayEffectLevel = gameplayEffectLevel;
            AbilityLevel = abilityLevel;
            IsGameplayEffectActive = isGameplayEffectActive;
        }

        /// <summary>
        /// Creates gameplay cue parameter replication state from network data.
        /// </summary>
        internal GameplayCueParametersReplicationState(
            NetworkReader reader)
            : this(
                reader.ReadGameplayEffectContextReplicationState(),
                reader.ReadFloat(),
                reader.ReadFloat(),
                reader.ReadVector3(),
                reader.ReadVector3(),
                reader.ReadInt(),
                reader.ReadInt(),
                reader.ReadBool())
        {
        }

        /// <summary>
        /// Reconstructs core gameplay cue parameters from replicated state.
        /// </summary>
        public GameplayCueParameters CreateGameplayCueParameters()
        {
            GameplayEffectContextHandle effectContext =
                new(
                    new GameplayEffectContext(
                        Context));

            return new GameplayCueParameters(
                effectContext)
            {
                NormalizedMagnitude =
                    NormalizedMagnitude,
                RawMagnitude =
                    RawMagnitude,
                Location =
                    Location,
                Normal =
                    Normal,
                GameplayEffectLevel =
                    GameplayEffectLevel,
                AbilityLevel =
                    AbilityLevel,
                IsGameplayEffectActive =
                    IsGameplayEffectActive
            };
        }

        /// <summary>
        /// Serializes this gameplay cue parameter state into network data.
        /// </summary>
        public void Serialize(
            NetworkWriter writer)
        {
            writer.WriteGameplayEffectContextReplicationState(
                Context);

            writer.WriteFloat(
                NormalizedMagnitude);

            writer.WriteFloat(
                RawMagnitude);

            writer.WriteVector3(
                Location);

            writer.WriteVector3(
                Normal);

            writer.WriteInt(
                GameplayEffectLevel);

            writer.WriteInt(
                AbilityLevel);

            writer.WriteBool(
                IsGameplayEffectActive);
        }
    }
}