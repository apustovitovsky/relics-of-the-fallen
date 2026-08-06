using System;
using System.Collections.Generic;
using UnityEngine;

namespace GAS.Common
{
    /// <summary>
    /// Defines reusable gameplay abilities granted together to one common ability system.
    /// </summary>
    [CreateAssetMenu(
        menuName = "GAS/Common/Gameplay Ability Set",
        fileName = "AbilitySet_")]
    public sealed class GameplayAbilitySet :
        ScriptableObject
    {
        [field: SerializeField]
        private List<GameplayAbilitySetEntry> GrantedGameplayAbilities
        {
            get;
            set;
        } = new();

        /// <summary>
        /// Grants this ability set to an authoritative common ability system.
        /// </summary>
        public void GiveToAbilitySystem(
            CommonAbilitySystemComponent abilitySystem,
            GameplayAbilitySetGrantedHandles grantedHandles = null,
            UnityEngine.Object sourceObject = null)
        {
            if (abilitySystem == null)
            {
                throw new ArgumentNullException(
                    nameof(abilitySystem));
            }

            if (!abilitySystem.IsOwnerActorAuthoritative())
            {
                return;
            }

            for (
                int index = 0;
                index < GrantedGameplayAbilities.Count;
                index++)
            {
                GameplayAbilitySetEntry entry =
                    GrantedGameplayAbilities[index];

                if (
                    entry == null ||
                    entry.Ability == null)
                {
                    Debug.LogError(
                        $"Granted ability entry {index} on " +
                        $"'{name}' is not valid.",
                        this);

                    continue;
                }

                if (
                    entry.Ability.ga is not
                        CommonGameplayAbility)
                {
                    Debug.LogError(
                        $"Granted ability '{entry.Ability.name}' " +
                        $"must contain {nameof(CommonGameplayAbility)}.",
                        this);

                    continue;
                }

                if (entry.AbilityLevel <= 0)
                {
                    Debug.LogError(
                        $"Granted ability '{entry.Ability.name}' " +
                        $"must have a positive level.",
                        this);

                    continue;
                }

                GameplayAbilitySpec abilitySpec =
                    new(
                        entry.Ability,
                        entry.AbilityLevel,
                        sourceObject);

                abilitySpec.DynamicAbilityTags.AddTag(
                    entry.InputTag);

                GameplayAbilitySpecHandle handle =
                    abilitySystem.GiveAbility(
                        abilitySpec);

                if (grantedHandles != null)
                {
                    grantedHandles.AddAbilitySpecHandle(
                        handle);
                }
            }
        }
    }
}