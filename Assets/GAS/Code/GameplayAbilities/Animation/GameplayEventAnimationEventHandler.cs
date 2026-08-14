using UnityEngine;

namespace GAS
{
    [DisallowMultipleComponent]
    public sealed class GameplayEventAnimationEventHandler :
        MonoBehaviour
    {
        private AbilitySystemComponent m_AbilitySystemComponent;

        private void Reset()
        {
            ResolveAbilitySystemComponent();
        }

        private void Awake()
        {
            ResolveAbilitySystemComponent();
        }

        /// <summary>
        /// Resolves the ability system associated with the root gameplay actor.
        /// </summary>
        private void ResolveAbilitySystemComponent()
        {
            GameObject actorRoot =
                transform.root.gameObject;

            AbilitySystemGlobals.TryGetAbilitySystemComponentFromActor(
                actorRoot,
                out m_AbilitySystemComponent);
        }

        /// <summary>
        /// Converts a Unity animation event into a gameplay event on the owning ability system.
        /// </summary>
        public void HandleGameplayEvent(
            AnimationEvent animationEvent)
        {
            if (animationEvent == null)
            {
                Debug.LogWarning(
                    $"{name} received an empty animation event.",
                    this);

                return;
            }

            GameplayTag eventTag =
                animationEvent.objectReferenceParameter as GameplayTag;

            if (eventTag == null)
            {
                Debug.LogWarning(
                    $"{name} received an animation event without a gameplay tag.",
                    this);

                return;
            }

            if (m_AbilitySystemComponent == null)
            {
                Debug.LogWarning(
                    $"{name} cannot deliver gameplay event " +
                    $"'{eventTag.name}' without an ability system component.",
                    this);

                return;
            }

            GameplayEventData payload =
                new(
                    eventTag);

            m_AbilitySystemComponent.HandleGameplayEvent(
                eventTag,
                payload);
        }
    }
}