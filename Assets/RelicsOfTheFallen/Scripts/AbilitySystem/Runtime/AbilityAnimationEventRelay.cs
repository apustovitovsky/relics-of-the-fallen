using GAS;
using UnityEngine;

namespace RelicsOfTheFallen.AbilitySystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class AbilityAnimationEventRelay
        : MonoBehaviour
    {
        [SerializeField]
        AbilitySystemComponent m_AbilitySystem;

        [SerializeField]
        AbilityAnimationPresenter m_AnimationPresenter;

        void Reset()
        {
            m_AbilitySystem =
                GetComponentInParent<AbilitySystemComponent>();

            m_AnimationPresenter =
                GetComponentInParent<AbilityAnimationPresenter>();
        }

        void Awake()
        {
            if (m_AbilitySystem == null)
            {
                Debug.LogError(
                    $"{nameof(AbilityAnimationEventRelay)} on " +
                    $"{name} has no AbilitySystemComponent.",
                    this);

                enabled = false;

                return;
            }

            if (m_AnimationPresenter == null)
            {
                Debug.LogError(
                    $"{nameof(AbilityAnimationEventRelay)} on " +
                    $"{name} has no " +
                    $"{nameof(AbilityAnimationPresenter)}.",
                    this);

                enabled = false;
            }
        }

        /// <summary>
        /// Вызывается Unity AnimationEvent из AnimationClip.
        /// GameplayTag передаётся через objectReferenceParameter.
        /// </summary>
        public void EmitGameplayEvent(
            AnimationEvent animationEvent)
        {
            if (animationEvent == null)
            {
                Debug.LogWarning(
                    $"{nameof(AbilityAnimationEventRelay)} on " +
                    $"{name} received a null AnimationEvent.",
                    this);

                return;
            }

            var gameplayTag =
                animationEvent.objectReferenceParameter
                    as GameplayTag;

            if (gameplayTag == null)
            {
                Debug.LogWarning(
                    $"AnimationEvent '{animationEvent.functionName}' " +
                    $"on {name} has no GameplayTag in its " +
                    "Object parameter.",
                    this);

                return;
            }

            if (!m_AnimationPresenter
                    .TryGetActiveActivationGUID(
                        out string activationGUID))
            {
                Debug.LogWarning(
                    $"AnimationEvent tag '{gameplayTag.name}' " +
                    $"was received by {name}, but there is no " +
                    "active NotifyGameplayAbility animation.",
                    this);

                return;
            }

            m_AbilitySystem.SendGameplayEvent(
                gameplayTag,
                activationGUID);
        }
    }
}