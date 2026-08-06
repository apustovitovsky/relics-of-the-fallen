using UnityEngine;

namespace RelicsOfTheFallen.Character
{
    /// <summary>
    /// Exposes animated attachment points owned by one character presentation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterSockets :
        MonoBehaviour
    {
        [field: SerializeField]
        public Transform ProjectileOrigin
        {
            get;
            private set;
        }
    }
}