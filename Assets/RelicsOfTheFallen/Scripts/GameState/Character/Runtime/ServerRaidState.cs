using Mirror;
using UnityEngine;

namespace RelicsOfTheFallen.GameState.Character
{
    public sealed class ServerRaidState : MonoBehaviour
    {
        [SerializeField]
        NetworkIdentity m_PlayerAvatarPrefab;

        [SerializeField]
        Transform[] m_PlayerSpawnPoints;

        int m_NextSpawnPointIndex;

        public bool TrySpawnPlayerAvatar(
            NetworkConnectionToClient connection)
        {
            if (!NetworkServer.active ||
                connection == null ||
                !connection.isReady ||
                connection.identity != null)
            {
                return false;
            }

            if (m_PlayerAvatarPrefab == null ||
                m_PlayerSpawnPoints == null ||
                m_PlayerSpawnPoints.Length == 0)
            {
                Debug.LogError(
                    $"{nameof(ServerRaidState)} requires a " +
                    "player avatar prefab and at least one " +
                    "spawn point.",
                    this);

                return false;
            }

            Transform spawnPoint =
                m_PlayerSpawnPoints[
                    m_NextSpawnPointIndex %
                    m_PlayerSpawnPoints.Length];

            m_NextSpawnPointIndex++;

            var playerAvatar = Instantiate(
                m_PlayerAvatarPrefab,
                spawnPoint.position,
                spawnPoint.rotation);

            if (!NetworkServer.AddPlayerForConnection(
                    connection,
                    playerAvatar.gameObject))
            {
                Destroy(playerAvatar.gameObject);

                return false;
            }

            return true;
        }
    }
}