using Mirror;
using UnityEngine;
using VContainer;

namespace RelicsOfTheFallen.ConnectionManagement
{
    public sealed class ConnectionManager : MonoBehaviour
    {
        NetworkManager m_NetworkManager;

        [Inject]
        void Construct(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
        }

        public bool StartHost()
        {
            if (IsNetworkActive)
            {
                return false;
            }

            m_NetworkManager.StartHost();

            return NetworkServer.active &&
                   NetworkClient.active;
        }

        public bool StartClient()
        {
            if (IsNetworkActive)
            {
                return false;
            }

            m_NetworkManager.StartClient();

            return NetworkClient.active;
        }

        public void Shutdown()
        {
            if (NetworkServer.active &&
                NetworkClient.active)
            {
                m_NetworkManager.StopHost();
            }
            else if (NetworkServer.active)
            {
                m_NetworkManager.StopServer();
            }
            else if (NetworkClient.active)
            {
                m_NetworkManager.StopClient();
            }
        }

        bool IsNetworkActive =>
            NetworkServer.active ||
            NetworkClient.active;
    }
}