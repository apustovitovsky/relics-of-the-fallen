using System.Collections.Generic;
using System.Text;
using GAS;
using RelicsOfTheFallen.Targeting;
using TMPro;
using UnityEngine;
using RelicsOfTheFallen.UI.AbilitySystem;

namespace RelicsOfTheFallen.UI.Debug
{
    [DefaultExecutionOrder(300)]
    [DisallowMultipleComponent]
    public sealed class AbilitySystemDebugPresenter :
        MonoBehaviour
    {
        private const float k_RefreshIntervalSeconds = 0.1f;

        [System.Serializable]
        private sealed class ResourceAttributePair
        {
            [field: SerializeField]
            internal AttributeName Attribute
            {
                get;
                set;
            }

            [field: SerializeField]
            internal AttributeName MaxAttribute
            {
                get;
                set;
            }
        }

        [field: SerializeField]
        private GameObject FrameRoot
        {
            get;
            set;
        }

        [field: SerializeField]
        private TMP_Text DebugText
        {
            get;
            set;
        }

        [field: SerializeField]
        private CastBarPresenter CastBar
        {
            get;
            set;
        }

        [field: SerializeField]
        private List<AttributeName> StatAttributes
        {
            get;
            set;
        } = new();

        [field: SerializeField]
        private List<ResourceAttributePair> ResourceAttributes
        {
            get;
            set;
        } = new();

        private readonly StringBuilder m_ContentBuilder = new();

        private TargetingController m_Targeting;
        private Camera m_Camera;
        private ITargetable m_CurrentTarget;
        private float m_NextRefreshTime;

        private void Awake()
        {
            bool hasNoAttributes =
                StatAttributes.Count == 0 &&
                ResourceAttributes.Count == 0;

            if (
                FrameRoot == null ||
                DebugText == null ||
                CastBar == null ||
                hasNoAttributes)
            {
                UnityEngine.Debug.LogError(
                    $"{nameof(AbilitySystemDebugPresenter)} on '{name}' " +
                    "requires a frame root, debug text, cast bar, and " +
                    "debug attributes.",
                    this);

                enabled = false;
                return;
            }

            SetFrameVisible(
                false);
        }

        private void LateUpdate()
        {
            if (
                m_Targeting == null ||
                m_Camera == null)
            {
                HideFrame();
                return;
            }

            ITargetable currentTarget =
                m_Targeting.CurrentTarget;

            if (
                currentTarget == null ||
                currentTarget.TargetActor == null ||
                currentTarget.UiAnchor == null)
            {
                HideFrame();
                return;
            }

            if (
                !AbilitySystemGlobals.TryGetAbilitySystemComponentFromActor(
                    currentTarget.TargetActor,
                    out AbilitySystemComponent abilitySystem))
            {
                HideFrame();
                return;
            }

            Vector3 screenPosition =
                m_Camera.WorldToScreenPoint(
                    currentTarget.UiAnchor.position);

            if (screenPosition.z <= 0f)
            {
                HideFrame();
                return;
            }

            FrameRoot.transform.position =
                screenPosition;

            bool targetChanged =
                !ReferenceEquals(
                    m_CurrentTarget,
                    currentTarget);

            if (targetChanged)
            {
                m_CurrentTarget =
                    currentTarget;

                m_NextRefreshTime =
                    0f;

                CastBar.Bind(
                    abilitySystem);
            }

            if (
                targetChanged ||
                Time.unscaledTime >= m_NextRefreshTime)
            {
                m_NextRefreshTime =
                    Time.unscaledTime +
                    k_RefreshIntervalSeconds;

                RefreshDebugText(
                    currentTarget,
                    abilitySystem);
            }

            SetFrameVisible(
                true);
        }

        private void OnDisable()
        {
            if (FrameRoot != null)
            {
                HideFrame();
            }
        }

        /// <summary>
        /// Binds this debug presenter to the locally controlled targeting component.
        /// </summary>
        public void Bind(
            TargetingController targeting,
            Camera targetCamera)
        {
            if (targeting == null ||
                targetCamera == null)
            {
                UnityEngine.Debug.LogError(
                    $"{nameof(AbilitySystemDebugPresenter)} cannot bind " +
                    "without targeting and camera references.",
                    this);

                Unbind();
                return;
            }

            m_Targeting = targeting;
            m_Camera = targetCamera;
            m_NextRefreshTime = 0f;
        }

        /// <summary>
        /// Releases the locally controlled character from this debug presenter.
        /// </summary>
        public void Unbind()
        {
            m_Targeting =
                null;

            m_Camera =
                null;

            m_NextRefreshTime =
                0f;

            HideFrame();

            if (DebugText != null)
            {
                DebugText.SetText(
                    string.Empty);
            }
        }

        /// <summary>
        /// Rebuilds the displayed ability-system values for the current debug target.
        /// </summary>
        private void RefreshDebugText(
            ITargetable currentTarget,
            AbilitySystemComponent abilitySystem)
        {
            m_ContentBuilder.Clear();
            m_ContentBuilder.Append(
                currentTarget.TargetActor.name);

            AppendStatAttributes(
                abilitySystem);

            AppendResourceAttributes(
                abilitySystem);

            DebugText.SetText(
                m_ContentBuilder.ToString());
        }

        /// <summary>
        /// Appends independent gameplay attributes to the current debug output.
        /// </summary>
        private void AppendStatAttributes(
            AbilitySystemComponent abilitySystem)
        {
            if (StatAttributes.Count == 0)
            {
                return;
            }

            m_ContentBuilder.AppendLine();
            m_ContentBuilder.AppendLine();
            m_ContentBuilder.Append("<b>Stats</b>");

            for (
                int index = 0;
                index < StatAttributes.Count;
                index++)
            {
                AttributeName attributeName =
                    StatAttributes[index];

                if (attributeName == null)
                {
                    continue;
                }

                m_ContentBuilder.AppendLine();
                m_ContentBuilder.Append(
                    attributeName.name);
                m_ContentBuilder.Append(": ");

                AppendAttributeValue(
                    abilitySystem,
                    attributeName);
            }
        }

        /// <summary>
        /// Appends current and maximum resource attribute pairs to the debug output.
        /// </summary>
        private void AppendResourceAttributes(
            AbilitySystemComponent abilitySystem)
        {
            if (ResourceAttributes.Count == 0)
            {
                return;
            }

            m_ContentBuilder.AppendLine();
            m_ContentBuilder.AppendLine();
            m_ContentBuilder.Append("<b>Resources</b>");

            for (
                int index = 0;
                index < ResourceAttributes.Count;
                index++)
            {
                ResourceAttributePair resource =
                    ResourceAttributes[index];

                if (resource == null ||
                    resource.Attribute == null ||
                    resource.MaxAttribute == null)
                {
                    continue;
                }

                m_ContentBuilder.AppendLine();
                m_ContentBuilder.Append(
                    resource.Attribute.name);
                m_ContentBuilder.Append(": ");

                AppendAttributeValue(
                    abilitySystem,
                    resource.Attribute);

                m_ContentBuilder.Append(" / ");

                AppendAttributeValue(
                    abilitySystem,
                    resource.MaxAttribute);
            }
        }

        /// <summary>
        /// Appends one evaluated gameplay attribute value to the debug output.
        /// </summary>
        private void AppendAttributeValue(
            AbilitySystemComponent abilitySystem,
            AttributeName attributeName)
        {
            if (
                abilitySystem.AttributesDictionary.TryGetValue(
                    attributeName.name,
                    out Attribute attribute))
            {
                m_ContentBuilder.AppendFormat(
                    "{0:0.##}",
                    attribute.CurrentValue);

                return;
            }

            m_ContentBuilder.Append(
                "Not registered");
        }

        /// <summary>
        /// Hides the debug frame and releases its current inspected target.
        /// </summary>
        private void HideFrame()
        {
            if (m_CurrentTarget != null)
            {
                CastBar.Unbind();

                m_CurrentTarget =
                    null;
            }

            SetFrameVisible(
                false);
        }

        /// <summary>
        /// Changes the visibility of the ability-system debug frame.
        /// </summary>
        private void SetFrameVisible(bool isVisible)
        {
            if (FrameRoot.activeSelf == isVisible)
            {
                return;
            }

            FrameRoot.SetActive(isVisible);
        }
    }
}