using System;
using UnityEngine;

namespace GAS
{
    public sealed class AbilityTask_SpawnActor :
        AbilityTask
    {
        private readonly GameplayAbilityTargetDataHandle
            m_CachedTargetDataHandle;

        private readonly GameObject m_ActorPrefab;

        private readonly DisposableEvent<GameObject>
            m_SuccessDelegate = new();

        private readonly DisposableEvent<GameObject>
            m_DidNotSpawnDelegate = new();

        private readonly DisposableGroup m_Subscriptions =
            new();

        private AbilityTask_SpawnActor(
            GameplayAbility owningAbility,
            GameplayAbilityTargetDataHandle targetData,
            GameObject actorPrefab)
            : base(
                owningAbility)
        {
            m_CachedTargetDataHandle = targetData ??
                throw new ArgumentNullException(
                    nameof(targetData));

            if (actorPrefab == null)
            {
                throw new ArgumentNullException(
                    nameof(actorPrefab));
            }

            m_ActorPrefab = actorPrefab;
        }

        /// <summary>
        /// Creates an authority-only task for spawning a gameplay actor.
        /// </summary>
        public static AbilityTask_SpawnActor SpawnActor(
            GameplayAbility owningAbility,
            GameplayAbilityTargetDataHandle targetData,
            GameObject actorPrefab)
        {
            return new AbilityTask_SpawnActor(
                owningAbility,
                targetData,
                actorPrefab);
        }

        /// <summary>
        /// Registers a callback invoked after an actor has been spawned successfully.
        /// </summary>
        public IDisposable RegisterSuccessDelegate(
            Action<GameObject> handler)
        {
            IDisposable subscription =
                m_SuccessDelegate.Subscribe(handler);

            m_Subscriptions.Add(subscription);

            return subscription;
        }

        /// <summary>
        /// Registers a callback invoked when the authority cannot spawn an actor.
        /// </summary>
        public IDisposable RegisterDidNotSpawnDelegate(
            Action<GameObject> handler)
        {
            IDisposable subscription =
                m_DidNotSpawnDelegate.Subscribe(handler);

            m_Subscriptions.Add(subscription);

            return subscription;
        }

        /// <summary>
        /// Instantiates an actor on authority and exposes it for runtime initialization.
        /// </summary>
        public bool BeginSpawningActor(
            out GameObject spawnedActor)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException(
                    "The spawn task must be activated before spawning an actor.");
            }

            spawnedActor = null;

            if (IsEnded)
            {
                return false;
            }

            GameplayAbilityActorInfo actorInfo =
                Ability.CurrentActorInfo;

            if (
                actorInfo == null ||
                !actorInfo.IsNetAuthority())
            {
                BroadcastDidNotSpawn();

                return false;
            }

            try
            {
                spawnedActor =
                    AbilitySystemComponent
                        .ActorSpawner
                        .InstantiateActor(m_ActorPrefab);
            }
            catch
            {
                EndTask();

                throw;
            }

            if (spawnedActor != null)
            {
                return true;
            }

            BroadcastDidNotSpawn();

            return false;
        }

        /// <summary>
        /// Applies the target transform and completes the authoritative actor spawn.
        /// </summary>
        public void FinishSpawningActor(
            GameObject spawnedActor)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException(
                    "The spawn task must be active while finishing an actor spawn.");
            }

            if (spawnedActor == null)
            {
                BroadcastDidNotSpawn();

                return;
            }

            Pose spawnPose = GetSpawnPose();

            spawnedActor.transform.SetPositionAndRotation(
                spawnPose.position,
                spawnPose.rotation);

            try
            {
                AbilitySystemComponent
                    .ActorSpawner
                    .FinishSpawningActor(spawnedActor);

                m_SuccessDelegate.Invoke(spawnedActor);
            }
            finally
            {
                EndTask();
            }
        }

        /// <summary>
        /// Releases spawn callbacks when the task or owning ability ends.
        /// </summary>
        protected override void OnDestroy(
            bool abilityEnded)
        {
            m_Subscriptions.Dispose();
            m_SuccessDelegate.Clear();
            m_DidNotSpawnDelegate.Clear();

            base.OnDestroy(abilityEnded);
        }

        /// <summary>
        /// Resolves the spawn transform from target data or the owning actor.
        /// </summary>
        private Pose GetSpawnPose()
        {
            GameplayAbilityTargetData locationData =
                m_CachedTargetDataHandle.Get(0);

            if (
                locationData != null &&
                locationData.HasEndPoint())
            {
                return locationData.GetEndPointTransform();
            }

            Transform ownerTransform =
                AbilitySystemComponent
                    .AbilityActorInfo
                    .OwnerActor
                    .transform;

            return new Pose(
                ownerTransform.position,
                ownerTransform.rotation);
        }

        /// <summary>
        /// Broadcasts spawn failure and ends this task.
        /// </summary>
        private void BroadcastDidNotSpawn()
        {
            try
            {
                m_DidNotSpawnDelegate.Invoke(null);
            }
            finally
            {
                EndTask();
            }
        }
    }
}