using UnityEngine;

namespace RelicsOfTheFallen.Targeting
{
    [DisallowMultipleComponent]
    public sealed class TargetingController :
        MonoBehaviour,
        ITargetProvider
    {
        [field: Header("References")]
        [field: SerializeField]
        private TargetingSensor Sensor
        {
            get; set;
        }

        [field: SerializeField]
        private Transform Origin
        {
            get; set;
        }

        [field: Header("Selection")]
        [field: SerializeField, Range(0f, 180f)]
        private float MaximumTargetAngle { get; set; } = 60f;

        [field: SerializeField, Min(0f)]
        private float DistanceScoreWeight { get; set; } = 10f;

        [field: SerializeField, Min(0f)]
        private float AngleScoreWeight { get; set; } = 100f;

        private TargetSelector m_Selector;

        public ITargetable CurrentTarget
        {
            get; private set;
        }

        private void Awake()
        {
            if (Sensor == null ||
                Origin == null)
            {
                Debug.LogError(
                    $"{nameof(TargetingController)} on '{name}' requires " +
                    "a targeting sensor and origin.",
                    this);

                enabled = false;
                return;
            }

            m_Selector = new TargetSelector(
                new ITargetFilter[]
                {
            new TargetableFilter(),
            new ViewAngleTargetFilter(
                MaximumTargetAngle)
                },
                new ITargetScorer[]
                {
            new DistanceTargetScorer(
                DistanceScoreWeight),
            new AngleTargetScorer(
                AngleScoreWeight)
                });

            Sensor.enabled = enabled;
        }

        private void OnEnable()
        {
            if (Sensor != null)
            {
                Sensor.enabled = true;
            }
        }

        private void OnDisable()
        {
            CurrentTarget = null;

            if (Sensor != null)
            {
                Sensor.enabled = false;
            }
        }

        private void Update()
        {
            CurrentTarget = m_Selector.SelectBest(
                Sensor.Candidates,
                Origin.position,
                Origin.forward);
        }
    }
}