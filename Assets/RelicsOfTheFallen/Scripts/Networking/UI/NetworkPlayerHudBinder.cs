using Mirror;
using RelicsOfTheFallen.Targeting;
using UnityEngine;

namespace RelicsOfTheFallen.Networking.UI
{
    [DisallowMultipleComponent]
    public sealed class NetworkPlayerHudBinder :
        MonoBehaviour
    {
        [field: SerializeField]
        private TargetFramePresenter TargetFrame
        {
            get; set;
        }

        private NetworkIdentity m_LocalPlayer;

        private void Awake()
        {
            if (TargetFrame == null)
            {
                Debug.LogError(
                    $"{nameof(NetworkPlayerHudBinder)} on '{name}' requires " +
                    "a target frame presenter.",
                    this);

                enabled = false;
            }
        }

        private void Update()
        {
            RefreshBinding();
        }

        private void OnDisable()
        {
            ClearBinding();
        }

        /// <summary>
        /// Refreshes the HUD binding when the local player object changes.
        /// </summary>
        private void RefreshBinding()
        {
            NetworkIdentity localPlayer = NetworkClient.localPlayer;

            if (m_LocalPlayer == localPlayer)
            {
                return;
            }

            ClearBinding();

            if (localPlayer == null)
            {
                return;
            }

            TargetingController targeting =
                localPlayer.GetComponentInChildren<
                    TargetingController>(
                    true);

            Camera targetCamera = Camera.main;

            if (targeting == null ||
                targetCamera == null)
            {
                Debug.LogError(
                    $"{nameof(NetworkPlayerHudBinder)} cannot bind the HUD " +
                    "because the local player targeting or main camera " +
                    "is missing.",
                    localPlayer);

                enabled = false;
                return;
            }

            m_LocalPlayer = localPlayer;

            TargetFrame.Bind(
                targeting,
                targetCamera);
        }

        /// <summary>
        /// Releases the current local player from every HUD presenter.
        /// </summary>
        private void ClearBinding()
        {
            m_LocalPlayer = null;

            if (TargetFrame != null)
            {
                TargetFrame.Unbind();
            }
        }
    }
}