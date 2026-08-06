using System;
using UnityEngine;

namespace GAS
{
    public class GameplayCueManager
    {
        private readonly GameplayCueSet m_RuntimeCueSet;

        private readonly DisposableEvent<
            AbilitySystemComponent,
            GameplayTag,
            GameplayCueParameters> m_GameplayCueExecuted = new();

        public GameplayCueManager(
            GameplayCueSet runtimeCueSet)
        {
            m_RuntimeCueSet =
                runtimeCueSet != null
                    ? runtimeCueSet
                    : throw new ArgumentNullException(
                        nameof(runtimeCueSet));
        }

        /// <summary>
        /// Returns the gameplay cue set used for runtime event routing.
        /// </summary>
        public GameplayCueSet GetRuntimeCueSet()
        {
            return m_RuntimeCueSet;
        }

        /// <summary>
        /// Registers a handler for standalone executed gameplay cues.
        /// </summary>
        public IDisposable RegisterGameplayCueExecuted(
            Action<
                AbilitySystemComponent,
                GameplayTag,
                GameplayCueParameters> handler)
        {
            return m_GameplayCueExecuted.Subscribe(
                handler);
        }

        /// <summary>
        /// Dispatches a standalone executed gameplay cue from its owning ability system.
        /// </summary>
        public virtual void InvokeGameplayCueExecuted(
            AbilitySystemComponent owningComponent,
            GameplayTag gameplayCueTag,
            GameplayCueParameters parameters)
        {
            if (owningComponent == null)
            {
                throw new ArgumentNullException(
                    nameof(owningComponent));
            }

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

            m_GameplayCueExecuted.Invoke(
                owningComponent,
                gameplayCueTag,
                parameters);

            owningComponent.InvokeGameplayCueEvent(
                gameplayCueTag,
                GameplayCueEvent.Executed,
                parameters);
        }

        /// <summary>
        /// Handles a gameplay cue event through the configured runtime cue set.
        /// </summary>
        public virtual void HandleGameplayCue(
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

            RouteGameplayCue(
                target,
                gameplayCueTag,
                eventType,
                parameters);
        }

        /// <summary>
        /// Routes a validated gameplay cue event to its runtime cue set.
        /// </summary>
        protected virtual void RouteGameplayCue(
            GameObject target,
            GameplayTag gameplayCueTag,
            GameplayCueEvent eventType,
            GameplayCueParameters parameters)
        {
            if (parameters.OriginalTag == null)
            {
                parameters.OriginalTag =
                    gameplayCueTag;
            }

            m_RuntimeCueSet.HandleGameplayCue(
                target,
                gameplayCueTag,
                eventType,
                parameters);
        }
    }
}