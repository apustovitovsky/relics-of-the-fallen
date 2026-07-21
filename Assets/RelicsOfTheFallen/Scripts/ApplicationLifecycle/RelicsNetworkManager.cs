using Mirror;
using RelicsOfTheFallen.GameState.Character;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RelicsOfTheFallen.ApplicationLifecycle
{
    public sealed class RelicsNetworkManager : NetworkManager
    {
        [SerializeField]
        string m_RaidSceneName = "Raid";

        public override void Awake()
        {
            autoCreatePlayer = false;

            base.Awake();
        }

        public override void OnServerReady(
            NetworkConnectionToClient connection)
        {
            base.OnServerReady(connection);

            TrySpawnRaidAvatar(connection);
        }

        public override void OnServerSceneChanged(
            string sceneName)
        {
            base.OnServerSceneChanged(sceneName);

            if (SceneManager.GetActiveScene().name !=
                m_RaidSceneName)
            {
                return;
            }

            foreach (var connection in
                     NetworkServer.connections.Values)
            {
                TrySpawnRaidAvatar(connection);
            }
        }

        void TrySpawnRaidAvatar(
            NetworkConnectionToClient connection)
        {
            if (SceneManager.GetActiveScene().name !=
                    m_RaidSceneName ||
                connection == null ||
                !connection.isReady ||
                connection.identity != null)
            {
                return;
            }

            var raidState =
                FindAnyObjectByType<ServerRaidState>();

            if (raidState == null)
            {
                Debug.LogError(
                    $"No {nameof(ServerRaidState)} exists in " +
                    $"{m_RaidSceneName}.",
                    this);

                return;
            }

            raidState.TrySpawnPlayerAvatar(connection);
        }
    }
}