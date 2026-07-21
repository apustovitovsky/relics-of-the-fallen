using GameStateId = RelicsOfTheFallen.GameState.GameState;
using Mirror;
using RelicsOfTheFallen.ConnectionManagement;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace RelicsOfTheFallen.GameState.Composition
{
    public sealed class ClientMainMenuState : GameStateBehaviour
    {
        [SerializeField]
        Button m_HostButton;

        [SerializeField]
        Button m_JoinButton;

        ConnectionManager m_ConnectionManager;
        NetworkManager m_NetworkManager;

        public override GameStateId ActiveState => GameStateId.MainMenu;

        [Inject]
        void Construct(
            ConnectionManager connectionManager,
            NetworkManager networkManager)
        {
            m_ConnectionManager = connectionManager;
            m_NetworkManager = networkManager;
        }

        protected override void Awake()
        {
            base.Awake();

            m_HostButton.onClick.AddListener(StartHost);
            m_JoinButton.onClick.AddListener(StartClient);
        }

        protected override void OnDestroy()
        {
            m_HostButton.onClick.RemoveListener(StartHost);
            m_JoinButton.onClick.RemoveListener(StartClient);

            base.OnDestroy();
        }

        void StartHost()
        {
            if (m_ConnectionManager.StartHost())
            {
                m_NetworkManager.ServerChangeScene("Raid");
            }
        }

        void StartClient()
        {
            m_ConnectionManager.StartClient();
        }
    }
}