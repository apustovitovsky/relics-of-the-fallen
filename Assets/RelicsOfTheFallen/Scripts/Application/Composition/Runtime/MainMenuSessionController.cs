using Mirror;
using RelicsOfTheFallen.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace RelicsOfTheFallen.Application.Composition
{
    [DisallowMultipleComponent]
    public sealed class MainMenuSessionController : MonoBehaviour
    {
        private const string k_ServerAddress = "localhost";

        [SerializeField]
        private Button m_HostRaidButton;

        [SerializeField]
        private Button m_JoinRaidButton;

        private NetworkSessionManager m_NetworkSessionManager;

        private void Awake()
        {
            m_NetworkSessionManager =
                NetworkManager.singleton as NetworkSessionManager;

            if (m_NetworkSessionManager == null)
            {
                Debug.LogError(
                    "NetworkSessionManager was not found.");

                enabled = false;
                return;
            }

            m_HostRaidButton.onClick.AddListener(
                StartHostRaid);

            m_JoinRaidButton.onClick.AddListener(
                JoinRaid);
        }

        private void OnDestroy()
        {
            if (m_HostRaidButton != null)
            {
                m_HostRaidButton.onClick.RemoveListener(
                    StartHostRaid);
            }

            if (m_JoinRaidButton != null)
            {
                m_JoinRaidButton.onClick.RemoveListener(
                    JoinRaid);
            }
        }

        private void StartHostRaid()
        {
            SetSessionButtonsInteractable(false);

            m_NetworkSessionManager.StartHost();
        }

        private void JoinRaid()
        {
            SetSessionButtonsInteractable(false);

            m_NetworkSessionManager.networkAddress =
                k_ServerAddress;

            m_NetworkSessionManager.StartClient();
        }

        private void SetSessionButtonsInteractable(
            bool interactable)
        {
            m_HostRaidButton.interactable =
                interactable;

            m_JoinRaidButton.interactable =
                interactable;
        }
    }
}