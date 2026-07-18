using RelicsOfTheFallen.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RelicsOfTheFallen.UserInput
{
    [RequireComponent(typeof(ServerCharacter))]
    public class ClientInputSender : NetworkBehaviour
    {
        const float k_MoveSendRateSeconds = 0.04f;

        [SerializeField] ServerCharacter m_ServerCharacter;
        [SerializeField] Transform m_CameraPivot;
        [SerializeField] InputActionReference m_MoveAction;

        float m_LastSentMove;
        Vector3 m_LastSentMovementInput;

        public override void OnNetworkSpawn()
        {
            if (!IsClient || !IsOwner)
            {
                enabled = false;
                return;
            }

            m_MoveAction.action.actionMap.Enable();
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                m_MoveAction.action.actionMap.Disable();
            }

            enabled = false;
        }

        void Update()
        {
            Vector2 moveInput =
                Vector2.ClampMagnitude(
                    m_MoveAction.action.ReadValue<Vector2>(),
                    1f);

            Vector3 forward = m_CameraPivot.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 right = m_CameraPivot.right;
            right.y = 0f;
            right.Normalize();

            Vector3 movementInput =
                Vector3.ClampMagnitude(
                    forward * moveInput.y + right * moveInput.x,
                    1f);

            bool inputChanged =
                movementInput != m_LastSentMovementInput;

            bool shouldSend =
                inputChanged || movementInput != Vector3.zero;

            if (!shouldSend ||
                Time.time - m_LastSentMove < k_MoveSendRateSeconds)
            {
                return;
            }

            m_LastSentMove = Time.time;
            m_LastSentMovementInput = movementInput;

            m_ServerCharacter.ServerSendCharacterInputRpc(movementInput);
        }
    }
}