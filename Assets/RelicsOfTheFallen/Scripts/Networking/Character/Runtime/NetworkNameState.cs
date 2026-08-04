using Mirror;
using RelicsOfTheFallen.Targeting;
using UnityEngine;

namespace RelicsOfTheFallen.Networking
{
    [DisallowMultipleComponent]
    public sealed class NetworkNameState :
        NetworkBehaviour
    {
        [field: SerializeField]
        private TargetingComponent Targeting
        {
            get; set;
        }

        [SyncVar(
            hook = nameof(
                OnDisplayNameChanged))]
        private string m_DisplayName = string.Empty;

        public string DisplayName => m_DisplayName;

        private void Awake()
        {
            if (Targeting == null)
            {
                Debug.LogError(
                    $"{nameof(NetworkNameState)} on '{name}' requires " +
                    "a targeting component.",
                    this);

                enabled = false;
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            SetDisplayName(
                $"Player {connectionToClient.connectionId + 1}");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            ApplyDisplayName(m_DisplayName);
        }

#if UNITY_EDITOR

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            CmdSetDisplayName(
                MultiplayerPlayModePlayerName.Get());
        }

#endif

        /// <summary>
        /// Changes the authoritative display name replicated to every client.
        /// </summary>
        [Server]
        public void SetDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(
                    displayName))
            {
                Debug.LogError(
                    "A replicated player display name cannot be empty.",
                    this);

                return;
            }

            m_DisplayName = displayName;

            ApplyDisplayName(displayName);
        }

#if UNITY_EDITOR

        /// <summary>
        /// Sends the Multiplayer Play Mode player name to the server.
        /// </summary>
        [Command]
        private void CmdSetDisplayName(string displayName)
        {
            SetDisplayName(displayName);
        }

#endif

        private void OnDisplayNameChanged(
            string oldDisplayName,
            string newDisplayName)
        {
            ApplyDisplayName(newDisplayName);
        }

        private void ApplyDisplayName(string displayName)
        {
            if (Targeting != null)
            {
                Targeting.SetDisplayName(
                    displayName);
            }
        }
    }
}