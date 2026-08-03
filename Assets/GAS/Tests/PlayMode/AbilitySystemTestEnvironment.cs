using System;
using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace GAS.Tests
{
    internal sealed class AbilitySystemTestEnvironment : IDisposable
    {
        private readonly List<UnityObject> m_Objects = new();

        /// <summary>
        /// Creates an initialized inactive ability system for an isolated Edit Mode test.
        /// </summary>
        public AbilitySystemComponent CreateAbilitySystem(
            string name)
        {
            GameObject actor = new(name);

            actor.SetActive(
                false);

            m_Objects.Add(
                actor);

            AbilitySystemComponent abilitySystem =
                actor.AddComponent<AbilitySystemComponent>();

            abilitySystem.InitAbilityActorInfo(
                actor,
                actor);

            return abilitySystem;
        }

        /// <summary>
        /// Creates a transient attribute identifier owned by this test environment.
        /// </summary>
        public AttributeName CreateAttributeName(
            string name)
        {
            AttributeName attributeName =
                ScriptableObject.CreateInstance<AttributeName>();

            attributeName.name = name;

            m_Objects.Add(
                attributeName);

            return attributeName;
        }

        /// <summary>
        /// Adds an initialized attribute to the supplied ability system.
        /// </summary>
        public Attribute AddAttribute(
            AbilitySystemComponent abilitySystem,
            AttributeName attributeName,
            float baseValue)
        {
            Attribute attribute =
                new(
                    attributeName,
                    baseValue);

            abilitySystem.attributes.Add(
                attribute);

            abilitySystem.AttributesDictionary.Add(
                attributeName.name,
                attribute);

            return attribute;
        }

        /// <summary>
        /// Creates a transient ScriptableObject owned by this test environment.
        /// </summary>
        public T CreateScriptableObject<T>(
            string assetName)
            where T : ScriptableObject
        {
            T asset =
                ScriptableObject.CreateInstance<T>();

            asset.name = assetName;

            m_Objects.Add(
                asset);

            return asset;
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