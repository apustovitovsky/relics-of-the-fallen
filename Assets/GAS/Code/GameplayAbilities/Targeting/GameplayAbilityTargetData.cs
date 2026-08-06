using System;
using System.Collections.Generic;
using UnityEngine;

namespace GAS
{
    public abstract class GameplayAbilityTargetData
    {
        public abstract IReadOnlyList<GameObject> GetActors();

        /// <summary>
        /// Returns whether this targeting payload provides a usable origin.
        /// </summary>
        public virtual bool HasOrigin()
        {
            return false;
        }

        /// <summary>
        /// Returns the origin transform represented by this targeting payload.
        /// </summary>
        public virtual Pose GetOrigin()
        {
            return Pose.identity;
        }

        /// <summary>
        /// Returns whether this targeting payload provides a usable endpoint.
        /// </summary>
        public virtual bool HasEndPoint()
        {
            return false;
        }

        /// <summary>
        /// Returns the endpoint transform represented by this targeting payload.
        /// </summary>
        public virtual Pose GetEndPointTransform()
        {
            return Pose.identity;
        }

        /// <summary>
        /// Applies a prepared gameplay effect specification to every represented target.
        /// </summary>
        public virtual IReadOnlyList<ActiveGameplayEffectHandle>
            ApplyGameplayEffectSpec(
                GameplayEffectSpec spec,
                PredictionKey predictionKey)
        {
            if (spec == null)
            {
                throw new ArgumentNullException(
                    nameof(spec));
            }

            List<ActiveGameplayEffectHandle> appliedEffectHandles =
                new();

            IReadOnlyList<GameObject> targetActors =
                GetActors();

            for (
                int index = 0;
                index < targetActors.Count;
                index++)
            {
                GameObject targetActor =
                    targetActors[index];

                if (
                    !AbilitySystemGlobals
                        .TryGetAbilitySystemComponentFromActor(
                            targetActor,
                            out AbilitySystemComponent targetAbilitySystem))
                {
                    continue;
                }

                ActiveGameplayEffectHandle appliedEffectHandle =
                    targetAbilitySystem.ApplyGameplayEffectSpecToSelf(
                        spec,
                        predictionKey);

                if (appliedEffectHandle.WasSuccessfullyApplied)
                {
                    appliedEffectHandles.Add(
                        appliedEffectHandle);
                }
            }

            return appliedEffectHandles;
        }
    }
}