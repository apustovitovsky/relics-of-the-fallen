using System;
using System.Collections.Generic;
using GAS;
using GAS.Common;
using RelicsOfTheFallen.Targeting;
using RelicsOfTheFallen.Character;
using UnityEngine;

namespace RelicsOfTheFallen.Abilities
{
    /// <summary>
    /// Casts a configurable projectile toward the target selected during activation.
    /// </summary>
    [Serializable]
    public sealed class ProjectileAbility :
        CommonGameplayAbility
    {
        [field: SerializeField]
        private ScalableFloat CastTime
        {
            get;
            set;
        } = new(1.5f);

        [field: SerializeField]
        private List<GameplayEffectSO> HitGameplayEffects
        {
            get;
            set;
        } = new();

        [field: SerializeField]
        private GameplayAbilityMontage CastMontage
        {
            get;
            set;
        }

        [field: SerializeField]
        private GameplayTag CastReleaseEventTag
        {
            get;
            set;
        }

        [field: SerializeField]
        private GameObject ProjectilePrefab
        {
            get;
            set;
        }


        [field: SerializeField]
        private float ProjectileSpeed
        {
            get;
            set;
        } = 12f;

        [field: SerializeField]
        private float ProjectileLifetime
        {
            get;
            set;
        } = 5f;

        private IDisposable m_TargetDataSetSubscription;
        private IDisposable m_TargetDataCancelledSubscription;

        private GameplayAbilityTargetDataHandle m_TargetData;

        /// <summary>
        /// Creates a runtime projectile ability instance preserving its configured gameplay data.
        /// </summary>
        public override GameplayAbility Instantiate(
            AbilitySystemComponent owner)
        {
            ProjectileAbility ability =
                (ProjectileAbility)base.Instantiate(
                    owner);

            ability.CastTime = CastTime;

            ability.HitGameplayEffects =
                new List<GameplayEffectSO>(
                    HitGameplayEffects);

            ability.CastMontage = CastMontage;
            ability.CastReleaseEventTag = CastReleaseEventTag;
            ability.ProjectilePrefab = ProjectilePrefab;
            ability.ProjectileSpeed = ProjectileSpeed;
            ability.ProjectileLifetime = ProjectileLifetime;

            return ability;
        }

        /// <summary>
        /// Acquires local target data or waits for target data produced by the owning client.
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

            if (
                ShouldWaitForReplicatedTargetData(
                    actorInfo,
                    activationInfo))
            {
                RegisterReplicatedTargetDataCallbacks(
                    actorInfo.AbilitySystemComponent,
                    handle,
                    activationInfo);

                return;
            }

            GameplayAbilityTargetDataHandle targetData =
                CreateTargetData(
                    actorInfo);

            if (targetData == null)
            {
                HandleTargetDataCancelled();
                return;
            }

            HandleTargetDataReady(
                targetData);
        }

        /// <summary>
        /// Releases target-data callbacks before ending the current activation.
        /// </summary>
        public override void EndAbility(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo,
            bool replicateEndAbility,
            bool wasCancelled)
        {
            DisposeTargetDataSubscriptions();

            m_TargetData =
                null;

            base.EndAbility(
                handle,
                actorInfo,
                activationInfo,
                replicateEndAbility,
                wasCancelled);
        }

        /// <summary>
        /// Creates actor-array target data from the avatar's currently selected target.
        /// </summary>
        private static GameplayAbilityTargetDataHandle CreateTargetData(
            GameplayAbilityActorInfo actorInfo)
        {
            TargetingController targeting =
                actorInfo.AvatarActor.GetComponentInChildren<TargetingController>();

            if (targeting == null)
            {
                return null;
            }

            ITargetable currentTarget =
                targeting.CurrentTarget;

            if (
                currentTarget == null ||
                currentTarget.TargetActor == null)
            {
                return null;
            }

            GameplayAbilityTargetData_ActorArray actorArray =
                new(
                    currentTarget.TargetActor);

            return new GameplayAbilityTargetDataHandle(
                actorArray);
        }

        /// <summary>
        /// Registers callbacks used by authority while awaiting client-produced target data.
        /// </summary>
        private void RegisterReplicatedTargetDataCallbacks(
            AbilitySystemComponent abilitySystem,
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActivationInfo activationInfo)
        {
            PredictionKey predictionKey =
                activationInfo.GetActivationPredictionKey();

            m_TargetDataSetSubscription =
                abilitySystem.AbilityTargetDataSetDelegate(
                    handle,
                    predictionKey,
                    HandleReplicatedTargetDataReady);

            m_TargetDataCancelledSubscription =
                abilitySystem.AbilityTargetDataCancelledDelegate(
                    handle,
                    predictionKey,
                    HandleTargetDataCancelled);

            abilitySystem.CallReplicatedTargetDataDelegatesIfSet(
                handle,
                predictionKey);
        }

