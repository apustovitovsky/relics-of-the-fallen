using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using UnityEngine.Events;
using EasyButtons;


namespace GAS
{
    /// <summary>
    ///  A Gameplay Ability is any action the ASC (Ability System Component) can use. <br />
    /// These can be spells, skills, passives, interactions or any other action.
    /// The most common uses (e.g. Instant, Projectile, etc..) examples are implemented. <br />
    /// You can also extend that class to create even more interesting abilities.
    /// </summary>
    // Instant, Passive, Toggeable, Channelled(todo), Cast(todo), triggered(todo?)
    // Some channelled abilities still have a duration time e.g. MindControl
    [Serializable]
    public class GameplayAbility
    {
        [ReadOnly] public string name;
        public GameplayEffect cooldown = null; // GE with duration of Xs
        public GameplayEffect cost = null;

        [SerializeField]
        private GameplayEffectSO m_CooldownGameplayEffect;

        [SerializeField]
        private GameplayEffectSO m_CostGameplayEffect;

        public GameplayEffectSO CooldownGameplayEffect =>
            m_CooldownGameplayEffect;

        [NonSerialized]
        private readonly List<AbilityTask> m_ActiveTasks = new();

        public GameplayEffectSO CostGameplayEffect =>
            m_CostGameplayEffect;

        public GameplayAbilitySpecHandle CurrentSpecHandle
        {
            get; internal set;
        }

