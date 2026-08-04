using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Linq;
using UnityEngine;
using System;

namespace GAS
{
    public readonly struct GameplayEventData
    {
        public GameplayEventData(
            GameplayTag tag,
            string activationGUID)
        {

            Tag = tag;
            ActivationGUID = activationGUID;
        }

        public GameplayTag Tag
        {
            get;
        }

        public string ActivationGUID
        {
            get;
        }
    }

    [Serializable]
    public class AbilitySystemComponent : MonoBehaviour
    {
        [field: NonSerialized]
        public GameplayAbilityActorInfo AbilityActorInfo
        {
            get;
            private set;
        }

        public IAbilitySystemReplicationTransport ReplicationTransport
        {
            private get;
            set;
        }

        private readonly Dictionary<
            ActiveGameplayEffectHandle,
            GameplayEffect> m_LegacyActiveEffectsByHandle = new();

        [NonSerialized]
        private ActiveGameplayEffectsContainer m_ActiveGameplayEffects;

        [NonSerialized]
        private PredictionKeyDelegates m_PredictionKeyDelegates;

        private readonly GameplayAbilityReplicatedDataContainer
            m_AbilityTargetDataMap = new();

        [NonSerialized]
        private uint m_PredictionKeySequence;

        public PredictionKeyDelegates PredictionKeyDelegates =>
            m_PredictionKeyDelegates ??=
                new PredictionKeyDelegates();

        /// <summary>
        /// Creates the next valid prediction key scoped to this ability system component.
        /// </summary>
        public PredictionKey CreateNewPredictionKey()
        {
            m_PredictionKeySequence =
                m_PredictionKeySequence == uint.MaxValue
                    ? 1
                    : m_PredictionKeySequence + 1;

            return new PredictionKey(
                m_PredictionKeySequence);
        }

        /// <summary>
        /// Initializes the cached ability actor information for the current owner and avatar.
        /// </summary>
        public virtual void InitAbilityActorInfo(
            GameObject ownerActor,
            GameObject avatarActor)
        {
            AbilityActorInfo ??= new GameplayAbilityActorInfo();

            AbilityActorInfo.InitFromActor(
                ownerActor,
                avatarActor,
                this);
        }

        /// <summary>
        /// Returns whether this component's owning actor has authoritative execution control.
        /// </summary>
        public virtual bool IsOwnerActorAuthoritative()
        {
            return
                AbilityActorInfo == null ||
                AbilityActorInfo.IsNetAuthority();
        }

        /// <summary>
        /// Plays a montage and records its local ability and prediction state.
        /// </summary>
        public virtual float PlayMontage(
            GameplayAbility animatingAbility,
            GameplayAbilityActivationInfo activationInfo,
            GameplayAbilityMontage montage,
            float playRate = 1f,
            string startSectionName = null,
            float startTimeSeconds = 0f)
        {
            if (!string.IsNullOrEmpty(
                    startSectionName))
            {
                throw new NotSupportedException(
                    "Montage sections are not implemented.");
            }

            if (AbilityActorInfo == null ||
                AbilityActorInfo.AnimInstance == null)
            {
                return 0f;
            }

            float duration = AbilityActorInfo.AnimInstance.MontagePlay(
                montage,
                playRate,
                startTimeSeconds);

            if (duration <= 0f)
            {
                return duration;
            }

            byte playInstanceId = unchecked(
                (byte)(LocalAnimMontageInfo.PlayInstanceId + 1));

            PredictionKey predictionKey =
                activationInfo.GetActivationPredictionKey();

            LocalAnimMontageInfo =
                new GameplayAbilityLocalAnimMontage(
                    animatingAbility,
                    montage,
                    playInstanceId,
                    predictionKey);

            if (activationInfo.ActivationMode ==
                    GameplayAbilityActivationMode.Predicting &&
                predictionKey.IsValid)
            {
                void HandlePredictionRejected()
                {
                    OnPredictiveMontageRejected(
                        montage);
                }

                _ = PredictionKeyDelegates.RegisterRejectedDelegate(
                    predictionKey,
                    HandlePredictionRejected);
            }

            AnimMontageUpdateReplicatedData();

            return duration;
        }

        /// <summary>
        /// Stops a predicted montage when its activation prediction is rejected.
        /// </summary>
        protected virtual void OnPredictiveMontageRejected(
            GameplayAbilityMontage predictiveMontage)
        {
            if (GetCurrentMontage() != predictiveMontage)
            {
                return;
            }

            CurrentMontageStop();
        }

        /// <summary>
        /// Plays a replicated montage without producing replication or prediction side effects.
        /// </summary>
        public virtual float PlayMontageSimulated(
            GameplayAbilityMontage montage,
            float playRate = 1f,
            string startSectionName = null)
        {
            if (!string.IsNullOrEmpty(
                    startSectionName))
            {
                throw new NotSupportedException(
                    "Montage sections are not implemented.");
            }

            if (AbilityActorInfo == null ||
                AbilityActorInfo.AnimInstance == null)
            {
                return 0f;
            }

            float duration = AbilityActorInfo.AnimInstance.MontagePlay(
                montage,
                playRate,
                0f);

            if (duration <= 0f)
            {
                return duration;
            }

            GameplayAbilityLocalAnimMontage localAnimMontageInfo =
                LocalAnimMontageInfo;

            localAnimMontageInfo.AnimMontage =
                montage;

            LocalAnimMontageInfo =
                localAnimMontageInfo;

            return duration;
        }

        /// <summary>
        /// Returns the gameplay ability currently responsible for montage playback.
        /// </summary>
        public GameplayAbility GetAnimatingAbility()
        {
            return LocalAnimMontageInfo.AnimatingAbility;
        }

        /// <summary>
        /// Returns the montage currently playing on the avatar animation instance.
        /// </summary>
        public GameplayAbilityMontage GetCurrentMontage()
        {
            if (AbilityActorInfo == null ||
                AbilityActorInfo.AnimInstance == null ||
                AbilityActorInfo.AnimInstance.MontageGetIsStopped())
            {
                return null;
            }

            return AbilityActorInfo.AnimInstance.CurrentMontage;
        }

        /// <summary>
        /// Clears the animating ability when it still owns the current local montage state.
        /// </summary>
        public virtual void ClearAnimatingAbility(
            GameplayAbility ability)
        {
            if (LocalAnimMontageInfo.AnimatingAbility != ability)
            {
                return;
            }

            GameplayAbilityLocalAnimMontage localAnimMontageInfo =
                LocalAnimMontageInfo;

            localAnimMontageInfo.AnimatingAbility = null;

            LocalAnimMontageInfo = localAnimMontageInfo;
        }

        /// <summary>
        /// Stops the current montage and records its final replicated state.
        /// </summary>
        public virtual void CurrentMontageStop(
            float overrideBlendOutTime = -1f)
        {
            GameplayAbilityMontage montage =
                LocalAnimMontageInfo.AnimMontage;

            if (montage == null ||
                AbilityActorInfo == null ||
                AbilityActorInfo.AnimInstance == null)
            {
                return;
            }

            float blendOutTime =
                overrideBlendOutTime < 0f
                    ? 0f
                    : overrideBlendOutTime;

            AbilityActorInfo.AnimInstance.MontageStop(
                blendOutTime,
                montage);

            GameplayAbilityLocalAnimMontage localAnimMontageInfo =
                LocalAnimMontageInfo;

            localAnimMontageInfo.AnimatingAbility = null;

            LocalAnimMontageInfo = localAnimMontageInfo;

            AnimMontageUpdateReplicatedData();
        }

