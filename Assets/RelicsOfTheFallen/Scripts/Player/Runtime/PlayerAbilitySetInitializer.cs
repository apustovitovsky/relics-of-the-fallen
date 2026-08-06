using System.Collections.Generic;
using GAS.Common;
using UnityEngine;

namespace RelicsOfTheFallen.Player
{
    /// <summary>
    /// Grants configured common ability sets for the authoritative player ability system.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerAbilitySetInitializer :
        MonoBehaviour
    {
        [field: SerializeField]
        private CommonAbilitySystemComponent AbilitySystem
        {
            get;
            set;
        }

        [field: SerializeField]
        private List<GameplayAbilitySet> InitialAbilitySets
        {
            get;
            set;
        } = new();

        private GameplayAbilitySetGrantedHandles m_GrantedHandles;

        /// <summary>
        /// Grants initial ability sets after the ability-system network role has been configured.
        /// </summary>
        private void Start()
        {
            if (AbilitySystem == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerAbilitySetInitializer)} on " +
                    $"'{name}' requires " +
                    $"{nameof(CommonAbilitySystemComponent)}.",
                    this);

                enabled = false;
                return;
            }

            if (!AbilitySystem.IsOwnerActorAuthoritative())
            {
                return;
            }

            m_GrantedHandles =
                new GameplayAbilitySetGrantedHandles();

            for (
                int index = 0;
                index < InitialAbilitySets.Count;
                index++)
            {
                GameplayAbilitySet abilitySet =
                    InitialAbilitySets[index];

                if (abilitySet == null)
                {
                    Debug.LogError(
                        $"Initial ability set {index} on " +
                        $"'{name}' is not assigned.",
                        this);

                    continue;
                }

                abilitySet.GiveToAbilitySystem(
                    AbilitySystem,
                    m_GrantedHandles,
                    this);
            }
        }

        /// <summary>
        /// Removes ability grants owned by this initializer when the player is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (
                m_GrantedHandles == null ||
                AbilitySystem == null)
            {
                return;
            }

            m_GrantedHandles.TakeFromAbilitySystem(
                AbilitySystem);
        }
    }
}