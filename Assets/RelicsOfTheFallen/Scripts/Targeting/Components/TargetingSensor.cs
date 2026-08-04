using System.Collections.Generic;
using UnityEngine;

namespace RelicsOfTheFallen.Targeting
{
    [DisallowMultipleComponent]
    public sealed class TargetingSensor :
        MonoBehaviour
    {
        private const int k_MaxColliderCount = 64;

        [field: SerializeField, Min(0f)]
        private float Radius { get; set; } = 15f;

        [field: SerializeField]
        private LayerMask TargetLayers { get; set; } = ~0;

        [field: SerializeField]
        private TargetingComponent Self
        {
            get; set;
        }

        private readonly Collider[] m_ColliderBuffer =
            new Collider[k_MaxColliderCount];

        private readonly HashSet<ITargetable> m_Candidates = new();

        public IReadOnlyCollection<ITargetable> Candidates =>
            m_Candidates;

        private void Awake()
        {
            if (Self == null)
            {
                Self = GetComponentInParent<TargetingComponent>();
            }
        }

        private void FixedUpdate()
        {
            RefreshCandidates();
        }

        private void OnDisable()
        {
            m_Candidates.Clear();
        }

        /// <summary>
        /// Rebuilds the nearby target candidate collection without managed allocations.
        /// </summary>
        public void RefreshCandidates()
        {
            m_Candidates.Clear();

            int colliderCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                Radius,
                m_ColliderBuffer,
                TargetLayers,
                QueryTriggerInteraction.Collide);

            for (int index = 0;
                index < colliderCount;
                index++)
            {
                Collider candidateCollider =
                    m_ColliderBuffer[index];

                TargetingComponent endpoint =
                    candidateCollider.GetComponentInParent<
                        TargetingComponent>();

                if (endpoint == null ||
                    endpoint == Self ||
                    !endpoint.IsTargetable)
                {
                    continue;
                }

                m_Candidates.Add(endpoint);
            }
        }
    }
}