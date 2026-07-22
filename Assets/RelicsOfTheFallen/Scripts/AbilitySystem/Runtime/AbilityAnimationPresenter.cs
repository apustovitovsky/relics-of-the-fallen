using System;
using GAS;
using UnityEngine;

namespace RelicsOfTheFallen.AbilitySystem
{
    [DisallowMultipleComponent]
    public sealed class AbilityAnimationPresenter
        : MonoBehaviour
    {
        [SerializeField]
        AbilitySystemComponent m_AbilitySystem;

        [SerializeField]
        Animator m_Animator;

        NotifyGameplayAbility m_ActiveAbility;

        string m_ActiveActivationGUID;

        public NotifyGameplayAbility ActiveAbility =>
            m_ActiveAbility;

        public string ActiveActivationGUID =>
            m_ActiveActivationGUID;

        void Reset()
        {
            m_AbilitySystem =
                GetComponent<AbilitySystemComponent>();

            m_Animator =
                GetComponentInChildren<Animator>();
        }

        void Awake()
        {
            if (m_AbilitySystem == null)
            {
                Debug.LogError(
                    $"{nameof(AbilityAnimationPresenter)} on " +
                    $"{name} has no AbilitySystemComponent.",
                    this);

                enabled = false;

                return;
            }

            if (m_Animator == null)
            {
                Debug.LogError(
                    $"{nameof(AbilityAnimationPresenter)} on " +
                    $"{name} has no Animator.",
                    this);

                enabled = false;
            }
        }

        void OnEnable()
        {
            if (m_AbilitySystem == null)
            {
                return;
            }

            m_AbilitySystem.OnGameplayAbilityActivated +=
                HandleAbilityActivated;

            m_AbilitySystem.OnGameplayAbilityDeactivated +=
                HandleAbilityDeactivated;
        }

        void OnDisable()
        {
            if (m_AbilitySystem != null)
            {
                m_AbilitySystem.OnGameplayAbilityActivated -=
                    HandleAbilityActivated;

                m_AbilitySystem.OnGameplayAbilityDeactivated -=
                    HandleAbilityDeactivated;
            }

            ClearActiveAbility();
        }

        public bool TryGetActiveActivationGUID(
            out string activationGUID)
        {
            activationGUID =
                m_ActiveActivationGUID;

            return m_ActiveAbility != null &&
                   !string.IsNullOrEmpty(
                       m_ActiveActivationGUID);
        }

        void HandleAbilityActivated(
            GameplayAbility gameplayAbility,
            string activationGUID)
        {
            if (gameplayAbility is not
                NotifyGameplayAbility notifyAbility)
            {
                return;
            }

            string animationTrigger =
                notifyAbility.AnimationTrigger;

            if (!HasTriggerParameter(
                    animationTrigger))
            {
                Debug.LogError(
                    $"{nameof(NotifyGameplayAbility)} " +
                    $"'{notifyAbility.name}' requested Animator " +
                    $"trigger '{animationTrigger}', but Animator " +
                    $"'{m_Animator.name}' has no Trigger parameter " +
                    "with that name.",
                    this);

                return;
            }

            if (m_ActiveAbility != null &&
                m_ActiveAbility != notifyAbility)
            {
                Debug.LogWarning(
                    $"{nameof(AbilityAnimationPresenter)} on " +
                    $"{name} received ability " +
                    $"'{notifyAbility.name}' while " +
                    $"'{m_ActiveAbility.name}' is still active. " +
                    "The current implementation supports one " +
                    "NotifyGameplayAbility action channel. " +
                    "Configure GAS ability tags so these abilities " +
                    "block or cancel each other.",
                    this);
            }

            m_ActiveAbility =
                notifyAbility;

            m_ActiveActivationGUID =
                activationGUID;

            m_Animator.SetTrigger(
                animationTrigger);
        }

        void HandleAbilityDeactivated(
            GameplayAbility gameplayAbility,
            string activationGUID)
        {
            if (gameplayAbility != m_ActiveAbility)
            {
                return;
            }

            if (!string.IsNullOrEmpty(
                    m_ActiveActivationGUID) &&
                !string.Equals(
                    m_ActiveActivationGUID,
                    activationGUID,
                    StringComparison.Ordinal))
            {
                return;
            }

            string animationTrigger =
                m_ActiveAbility.AnimationTrigger;

            if (HasTriggerParameter(
                    animationTrigger))
            {
                m_Animator.ResetTrigger(
                    animationTrigger);
            }

            ClearActiveAbility();
        }

        bool HasTriggerParameter(
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(
                    parameterName))
            {
                return false;
            }

            int parameterHash =
                Animator.StringToHash(
                    parameterName);

            foreach (
                AnimatorControllerParameter parameter
                in m_Animator.parameters)
            {
                if (parameter.nameHash ==
                        parameterHash &&
                    parameter.type ==
                        AnimatorControllerParameterType.Trigger)
                {
                    return true;
                }
            }

            return false;
        }

        void ClearActiveAbility()
        {
            m_ActiveAbility = null;
            m_ActiveActivationGUID = null;
        }
    }
}