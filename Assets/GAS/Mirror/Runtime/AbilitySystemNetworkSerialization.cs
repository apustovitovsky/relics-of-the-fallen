using Mirror;

namespace GAS.Mirror
{
    internal static class AbilitySystemNetworkSerialization
    {
        public static void WriteAssetId(
            this NetworkWriter writer,
            AssetId value)
        {
            writer.WriteGuid(
                value.Value);
        }

        public static AssetId ReadAssetId(
            this NetworkReader reader)
        {
            return new AssetId(
                reader.ReadGuid());
        }

        public static void WriteGameplayAbilitySpecHandle(
            this NetworkWriter writer,
            GameplayAbilitySpecHandle value)
        {
            writer.WriteInt(
                value.Value);
        }

        public static GameplayAbilitySpecHandle ReadGameplayAbilitySpecHandle(
            this NetworkReader reader)
        {
            return new GameplayAbilitySpecHandle(
                reader.ReadInt());
        }

        /// <summary>
        /// Writes the replication state of one granted gameplay ability specification.
        /// </summary>
        public static void WriteGameplayAbilitySpecReplicationState(
            this NetworkWriter writer,
            GameplayAbilitySpecReplicationState value)
        {
            value.Serialize(
                writer);
        }

        /// <summary>
        /// Reads the replication state of one granted gameplay ability specification.
        /// </summary>
        public static GameplayAbilitySpecReplicationState
            ReadGameplayAbilitySpecReplicationState(
                this NetworkReader reader)
        {
            return new GameplayAbilitySpecReplicationState(
                reader);
        }

        /// <summary>
        /// Writes an owner-scoped gameplay prediction key.
        /// </summary>
        public static void WritePredictionKey(
            this NetworkWriter writer,
            PredictionKey value)
        {
            writer.WriteUInt(
                value.Sequence);
        }

        /// <summary>
        /// Reads an owner-scoped gameplay prediction key.
        /// </summary>
        public static PredictionKey ReadPredictionKey(
            this NetworkReader reader)
        {
            return new PredictionKey(
                reader.ReadUInt());
        }

        /// <summary>
        /// Writes the replication state of one gameplay ability animation montage.
        /// </summary>
        public static void WriteGameplayAbilityRepAnimMontageReplicationState(
            this NetworkWriter writer,
            GameplayAbilityRepAnimMontageReplicationState value)
        {
            value.Serialize(
                writer);
        }

        /// <summary>
        /// Reads the replication state of one gameplay ability animation montage.
        /// </summary>
        public static GameplayAbilityRepAnimMontageReplicationState
            ReadGameplayAbilityRepAnimMontageReplicationState(
                this NetworkReader reader)
        {
            return new GameplayAbilityRepAnimMontageReplicationState(
                reader);
        }

        /// <summary>
        /// Writes the replication state of one authoritative active gameplay effect.
        /// </summary>
        public static void WriteActiveGameplayEffectReplicationState(
            this NetworkWriter writer,
            ActiveGameplayEffectReplicationState value)
        {
            value.Serialize(
                writer);
        }

        /// <summary>
        /// Reads the replication state of one authoritative active gameplay effect.
        /// </summary>
        public static ActiveGameplayEffectReplicationState
            ReadActiveGameplayEffectReplicationState(
                this NetworkReader reader)
        {
            return new ActiveGameplayEffectReplicationState(
                reader);
        }
    }
}