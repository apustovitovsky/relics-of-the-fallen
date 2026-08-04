using Mirror;
using UnityEngine;

namespace RelicsOfTheFallen.Networking
{
    [DisallowMultipleComponent]
    public sealed class NetworkCharacter : NetworkBehaviour
    {
        [Header("Authority Player")]
        [SerializeField]
        private CharacterController m_CharacterController;

        [SerializeField]
        private Behaviour m_Input;

        [SerializeField]
        private Behaviour m_Movement;

        [SerializeField]
        private Behaviour m_Targeting;

        [SerializeField]
        private Behaviour m_Look;

        [Header("Local Player")]
        [SerializeField]
        private Behaviour m_Camera;

        private void Awake()
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

        private void SetAuthorityControl(bool enabled)
        {
            if (m_CharacterController != null)
            {
                m_CharacterController.enabled = enabled;
            }

            SetEnabled(
                m_Input,
                enabled);

            SetEnabled(
                m_Movement,
                enabled);

            SetEnabled(
                m_Look,
                enabled);

            SetEnabled(
                m_Targeting,
                enabled);
        }

        private void SetCameraControl(bool enabled)
        {
            SetEnabled(m_Camera, enabled);
        }

        private static void SetEnabled(
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