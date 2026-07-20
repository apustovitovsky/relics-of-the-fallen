using UnityEngine;

namespace Game.Actor
{
    [CreateAssetMenu(
        fileName = "LookConfig",
        menuName = "Game/Actor/Look Config")]
    public sealed class LookConfigSO : ScriptableObject
    {
        [field: SerializeField] public bool InvertVerticalLook { get; private set; }

        [field: SerializeField] public float Sensitivity { get; private set; } = 2f;

        [field: SerializeField]
        public Vector2 PitchBounds { get; private set; } =
            new(-70f, 70f);

        [field: SerializeField] public float PositionLag { get; private set; } = 0.2f;

        [field: SerializeField] public float RotationLag { get; private set; } = 0.2f;
    }
}