using System;

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
            m_CastingTagSubscription?.Dispose();

            m_CastingTagSubscription =
                null;

            m_AbilitySystem =
                null;

            if (FrameRoot != null)
            {
                SetFrameVisible(
                    false);
            }
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
        /// Updates cast progress from the currently playing replicated ability montage.
        /// </summary>
        private void RefreshProgress()
        {
            float sectionLength =
                m_AbilitySystem
                    .GetCurrentMontageSectionLength();

            if (sectionLength <= 0f)
            {
                SetProgress(
                    0f);

                return;
            }

            float timeLeft =
                m_AbilitySystem
                    .GetCurrentMontageSectionTimeLeft();

            float progress =
                1f -
                timeLeft /
                sectionLength;

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