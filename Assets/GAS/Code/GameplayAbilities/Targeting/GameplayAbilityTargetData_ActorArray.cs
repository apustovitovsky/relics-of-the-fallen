using System;
using System.Collections.Generic;
using UnityEngine;

namespace GAS
{
    public sealed class GameplayAbilityTargetData_ActorArray :
        GameplayAbilityTargetData
    {
        private readonly List<GameObject> m_TargetActorArray =
            new();

        public IReadOnlyList<GameObject> TargetActorArray =>
            m_TargetActorArray;

        /// <summary>
        /// Creates an empty actor-array targeting payload.
        /// </summary>
        public GameplayAbilityTargetData_ActorArray()
        {
        }

        /// <summary>
        /// Creates an actor-array targeting payload containing one gameplay actor.
        /// </summary>
        public GameplayAbilityTargetData_ActorArray(
            GameObject targetActor)
        {
            AddActor(
                targetActor);
        }

        /// <summary>
        /// Adds one gameplay actor to this targeting payload.
        /// </summary>
        public void AddActor(
            GameObject targetActor)
        {
            if (targetActor == null)
            {
                throw new ArgumentNullException(
                    nameof(targetActor));
            }

            m_TargetActorArray.Add(
                targetActor);
        }

        /// <summary>
        /// Returns whether the actor array contains at least one valid endpoint.
        /// </summary>
        public override bool HasEndPoint()
        {
            for (
                int index = 0;
                index < m_TargetActorArray.Count;
                index++)
            {
                if (m_TargetActorArray[index] != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the position of the first valid targeted actor.
        /// </summary>
        public override Pose GetEndPointTransform()
        {
            for (
                int index = 0;
                index < m_TargetActorArray.Count;
                index++)
            {
                GameObject targetActor =
                    m_TargetActorArray[index];

                if (targetActor == null)
                {
                    continue;
                }

                return new Pose(
                    targetActor.transform.position,
                    Quaternion.identity);
            }

            return Pose.identity;
        }

        /// <summary>
        /// Returns the gameplay actors represented by this targeting payload.
        /// </summary>
        public override IReadOnlyList<GameObject> GetActors()
        {
            return m_TargetActorArray;
        }
    }
}