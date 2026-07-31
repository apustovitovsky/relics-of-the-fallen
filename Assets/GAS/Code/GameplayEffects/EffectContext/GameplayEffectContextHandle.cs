using System;
using UnityEngine;

namespace GAS
{
    public struct GameplayEffectContextHandle
    {
        private GameplayEffectContext m_Data;

        public readonly bool IsValid =>
            m_Data != null;

        /// <summary>
        /// Creates a handle that owns one gameplay effect context reference.
        /// </summary>
        public GameplayEffectContextHandle(
            GameplayEffectContext data)
        {
            m_Data =
                data ?? throw new ArgumentNullException(
                    nameof(data));
        }

        /// <summary>
        /// Returns the gameplay effect context referenced by this handle.
        /// </summary>
        public readonly GameplayEffectContext Get()
        {
            return m_Data;
        }

        /// <summary>
        /// Releases the gameplay effect context referenced by this handle.
        /// </summary>
        public void Clear()
        {
            m_Data = null;
        }

        /// <summary>
        /// Creates a handle containing an independent copy of the referenced context.
        /// </summary>
        public GameplayEffectContextHandle Duplicate()
        {
            if (!IsValid)
            {
                return default;
            }

            return new GameplayEffectContextHandle(
                m_Data.Duplicate());
        }

        /// <summary>
        /// Sets the gameplay ability that created the referenced effect context.
        /// </summary>
        public void SetAbility(
            GameplayAbility ability)
        {
            if (!IsValid)
            {
                return;
            }

            m_Data.SetAbility(
                ability);
        }

        /// <summary>
        /// Returns the persistent ability definition stored by the referenced context.
        /// </summary>
        public GameplayAbilitySO GetAbility()
        {
            return IsValid
                ? m_Data.GetAbility()
                : null;
        }

        /// <summary>
        /// Returns the non-replicated runtime ability instance stored by the referenced context.
        /// </summary>
        public GameplayAbility GetAbilityInstance_NotReplicated()
        {
            return IsValid
                ? m_Data.GetAbilityInstance_NotReplicated()
                : null;
        }

        /// <summary>
        /// Returns the ability level captured by the referenced gameplay effect context.
        /// </summary>
        public int GetAbilityLevel()
        {
            return IsValid
                ? m_Data.GetAbilityLevel()
                : 1;
        }

        /// <summary>
        /// Sets the immediate instigator and effect causer on the referenced context.
        /// </summary>
        public void AddInstigator(
            GameObject instigator,
            GameObject effectCauser)
        {
            if (!IsValid)
            {
                return;
            }

            m_Data.AddInstigator(
                instigator,
                effectCauser);
        }

        /// <summary>
        /// Returns the immediate instigator stored by the referenced gameplay effect context.
        /// </summary>
        public GameObject GetInstigator()
        {
            return IsValid
                ? m_Data.GetInstigator()
                : null;
        }

        /// <summary>
        /// Returns the original instigator stored by the referenced gameplay effect context.
        /// </summary>
        public GameObject GetOriginalInstigator()
        {
            return IsValid
                ? m_Data.GetOriginalInstigator()
                : null;
        }

        /// <summary>
        /// Returns the physical effect causer stored by the referenced gameplay effect context.
        /// </summary>
        public GameObject GetEffectCauser()
        {
            return IsValid
                ? m_Data.GetEffectCauser()
                : null;
        }

        /// <summary>
        /// Sets the source object on the referenced gameplay effect context.
        /// </summary>
        public void AddSourceObject(
            UnityEngine.Object sourceObject)
        {
            if (!IsValid)
            {
                return;
            }

            m_Data.AddSourceObject(
                sourceObject);
        }

        /// <summary>
        /// Returns the source object stored by the referenced gameplay effect context.
        /// </summary>
        public UnityEngine.Object GetSourceObject()
        {
            return IsValid
                ? m_Data.GetSourceObject()
                : null;
        }

        /// <summary>
        /// Returns the ability system component of the effect instigator.
        /// </summary>
        public AbilitySystemComponent
            GetInstigatorAbilitySystemComponent()
        {
            return IsValid
                ? m_Data.GetInstigatorAbilitySystemComponent()
                : null;
        }

        /// <summary>
        /// Returns the ability system component of the original effect instigator.
        /// </summary>
        public AbilitySystemComponent
            GetOriginalInstigatorAbilitySystemComponent()
        {
            return IsValid
                ? m_Data.GetOriginalInstigatorAbilitySystemComponent()
                : null;
        }
    }
}