using System;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Handles gameplay cue events without creating a persistent runtime notify instance.
    /// </summary>
    public abstract class GameplayCueNotify_Static :
        GameplayCueNotify
    {
        /// <summary>
        /// Returns whether this static notify handles the supplied gameplay cue event.
        /// </summary>
        public override bool HandlesEvent(
            GameplayCueEvent eventType)
        {
            return true;
        }

        /// <summary>
        /// Routes one gameplay cue event to its corresponding static notify callback.
        /// </summary>
        public override void HandleGameplayCue(
            GameObject target,
            GameplayCueEvent eventType,
            GameplayCueParameters parameters)
        {
            if (target == null)
            {
                Debug.LogWarning(
                    $"{name} cannot handle a gameplay cue without a target.",
                    this);

                return;
            }

            if (parameters == null)
            {
                throw new ArgumentNullException(
                    nameof(parameters));
            }

            switch (eventType)
            {
                case GameplayCueEvent.OnActive:
                    OnActive(
                        target,
                        parameters);
                    break;

                case GameplayCueEvent.WhileActive:
                    WhileActive(
                        target,
                        parameters);
                    break;

                case GameplayCueEvent.Executed:
                    OnExecute(
                        target,
                        parameters);
                    break;

                case GameplayCueEvent.Removed:
                    OnRemove(
                        target,
                        parameters);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(eventType),
                        eventType,
                        "Unsupported gameplay cue event.");
            }
        }

        /// <summary>
        /// Handles execution of an instant or periodic gameplay cue.
        /// </summary>
        protected virtual bool OnExecute(
            GameObject target,
            GameplayCueParameters parameters)
        {
            return false;
        }

        /// <summary>
        /// Handles the initial activation of a persistent gameplay cue.
        /// </summary>
        protected virtual bool OnActive(
            GameObject target,
            GameplayCueParameters parameters)
        {
            return false;
        }

        /// <summary>
        /// Handles discovery of an already active persistent gameplay cue.
        /// </summary>
        protected virtual bool WhileActive(
            GameObject target,
            GameplayCueParameters parameters)
        {
            return false;
        }

        /// <summary>
        /// Handles removal of a persistent gameplay cue.
        /// </summary>
        protected virtual bool OnRemove(
            GameObject target,
            GameplayCueParameters parameters)
        {
            return false;
        }
    }
}