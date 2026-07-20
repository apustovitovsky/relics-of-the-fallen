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
        [SerializeField] float m_RunSpeed = 5f;
        [SerializeField] float m_SprintSpeed = 7f;
        [SerializeField] float m_GravityMultiplier = 2f;

        float m_VerticalVelocity;
        uint m_ServerTick;

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
            m_ServerTick = 0;
            m_CharacterController.enabled = false;
            enabled = false;
        }

        void Update()
        {
            CharacterInputCommand inputCommand =
                m_CharLogic.InputCommand;

            Vector3 movementInput =
                GetMovementDirection(inputCommand);

            bool isMoving = movementInput != Vector3.zero;
            bool isSprinting =
                isMoving &&
                inputCommand.IsPressed(
                    CharacterInputButtons.SprintHeld);

            if (m_CharacterController.isGrounded &&
                m_VerticalVelocity < 0f)
            {
                m_VerticalVelocity = -2f;
            }

            m_VerticalVelocity +=
                Physics.gravity.y *
                m_GravityMultiplier *
                Time.deltaTime;

            float moveSpeed =
                isSprinting ? m_SprintSpeed : m_RunSpeed;

            Vector3 velocity = movementInput * moveSpeed;
            velocity.y = m_VerticalVelocity;

            m_CharacterController.Move(
                velocity * Time.deltaTime);

            if (isMoving)
            {
                transform.rotation =
                    Quaternion.LookRotation(movementInput);
            }

            bool isGrounded =
                m_CharacterController.isGrounded;

            m_CharLogic.LocomotionState.Value =
                new CharacterLocomotionState
                {
                    ServerTick = ++m_ServerTick,
                    LastProcessedInputTick = inputCommand.Tick,
                    Velocity = velocity,
                    MoveInput = inputCommand.Move,
                    FacingYaw = transform.eulerAngles.y,
                    AimYaw = inputCommand.LookYaw,
                    AimPitch = inputCommand.LookPitch,
                    Gait = GetGait(isMoving, isSprinting),
                    IsGrounded = isGrounded
                };
        }

        static Vector3 GetMovementDirection(
            CharacterInputCommand inputCommand)
        {
            Quaternion lookRotation =
                Quaternion.Euler(
                    0f,
                    inputCommand.LookYaw,
                    0f);

            Vector3 movementInput =
                lookRotation * Vector3.forward *
                inputCommand.Move.y;

            movementInput +=
                lookRotation * Vector3.right *
                inputCommand.Move.x;

            return Vector3.ClampMagnitude(movementInput, 1f);
        }

        static CharacterGait GetGait(
            bool isMoving,
            bool isSprinting)
        {
            if (!isMoving)
            {
                return CharacterGait.Idle;
            }

            return isSprinting
                ? CharacterGait.Sprint
                : CharacterGait.Run;
        }
    }
}