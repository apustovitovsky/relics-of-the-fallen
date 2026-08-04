using System;
using System.Collections.Generic;

namespace GAS
{
    public sealed class GameplayTagCountContainer
    {
        private readonly Dictionary<GameplayTag, int>
            m_ExplicitTagCounts = new();

        private readonly Dictionary<GameplayTag, int>
            m_AggregatedTagCounts = new();

        private readonly Dictionary<
            GameplayTag,
            Action<GameplayTag, int>>
            m_NewOrRemovedEvents = new();

        private readonly Dictionary<
            GameplayTag,
            Action<GameplayTag, int>>
            m_AnyCountChangeEvents = new();

        private Action<GameplayTag, int> m_GenericGameplayEvent;

        /// <summary>
        /// Registers a callback for changes to one gameplay tag.
        /// </summary>
        internal IDisposable RegisterGameplayTagEvent(
            GameplayTag tag,
            GameplayTagEventType eventType,
            Action<GameplayTag, int> handler)
        {
            if (tag == null)
            {
                throw new ArgumentNullException(
                    nameof(tag));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(
                    nameof(handler));
            }

            Dictionary<
                GameplayTag,
                Action<GameplayTag, int>> events =
                GetGameplayTagEventMap(
                    eventType);

            events.TryGetValue(
                tag,
                out Action<GameplayTag, int> handlers);

            events[tag] =
                handlers +
                handler;

            return
                new DisposableSubscription(() =>
                    UnregisterGameplayTagEvent(
                        tag,
                        eventType,
                        handler));
        }

        /// <summary>
        /// Registers a callback for additions or removals of any gameplay tag.
        /// </summary>
        internal IDisposable RegisterGenericGameplayEvent(
            Action<GameplayTag, int> handler)
        {
            m_GenericGameplayEvent +=
                handler ?? throw new ArgumentNullException(
                    nameof(handler));

            return
                new DisposableSubscription(() =>
                    m_GenericGameplayEvent -=
                        handler);
        }

        /// <summary>
        /// Removes one previously registered gameplay tag callback.
        /// </summary>
        private void UnregisterGameplayTagEvent(
            GameplayTag tag,
            GameplayTagEventType eventType,
            Action<GameplayTag, int> handler)
        {
            Dictionary<
                GameplayTag,
                Action<GameplayTag, int>> events =
                GetGameplayTagEventMap(eventType);

            if (
                !events.TryGetValue(
                    tag,
                    out Action<GameplayTag, int> handlers))
            {
                return;
            }

            handlers -= handler;

            if (handlers == null)
            {
                events.Remove(tag);
                return;
            }

            events[tag] = handlers;
        }

        /// <summary>
        /// Returns the callback map associated with a gameplay tag event type.
        /// </summary>
        private Dictionary<
            GameplayTag,
            Action<GameplayTag, int>> GetGameplayTagEventMap(
            GameplayTagEventType eventType)
        {
            return
                eventType switch
                {
                    GameplayTagEventType.NewOrRemoved =>
                        m_NewOrRemovedEvents,

                    GameplayTagEventType.AnyCountChange =>
                        m_AnyCountChangeEvents,

                    _ =>
                        throw new ArgumentOutOfRangeException(
                            nameof(eventType),
                            eventType,
                            "Unsupported gameplay tag event type.")
                };
        }

        /// <summary>
        /// Returns the aggregated count of a gameplay tag.
        /// </summary>
        public int GetTagCount(
            GameplayTag tag)
        {
            if (tag == null)
            {
                throw new ArgumentNullException(
                    nameof(tag));
            }

            return
                m_AggregatedTagCounts.TryGetValue(
                    tag,
                    out int count)
                    ? count
                    : 0;
        }

        /// <summary>
        /// Returns whether the container owns the gameplay tag or one of its children.
        /// </summary>
        public bool HasMatchingGameplayTag(
            GameplayTag tag)
        {
            return
                GetTagCount(tag) >
                0;
        }

