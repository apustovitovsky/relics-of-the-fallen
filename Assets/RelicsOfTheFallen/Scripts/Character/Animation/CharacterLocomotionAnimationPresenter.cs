using UnityEngine;

namespace RelicsOfTheFallen.Character
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class CharacterLocomotionAnimationPresenter :
            MonoBehaviour
    {
        private static readonly int k_SpeedParameter =
            Animator.StringToHash("Speed");

        [field: Header("References")]
        [field: SerializeField]
        private Animator Animator
        {
            get; set;
        }

        [field: Header("Locomotion")]
        [field: SerializeField, Min(0.01f)]
        private float RunSpeed { get; set; } = 5f;

        [field: SerializeField, Min(0f)]
        private float DampTime { get; set; } = 0.1f;

        [field: SerializeField, Min(1f)]
        private float MaximumNormalizedSpeed { get; set; } = 2f;

        private Vector3 m_PreviousPosition;

        private void Awake()
        {
            if (Animator == null)
            {
                Animator = GetComponent<Animator>();
            }

            if (Animator == null)
            {
                Debug.LogError(
                    $"{nameof(CharacterLocomotionAnimationPresenter)} on " +
                    $"'{name}' requires an Animator.", this);

                enabled = false;
            }
        }

        private void OnEnable()
        {
            m_PreviousPosition = transform.position;
        }

        private void LateUpdate()
        {
            UpdateLocomotion(
                Time.deltaTime);
        }

        /// <summary>
        /// Updates the locomotion blend parameter from planar world-space movement.
        /// </summary>
        private void UpdateLocomotion(float deltaTime)
        {
            Vector3 currentPosition = transform.position;
            Vector3 displacement = currentPosition - m_PreviousPosition;

            m_PreviousPosition = currentPosition;

            if (deltaTime <= 0f)
            {
                return;
            }

            displacement.y = 0f;

            float speed = displacement.magnitude / deltaTime;
            float normalizedSpeed = Mathf.Clamp(
                speed / RunSpeed,
                0f,
                MaximumNormalizedSpeed);

            Animator.SetFloat(
                k_SpeedParameter,
                normalizedSpeed,
                DampTime,
                deltaTime);
        }
    }
}