        /// <summary>
        /// Updates replicated montage data from the current local animation state.
        /// </summary>
        public virtual void AnimMontageUpdateReplicatedData()
        {
            GameplayAbilityMontage montage =
                LocalAnimMontageInfo.AnimMontage;

            if (montage == null ||
                AbilityActorInfo == null ||
                AbilityActorInfo.AnimInstance == null)
            {
                return;
            }

            AnimInstance animInstance =
                AbilityActorInfo.AnimInstance;

            RepAnimMontageInfo =
                new GameplayAbilityRepAnimMontage(
                    montage,
                    LocalAnimMontageInfo.PlayInstanceId,
                    animInstance.MontageGetPlayRate(),
                    animInstance.MontageGetPosition(),
                    0f,
                    animInstance.MontageGetIsStopped(),
                    LocalAnimMontageInfo.PredictionKey);
        }

        /// <summary>
        /// Applies authoritative replicated montage state to a simulated animation instance.
        /// </summary>
        public virtual void OnRepReplicatedAnimMontage(
            GameplayAbilityRepAnimMontage repAnimMontageInfo)
        {
            RepAnimMontageInfo = repAnimMontageInfo;

            GameplayAbilityMontage montage = repAnimMontageInfo.Animation;

            if (montage == null ||
                AbilityActorInfo == null ||
                AbilityActorInfo.AnimInstance == null)
            {
                return;
            }

            AnimInstance animInstance = AbilityActorInfo.AnimInstance;

            bool isNewMontage =
                LocalAnimMontageInfo.AnimMontage != montage ||
                LocalAnimMontageInfo.PlayInstanceId !=
                    repAnimMontageInfo.PlayInstanceId;

            if (isNewMontage &&
                !repAnimMontageInfo.IsStopped)
            {
                float duration = PlayMontageSimulated(
                    montage,
                    repAnimMontageInfo.PlayRate);

                if (duration <= 0f)
                {
                    return;
                }
            }

            if (isNewMontage)
            {
                GameplayAbilityLocalAnimMontage localAnimMontageInfo =
                    LocalAnimMontageInfo;

                localAnimMontageInfo.AnimatingAbility = null;
                localAnimMontageInfo.AnimMontage = montage;
                localAnimMontageInfo.PlayInstanceId =
                    repAnimMontageInfo.PlayInstanceId;
                localAnimMontageInfo.PredictionKey = default;

                LocalAnimMontageInfo = localAnimMontageInfo;
            }

            if (repAnimMontageInfo.IsStopped)
            {
                animInstance.MontageStop(
                    repAnimMontageInfo.BlendTime,
                    montage);

                return;
            }

            animInstance.MontageSetPlayRate(
                montage,
                repAnimMontageInfo.PlayRate);

            animInstance.MontageSetPosition(
                montage,
                repAnimMontageInfo.Position);
        }

        [NonSerialized]
        private GameplayTagCountContainer m_OwnedGameplayTags;

        public GameplayTagCountContainer OwnedGameplayTags =>
            m_OwnedGameplayTags ??=
                new GameplayTagCountContainer();



        public ActiveGameplayEffectsContainer ActiveGameplayEffects =>
            m_ActiveGameplayEffects ??=
                new ActiveGameplayEffectsContainer(this);

        [field: NonSerialized]
        public GameplayAbilityLocalAnimMontage LocalAnimMontageInfo
        {
            get;
            private set;
        }

        [field: NonSerialized]
        public GameplayAbilityRepAnimMontage RepAnimMontageInfo
        {
            get;
            private set;
        }

        public GroupASC InitialData;

        [ReadOnly]
        public Dictionary<string, Attribute> AttributesDictionary = new();

        private readonly Dictionary<GameplayTag, int>
            m_BlockedAbilityTagCounts = new();

        public List<Attribute> attributes = new();

        public Action<
            AttributeName,
            float,
            float,
            GameplayEffect> OnAttributeChanged;

        [SerializeReference]
        public List<AttributeProcessor> attributesProcessors =
            new();

        private readonly GameplayAbilitySpecContainer
            m_ActivatableAbilities = new();

        public GameplayAbilitySpecContainer ActivatableAbilities =>
            m_ActivatableAbilities;

        [SerializeReference]
        public List<GameplayAbility> grantedGameplayAbilities =
            new();

        public Action<
            GameplayAbility,
            string> OnGameplayAbilityPreActivate;

        public Action<
            GameplayAbility,
            string> OnGameplayAbilityActivated;

        public Action<
            GameplayAbility,
            string> OnGameplayAbilityTryActivate;

        public Action<
            GameplayAbility,
            string> OnGameplayAbilityDeactivated;

        public Action<
            GameplayAbility,
            string,
            ActivationFailure> OnGameplayAbilityFailedActivation;

        public Action<GameplayAbility> OnGameplayAbilityGranted;
        public Action<GameplayAbility> OnGameplayAbilityUngranted;

        public event Action<GameplayEventData> OnGameplayEvent;

        public List<GameplayEffect> AppliedGameplayEffects =
            new();

        public Action<GameplayEffect> OnGameplayEffectApplied;
        public Action<GameplayEffect> OnGameplayEffectRemoved;

        public Action<
            List<GameplayEffect>> OnGameplayEffectsChanged;

        public List<GameplayTag> tags =
            new();

        public Action<
            List<GameplayTag>,
            AbilitySystemComponent,
            AbilitySystemComponent,
            string> OnTagsInstant;

        public float level = 1;

        public List<GameplayCue> instancedCues =
            new();

        public bool logging = false;

        [ReadOnly]
        public bool invokeEventsGA = true;

        [ReadOnly]
        public bool invokeEventsGE = true;

        /// <summary>
        /// If an ability can't be activated immediately,
        /// keeps retrying it for a moment.
        /// </summary>
        public bool inputBuffering = true;

        private readonly float inputBufferDurationSeconds = .16f;

        /// <summary>
        /// Initializes attributes, abilities, and gameplay system listeners.
        /// </summary>
        public void Awake()
        {
            InitialData.AddAttributes(this);
            InitialData.AddAttributeProcessors(this);
            InitialData.GrantAbilities(this);

            OnGameplayEffectApplied +=
                ge => OnGameplayEffectsChanged?.Invoke(
                    AppliedGameplayEffects);

            OnGameplayEffectRemoved +=
                ge => OnGameplayEffectsChanged?.Invoke(
                    AppliedGameplayEffects);

            attributes.ForEach(
                attribute =>
                    attribute.OnPostAttributeChange +=
                        (
                            attributeName,
                            oldValue,
                            newValue,
                            gameplayEffect) =>
                        {
                            OnAttributeChanged?.Invoke(
                                attributeName,
                                oldValue,
                                newValue,
                                gameplayEffect);
                        });

            attributes.ForEach(
                attribute =>
                {
                    attribute.name =
                        attribute.attributeName.name;

                    AttributesDictionary.Add(
                        attribute.attributeName.name,
                        attribute);
                });

            OnGameplayEffectApplied +=
                TriggerOnTagsAdded;

            GameplayCueManager.Register(this);
        }