        public GameplayAbilityActivationInfo CurrentActivationInfo
        {
            get; internal set;
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
        private float m_TimeActivated;
        public string ActivationGUID;


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

            if (cooldown != null)
            {
                gaCopy.CreateCoolDownGE(
                    cooldown.durationValue);

                gaCopy.cooldown.gameplayEffectTags =
                    cooldown.gameplayEffectTags;
            }

            if (cost != null)
            {
                gaCopy.CreateCostGE(
                    cost.modifiers,
                    cost.durationType,
                    cost.durationValue);

                gaCopy.cost.gameplayEffectTags =
                    cost.gameplayEffectTags;
            }

            gaCopy.abilityTags =
                abilityTags;

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
        /// Initializes runtime state and applies activation-owned and blocking tags.
        /// </summary>
        public virtual void PreActivate(
            AbilitySystemComponent source,
            string activationGUID)
        {
            IsActive =
                true;

            this.source =
                source;

            ActivationGUID =
                activationGUID;

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

            source.OnGameplayAbilityPreActivate?.Invoke(
                this,
                activationGUID);
        }

        /// <summary>
        /// Executes the ability-specific behavior after pre-activation completes.
        /// </summary>
        public virtual void ActivateAbility(
            AbilitySystemComponent source,
            string activationGUID)
        {
        }

        /// <summary>
        /// Applies a prepared gameplay effect specification to the owner of this ability.
        /// </summary>
        protected ActiveGameplayEffectHandle ApplyGameplayEffectSpecToOwner(
            AbilitySystemComponent ownerAbilitySystem,
            GameplayAbilityActivationInfo activationInfo,
            GameplayEffectSpec spec)
        {
            PredictionKey predictionKey =
                activationInfo.GetActivationPredictionKey();

            return ownerAbilitySystem.ApplyGameplayEffectSpecToSelf(
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
                ownerAbilitySystem,
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
            GameplayAbilityTargetDataHandle targetData,
            string activationGUID)
        {
            for (
                int index = 0;
                index < effectsSO.Count;
                index++)
            {
                ApplyGameplayEffectToTarget(
                    source,
                    CurrentActivationInfo,
                    targetData,
                    effectsSO[index],
                    Level,
                    activationGUID);
            }
        }

        /// <summary>
        /// Performs the final commit check and applies the ability cost and cooldown.
        /// </summary>
        public virtual bool CommitAbility(
            AbilitySystemComponent source,
            string activationGUID)
        {
            if (
                !CommitCheck(
                    source,
                    activationGUID))
            {
                return false;
            }

            CommitExecute(
                source,
                activationGUID);

            return true;
        }

        /// <summary>
        /// Performs the final cost and cooldown checks before committing the ability.
        /// </summary>
        public virtual bool CommitCheck(
            AbilitySystemComponent source,
            string activationGUID)
        {
            return
                CheckCooldown() &&
                CheckCost(
                    source);
        }

        /// <summary>
        /// Applies the ability cooldown and cost after a successful commit check.
        /// </summary>
        public virtual void CommitExecute(
            AbilitySystemComponent source,
            string activationGUID)
        {
            ApplyCooldown(
                source,
                activationGUID);

            ApplyCost(
                source,
                activationGUID);
        }

        /// <summary>
        /// Checks whether the ability is currently outside its cooldown period.
        /// </summary>
        public virtual bool CheckCooldown()
        {
            return
                cooldown == null ||
                GetCooldownRemaining() <= 0f;
        }

        /// <summary>
        /// Applies the configured cooldown gameplay effect to the ability owner.
        /// </summary>
        public virtual void ApplyCooldown(
            AbilitySystemComponent source,
            string activationGUID)
        {
            if (
                cooldown == null ||
                cooldown.durationValue <= 0f)
            {
                return;
            }

            m_TimeActivated =
                Time.time;

            source.ApplyGameplayEffect(
                source,
                source,
                cooldown,
                activationGUID);
        }

        /// <summary>
        /// Applies the configured cost gameplay effect to the ability owner.
        /// </summary>
        public virtual void ApplyCost(
            AbilitySystemComponent source,
            string activationGUID)
        {
            if (
                cost == null ||
                !cost.HasModifiers)
            {
                return;
            }

            source.ApplyGameplayEffect(
                source,
                source,
                cost,
                activationGUID);
        }

        /// <summary>
        /// Checks and applies only the configured ability cost.
        /// </summary>
        public virtual bool CommitAbilityCost(
            AbilitySystemComponent source,
            string activationGUID)
        {
            if (!CheckCost(source))
            {
                return false;
            }

            ApplyCost(
                source,
                activationGUID);

            return true;
        }

        /// <summary>
        /// Checks and applies only the configured ability cooldown.
        /// </summary>
        public virtual bool CommitAbilityCooldown(
            AbilitySystemComponent source,
            string activationGUID,
            bool forceCooldown = false)
        {
            if (
                !forceCooldown &&
                !CheckCooldown())
            {
                return false;
            }

            ApplyCooldown(
                source,
                activationGUID);

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
            string activationGUID = null)
        {
            if (!IsActive)
            {
                return;
            }

            AbilityTask[] activeTasks =
                m_ActiveTasks.ToArray();

            for (
                int index = 0;
                index < activeTasks.Length;
                index++)
            {
                if (!IsActive)
                {
                    return;
                }

                AbilityTask task =
                    activeTasks[index];

                if (!task.IsEnded)
                {
                    task.ExternalCancel();
                }
            }

            string resolvedActivationGUID =
                string.IsNullOrEmpty(
                    activationGUID)
                    ? this.ActivationGUID
                    : activationGUID;

            DeactivateAbility(
                resolvedActivationGUID);
        }

        /// <summary>
        /// Ends the active ability, its tasks, replicated data, and owned and blocking tags.
        /// </summary>
        public virtual void DeactivateAbility(
            string activationGUID = null)
        {
            if (!IsActive)
            {
                return;
            }

            EndAbilityTasks();

            source.ClearAbilityReplicatedDataCache(
                CurrentSpecHandle,
                CurrentActivationInfo);

            source.ApplyAbilityBlockAndCancelTags(
                abilityTags.DescriptionTags,
                this,
                false,
                abilityTags.BlockAbilitiesWithTags,
                false,
                abilityTags.CancelAbilitiesWithTags);

            source.UpdateTagMap(
                abilityTags.ActivationOwnedTags,
                -1);

            IsActive = false;

            if (source.invokeEventsGA)
            {
                source.OnGameplayAbilityDeactivated?.Invoke(
                    this,
                    activationGUID);
            }
        }

        public float GetCooldownRemaining()
        {
            if (cooldown == null)
                return 0;
            return Math.Clamp((m_TimeActivated + cooldown.durationValue) - Time.time, 0, 100000f);
        }

        public GameplayEffect CreateCoolDownGE(float durationValue, GameplayTag cooldownTag = null, string cooldownName = "Cooldown")
        {
            cooldown = new GameplayEffect()
            {
                durationType = GameplayEffectDurationType.Duration,
                name = cooldownName + " " + name,
                durationValue = durationValue,
            };
            if (cooldownTag != null)
            {
                cooldown.gameplayEffectTags = new GameplayEffectTags()
                {
                    GrantedTags = new List<GameplayTag>() { cooldownTag }
                };
            }
            // Debug.Log("cooldown from" + name + " : " + JsonUtility.ToJson(cooldown)); //This causes some weirds error when exiting playmode. related to coroutine usage.
            return cooldown;
        }

        public GameplayEffect CreateCostGE(List<Modifier> modifiers, GameplayEffectDurationType durationType = GameplayEffectDurationType.Instant, float duration = 0, GameplayTag costTag = null, string costName = "Cost")
        {
            if (costTag == null)
                costTag = GameplayTagLibrary.Instance.GetByName("Ability.Cost"); // GameplayTags.library.GetByName("Ability.Cost");

            var createdCost = new GameplayEffect()
            {
                durationType = durationType,
                name = costName + " " + name,
                durationValue = duration,
                gameplayEffectTags = new GameplayEffectTags()
                {
                    GrantedTags = new List<GameplayTag>() { costTag }
                }
            };
            if (costTag != null)
            {
                createdCost.gameplayEffectTags = new GameplayEffectTags()
                {
                    GrantedTags = new List<GameplayTag>() { costTag }
                };
            }
            // Debug.Log("createdCost from" + createdCost + " : " + JsonUtility.ToJson(createdCost)); //This causes some weirds error when exiting playmode. related to coroutine usage.
            createdCost.modifiers = modifiers;
            cost = createdCost;
            return createdCost;
        }

        /// <summary>
        /// Determines whether the owning ability system can activate this ability.
        /// </summary>
        public virtual bool CanActivateAbility(
            AbilitySystemComponent source,
            string activationGUID,
            bool sendFailedEvent)
        {
            if (IsActive)
            {
                if (source.logging)
                {
                    Debug.Log(
                        $"{name} is already active.");
                }

                if (sendFailedEvent)
                {
                    source.OnGameplayAbilityFailedActivation?.Invoke(
                        this,
                        activationGUID,
                        ActivationFailure.ALREADY_ACTIVE);
                }

                return false;
            }

            if (
                source.AreAbilityTagsBlocked(
                    abilityTags.DescriptionTags))
            {
                if (source.logging)
                {
                    Debug.Log(
                        $"{name} is blocked by ability tags.");
                }

                if (sendFailedEvent)
                {
                    source.OnGameplayAbilityFailedActivation?.Invoke(
                        this,
                        activationGUID,
                        ActivationFailure.TAGS_BLOCKED);
                }

                return false;
            }

            float cooldownRemaining =
                GetCooldownRemaining();

            if (cooldownRemaining > 0f)
            {
                if (source.logging)
                {
                    Debug.Log(
                        $"{name} is on cooldown. " +
                        $"Time remaining: {cooldownRemaining}.");
                }

                if (sendFailedEvent)
                {
                    source.OnGameplayAbilityFailedActivation?.Invoke(
                        this,
                        activationGUID,
                        ActivationFailure.COOLDOWN);
                }

                return false;
            }

            if (!CheckCost(
                    source))
            {
                if (sendFailedEvent)
                {
                    source.OnGameplayAbilityFailedActivation?.Invoke(
                        this,
                        activationGUID,
                        ActivationFailure.COST);
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether the source can afford the evaluated gameplay effect cost.
        /// </summary>
        public virtual bool CheckCost(
            AbilitySystemComponent source)
        {
            if (
                cost == null ||
                !cost.HasModifiers)
            {
                return true;
            }

            GameplayEffectContextHandle effectContext = MakeEffectContext(
                CurrentSpecHandle,
                source.AbilityActorInfo);

            GameplayEffectSpec costSpec = source.MakeOutgoingSpec(
                cost,
                cost.level,
                effectContext);

            costSpec.CaptureAttributeDataFromTarget(
                source);

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
                    !source.AttributesDictionary.TryGetValue(
                        attributeName.name,
                        out Attribute attribute))
                {
                    if (source.logging)
                    {
                        Debug.Log(
                            $"ASC does not contain cost attribute " +
                            $"'{attributeName}'.");
                    }

                    return false;
                }

                float resultingValue =
                    attribute.CurrentValue +
                    magnitude;

                if (resultingValue >= 0f)
                {
                    continue;
                }

                if (source.logging)
                {
                    Debug.Log(
                        $"Cannot pay ability cost: " +
                        $"{attributeName} has {attribute.CurrentValue}, " +
                        $"cost magnitude is {magnitude}.");
                }

                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Reason for an activation failure. You can add new reasons when implementing your own abilities.
    /// </summary>
    public enum ActivationFailure
    {
        ALREADY_ACTIVE,
        COST,
        COOLDOWN,
        TAGS_SOURCE_FAILED,
        TAGS_TARGET_FAILED,
        TAGS_BLOCKED,
        OTHER,
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