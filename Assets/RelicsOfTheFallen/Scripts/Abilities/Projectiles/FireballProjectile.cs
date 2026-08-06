using System;
using GAS;
using Mirror;
using UnityEngine;

namespace RelicsOfTheFallen.Abilities
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class FireballProjectile :
        NetworkBehaviour
    {
        [field: SerializeField]
        private GameplayTag ImpactGameplayCue
        {
            get;
            set;
        }

        private Rigidbody m_Rigidbody;
        private GameObject m_SourceActor;
        private AbilitySystemComponent m_SourceAbilitySystem;
        private GameplayEffectSpec m_DamageSpec;
        private Vector3 m_Direction;
        private float m_Speed;
        private float m_RemainingLifetime;
        private bool m_IsInitialized;
        private bool m_HasImpacted;

        private void Awake()
        {
            m_Rigidbody =
                GetComponent<Rigidbody>();
        }

        /// <summary>
        /// Initializes authoritative movement, source ability system, and damage data.
        /// </summary>
        public void Initialize(
            GameObject sourceActor,
            Vector3 direction,
            float speed,
            float lifetime,
            GameplayEffectSpec damageSpec)
        {
            if (sourceActor == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceActor));
            }

            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                throw new ArgumentException(
                    "A projectile direction must be non-zero.",
                    nameof(direction));
            }

            if (speed <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(speed),
                    speed,
                    "A projectile speed must be positive.");
            }

            if (lifetime <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lifetime),
                    lifetime,
                    "A projectile lifetime must be positive.");
            }

            if (damageSpec == null)
            {
                throw new ArgumentNullException(
                    nameof(damageSpec));
            }

            if (ImpactGameplayCue == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(FireballProjectile)} requires an impact gameplay cue.");
            }

            GameObject sourceActorRoot =
                sourceActor.transform.root.gameObject;

            if (
                !AbilitySystemGlobals.TryGetAbilitySystemComponentFromActor(
                    sourceActorRoot,
                    out AbilitySystemComponent sourceAbilitySystem))
            {
                throw new InvalidOperationException(
                    $"{sourceActorRoot.name} has no ability system component.");
            }

            m_SourceActor =
                sourceActorRoot;

            m_SourceAbilitySystem =
                sourceAbilitySystem;

            m_Direction =
                direction.normalized;

            m_Speed =
                speed;

            m_RemainingLifetime =
                lifetime;

            m_DamageSpec =
                damageSpec;

            m_IsInitialized =
                true;
        }

        [ServerCallback]
        private void FixedUpdate()
        {
            if (
                !m_IsInitialized ||
                m_HasImpacted)
            {
                return;
            }

            float deltaTime = Time.fixedDeltaTime;

            m_RemainingLifetime -= deltaTime;

            if (m_RemainingLifetime <= 0f)
            {
                DestroyProjectile();
                return;
            }

            Vector3 nextPosition =
                m_Rigidbody.position +
                m_Direction *
                (m_Speed * deltaTime);

            m_Rigidbody.MovePosition(
                nextPosition);
        }

        [ServerCallback]
        private void OnTriggerEnter(
     Collider other)
        {
            if (
                !m_IsInitialized ||
                m_HasImpacted)
            {
                return;
            }

            GameObject hitActor =
                other.transform.root.gameObject;

            if (hitActor == m_SourceActor)
            {
                return;
            }

            bool hasTargetAbilitySystem =
                AbilitySystemGlobals
                    .TryGetAbilitySystemComponentFromActor(
                        hitActor,
                        out AbilitySystemComponent targetAbilitySystem);

            if (
                other.isTrigger &&
                !hasTargetAbilitySystem)
            {
                return;
            }

            m_HasImpacted = true;

            try
            {
                if (hasTargetAbilitySystem)
                {
                    targetAbilitySystem.ApplyGameplayEffectSpecToSelf(
                        m_DamageSpec);
                }

                ExecuteImpactGameplayCue(
                    other);
            }
            finally
            {
                DestroyProjectile();
            }
        }

        /// <summary>
        /// Executes the fireball impact cue at the authoritative collision location.
        /// </summary>
        private void ExecuteImpactGameplayCue(
            Collider hitCollider)
        {
            GameplayCueParameters parameters =
                new(
                    m_DamageSpec.EffectContext)
                {
                    NormalizedMagnitude = 1f,
                    Location = hitCollider.ClosestPoint(
                        m_Rigidbody.position),
                    Normal = -m_Direction,
                    GameplayEffectLevel = Mathf.RoundToInt(
                        m_DamageSpec.Level),
                    AbilityLevel =
                        m_DamageSpec.EffectContext.GetAbilityLevel()
                };

            m_SourceAbilitySystem.ExecuteGameplayCue(
                ImpactGameplayCue,
                parameters);
        }

        private void DestroyProjectile()
        {
            NetworkServer.Destroy(
                gameObject);
        }
    }
}