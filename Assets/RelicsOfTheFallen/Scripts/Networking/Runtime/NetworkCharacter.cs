using Mirror;
using UnityEngine;

namespace RelicsOfTheFallen.Networking
{
    [DisallowMultipleComponent]
    public sealed class NetworkCharacter :
        NetworkBehaviour
    {
        [Header("Authority Player")]
        [SerializeField]
        CharacterController m_CharacterController;

        [SerializeField]
        Behaviour m_Input;

        [SerializeField]
        Behaviour m_Movement;

        [SerializeField]
        Behaviour m_Look;

        [Header("Local Player")]
        [SerializeField]
        Behaviour m_Camera;

        void Awake()
        {
            SetAuthorityControl(false);
            SetCameraControl(false);
        }

        public override void OnStartAuthority()
        {
            SetAuthorityControl(true);
        }

        public override void OnStopAuthority()
        {
            SetAuthorityControl(false);
        }

        public override void OnStartLocalPlayer()
        {
            SetCameraControl(true);
        }

        public override void OnStopLocalPlayer()
        {
            SetCameraControl(false);
        }

        public override void OnStopClient()
        {
            SetAuthorityControl(false);
            SetCameraControl(false);
        }

        void SetAuthorityControl(bool enabled)
        {
            if (m_CharacterController != null)
            {
                m_CharacterController.enabled = enabled;
            }

            SetEnabled(m_Input, enabled);
            SetEnabled(m_Movement, enabled);
            SetEnabled(m_Look, enabled);
        }

        void SetCameraControl(bool enabled)
        {
            SetEnabled(m_Camera, enabled);
        }

        static void SetEnabled(
            Behaviour behaviour,
            bool enabled)
        {
            if (behaviour != null)
            {
                behaviour.enabled = enabled;
            }
        }
    }
}