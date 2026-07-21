using UnityEngine;

namespace RelicsOfTheFallen.Character
{
    public interface ICharacterLookSource
    {
        float Yaw { get; }

        float Pitch { get; }

        Vector3 ForwardOnGround { get; }

        Vector3 RightOnGround { get; }
    }
}