        /// <summary>
        /// Initializes attribute notifications and diagnostic listeners.
        /// </summary>
        private void Start()
        {
            InitializeAttributesListeners();

            if (logging)
            {
                OnAttributeChanged +=
                    (
                        attributeName,
                        oldValue,
                        newValue,
                        gameplayEffect) =>
                    {
                        Debug.Log(
                            $"{attributeName.name} " +
                            $"{oldValue} -> {newValue} / " +
                            $"ge: {gameplayEffect?.name}");
                    };

                OnTagsInstant +=
                    (
                        newTags,
                        source,
                        target,
                        applicationGUID) =>
                    {
                        Debug.Log(
                            $"[TAGS] OnTagsInstant! tags: " +
                            $"[{string.Join(", ", newTags.Select(tag => tag.name))}]");
                    };

                OnGameplayEvent +=
                    gameplayEvent =>
                    {
                        Debug.Log(
                            $"[GAMEPLAY EVENT] " +
                            $"{gameplayEvent.Tag.name} / " +
                            $"activation: " +
                            $"{gameplayEvent.ActivationGUID}");
                    };
            }

            OnGameplayAbilityFailedActivation +=
                (
                    gameplayAbility,
                    activationGUID,
                    failureCause) =>
                {
                    Debug.Log(
                        $"GA Failed Activation: " +
                        $"{gameplayAbility.name} " +
                        $"{failureCause}");
                };
        }

        private void OnDestroy()
        {
            foreach (
                GameplayAbility gameplayAbility
                in grantedGameplayAbilities)
            {

                if (gameplayAbility.IsActive)
                {
                    gameplayAbility.DeactivateAbility();
                }
            }
        }

        /// <summary>
        /// Отправляет одноразовое локальное событие активным
        /// способностям этого ASC.
        ///
        /// GameplayTag используется только как идентификатор события
        /// и не добавляется в список постоянных ASC tags.
        /// </summary>
        public void SendGameplayEvent(
            GameplayTag tag,
            string activationGUID = null)
        {

            if (tag == null)
            {
                Debug.LogWarning(
                    $"ASC {name} ignored a gameplay event " +
                    "without a tag.",
                    this);

                return;
            }

            OnGameplayEvent?.Invoke(
                new GameplayEventData(
                    tag,
                    activationGUID));
        }

        public void TriggerOnTagsAdded(
            GameplayEffect appliedGameplayEffect)
        {

            if (
                appliedGameplayEffect
                    .gameplayEffectTags
                    .GrantedTags
                    .Count == 0)
            {

                return;
            }

            if (
                appliedGameplayEffect.durationType ==
                GameplayEffectDurationType.Instant)
            {

                OnTagsInstant?.Invoke(
                    appliedGameplayEffect
                        .gameplayEffectTags
                        .GrantedTags,
                    appliedGameplayEffect.source,
                    appliedGameplayEffect.target,
                    appliedGameplayEffect.applicationGUID);
            }
        }

        /// <summary>
        /// Returns the attribute instance identified by its definition.
        /// </summary>
        public Attribute GetAttribute(
            AttributeName attributeName)
        {
            if (attributeName == null)
            {
                throw new ArgumentNullException(
                    nameof(attributeName));
            }

            if (
                !AttributesDictionary.TryGetValue(
                    attributeName.name,
                    out Attribute attribute))
            {
                throw new KeyNotFoundException(
                    $"Attribute '{attributeName.name}' is not registered.");
            }

            return attribute;
        }

        /// <summary>
        /// Returns the current evaluated value of an attribute.
        /// </summary>
        public float GetAttributeValue(
            string attributeName)
        {
            if (
                AttributesDictionary.TryGetValue(
                    attributeName,
                    out Attribute attribute))
            {
                return attribute.CurrentValue;
            }

            Debug.LogWarning(
                $"No Attribute named {attributeName}");

            return 0;
        }

        public float GetAttributeValue(
            AttributeName attributeName)
        {

            return GetAttributeValue(
                attributeName.name);
        }

        /// <summary>
        /// Sets an attribute base value while preserving its active modifiers.
        /// </summary>
        public void SetNumericAttributeBase(
            AttributeName attributeName,
            float newBaseValue)
        {
            Attribute attribute =
                GetAttribute(
                    attributeName);

            float oldValue =
                attribute.CurrentValue;

            foreach (
                AttributeProcessor processor
                in attributesProcessors)
            {
                processor.PreAttributeBaseChange(
                    attribute,
                    ref newBaseValue,
                    this);
            }

            attribute.SetBaseValue(
                newBaseValue);

            float newValue =
                attribute.CurrentValue;

            if (
                Mathf.Approximately(
                    oldValue,
                    newValue))
            {
                return;
            }

            attribute.OnPostAttributeChange?.Invoke(
                attribute.attributeName,
                oldValue,
                newValue,
                null);
        }

        /// <summary>
        /// Applies an authoritative replicated attribute base value while preserving local modifiers.
        /// </summary>
        public void SetBaseAttributeValueFromReplication(
            AttributeName attributeName,
            float newBaseValue)
        {
            Attribute attribute =
                GetAttribute(
                    attributeName);

            float oldValue =
                attribute.CurrentValue;

            attribute.SetBaseValue(
                newBaseValue);

            float newValue =
                attribute.CurrentValue;

            if (
                Mathf.Approximately(
                    oldValue,
                    newValue))
            {
                return;
            }

            attribute.OnPostAttributeChange?.Invoke(
                attribute.attributeName,
                oldValue,
                newValue,
                null);
        }

        /// <summary>
        /// Publishes the initial evaluated values of all registered attributes.
        /// </summary>
        public void InitializeAttributesListeners()
        {
            foreach (
                Attribute attribute
                in attributes)
            {
                attribute.OnPostAttributeChange?.Invoke(
                    attribute.attributeName,
                    0f,
                    attribute.CurrentValue,
                    null);
            }
        }



        /// <summary>
        /// Grants ability and returns the newly instantiated GA.
        /// </summary>
        public GameplayAbility GrantAbility(
            GameplayAbility gameplayAbility)
        {

            GameplayAbility abilityCopy =
                gameplayAbility.Instantiate(this);

            grantedGameplayAbilities.Add(
                abilityCopy);

            OnGameplayAbilityGranted?.Invoke(
                abilityCopy);

            return abilityCopy;
        }

        /// <summary>
        /// Gives an ability specification to this ability system and returns its stable handle.
        /// </summary>
        public GameplayAbilitySpecHandle GiveAbility(
            GameplayAbilitySpec abilitySpec)
        {
            if (abilitySpec == null)
            {
                throw new ArgumentNullException(
                    nameof(abilitySpec));
            }

            GameplayAbility ability =
                abilitySpec.CreatePrimaryInstance(
                    this);

            m_ActivatableAbilities.Add(
                abilitySpec);

            grantedGameplayAbilities.Add(
                ability);

            OnGameplayAbilityGranted?.Invoke(
                ability);

            return abilitySpec.Handle;
        }

        /// <summary>
        /// Finds an activatable gameplay ability specification by its stable handle.
        /// </summary>
        public GameplayAbilitySpec FindAbilitySpecFromHandle(
            GameplayAbilitySpecHandle handle)
        {
            if (!handle.IsValid)
            {
                return null;
            }

            return
                m_ActivatableAbilities.FindAbilitySpecFromHandle(
                    handle);
        }

