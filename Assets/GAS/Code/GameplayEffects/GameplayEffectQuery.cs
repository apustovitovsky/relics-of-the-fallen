using System;
using System.Collections.Generic;

namespace GAS
{
    /// <summary>
    /// Describes conditions used to select active gameplay effects.
    /// </summary>
    public sealed class GameplayEffectQuery
    {
        private readonly GameplayTagContainer m_AnyOwningTags = new();

        private GameplayEffectQuery()
        {
        }

        /// <summary>
        /// Creates a query matching effects that own any of the supplied gameplay tags.
        /// </summary>
        public static GameplayEffectQuery MakeQuery_MatchAnyOwningTags(
            GameplayTagContainer owningTags)
        {
            if (owningTags == null)
            {
                throw new ArgumentNullException(
                    nameof(owningTags));
            }

            GameplayEffectQuery query = new();

            query.m_AnyOwningTags.AppendTags(
                owningTags);

            return query;
        }

        /// <summary>
        /// Returns whether an active gameplay effect satisfies this query.
        /// </summary>
        public bool Matches(
            ActiveGameplayEffect activeEffect)
        {
            if (activeEffect == null)
            {
                throw new ArgumentNullException(
                    nameof(activeEffect));
            }

            if (m_AnyOwningTags.IsEmpty())
            {
                return true;
            }

            GameplayEffectTags effectTags =
                activeEffect.Spec.Definition.gameplayEffectTags;

            if (effectTags == null)
            {
                return false;
            }

            IReadOnlyList<GameplayTag> owningTags =
                effectTags.GrantedTags;

            for (
                int tagIndex = 0;
                tagIndex < owningTags.Count;
                tagIndex++)
            {
                GameplayTag owningTag =
                    owningTags[tagIndex];

                if (owningTag == null)
                {
                    continue;
                }

                IReadOnlyList<GameplayTag> hierarchy =
                    GameplayTagLibrary.Instance.GetHierarchy(
                        owningTag);

                for (
                    int hierarchyIndex = 0;
                    hierarchyIndex < hierarchy.Count;
                    hierarchyIndex++)
                {
                    if (
                        m_AnyOwningTags.HasTagExact(
                            hierarchy[hierarchyIndex]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}