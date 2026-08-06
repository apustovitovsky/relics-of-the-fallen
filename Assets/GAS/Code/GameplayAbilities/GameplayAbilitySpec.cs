using System;

namespace GAS
{
    public sealed class GameplayAbilitySpec
    {
        public GameplayAbilitySpecHandle Handle
        {
            get;
        }

        public GameplayAbilitySO Ability
        {
            get;
        }

        public UnityEngine.Object SourceObject
        {
            get;
        }

        public int Level
        {
            get; internal set;
        }

        public GameplayTagContainer DynamicAbilityTags
        {
            get;
        } = new();

        public bool InputPressed
        {
            get; internal set;
        }

        public GameplayAbilityActivationInfo ActivationInfo
        {
            get; internal set;
        }

        public GameplayAbility PrimaryInstance
        {
            get; private set;
        }

        /// <summary>
        /// Returns whether this gameplay ability specification currently has an active instance.
        /// </summary>
        public bool IsActive()
        {
            return
                PrimaryInstance != null &&
                PrimaryInstance.IsActive;
        }

        /// <summary>
        /// Creates an authoritative specification for one granted gameplay ability.
        /// </summary>
        public GameplayAbilitySpec(
            GameplayAbilitySO ability,
            int level,
            UnityEngine.Object sourceObject = null) : this(
                GameplayAbilitySpecHandle.GenerateNewHandle(),
                ability,
                level,
                sourceObject)
        {
        }

        /// <summary>
        /// Creates a gameplay ability specification with an existing replicated handle.
        /// </summary>
        public GameplayAbilitySpec(
            GameplayAbilitySpecHandle handle,
            GameplayAbilitySO ability,
            int level,
            UnityEngine.Object sourceObject = null)
        {
            if (!handle.IsValid)
            {
                throw new ArgumentException(
                    "Gameplay ability specification handle must be valid.",
                    nameof(handle));
            }

            if (ability == null)
            {
                throw new ArgumentNullException(
                    nameof(ability));
            }

            if (ability.ga == null)
            {
                throw new InvalidOperationException(
                    $"Ability definition '{ability.name}' has no gameplay ability.");
            }

            Handle =
                handle;

            Ability =
                ability;

            Level =
                level;

            SourceObject =
                sourceObject;

            ActivationInfo =
                new GameplayAbilityActivationInfo(
                    GameplayAbilityActivationMode.Authority);
        }

        /// <summary>
        /// Creates and returns the owner-local primary instance for this ability specification.
        /// </summary>
        internal GameplayAbility CreatePrimaryInstance(
            AbilitySystemComponent owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(
                    nameof(owner));
            }

            if (PrimaryInstance != null)
            {
                throw new InvalidOperationException(
                    $"Ability specification '{Handle}' already has a primary instance.");
            }

            PrimaryInstance =
                Ability.ga.Instantiate(
                    owner,
                    Ability);

            PrimaryInstance.CurrentSpecHandle =
                Handle;

            PrimaryInstance.CurrentActivationInfo =
                ActivationInfo;

            PrimaryInstance.Level =
                Level;

            return PrimaryInstance;
        }
    }
}