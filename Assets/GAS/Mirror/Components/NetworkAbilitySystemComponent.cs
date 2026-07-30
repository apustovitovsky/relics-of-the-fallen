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
        NetworkBehaviour
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
        /// Initializes ability actor information for this networked avatar instance.
        /// </summary>
        private void Awake()
        {
            m_AbilitySystem.InitAbilityActorInfo(
                gameObject,
                gameObject);
        }

        /// <summary>
        /// Starts replicated active-effect lifecycle handling for the owning client.
        /// </summary>
        public override void OnStartAuthority()
        {
            base.OnStartAuthority();

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

            if (
                oldState.SourceNetworkId !=
                currentState.SourceNetworkId)
            {
                throw new InvalidOperationException(
                    $"Active gameplay effect '{replicationId}' cannot change " +
                    "its source identity.");
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

            GameplayEffectContextHandle effectContext;

            if (
                NetworkClient.spawned.TryGetValue(
                    state.SourceNetworkId,
                    out NetworkIdentity sourceIdentity))
            {
                if (
                    !sourceIdentity.TryGetComponent(
                        out NetworkAbilitySystemComponent sourceNetwork))
                {
                    throw new InvalidOperationException(
                        "Gameplay effect source has no network ability-system component.");
                }

                effectContext =
                    sourceNetwork.m_AbilitySystem.MakeEffectContext();
            }
            else
            {
                effectContext =
                    new GameplayEffectContextHandle(
                        new GameplayEffectContext());
            }

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

            double currentWorldTime =
                Time.timeAsDouble;

            double currentServerWorldTime =
                NetworkTime.time;

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
        /// Creates network state from one authoritative active gameplay effect.
        /// </summary>
        private bool TryCreateActiveGameplayEffectReplicationState(
            ActiveGameplayEffect activeEffect,
            out ActiveGameplayEffectReplicationState state)
        {
            GameplayEffectSpec spec =
                activeEffect.Spec;

            GameplayEffectSO definitionAsset =
                spec.DefinitionAsset;

            if (definitionAsset == null)
            {
                state =
                    default;

                return false;
            }

            GameplayAbilityActorInfo sourceActorInfo =
                spec.Source.AbilityActorInfo;

            if (
                sourceActorInfo == null ||
                sourceActorInfo.OwnerActor == null)
            {
                state =
                    default;

                return false;
            }

            if (
                !sourceActorInfo.OwnerActor.TryGetComponent(
                    out NetworkIdentity sourceIdentity) ||
                sourceIdentity.netId == 0)
            {
                state =
                    default;

                return false;
            }

            int modifierCount =
                spec.ModifierSpecs.Count;

            float[] evaluatedModifierMagnitudes =
                new float[modifierCount];

            for (
                int index = 0;
                index < modifierCount;
                index++)
            {
                evaluatedModifierMagnitudes[index] =
                    spec.ModifierSpecs[index].EvaluatedMagnitude;
            }

            AssetId definitionId =
                m_AssetRegistry.GetAssetId(
                    definitionAsset);

            state =
                new ActiveGameplayEffectReplicationState(
                    definitionId,
                    sourceIdentity.netId,
                    spec.Level,
                    spec.Duration,
                    activeEffect.StartServerWorldTime,
                    activeEffect.PredictionKey,
                    evaluatedModifierMagnitudes);

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
            GameplayAbilitySO ability,
            NetworkAbilitySystemComponent target)
        {
            InternalTryActivateAbility(
                ability,
                target).Forget();
        }

        /// <summary>
        /// Performs local prediction and forwards a successful activation request to the server.
        /// </summary>
        private async UniTask InternalTryActivateAbility(
            GameplayAbilitySO ability,
            NetworkAbilitySystemComponent target)
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

            if (target == null)
            {
                Debug.LogWarning(
                    $"Cannot activate ability '{abilitySpec.Handle}' without a network target.",
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
                target.m_AbilitySystem,
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
                target,
                predictionKey,
                activationGUID);
        }

        /// <summary>
        /// Sends an owning client's predicted ability activation request to the server.
        /// </summary>
        private void CallServerTryActivateAbility(
            GameplayAbilitySpecHandle handle,
            NetworkAbilitySystemComponent target,
            PredictionKey predictionKey,
            string activationGUID)
        {
            ServerTryActivateAbility(
                handle,
                target.netId,
                predictionKey,
                activationGUID);
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
            uint targetNetworkId,
            PredictionKey predictionKey,
            string activationGUID)
        {
            InternalServerTryActivateAbility(
                handle,
                targetNetworkId,
                predictionKey,
                activationGUID).Forget();
        }

        /// <summary>
        /// Validates and executes one owner-predicted ability activation on the server.
        /// </summary>
        private async UniTask InternalServerTryActivateAbility(
            GameplayAbilitySpecHandle handle,
            uint targetNetworkId,
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

            if (!NetworkServer.spawned.TryGetValue(
                    targetNetworkId,
                    out NetworkIdentity targetIdentity))
            {
                Debug.LogWarning(
                    $"Cannot activate ability '{handle}': " +
                    $"target network identity '{targetNetworkId}' was not found.",
                    this);

                ClientActivateAbilityFailed(
                    handle,
                    predictionKey);

                return;
            }

            if (!targetIdentity.TryGetComponent(
                    out NetworkAbilitySystemComponent targetNetwork))
            {
                Debug.LogWarning(
                    $"Cannot activate ability '{handle}': " +
                    $"target has no {nameof(NetworkAbilitySystemComponent)}.",
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
                targetNetwork.m_AbilitySystem,
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