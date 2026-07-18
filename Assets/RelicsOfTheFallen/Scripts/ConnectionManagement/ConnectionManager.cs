using Unity.Netcode;
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
            return !m_NetworkManager.IsListening && m_NetworkManager.StartHost();
        }

        public bool StartClient()
        {
            return !m_NetworkManager.IsListening && m_NetworkManager.StartClient();
        }

        public void Shutdown()
        {
            if (m_NetworkManager.IsListening)
            {
                m_NetworkManager.Shutdown();
            }
        }
    }
}