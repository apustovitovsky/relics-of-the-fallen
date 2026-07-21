using Unity.Cinemachine;
using UnityEngine;

namespace RelicsOfTheFallen.Character.Cinemachine
{
    [DisallowMultipleComponent]
    public sealed class LocalPlayerCameraController :
        MonoBehaviour
    {
        const string k_CinemachineCameraTag = "CMCamera";

        [SerializeField]
        Transform m_CameraPivot;

        CinemachineCamera m_CinemachineCamera;

        void Start()
        {
            if (m_CameraPivot == null)
            {
                Debug.LogError(
                    $"{nameof(LocalPlayerCameraController)} on '{name}' " +
                    "requires a camera pivot.",
                    this);

                enabled = false;
                return;
            }

            var cameraGameObject =
                GameObject.FindGameObjectWithTag(
                    k_CinemachineCameraTag);

            if (cameraGameObject == null ||
                !cameraGameObject.TryGetComponent(
                    out m_CinemachineCamera))
            {
                Debug.LogError(
                    "The scene requires a Cinemachine Camera tagged " +
                    $"'{k_CinemachineCameraTag}'.",
                    this);

                enabled = false;
                return;
            }

            if (!cameraGameObject.TryGetComponent<
                    CinemachineThirdPersonFollow>(
                    out _))
            {
                Debug.LogError(
                    "The Cinemachine Camera requires " +
                    $"{nameof(CinemachineThirdPersonFollow)}.",
                    cameraGameObject);

                enabled = false;
                return;
            }

            m_CinemachineCamera.Follow = m_CameraPivot;
            m_CinemachineCamera.LookAt = null;
        }

        void OnDisable()
        {
            if (m_CinemachineCamera != null &&
                m_CinemachineCamera.Follow == m_CameraPivot)
            {
                m_CinemachineCamera.Follow = null;
            }
        }
    }
}