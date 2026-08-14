using System;
using System.Collections.Generic;

namespace GAS
{
    internal sealed class GameplayEventTagContainerDelegateRegistration
    {
        private readonly GameplayTagContainer m_TagFilter = new();

        private readonly Action<
            GameplayTag,
            GameplayEventData> m_Handler;

        public GameplayEventTagContainerDelegateRegistration(
            GameplayTagContainer tagFilter,
            Action<GameplayTag, GameplayEventData> handler)
        {
            if (tagFilter == null)
            {
                throw new ArgumentNullException(
                    nameof(tagFilter));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(
                    nameof(handler));
            }

            m_TagFilter.AppendTags(
                tagFilter);

            m_Handler = handler;
        }

        /// <summary>
        /// Returns whether the event tag matches this registration filter.
        /// </summary>
        public bool Matches(
            GameplayTag eventTag)
        {
            if (m_TagFilter.IsEmpty())
            {
                return true;
            }

            IReadOnlyList<GameplayTag> hierarchy =
                GameplayTagLibrary.Instance.GetHierarchy(
                    eventTag);

            IReadOnlyList<GameplayTag> filterTags =
                m_TagFilter.GetGameplayTagArray();

            for (
                int filterIndex = 0;
                filterIndex < filterTags.Count;
                filterIndex++)
            {
                for (
                    int hierarchyIndex = 0;
                    hierarchyIndex < hierarchy.Count;
                    hierarchyIndex++)
                {
                    if (filterTags[filterIndex] ==
                        hierarchy[hierarchyIndex])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void Invoke(
            GameplayTag eventTag,
            GameplayEventData payload)
        {
            m_Handler(
                eventTag,
                payload);
        }
    }
}