        /// <summary>
        /// Finds the first activatable ability specification created from the requested definition.
        /// </summary>
        public GameplayAbilitySpec FindAbilitySpecFromClass(
            GameplayAbilitySO ability)
        {
            return m_ActivatableAbilities.FindAbilitySpecFromClass(
                ability);
        }

        /// <summary>
        /// Updates an ability specification level and marks it for replicated synchronization.
        /// </summary>
        public void SetGameplayAbilitySpecLevel(
            GameplayAbilitySpecHandle handle,
            int level)
        {
            GameplayAbilitySpec abilitySpec =
                FindAbilitySpecFromHandle(
                    handle);

            if (
                abilitySpec == null ||
                abilitySpec.Level == level)
            {
                return;
            }

            abilitySpec.Level =
                level;

            if (abilitySpec.PrimaryInstance != null)
            {
                abilitySpec.PrimaryInstance.Level =
                    level;
            }

            m_ActivatableAbilities.MarkAbilitySpecDirty(
                abilitySpec);
        }

        /// <summary>
        /// Updates activation state on an ability specification and its primary runtime instance.
        /// </summary>
        public void SetGameplayAbilityActivationInfo(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActivationInfo activationInfo)
        {
            GameplayAbilitySpec abilitySpec =
                FindAbilitySpecFromHandle(handle);

            if (abilitySpec == null)
            {
                return;
            }

            abilitySpec.ActivationInfo =
                activationInfo;

            if (abilitySpec.PrimaryInstance != null)
            {
                abilitySpec.PrimaryInstance.CurrentActivationInfo =
                    activationInfo;
            }
        }

        /// <summary>
        /// Registers a callback for confirmed target data belonging to one ability activation.
        /// </summary>
        public IDisposable AbilityTargetDataSetDelegate(
            GameplayAbilitySpecHandle abilityHandle,
            PredictionKey abilityOriginalPredictionKey,
            Action<GameplayAbilityTargetDataHandle, GameplayTag> handler)
        {
            GameplayAbilitySpecHandleAndPredictionKey key =
                new(
                    abilityHandle,
                    abilityOriginalPredictionKey);

            AbilityReplicatedDataCache cache =
                m_AbilityTargetDataMap.FindOrAdd(
                    key);

            return cache.RegisterTargetSetDelegate(
                handler);
        }

        /// <summary>
        /// Registers a callback for target cancellation belonging to one ability activation.
        /// </summary>
        public IDisposable AbilityTargetDataCancelledDelegate(
            GameplayAbilitySpecHandle abilityHandle,
            PredictionKey abilityOriginalPredictionKey,
            Action handler)
        {
            GameplayAbilitySpecHandleAndPredictionKey key =
                new(
                    abilityHandle,
                    abilityOriginalPredictionKey);

            AbilityReplicatedDataCache cache =
                m_AbilityTargetDataMap.FindOrAdd(
                    key);

            return cache.RegisterTargetCancelledDelegate(
                handler);
        }

        /// <summary>
        /// Sends confirmed target data through the configured ability-system replication transport.
        /// </summary>
        public void CallServerSetReplicatedTargetData(
            GameplayAbilitySpecHandle abilityHandle,
            PredictionKey abilityOriginalPredictionKey,
            GameplayAbilityTargetDataHandle replicatedTargetDataHandle,
            GameplayTag applicationTag,
            PredictionKey currentPredictionKey)
        {
            IAbilitySystemReplicationTransport replicationTransport =
                GetReplicationTransport();

            replicationTransport.CallServerSetReplicatedTargetData(
                abilityHandle,
                abilityOriginalPredictionKey,
                replicatedTargetDataHandle,
                applicationTag,
                currentPredictionKey);
        }

        /// <summary>
        /// Sends target-data cancellation through the configured ability-system replication transport.
        /// </summary>
        public void ServerSetReplicatedTargetDataCancelled(
            GameplayAbilitySpecHandle abilityHandle,
            PredictionKey abilityOriginalPredictionKey,
            PredictionKey currentPredictionKey)
        {
            IAbilitySystemReplicationTransport replicationTransport =
                GetReplicationTransport();

            replicationTransport.ServerSetReplicatedTargetDataCancelled(
                abilityHandle,
                abilityOriginalPredictionKey,
                currentPredictionKey);
        }

        /// <summary>
        /// Returns the configured replication transport required by non-authoritative execution.
        /// </summary>
        private IAbilitySystemReplicationTransport GetReplicationTransport()
        {
            if (ReplicationTransport == null)
            {
                throw new InvalidOperationException(
                    "Ability system replication transport is not configured.");
            }

            return ReplicationTransport;
        }

        /// <summary>
        /// Stores confirmed replicated target data and notifies the waiting ability task.
        /// </summary>
        public void SetReplicatedTargetData(
            GameplayAbilitySpecHandle abilityHandle,
            PredictionKey abilityOriginalPredictionKey,
            GameplayAbilityTargetDataHandle replicatedTargetDataHandle,
            GameplayTag applicationTag,
            PredictionKey currentPredictionKey)
        {
            GameplayAbilitySpecHandleAndPredictionKey key =
                new(
                    abilityHandle,
                    abilityOriginalPredictionKey);

            AbilityReplicatedDataCache cache =
                m_AbilityTargetDataMap.FindOrAdd(
                    key);

            cache.SetTargetData(
                replicatedTargetDataHandle,
                applicationTag,
                currentPredictionKey);
        }

        /// <summary>
        /// Stores replicated target cancellation and notifies the waiting ability task.
        /// </summary>
        public void SetReplicatedTargetDataCancelled(
            GameplayAbilitySpecHandle abilityHandle,
            PredictionKey abilityOriginalPredictionKey,
            PredictionKey currentPredictionKey)
        {
            GameplayAbilitySpecHandleAndPredictionKey key =
                new(
                    abilityHandle,
                    abilityOriginalPredictionKey);

            AbilityReplicatedDataCache cache =
                m_AbilityTargetDataMap.FindOrAdd(
                    key);

            cache.SetTargetCancelled(
                currentPredictionKey);
        }

        /// <summary>
        /// Invokes cached target confirmation or cancellation when it has already arrived.
        /// </summary>
        public bool CallReplicatedTargetDataDelegatesIfSet(
            GameplayAbilitySpecHandle abilityHandle,
            PredictionKey abilityOriginalPredictionKey)
        {
            GameplayAbilitySpecHandleAndPredictionKey key =
                new(
                    abilityHandle,
                    abilityOriginalPredictionKey);

            AbilityReplicatedDataCache cache =
                m_AbilityTargetDataMap.Find(
                    key);

            return
                cache != null &&
                cache.CallDelegatesIfSet();
        }

        /// <summary>
        /// Consumes cached client target data while preserving registered delegates.
        /// </summary>
        public void ConsumeClientReplicatedTargetData(
            GameplayAbilitySpecHandle abilityHandle,
            PredictionKey abilityOriginalPredictionKey)
        {
            GameplayAbilitySpecHandleAndPredictionKey key =
                new(
                    abilityHandle,
                    abilityOriginalPredictionKey);

            AbilityReplicatedDataCache cache =
                m_AbilityTargetDataMap.Find(
                    key);

            if (cache == null)
            {
                return;
            }

            cache.Reset();
        }

