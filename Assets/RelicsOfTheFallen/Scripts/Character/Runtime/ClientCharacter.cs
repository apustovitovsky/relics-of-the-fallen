using Unity.Netcode;
using UnityEngine;

namespace RelicsOfTheFallen.Character
{
    /// <summary>
    /// Responsible for displaying a character on the client's screen
    /// based on state information sent by the server.
    /// </summary>
    public class ClientCharacter : NetworkBehaviour
    {
        [SerializeField]
        Animator m_ClientVisualsAnimator;

        public Animator OurAnimator => m_ClientVisualsAnimator;

        ServerCharacter m_ServerCharacter;

        public ServerCharacter ServerCharacter => m_ServerCharacter;

        void Awake()
        {
            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsClient || transform.parent == null)
            {
                return;
            }

            enabled = true;

            m_ServerCharacter =
                GetComponentInParent<ServerCharacter>();

            name = "AvatarGraphics" +
                m_ServerCharacter.OwnerClientId;
        }

        public override void OnNetworkDespawn()
        {
            enabled = false;
        }
    }
}