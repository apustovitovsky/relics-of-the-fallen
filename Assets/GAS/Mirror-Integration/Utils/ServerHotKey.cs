using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GAS
{
    [RequireComponent(typeof(NetworkManager))]
    public class ServerHotKey : MonoBehaviour
    {
        [SerializeField] InputActionAsset m_InputActions;

        NetworkManager m_NetworkManager;
        InputActionMap m_NetworkingActions;
        InputAction m_StartHostAction;
        InputAction m_StartClientAction;
        InputAction m_StartServerAction;
        InputAction m_StopClientAction;

        void Awake()
        {
            m_NetworkManager = GetComponent<NetworkManager>();

            if (m_InputActions == null)
            {
                Debug.LogError(
                    $"{nameof(ServerHotKey)} on '{name}' needs an Input Actions asset.",
                    this);

                enabled = false;
                return;
            }

            m_NetworkingActions = m_InputActions.FindActionMap("Networking", true);
            m_StartHostAction = m_NetworkingActions.FindAction("StartHost", true);
            m_StartClientAction = m_NetworkingActions.FindAction("StartClient", true);
            m_StartServerAction = m_NetworkingActions.FindAction("StartServer", true);
            m_StopClientAction = m_NetworkingActions.FindAction("StopClient", true);
        }

        void OnEnable()
        {
            m_NetworkingActions?.Enable();
        }

        void OnDisable()
        {
            m_NetworkingActions?.Disable();
        }

        void Update()
        {
            if (m_NetworkingActions == null)
            {
                return;
            }

            if (m_StartHostAction.WasPressedThisFrame())
            {
                m_NetworkManager.StartHost();
            }

            if (m_StartClientAction.WasPressedThisFrame())
            {
                m_NetworkManager.StartClient();
            }

            if (m_StartServerAction.WasPressedThisFrame())
            {
                m_NetworkManager.StartServer();
            }

            if (m_StopClientAction.WasPressedThisFrame())
            {
                m_NetworkManager.StopClient();
            }
        }
    }
}