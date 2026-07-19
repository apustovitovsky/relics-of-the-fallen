using RelicsOfTheFallen.Character;
using Unity.Netcode;
using UnityEngine;

namespace RelicsOfTheFallen.Character.Movement
{
    [RequireComponent(typeof(CharacterController))]
    public class ServerCharacterMovement : NetworkBehaviour
    {
        [SerializeField] ServerCharacter m_CharLogic;
        [SerializeField] CharacterController m_CharacterController;
        [SerializeField] float m_MoveSpeed = 5f;
        [SerializeField] float m_GravityMultiplier = 2f;

        float m_VerticalVelocity;

        void Awake()
        {
            enabled = false;
            m_CharacterController.enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                return;
            }

            m_CharacterController.enabled = true;
            enabled = true;
        }

        public override void OnNetworkDespawn()
        {
            m_VerticalVelocity = 0f;
            m_CharacterController.enabled = false;
            enabled = false;
        }

        void Update()
        {
            Vector3 movementInput = m_CharLogic.MovementInput;

            if (m_CharacterController.isGrounded &&
                m_VerticalVelocity < 0f)
            {
                m_VerticalVelocity = -2f;
            }

            m_VerticalVelocity +=
                Physics.gravity.y *
                m_GravityMultiplier *
                Time.deltaTime;

            Vector3 velocity = movementInput * m_MoveSpeed;
            velocity.y = m_VerticalVelocity;

            m_CharacterController.Move(
                velocity * Time.deltaTime);

            m_CharLogic.IsGrounded.Value =
                m_CharacterController.isGrounded;

            if (movementInput != Vector3.zero)
            {
                transform.rotation =
                    Quaternion.LookRotation(movementInput);
            }
        }
    }
}