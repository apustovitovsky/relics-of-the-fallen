using System;
using System.Collections.Generic;

namespace GAS
{
    public sealed class GameplayAbilityTargetDataHandle
    {
        private readonly List<GameplayAbilityTargetData> m_Data =
            new();

        /// <summary>
        /// Creates an empty polymorphic target-data container.
        /// </summary>
        public GameplayAbilityTargetDataHandle()
        {
        }

        /// <summary>
        /// Creates a target-data container initialized with one payload.
        /// </summary>
        public GameplayAbilityTargetDataHandle(
            GameplayAbilityTargetData data)
        {
            Add(
                data);
        }

        /// <summary>
        /// Returns the number of target-data payloads stored by this handle.
        /// </summary>
        public int Num()
        {
            return m_Data.Count;
        }

        /// <summary>
        /// Returns the target-data payload at the requested index when available.
        /// </summary>
        public GameplayAbilityTargetData Get(
            int index)
        {
            if (
                index < 0 ||
                index >= m_Data.Count)
            {
                return null;
            }

            return m_Data[index];
        }

        /// <summary>
        /// Adds one polymorphic targeting payload to this handle.
        /// </summary>
        public void Add(
            GameplayAbilityTargetData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(
                    nameof(data));
            }

            m_Data.Add(
                data);
        }
    }
}