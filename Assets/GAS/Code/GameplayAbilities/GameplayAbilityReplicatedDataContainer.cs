using System.Collections.Generic;

namespace GAS
{
    public sealed class GameplayAbilityReplicatedDataContainer
    {
        private readonly Dictionary<
            GameplayAbilitySpecHandleAndPredictionKey,
            AbilityReplicatedDataCache> m_InUseData = new();

        private readonly Stack<
            AbilityReplicatedDataCache> m_FreeData = new();

        /// <summary>
        /// Returns the replicated data cache associated with one ability activation.
        /// </summary>
        public AbilityReplicatedDataCache Find(
            GameplayAbilitySpecHandleAndPredictionKey key)
        {
            m_InUseData.TryGetValue(
                key,
                out AbilityReplicatedDataCache cache);

            return cache;
        }

        /// <summary>
        /// Returns or creates the replicated data cache associated with one ability activation.
        /// </summary>
        public AbilityReplicatedDataCache FindOrAdd(
            GameplayAbilitySpecHandleAndPredictionKey key)
        {
            if (
                m_InUseData.TryGetValue(
                    key,
                    out AbilityReplicatedDataCache cache))
            {
                return cache;
            }

            cache =
                m_FreeData.Count > 0
                    ? m_FreeData.Pop()
                    : new AbilityReplicatedDataCache();

            m_InUseData.Add(
                key,
                cache);

            return cache;
        }

        /// <summary>
        /// Removes one activation cache and recycles its storage for later use.
        /// </summary>
        public void Remove(
            GameplayAbilitySpecHandleAndPredictionKey key)
        {
            if (
                !m_InUseData.TryGetValue(
                    key,
                    out AbilityReplicatedDataCache cache))
            {
                return;
            }

            m_InUseData.Remove(
                key);

            cache.ResetAll();

            m_FreeData.Push(
                cache);
        }
    }
}