using TMPro;
using UnityEngine;

namespace RelicsOfTheFallen.Targeting
{
    [DefaultExecutionOrder(300)]
    [DisallowMultipleComponent]
    public sealed class TargetFramePresenter :
        MonoBehaviour
    {
        [field: SerializeField]
        private GameObject FrameRoot
        {
            get; set;
        }

        [field: SerializeField]
        private TMP_Text TargetName
        {
            get; set;
        }

        private TargetingController m_Targeting;
        private Camera m_Camera;

        private void Awake()
        {
            if (FrameRoot == null ||
                TargetName == null)
            {
                Debug.LogError(
                    $"{nameof(TargetFramePresenter)} on '{name}' requires " +
                    "frame root and target name references.",
                    this);

                enabled = false;
                return;
            }

            SetFrameVisible(false);
        }

        private void LateUpdate()
        {
            if (m_Targeting == null ||
                m_Camera == null)
            {
                SetFrameVisible(false);
                return;
            }

            ITargetable currentTarget = m_Targeting.CurrentTarget;

            if (currentTarget == null ||
                string.IsNullOrWhiteSpace(
                    currentTarget.DisplayName))
            {
                SetFrameVisible(false);
                return;
            }

            Vector3 screenPosition = m_Camera.WorldToScreenPoint(
                currentTarget.UiAnchor.position);

            if (screenPosition.z <= 0f)
            {
                SetFrameVisible(false);
                return;
            }

            FrameRoot.transform.position = screenPosition;

            if (TargetName.text != currentTarget.DisplayName)
            {
                TargetName.SetText(
                    currentTarget.DisplayName);
            }

            SetFrameVisible(true);
        }

        private void OnDisable()
        {
            if (FrameRoot != null)
            {
                SetFrameVisible(false);
            }
        }

        /// <summary>
        /// Binds this target frame to the locally controlled character.
        /// </summary>
        public void Bind(
            TargetingController targeting,
            Camera targetCamera)
        {
            if (targeting == null ||
                targetCamera == null)
            {
                Debug.LogError(
                    $"{nameof(TargetFramePresenter)} cannot bind without " +
                    "targeting and camera references.",
                    this);

                Unbind();
                return;
            }

            m_Targeting = targeting;
            m_Camera = targetCamera;
        }

        /// <summary>
        /// Releases the locally controlled character from this target frame.
        /// </summary>
        public void Unbind()
        {
            m_Targeting = null;
            m_Camera = null;

            if (TargetName != null)
            {
                TargetName.SetText(string.Empty);
            }

            if (FrameRoot != null)
            {
                SetFrameVisible(false);
            }
        }

        /// <summary>
        /// Changes the visibility of the current target frame.
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