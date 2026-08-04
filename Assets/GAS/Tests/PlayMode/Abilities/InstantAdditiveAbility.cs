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
        /// Waits for target data before applying the configured instant gameplay effect.
        /// </summary>
        public override void ActivateAbility(
            AbilitySystemComponent source,
            string activationGUID)
        {
            base.ActivateAbility(
                source,
                activationGUID);

            if (
                !CommitAbility(
                    source,
                    activationGUID))
            {
                DeactivateAbility(
                    activationGUID);

                return;
            }

            void HandleTargetDataReady(
                GameplayAbilityTargetDataHandle targetData)
            {
                ApplyGameplayEffects(
                    source,
                    targetData,
                    activationGUID);

                DeactivateAbility(
                    activationGUID);
            }

            void HandleTargetDataCancelled(
                GameplayAbilityTargetDataHandle _)
            {
                DeactivateAbility(
                    activationGUID);
            }

            AbilityTask_WaitTargetData targetDataTask =
                AbilityTask_WaitTargetData.WaitTargetData(
                    this,
                    string.Empty,
                    GameplayTargetingConfirmation.Instant,
                    m_TargetActorPrefab);

            targetDataTask.ValidData +=
                HandleTargetDataReady;

            targetDataTask.Cancelled +=
                HandleTargetDataCancelled;

            targetDataTask.ReadyForActivation();
        }
    }
}