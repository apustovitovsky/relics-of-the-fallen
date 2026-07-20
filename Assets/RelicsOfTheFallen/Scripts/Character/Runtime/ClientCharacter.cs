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
        CharacterLocomotionState m_LocomotionState;

        public ServerCharacter ServerCharacter => m_ServerCharacter;

        public CharacterLocomotionState LocomotionState =>
            m_LocomotionState;

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

            m_ServerCharacter.LocomotionState.OnValueChanged +=
                OnLocomotionStateChanged;

            m_LocomotionState =
                m_ServerCharacter.LocomotionState.Value;

            name = "AvatarGraphics" +
                m_ServerCharacter.OwnerClientId;
        }

        public override void OnNetworkDespawn()
        {
            if (m_ServerCharacter != null)
            {
                m_ServerCharacter.LocomotionState.OnValueChanged -=
                    OnLocomotionStateChanged;
            }

            enabled = false;
        }

        void OnLocomotionStateChanged(
            CharacterLocomotionState previousValue,
            CharacterLocomotionState newValue)
        {
            m_LocomotionState = newValue;
        }
    }
}