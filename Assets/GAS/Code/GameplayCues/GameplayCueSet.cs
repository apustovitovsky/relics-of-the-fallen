using System;
using System.Collections.Generic;
using UnityEngine;

namespace GAS
{
    [CreateAssetMenu(
        menuName = "GAS/Gameplay Cue Set",
        fileName = "GameplayCueSet")]
    public sealed class GameplayCueSet :
        ScriptableObject
    {
        [field: SerializeField]
        public GameplayCueNotifyData[] GameplayCueData
        {
            get;
            private set;
        } = Array.Empty<GameplayCueNotifyData>();

        private Dictionary<
            GameplayTag,
            GameplayCueNotifyData> m_GameplayCueDataMap;

        /// <summary>
        /// Routes a gameplay cue event through the matching notify hierarchy.
        /// </summary>
        public bool HandleGameplayCue(
            GameObject target,
            GameplayTag gameplayCueTag,
            GameplayCueEvent eventType,
            GameplayCueParameters parameters)
        {
            if (gameplayCueTag == null)
            {
                throw new ArgumentNullException(
                    nameof(gameplayCueTag));
            }

            if (parameters == null)
            {
                throw new ArgumentNullException(
                    nameof(parameters));
            }

            EnsureLookup();

            IReadOnlyList<GameplayTag> hierarchy =
                GameplayTagLibrary.Instance.GetHierarchy(
                    gameplayCueTag);

            bool handled = false;

            for (
                int tagIndex = 0;
                tagIndex < hierarchy.Count;
                tagIndex++)
            {
                GameplayTag hierarchyTag =
                    hierarchy[tagIndex];

                if (
                    !m_GameplayCueDataMap.TryGetValue(
                        hierarchyTag,
                        out GameplayCueNotifyData cueData))
                {
                    continue;
                }

                GameplayCueNotify notify =
                    cueData.GameplayCueNotify;

                if (
                    notify == null ||
                    !notify.HandlesEvent(
                        eventType))
                {
                    continue;
                }

                parameters.MatchedTagName =
                    cueData.GameplayCueTag;

                notify.HandleGameplayCue(
                    target,
                    eventType,
                    parameters);

                handled = true;

                if (notify.IsOverride)
                {
                    break;
                }
            }

            return handled;
        }

        private void OnValidate()
        {
            m_GameplayCueDataMap = null;
        }

        private void EnsureLookup()
        {
            if (m_GameplayCueDataMap != null)
            {
                return;
            }

            BuildAccelerationMap();
        }

        /// <summary>
        /// Builds the runtime lookup used to resolve gameplay cue notify assets.
        /// </summary>
        private void BuildAccelerationMap()
        {
            m_GameplayCueDataMap =
                new Dictionary<
                    GameplayTag,
                    GameplayCueNotifyData>(
                    GameplayCueData.Length);

            for (
                int dataIndex = 0;
                dataIndex < GameplayCueData.Length;
                dataIndex++)
            {
                GameplayCueNotifyData cueData =
                    GameplayCueData[dataIndex];

                if (
                    cueData == null ||
                    cueData.GameplayCueNotify == null)
                {
                    throw new InvalidOperationException(
                        $"{name} contains an empty gameplay cue entry.");
                }

                GameplayTag gameplayCueTag =
                    cueData.GameplayCueTag;

                if (gameplayCueTag == null)
                {
                    throw new InvalidOperationException(
                        $"{cueData.GameplayCueNotify.name} has no gameplay cue tag.");
                }

                if (
                    !m_GameplayCueDataMap.TryAdd(
                        gameplayCueTag,
                        cueData))
                {
                    throw new InvalidOperationException(
                        $"{name} contains multiple notifies for tag '{gameplayCueTag.name}'.");
                }
            }
        }
    }
}