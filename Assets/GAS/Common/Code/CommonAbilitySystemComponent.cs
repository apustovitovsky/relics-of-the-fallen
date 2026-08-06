using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace GAS.Common
{
    /// <summary>
    /// Extends the core ability system with reusable Lyra-derived orchestration.
    /// </summary>
    public class CommonAbilitySystemComponent :
        AbilitySystemComponent
    {
        private readonly int[] m_ActivationGroupCounts =
            new int[Enum.GetValues(typeof(GameplayAbilityActivationGroup)).Length];

        private readonly List<GameplayAbilitySpecHandle>
            m_InputPressedSpecHandles = new();

        private readonly List<GameplayAbilitySpecHandle>
            m_InputReleasedSpecHandles = new();

        private readonly List<GameplayAbilitySpecHandle>
            m_InputHeldSpecHandles = new();

        [field: SerializeField]
        private GameplayTag AbilityInputBlockedTag
        {
            get;
            set;
        }

        private readonly List<GameplayAbilitySpecHandle>
            m_AbilitiesToActivate = new();

        /// <summary>
        /// Returns whether the requested activation group is blocked by an active blocking ability.
        /// </summary>
        public bool IsActivationGroupBlocked(
            GameplayAbilityActivationGroup group)
        {
            switch (group)
            {
                case GameplayAbilityActivationGroup.Independent:
                    return false;

                case GameplayAbilityActivationGroup.ExclusiveReplaceable:
                case GameplayAbilityActivationGroup.ExclusiveBlocking:
                    int blockingGroupIndex =
                        (int)GameplayAbilityActivationGroup.ExclusiveBlocking;

                    return m_ActivationGroupCounts[blockingGroupIndex] > 0;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(group),
                        group,
                        "Unsupported gameplay ability activation group.");
            }
        }

        /// <summary>
        /// Registers an active ability in its activation group and resolves exclusive conflicts.
        /// </summary>
        public void AddAbilityToActivationGroup(
            GameplayAbilityActivationGroup group,
            CommonGameplayAbility ability)
        {
            if (ability == null)
            {
                throw new ArgumentNullException(
                    nameof(ability));
            }

            int groupIndex =
                GetActivationGroupIndex(
                    group);

            if (m_ActivationGroupCounts[groupIndex] == int.MaxValue)
            {
                throw new InvalidOperationException(
                    $"Activation group '{group}' has reached its maximum count.");
            }

            m_ActivationGroupCounts[groupIndex]++;

            switch (group)
            {
                case GameplayAbilityActivationGroup.Independent:
                    break;

                case GameplayAbilityActivationGroup.ExclusiveReplaceable:
                case GameplayAbilityActivationGroup.ExclusiveBlocking:
                    CancelActivationGroupAbilities(
                        GameplayAbilityActivationGroup.ExclusiveReplaceable,
                        ability,
                        false);

                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(group),
                        group,
                        "Unsupported gameplay ability activation group.");
            }

            int replaceableGroupIndex =
                (int)GameplayAbilityActivationGroup.ExclusiveReplaceable;

            int blockingGroupIndex =
                (int)GameplayAbilityActivationGroup.ExclusiveBlocking;

            int exclusiveCount =
                m_ActivationGroupCounts[replaceableGroupIndex] +
                m_ActivationGroupCounts[blockingGroupIndex];

            if (exclusiveCount > 1)
            {
                Debug.LogError(
                    "Multiple exclusive gameplay abilities are active.",
                    this);
            }
        }

        /// <summary>
        /// Removes a finished ability from its activation group.
        /// </summary>
        public void RemoveAbilityFromActivationGroup(
            GameplayAbilityActivationGroup group,
            CommonGameplayAbility ability)
        {
            if (ability == null)
            {
                throw new ArgumentNullException(
                    nameof(ability));
            }

            int groupIndex =
                GetActivationGroupIndex(
                    group);

            if (m_ActivationGroupCounts[groupIndex] <= 0)
            {
                throw new InvalidOperationException(
                    $"Activation group '{group}' contains no active abilities.");
            }

            m_ActivationGroupCounts[groupIndex]--;
        }

        /// <summary>
        /// Cancels active common abilities belonging to the requested activation group.
        /// </summary>
        public void CancelActivationGroupAbilities(
            GameplayAbilityActivationGroup group,
            CommonGameplayAbility ignoreAbility,
            bool replicateCancelAbility)
        {
            _ = GetActivationGroupIndex(
                group);

            IReadOnlyList<GameplayAbilitySpec> abilitySpecs =
                ActivatableAbilities.Items;

            for (
                int index = 0;
                index < abilitySpecs.Count;
                index++)
            {
                GameplayAbilitySpec abilitySpec =
                    abilitySpecs[index];

                CommonGameplayAbility ability =
                    GetCommonAbility(
                        abilitySpec);

                if (
                    !ability.IsActive ||
                    ReferenceEquals(
                        ability,
                        ignoreAbility) ||
                    ability.GetActivationGroup() != group)
                {
                    continue;
                }

                ability.CancelAbility(
                    abilitySpec.Handle,
                    AbilityActorInfo,
                    ability.CurrentActivationInfo,
                    replicateCancelAbility);
            }
        }

        private int GetActivationGroupIndex(
            GameplayAbilityActivationGroup group)
        {
            int groupIndex =
                (int)group;

            if (
                groupIndex < 0 ||
                groupIndex >= m_ActivationGroupCounts.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(group),
                    group,
                    "Unsupported gameplay ability activation group.");
            }

            return groupIndex;
        }

        /// <summary>
        /// Registers a newly active common ability in its configured activation group.
        /// </summary>
        public override void NotifyAbilityActivated(
            GameplayAbilitySpecHandle handle,
            GameplayAbility gameplayAbility)
        {
            base.NotifyAbilityActivated(
                handle,
                gameplayAbility);

            CommonGameplayAbility commonAbility =
                (CommonGameplayAbility)gameplayAbility;

            AddAbilityToActivationGroup(
                commonAbility.GetActivationGroup(),
                commonAbility);
        }

        /// <summary>
        /// Removes a finished common ability from its configured activation group.
        /// </summary>
        public override void NotifyAbilityEnded(
            GameplayAbilitySpecHandle handle,
            GameplayAbility gameplayAbility,
            bool wasCancelled)
        {
            base.NotifyAbilityEnded(
                handle,
                gameplayAbility,
                wasCancelled);

            CommonGameplayAbility commonAbility =
                (CommonGameplayAbility)gameplayAbility;

            RemoveAbilityFromActivationGroup(
                commonAbility.GetActivationGroup(),
                commonAbility);
        }

        /// <summary>
        /// Records ability specifications associated with a pressed input gameplay tag.
        /// </summary>
        public void AbilityInputTagPressed(
            GameplayTag inputTag)
        {
            if (inputTag == null)
            {
                return;
            }

            IReadOnlyList<GameplayAbilitySpec> abilitySpecs =
                ActivatableAbilities.Items;

            for (
                int index = 0;
                index < abilitySpecs.Count;
                index++)
            {
                GameplayAbilitySpec abilitySpec =
                    abilitySpecs[index];

                if (!abilitySpec.DynamicAbilityTags.HasTagExact(
                        inputTag))
                {
                    continue;
                }

                if (!m_InputPressedSpecHandles.Contains(
                        abilitySpec.Handle))
                {
                    m_InputPressedSpecHandles.Add(
                        abilitySpec.Handle);
                }

                if (!m_InputHeldSpecHandles.Contains(
                        abilitySpec.Handle))
                {
                    m_InputHeldSpecHandles.Add(
                        abilitySpec.Handle);
                }
            }
        }

        /// <summary>
        /// Records ability specifications associated with a released input gameplay tag.
        /// </summary>
        public void AbilityInputTagReleased(
            GameplayTag inputTag)
        {
            if (inputTag == null)
            {
                return;
            }

            IReadOnlyList<GameplayAbilitySpec> abilitySpecs =
                ActivatableAbilities.Items;

            for (
                int index = 0;
                index < abilitySpecs.Count;
                index++)
            {
                GameplayAbilitySpec abilitySpec =
                    abilitySpecs[index];

                if (!abilitySpec.DynamicAbilityTags.HasTagExact(
                        inputTag))
                {
                    continue;
                }

                if (!m_InputReleasedSpecHandles.Contains(
                        abilitySpec.Handle))
                {
                    m_InputReleasedSpecHandles.Add(
                        abilitySpec.Handle);
                }

                m_InputHeldSpecHandles.Remove(
                    abilitySpec.Handle);
            }
        }

        /// <summary>
        /// Processes cached ability input using the common activation policies.
        /// </summary>
        public void ProcessAbilityInput(
            float deltaTime,
            bool gamePaused)
        {
            _ = deltaTime;
            _ = gamePaused;

            if (
                AbilityInputBlockedTag != null &&
                HasMatchingGameplayTag(
                    AbilityInputBlockedTag))
            {
                ClearAbilityInput();
                return;
            }

            m_AbilitiesToActivate.Clear();

            for (
                int index = 0;
                index < m_InputHeldSpecHandles.Count;
                index++)
            {
                GameplayAbilitySpecHandle handle =
                    m_InputHeldSpecHandles[index];

                GameplayAbilitySpec abilitySpec =
                    FindAbilitySpecFromHandle(
                        handle);

                if (
                    abilitySpec == null ||
                    abilitySpec.IsActive())
                {
                    continue;
                }

                CommonGameplayAbility ability =
                    GetCommonAbility(
                        abilitySpec);

                if (
                    ability.GetActivationPolicy() ==
                    GameplayAbilityActivationPolicy.WhileInputActive)
                {
                    AddAbilityToActivationQueue(
                        handle);
                }
            }

            for (
                int index = 0;
                index < m_InputPressedSpecHandles.Count;
                index++)
            {
                GameplayAbilitySpecHandle handle =
                    m_InputPressedSpecHandles[index];

                GameplayAbilitySpec abilitySpec =
                    FindAbilitySpecFromHandle(
                        handle);

                if (abilitySpec == null)
                {
                    continue;
                }

                bool wasActive =
                    abilitySpec.IsActive();

                AbilitySpecInputPressed(
                    handle);

                if (wasActive)
                {
                    continue;
                }

                CommonGameplayAbility ability =
                    GetCommonAbility(
                        abilitySpec);

                if (
                    ability.GetActivationPolicy() ==
                    GameplayAbilityActivationPolicy.OnInputTriggered)
                {
                    AddAbilityToActivationQueue(
                        handle);
                }
            }

            for (
                int index = 0;
                index < m_AbilitiesToActivate.Count;
                index++)
            {
                TryActivateAbility(
                    m_AbilitiesToActivate[index]).Forget();
            }

            for (
                int index = 0;
                index < m_InputReleasedSpecHandles.Count;
                index++)
            {
                AbilitySpecInputReleased(
                    m_InputReleasedSpecHandles[index]);
            }

            m_InputPressedSpecHandles.Clear();
            m_InputReleasedSpecHandles.Clear();
        }

        /// <summary>
        /// Clears all cached ability input state owned by this ability system.
        /// </summary>
        public void ClearAbilityInput()
        {
            m_InputPressedSpecHandles.Clear();
            m_InputReleasedSpecHandles.Clear();
            m_InputHeldSpecHandles.Clear();
        }

        /// <summary>
        /// Returns the common ability represented by the supplied gameplay ability specification.
        /// </summary>
        private static CommonGameplayAbility GetCommonAbility(
            GameplayAbilitySpec abilitySpec)
        {
            if (
                abilitySpec.PrimaryInstance is
                    CommonGameplayAbility commonAbility)
            {
                return commonAbility;
            }

            throw new InvalidOperationException(
                $"Ability specification '{abilitySpec.Handle}' " +
                $"does not contain {nameof(CommonGameplayAbility)}.");
        }

        /// <summary>
        /// Adds one unique gameplay ability specification handle to the activation queue.
        /// </summary>
        private void AddAbilityToActivationQueue(
            GameplayAbilitySpecHandle handle)
        {
            if (m_AbilitiesToActivate.Contains(
                    handle))
            {
                return;
            }

            m_AbilitiesToActivate.Add(
                handle);
        }
    }
}