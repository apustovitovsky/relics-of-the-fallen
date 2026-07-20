using Game.Input;
using UnityEngine;

namespace Game.Actor
{
    public class ActorLookController : MonoBehaviour
    {
        [SerializeField]
        private LookConfigSO _config;

        [field: SerializeField]
        public Transform Pivot { get; private set; }

        [field: SerializeField]
        public Transform Origin { get; private set; }

        [field: SerializeField]
        public Transform TargetProxy { get; private set; }

        [SerializeField]
        private bool _invertVerticalLook;
        [SerializeField]
        private bool _hasTarget;
        [SerializeField]
        private float _sensitivity = 5f;
        [SerializeField]
        private Vector2 _pitchBounds = new(-10f, 45f);
        [SerializeField]
        private float _positionLag = 1f;
        [SerializeField]
        private float _rotationLag = 1f;

        private IControlInput _input;
        private Transform _target;
        private const int _LAG_DELTA_TIME_ADJUSTMENT = 20;
        private float _verticalInversion;
        private float _lastPitch;
        private float _lastYaw;
        private Vector3 _lastPosition;
        private float _pitch;
        private float _yaw;
        private Vector3 _newPosition;
        private float _pitchDelta;
        private float _yawDelta;

        public void Bind(IControlInput input)
        {
            _input = input;
        }

        public void Unbind()
        {
            _input = null;
        }

        private void Start()
        {
            _verticalInversion = _config != null && _config.InvertVerticalLook ? 1 : -1;

            Pivot.SetPositionAndRotation(Origin.position, Origin.rotation);
            _lastPosition = Pivot.position;
        }

        private void Update()
        {
            float positionLag =
                _config != null ? _config.PositionLag : _positionLag;

            float rotationLag =
                _config != null ? _config.RotationLag : _rotationLag;

            float positionalFollowSpeed = 1 / (positionLag / _LAG_DELTA_TIME_ADJUSTMENT);
            float rotationalFollowSpeed = 1 / (rotationLag / _LAG_DELTA_TIME_ADJUSTMENT);

            float sensitivity =
                _config != null ? _config.Sensitivity : _sensitivity;

            var lookDelta = _input != null ? _input.LookDelta : Vector2.zero;

            _pitchDelta = lookDelta.y * _verticalInversion * sensitivity;
            _yawDelta = lookDelta.x * sensitivity;

            _pitch += _pitchDelta;

            Vector2 pitchBounds =
                _config != null ? _config.PitchBounds : _pitchBounds;

            _pitch = Mathf.Clamp(_pitch, pitchBounds.x, pitchBounds.y);

            _pitch = Mathf.Lerp(_lastPitch, _pitch, rotationalFollowSpeed * Time.deltaTime);

            if (_hasTarget && _target != null)
            {
                Vector3 targetPosition = _target.position;

                if (TargetProxy != null)
                {
                    TargetProxy.position = targetPosition;
                    targetPosition = TargetProxy.position;
                }

                Vector3 aimVector = targetPosition - Origin.position;

                if (aimVector.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(aimVector);
                    targetRotation = Quaternion.Lerp(
                        Pivot.rotation,
                        targetRotation,
                        rotationalFollowSpeed * Time.deltaTime);

                    var euler = targetRotation.eulerAngles;

                    _pitch = NormalizePitch(euler.x);
                    _yaw = euler.y;
                }
            }
            else
            {
                _hasTarget = false;

                _yaw += _yawDelta;
                _yaw = Mathf.Lerp(
                    _lastYaw,
                    _yaw,
                    rotationalFollowSpeed * Time.deltaTime);
            }

            _newPosition = Origin.position;
            _newPosition = Vector3.Lerp(_lastPosition, _newPosition, positionalFollowSpeed * Time.deltaTime);

            Pivot.position = _newPosition;
            Pivot.eulerAngles = new Vector3(_pitch, _yaw, 0);

            _lastPosition = _newPosition;
            _lastPitch = _pitch;
            _lastYaw = _yaw;
        }

        public void SetTarget(Transform target)
        {
            _target = target;
            _hasTarget = _target != null;
        }

        public void ClearTarget()
        {
            _target = null;
            _hasTarget = false;
        }

        public void SetWorldDirection(Vector3 direction)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
                return;

            ClearTarget();

            Quaternion rotation = Quaternion.LookRotation(direction.normalized);
            _yaw = rotation.eulerAngles.y;
        }

        private static float NormalizePitch(float pitch)
        {
            return pitch > 180f
                ? pitch - 360f
                : pitch;
        }

        public Vector3 Position => Pivot.position;

        public Vector3 Forward => Pivot.forward;

        public Vector3 ForwardFlat => new(Forward.x, 0f, Forward.z);

        public Vector3 ForwardFlatNormalized => ForwardFlat.normalized;

        public Vector3 RightFlat
        {
            get
            {
                var right = Pivot.right;
                return new Vector3(right.x, 0, right.z);
            }
        }

        public Vector3 RightFlatNormalized => RightFlat.normalized;

        public float Pitch => Pivot.eulerAngles.x;
    }
}
