using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

namespace GAS.Mirror
{
    /// <summary>
    /// Replicates observable ability-system state to every relevant client.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkAbilitySystemObserverComponent :
        NetworkBehaviour
    {

        [SerializeField]
        private AssetRegistry m_AssetRegistry;

        [SyncVar(
            hook = nameof(OnReplicatedAnimMontageChanged))]
        private GameplayAbilityRepAnimMontageReplicationState m_ReplicatedAnimMontage;

        internal readonly SyncDictionary<
            AssetId,
            GameplayAttributeReplicationState> m_Attributes = new();

        [FormerlySerializedAs("asc")]
        [SerializeField]
        private AbilitySystemComponent m_AbilitySystem;

        /// <summary>
        /// Validates the serialized dependencies required by observer replication.
        /// </summary>
        private void Awake()
        {
            if (m_AbilitySystem == null)
            {
                throw new InvalidOperationException(
                    "AbilitySystemComponent must be assigned.");
            }

            if (m_AssetRegistry == null)
            {
                throw new InvalidOperationException(
                    "AssetRegistry must be assigned.");
            }
        }

        /// <summary>
        /// Starts authoritative observable attribute replication on the server.
        /// </summary>
        public override void OnStartServer()
        {
            base.OnStartServer();

            m_AbilitySystem.attributes.ForEach(
                SynchronizeAttribute);

            m_AbilitySystem.OnAttributeChanged +=
                SynchronizeAttribute;
        }

        /// <summary>
        /// Stops authoritative observable attribute replication on the server.
        /// </summary>
        public override void OnStopServer()
        {
            m_AbilitySystem.OnAttributeChanged -=
                SynchronizeAttribute;

            base.OnStopServer();
        }

        /// <summary>
        /// Resolves and applies authoritative montage state on a simulated client proxy.
        /// </summary>
        private void OnReplicatedAnimMontageChanged(
            GameplayAbilityRepAnimMontageReplicationState _,
            GameplayAbilityRepAnimMontageReplicationState newValue)
        {
            if (isServer ||
                isOwned ||
                !newValue.IsValid)
            {
                return;
            }

            GameplayAbilityMontage montage =
                m_AssetRegistry.GetAsset<GameplayAbilityMontage>(
                    newValue.AnimationId);

            GameplayAbilityRepAnimMontage repAnimMontageInfo =
                new(
                    montage,
                    newValue.PlayInstanceId,
                    newValue.PlayRate,
                    newValue.Position,
                    newValue.BlendTime,
                    newValue.IsStopped,
                    newValue.PredictionKey);

            m_AbilitySystem.OnRepReplicatedAnimMontage(
                repAnimMontageInfo);
        }

        [ServerCallback]
        private void LateUpdate()
        {
            SynchronizeReplicatedAnimMontage();
        }

        /// <summary>
        /// Copies the authoritative core montage state into its Mirror transport state.
        /// </summary>
        private void SynchronizeReplicatedAnimMontage()
        {
            m_AbilitySystem.AnimMontageUpdateReplicatedData();

            GameplayAbilityRepAnimMontage repAnimMontageInfo =
                m_AbilitySystem.RepAnimMontageInfo;

            GameplayAbilityMontage montage =
                repAnimMontageInfo.Animation;

            if (montage == null)
            {
                return;
            }

            AssetId montageId =
                m_AssetRegistry.GetAssetId(
                    montage);

            m_ReplicatedAnimMontage =
                new GameplayAbilityRepAnimMontageReplicationState(
                    montageId,
                    repAnimMontageInfo.PlayInstanceId,
                    repAnimMontageInfo.PlayRate,
                    repAnimMontageInfo.Position,
                    repAnimMontageInfo.BlendTime,
                    repAnimMontageInfo.IsStopped,
                    repAnimMontageInfo.PredictionKey);
        }

        /// <summary>
        /// Starts observable attribute replication on a remote client copy.
        /// </summary>
        public override void OnStartClient()
        {
            base.OnStartClient();

            if (isServer)
            {
                return;
            }

            if (!isOwned)
            {
                m_AbilitySystem.ClearAllAbilities();
            }

            m_Attributes.OnChange +=
                SynchronizeAttribute;

            foreach (
                KeyValuePair<
                    AssetId,
                    GameplayAttributeReplicationState> attributeEntry
                in m_Attributes)
            {
                SynchronizeAttribute(
                    SyncDictionary<
                        AssetId,
                        GameplayAttributeReplicationState>
                        .Operation.OP_ADD,
                    attributeEntry.Key,
                    attributeEntry.Value);
            }
        }

        /// <summary>
        /// Stops observable attribute replication on a remote client copy.
        /// </summary>
        public override void OnStopClient()
        {
            m_Attributes.OnChange -=
                SynchronizeAttribute;

            base.OnStopClient();
        }

        /// <summary>
        /// Applies replicated gameplay attribute state according to the local network role.
        /// </summary>
        private void SynchronizeAttribute(
            SyncDictionary<
                AssetId,
                GameplayAttributeReplicationState>.Operation operation,
            AssetId attributeId,
            GameplayAttributeReplicationState replicationState)
        {
            if (
                !isClientOnly ||
                operation ==
                SyncDictionary<
                    AssetId,
                    GameplayAttributeReplicationState>
                    .Operation.OP_REMOVE ||
                operation ==
                SyncDictionary<
                    AssetId,
                    GameplayAttributeReplicationState>
                    .Operation.OP_CLEAR)
            {
                return;
            }

            AttributeName attributeName =
                m_AssetRegistry.GetAsset<AttributeName>(
                    attributeId);

            float replicatedValue =
                isOwned
                    ? replicationState.BaseValue
                    : replicationState.CurrentValue;

            m_AbilitySystem.SetBaseAttributeValueFromReplication(
                attributeName,
                replicatedValue);
        }

        /// <summary>
        /// Refreshes replicated state after an authoritative gameplay attribute changes.
        /// </summary>
        private void SynchronizeAttribute(
            AttributeName attributeName,
            float oldValue,
            float newValue,
            GameplayEffect gameplayEffect)
        {
            SynchronizeAttribute(
                m_AbilitySystem.GetAttribute(
                    attributeName));
        }

        /// <summary>
        /// Copies one authoritative gameplay attribute into observer replication state.
        /// </summary>
        private void SynchronizeAttribute(
            Attribute attribute)
        {
            AssetId attributeId =
                m_AssetRegistry.GetAssetId(
                    attribute.attributeName);

            m_Attributes[attributeId] =
                new GameplayAttributeReplicationState(
                    attribute.BaseValue,
                    attribute.CurrentValue);
        }
    }
}

