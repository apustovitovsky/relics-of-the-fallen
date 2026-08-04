using System;

namespace GAS.Tests
{
    [Serializable]
    internal sealed class InstantAdditiveAbility :
        GameplayAbility
    {
        private GameplayAbilityTargetActor m_TargetActorPrefab;

        /// <summary>
        /// Assigns the target actor prefab used to produce target data for this test ability.
        /// </summary>
        public void SetTargetActorPrefab(
            GameplayAbilityTargetActor targetActorPrefab)
        {
            if (targetActorPrefab == null)
            {
                throw new ArgumentNullException(
                    nameof(targetActorPrefab));
            }

            m_TargetActorPrefab = targetActorPrefab;
        }

        /// <summary>
        /// Assigns the cooldown gameplay effect used by this test ability.
        /// </summary>
        public void SetCooldownGameplayEffect(
            GameplayEffectSO cooldownGameplayEffect)
        {
            if (cooldownGameplayEffect == null)
            {
                throw new ArgumentNullException(
                    nameof(cooldownGameplayEffect));
            }

            m_CooldownGameplayEffect =
                cooldownGameplayEffect;
        }

        /// <summary>
        /// Creates a runtime ability instance that retains its targeting prefab.
        /// </summary>
        public override GameplayAbility Instantiate(
            AbilitySystemComponent owner)
        {
            InstantAdditiveAbility instance =
                (InstantAdditiveAbility)base.Instantiate(
                    owner);

            instance.m_TargetActorPrefab =
                m_TargetActorPrefab;

            return instance;
        }

        /// <summary>
        /// Assigns the cost gameplay effect used by this test ability.
        /// </summary>
        public void SetCostGameplayEffect(
            GameplayEffectSO costGameplayEffect)
        {
            if (costGameplayEffect == null)
            {
                throw new ArgumentNullException(
                    nameof(costGameplayEffect));
            }

            m_CostGameplayEffect =
                costGameplayEffect;
        }

        /// <summary>
        /// Waits for target data before applying the configured instant gameplay effect.
        /// </summary>
        /// <summary>
        /// Waits for target data before applying the configured instant gameplay effect.
        /// </summary>
        protected override void ActivateAbility(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo,
            GameplayEventData? triggerEventData)
        {
            base.ActivateAbility(
                handle,
                actorInfo,
                activationInfo,
                triggerEventData);

            AbilitySystemComponent source =
                actorInfo.AbilitySystemComponent;

            if (
                !CommitAbility(
                    handle,
                    actorInfo,
                    activationInfo))
            {
                EndAbility(
                    handle,
                    actorInfo,
                    activationInfo,
                    true,
                    false);

                return;
            }

            void HandleTargetDataReady(
                GameplayAbilityTargetDataHandle targetData)
            {
                ApplyGameplayEffects(
                    source,
                    activationInfo,
                    targetData);

                EndAbility(
                    handle,
                    actorInfo,
                    activationInfo,
                    true,
                    false);
            }

            void HandleTargetDataCancelled(
                GameplayAbilityTargetDataHandle _)
            {
                EndAbility(
                    handle,
                    actorInfo,
                    activationInfo,
                    true,
                    true);
            }

            AbilityTask_WaitTargetData targetDataTask =
                AbilityTask_WaitTargetData.WaitTargetData(
                    this,
                    string.Empty,
                    GameplayTargetingConfirmation.Instant,
                    m_TargetActorPrefab);

            targetDataTask.RegisterValidDataDelegate(
                HandleTargetDataReady);

            targetDataTask.RegisterCancelledDelegate(
                HandleTargetDataCancelled);

            targetDataTask.ReadyForActivation();
        }
    }
}