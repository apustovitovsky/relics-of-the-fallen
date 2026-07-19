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

        Vector3 m_LastPosition;
        float m_FallStartTime;
        bool m_IsInitialized;
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
            m_IsInitialized = false;
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
                m_ClientCharacter.ServerCharacter == null ||
                m_Animator == null)
            {
                return;
            }

            Vector3 position =
                m_ClientCharacter.ServerCharacter.transform.position;

            if (!m_IsInitialized)
            {
                m_LastPosition = position;
                m_IsInitialized = true;
            }

            float speed =
                (position - m_LastPosition).magnitude /
                Time.deltaTime;

            m_Animator.SetFloat(
                s_MoveSpeed,
                speed,
                m_SpeedDampTime,
                Time.deltaTime);

            bool isGrounded = m_ClientCharacter.IsGrounded;

            if (!isGrounded && m_WasGrounded)
            {
                m_FallStartTime = Time.time;
            }

            m_Animator.SetBool(s_IsGrounded, isGrounded);

            m_Animator.SetFloat(
                s_FallingDuration,
                isGrounded
                    ? 0f
                    : Time.time - m_FallStartTime);

            m_LastPosition = position;
            m_WasGrounded = isGrounded;
        }
    }
}