        /// <summary>
        /// Removes all cached replicated data and delegates for one ability activation.
        /// </summary>
        public void ClearAbilityReplicatedDataCache(
            GameplayAbilitySpecHandle abilityHandle,
            GameplayAbilityActivationInfo activationInfo)
        {
            GameplayAbilitySpecHandleAndPredictionKey key =
                new(
                    abilityHandle,
                    activationInfo.GetActivationPredictionKey());

            m_AbilityTargetDataMap.Remove(
                key);
        }

        /// <summary>
        /// Confirms a predicted ability activation without resolving its predicted side effects.
        /// </summary>
        public void ConfirmAbilityActivation(
            GameplayAbilitySpecHandle handle,
            PredictionKey predictionKey)
        {
            GameplayAbilitySpec abilitySpec =
                FindAbilitySpecFromHandle(
                    handle);

            if (abilitySpec == null ||
                abilitySpec
                    .ActivationInfo
                    .GetActivationPredictionKey() !=
                predictionKey)
            {
                return;
            }

            GameplayAbilityActivationInfo activationInfo =
                abilitySpec.ActivationInfo;

            activationInfo.SetActivationConfirmed();

            SetGameplayAbilityActivationInfo(
                handle,
                activationInfo);
        }

        /// <summary>
        /// Rejects a predicted ability activation and resolves its prediction callbacks.
        /// </summary>
        public void RejectAbilityActivation(
            GameplayAbilitySpecHandle handle,
            PredictionKey predictionKey)
        {
            GameplayAbilitySpec abilitySpec =
                FindAbilitySpecFromHandle(handle);

            if (
                abilitySpec == null ||
                abilitySpec
                    .ActivationInfo
                    .GetActivationPredictionKey() !=
                predictionKey)
            {
                return;
            }

            GameplayAbilityActivationInfo activationInfo =
                abilitySpec.ActivationInfo;

            activationInfo.SetActivationRejected();

            SetGameplayAbilityActivationInfo(
                handle,
                activationInfo);

            PredictionKeyDelegates.Reject(
                predictionKey);
        }

        /// <summary>
        /// Grants an ability from its persistent definition asset and returns the runtime instance.
        /// </summary>
        public GameplayAbility GrantAbility(
            GameplayAbilitySO definitionAsset,
            int level = 1)
        {
            GameplayAbilitySpec abilitySpec =
                new(
                    definitionAsset,
                    level);

            GiveAbility(
                abilitySpec);

            return abilitySpec.PrimaryInstance;
        }

        /// <summary>
        /// Removes the gameplay ability specification identified by the requested handle.
        /// </summary>
        public void ClearAbility(
            GameplayAbilitySpecHandle handle)
        {
            GameplayAbilitySpec abilitySpec =
                FindAbilitySpecFromHandle(
                    handle);

            if (abilitySpec == null)
            {
                return;
            }

            GameplayAbility primaryInstance =
                abilitySpec.PrimaryInstance;

            if (
                !m_ActivatableAbilities.Remove(
                    handle,
                    out _))
            {
                throw new InvalidOperationException(
                    $"Ability specification '{handle}' could not be removed.");
            }

            if (primaryInstance == null)
            {
                return;
            }

            primaryInstance.DeactivateAbility(
                null);

            grantedGameplayAbilities.Remove(
                primaryInstance);

            OnGameplayAbilityUngranted?.Invoke(
                primaryInstance);
        }

        /// <summary>
        /// Removes every granted ability and clears all associated ability specifications.
        /// </summary>
        public void ClearAllAbilities()
        {
            while (m_ActivatableAbilities.Count > 0)
            {
                GameplayAbilitySpec abilitySpec =
                    m_ActivatableAbilities[
                        m_ActivatableAbilities.Count - 1];

                ClearAbility(
                    abilitySpec.Handle);
            }

            for (
                int index =
                    grantedGameplayAbilities.Count - 1;
                index >= 0;
                index--)
            {
                UngrantAbility(
                    grantedGameplayAbilities[index]);
            }
        }

        public void UngrantAbilityByTag(
            GameplayTag tag)
        {
            List<int> removeIndexes =
                new();

            grantedGameplayAbilities.ForEach(
                gameplayAbility =>
                {
                    if (
                        gameplayAbility
                            .abilityTags
                            .DescriptionTags
                            .Contains(tag))
                    {

                        removeIndexes.Add(
                            grantedGameplayAbilities.IndexOf(
                                gameplayAbility));
                    }
                });

            removeIndexes.ForEach(
                index => UngrantAbility(index));
        }

        [EasyButtons.Button]
        public void UngrantAbility(
            int index)
        {

            UngrantAbility(
                grantedGameplayAbilities[index]);
        }

        public void UngrantAbility(
            string guid)
        {

            UngrantAbility(
                grantedGameplayAbilities.Find(
                    gameplayAbility =>
                        gameplayAbility.Guid == guid));
        }

        /// <summary>
        /// Removes a legacy runtime ability or delegates removal to its owning specification.
        /// </summary>
        public void UngrantAbility(
            GameplayAbility gameplayAbility)
        {
            if (gameplayAbility == null)
            {
                throw new ArgumentNullException(
                    nameof(gameplayAbility));
            }

            for (
                int index = 0;
                index < m_ActivatableAbilities.Count;
                index++)
            {
                GameplayAbilitySpec abilitySpec =
                    m_ActivatableAbilities[index];

                if (
                    !ReferenceEquals(
                        abilitySpec.PrimaryInstance,
                        gameplayAbility))
                {
                    continue;
                }

                ClearAbility(
                    abilitySpec.Handle);

                return;
            }

            gameplayAbility.DeactivateAbility(
                null);

            grantedGameplayAbilities.Remove(
                gameplayAbility);

            OnGameplayAbilityUngranted?.Invoke(
                gameplayAbility);
        }

        public List<GameplayTag> GetAllTags()
        {
            return tags;
        }

        /// <summary>
        /// Registers a callback for changes to one owned gameplay tag.
        /// </summary>
        public IDisposable RegisterGameplayTagEvent(
            GameplayTag tag,
            GameplayTagEventType eventType,
            Action<GameplayTag, int> handler)
        {
            return
                OwnedGameplayTags.RegisterGameplayTagEvent(
                    tag,
                    eventType,
                    handler);
        }

        /// <summary>
        /// Registers a callback for additions or removals of any owned gameplay tag.
        /// </summary>
        public IDisposable RegisterGenericGameplayTagEvent(
            Action<GameplayTag, int> handler)
        {
            return
                OwnedGameplayTags.RegisterGenericGameplayEvent(
                    handler);
        }

        /// <summary>
        /// Returns the owned count of a gameplay tag including matching child tags.
        /// </summary>
        public int GetGameplayTagCount(
            GameplayTag tag)
        {
            return
                OwnedGameplayTags.GetTagCount(
                    tag);
        }

        /// <summary>
        /// Returns whether this component owns the gameplay tag or one of its children.
        /// </summary>
        public bool HasMatchingGameplayTag(
            GameplayTag tag)
        {
            return
                OwnedGameplayTags.HasMatchingGameplayTag(
                    tag);
        }

        /// <summary>
        /// Returns whether this component owns any supplied gameplay tag.
        /// </summary>
        public bool HasAnyMatchingGameplayTags(
            IReadOnlyList<GameplayTag> gameplayTags)
        {
            return
                OwnedGameplayTags.HasAnyMatchingGameplayTags(
                    gameplayTags);
        }

