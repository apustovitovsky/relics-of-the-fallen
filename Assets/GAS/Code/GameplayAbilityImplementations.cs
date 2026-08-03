
using UnityEngine;
using System;

namespace GAS
{

    [Serializable]
    public class InstantAbility : GameplayAbility
    {

        /// <summary>
        /// Applies all configured effects to directly produced actor target data and ends the ability.
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
                new(targetActorData);

            ApplyGameplayEffects(
                source,
                targetData,
                activationGUID);

            DeactivateAbility(
                activationGUID);
        }
    }

    [Serializable]
    public class ProjectileAbility : GameplayAbility
    {
        public GameObject projectilePrefab = null;
        public GameObject projectile = null;
        public string projectileName = "";

        public override void SerializeAdditionalData()
        { //Searches a projectile prefab by its name. Prefab must be in root level of a Resources folder. You can also use any other way of referencing to it here. e.g. A scriptableObject or some other list.
            base.SerializeAdditionalData();
            if (projectilePrefab != null)
            {
                projectileName = projectilePrefab != null ? projectilePrefab.name : null;
            }

        }
        public override void DeserializeAdditionalData()
        {
            base.DeserializeAdditionalData();
            projectilePrefab = Resources.Load<GameObject>(projectileName);
        }

        public override GameplayAbility Instantiate(AbilitySystemComponent asc)
        {
            ProjectileAbility newInstance = (ProjectileAbility)base.Instantiate(asc);
            newInstance.projectilePrefab = projectilePrefab;
            return newInstance;
        }

        /// <summary>
        /// Spawns a projectile that produces actor target data and applies effects on impact.
        /// </summary>
        public override void ActivateAbility(
            AbilitySystemComponent source,
            AbilitySystemComponent target,
            string activationGUID)
        {
            void ApplyEffectsOnHit(
                AbilitySystemComponent hitAbilitySystem)
            {
                GameplayAbilityTargetData targetActorData =
                    new GameplayAbilityTargetData_ActorArray(
                        hitAbilitySystem.AbilityActorInfo.OwnerActor);

                GameplayAbilityTargetDataHandle targetData =
                    new(targetActorData);

                ApplyGameplayEffects(
                    source,
                    targetData,
                    activationGUID);
            }

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

            projectile =
                projectilePrefab == null
                    ? GameObject.CreatePrimitive(
                        PrimitiveType.Sphere)
                    : GameObject.Instantiate(
                        projectilePrefab);

            projectile.name =
                "projectile";

            projectile.transform.SetPositionAndRotation(
                source.transform.position +
                source.transform.forward,
                source.transform.rotation);

            Rigidbody rigidbody =
                projectile.AddComponent<Rigidbody>();

            rigidbody.linearDamping =
                0f;

            rigidbody.useGravity =
                false;

            rigidbody.AddForce(
                rigidbody.transform.forward * 10f,
                ForceMode.VelocityChange);

            Projectile projectileComponent =
                projectile.AddComponent<Projectile>();

            projectileComponent.OnHit +=
                ApplyEffectsOnHit;

            projectileComponent.source =
                source;

            base.DeactivateAbility(
                activationGUID);
        }
    }

    public class Projectile : MonoBehaviour
    {
        public float speed;
        public Action<AbilitySystemComponent> OnHit;
        public AbilitySystemComponent source;

        private void Start()
        {
            Destroy(gameObject, 30f);
        }
        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.GetComponent<AbilitySystemComponent>() != null && other.gameObject.GetComponent<AbilitySystemComponent>() != source)
            {
                OnHit?.Invoke(other.gameObject.GetComponent<AbilitySystemComponent>());
                Destroy(gameObject);
            }
        }

    }

    [Serializable]
    public class TargetedProjectileAbility : GameplayAbility
    {
        /// <summary>
        /// Spawns a targeted projectile that produces actor target data on impact.
        /// </summary>
        public override void ActivateAbility(
            AbilitySystemComponent source,
            AbilitySystemComponent target,
            string activationGUID)
        {
            void ApplyEffectsOnHit()
            {
                GameplayAbilityTargetData targetActorData =
                    new GameplayAbilityTargetData_ActorArray(
                        target.AbilityActorInfo.OwnerActor);

                GameplayAbilityTargetDataHandle targetData =
                    new(targetActorData);

                ApplyGameplayEffects(
                    source,
                    targetData,
                    activationGUID);
            }

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

            GameObject projectile =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere);

            projectile.name =
                "projectile";

            projectile.transform.SetPositionAndRotation(
                source.transform.position + source.transform.forward,
                source.transform.rotation);

            TargetedProjectile projectileComponent =
                projectile.AddComponent<TargetedProjectile>();

            projectileComponent.speed =
                15f;

            projectileComponent.target =
                target.transform;

            projectileComponent.OnHit +=
                ApplyEffectsOnHit;

            base.DeactivateAbility(
                activationGUID);
        }
    }

    [RequireComponent(typeof(Collider))]
    public class TargetedProjectile : MonoBehaviour
    {
        public float speed;
        public Transform target;
        public Action OnHit;

        public Rigidbody rb;

        public float t = 0;
        public float turnRate = 80f;

        private void Start()
        {
            Destroy(gameObject, 30f);
            if (GetComponent<Collider>() != null)
            {
                Destroy(GetComponent<Collider>());
            }

            rb = gameObject.AddComponent<Rigidbody>();
            rb.linearDamping = 0;
            rb.useGravity = false;
            rb.linearVelocity = transform.forward * speed;

            TrailRenderer trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = .3f;
            trail.startWidth = 1;
            trail.endWidth = 0;
        }
        private void FixedUpdate()
        {
            t += Time.fixedDeltaTime;

            if (target == null)
            {
                return;
            }

            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(target.position - transform.position), Time.fixedDeltaTime * turnRate * t);
            rb.linearVelocity = transform.forward * speed;

            if (Vector3.Distance(transform.position, target.position) < 0.6f)
            {
                OnHit?.Invoke();
                Destroy(gameObject);
            }
        }

    }

}