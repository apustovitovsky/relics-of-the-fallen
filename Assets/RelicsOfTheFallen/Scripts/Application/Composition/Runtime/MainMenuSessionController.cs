using Mirror;
using RelicsOfTheFallen.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace RelicsOfTheFallen.Application.Composition
{
    [DisallowMultipleComponent]
    public sealed class MainMenuSessionController : MonoBehaviour
    {
        const string k_ServerAddress = "localhost";

        [SerializeField]
        Button m_HostRaidButton;

        [SerializeField]
        Button m_JoinRaidButton;

        NetworkSessionManager m_NetworkSessionManager;

        void Awake()
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

        void OnDestroy()
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

        void StartHostRaid()
        {
            SetSessionButtonsInteractable(false);

            m_NetworkSessionManager.StartHost();
        }

        void JoinRaid()
        {
            SetSessionButtonsInteractable(false);

            m_NetworkSessionManager.networkAddress =
                k_ServerAddress;

            m_NetworkSessionManager.StartClient();
        }

        void SetSessionButtonsInteractable(
            bool interactable)
        {
            m_HostRaidButton.interactable =
                interactable;

            m_JoinRaidButton.interactable =
                interactable;
        }
    }
}