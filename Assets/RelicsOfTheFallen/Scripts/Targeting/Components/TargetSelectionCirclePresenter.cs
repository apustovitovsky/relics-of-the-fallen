using UnityEngine;

namespace RelicsOfTheFallen.Targeting
{
    [DisallowMultipleComponent]
    public sealed class TargetSelectionCirclePresenter :
        MonoBehaviour
    {
        [field: SerializeField]
        private TargetingController Targeting
        {
            get; set;
        }

        [field: SerializeField]
        private Transform Circle
        {
            get; set;
        }

        [field: SerializeField, Min(0f)]
        private float GroundOffset { get; set; } = 0.03f;

        private void Awake()
        {
            if (Targeting == null ||
                Circle == null)
            {
                Debug.LogError(
                    $"{nameof(TargetSelectionCirclePresenter)} on '{name}' " +
                    "requires targeting and circle references.",
                    this);

                enabled = false;
                return;
            }

            SetCircleVisible(false);
        }

        private void LateUpdate()
        {
            ITargetable currentTarget = Targeting.CurrentTarget;

            if (!Targeting.isActiveAndEnabled ||
                currentTarget == null)
            {
                SetCircleVisible(false);
                return;
            }

            Vector3 position =
                currentTarget.TargetActor.transform.position +
                Vector3.up * GroundOffset;

            Circle.SetPositionAndRotation(
                position,
                Quaternion.Euler(
                    90f,
                    0f,
                    0f));

            SetCircleVisible(true);
        }

        private void OnDisable()
        {
            if (Circle != null)
            {
                SetCircleVisible(false);
            }
        }

        /// <summary>
        /// Changes the visibility of the current target selection circle.
        /// </summary>
        private void SetCircleVisible(bool isVisible)
        {
            if (Circle.gameObject.activeSelf == isVisible)
            {
                return;
            }

            Circle.gameObject.SetActive(isVisible);
        }
    }
}