        /// <summary>
        /// Returns whether the container owns any tag from the supplied collection.
        /// </summary>
        public bool HasAnyMatchingGameplayTags(
            IReadOnlyList<GameplayTag> tags)
        {
            if (tags == null)
            {
                throw new ArgumentNullException(
                    nameof(tags));
            }

            for (
                int index = 0;
                index < tags.Count;
                index++)
            {
                GameplayTag tag =
                    tags[index];

                if (
                    tag != null &&
                    HasMatchingGameplayTag(tag))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns whether the container owns every tag from the supplied collection.
        /// </summary>
        public bool HasAllMatchingGameplayTags(
            IReadOnlyList<GameplayTag> tags)
        {
            if (tags == null)
            {
                throw new ArgumentNullException(
                    nameof(tags));
            }

            for (
                int index = 0;
                index < tags.Count;
                index++)
            {
                GameplayTag tag =
                    tags[index];

                if (
                    tag == null ||
                    !HasMatchingGameplayTag(tag))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Changes the explicit contribution count and updates cached parent counts.
        /// </summary>
        public void UpdateTagCount(
            GameplayTag tag,
            int countDelta)
        {
            if (tag == null)
            {
                throw new ArgumentNullException(
                    nameof(tag));
            }

            if (countDelta == 0)
            {
                return;
            }

            m_ExplicitTagCounts.TryGetValue(
                tag,
                out int oldExplicitCount);

            int newExplicitCount =
                oldExplicitCount +
                countDelta;

            if (newExplicitCount < 0)
            {
                throw new InvalidOperationException(
                    $"Gameplay tag '{tag.name}' count cannot become negative.");
            }

            if (newExplicitCount == 0)
            {
                m_ExplicitTagCounts.Remove(
                    tag);
            }
            else
            {
                m_ExplicitTagCounts[tag] =
                    newExplicitCount;
            }

            IReadOnlyList<GameplayTag> hierarchy =
                GameplayTagLibrary.Instance.GetHierarchy(
                    tag);

            for (
                int index = 0;
                index < hierarchy.Count;
                index++)
            {
                GameplayTag hierarchyTag =
                    hierarchy[index];

                m_AggregatedTagCounts.TryGetValue(
                    hierarchyTag,
                    out int oldAggregatedCount);

                int newAggregatedCount =
                    oldAggregatedCount +
                    countDelta;

                if (newAggregatedCount == 0)
                {
                    m_AggregatedTagCounts.Remove(
                        hierarchyTag);
                }
                else
                {
                    m_AggregatedTagCounts[hierarchyTag] =
                        newAggregatedCount;
                }
            }

            for (
                int index = 0;
                index < hierarchy.Count;
                index++)
            {
                GameplayTag hierarchyTag =
                    hierarchy[index];

                m_AggregatedTagCounts.TryGetValue(
                    hierarchyTag,
                    out int newAggregatedCount);

                NotifyTagCountChanged(
                    hierarchyTag,
                    newAggregatedCount - countDelta,
                    newAggregatedCount);
            }
        }

        /// <summary>
        /// Dispatches gameplay tag events according to the requested count semantics.
        /// </summary>
        private void NotifyTagCountChanged(
            GameplayTag tag,
            int oldCount,
            int newCount)
        {
            if (
                m_AnyCountChangeEvents.TryGetValue(
                    tag,
                    out Action<GameplayTag, int> anyCountChangeHandlers))
            {
                anyCountChangeHandlers(
                    tag,
                    newCount);
            }

            if (
                (oldCount > 0) ==
                (newCount > 0))
            {
                return;
            }

            m_GenericGameplayEvent?.Invoke(
                tag,
                newCount);

            if (
                m_NewOrRemovedEvents.TryGetValue(
                    tag,
                    out Action<GameplayTag, int> newOrRemovedHandlers))
            {
                newOrRemovedHandlers(
                    tag,
                    newCount);
            }
        }

        /// <summary>
        /// Copies explicitly owned gameplay tags without allocating a new collection.
        /// </summary>
        public void GetOwnedGameplayTags(
            List<GameplayTag> result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(
                    nameof(result));
            }

            result.Clear();

            foreach (
                KeyValuePair<GameplayTag, int> entry
                in m_ExplicitTagCounts)
            {
                if (entry.Value > 0)
                {
                    result.Add(
                        entry.Key);
                }
            }
        }
    }
}