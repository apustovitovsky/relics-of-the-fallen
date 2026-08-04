using System;
using System.Collections.Generic;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Stores a unique collection of explicit gameplay tags and supports hierarchical matching.
    /// </summary>
    [Serializable]
    public sealed class GameplayTagContainer
    {
        [SerializeField]
        private List<GameplayTag> m_GameplayTags = new();

        /// <summary>
        /// Returns whether the container has no explicit gameplay tags.
        /// </summary>
        public bool IsEmpty()
        {
            return
                m_GameplayTags.Count ==
                0;
        }

        /// <summary>
        /// Returns the number of explicit gameplay tags in the container.
        /// </summary>
        public int Num()
        {
            return
                m_GameplayTags.Count;
        }

        /// <summary>
        /// Returns the explicit gameplay tags stored by the container.
        /// </summary>
        public IReadOnlyList<GameplayTag> GetGameplayTagArray()
        {
            return m_GameplayTags;
        }

        /// <summary>
        /// Removes every explicitly stored gameplay tag from the container.
        /// </summary>
        public void Reset()
        {
            m_GameplayTags.Clear();
        }

        /// <summary>
        /// Adds one valid gameplay tag if it is not already explicitly present.
        /// </summary>
        public void AddTag(
            GameplayTag tag)
        {
            if (
                tag == null ||
                m_GameplayTags.Contains(tag))
            {
                return;
            }

            m_GameplayTags.Add(tag);
        }

        /// <summary>
        /// Removes one explicitly stored gameplay tag.
        /// </summary>
        public bool RemoveTag(
            GameplayTag tag)
        {
            if (tag == null)
            {
                return false;
            }

            return
                m_GameplayTags.Remove(tag);
        }

        /// <summary>
        /// Appends the unique explicit tags from another gameplay tag container.
        /// </summary>
        public void AppendTags(
            GameplayTagContainer other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(
                    nameof(other));
            }

            for (
                int index = 0;
                index < other.m_GameplayTags.Count;
                index++)
            {
                AddTag(
                    other.m_GameplayTags[index]);
            }
        }

        /// <summary>
        /// Returns whether an explicit tag or one of its parents matches the supplied tag.
        /// </summary>
        public bool HasTag(
            GameplayTag tag)
        {
            if (tag == null)
            {
                return false;
            }

            for (
                int tagIndex = 0;
                tagIndex < m_GameplayTags.Count;
                tagIndex++)
            {
                GameplayTag explicitTag =
                    m_GameplayTags[tagIndex];

                if (explicitTag == null)
                {
                    continue;
                }

                IReadOnlyList<GameplayTag> hierarchy =
                    GameplayTagLibrary.Instance.GetHierarchy(
                        explicitTag);

                for (
                    int hierarchyIndex = 0;
                    hierarchyIndex < hierarchy.Count;
                    hierarchyIndex++)
                {
                    if (hierarchy[hierarchyIndex] == tag)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns whether the supplied tag is explicitly present in the container.
        /// </summary>
        public bool HasTagExact(
            GameplayTag tag)
        {
            return
                tag != null &&
                m_GameplayTags.Contains(tag);
        }

        /// <summary>
        /// Returns whether any tag from another container matches this container.
        /// </summary>
        public bool HasAny(
            GameplayTagContainer other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(
                    nameof(other));
            }

            for (
                int index = 0;
                index < other.m_GameplayTags.Count;
                index++)
            {
                if (
                    HasTag(
                        other.m_GameplayTags[index]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns whether any explicit tag from another container is explicitly present.
        /// </summary>
        public bool HasAnyExact(
            GameplayTagContainer other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(
                    nameof(other));
            }

            for (
                int index = 0;
                index < other.m_GameplayTags.Count;
                index++)
            {
                if (
                    HasTagExact(
                        other.m_GameplayTags[index]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns whether every tag from another container matches this container.
        /// </summary>
        public bool HasAll(
            GameplayTagContainer other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(
                    nameof(other));
            }

            for (
                int index = 0;
                index < other.m_GameplayTags.Count;
                index++)
            {
                if (
                    !HasTag(
                        other.m_GameplayTags[index]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns whether every explicit tag from another container is explicitly present.
        /// </summary>
        public bool HasAllExact(
            GameplayTagContainer other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(
                    nameof(other));
            }

            for (
                int index = 0;
                index < other.m_GameplayTags.Count;
                index++)
            {
                if (
                    !HasTagExact(
                        other.m_GameplayTags[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}