        /// <summary>
        /// Forwards replicated target data into the shared fireball cast pipeline.
        /// </summary>
        private void HandleReplicatedTargetDataReady(
            GameplayAbilityTargetDataHandle targetData,
            GameplayTag _)
        {
            HandleTargetDataReady(
                targetData);
        }

        /// <summary>
        /// Replicates accepted target data and starts the predicted casting phase.
        /// </summary>
        private void HandleTargetDataReady(
            GameplayAbilityTargetDataHandle targetData)
        {
            if (!HasValidTargetData(targetData))
            {
                HandleTargetDataCancelled();
                return;
            }

            DisposeTargetDataSubscriptions();

            AbilitySystemComponent abilitySystem =
                CurrentActorInfo.AbilitySystemComponent;

            PredictionKey predictionKey =
                CurrentActivationInfo.GetActivationPredictionKey();

            if (
                ShouldReplicateTargetDataToServer(
                    CurrentActorInfo,
                    CurrentActivationInfo))
            {
                abilitySystem.CallServerSetReplicatedTargetData(
                    CurrentSpecHandle,
                    predictionKey,
                    targetData,
                    null,
                    predictionKey);
            }

            if (
                ShouldWaitForReplicatedTargetData(
                    CurrentActorInfo,
                    CurrentActivationInfo))
            {
                abilitySystem.ConsumeClientReplicatedTargetData(
                    CurrentSpecHandle,
                    predictionKey);
            }

            m_TargetData =
                targetData;

            if (TryStartCasting())
            {
                return;
            }

            EndAbility(
                CurrentSpecHandle,
                CurrentActorInfo,
                CurrentActivationInfo,
                true,
                true);
        }

        /// <summary>
        /// Starts an interruptible montage and waits for its configured cast release event.
        /// </summary>
        private bool TryStartCasting()
        {
            if (CastMontage == null ||
                CastMontage.Animation == null)
            {
                Debug.LogError(
                    $"{nameof(ProjectileAbility)} requires a casting montage.");

                return false;
            }

            if (CastReleaseEventTag == null)
            {
                Debug.LogError(
                    $"{nameof(ProjectileAbility)} requires a cast release event tag.");

                return false;
            }

            int abilityLevel = GetAbilityLevel(
                CurrentSpecHandle,
                CurrentActorInfo);

            float castDuration = CastTime.GetValueAtLevel(
                abilityLevel);

            if (castDuration <= 0f)
            {
                Debug.LogError(
                    $"{nameof(ProjectileAbility)} requires a positive cast duration.");

                return false;
            }

            if (!TryGetCastReleaseTime(
                    out float castReleaseTime))
            {
                Debug.LogError(
                    $"{nameof(ProjectileAbility)} montage " +
                    $"'{CastMontage.name}' has no animation event for " +
                    $"'{CastReleaseEventTag.name}'.");

                return false;
            }

            float montageRate =
                castReleaseTime /
                castDuration;

            AbilityTask_WaitGameplayEvent castReleaseTask =
                AbilityTask_WaitGameplayEvent.WaitGameplayEvent(
                    this,
                    CastReleaseEventTag,
                    onlyTriggerOnce: true);

            castReleaseTask.RegisterEventReceivedDelegate(
                HandleCastReleased);

            castReleaseTask.ReadyForActivation();

            AbilityTask_PlayMontageAndWait montageTask =
                AbilityTask_PlayMontageAndWait
                    .CreatePlayMontageAndWaitProxy(
                        this,
                        nameof(ProjectileAbility),
                        CastMontage,
                        rate: montageRate,
                        stopWhenAbilityEnds: false);

            montageTask.RegisterCompletedDelegate(
                HandleCastMontageCancelled);

            montageTask.RegisterInterruptedDelegate(
                HandleCastMontageCancelled);

            montageTask.RegisterCancelledDelegate(
                HandleCastMontageCancelled);

            montageTask.ReadyForActivation();

            return IsActive;
        }

        /// <summary>
        /// Finds the animation time associated with the configured cast release event.
        /// </summary>
        private bool TryGetCastReleaseTime(
            out float castReleaseTime)
        {
            AnimationEvent[] animationEvents =
                CastMontage.Animation.events;

            for (
                int index = 0;
                index < animationEvents.Length;
                index++)
            {
                AnimationEvent animationEvent =
                    animationEvents[index];

                if (animationEvent.functionName !=
                        nameof(
                            GameplayEventAnimationEventHandler
                                .HandleGameplayEvent) ||
                    animationEvent.objectReferenceParameter !=
                        CastReleaseEventTag)
                {
                    continue;
                }

                castReleaseTime =
                    animationEvent.time;

                return castReleaseTime > 0f;
            }

            castReleaseTime = 0f;

            return false;
        }

