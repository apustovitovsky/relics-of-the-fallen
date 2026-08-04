using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace GAS.Mirror
{
    internal static class AbilitySystemNetworkSerializationExtensions
    {
        private const byte k_ActorArrayTargetDataTypeId = 1;
        private const int k_MaxTargetDataCount = 256;
        private const int k_MaxTargetActorCount = 256;

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

        /// <summary>
        /// Writes the replicated base and current values of one gameplay attribute.
        /// </summary>
        public static void WriteGameplayAttributeReplicationState(
            this NetworkWriter writer,
            GameplayAttributeReplicationState value)
        {
            value.Serialize(
                writer);
        }

        /// <summary>
        /// Reads the replicated base and current values of one gameplay attribute.
        /// </summary>
        public static GameplayAttributeReplicationState
            ReadGameplayAttributeReplicationState(
                this NetworkReader reader)
        {
            return new GameplayAttributeReplicationState(
                reader);
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
        /// Writes the replicated actor-reference state of one gameplay effect context.
        /// </summary>
        public static void WriteGameplayEffectContextReplicationState(
            this NetworkWriter writer,
            GameplayEffectContextReplicationState value)
        {
            value.Serialize(
                writer);
        }

        /// <summary>
        /// Reads the replicated actor-reference state of one gameplay effect context.
        /// </summary>
        public static GameplayEffectContextReplicationState
            ReadGameplayEffectContextReplicationState(
                this NetworkReader reader)
        {
            return new GameplayEffectContextReplicationState(
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

        /// <summary>
        /// Writes a polymorphic gameplay ability target-data handle.
        /// </summary>
        public static void WriteGameplayAbilityTargetDataHandle(
            this NetworkWriter writer,
            GameplayAbilityTargetDataHandle value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(
                    nameof(value));
            }

            int targetDataCount = value.Num();

            if (targetDataCount > k_MaxTargetDataCount)
            {
                throw new InvalidOperationException(
                    $"Target data count exceeds {k_MaxTargetDataCount}.");
            }

            writer.WriteUShort(
                (ushort)targetDataCount);

            for (
                int index = 0;
                index < targetDataCount;
                index++)
            {
                WriteGameplayAbilityTargetData(
                    writer,
                    value.Get(
                        index));
            }
        }

        /// <summary>
        /// Reads a polymorphic gameplay ability target-data handle.
        /// </summary>
        public static GameplayAbilityTargetDataHandle
            ReadGameplayAbilityTargetDataHandle(
                this NetworkReader reader)
        {
            int targetDataCount = reader.ReadUShort();

            if (targetDataCount > k_MaxTargetDataCount)
            {
                throw new InvalidOperationException(
                    $"Target data count exceeds {k_MaxTargetDataCount}.");
            }

            GameplayAbilityTargetDataHandle targetDataHandle =
                new();

            for (
                int index = 0;
                index < targetDataCount;
                index++)
            {
                targetDataHandle.Add(
                    ReadGameplayAbilityTargetData(
                        reader));
            }

            return targetDataHandle;
        }

        /// <summary>
        /// Writes one polymorphic gameplay ability target-data payload.
        /// </summary>
        private static void WriteGameplayAbilityTargetData(
            NetworkWriter writer,
            GameplayAbilityTargetData targetData)
        {
            if (
                targetData is
                    GameplayAbilityTargetData_ActorArray actorArray)
            {
                writer.WriteByte(
                    k_ActorArrayTargetDataTypeId);

                WriteGameplayAbilityTargetDataActorArray(
                    writer,
                    actorArray);

                return;
            }

            throw new InvalidOperationException(
                $"Unsupported target data type: {targetData?.GetType().Name}.");
        }

        /// <summary>
        /// Reads one polymorphic gameplay ability target-data payload.
        /// </summary>
        private static GameplayAbilityTargetData
            ReadGameplayAbilityTargetData(
                NetworkReader reader)
        {
            byte targetDataTypeId = reader.ReadByte();

            if (targetDataTypeId == k_ActorArrayTargetDataTypeId)
            {
                return ReadGameplayAbilityTargetDataActorArray(
                    reader);
            }

            throw new InvalidOperationException(
                $"Unsupported target data type ID: {targetDataTypeId}.");
        }

        /// <summary>
        /// Writes the network actors stored by an actor-array target payload.
        /// </summary>
        private static void WriteGameplayAbilityTargetDataActorArray(
            NetworkWriter writer,
            GameplayAbilityTargetData_ActorArray targetData)
        {
            IReadOnlyList<GameObject> targetActors =
                targetData.TargetActorArray;

            if (targetActors.Count > k_MaxTargetActorCount)
            {
                throw new InvalidOperationException(
                    $"Target actor count exceeds {k_MaxTargetActorCount}.");
            }

            writer.WriteUShort(
                (ushort)targetActors.Count);

            for (
                int index = 0;
                index < targetActors.Count;
                index++)
            {
                writer.WriteGameObject(
                    targetActors[index]);
            }
        }

        /// <summary>
        /// Reads the network actors stored by an actor-array target payload.
        /// </summary>
        private static GameplayAbilityTargetData_ActorArray
            ReadGameplayAbilityTargetDataActorArray(
                NetworkReader reader)
        {
            int targetActorCount = reader.ReadUShort();

            if (targetActorCount > k_MaxTargetActorCount)
            {
                throw new InvalidOperationException(
                    $"Target actor count exceeds {k_MaxTargetActorCount}.");
            }

            GameplayAbilityTargetData_ActorArray targetData =
                new();

            for (
                int index = 0;
                index < targetActorCount;
                index++)
            {
                GameObject targetActor = reader.ReadGameObject();

                if (targetActor == null)
                {
                    throw new InvalidOperationException(
                        "Target actor is not spawned on the receiving peer.");
                }

                targetData.AddActor(
                    targetActor);
            }

            return targetData;
        }
    }
}