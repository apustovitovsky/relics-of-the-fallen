using RelicsOfTheFallen.Character;
using UnityEngine;

namespace RelicsOfTheFallen.Character.Movement
{
    public sealed class MovementController : MonoBehaviour
    {
        static readonly int s_MoveSpeed =
            Animator.StringToHash("MoveSpeed");

        static readonly int s_IsGrounded =
            Animator.StringToHash("IsGrounded");

        static readonly int s_FallingDuration =
            Animator.StringToHash("FallingDuration");

        [SerializeField] ClientCharacter m_ClientCharacter;
        [SerializeField] Animator m_Animator;
        [SerializeField] float m_SpeedDampTime = 0.1f;

        float m_FallStartTime;
        bool m_WasGrounded;

        void Awake()
        {
            if (m_ClientCharacter == null)
            {
                m_ClientCharacter =
                    GetComponentInParent<ClientCharacter>();
            }

            if (m_Animator == null)
            {
                m_Animator = GetComponent<Animator>();
            }
        }

        void OnEnable()
        {
            m_WasGrounded = true;
            m_FallStartTime = Time.time;
        }

        void OnDisable()
        {
            if (m_Animator == null)
            {
                return;
            }

            m_Animator.SetFloat(s_MoveSpeed, 0f);
            m_Animator.SetBool(s_IsGrounded, true);
            m_Animator.SetFloat(s_FallingDuration, 0f);
        }

        void Update()
        {
            if (m_ClientCharacter == null ||
                m_Animator == null)
            {
                return;
            }

            CharacterLocomotionState locomotionState =
                m_ClientCharacter.LocomotionState;

            Vector3 horizontalVelocity =
                locomotionState.Velocity;

            horizontalVelocity.y = 0f;

            m_Animator.SetFloat(
                s_MoveSpeed,
                horizontalVelocity.magnitude,
                m_SpeedDampTime,
                Time.deltaTime);

            if (!locomotionState.IsGrounded &&
                m_WasGrounded)
            {
                m_FallStartTime = Time.time;
            }

            m_Animator.SetBool(
                s_IsGrounded,
                locomotionState.IsGrounded);

            m_Animator.SetFloat(
                s_FallingDuration,
                locomotionState.IsGrounded
                    ? 0f
                    : Time.time - m_FallStartTime);

            m_WasGrounded =
                locomotionState.IsGrounded;
        }
    }
}