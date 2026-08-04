using GAS;
using RelicsOfTheFallen.Character;
using UnityEngine;
using GAS.Mirror;

namespace RelicsOfTheFallen.Player
{
    [DisallowMultipleComponent]
    public sealed class LocalAbilityInput :
        MonoBehaviour
    {
        [SerializeField]
        private NetworkAbilitySystemComponent m_NetworkAbilitySystem;

        [SerializeField]
        private LocalCharacterInput m_CharacterInput;

        [SerializeField]
        private AbilitySystemComponent m_AbilitySystem;

        [SerializeField]
        private GameplayAbilitySO m_AttackAbility;

        [Header("Debug")]
        [SerializeField]
        private bool m_LogDebugEvents = true;

        private void OnEnable()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            m_CharacterInput.AttackPerformed +=
                HandleAttackPerformed;

            m_AbilitySystem.OnGameplayEvent +=
                HandleGameplayEvent;
        }

        private void OnDisable()
        {
            if (m_CharacterInput != null)
            {
                m_CharacterInput.AttackPerformed -=
                    HandleAttackPerformed;
            }

            if (m_AbilitySystem != null)
            {
                m_AbilitySystem.OnGameplayEvent -=
                    HandleGameplayEvent;
            }
        }

        private void HandleAttackPerformed()
        {
            string abilityName =
                m_AttackAbility.ga.name;

            if (m_LogDebugEvents)
            {
                Debug.Log(
                    $"[AbilityTest] Attack input requested " +
                    $"'{abilityName}' at {Time.time:F3}.",
                    this);
            }

            m_NetworkAbilitySystem.TryActivateAbility(
                m_AttackAbility);
        }

        private void HandleGameplayEvent(
            GameplayEventData gameplayEvent)
        {
            if (!m_LogDebugEvents)
            {
                return;
            }

            string tagName =
                gameplayEvent.Tag != null
                    ? gameplayEvent.Tag.name
                    : "<null>";

            Debug.Log(
                $"[AbilityTest] Gameplay event " +
                $"'{tagName}', activation " +
                $"'{gameplayEvent.ActivationGUID}' " +
                $"at {Time.time:F3}.",
                this);
        }

        private bool ValidateReferences()
        {
            if (m_CharacterInput == null)
            {
                Debug.LogError(
                    $"{nameof(LocalAbilityInput)} on " +
                    $"'{name}' requires " +
                    $"{nameof(LocalCharacterInput)}.",
                    this);

                return false;
            }

            if (m_AbilitySystem == null)
            {
                Debug.LogError(
                    $"{nameof(LocalAbilityInput)} on " +
                    $"'{name}' requires " +
                    $"{nameof(AbilitySystemComponent)}.",
                    this);

                return false;
            }

            if (m_NetworkAbilitySystem == null)
            {
                Debug.LogError(
                    $"{nameof(LocalAbilityInput)} on " +
                    $"'{name}' requires " +
                    $"{nameof(NetworkAbilitySystemComponent)}.",
                    this);

                return false;
            }

            if (m_AttackAbility == null ||
                m_AttackAbility.ga == null)
            {
                Debug.LogError(
                    $"{nameof(LocalAbilityInput)} on " +
                    $"'{name}' requires an attack ability.",
                    this);

                return false;
            }

            return true;
        }
    }
}