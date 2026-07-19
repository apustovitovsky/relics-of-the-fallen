using UnityEngine;

namespace Game.Actor
{
    public sealed class MovementAnimationEvents : MonoBehaviour
    {
        [SerializeField]
        private MovementController _movement;

        public void ActivateSliding()
        {
            if (_movement != null)
                _movement.ActivateSliding();
        }

        public void DeactivateSliding()
        {
            if (_movement != null)
                _movement.DeactivateSliding();
        }
    }
}