        /// <summary>
        /// Returns whether this component owns every supplied gameplay tag.
        /// </summary>
        public bool HasAllMatchingGameplayTags(
            IReadOnlyList<GameplayTag> gameplayTags)
        {
            return
                OwnedGameplayTags.HasAllMatchingGameplayTags(
                    gameplayTags);
        }

        /// <summary>
        /// Updates owned gameplay tag contribution counts and refreshes the legacy tag view.
        /// </summary>
        internal void UpdateTagMap(
            IReadOnlyList<GameplayTag> gameplayTags,
            int countDelta)
        {
            if (gameplayTags == null)
            {
                throw new ArgumentNullException(
                    nameof(gameplayTags));
            }

            bool hasChanges =
                false;

            for (
                int index = 0;
                index < gameplayTags.Count;
                index++)
            {
                GameplayTag tag =
                    gameplayTags[index];

                if (tag == null)
                {
                    continue;
                }

                OwnedGameplayTags.UpdateTagCount(
                    tag,
                    countDelta);

                hasChanges =
                    true;
            }

            if (!hasChanges)
            {
                return;
            }

            OwnedGameplayTags.GetOwnedGameplayTags(
                tags);
        }

        /// <summary>
        /// Returns whether any ability tag is currently blocked.
        /// </summary>
        public virtual bool AreAbilityTagsBlocked(
            IReadOnlyList<GameplayTag> abilityTags)
        {
            if (abilityTags == null)
            {
                throw new ArgumentNullException(
                    nameof(abilityTags));
            }

            for (
                int index = 0;
                index < abilityTags.Count;
                index++)
            {
                GameplayTag tag =
                    abilityTags[index];

                if (
                    tag != null &&
                    m_BlockedAbilityTagCounts.TryGetValue(
                        tag,
                        out int count) &&
                    count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Applies ability blocking tags and cancels matching active abilities.
        /// </summary>
        public virtual void ApplyAbilityBlockAndCancelTags(
            IReadOnlyList<GameplayTag> abilityTags,
            GameplayAbility requestingAbility,
            bool enableBlockTags,
            IReadOnlyList<GameplayTag> blockTags,
            bool executeCancelTags,
            IReadOnlyList<GameplayTag> cancelTags)
        {
            if (abilityTags == null)
            {
                throw new ArgumentNullException(
                    nameof(abilityTags));
            }

            UpdateAbilityBlockTagCounts(
                blockTags,
                enableBlockTags ? 1 : -1);

            if (executeCancelTags)
            {
                CancelAbilities(
                    cancelTags,
                    null,
                    requestingAbility);
            }
        }

        /// <summary>
        /// Cancels active abilities selected by their identifying tags.
        /// </summary>
        public virtual void CancelAbilities(
            IReadOnlyList<GameplayTag> withTags = null,
            IReadOnlyList<GameplayTag> withoutTags = null,
            GameplayAbility ignore = null)
        {
            for (
                int abilityIndex = 0;
                abilityIndex < grantedGameplayAbilities.Count;
                abilityIndex++)
            {
                GameplayAbility ability =
                    grantedGameplayAbilities[abilityIndex];

                if (ability == null ||
                    ability == ignore ||
                    !ability.IsActive)
                {
                    continue;
                }

                IReadOnlyList<GameplayTag> abilityTags =
                    ability.abilityTags.DescriptionTags;

                if (withTags != null &&
                    !HasAnyAbilityTag(
                        abilityTags,
                        withTags))
                {
                    continue;
                }

                if (withoutTags != null &&
                    HasAnyAbilityTag(
                        abilityTags,
                        withoutTags))
                {
                    continue;
                }

                ability.CancelAbility(
                    ability.ActivationGUID);
            }
        }

        private void UpdateAbilityBlockTagCounts(
            IReadOnlyList<GameplayTag> blockTags,
            int countDelta)
        {
            if (blockTags == null)
            {
                return;
            }

            for (
                int index = 0;
                index < blockTags.Count;
                index++)
            {
                GameplayTag tag =
                    blockTags[index];

                if (tag == null)
                {
                    continue;
                }

                m_BlockedAbilityTagCounts.TryGetValue(
                    tag,
                    out int currentCount);

                int newCount =
                    currentCount +
                    countDelta;

                if (newCount > 0)
                {
                    m_BlockedAbilityTagCounts[tag] =
                        newCount;
                }
                else
                {
                    m_BlockedAbilityTagCounts.Remove(
                        tag);
                }
            }
        }

        private static bool HasAnyAbilityTag(
            IReadOnlyList<GameplayTag> abilityTags,
            IReadOnlyList<GameplayTag> tagsToMatch)
        {
            for (
                int abilityTagIndex = 0;
                abilityTagIndex < abilityTags.Count;
                abilityTagIndex++)
            {
                GameplayTag abilityTag =
                    abilityTags[abilityTagIndex];

                for (
                    int matchIndex = 0;
                    matchIndex < tagsToMatch.Count;
                    matchIndex++)
                {
                    if (
                        abilityTag ==
                        tagsToMatch[matchIndex])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Notifies listeners that a gameplay ability has entered its active state.
        /// </summary>
        public virtual void NotifyAbilityActivated(
            GameplayAbility gameplayAbility,
            string activationGUID)
        {
            if (!invokeEventsGA)
            {
                return;
            }

            OnGameplayAbilityActivated?.Invoke(
                gameplayAbility,
                activationGUID);
        }

        /// <summary>
        /// Initializes and invokes an ability after its activation checks succeed.
        /// </summary>
        public void CallActivateAbility(
            GameplayAbility gameplayAbility,
            string activationGUID)
        {
            if (gameplayAbility == null)
            {
                throw new ArgumentNullException(
                    nameof(gameplayAbility));
            }

            gameplayAbility.PreActivate(
                this,
                activationGUID);

            NotifyAbilityActivated(
                gameplayAbility,
                activationGUID);

            gameplayAbility.ActivateAbility(
                this,
                activationGUID);
        }

        /// <summary>
        /// Attempts to activate an ability specification and reports whether activation succeeded.
        /// </summary>
        public UniTask<bool> TryActivateAbility(
            GameplayAbilitySpecHandle handle,
            string activationGUID = null)
        {
            return InternalTryActivateAbility(
                handle,
                activationGUID);
        }

        /// <summary>
        /// Performs local validation and reports whether the requested ability entered its active state.
        /// </summary>
        private async UniTask<bool> InternalTryActivateAbility(
            GameplayAbilitySpecHandle handle,
            string activationGUID = null)
        {
            GameplayAbilitySpec abilitySpec = FindAbilitySpecFromHandle(
                handle);

            if (abilitySpec == null)
            {
                Debug.LogWarning(
                    $"No granted ability specification with handle '{handle}'.",
                    this);

                return false;
            }

            GameplayAbility gameplayAbility = abilitySpec.PrimaryInstance;

            if (
                string.IsNullOrEmpty(
                    activationGUID))
            {
                activationGUID = Guid.NewGuid().ToString();
            }

            gameplayAbility.source = this;
            gameplayAbility.ActivationGUID = activationGUID;

            OnGameplayAbilityTryActivate?.Invoke(
                gameplayAbility,
                activationGUID);

            await InputBuffering(
                gameplayAbility,
                gameplayAbility.ActivationGUID);

            if (
                !gameplayAbility.CanActivateAbility(
                    this,
                    gameplayAbility.ActivationGUID,
                    true))
            {
                return false;
            }

            CallActivateAbility(
                gameplayAbility,
                gameplayAbility.ActivationGUID);

            return true;
        }

        /// <summary>
        /// Waits briefly for an ability to satisfy its activation requirements.
        /// </summary>
        public async UniTask InputBuffering(
            GameplayAbility gameplayAbility,
            string activationGUID = null)
        {
            float finalTime =
                Time.realtimeSinceStartup +
                inputBufferDurationSeconds;

            while (
                !gameplayAbility.IsActive &&
                Time.realtimeSinceStartup < finalTime &&
                !gameplayAbility.CanActivateAbility(
                    this,
                    activationGUID,
                    false))
            {
                await UniTask.Delay(
                    10,
                    DelayType.Realtime);
            }
        }

        /// <summary>
        /// Modifies an active gameplay effect start time by the requested offset.
        /// </summary>
        public void ModifyActiveEffectStartTime(
            ActiveGameplayEffectHandle handle,
            float startTimeDiff)
        {
            ActiveGameplayEffects.ModifyActiveEffectStartTime(
                handle,
                startTimeDiff);
        }

        /// <summary>
        /// Returns the active gameplay effect identified by its local handle.
        /// </summary>
        public ActiveGameplayEffect GetActiveGameplayEffect(
            ActiveGameplayEffectHandle handle)
        {
            ActiveGameplayEffects.TryGetActiveGameplayEffect(
                handle,
                out ActiveGameplayEffect activeEffect);

            return activeEffect;
        }

        /// <summary>
        /// Executes one authoritative periodic tick for the specified active gameplay effect.
        /// </summary>
        public void ExecutePeriodicEffect(
            ActiveGameplayEffectHandle handle)
        {
            ActiveGameplayEffects.ExecutePeriodicGameplayEffect(
                handle);
        }

        /// <summary>
        /// Removes the active gameplay effect identified by its local handle.
        /// </summary>
        public bool RemoveActiveGameplayEffect(
            ActiveGameplayEffectHandle handle)
        {
            if (
                !ActiveGameplayEffects.RemoveActiveGameplayEffect(
                    handle))
            {
                return false;
            }

            if (
                m_LegacyActiveEffectsByHandle.TryGetValue(
                    handle,
                    out GameplayEffect runtimeEffect))
            {
                m_LegacyActiveEffectsByHandle.Remove(
                    handle);

                AppliedGameplayEffects.Remove(
                    runtimeEffect);

                if (invokeEventsGE)
                {
                    OnGameplayEffectRemoved?.Invoke(
                        runtimeEffect);
                }
            }

            return true;
        }

        /// <summary>
        /// Creates gameplay effect context initialized with this component's owner and avatar.
        /// </summary>
        public GameplayEffectContextHandle MakeEffectContext()
        {
            return new GameplayEffectContextHandle(
                new GameplayEffectContext(
                    AbilityActorInfo));
        }

        /// <summary>
        /// Creates outgoing runtime effect data using an explicit gameplay effect context.
        /// </summary>
        public GameplayEffectSpec MakeOutgoingSpec(
            GameplayEffect definition,
            float level,
            GameplayEffectContextHandle effectContext,
            string applicationGuid = null)
        {
            return new GameplayEffectSpec(
                definition,
                effectContext,
                level,
                applicationGuid);
        }

        /// <summary>
        /// Creates outgoing asset-based effect data using an explicit gameplay effect context.
        /// </summary>
        public GameplayEffectSpec MakeOutgoingSpec(
            GameplayEffectSO definitionAsset,
            float level,
            GameplayEffectContextHandle effectContext,
            string applicationGuid = null)
        {
            return new GameplayEffectSpec(
                definitionAsset,
                effectContext,
                level,
                applicationGuid);
        }

        /// <summary>
        /// Registers authoritative active state with its prediction identity and legacy notifications.
        /// </summary>
        private ActiveGameplayEffectHandle ApplyAuthoritativeActiveGameplayEffect(
            GameplayEffectSpec applicationSpec,
            GameplayEffect runtimeEffect,
            double startWorldTime,
            double startServerWorldTime,
            PredictionKey predictionKey = default)
        {
            ActiveGameplayEffect activeEffect =
                ActiveGameplayEffects.RegisterAuthoritative(
                    applicationSpec,
                    predictionKey,
                    startWorldTime,
                    startServerWorldTime);

            try
            {
                m_LegacyActiveEffectsByHandle.Add(
                    activeEffect.Handle,
                    runtimeEffect);

                AppliedGameplayEffects.Add(
                    runtimeEffect);

                return activeEffect.Handle;
            }
            catch
            {
                m_LegacyActiveEffectsByHandle.Remove(
                    activeEffect.Handle);

                AppliedGameplayEffects.Remove(
                    runtimeEffect);

                ActiveGameplayEffects.RemoveActiveGameplayEffect(
                    activeEffect.Handle);

                throw;
            }
        }

        /// <summary>
        /// Registers predicted active state with rollback ownership for its prediction key.
        /// </summary>
        private ActiveGameplayEffectHandle ApplyPredictedActiveGameplayEffect(
            GameplayEffectSpec applicationSpec,
            PredictionKey predictionKey,
            double startWorldTime,
            double startServerWorldTime)
        {
            ActiveGameplayEffect activeEffect =
                ActiveGameplayEffects.RegisterPredicted(
                    applicationSpec,
                    predictionKey,
                    startWorldTime,
                    startServerWorldTime);

            return activeEffect.Handle;
        }

        /// <summary>
        /// Registers active gameplay effect state according to its execution authority.
        /// </summary>
        private ActiveGameplayEffectHandle ApplyActiveGameplayEffect(
            GameplayEffectSpec applicationSpec,
            GameplayEffect runtimeEffect,
            PredictionKey predictionKey,
            ActiveEffectAuthority authority,
            double startWorldTime,
            double startServerWorldTime)
        {
            return authority switch
            {
                ActiveEffectAuthority.Predicted =>
                    ApplyPredictedActiveGameplayEffect(
                        applicationSpec,
                        predictionKey,
                        startWorldTime,
                        startServerWorldTime),
                ActiveEffectAuthority.Authoritative =>
                    ApplyAuthoritativeActiveGameplayEffect(
                        applicationSpec,
                        runtimeEffect,
                        startWorldTime,
                        startServerWorldTime,
                        predictionKey),
                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(authority),
                        authority,
                        "Unsupported active gameplay effect authority."),
            };
        }

        /// <summary>
        /// Applies a prepared gameplay effect specification with its prediction identity.
        /// </summary>
        public ActiveGameplayEffectHandle ApplyGameplayEffectSpecToSelf(
            GameplayEffectSpec spec,
            PredictionKey predictionKey = default)
        {
            bool isAuthoritative =
                IsOwnerActorAuthoritative();

            if (
                !isAuthoritative &&
                !predictionKey.IsValid)
            {
                return default;
            }

            ActiveEffectAuthority authority =
                isAuthoritative
                    ? ActiveEffectAuthority.Authoritative
                    : ActiveEffectAuthority.Predicted;

            return ApplyGameplayEffectSpecToSelf(
                spec,
                predictionKey,
                authority);
        }

        /// <summary>
        /// Applies a prepared gameplay effect specification using explicit execution authority.
        /// </summary>
        private ActiveGameplayEffectHandle ApplyGameplayEffectSpecToSelf(
            GameplayEffectSpec spec,
            PredictionKey predictionKey,
            ActiveEffectAuthority authority)
        {
            if (spec == null)
            {
                throw new ArgumentNullException(
                    nameof(spec));
            }

            GameplayEffect definition =
                spec.Definition;

            if (
                authority == ActiveEffectAuthority.Predicted &&
                spec.IsPeriodic)
            {
                return default;
            }

            // if (logging)
            // {
            //     Debug.Log(
            //         $"ASC ApplyGameplayEffectSpecToSelf " +
            //         $"{definition.name} {name} " +
            //         $"applicationGuid: {spec.ApplicationGuid} " +
            //         $"data: " +
            //         $"{JsonUtility.ToJson(definition, true)}");
            // }

            if (
                !TagProcessor.CheckApplicationTagRequirementsGE(
                    this,
                    definition,
                    tags))
            {
                // if (logging)
                // {
                //     Debug.Log(
                //         $"GE: {definition.name} " +
                //         "couldnt be applied on this ASC. " +
                //         "Failed application tag requirements");
                // }

                return default;
            }

            if (
                definition.chanceToApply < 1f &&
                UnityEngine.Random.Range(0f, 1f) >
                definition.chanceToApply)
            {
                return default;
            }

            GameplayEffectSpec applicationSpec =
                new(spec);

            applicationSpec.CaptureAttributeDataFromTarget(
                this);

            applicationSpec.CalculateModifierMagnitudes();

            GameplayEffect runtimeEffect =
                definition.Instantiate();

            runtimeEffect.source =
                applicationSpec.Source;

            runtimeEffect.target =
                this;

            runtimeEffect.level =
                applicationSpec.Level;

            runtimeEffect.applicationGUID =
                applicationSpec.ApplicationGuid;

            ActiveGameplayEffectHandle result;

            switch (definition.durationType)
            {
                case GameplayEffectDurationType.Instant:
                    {
                        if (authority == ActiveEffectAuthority.Authoritative)
                        {
                            ExecuteGameplayEffect(
                                applicationSpec,
                                runtimeEffect);

                            result =
                                ActiveGameplayEffectHandle
                                    .GetInstantExecutedHandle();

                            break;
                        }

                        double startTime =
                            Time.timeAsDouble;

                        result =
                            ApplyActiveGameplayEffect(
                                applicationSpec,
                                runtimeEffect,
                                predictionKey,
                                authority,
                                startTime,
                                startTime);

                        break;
                    }

                case GameplayEffectDurationType.Infinite:
                    {
                        double startTime =
                            Time.timeAsDouble;

                        result =
                            ApplyActiveGameplayEffect(
                                applicationSpec,
                                runtimeEffect,
                                predictionKey,
                                authority,
                                startTime,
                                startTime);

                        break;
                    }

                case GameplayEffectDurationType.Duration:
                    {
                        if (applicationSpec.Duration <= 0f)
                        {
                            throw new InvalidOperationException(
                                "A duration gameplay effect requires a positive duration.");
                        }

                        double startTime =
                            Time.timeAsDouble;

                        result =
                            ApplyActiveGameplayEffect(
                                applicationSpec,
                                runtimeEffect,
                                predictionKey,
                                authority,
                                startTime,
                                startTime);

                        ActiveGameplayEffects.CheckDuration(
                            result);

                        break;
                    }

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(definition.durationType),
                        definition.durationType,
                        "Unsupported gameplay effect duration type.");
            }

            if (
                authority ==
                ActiveEffectAuthority.Authoritative &&
                applicationSpec.IsPeriodic)
            {
                if (
                    applicationSpec
                        .Definition
                        .ExecutePeriodicEffectOnApplication)
                {
                    ExecutePeriodicEffect(
                        result);
                }

                ActiveGameplayEffects.StartPeriodicGameplayEffect(
                    result);
            }

            if (invokeEventsGE)
            {
                OnGameplayEffectApplied?.Invoke(
                    runtimeEffect);
            }

            return result;
        }

        /// <summary>
        /// Applies a gameplay effect through the specification pipeline.
        /// </summary>
        public ActiveGameplayEffectHandle ApplyGameplayEffect(
            AbilitySystemComponent source,
            AbilitySystemComponent target,
            GameplayEffect gameplayEffect,
            string applicationGUID = null)
        {
            GameplayEffectContextHandle effectContext =
                source.MakeEffectContext();

            GameplayEffectSpec spec = source.MakeOutgoingSpec(
                gameplayEffect,
                gameplayEffect.level,
                effectContext,
                applicationGUID);

            return target.ApplyGameplayEffectSpecToSelf(
                spec);
        }

        /// <summary>
        /// Applies a gameplay effect definition asset through the specification pipeline.
        /// </summary>
        public ActiveGameplayEffectHandle ApplyGameplayEffect(
            AbilitySystemComponent source,
            AbilitySystemComponent target,
            GameplayEffectSO definitionAsset,
            float level,
            string applicationGUID = null)
        {
            GameplayEffectContextHandle effectContext =
                source.MakeEffectContext();

            GameplayEffectSpec spec = source.MakeOutgoingSpec(
                definitionAsset,
                level,
                effectContext,
                applicationGUID);

            return target.ApplyGameplayEffectSpecToSelf(
                spec);
        }

        /// <summary>
        /// Executes evaluated gameplay effect modifiers against authoritative attribute base values.
        /// </summary>
        internal void ExecuteGameplayEffect(
            GameplayEffectSpec applicationSpec,
            GameplayEffect runtimeEffect = null)
        {
            foreach (
                AttributeModifierSpec modifierSpec
                in applicationSpec.ModifierSpecs)
            {
                if (!modifierSpec.HasEvaluatedMagnitude)
                {
                    throw new InvalidOperationException(
                        "An instant effect requires evaluated modifier magnitudes.");
                }

                Attribute targetAttribute =
                    GetAttribute(
                        modifierSpec.Definition.Attribute);

                float oldValue =
                    targetAttribute.CurrentValue;

                float newBaseValue =
                    AttributeModifierAggregator
                        .ExecuteModifierOnBaseValue(
                            targetAttribute.BaseValue,
                            modifierSpec.Definition.Operation,
                            modifierSpec.EvaluatedMagnitude);

                foreach (
                    AttributeProcessor processor
                    in attributesProcessors)
                {
                    processor.PreAttributeBaseChange(
                        targetAttribute,
                        ref newBaseValue,
                        this);
                }

                targetAttribute.SetBaseValue(
                    newBaseValue);

                GameplayModifierEvaluatedData evaluatedData =
                    new(
                        modifierSpec.Definition.Attribute,
                        modifierSpec.Definition.Operation,
                        modifierSpec.EvaluatedMagnitude,
                        default);

                GameplayEffectModCallbackData callbackData =
                    new(
                        applicationSpec,
                        evaluatedData,
                        this);

                foreach (
                    AttributeProcessor processor
                    in attributesProcessors)
                {
                    processor.PostGameplayEffectExecute(
                        callbackData);
                }

                float newValue =
                    targetAttribute.CurrentValue;

                if (
                    !Mathf.Approximately(
                        oldValue,
                        newValue))
                {
                    targetAttribute.OnPostAttributeChange?.Invoke(
                        targetAttribute.attributeName,
                        oldValue,
                        newValue,
                        runtimeEffect);
                }
            }
        }
    }
}