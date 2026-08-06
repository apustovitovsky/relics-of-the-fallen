using GAS;
using GAS.Common;
using RelicsOfTheFallen.Character;
using UnityEngine;

namespace RelicsOfTheFallen.Player
{
    [DisallowMultipleComponent]
    public sealed class LocalAbilityInput :
        MonoBehaviour
    {
        [SerializeField]
        private LocalCharacterInput m_CharacterInput;

        [SerializeField]
        private CommonAbilitySystemComponent m_AbilitySystem;

        [SerializeField]
        private GameplayTag m_AttackInputTag;

        private void OnEnable()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            m_CharacterInput.AttackPerformed +=
                HandleAttackPerformed;

            m_CharacterInput.AttackReleased +=
                HandleAttackReleased;
        }

        private void Update()
        {
            m_AbilitySystem.ProcessAbilityInput(
                Time.deltaTime,
                false);
        }

        private void OnDisable()
        {
            if (m_CharacterInput != null)
            {
                m_CharacterInput.AttackPerformed -=
                    HandleAttackPerformed;

                m_CharacterInput.AttackReleased -=
                    HandleAttackReleased;
            }

            if (m_AbilitySystem != null)
            {
                m_AbilitySystem.ClearAbilityInput();
            }
        }

        private void HandleAttackReleased()
        {
            m_AbilitySystem.AbilityInputTagReleased(
                m_AttackInputTag);
        }

        private void HandleAttackPerformed()
        {
            m_AbilitySystem.AbilityInputTagPressed(
                m_AttackInputTag);
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
                    $"{nameof(CommonAbilitySystemComponent)}.",
                    this);

                return false;
            }

            if (m_AttackInputTag == null)
            {
                Debug.LogError(
                    $"{nameof(LocalAbilityInput)} on " +
                    $"'{name}' requires an attack input tag.",
                    this);

                return false;
            }

            return true;
        }
    }
}