        /// <summary>
        /// Cancels the cast when its montage is interrupted or cannot begin playback.
        /// </summary>
        private void HandleCastMontageCancelled()
        {
            if (!IsActive)
            {
                return;
            }

            EndAbility(
                CurrentSpecHandle,
                CurrentActorInfo,
                CurrentActivationInfo,
                true,
                true);
        }

        /// <summary>
        /// Commits the released cast and spawns its authoritative projectile.
        /// </summary>
        private void HandleCastReleased(
            GameplayEventData payload)
        {
            if (!IsActive ||
                !CurrentActorInfo.IsNetAuthority())
            {
                return;
            }

            bool wasCommitted = CommitAbility(
                CurrentSpecHandle,
                CurrentActorInfo,
                CurrentActivationInfo);

            bool wasSpawned =
                wasCommitted &&
                TrySpawnProjectile();

            EndAbility(
                CurrentSpecHandle,
                CurrentActorInfo,
                CurrentActivationInfo,
                true,
                !wasSpawned);
        }

        /// <summary>
        /// Creates and initializes the server-authoritative projectile actor.
        /// </summary>
        private bool TrySpawnProjectile()
        {
            if (
                ProjectilePrefab == null ||
                HitGameplayEffects == null ||
                HitGameplayEffects.Count == 0)
            {
                Debug.LogError(
                    $"{nameof(ProjectileAbility)} requires a projectile prefab and hit effects.");

                return false;
            }

            for (
                int index = 0;
                index < HitGameplayEffects.Count;
                index++)
            {
                if (HitGameplayEffects[index] != null)
                {
                    continue;
                }

                Debug.LogError(
                    $"{nameof(ProjectileAbility)} contains an empty hit effect.");

                return false;
            }

            if (
                ProjectileSpeed <= 0f ||
                ProjectileLifetime <= 0f)
            {
                Debug.LogError(
                    $"{nameof(ProjectileAbility)} requires positive projectile settings.");

                return false;
            }

            if (
                !ProjectilePrefab.TryGetComponent(
                    out GameplayProjectile _))
            {
                Debug.LogError(
                    $"{nameof(ProjectileAbility)} requires a projectile component on its prefab.");

                return false;
            }

            if (
                !TryGetProjectileSpawnPose(
                    out Pose spawnPose,
                    out Vector3 direction))
            {
                return false;
            }

            GameplayAbilityTargetingLocationInfo spawnLocation =
                new(
                    spawnPose);

            GameplayAbilityTargetData_LocationInfo spawnData =
                new(
                    spawnLocation,
                    spawnLocation);

            GameplayAbilityTargetDataHandle spawnTargetData =
                new(
                    spawnData);

            AbilityTask_SpawnActor spawnTask =
                AbilityTask_SpawnActor.SpawnActor(
                    this,
                    spawnTargetData,
                    ProjectilePrefab);

            spawnTask.ReadyForActivation();

            if (
                !spawnTask.BeginSpawningActor(
                    out GameObject spawnedActor))
            {
                return false;
            }

            GameplayEffectContextHandle effectContext =
                MakeEffectContext(
                    CurrentSpecHandle,
                    CurrentActorInfo);

            effectContext.AddInstigator(
                CurrentActorInfo.OwnerActor,
                spawnedActor);

            int abilityLevel =
                GetAbilityLevel(
                    CurrentSpecHandle,
                    CurrentActorInfo);

            AbilitySystemComponent abilitySystem =
                CurrentActorInfo.AbilitySystemComponent;

            List<GameplayEffectSpec> gameplayEffectSpecs =
                new(
                    HitGameplayEffects.Count);

            for (
                int index = 0;
                index < HitGameplayEffects.Count;
                index++)
            {
                GameplayEffectSpec gameplayEffectSpec =
                    abilitySystem.MakeOutgoingSpec(
                        HitGameplayEffects[index],
                        abilityLevel,
                        effectContext);

                gameplayEffectSpecs.Add(
                    gameplayEffectSpec);
            }

            GameplayProjectile projectile =
                spawnedActor.GetComponent<GameplayProjectile>();

            projectile.Initialize(
                CurrentActorInfo.AvatarActor,
                direction,
                ProjectileSpeed,
                ProjectileLifetime,
                gameplayEffectSpecs);

            spawnTask.FinishSpawningActor(
                spawnedActor);

            return true;
        }

