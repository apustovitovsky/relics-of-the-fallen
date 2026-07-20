using RelicsOfTheFallen.Character;
using UnityEngine;

namespace RelicsOfTheFallen.Character.Look
{
    /// <summary>
    /// Applies replicated gameplay aim to the currently selected
    /// character visual. It never controls the local camera,
    /// root Transform or network state.
    /// </summary>
    public sealed class CharacterAimPresenter : MonoBehaviour
    {
        private static readonly int s_HeadLookX =
            Animator.StringToHash("HeadLookX");

        private static readonly int s_HeadLookY =
            Animator.StringToHash("HeadLookY");

        private static readonly int s_BodyLookX =
            Animator.StringToHash("BodyLookX");

        private static readonly int s_BodyLookY =
            Animator.StringToHash("BodyLookY");

        [Header("References")]
        [SerializeField]
        private ClientCharacter m_ClientCharacter;

        [Header("Horizontal Aim")]
        [SerializeField]
        [Min(1f)]
        private float m_MaxYawOffset = 90f;

        [SerializeField]
        [Range(0f, 1f)]
        private float m_HeadYawMultiplier = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        private float m_BodyYawMultiplier = 0.6f;

        [Header("Vertical Aim")]
        [SerializeField]
        private float m_MinLookPitch = -0.1f;

        [SerializeField]
        private float m_MaxLookPitch = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        private float m_BodyPitchMultiplier = 1f;

        [Header("Smoothing")]
        [SerializeField]
        [Min(0f)]
        private float m_AimDampTime = 0.1f;

        private Animator m_Animator;

        private void Awake()
        {
            if (m_ClientCharacter == null)
            {
                m_ClientCharacter =
                    GetComponent<ClientCharacter>();
            }

            if (m_ClientCharacter == null)
            {
                m_ClientCharacter =
                    GetComponentInParent<ClientCharacter>();
            }
        }

        private void OnDisable()
        {
            ResetAnimatorParameters();
        }

        private void Update()
        {
            if (!TryResolveAnimator())
            {
                return;
            }

            CharacterLocomotionState locomotionState =
                m_ClientCharacter.LocomotionState;

            float aimYawOffset = Mathf.DeltaAngle(
                locomotionState.FacingYaw,
                locomotionState.AimYaw);

            float normalizedYaw = Mathf.Clamp(
                aimYawOffset / m_MaxYawOffset,
                -1f,
                1f);

            float normalizedPitch = Mathf.Clamp(
                -locomotionState.AimPitch / 180f,
                m_MinLookPitch,
                m_MaxLookPitch);

            m_Animator.SetFloat(
                s_HeadLookX,
                normalizedYaw * m_HeadYawMultiplier,
                m_AimDampTime,
                Time.deltaTime);

            m_Animator.SetFloat(
                s_BodyLookX,
                normalizedYaw * m_BodyYawMultiplier,
                m_AimDampTime,
                Time.deltaTime);

            m_Animator.SetFloat(
                s_HeadLookY,
                normalizedPitch,
                m_AimDampTime,
                Time.deltaTime);

            m_Animator.SetFloat(
                s_BodyLookY,
                normalizedPitch *
                m_BodyPitchMultiplier,
                m_AimDampTime,
                Time.deltaTime);
        }

        private bool TryResolveAnimator()
        {
            if (m_ClientCharacter == null)
            {
                return false;
            }

            if (m_Animator == null)
            {
                m_Animator = m_ClientCharacter.OurAnimator;
            }

            return m_Animator != null;
        }

        private void ResetAnimatorParameters()
        {
            if (m_Animator == null)
            {
                return;
            }

            m_Animator.SetFloat(s_HeadLookX, 0f);
            m_Animator.SetFloat(s_HeadLookY, 0f);
            m_Animator.SetFloat(s_BodyLookX, 0f);
            m_Animator.SetFloat(s_BodyLookY, 0f);
        }
    }
}