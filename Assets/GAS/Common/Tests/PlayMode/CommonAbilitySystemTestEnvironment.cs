using System;
using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace GAS.Common.Tests
{
    internal sealed class CommonAbilitySystemTestEnvironment :
        IDisposable
    {
        private readonly List<UnityObject> m_Objects = new();

        /// <summary>
        /// Creates an initialized common ability system owned by this test environment.
        /// </summary>
        public CommonAbilitySystemComponent CreateAbilitySystem(
            string name)
        {
            GameObject actor = new(name);

            actor.SetActive(
                false);

            m_Objects.Add(
                actor);

            CommonAbilitySystemComponent abilitySystem =
                actor.AddComponent<CommonAbilitySystemComponent>();

            abilitySystem.InitAbilityActorInfo(
                actor,
                actor);

            return abilitySystem;
        }

        /// <summary>
        /// Creates a persistent test ability definition with the requested activation group.
        /// </summary>
        public GameplayAbilitySO CreateAbility(
            string name,
            GameplayAbilityActivationGroup activationGroup)
        {
            CommonGameplayAbilityTestAsset definition =
                ScriptableObject.CreateInstance<
                    CommonGameplayAbilityTestAsset>();

            definition.name =
                name;

            m_Objects.Add(
                definition);

            ActivationGroupTestAbility ability =
                new()
                {
                    name = name
                };

            ability.SetActivationGroup(
                activationGroup);

            ability.abilityTags.initialized =
                true;

            definition.ga =
                ability;

            return definition;
        }

        /// <summary>
        /// Destroys every transient Unity object created by this test environment.
        /// </summary>
        public void Dispose()
        {
            for (
                int index = m_Objects.Count - 1;
                index >= 0;
                index--)
            {
                UnityObject.DestroyImmediate(
                    m_Objects[index]);
            }

            m_Objects.Clear();
        }
    }
}