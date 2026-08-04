using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace GAS
{
    [Serializable]
    public class GameplayAbility
    {
        [ReadOnly] public string name;

        [SerializeField]
        protected GameplayEffectSO m_CooldownGameplayEffect;

        [SerializeField]
        protected GameplayEffectSO m_CostGameplayEffect;

        [NonSerialized]
        private readonly GameplayTagContainer m_TempCooldownTags = new();

        [NonSerialized]
        private readonly List<AbilityTask> m_ActiveTasks = new();

        /// <summary>
        /// Returns the gameplay effect definition used to apply this ability's cooldown.
        /// </summary>
        public virtual GameplayEffectSO GetCooldownGameplayEffect()
        {
            return m_CooldownGameplayEffect;
        }

        /// <summary>
        /// Returns the gameplay tags used to identify this ability's active cooldown.
        /// </summary>
        public virtual GameplayTagContainer GetCooldownTags()
        {
            GameplayEffectSO cooldownGameplayEffect =
                GetCooldownGameplayEffect();

            if (
                cooldownGameplayEffect == null ||
                cooldownGameplayEffect.ge == null ||
                cooldownGameplayEffect.ge.gameplayEffectTags == null)
            {
                return null;
            }

            m_TempCooldownTags.Reset();

            IReadOnlyList<GameplayTag> grantedTags =
                cooldownGameplayEffect.ge.gameplayEffectTags.GrantedTags;

            for (
                int index = 0;
                index < grantedTags.Count;
                index++)
            {
                m_TempCooldownTags.AddTag(
                    grantedTags[index]);
            }

            return m_TempCooldownTags;
        }

        /// <summary>
        /// Returns the longest remaining time among this ability's active cooldown effects.
        /// </summary>
        public virtual float GetCooldownTimeRemaining(
            GameplayAbilityActorInfo actorInfo)
        {
            if (actorInfo == null)
            {
                throw new ArgumentNullException(
                    nameof(actorInfo));
            }

            GameplayTagContainer cooldownTags =
                GetCooldownTags();

            if (
                cooldownTags == null ||
                cooldownTags.IsEmpty())
            {
                return 0f;
            }

            GameplayEffectQuery query =
                GameplayEffectQuery.MakeQuery_MatchAnyOwningTags(
                    cooldownTags);

            List<float> timesRemaining =
                actorInfo
                    .AbilitySystemComponent
                    .GetActiveEffectsTimeRemaining(
                        query);

            float longestTimeRemaining = 0f;

            for (
                int index = 0;
                index < timesRemaining.Count;
                index++)
            {
                longestTimeRemaining = Math.Max(
                    longestTimeRemaining,
                    timesRemaining[index]);
            }

            return longestTimeRemaining;
        }

        /// <summary>
        /// Returns the gameplay effect definition used to apply this ability's cost.
        /// </summary>
        public virtual GameplayEffectSO GetCostGameplayEffect()
        {
            return m_CostGameplayEffect;
        }

        public GameplayAbilitySpecHandle CurrentSpecHandle
        {
            get; internal set;
        }

        public GameplayAbilityActorInfo CurrentActorInfo
        {
            get; internal set;
        }

        public GameplayAbilityActivationInfo CurrentActivationInfo
        {
            get; internal set;
        }

        /// <summary>
        /// Returns the level currently assigned to this instantiated ability.
        /// </summary>
        public int GetAbilityLevel()
        {
            return Level;
        }

        /// <summary>
        /// Returns the level stored by the requested gameplay ability specification.
        /// </summary>
        public int GetAbilityLevel(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActorInfo actorInfo)
        {
            if (actorInfo == null)
            {
                return 1;
            }

            GameplayAbilitySpec abilitySpec =
                actorInfo
                    .AbilitySystemComponent
                    .FindAbilitySpecFromHandle(
                        handle);

            return
                abilitySpec != null
                    ? abilitySpec.Level
                    : 1;
        }

        /// <summary>
        /// Returns whether this ability is executing on a predicting client.
        /// </summary>
        public bool IsPredictingClient()
        {
            return
                CurrentActivationInfo.ActivationMode ==
                GameplayAbilityActivationMode.Predicting;
        }

        /// <summary>
        /// Creates an extensible gameplay effect context for this ability execution.
        /// </summary>
        public virtual GameplayEffectContextHandle MakeEffectContext(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActorInfo actorInfo)
        {
            if (!handle.IsValid)
            {
                throw new ArgumentException(
                    "Gameplay effect context requires a valid ability specification handle.",
                    nameof(handle));
            }

            if (actorInfo == null)
            {
                throw new ArgumentNullException(
                    nameof(actorInfo));
            }

            GameplayEffectContextHandle effectContext =
                actorInfo.AbilitySystemComponent.MakeEffectContext();

            effectContext.SetAbility(
                this);

            effectContext.AddSourceObject(
                GetSourceObject(
                    handle,
                    actorInfo));

            return effectContext;
        }

        /// <summary>
        /// Returns the source object associated with the requested ability specification.
        /// </summary>
        public UnityEngine.Object GetSourceObject(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActorInfo actorInfo)
        {
            GameplayAbilitySpec abilitySpec =
                actorInfo
                    .AbilitySystemComponent
                    .FindAbilitySpecFromHandle(
                        handle);

            if (abilitySpec == null)
            {
                return null;
            }

            return abilitySpec.SourceObject;
        }

        public GameplayAbilitySO DefinitionAsset
        {
            get; private set;
        }

        public List<GameplayEffectSO> effectsSO = new();

        [ReadOnly]
        public List<GameplayEffect> effects = new();


        [SerializeReference]
        public AbilityTags abilityTags = new();

        public AbilitySystemComponent source, owner;

        [SerializeReference]
        public List<GameplayTag> cuesTags = new();

        [ReadOnly] public string Guid;
        [ReadOnly] public string ClassName;

        public int Level = 1;
        public bool IsActive;


        /// <summary>
        /// Creates a runtime ability instance with independent gameplay effect state.
        /// </summary>
        public virtual GameplayAbility Instantiate(
            AbilitySystemComponent owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(
                    nameof(owner));
            }

            Type classType =
                GetType();

            GameplayAbility gaCopy =
                (GameplayAbility)Activator.CreateInstance(
                    classType);

            gaCopy.owner =
                owner;

            gaCopy.Guid =
                System.Guid.NewGuid().ToString();

            gaCopy.ClassName =
                GetType().FullName;

            gaCopy.effectsSO =
                new List<GameplayEffectSO>(
                    effectsSO);

            gaCopy.effects =
                effectsSO.Count > 0
                    ? effectsSO
                        .Select(
                            effectAsset =>
                                effectAsset.ge.Instantiate())
                        .ToList()
                    : effects
                        .Select(
                            effect =>
                                effect.Instantiate())
                        .ToList();

            gaCopy.m_CooldownGameplayEffect =
                m_CooldownGameplayEffect;

            gaCopy.m_CostGameplayEffect =
                m_CostGameplayEffect;

            gaCopy.name =
                name;

            gaCopy.abilityTags =
                abilityTags;

            gaCopy.Level =
                Level;

            gaCopy.IsActive =
                false;

            gaCopy.cuesTags =
                cuesTags;

            if (!abilityTags.initialized)
            {
                gaCopy.abilityTags.FillTags(
                    gaCopy);

                gaCopy.abilityTags.ClearStrings();
            }

            return gaCopy;
        }

        /// <summary>
        /// Creates a runtime ability instance associated with its persistent definition asset.
        /// </summary>
        internal GameplayAbility Instantiate(
            AbilitySystemComponent owner,
            GameplayAbilitySO definitionAsset)
        {
            if (definitionAsset == null)
            {
                throw new ArgumentNullException(
                    nameof(definitionAsset));
            }

            GameplayAbility ability =
                Instantiate(
                    owner);

            ability.DefinitionAsset =
                definitionAsset;

            return ability;
        }

        /// <summary>
        /// Additional network serialization for inherited classes
        /// </summary>
        public virtual void SerializeAdditionalData()
        {
        }
        /// <summary>
        /// Additional network serialization for inherited classes
        /// </summary>
        public virtual void DeserializeAdditionalData()
        {
        }

        /// <summary>
        /// Executes the pre-activation and activation stages for this gameplay ability.
        /// </summary>
        public void CallActivateAbility(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo,
            Action<GameplayAbility> onGameplayAbilityEndedDelegate,
            GameplayEventData? triggerEventData)
        {
            if (!handle.IsValid)
            {
                throw new ArgumentException(
                    "Gameplay ability activation requires a valid specification handle.",
                    nameof(handle));
            }

            if (actorInfo == null)
            {
                throw new ArgumentNullException(
                    nameof(actorInfo));
            }

            AbilitySystemComponent abilitySystemComponent =
                actorInfo.AbilitySystemComponent;

            if (onGameplayAbilityEndedDelegate != null)
            {
                IDisposable abilityEndedSubscription = null;

                void HandleGameplayAbilityEnded(
                    AbilityEndedData abilityEndedData)
                {
                    if (
                        !ReferenceEquals(
                            abilityEndedData.AbilityThatEnded,
                            this))
                    {
                        return;
                    }

                    abilityEndedSubscription.Dispose();

                    onGameplayAbilityEndedDelegate(
                        this);
                }

                abilityEndedSubscription =
                    abilitySystemComponent.RegisterAbilityEnded(
                        HandleGameplayAbilityEnded);
            }

            PreActivate(
                handle,
                actorInfo,
                activationInfo);

            abilitySystemComponent.NotifyAbilityActivated(
                handle,
                this);

            ActivateAbility(
                handle,
                actorInfo,
                activationInfo,
                triggerEventData);
        }

        /// <summary>
        /// Initializes runtime state and applies activation-owned and blocking tags.
        /// </summary>
        public virtual void PreActivate(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo)
        {
            if (!handle.IsValid)
            {
                throw new ArgumentException(
                    "Gameplay ability activation requires a valid specification handle.",
                    nameof(handle));
            }

            if (actorInfo == null)
            {
                throw new ArgumentNullException(
                    nameof(actorInfo));
            }

            CurrentSpecHandle =
                handle;

            CurrentActorInfo =
                actorInfo;

            CurrentActivationInfo =
                activationInfo;

            IsActive =
                true;

            source =
                actorInfo.AbilitySystemComponent;

            source.ApplyAbilityBlockAndCancelTags(
                abilityTags.DescriptionTags,
                this,
                true,
                abilityTags.BlockAbilitiesWithTags,
                true,
                abilityTags.CancelAbilitiesWithTags);

            source.UpdateTagMap(
                abilityTags.ActivationOwnedTags,
                1);
        }

        /// <summary>
        /// Executes ability-specific behavior using the supplied activation context.
        /// </summary>
        protected virtual void ActivateAbility(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo,
            GameplayEventData? triggerEventData)
        {
        }

        /// <summary>
        /// Applies a prepared gameplay effect specification to the owner of this ability.
        /// </summary>
        protected ActiveGameplayEffectHandle ApplyGameplayEffectSpecToOwner(
            GameplayAbilitySpecHandle abilityHandle,
            GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo,
            GameplayEffectSpec spec)
        {
            PredictionKey predictionKey =
                activationInfo.GetActivationPredictionKey();

            return actorInfo.AbilitySystemComponent.ApplyGameplayEffectSpecToSelf(
                spec,
                predictionKey);
        }

        /// <summary>
        /// Applies a prepared gameplay effect specification through polymorphic target data.
        /// </summary>
        protected IReadOnlyList<ActiveGameplayEffectHandle>
            ApplyGameplayEffectSpecToTarget(
                GameplayAbilityActivationInfo activationInfo,
                GameplayEffectSpec spec,
                GameplayAbilityTargetDataHandle targetData)
        {
            if (targetData == null)
            {
                throw new ArgumentNullException(
                    nameof(targetData));
            }

            PredictionKey predictionKey =
                activationInfo.GetActivationPredictionKey();

            List<ActiveGameplayEffectHandle> appliedEffectHandles =
                new();

            for (
                int index = 0;
                index < targetData.Num();
                index++)
            {
                GameplayAbilityTargetData targetingPayload =
                    targetData.Get(
                        index);

                if (targetingPayload == null)
                {
                    continue;
                }

                appliedEffectHandles.AddRange(
                    targetingPayload.ApplyGameplayEffectSpec(
                        spec,
                        predictionKey));
            }

            return appliedEffectHandles;
        }

        /// <summary>
        /// Creates and applies a gameplay effect specification to the owner of this ability.
        /// </summary>
        protected ActiveGameplayEffectHandle
            ApplyGameplayEffectToOwner(
                AbilitySystemComponent ownerAbilitySystem,
                GameplayAbilityActivationInfo activationInfo,
                GameplayEffectSO gameplayEffect,
                float gameplayEffectLevel,
                string applicationGuid = null)
        {
            GameplayEffectContextHandle effectContext = MakeEffectContext(
                CurrentSpecHandle,
                ownerAbilitySystem.AbilityActorInfo);

            GameplayEffectSpec spec = ownerAbilitySystem.MakeOutgoingSpec(
                gameplayEffect,
                gameplayEffectLevel,
                effectContext,
                applicationGuid);

            return ApplyGameplayEffectSpecToOwner(
                CurrentSpecHandle,
                ownerAbilitySystem.AbilityActorInfo,
                activationInfo,
                spec);
        }

        /// <summary>
        /// Creates and applies a gameplay effect specification through polymorphic target data.
        /// </summary>
        protected IReadOnlyList<ActiveGameplayEffectHandle>
            ApplyGameplayEffectToTarget(
                AbilitySystemComponent sourceAbilitySystem,
                GameplayAbilityActivationInfo activationInfo,
                GameplayAbilityTargetDataHandle targetData,
                GameplayEffectSO gameplayEffect,
                float gameplayEffectLevel,
                string applicationGuid = null)
        {
            GameplayEffectContextHandle effectContext = MakeEffectContext(
                CurrentSpecHandle,
                sourceAbilitySystem.AbilityActorInfo);

            GameplayEffectSpec spec = sourceAbilitySystem.MakeOutgoingSpec(
                gameplayEffect,
                gameplayEffectLevel,
                effectContext,
                applicationGuid);

            return ApplyGameplayEffectSpecToTarget(
                activationInfo,
                spec,
                targetData);
        }

        /// <summary>
        /// Applies every configured gameplay effect through prepared target data.
        /// </summary>
        protected void ApplyGameplayEffects(
            AbilitySystemComponent source,
            GameplayAbilityActivationInfo activationInfo,
            GameplayAbilityTargetDataHandle targetData)
        {
            for (
                int index = 0;
                index < effectsSO.Count;
                index++)
            {
                ApplyGameplayEffectToTarget(
                    source,
                    activationInfo,
                    targetData,
                    effectsSO[index],
                    Level);
            }
        }

        /// <summary>
        /// Performs the final commit check and applies the ability cost and cooldown.
        /// </summary>
        public virtual bool CommitAbility(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo,
            GameplayTagContainer optionalRelevantTags = null)
        {
            if (
                !CommitCheck(
                    handle,
                    actorInfo,
                    activationInfo,
                    optionalRelevantTags))
            {
                return false;
            }

            CommitExecute(
                handle,
                actorInfo,
                activationInfo);

            return true;
        }

        /// <summary>
        /// Performs the final cost and cooldown checks before committing the ability.
        /// </summary>
        public virtual bool CommitCheck(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo,
            GameplayTagContainer optionalRelevantTags = null)
        {
            return
                CheckCooldown(
                    handle,
                    actorInfo,
                    optionalRelevantTags) &&
                CheckCost(
                    handle,
                    actorInfo,
                    optionalRelevantTags);
        }

        /// <summary>
        /// Applies the ability cooldown and cost after a successful commit check.
        /// </summary>
        public virtual void CommitExecute(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo)
        {
            ApplyCooldown(
                handle,
                actorInfo,
                activationInfo);

            ApplyCost(
                handle,
                actorInfo,
                activationInfo);
        }

        /// <summary>
        /// Checks whether the ability owner has any tag that places this ability on cooldown.
        /// </summary>
        public virtual bool CheckCooldown(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActorInfo actorInfo,
            GameplayTagContainer optionalRelevantTags = null)
        {
            GameplayTagContainer cooldownTags =
                GetCooldownTags();

            if (
                cooldownTags == null ||
                cooldownTags.IsEmpty())
            {
                return true;
            }

            AbilitySystemComponent abilitySystem =
                actorInfo.AbilitySystemComponent;

            if (
                !abilitySystem.HasAnyMatchingGameplayTags(
                    cooldownTags))
            {
                return true;
            }

            if (optionalRelevantTags != null)
            {
                optionalRelevantTags.AddTag(
                    AbilitySystemGlobals.ActivateFailCooldownTag);
            }

            return false;
        }

        /// <summary>
        /// Applies the configured cooldown gameplay effect to the ability owner.
        /// </summary>
        public virtual void ApplyCooldown(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo)
        {
            GameplayEffectSO cooldownGameplayEffect =
                GetCooldownGameplayEffect();

            if (
                cooldownGameplayEffect == null ||
                cooldownGameplayEffect.ge == null)
            {
                return;
            }

            AbilitySystemComponent abilitySystem =
                actorInfo.AbilitySystemComponent;

            GameplayEffectContextHandle effectContext = MakeEffectContext(
                handle,
                actorInfo);

            int abilityLevel =
                GetAbilityLevel(
                    handle,
                    actorInfo);

            GameplayEffectSpec cooldownSpec = abilitySystem.MakeOutgoingSpec(
                cooldownGameplayEffect,
                abilityLevel,
                effectContext);

            ApplyGameplayEffectSpecToOwner(
                handle,
                actorInfo,
                activationInfo,
                cooldownSpec);
        }

        /// <summary>
        /// Applies the configured cost gameplay effect to the ability owner.
        /// </summary>
        public virtual void ApplyCost(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo)
        {
            GameplayEffectSO costGameplayEffect =
                GetCostGameplayEffect();

            if (
                costGameplayEffect == null ||
                costGameplayEffect.ge == null ||
                !costGameplayEffect.ge.HasModifiers)
            {
                return;
            }

            AbilitySystemComponent abilitySystem =
                actorInfo.AbilitySystemComponent;

            GameplayEffectContextHandle effectContext = MakeEffectContext(
                handle,
                actorInfo);

            int abilityLevel =
                GetAbilityLevel(
                    handle,
                    actorInfo);

            GameplayEffectSpec costSpec = abilitySystem.MakeOutgoingSpec(
                costGameplayEffect,
                abilityLevel,
                effectContext);

            ApplyGameplayEffectSpecToOwner(
                handle,
                actorInfo,
                activationInfo,
                costSpec);
        }

        /// <summary>
        /// Checks and applies only the configured ability cost.
        /// </summary>
        public virtual bool CommitAbilityCost(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo,
            GameplayTagContainer optionalRelevantTags = null)
        {
            if (
                !CheckCost(
                    handle,
                    actorInfo,
                    optionalRelevantTags))
            {
                return false;
            }

            ApplyCost(
                handle,
                actorInfo,
                activationInfo);

            return true;
        }

        /// <summary>
        /// Checks and applies only the configured ability cooldown.
        /// </summary>
        public virtual bool CommitAbilityCooldown(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo,
            bool forceCooldown,
            GameplayTagContainer optionalRelevantTags = null)
        {
            if (
                !forceCooldown &&
                !CheckCooldown(
                    handle,
                    actorInfo,
                    optionalRelevantTags))
            {
                return false;
            }

            ApplyCooldown(
                handle,
                actorInfo,
                activationInfo);

            return true;
        }

        /// <summary>
        /// Registers a gameplay task that has entered its active state.
        /// </summary>
        internal void OnGameplayTaskActivated(
            AbilityTask task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(
                    nameof(task));
            }

            if (!m_ActiveTasks.Contains(
                    task))
            {
                m_ActiveTasks.Add(
                    task);
            }
        }

        /// <summary>
        /// Unregisters a gameplay task that has left its active state.
        /// </summary>
        internal void OnGameplayTaskDeactivated(
            AbilityTask task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(
                    nameof(task));
            }

            m_ActiveTasks.Remove(
                task);
        }

        /// <summary>
        /// Ends every active gameplay task owned by this ability.
        /// </summary>
        private void EndAbilityTasks()
        {
            while (m_ActiveTasks.Count > 0)
            {
                AbilityTask task =
                    m_ActiveTasks[m_ActiveTasks.Count - 1];

                task.TaskOwnerEnded();
            }
        }

        /// <summary>
        /// Cancels every active task before ending this gameplay ability.
        /// </summary>
        public virtual void CancelAbility(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo,
            bool replicateCancelAbility)
        {
            if (!IsActive)
            {
                return;
            }

            AbilityTask[] activeTasks = m_ActiveTasks.ToArray();

            for (
                int index = 0;
                index < activeTasks.Length;
                index++)
            {
                if (!IsActive)
                {
                    return;
                }

                AbilityTask task = activeTasks[index];

                if (!task.IsEnded)
                {
                    task.ExternalCancel();
                }
            }

            EndAbility(
                handle,
                actorInfo,
                activationInfo,
                replicateCancelAbility,
                true);
        }

        /// <summary>
        /// Ends the active ability and releases all state owned by its activation.
        /// </summary>
        public virtual void EndAbility(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo,
            bool replicateEndAbility,
            bool wasCancelled)
        {
            if (!IsActive)
            {
                return;
            }

            AbilitySystemComponent abilitySystemComponent =
                actorInfo.AbilitySystemComponent;

            if (replicateEndAbility)
            {
                abilitySystemComponent.ReplicateEndOrCancelAbility(
                    handle,
                    activationInfo,
                    this,
                    wasCancelled);
            }

            EndAbilityTasks();

            abilitySystemComponent.ClearAbilityReplicatedDataCache(
                handle,
                activationInfo);

            abilitySystemComponent.ApplyAbilityBlockAndCancelTags(
                abilityTags.DescriptionTags,
                this,
                false,
                abilityTags.BlockAbilitiesWithTags,
                false,
                abilityTags.CancelAbilitiesWithTags);

            abilitySystemComponent.UpdateTagMap(
                abilityTags.ActivationOwnedTags,
                -1);

            IsActive =
                false;

            AbilityEndedData abilityEndedData =
                new AbilityEndedData(
                    this,
                    handle,
                    replicateEndAbility,
                    wasCancelled);

            abilitySystemComponent.BroadcastAbilityEnded(
                abilityEndedData);

            abilitySystemComponent.NotifyAbilityEnded(
                handle,
                this,
                wasCancelled);
        }
        
        /// <summary>
        /// Determines whether the supplied ability specification can activate for the current actor.
        /// </summary>
        public virtual bool CanActivateAbility(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActorInfo actorInfo,
            GameplayTagContainer sourceTags = null,
            GameplayTagContainer targetTags = null,
            GameplayTagContainer optionalRelevantTags = null)
        {
            AbilitySystemComponent abilitySystem =
                actorInfo.AbilitySystemComponent;

            if (IsActive)
            {
                if (abilitySystem.logging)
                {
                    Debug.Log(
                        $"{name} is already active.");
                }

                return false;
            }

            if (
                abilitySystem.AreAbilityTagsBlocked(
                    abilityTags.DescriptionTags))
            {
                if (abilitySystem.logging)
                {
                    Debug.Log(
                        $"{name} is blocked by ability tags.");
                }

                return false;
            }

            if (
                !CheckCooldown(
                    handle,
                    actorInfo,
                    optionalRelevantTags))
            {
                if (abilitySystem.logging)
                {
                    float cooldownRemaining =
                        GetCooldownTimeRemaining(
                            actorInfo);

                    Debug.Log(
                        $"{name} is on cooldown. " +
                        $"Time remaining: {cooldownRemaining}.");
                }

                return false;
            }

            return CheckCost(
                handle,
                actorInfo,
                optionalRelevantTags);
        }

        /// <summary>
        /// Checks whether the ability owner can afford the evaluated gameplay effect cost.
        /// </summary>
        public virtual bool CheckCost(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActorInfo actorInfo,
            GameplayTagContainer optionalRelevantTags = null)
        {
            GameplayEffectSO costGameplayEffect =
                GetCostGameplayEffect();

            if (
                costGameplayEffect == null ||
                costGameplayEffect.ge == null ||
                !costGameplayEffect.ge.HasModifiers)
            {
                return true;
            }

            AbilitySystemComponent abilitySystem =
                actorInfo.AbilitySystemComponent;

            GameplayEffectContextHandle effectContext = MakeEffectContext(
                handle,
                actorInfo);

            int abilityLevel =
                GetAbilityLevel(
                    handle,
                    actorInfo);

            GameplayEffectSpec costSpec = abilitySystem.MakeOutgoingSpec(
                costGameplayEffect,
                abilityLevel,
                effectContext);

            costSpec.CaptureAttributeDataFromTarget(
                abilitySystem);

            costSpec.CalculateModifierMagnitudes();

            foreach (
                AttributeModifierSpec modifierSpec
                in costSpec.ModifierSpecs)
            {
                if (!modifierSpec.HasEvaluatedMagnitude)
                {
                    throw new InvalidOperationException(
                        "Ability cost requires evaluated modifier magnitudes.");
                }

                if (
                    modifierSpec.Definition.Operation !=
                    AttributeModifierOperation.Additive)
                {
                    throw new InvalidOperationException(
                        "Default ability costs require additive attribute modifiers.");
                }

                float magnitude =
                    modifierSpec.EvaluatedMagnitude;

                if (magnitude >= 0f)
                {
                    continue;
                }

                AttributeName attributeName =
                    modifierSpec.Definition.Attribute;

                if (
                    !abilitySystem.AttributesDictionary.TryGetValue(
                        attributeName.name,
                        out Attribute attribute))
                {
                    if (abilitySystem.logging)
                    {
                        Debug.Log(
                            $"ASC does not contain cost attribute " +
                            $"'{attributeName}'.");
                    }

                    optionalRelevantTags?.AddTag(
                        AbilitySystemGlobals.ActivateFailCostTag);

                    return false;
                }

                float resultingValue =
                    attribute.CurrentValue +
                    magnitude;

                if (resultingValue >= 0f)
                {
                    continue;
                }

                if (abilitySystem.logging)
                {
                    Debug.Log(
                        $"Cannot pay ability cost: " +
                        $"{attributeName} has {attribute.CurrentValue}, " +
                        $"cost magnitude is {magnitude}.");
                }

                optionalRelevantTags?.AddTag(
                    AbilitySystemGlobals.ActivateFailCostTag);

                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Tags for Gameplay Abilities
    /// </summary>
    [Serializable]
    public class AbilityTags
    {//Block and Cancel and similar to ActivationIgnored, but instead of being from this GA it is related to other GAs
        /// <summary> While this Ability is active/executing, the owner of the Ability will be granted this set of Tags. ( ActivationOwnedTags) </summary>
        [Tooltip("While this Ability is active/executing, the owner of the Ability will be granted this set of Tags.")]
        [SerializeField] public List<GameplayTag> ActivationOwnedTags = new();

        /// <summary> Tags that describe the GameplayAbility. They do not do any function on their own and serve only the purpose of describing the GameplayEffect. </summary>
        [Tooltip("GameplayTags that the GameplayAbility owns. These are just GameplayTags to describe the GameplayAbility")]
        [SerializeField] public List<GameplayTag> DescriptionTags = new();

        /// <summary> Active Gameplay Abilities (on the same character) that have these tags will be cancelled.
        /// Cancels any already-executing Ability with Tags matching the list provided while this Ability is executing. </summary>
        [Tooltip("Active Gameplay Abilities (on the same character) that have these tags will be cancelled. Cancels any already-executing Ability with Tags matching the list provided while this Ability is executing")]
        [SerializeField] public List<GameplayTag> CancelAbilitiesWithTags = new();
        /// <summary> Gameplay Abilities that have these tags will be blocked from activating on the same character</summary>
        [Tooltip("Gameplay Abilities that have these tags will be blocked from activating on the same character. (blocking others)")]
        [SerializeField] public List<GameplayTag> BlockAbilitiesWithTags = new(); //Gameplay Abilities that have these tags will be blocked from activating on the same character

        /// <summary>If any of these tags IS NOT present on source ASC, this ability won't be activated.</summary>
        [Tooltip("If any of these tags IS NOT present on source ASC, this ability won't be activated.")]
        [SerializeField] public List<GameplayTag> SourceTagsRequired = new(); //The Ability can only be activated if the activating Component has all Required Tags and none of Ingnored Tags.
        /// <summary> If any of these tags IS present on source ASC, this ability won't be activated.</summary>
        [Tooltip("If any of these tags IS present on source ASC, this ability won't be activated. (self ignoring)")]
        [SerializeField] public List<GameplayTag> SourceTagsForbidden = new();

        /// <summary> If any of these tags IS NOT present on target, this ability won't be activated. </summary>
        [Tooltip("If any of these tags IS NOT present on target, this ability won't be activated. ")]
        [SerializeField] public List<GameplayTag> TargetTagsRequired = new(); //The Ability can only be activated if the activating Component has all Required Tags and none of Ingnored Tags. </summary>
        /// <summary> If any of these tags IS present on target, this ability won't be activated. </summary>
        [Tooltip("If any of these tags IS present on target, this ability won't be activated.")]
        [SerializeField] public List<GameplayTag> TargetTagsForbidden = new();


        [HideInInspector] public bool initialized = false;
        [ReadOnly][HideInInspector] public List<string> stringActivationOwnedTags = new();
        [ReadOnly][HideInInspector] public List<string> stringDescriptionTags = new();
        [ReadOnly][HideInInspector] public List<string> stringCancelAbilitiesWithTags = new();
        [ReadOnly][HideInInspector] public List<string> stringBlockAbilitiesWithTags = new();

        [ReadOnly][HideInInspector] public List<string> stringSourceTagsRequired = new();
        [ReadOnly][HideInInspector] public List<string> stringSourceTagsForbidden = new();
        [ReadOnly][HideInInspector] public List<string> stringTargetTagsRequired = new();
        [ReadOnly][HideInInspector] public List<string> stringTargetTagsForbidden = new();

        [ReadOnly][HideInInspector] public List<string> string_CueTags = new();

        public void FillTags(GameplayAbility ga)
        {
            initialized = true;
            // Debug.Log($"GA {ga.name} - GetAllTags GrantedTags: [{string.Join(", ", ActivationOwnedTags.Select(x => x.name))}]  string_GrantedTags: [{string.Join(", ", stringActivationOwnedTags.Select(x => x))}]");
            ActivationOwnedTags = ActivationOwnedTags.Union(GameplayTagLibrary.Instance.GetByNames(stringActivationOwnedTags)).ToList();
            DescriptionTags = DescriptionTags.Union(GameplayTagLibrary.Instance.GetByNames(stringDescriptionTags)).ToList();
            CancelAbilitiesWithTags = CancelAbilitiesWithTags.Union(GameplayTagLibrary.Instance.GetByNames(stringCancelAbilitiesWithTags)).ToList();
            BlockAbilitiesWithTags = BlockAbilitiesWithTags.Union(GameplayTagLibrary.Instance.GetByNames(stringBlockAbilitiesWithTags)).ToList();

            SourceTagsRequired = SourceTagsRequired.Union(GameplayTagLibrary.Instance.GetByNames(stringSourceTagsRequired)).ToList();
            SourceTagsForbidden = SourceTagsForbidden.Union(GameplayTagLibrary.Instance.GetByNames(stringSourceTagsForbidden)).ToList();
            TargetTagsRequired = TargetTagsRequired.Union(GameplayTagLibrary.Instance.GetByNames(stringTargetTagsRequired)).ToList();
            TargetTagsForbidden = TargetTagsForbidden.Union(GameplayTagLibrary.Instance.GetByNames(stringTargetTagsForbidden)).ToList();

            ga.cuesTags = ga.cuesTags.Union(GameplayTagLibrary.Instance.GetByNames(string_CueTags)).ToList();

        }

        public void FillStrings(GameplayAbility ga)
        {
            stringActivationOwnedTags = ActivationOwnedTags.Select(tag => tag.name).ToList();
            stringDescriptionTags = DescriptionTags.Select(tag => tag.name).ToList();
            stringCancelAbilitiesWithTags = CancelAbilitiesWithTags.Select(tag => tag.name).ToList();
            stringBlockAbilitiesWithTags = BlockAbilitiesWithTags.Select(tag => tag.name).ToList();
            stringSourceTagsRequired = SourceTagsRequired.Select(tag => tag.name).ToList();
            stringSourceTagsForbidden = SourceTagsForbidden.Select(tag => tag.name).ToList();
            stringTargetTagsRequired = TargetTagsRequired.Select(tag => tag.name).ToList();
            stringTargetTagsForbidden = TargetTagsForbidden.Select(tag => tag.name).ToList();

            string_CueTags = ga.cuesTags.Select(tag => tag.name).ToList();
        }


        public void ClearTags(GameplayAbility ga)
        {
            ActivationOwnedTags.Clear();
            DescriptionTags.Clear();
            CancelAbilitiesWithTags.Clear();
            BlockAbilitiesWithTags.Clear();
            SourceTagsRequired.Clear();
            SourceTagsForbidden.Clear();
            TargetTagsRequired.Clear();
            TargetTagsForbidden.Clear();

            ga.cuesTags.Clear();
        }

        public void ClearStrings()
        {
            stringActivationOwnedTags.Clear();
            stringDescriptionTags.Clear();
            stringCancelAbilitiesWithTags.Clear();
            stringBlockAbilitiesWithTags.Clear();
            stringSourceTagsRequired.Clear();
            stringSourceTagsForbidden.Clear();
            stringTargetTagsRequired.Clear();
            stringTargetTagsForbidden.Clear();


            string_CueTags.Clear();
        }
    }
}