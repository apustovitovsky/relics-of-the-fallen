using System;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace GAS
{
    public interface IAbilitySystemActorSpawner
    {
        /// <summary>
        /// Instantiates an actor before its spawn data has been finalized.
        /// </summary>
        GameObject InstantiateActor(
            GameObject actorPrefab);

        /// <summary>
        /// Completes the actor spawn after its runtime data has been initialized.
        /// </summary>
        void FinishSpawningActor(
            GameObject spawnedActor);
    }

    internal sealed class UnityAbilitySystemActorSpawner :
        IAbilitySystemActorSpawner
    {
        internal static UnityAbilitySystemActorSpawner Instance
        {
            get;
        } = new();

        private UnityAbilitySystemActorSpawner()
        {
        }

        /// <summary>
        /// Instantiates an actor for an ordinary non-networked Unity world.
        /// </summary>
        public GameObject InstantiateActor(
            GameObject actorPrefab)
        {
            if (actorPrefab == null)
            {
                throw new ArgumentNullException(
                    nameof(actorPrefab));
            }

            return UnityObject.Instantiate(
                actorPrefab);
        }

        /// <summary>
        /// Activates an initialized actor in an ordinary non-networked Unity world.
        /// </summary>
        public void FinishSpawningActor(
            GameObject spawnedActor)
        {
            if (spawnedActor == null)
            {
                throw new ArgumentNullException(
                    nameof(spawnedActor));
            }

            spawnedActor.SetActive(
                true);
        }
    }
}