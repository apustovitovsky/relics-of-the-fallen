using System;
using UnityEngine;

namespace GAS.Tests
{
    internal sealed class DirectActorTargetActor :
        GameplayAbilityTargetActor
    {
        [field: SerializeField]
        public GameObject TargetActor
        {
            get;
            private set;
        }

        /// <summary>
        /// Assigns the gameplay actor that this targeting fixture must return.
        /// </summary>
        public void SetTargetActor(
            GameObject targetActor)
        {
            if (targetActor == null)
            {
                throw new ArgumentNullException(
                    nameof(targetActor));
            }

            TargetActor = targetActor;
        }

        /// <summary>
        /// Produces actor-array target data for the assigned gameplay actor.
        /// </summary>
        public override void ConfirmTargetingAndContinue()
        {
            if (TargetActor == null)
            {
                CancelTargeting();

                return;
            }

            GameplayAbilityTargetData actorData =
                new GameplayAbilityTargetData_ActorArray(
                    TargetActor);

            BroadcastTargetDataReady(
                new GameplayAbilityTargetDataHandle(
                    actorData));
        }
    }
}