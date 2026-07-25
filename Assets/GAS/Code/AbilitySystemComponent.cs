using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        public GameplayTag Tag { get; }

        public string ActivationGUID { get; }
    }

    /// <summary>
    /// ASC for short, frequently also named AbilitySystemCHARACTER.
    /// This is the container that holds all the gears of the
    /// GameplayAbilitySystem. It represents a single character/entity
    /// with its attributes and abilities.
    /// </summary>
    [Serializable]
    public class AbilitySystemComponent : MonoBehaviour
    {
        public GroupASC initialData;

        [ReadOnly]
        public Dictionary<string, Attribute> attributesDictionary = new();

        public List<Attribute> attributes = new();

        public Action<
            AttributeName,
            float,
            float,
            GameplayEffect> OnAttributeChanged;

        public Action<
            Attribute,
            GameplayEffect> OnPreAttributeChange;

        [SerializeReference]
        public List<AttributeProcessor> attributesProcessors =
            new();

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

        /// <summary>
        /// Локальный поток одноразовых gameplay events этого ASC.
        /// События не добавляются в постоянные tags и сами по себе
        /// не реплицируются.
        /// </summary>
        public event Action<GameplayEventData> OnGameplayEvent;

        public List<GameplayEffect> appliedGameplayEffects;

        public Action<GameplayEffect> OnGameplayEffectApplied;
        public Action<GameplayEffect> OnGameplayEffectRemoved;

        public Action<
            List<GameplayEffect>> OnGameplayEffectsChanged;

        public List<GameplayTag> tags;

        public Action<
            List<GameplayTag>,
            AbilitySystemComponent,
            AbilitySystemComponent,
            string> OnTagsChanged;

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

        private float inputBufferDurationSeconds = .16f;

        public void Awake()
        {
            initialData.AddAttributes(this);
            initialData.AddAttributeProcessors(this);
            initialData.GrantAbilities(this);

            ResetStatsAttributesValues();

            OnGameplayEffectApplied +=
                ge => OnGameplayEffectsChanged?.Invoke(
                    appliedGameplayEffects);

            OnGameplayEffectRemoved +=
                ge => OnGameplayEffectsChanged?.Invoke(
                    appliedGameplayEffects);

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
                    attribute.OnPreAttributeChange +=
                        (changedAttribute, gameplayEffect) =>
                        {
                            OnPreAttributeChange?.Invoke(
                                changedAttribute,
                                gameplayEffect);
                        });

            attributes.ForEach(
                attribute =>
                {
                    attribute.name =
                        attribute.attributeName.name;

                    attributesDictionary.Add(
                        attribute.attributeName.name,
                        attribute);
                });

            OnGameplayEffectApplied +=
                UpdateTagsOnEffectChange;

            OnGameplayEffectRemoved +=
                UpdateTagsOnEffectChange;

            OnGameplayAbilityActivated +=
                UpdateTagsOnGameplayAbilityActivate;

            OnGameplayAbilityDeactivated +=
                UpdateTagsOnGameplayAbilityDeactivate;

            OnGameplayEffectApplied +=
                TriggerOnTagsAdded;

            attributesProcessors.ForEach(
                processor =>
                    OnPreAttributeChange +=
                        (attribute, gameplayEffect) =>
                        {
                            processor.PreProcess(
                                attribute,
                                gameplayEffect,
                                this);
                        });

            attributesProcessors.ForEach(
                processor =>
                    OnAttributeChanged +=
                        (
                            attributeName,
                            oldValue,
                            newValue,
                            gameplayEffect) =>
                        {
                            processor.PostProcessed(
                                attributeName,
                                oldValue,
                                newValue,
                                gameplayEffect);
                        });

            GameplayCueManager.Register(this);
        }

        private void Start()
        {
            InitializeAttributesListeners();

            if (logging)
            {
                OnPreAttributeChange +=
                    (attribute, gameplayEffect) =>
                    {
                        Debug.Log(
                            $"OnPreAttributeChange: " +
                            $"{attribute.attributeName.name} " +
                            $"{gameplayEffect?.name}");
                    };

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

                if (gameplayAbility.isActive)
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

        public void UpdateTagsOnEffectChange(
            GameplayEffect gameplayEffect)
        {

            TagProcessor.UpdateTags(
                gameplayEffect.source,
                gameplayEffect.target,
                ref tags,
                appliedGameplayEffects,
                grantedGameplayAbilities,
                OnTagsChanged,
                gameplayEffect.applicationGUID);
        }

        public void UpdateTagsOnGameplayAbilityActivate(
            GameplayAbility gameplayAbility,
            string activationGUID)
        {

            TagProcessor.UpdateTags(
                gameplayAbility.source,
                gameplayAbility.target,
                ref tags,
                appliedGameplayEffects,
                grantedGameplayAbilities,
                OnTagsChanged,
                activationGUID);
        }

        public void UpdateTagsOnGameplayAbilityDeactivate(
            GameplayAbility gameplayAbility,
            string activationGUID)
        {

            TagProcessor.UpdateTags(
                gameplayAbility.source,
                gameplayAbility.target,
                ref tags,
                appliedGameplayEffects,
                grantedGameplayAbilities,
                OnTagsChanged,
                activationGUID);
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

        public float GetAttributeValue(
            string attributeName)
        {

            if (
                attributesDictionary.TryGetValue(
                    attributeName,
                    out Attribute attribute))
            {

                return attribute.GetValue();
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

        public void ResetStatsAttributesValues()
        {
            foreach (Attribute attribute in attributes)
            {
                attribute.currentValue =
                    attribute.baseValue;
            }
        }

        public void InitializeAttributesListeners()
        {
            foreach (Attribute attribute in attributes)
            {
                attribute.OnPostAttributeChange?.Invoke(
                    attribute.attributeName,
                    0,
                    attribute.baseValue,
                    null);
            }
        }

        public void ApplyAttributeModifiersValues(
            GameplayEffect gameplayEffect)
        {

            attributes.ForEach(
                attribute =>
                    attribute.ApplyModifiers(
                        gameplayEffect));
        }

        public void RefreshAttributesModifiers(
            GameplayEffect gameplayEffect)
        {

            attributes.ForEach(
                attribute =>
                    attribute.modification.Clear());
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

        public void UngrantAbilityByTag(
            GameplayTag tag)
        {

            var removeIndexes =
                new List<int>();

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
                        gameplayAbility.guid == guid));
        }

        public void UngrantAbility(
            GameplayAbility gameplayAbility)
        {

            gameplayAbility.DeactivateAbility(null);

            grantedGameplayAbilities.Remove(
                gameplayAbility);

            OnGameplayAbilityUngranted?.Invoke(
                gameplayAbility);
        }

        public List<GameplayTag> GetAllTags()
        {
            return tags;
        }

        public void TryActivateAbility(
            string abilityName,
            AbilitySystemComponent target)
        {

            GameplayAbility gameplayAbility =
                grantedGameplayAbilities.Find(
                    ability =>
                        ability.name == abilityName);

            if (gameplayAbility == null)
            {
                gameplayAbility =
                    grantedGameplayAbilities.Find(
                        ability =>
                            ability.name.Contains(
                                abilityName));
            }

            if (gameplayAbility == null)
            {
                Debug.Log(
                    $"No granted Ability named " +
                    $"{abilityName}");

                return;
            }

            TryActivateAbility(
                gameplayAbility,
                target,
                null);
        }

        [EasyButtons.Button]
        public void TryActivateAbility(
            int index,
            AbilitySystemComponent target)
        {

            if (
                index >=
                grantedGameplayAbilities.Count)
            {

                Debug.Log(
                    $"No granted Ability at given index " +
                    $"{grantedGameplayAbilities}");

                return;
            }

            TryActivateAbility(
                grantedGameplayAbilities[index],
                target);
        }

        public void TryActivateAbility(
            string guid,
            AbilitySystemComponent target,
            string activationGUID)
        {

            GameplayAbility gameplayAbility =
                grantedGameplayAbilities.Find(
                    ability =>
                        ability.guid == guid);

            if (gameplayAbility == null)
            {
                Debug.Log(
                    $"No granted Ability with guid {guid}");

                return;
            }

            TryActivateAbility(
                gameplayAbility,
                target,
                activationGUID);
        }

        public async void TryActivateAbility(
            GameplayAbility gameplayAbility,
            AbilitySystemComponent target,
            string activationGUID = null)
        {

            if (string.IsNullOrEmpty(activationGUID))
            {
                activationGUID =
                    Guid.NewGuid().ToString();
            }

            gameplayAbility.source = this;
            gameplayAbility.target = target;
            gameplayAbility.activationGUID =
                activationGUID;

            OnGameplayAbilityTryActivate?.Invoke(
                gameplayAbility,
                activationGUID);

            if (gameplayAbility.isActive)
            {
                gameplayAbility.DeactivateAbility(
                    gameplayAbility.activationGUID);

                return;
            }

            await InputBuffering(
                gameplayAbility,
                target,
                gameplayAbility.activationGUID);

            if (
                !gameplayAbility.CanActivateAbility(
                    this,
                    target,
                    gameplayAbility.activationGUID,
                    true))
            {

                return;
            }

            gameplayAbility.CommitAbility(
                this,
                target,
                gameplayAbility.activationGUID);
        }

        public async Task InputBuffering(
            GameplayAbility gameplayAbility,
            AbilitySystemComponent target,
            string activationGUID = null)
        {

            float finalTime =
                Time.realtimeSinceStartup +
                inputBufferDurationSeconds;

            while (
                !gameplayAbility.isActive &&
                Time.realtimeSinceStartup < finalTime &&
                !gameplayAbility.CanActivateAbility(
                    this,
                    target,
                    activationGUID,
                    false))
            {

                await Task.Delay(10);
            }
        }

        /// <summary>
        /// Applies a GameplayEffect to an ASC.
        /// ApplicationGUID is used for client-side prediction.
        /// </summary>
        public GameplayEffect ApplyGameplayEffect(
            AbilitySystemComponent source,
            AbilitySystemComponent target,
            GameplayEffect gameplayEffect,
            string applicationGUID = null)
        {

            if (logging)
            {
                Debug.Log(
                    $"ASC ApplyGameplayEffect " +
                    $"{gameplayEffect.name} {name} " +
                    $"applicationGUID: {applicationGUID} " +
                    $"data: " +
                    $"{JsonUtility.ToJson(gameplayEffect, true)}");
            }

            gameplayEffect.source = source;
            gameplayEffect.target = target;
            gameplayEffect.applicationGUID =
                applicationGUID;

            if (
                !TagProcessor.CheckApplicationTagRequirementsGE(
                    this,
                    gameplayEffect,
                    tags))
            {

                if (logging)
                {
                    Debug.Log(
                        $"GE: {gameplayEffect.name} " +
                        "couldnt be applied on this ASC. " +
                        "Failed application tag requirements");
                }

                return null;
            }

            if (gameplayEffect.chanceToApply < 1f)
            {
                if (
                    !(
                        UnityEngine.Random.Range(
                            0f,
                            1f) <=
                        gameplayEffect.chanceToApply))
                {

                    return null;
                }
            }

            GameplayEffect gameplayEffectCopy =
                gameplayEffect.Instantiate();

            if (
                gameplayEffectCopy.durationType !=
                GameplayEffectDurationType.Instant)
            {

                appliedGameplayEffects.Add(
                    gameplayEffectCopy);
            }

            switch (gameplayEffect.durationType)
            {
                case GameplayEffectDurationType.Infinite:
                case GameplayEffectDurationType.Duration:
                    Debug.Log(
                        "[FREE VERSION] Duration and Infinite " +
                        "GameplayEffects are not fully available " +
                        "on the free version. Check GASify on " +
                        "the Assetstore for more options.");

                    RemoveDurationGE(
                        gameplayEffectCopy);

                    break;

                case GameplayEffectDurationType.Instant:
                    ApplyInstantGameplayEffect(
                        gameplayEffectCopy);

                    break;
            }

            if (invokeEventsGE)
            {
                OnGameplayEffectApplied?.Invoke(
                    gameplayEffectCopy);
            }

            return gameplayEffectCopy;
        }

        public async void RemoveDurationGE(
            GameplayEffect gameplayEffect)
        {

            await Task.Delay(1000);

            appliedGameplayEffects.Remove(
                gameplayEffect);

            OnGameplayEffectRemoved?.Invoke(
                gameplayEffect);
        }

        private readonly List<Modifier> modifiersToProcess =
            new();

        public void ApplyInstantGameplayEffect(
            GameplayEffect gameplayEffect)
        {

            modifiersToProcess.Clear();

            modifiersToProcess.AddRange(
                gameplayEffect.modifiers);

            foreach (Attribute attribute in attributes)
            {
                foreach (
                    Modifier modifier
                    in modifiersToProcess)
                {

                    if (
                        attribute.attributeName ==
                        modifier.attributeName)
                    {

                        attribute.ApplyModifierAsResource(
                            modifier,
                            gameplayEffect);
                    }
                }
            }
        }
    }
}