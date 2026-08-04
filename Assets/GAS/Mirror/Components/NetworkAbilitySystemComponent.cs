using System.Collections.Generic;
using System;
using Cysharp.Threading.Tasks;
using Mirror;
using UnityEngine;

namespace GAS.Mirror
{
    /// <summary>
    /// Replicates private ability-system state exclusively to the owning client.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkAbilitySystemComponent :
        NetworkBehaviour,
        IAbilitySystemReplicationTransport
    {
        [SerializeField]
        private AbilitySystemComponent m_AbilitySystem;

        [SerializeField]
        private AssetRegistry m_AssetRegistry;

        private const int k_PredictionKeyRingBufferSize = 32;

        private bool m_AreOwnerCallbacksRegistered;

        private readonly Dictionary<
            ulong,
            ActiveGameplayEffect>
            m_ReplicatedActiveGameplayEffectItems = new();

        internal readonly SyncDictionary<
            GameplayAbilitySpecHandle,
            GameplayAbilitySpecReplicationState> m_AbilitySpecs = new();

        internal readonly SyncDictionary<
            ulong,
            ActiveGameplayEffectReplicationState> m_ActiveGameplayEffects = new();

        internal readonly SyncList<
            PredictionKey> m_ReplicatedPredictionKeyMap = new();

        /// <summary>
        /// Initializes ability actor information and installs the Mirror target-data transport.
        /// </summary>
        private void Awake()
        {
            m_AbilitySystem.InitAbilityActorInfo(
                gameObject,
                gameObject);

            m_AbilitySystem.ReplicationTransport =
                this;
        }

        /// <summary>
        /// Starts replicated active-effect lifecycle handling for the owning client.
        /// </summary>
        public override void OnStartAuthority()
        {
            base.OnStartAuthority();
            m_AbilitySystem.AbilityActorInfo.SetLocallyControlled(
                true);

            if (!isClientOnly)
            {
                return;
            }

            m_ActiveGameplayEffects.OnAdd +=
                AddReplicatedActiveGameplayEffect;

            m_ActiveGameplayEffects.OnSet +=
                ChangeReplicatedActiveGameplayEffect;

            m_ActiveGameplayEffects.OnRemove +=
                RemoveReplicatedActiveGameplayEffect;

            m_ActiveGameplayEffects.OnClear +=
                ClearReplicatedActiveGameplayEffects;

            foreach (
                ulong replicationId
                in m_ActiveGameplayEffects.Keys)
            {
                AddReplicatedActiveGameplayEffect(
                    replicationId);
            }
        }

        /// <summary>
        /// Stops replicated active-effect lifecycle handling for the owning client.
        /// </summary>
        public override void OnStopAuthority()
        {
            m_AbilitySystem.AbilityActorInfo.SetLocallyControlled(
                false);

            m_ActiveGameplayEffects.OnAdd -=
                AddReplicatedActiveGameplayEffect;

            m_ActiveGameplayEffects.OnSet -=
                ChangeReplicatedActiveGameplayEffect;

            m_ActiveGameplayEffects.OnRemove -=
                RemoveReplicatedActiveGameplayEffect;

            m_ActiveGameplayEffects.OnClear -=
                ClearReplicatedActiveGameplayEffects;

            ClearReplicatedActiveGameplayEffects();

            base.OnStopAuthority();
        }

        /// <summary>
        /// Initializes owner-only ability, active-effect, and prediction state replication on the server.
        /// </summary>
        public override void OnStartServer()
        {
            base.OnStartServer();

            m_AbilitySystem.AbilityActorInfo.SetLocallyControlled(
                false);

            GameplayAbilitySpecContainer abilitySpecs =
                m_AbilitySystem.ActivatableAbilities;

            abilitySpecs.AbilitySpecAdded +=
                SynchronizeAbilitySpec;

            abilitySpecs.AbilitySpecChanged +=
                SynchronizeAbilitySpec;

            abilitySpecs.AbilitySpecRemoved +=
                RemoveAbilitySpec;

            for (
                int index = 0;
                index < abilitySpecs.Count;
                index++)
            {
                SynchronizeAbilitySpec(
                    abilitySpecs.Items[index]);
            }

            ActiveGameplayEffectsContainer activeGameplayEffects =
                m_AbilitySystem.ActiveGameplayEffects;

            activeGameplayEffects.AuthoritativeGameplayEffectAdded +=
                SynchronizeActiveGameplayEffect;

            activeGameplayEffects.AuthoritativeGameplayEffectChanged +=
                SynchronizeActiveGameplayEffect;

            activeGameplayEffects.AuthoritativeGameplayEffectRemoved +=
                RemoveActiveGameplayEffect;

            foreach (
                ActiveGameplayEffect activeEffect
                in activeGameplayEffects.AuthoritativeGameplayEffects)
            {
                SynchronizeActiveGameplayEffect(
                    activeEffect);
            }

            InitializeReplicatedPredictionKeyMap();
        }

        /// <summary>
        /// Stops owner-only ability, active-effect, and prediction state replication on the server.
        /// </summary>
        public override void OnStopServer()
        {
            GameplayAbilitySpecContainer abilitySpecs =
                m_AbilitySystem.ActivatableAbilities;

            abilitySpecs.AbilitySpecAdded -=
                SynchronizeAbilitySpec;

            abilitySpecs.AbilitySpecChanged -=
                SynchronizeAbilitySpec;

            abilitySpecs.AbilitySpecRemoved -=
                RemoveAbilitySpec;

            ActiveGameplayEffectsContainer activeGameplayEffects =
                m_AbilitySystem.ActiveGameplayEffects;

            activeGameplayEffects.AuthoritativeGameplayEffectAdded -=
                SynchronizeActiveGameplayEffect;

            activeGameplayEffects.AuthoritativeGameplayEffectChanged -=
                SynchronizeActiveGameplayEffect;

            activeGameplayEffects.AuthoritativeGameplayEffectRemoved -=
                RemoveActiveGameplayEffect;

            base.OnStopServer();
        }

        /// <summary>
        /// Initializes owner-only ability state and prediction catch-up on the owning client.
        /// </summary>
        public override void OnStartClient()
        {
            base.OnStartClient();

            m_AbilitySystem.AbilityActorInfo.SetLocallyControlled(
                isOwned);

            if (isClientOnly)
            {
                m_AbilitySystem.AbilityActorInfo.SetNetAuthority(
                    false);
            }

            if (!isOwned)
            {
                return;
            }

            m_ReplicatedPredictionKeyMap.OnSet +=
                CatchUpReplicatedPredictionKey;

            m_AreOwnerCallbacksRegistered =
                true;

            for (
                int index = 0;
                index < m_ReplicatedPredictionKeyMap.Count;
                index++)
            {
                CatchUpReplicatedPredictionKey(
                    index,
                    default);
            }

            if (!isClientOnly)
            {
                return;
            }

            m_AbilitySystem.ClearAllAbilities();

            m_AbilitySpecs.OnAdd +=
                AddReplicatedAbilitySpec;

            m_AbilitySpecs.OnSet +=
                UpdateReplicatedAbilitySpec;

            m_AbilitySpecs.OnRemove +=
                RemoveReplicatedAbilitySpec;

            m_AbilitySpecs.OnClear +=
                ClearReplicatedAbilitySpecs;

            foreach (
                GameplayAbilitySpecHandle handle
                in m_AbilitySpecs.Keys)
            {
                AddReplicatedAbilitySpec(
                    handle);
            }
        }

        /// <summary>
        /// Releases owner-only ability and prediction callbacks on the owning client.
        /// </summary>
        public override void OnStopClient()
        {
            if (m_AreOwnerCallbacksRegistered)
            {
                m_ReplicatedPredictionKeyMap.OnSet -=
                    CatchUpReplicatedPredictionKey;

                m_AbilitySpecs.OnAdd -=
                    AddReplicatedAbilitySpec;

                m_AbilitySpecs.OnSet -=
                    UpdateReplicatedAbilitySpec;

                m_AbilitySpecs.OnRemove -=
                    RemoveReplicatedAbilitySpec;

                m_AbilitySpecs.OnClear -=
                    ClearReplicatedAbilitySpecs;

                m_AreOwnerCallbacksRegistered =
                    false;
            }

            base.OnStopClient();
        }

        /// <summary>
        /// Reconstructs one newly replicated ability specification on the owning client.
        /// </summary>
        private void AddReplicatedAbilitySpec(
            GameplayAbilitySpecHandle handle)
        {
            GameplayAbilitySpecReplicationState state =
                m_AbilitySpecs[handle];

            GameplayAbilitySO ability =
                m_AssetRegistry.GetAsset<GameplayAbilitySO>(
                    state.AbilityId);

            m_AbilitySystem.GiveAbility(
                new GameplayAbilitySpec(
                    handle,
                    ability,
                    state.Level));
        }

        /// <summary>
        /// Applies a replicated ability specification update on the owning client.
        /// </summary>
        private void UpdateReplicatedAbilitySpec(
            GameplayAbilitySpecHandle handle,
            GameplayAbilitySpecReplicationState _)
        {
            m_AbilitySystem.SetGameplayAbilitySpecLevel(
                handle,
                m_AbilitySpecs[handle].Level);
        }

        /// <summary>
        /// Removes one replicated ability specification from the owning client.
        /// </summary>
        private void RemoveReplicatedAbilitySpec(
            GameplayAbilitySpecHandle handle,
            GameplayAbilitySpecReplicationState _)
        {
            m_AbilitySystem.ClearAbility(
                handle);
        }

        /// <summary>
        /// Removes every replicated ability specification from the owning client.
        /// </summary>
        private void ClearReplicatedAbilitySpecs()
        {
            m_AbilitySystem.ClearAllAbilities();
        }

        /// <summary>
        /// Adds or updates one authoritative ability specification in the replicated owner state.
        /// </summary>
        private void SynchronizeAbilitySpec(
            GameplayAbilitySpec abilitySpec)
        {
            AssetId abilityId =
                m_AssetRegistry.GetAssetId(
                    abilitySpec.Ability);

            m_AbilitySpecs[abilitySpec.Handle] =
                new GameplayAbilitySpecReplicationState(
                    abilityId,
                    abilitySpec.Level);
        }

        /// <summary>
        /// Removes one authoritative ability specification from the replicated owner state.
        /// </summary>
        private void RemoveAbilitySpec(
            GameplayAbilitySpec abilitySpec)
        {
            m_AbilitySpecs.Remove(
                abilitySpec.Handle);
        }

        /// <summary>
        /// Reconstructs and installs one newly replicated active gameplay effect.
        /// </summary>
        private void AddReplicatedActiveGameplayEffect(
            ulong replicationId)
        {
            if (
                m_ReplicatedActiveGameplayEffectItems.ContainsKey(
                    replicationId))
            {
                throw new InvalidOperationException(
                    $"Active gameplay effect '{replicationId}' is already replicated.");
            }

            ActiveGameplayEffect activeEffect =
                CreateReplicatedActiveGameplayEffect(
                    replicationId,
                    m_ActiveGameplayEffects[replicationId]);

            activeEffect.PostReplicatedAdd(
                m_AbilitySystem.ActiveGameplayEffects);

            m_ReplicatedActiveGameplayEffectItems.Add(
                replicationId,
                activeEffect);
        }

        /// <summary>
        /// Applies changed transport state to an existing replicated active effect.
        /// </summary>
        private void ChangeReplicatedActiveGameplayEffect(
            ulong replicationId,
            ActiveGameplayEffectReplicationState oldState)
        {
            if (
                !m_ReplicatedActiveGameplayEffectItems.TryGetValue(
                    replicationId,
                    out ActiveGameplayEffect activeEffect))
            {
                throw new InvalidOperationException(
                    $"Active gameplay effect '{replicationId}' is not locally registered.");
            }

            ActiveGameplayEffectReplicationState currentState =
                m_ActiveGameplayEffects[replicationId];

            GameplayEffectContextReplicationState oldContext =
                oldState.Context;

            GameplayEffectContextReplicationState currentContext =
                currentState.Context;

            if (
                oldContext.InstigatorNetworkId !=
                currentContext.InstigatorNetworkId ||
                oldContext.EffectCauserNetworkId !=
                currentContext.EffectCauserNetworkId)
            {
                throw new InvalidOperationException(
                    $"Active gameplay effect '{replicationId}' cannot change " +
                    "its context actor identities.");
            }

            ActiveGameplayEffect replicatedEffect =
                CreateReplicatedActiveGameplayEffect(
                    replicationId,
                    currentState);

            activeEffect.CopyReplicatedStateFrom(
                replicatedEffect);

            activeEffect.PostReplicatedChange(
                m_AbilitySystem.ActiveGameplayEffects);
        }

        /// <summary>
        /// Removes one active effect after its transport entry is removed.
        /// </summary>
        private void RemoveReplicatedActiveGameplayEffect(
            ulong replicationId,
            ActiveGameplayEffectReplicationState _)
        {
            if (
                !m_ReplicatedActiveGameplayEffectItems.TryGetValue(
                    replicationId,
                    out ActiveGameplayEffect activeEffect))
            {
                throw new InvalidOperationException(
                    $"Active gameplay effect '{replicationId}' is not locally registered.");
            }

            activeEffect.PreReplicatedRemove(
                m_AbilitySystem.ActiveGameplayEffects);

            m_ReplicatedActiveGameplayEffectItems.Remove(
                replicationId);
        }

        /// <summary>
        /// Removes every locally reconstructed replicated active gameplay effect.
        /// </summary>
        private void ClearReplicatedActiveGameplayEffects()
        {
            foreach (
                ActiveGameplayEffect activeEffect
                in m_ReplicatedActiveGameplayEffectItems.Values)
            {
                activeEffect.PreReplicatedRemove(
                    m_AbilitySystem.ActiveGameplayEffects);
            }

            m_ReplicatedActiveGameplayEffectItems.Clear();
        }

        /// <summary>
        /// Reconstructs one authoritative active effect from its replicated transport state.
        /// </summary>
        private ActiveGameplayEffect CreateReplicatedActiveGameplayEffect(
            ulong replicationId,
            ActiveGameplayEffectReplicationState state)
        {
            if (!state.IsValid)
            {
                throw new ArgumentException(
                    "Active gameplay effect replication state must be valid.",
                    nameof(state));
            }

            GameplayEffectContextHandle effectContext =
                CreateReplicatedGameplayEffectContext(
                    state.Context);

            GameplayEffectSO definitionAsset =
                m_AssetRegistry.GetAsset<GameplayEffectSO>(
                    state.DefinitionId);

            GameplayEffectSpec spec =
                new(
                    definitionAsset,
                    effectContext,
                    state.Level,
                    state.Duration,
                    state.EvaluatedModifierMagnitudes);

            double currentWorldTime = Time.timeAsDouble;
            double currentServerWorldTime = NetworkTime.time;

            double startWorldTime =
                currentWorldTime -
                (currentServerWorldTime -
                state.StartServerWorldTime);

            return new ActiveGameplayEffect(
                replicationId,
                spec,
                state.PredictionKey,
                startWorldTime,
                state.StartServerWorldTime);
        }

        /// <summary>
        /// Creates a gameplay effect context backed by replicated object references.
        /// </summary>
        private static GameplayEffectContextHandle
            CreateReplicatedGameplayEffectContext(
                GameplayEffectContextReplicationState state)
        {
            return new GameplayEffectContextHandle(
                new GameplayEffectContext(
                    state));
        }

        /// <summary>
        /// Creates network state from one authoritative active gameplay effect.
        /// </summary>
        private bool TryCreateActiveGameplayEffectReplicationState(
            ActiveGameplayEffect activeEffect,
            out ActiveGameplayEffectReplicationState state)
        {
            GameplayEffectSpec spec = activeEffect.Spec;
            GameplayEffectSO definitionAsset = spec.DefinitionAsset;
            GameplayEffectContextHandle effectContext = spec.EffectContext;

            if (definitionAsset == null)
            {
                state = default;
                return false;
            }

            if (
                !TryGetReplicatedActorNetworkId(
                    effectContext.GetInstigator(),
                    out uint instigatorNetworkId) ||
                instigatorNetworkId == 0 ||
                !TryGetReplicatedActorNetworkId(
                    effectContext.GetEffectCauser(),
                    out uint effectCauserNetworkId))
            {
                state = default;
                return false;
            }

            GameplayEffectContextReplicationState contextState =
                new(
                    instigatorNetworkId,
                    effectCauserNetworkId);

            int modifierCount = spec.ModifierSpecs.Count;
            float[] evaluatedModifierMagnitudes = new float[modifierCount];

            for (
                int index = 0;
                index < modifierCount;
                index++)
            {
                evaluatedModifierMagnitudes[index] =
                    spec.ModifierSpecs[index].EvaluatedMagnitude;
            }

            AssetId definitionId = m_AssetRegistry.GetAssetId(
                definitionAsset);

            state =
                new ActiveGameplayEffectReplicationState(
                    definitionId,
                    contextState,
                    spec.Level,
                    spec.Duration,
                    activeEffect.StartServerWorldTime,
                    activeEffect.PredictionKey,
                    evaluatedModifierMagnitudes);

            return true;
        }

        /// <summary>
        /// Resolves the spawned network identity represented by one optional gameplay actor.
        /// </summary>
        private static bool TryGetReplicatedActorNetworkId(
            GameObject actor,
            out uint networkId)
        {
            networkId = 0;

            if (actor == null)
            {
                return true;
            }

            if (
                !actor.TryGetComponent(
                    out NetworkIdentity identity) ||
                identity.netId == 0)
            {
                return false;
            }

            networkId = identity.netId;
            return true;
        }

        /// <summary>
        /// Adds or updates one authoritative active effect in the replicated owner state.
        /// </summary>
        private void SynchronizeActiveGameplayEffect(
            ActiveGameplayEffect activeEffect)
        {
            if (
                !TryCreateActiveGameplayEffectReplicationState(
                    activeEffect,
                    out ActiveGameplayEffectReplicationState state))
            {
                return;
            }

            m_ActiveGameplayEffects[activeEffect.ReplicationId] =
                state;
        }

        /// <summary>
        /// Removes one authoritative active effect from the replicated owner state.
        /// </summary>
        private void RemoveActiveGameplayEffect(
            ActiveGameplayEffect activeEffect)
        {
            m_ActiveGameplayEffects.Remove(
                activeEffect.ReplicationId);
        }

        /// <summary>
        /// Initializes the fixed-size replicated prediction key ring buffer.
        /// </summary>
        private void InitializeReplicatedPredictionKeyMap()
        {
            m_ReplicatedPredictionKeyMap.Clear();

            for (
                int index = 0;
                index < k_PredictionKeyRingBufferSize;
                index++)
            {
                m_ReplicatedPredictionKeyMap.Add(
                    default);
            }
        }

        /// <summary>
        /// Records one successfully processed prediction key in the replicated ring buffer.
        /// </summary>
        private void ReplicatePredictionKey(
            PredictionKey predictionKey)
        {
            if (!predictionKey.IsValid)
            {
                throw new ArgumentException(
                    "A replicated prediction key must be valid.",
                    nameof(predictionKey));
            }

            if (m_ReplicatedPredictionKeyMap.Count !=
                k_PredictionKeyRingBufferSize)
            {
                throw new InvalidOperationException(
                    "The replicated prediction key map is not initialized.");
            }

            int slotIndex =
                (int)((predictionKey.Sequence - 1u) %
                k_PredictionKeyRingBufferSize);

            if (m_ReplicatedPredictionKeyMap[slotIndex] ==
                predictionKey)
            {
                m_ReplicatedPredictionKeyMap[slotIndex] =
                    default;
            }

            m_ReplicatedPredictionKeyMap[slotIndex] =
                predictionKey;
        }

        /// <summary>
        /// Resolves predicted side effects after authoritative state reaches the owning client.
        /// </summary>
        private void CatchUpReplicatedPredictionKey(
            int index,
            PredictionKey _)
        {
            PredictionKey predictionKey =
                m_ReplicatedPredictionKeyMap[index];

            if (!predictionKey.IsValid)
            {
                return;
            }

            m_AbilitySystem
                .PredictionKeyDelegates
                .CatchUpTo(
                    predictionKey);
        }

        /// <summary>
        /// Starts a predicted ability activation for the owning client.
        /// </summary>
        public void TryActivateAbility(
            GameplayAbilitySO ability)
        {
            InternalTryActivateAbility(
                ability).Forget();
        }

        /// <summary>
        /// Performs local prediction and forwards a successful activation request to the server.
        /// </summary>
        private async UniTask InternalTryActivateAbility(
            GameplayAbilitySO ability)
        {
            if (!isOwned)
            {
                Debug.LogWarning(
                    "Only the owning client can request an ability activation.",
                    this);

                return;
            }

            GameplayAbilitySpec abilitySpec = m_AbilitySystem.FindAbilitySpecFromClass(
                ability);

            if (abilitySpec == null)
            {
                Debug.LogWarning(
                    $"Cannot activate ungranted ability '{ability}'.",
                    this);

                return;
            }

            PredictionKey predictionKey = m_AbilitySystem.CreateNewPredictionKey();
            string activationGUID = System.Guid.NewGuid().ToString();

            GameplayAbilityActivationInfo activationInfo =
                new(
                    GameplayAbilityActivationMode.Predicting,
                    predictionKey);

            m_AbilitySystem.SetGameplayAbilityActivationInfo(
                abilitySpec.Handle,
                activationInfo);

            bool wasActivated = await m_AbilitySystem.TryActivateAbility(
                abilitySpec.Handle,
                activationGUID);

            if (!wasActivated)
            {
                m_AbilitySystem.RejectAbilityActivation(
                    abilitySpec.Handle,
                    predictionKey);

                return;
            }

            CallServerTryActivateAbility(
                abilitySpec.Handle,
                predictionKey,
                activationGUID);
        }

        /// <summary>
        /// Sends an owning client's predicted ability activation request to the server.
        /// </summary>
        private void CallServerTryActivateAbility(
            GameplayAbilitySpecHandle handle,
            PredictionKey predictionKey,
            string activationGUID)
        {
            ServerTryActivateAbility(
                handle,
                predictionKey,
                activationGUID);
        }

        /// <summary>
        /// Sends client-produced target data to authoritative ability execution.
        /// </summary>
        public void CallServerSetReplicatedTargetData(
            GameplayAbilitySpecHandle abilityHandle,
            PredictionKey abilityOriginalPredictionKey,
            GameplayAbilityTargetDataHandle replicatedTargetDataHandle,
            GameplayTag applicationTag,
            PredictionKey currentPredictionKey)
        {
            Guid applicationTagId = Guid.Empty;

            if (applicationTag != null)
            {
                applicationTagId =
                    m_AssetRegistry
                        .GetAssetId(
                            applicationTag)
                        .Value;
            }

            ServerSetReplicatedTargetData(
                abilityHandle,
                abilityOriginalPredictionKey,
                replicatedTargetDataHandle,
                applicationTagId,
                currentPredictionKey);
        }

        /// <summary>
        /// Receives client-produced target data for one predicted ability activation.
        /// </summary>
        [Command]
        private void ServerSetReplicatedTargetData(
            GameplayAbilitySpecHandle abilityHandle,
            PredictionKey abilityOriginalPredictionKey,
            GameplayAbilityTargetDataHandle replicatedTargetDataHandle,
            Guid applicationTagId,
            PredictionKey currentPredictionKey)
        {
            if (
                !CanAcceptReplicatedTargetData(
                    abilityHandle,
                    abilityOriginalPredictionKey))
            {
                return;
            }

            GameplayTag applicationTag =
                null;

            if (applicationTagId != Guid.Empty)
            {
                applicationTag =
                    m_AssetRegistry.GetAsset<GameplayTag>(
                        new AssetId(
                            applicationTagId));
            }

            m_AbilitySystem.SetReplicatedTargetData(
                abilityHandle,
                abilityOriginalPredictionKey,
                replicatedTargetDataHandle,
                applicationTag,
                currentPredictionKey);
        }

        /// <summary>
        /// Receives target-data cancellation for one predicted ability activation.
        /// </summary>
        [Command]
        public void ServerSetReplicatedTargetDataCancelled(
            GameplayAbilitySpecHandle abilityHandle,
            PredictionKey abilityOriginalPredictionKey,
            PredictionKey currentPredictionKey)
        {
            if (
                !CanAcceptReplicatedTargetData(
                    abilityHandle,
                    abilityOriginalPredictionKey))
            {
                return;
            }

            m_AbilitySystem.SetReplicatedTargetDataCancelled(
                abilityHandle,
                abilityOriginalPredictionKey,
                currentPredictionKey);
        }

        /// <summary>
        /// Validates the ability activation addressed by incoming replicated target data.
        /// </summary>
        private bool CanAcceptReplicatedTargetData(
            GameplayAbilitySpecHandle abilityHandle,
            PredictionKey abilityOriginalPredictionKey)
        {
            GameplayAbilitySpec abilitySpec =
                m_AbilitySystem.FindAbilitySpecFromHandle(
                    abilityHandle);

            if (
                abilitySpec != null &&
                abilityOriginalPredictionKey.IsValid &&
                abilitySpec
                    .ActivationInfo
                    .GetActivationPredictionKey() ==
                abilityOriginalPredictionKey)
            {
                return true;
            }

            Debug.LogWarning(
                $"Rejected target data for ability '{abilityHandle}'.",
                this);

            return false;
        }

        /// <summary>
        /// Confirms a predicted ability activation on the owning client.
        /// </summary>
        [TargetRpc]
        private void ClientActivateAbilitySucceed(
            GameplayAbilitySpecHandle handle,
            PredictionKey predictionKey)
        {
            m_AbilitySystem.ConfirmAbilityActivation(
                handle,
                predictionKey);
        }

        /// <summary>
        /// Rejects a predicted ability activation on the owning client.
        /// </summary>
        [TargetRpc]
        private void ClientActivateAbilityFailed(
            GameplayAbilitySpecHandle handle,
            PredictionKey predictionKey)
        {
            m_AbilitySystem.RejectAbilityActivation(
                handle,
                predictionKey);
        }

        /// <summary>
        /// Receives an owning client's predicted ability activation request.
        /// </summary>
        [Command]
        private void ServerTryActivateAbility(
            GameplayAbilitySpecHandle handle,
            PredictionKey predictionKey,
            string activationGUID)
        {
            InternalServerTryActivateAbility(
                handle,
                predictionKey,
                activationGUID).Forget();
        }

        /// <summary>
        /// Validates and executes one owner-predicted ability activation on the server.
        /// </summary>
        private async UniTask InternalServerTryActivateAbility(
            GameplayAbilitySpecHandle handle,
            PredictionKey predictionKey,
            string activationGUID)
        {
            if (!predictionKey.IsValid)
            {
                Debug.LogWarning(
                    $"Cannot activate ability '{handle}': prediction key is invalid.",
                    this);

                return;
            }

            GameplayAbilitySpec abilitySpec =
                m_AbilitySystem.FindAbilitySpecFromHandle(
                    handle);

            if (abilitySpec == null)
            {
                Debug.LogWarning(
                    $"Cannot activate ability '{handle}': specification was not found.",
                    this);

                ClientActivateAbilityFailed(
                    handle,
                    predictionKey);

                return;
            }

            GameplayAbilityActivationInfo activationInfo = new(
                GameplayAbilityActivationMode.Authority,
                predictionKey);

            m_AbilitySystem.SetGameplayAbilityActivationInfo(
                handle,
                activationInfo);

            bool wasActivated = await m_AbilitySystem.TryActivateAbility(
                handle,
                activationGUID);

            if (wasActivated)
            {
                ClientActivateAbilitySucceed(
                    handle,
                    predictionKey);

                ReplicatePredictionKey(
                    predictionKey);

                return;
            }

            ClientActivateAbilityFailed(
                handle,
                predictionKey);

            m_AbilitySystem.SetGameplayAbilityActivationInfo(
                handle,
                new GameplayAbilityActivationInfo(
                    GameplayAbilityActivationMode.Authority));
        }

        /// <summary>
        /// Ensures this adapter uses owner-only synchronization while editing.
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();

            if (Application.isPlaying)
            {
                return;
            }

            syncMode = SyncMode.Owner;
        }
    }
}