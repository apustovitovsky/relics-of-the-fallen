using Mirror;
using Unity.Cinemachine;
using UnityEngine;

namespace RelicsOfTheFallen.Character.Cinemachine
{
    public sealed class LocalPlayerCameraController :
        NetworkBehaviour
    {
        const string k_CinemachineCameraTag = "CMCamera";

        [SerializeField]
        Transform m_CameraPivot;

        public override void OnStartAuthority()
        {
            if (m_CameraPivot == null)
            {
                Debug.LogError(
                    $"{nameof(LocalPlayerCameraController)} requires " +
                    "a camera pivot.",
                    this);

                return;
            }

            var cameraGameObject =
                GameObject.FindGameObjectWithTag(
                    k_CinemachineCameraTag);

            if (cameraGameObject == null)
            {
                Debug.LogError(
                    "The Raid scene requires a Cinemachine Camera tagged " +
                    $"'{k_CinemachineCameraTag}'.",
                    this);

                return;
            }

            if (!cameraGameObject.TryGetComponent(
                    out CinemachineCamera cinemachineCamera))
            {
                Debug.LogError(
                    "The tagged camera object requires a Cinemachine Camera.",
                    cameraGameObject);

                return;
            }

            if (!cameraGameObject.TryGetComponent<
                    CinemachineThirdPersonFollow>(
                    out _))
            {
                Debug.LogError(
                    "The Cinemachine Camera requires Third Person Follow.",
                    cameraGameObject);

                return;
            }

            cinemachineCamera.Follow = m_CameraPivot;
            cinemachineCamera.LookAt = null;
        }
    }
}