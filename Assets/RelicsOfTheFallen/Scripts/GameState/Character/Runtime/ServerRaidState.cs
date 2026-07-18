using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RelicsOfTheFallen.GameState.Character
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class ServerRaidState : NetworkBehaviour
    {
        [SerializeField]
        NetworkObject m_PlayerAvatarPrefab;

        [SerializeField]
        Transform[] m_PlayerSpawnPoints;

        readonly HashSet<ulong> m_SpawnedClientIds = new();

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }

            NetworkManager.SceneManager.OnLoadEventCompleted +=
                OnLoadEventCompleted;
            NetworkManager.SceneManager.OnSynchronizeComplete +=
                OnSynchronizeComplete;
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager != null)
            {
                NetworkManager.SceneManager.OnLoadEventCompleted -=
                    OnLoadEventCompleted;
                NetworkManager.SceneManager.OnSynchronizeComplete -=
                    OnSynchronizeComplete;
            }
        }

        void OnLoadEventCompleted(
            string sceneName,
            LoadSceneMode loadSceneMode,
            List<ulong> clientsCompleted,
            List<ulong> clientsTimedOut)
        {
            if (sceneName != gameObject.scene.name ||
                loadSceneMode != LoadSceneMode.Single)
            {
                return;
            }

            foreach (var clientId in clientsCompleted)
            {
                SpawnPlayerAvatar(clientId);
            }
        }

        void OnSynchronizeComplete(ulong clientId)
        {
            SpawnPlayerAvatar(clientId);
        }

        void SpawnPlayerAvatar(ulong clientId)
        {
            if (m_SpawnedClientIds.Contains(clientId) ||
                !NetworkManager.ConnectedClients.ContainsKey(clientId))
            {
                return;
            }

            if (m_PlayerAvatarPrefab == null ||
                m_PlayerSpawnPoints.Length == 0)
            {
                Debug.LogError(
                    $"{nameof(ServerRaidState)} requires a player avatar " +
                    "prefab and at least one spawn point.",
                    this);

                return;
            }

            var spawnPoint =
                m_PlayerSpawnPoints[
                    m_SpawnedClientIds.Count %
                    m_PlayerSpawnPoints.Length];

            var playerAvatar = Instantiate(
                m_PlayerAvatarPrefab,
                spawnPoint.position,
                spawnPoint.rotation);

            playerAvatar.SpawnWithOwnership(clientId, true);
            m_SpawnedClientIds.Add(clientId);
        }
    }
}