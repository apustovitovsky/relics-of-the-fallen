using System;
using System.Collections.Generic;
using GAS;
using GAS.Common;
using UnityEngine;


namespace RelicsOfTheFallen.UI.AbilitySystem
{
    [DisallowMultipleComponent]
    public sealed class CastBarPresenter :
        MonoBehaviour
    {
        [field: SerializeField]
        private GameObject FrameRoot
        {
            get;
            set;
        }

        [field: SerializeField]
        private RectTransform ProgressFill
        {
            get;
            set;
        }

        private AbilitySystemComponent m_AbilitySystem;
        private GameplayEffectQuery m_CastingQuery;

        private IDisposable m_ActiveEffectAddedSubscription;
        private IDisposable m_CastingTagSubscription;

        private void Awake()
        {
            if (
                FrameRoot == null ||
                ProgressFill == null)
            {
                Debug.LogError(
                    $"{nameof(CastBarPresenter)} on '{name}' requires " +
                    "a frame root and progress fill rect transform.",
                    this);

                enabled = false;
                return;
            }

            SetFrameVisible(
                false);
        }

        private void Update()
        {
            if (
                m_AbilitySystem == null ||
                !FrameRoot.activeSelf)
            {
                return;
            }

            RefreshProgress();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        /// <summary>
        /// Binds the cast bar to one locally observed ability system component.
        /// </summary>
        public void Bind(
            AbilitySystemComponent abilitySystem)
        {
            if (abilitySystem == null)
            {
                throw new ArgumentNullException(
                    nameof(abilitySystem));
            }

            Unbind();

            m_AbilitySystem =
                abilitySystem;

            GameplayTagContainer castingTags =
                new();

            castingTags.AddTag(
                CommonGameplayTags.StateCastingTag);

            m_CastingQuery =
                GameplayEffectQuery.MakeQuery_MatchAnyOwningTags(
                    castingTags);

            m_ActiveEffectAddedSubscription =
                abilitySystem
                    .RegisterActiveGameplayEffectAddedDelegateToSelf(
                        HandleActiveGameplayEffectAdded);

            m_CastingTagSubscription =
                abilitySystem.RegisterGameplayTagEvent(
                    CommonGameplayTags.StateCastingTag,
                    GameplayTagEventType.NewOrRemoved,
                    HandleCastingTagChanged);

            bool isCasting =
                abilitySystem.HasMatchingGameplayTag(
                    CommonGameplayTags.StateCastingTag);

            SetFrameVisible(
                isCasting);

            if (isCasting)
            {
                RefreshProgress();
            }
        }

        /// <summary>
        /// Releases the observed ability system and hides the cast bar.
        /// </summary>
        public void Unbind()
        {
            m_ActiveEffectAddedSubscription?.Dispose();
            m_CastingTagSubscription?.Dispose();

            m_ActiveEffectAddedSubscription =
                null;

            m_CastingTagSubscription =
                null;

            m_AbilitySystem =
                null;

            m_CastingQuery =
                null;

            if (FrameRoot != null)
            {
                SetFrameVisible(
                    false);
            }
        }

        /// <summary>
        /// Refreshes the cast bar when a matching active gameplay effect is registered.
        /// </summary>
        private void HandleActiveGameplayEffectAdded(
            AbilitySystemComponent target,
            GameplayEffectSpec appliedSpec,
            ActiveGameplayEffectHandle activeHandle)
        {
            if (
                !ReferenceEquals(
                    target,
                    m_AbilitySystem))
            {
                return;
            }

            ActiveGameplayEffect activeEffect =
                target.GetActiveGameplayEffect(
                    activeHandle);

            if (
                activeEffect == null ||
                !ReferenceEquals(
                    activeEffect.Spec,
                    appliedSpec) ||
                !m_CastingQuery.Matches(
                    activeEffect))
            {
                return;
            }

            SetFrameVisible(
                true);

            RefreshProgress();
        }

        /// <summary>
        /// Shows or hides the cast bar when the reusable casting tag count changes.
        /// </summary>
        private void HandleCastingTagChanged(
            GameplayTag changedTag,
            int newCount)
        {
            if (
                changedTag !=
                CommonGameplayTags.StateCastingTag)
            {
                return;
            }

            bool isCasting =
                newCount > 0;

            SetFrameVisible(
                isCasting);

            if (isCasting)
            {
                RefreshProgress();
            }
        }

        /// <summary>
        /// Updates the cast progress from the longest matching active gameplay effect.
        /// </summary>
        private void RefreshProgress()
        {
            List<(
                float TimeRemaining,
                float Duration)> effectTimes =
                    m_AbilitySystem
                        .GetActiveEffectsTimeRemainingAndDuration(
                            m_CastingQuery);

            if (effectTimes.Count == 0)
            {
                SetProgress(
                    0f);

                return;
            }

            (
                float TimeRemaining,
                float Duration) selectedTime =
                    effectTimes[0];

            for (
                int index = 1;
                index < effectTimes.Count;
                index++)
            {
                if (
                    effectTimes[index].TimeRemaining >
                    selectedTime.TimeRemaining)
                {
                    selectedTime =
                        effectTimes[index];
                }
            }

            if (selectedTime.Duration <= 0f)
            {
                SetProgress(
                    0f);

                return;
            }

            float progress =
                1f -
                selectedTime.TimeRemaining /
                selectedTime.Duration;

            SetProgress(
                progress);
        }

        /// <summary>
        /// Changes the horizontal scale of the cast progress rectangle.
        /// </summary>
        private void SetProgress(
            float progress)
        {
            Vector3 localScale =
                ProgressFill.localScale;

            localScale.x =
                Mathf.Clamp01(
                    progress);

            ProgressFill.localScale =
                localScale;
        }

        /// <summary>
        /// Changes cast-bar visibility and clears progress when casting ends.
        /// </summary>
        private void SetFrameVisible(
            bool isVisible)
        {
            if (!isVisible)
            {
                SetProgress(
                    0f);
            }

            if (
                FrameRoot.activeSelf ==
                isVisible)
            {
                return;
            }

            FrameRoot.SetActive(
                isVisible);
        }
    }
}