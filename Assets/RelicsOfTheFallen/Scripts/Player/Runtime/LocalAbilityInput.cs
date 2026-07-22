using GAS;
using RelicsOfTheFallen.Character;
using UnityEngine;

namespace RelicsOfTheFallen.Player
{
    [DisallowMultipleComponent]
    public sealed class LocalAbilityInput :
        MonoBehaviour
    {
        [SerializeField]
        LocalCharacterInput m_CharacterInput;

        [SerializeField]
        AbilitySystemComponent m_AbilitySystem;

        [SerializeField]
        GameplayAbilitySO m_AttackAbility;

        [Header("Debug")]
        [SerializeField]
        bool m_LogDebugEvents = true;

        void OnEnable()
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

        void OnDisable()
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

        void HandleAttackPerformed()
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

            m_AbilitySystem.TryActivateAbility(
                abilityName,
                m_AbilitySystem);
        }

        void HandleGameplayEvent(
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

        bool ValidateReferences()
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