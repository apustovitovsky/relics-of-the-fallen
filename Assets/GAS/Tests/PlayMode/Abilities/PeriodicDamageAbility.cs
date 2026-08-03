using System;

namespace GAS.Tests
{
    [Serializable]
    internal sealed class PeriodicDamageAbility :
        GameplayAbility
    {
        /// <summary>
        /// Applies a periodic damage effect through directly produced actor target data.
        /// </summary>
        public override void ActivateAbility(
            AbilitySystemComponent source,
            AbilitySystemComponent target,
            string activationGUID)
        {
            base.ActivateAbility(
                source,
                target,
                activationGUID);

            if (
                !CommitAbility(
                    source,
                    target,
                    activationGUID))
            {
                DeactivateAbility(
                    activationGUID);

                return;
            }

            GameplayAbilityTargetData targetActorData =
                new GameplayAbilityTargetData_ActorArray(
                    target.AbilityActorInfo.OwnerActor);

            GameplayAbilityTargetDataHandle targetData =
                new(
                    targetActorData);

            ApplyGameplayEffects(
                source,
                targetData,
                activationGUID);

            DeactivateAbility(
                activationGUID);
        }
    }
}