        /// <summary>
        /// Resolves the animated projectile socket and freezes its direction toward the target.
        /// </summary>
        private bool TryGetProjectileSpawnPose(
            out Pose spawnPose,
            out Vector3 direction)
        {
            spawnPose = Pose.identity;
            direction = Vector3.zero;

            GameObject targetActor = GetFirstTargetActor();

            if (targetActor == null)
            {
                return false;
            }

            GameObject avatarActor = CurrentActorInfo.AvatarActor;

            if (
                targetActor.transform.root ==
                avatarActor.transform.root)
            {
                return false;
            }

            TargetingComponent target =
                targetActor.GetComponentInChildren<TargetingComponent>(true);

            if (
                target == null ||
                !target.IsTargetable ||
                target.TargetAnchor == null)
            {
                return false;
            }

            CharacterSockets sockets =
                avatarActor.GetComponentInChildren<CharacterSockets>(true);

            if (
                sockets == null ||
                sockets.ProjectileOrigin == null)
            {
                Debug.LogError(
                    $"{nameof(ProjectileAbility)} requires a projectile origin socket.");

                return false;
            }

            Vector3 originPosition =
                sockets.ProjectileOrigin.position;

            direction =
                target.TargetAnchor.position -
                originPosition;

            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            direction.Normalize();

            spawnPose =
                new Pose(
                    originPosition,
                    Quaternion.LookRotation(
                        direction,
                        Vector3.up));

            return true;
        }

        /// <summary>
        /// Returns the first live target actor retained by the current activation.
        /// </summary>
        private GameObject GetFirstTargetActor()
        {
            if (m_TargetData == null)
            {
                return null;
            }

            for (
                int dataIndex = 0;
                dataIndex < m_TargetData.Num();
                dataIndex++)
            {
                GameplayAbilityTargetData targetData =
                    m_TargetData.Get(
                        dataIndex);

                if (targetData == null)
                {
                    continue;
                }

                IReadOnlyList<GameObject> targetActors =
                    targetData.GetActors();

                for (
                    int actorIndex = 0;
                    actorIndex < targetActors.Count;
                    actorIndex++)
                {
                    GameObject targetActor =
                        targetActors[actorIndex];

                    if (targetActor != null)
                    {
                        return targetActor;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Cancels activation and forwards locally produced cancellation when required.
        /// </summary>
        private void HandleTargetDataCancelled()
        {
            DisposeTargetDataSubscriptions();

            PredictionKey predictionKey =
                CurrentActivationInfo.GetActivationPredictionKey();

            if (
                ShouldReplicateTargetDataToServer(
                    CurrentActorInfo,
                    CurrentActivationInfo))
            {
                CurrentActorInfo
                    .AbilitySystemComponent
                    .ServerSetReplicatedTargetDataCancelled(
                        CurrentSpecHandle,
                        predictionKey,
                        predictionKey);
            }

            EndAbility(
                CurrentSpecHandle,
                CurrentActorInfo,
                CurrentActivationInfo,
                true,
                true);
        }

        /// <summary>
        /// Returns whether the target-data handle contains at least one live gameplay actor.
        /// </summary>
        private static bool HasValidTargetData(
            GameplayAbilityTargetDataHandle targetData)
        {
            if (targetData == null)
            {
                return false;
            }

            for (
                int dataIndex = 0;
                dataIndex < targetData.Num();
                dataIndex++)
            {
                GameplayAbilityTargetData data =
                    targetData.Get(
                        dataIndex);

                if (data == null)
                {
                    continue;
                }

                IReadOnlyList<GameObject> targetActors =
                    data.GetActors();

                for (
                    int actorIndex = 0;
                    actorIndex < targetActors.Count;
                    actorIndex++)
                {
                    if (targetActors[actorIndex] != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns whether authority must wait for target data produced by a remote client.
        /// </summary>
        private static bool ShouldWaitForReplicatedTargetData(
            GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo)
        {
            PredictionKey predictionKey =
                activationInfo.GetActivationPredictionKey();

            return
                actorInfo.IsNetAuthority() &&
                !actorInfo.IsLocallyControlled() &&
                predictionKey.IsValid;
        }

        /// <summary>
        /// Returns whether locally produced target data must be forwarded to authority.
        /// </summary>
        private static bool ShouldReplicateTargetDataToServer(
            GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo)
        {
            PredictionKey predictionKey =
                activationInfo.GetActivationPredictionKey();

            return
                !actorInfo.IsNetAuthority() &&
                actorInfo.IsLocallyControlled() &&
                predictionKey.IsValid;
        }

        /// <summary>
        /// Releases callbacks registered while authority awaited replicated target data.
        /// </summary>
        private void DisposeTargetDataSubscriptions()
        {
            m_TargetDataSetSubscription?.Dispose();

            m_TargetDataSetSubscription = null;

            m_TargetDataCancelledSubscription?.Dispose();

            m_TargetDataCancelledSubscription = null;
        }
    }
}