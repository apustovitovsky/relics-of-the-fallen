using RelicsOfTheFallen.Character;
using UnityEngine;

namespace RelicsOfTheFallen.Character.Look
{
    /// <summary>
    /// Reads locomotion state and applies aim values to the Animator.
    /// It never controls the local camera, root Transform, or network state.
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
        MonoBehaviour m_LocomotionStateProviderComponent;

        [SerializeField]
        Animator m_Animator;

        [Header("Horizontal Aim")]
        [SerializeField]
        [Min(1f)]
        float m_MaxYawOffset = 90f;

        [SerializeField]
        [Range(0f, 1f)]
        float m_HeadYawMultiplier = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        float m_BodyYawMultiplier = 0.6f;

        [Header("Vertical Aim")]
        [SerializeField]
        float m_MinLookPitch = -0.1f;

        [SerializeField]
        float m_MaxLookPitch = 1f;

        [SerializeField]
        [Range(0f, 1f)]
        float m_BodyPitchMultiplier = 1f;

        [Header("Smoothing")]
        [SerializeField]
        [Min(0f)]
        float m_AimDampTime = 0.1f;

        ICharacterLocomotionStateProvider
            m_LocomotionStateProvider;

        void Awake()
        {
            TryResolveDependencies();
        }

        void OnDisable()
        {
            ResetAnimatorParameters();
        }

        void Update()
        {
            if (!TryResolveDependencies())
            {
                return;
            }

            CharacterLocomotionState locomotionState =
                m_LocomotionStateProvider.LocomotionState;

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

        bool TryResolveDependencies()
        {
            if (m_LocomotionStateProvider == null)
            {
                if (m_LocomotionStateProviderComponent == null)
                {
                    foreach (MonoBehaviour component in
                             GetComponentsInParent<
                                 MonoBehaviour>(true))
                    {
                        if (component is
                            ICharacterLocomotionStateProvider)
                        {
                            m_LocomotionStateProviderComponent =
                                component;
                            break;
                        }
                    }
                }

                m_LocomotionStateProvider =
                    m_LocomotionStateProviderComponent as
                    ICharacterLocomotionStateProvider;
            }

            if (m_Animator == null)
            {
                m_Animator = GetComponent<Animator>();
            }

            return m_LocomotionStateProvider != null &&
                   m_Animator != null;
        }

        void ResetAnimatorParameters()
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