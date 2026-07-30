using System;
using System.Collections.Generic;

namespace GAS
{
    /// <summary>
    /// Owns the activatable ability specifications registered on one ability system.
    /// </summary>
    public sealed class GameplayAbilitySpecContainer
    {
        private readonly List<GameplayAbilitySpec>
            m_Items = new();

        private readonly Dictionary<
            GameplayAbilitySpecHandle,
            GameplayAbilitySpec> m_SpecsByHandle = new();

        public IReadOnlyList<GameplayAbilitySpec> Items =>
            m_Items;

        public int Count =>
            m_Items.Count;

        internal GameplayAbilitySpec this[int index] =>
            m_Items[index];

        public event Action<GameplayAbilitySpec> AbilitySpecAdded;

        public event Action<GameplayAbilitySpec> AbilitySpecChanged;

        public event Action<GameplayAbilitySpec> AbilitySpecRemoved;

        /// <summary>
        /// Adds a unique gameplay ability specification to this container.
        /// </summary>
        internal void Add(
            GameplayAbilitySpec abilitySpec)
        {
            if (abilitySpec == null)
            {
                throw new ArgumentNullException(
                    nameof(abilitySpec));
            }

            if (
                !m_SpecsByHandle.TryAdd(
                    abilitySpec.Handle,
                    abilitySpec))
            {
                throw new InvalidOperationException(
                    $"Ability specification handle '{abilitySpec.Handle}' is already registered.");
            }

            m_Items.Add(
                abilitySpec);

            AbilitySpecAdded?.Invoke(
                abilitySpec);
        }

        /// <summary>
        /// Finds a gameplay ability specification by its stable handle.
        /// </summary>
        internal GameplayAbilitySpec FindAbilitySpecFromHandle(
            GameplayAbilitySpecHandle handle)
        {
            m_SpecsByHandle.TryGetValue(
                handle,
                out GameplayAbilitySpec abilitySpec);

            return abilitySpec;
        }

        /// <summary>
        /// Finds the first gameplay ability specification created from the requested definition.
        /// </summary>
        internal GameplayAbilitySpec FindAbilitySpecFromClass(
            GameplayAbilitySO ability)
        {
            if (ability == null)
            {
                return null;
            }

            for (
                int index = 0;
                index < m_Items.Count;
                index++)
            {
                GameplayAbilitySpec abilitySpec = m_Items[index];

                if (abilitySpec.Ability == ability)
                {
                    return abilitySpec;
                }
            }

            return null;
        }

        /// <summary>
        /// Removes a gameplay ability specification identified by its stable handle.
        /// </summary>
        internal bool Remove(
            GameplayAbilitySpecHandle handle,
            out GameplayAbilitySpec abilitySpec)
        {
            if (
                !m_SpecsByHandle.Remove(
                    handle,
                    out abilitySpec))
            {
                return false;
            }

            if (!m_Items.Remove(abilitySpec))
            {
                throw new InvalidOperationException(
                    "Ability specification container lookup is inconsistent.");
            }

            AbilitySpecRemoved?.Invoke(
                abilitySpec);

            return true;
        }

        /// <summary>
        /// Removes the gameplay ability specification stored at the requested index.
        /// </summary>
        internal void RemoveAt(
            int index)
        {
            GameplayAbilitySpec abilitySpec =
                m_Items[index];

            if (
                !Remove(
                    abilitySpec.Handle,
                    out _))
            {
                throw new InvalidOperationException(
                    "Ability specification container lookup is inconsistent.");
            }
        }

        /// <summary>
        /// Removes every gameplay ability specification from this container.
        /// </summary>
        internal void Clear()
        {
            while (m_Items.Count > 0)
            {
                RemoveAt(
                    m_Items.Count - 1);
            }
        }

        /// <summary>
        /// Marks a registered gameplay ability specification for replicated synchronization.
        /// </summary>
        internal void MarkAbilitySpecDirty(
            GameplayAbilitySpec abilitySpec)
        {
            if (abilitySpec == null)
            {
                throw new ArgumentNullException(
                    nameof(abilitySpec));
            }

            if (
                !m_SpecsByHandle.TryGetValue(
                    abilitySpec.Handle,
                    out GameplayAbilitySpec registeredSpec) ||
                !ReferenceEquals(
                    registeredSpec,
                    abilitySpec))
            {
                throw new InvalidOperationException(
                    $"Ability specification '{abilitySpec.Handle}' is not registered.");
            }

            AbilitySpecChanged?.Invoke(
                abilitySpec);
        }
    }
}