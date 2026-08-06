using System;
using System.Collections.Generic;

namespace GAS.Common
{
    /// <summary>
    /// Tracks handles granted by one common gameplay ability set.
    /// </summary>
    public sealed class GameplayAbilitySetGrantedHandles
    {
        private readonly List<GameplayAbilitySpecHandle>
            m_AbilitySpecHandles = new();

        /// <summary>
        /// Records one valid gameplay ability specification handle.
        /// </summary>
        public void AddAbilitySpecHandle(
            GameplayAbilitySpecHandle handle)
        {
            if (!handle.IsValid)
            {
                return;
            }

            m_AbilitySpecHandles.Add(
                handle);
        }

        /// <summary>
        /// Removes all tracked grants from the authoritative ability system.
        /// </summary>
        public void TakeFromAbilitySystem(
            CommonAbilitySystemComponent abilitySystem)
        {
            if (abilitySystem == null)
            {
                throw new ArgumentNullException(
                    nameof(abilitySystem));
            }

            if (!abilitySystem.IsOwnerActorAuthoritative())
            {
                return;
            }

            for (
                int index = 0;
                index < m_AbilitySpecHandles.Count;
                index++)
            {
                GameplayAbilitySpecHandle handle =
                    m_AbilitySpecHandles[index];

                if (!handle.IsValid)
                {
                    continue;
                }

                abilitySystem.ClearAbility(
                    handle);
            }

            m_AbilitySpecHandles.Clear();
        }
    }
}