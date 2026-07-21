using System;
using Mirror;
using RelicsOfTheFallen.Character;
using RelicsOfTheFallen.Character.Movement;
using RelicsOfTheFallen.UserInput;
using UnityEngine;

namespace RelicsOfTheFallen.Networking.Character
{
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class NetworkCharacter :
        NetworkBehaviour,
        ICharacterLocomotionStateProvider
    {
        [Header("References")]
        [SerializeField]
        LocalCharacterInput m_LocalCharacterInput;

        [SerializeField]
        CharacterLocomotionController
            m_LocomotionController;

        [SyncVar(
            hook = nameof(OnRemoteLocomotionStateChanged))]
        CharacterLocomotionState m_RemoteLocomotionState;

        public CharacterLocomotionState LocomotionState =>
            isOwned
                ? m_LocomotionController.LocomotionState
                : m_RemoteLocomotionState;

        public event Action<CharacterLocomotionState>
            LocomotionStateChanged;

        public override void OnStartClient()
        {
            if (!isOwned)
            {
                m_LocomotionController
                    .SetSimulationEnabled(false);
            }
        }

        public override void OnStartAuthority()
        {
            m_LocalCharacterInput.SetInputEnabled(true);

            m_LocomotionController
                .LocomotionStateChanged +=
                OnLocalLocomotionStateChanged;

            m_LocomotionController
                .SetSimulationEnabled(true);
        }

        public override void OnStopAuthority()
        {
            m_LocomotionController
                .LocomotionStateChanged -=
                OnLocalLocomotionStateChanged;

            m_LocalCharacterInput.SetInputEnabled(false);

            m_LocomotionController
                .SetSimulationEnabled(false);
        }

        void Update()
        {
            if (!isOwned ||
                !m_LocalCharacterInput.TryReadCommand(
                    out CharacterInputCommand inputCommand))
            {
                return;
            }

            m_LocomotionController.Simulate(
                inputCommand,
                Time.deltaTime);
        }

        void OnLocalLocomotionStateChanged(
            CharacterLocomotionState locomotionState)
        {
            LocomotionStateChanged?.Invoke(
                locomotionState);

            CmdReportLocomotionState(
                locomotionState);
        }

        [Command(channel = Channels.Unreliable)]
        void CmdReportLocomotionState(
            CharacterLocomotionState locomotionState)
        {
            if (!IsFinite(locomotionState))
            {
                return;
            }

            m_RemoteLocomotionState = locomotionState;
        }

        void OnRemoteLocomotionStateChanged(
            CharacterLocomotionState previousState,
            CharacterLocomotionState newState)
        {
            if (isOwned)
            {
                return;
            }

            LocomotionStateChanged?.Invoke(newState);
        }

        static bool IsFinite(
            in CharacterLocomotionState locomotionState)
        {
            return IsFinite(locomotionState.Velocity.x) &&
                   IsFinite(locomotionState.Velocity.y) &&
                   IsFinite(locomotionState.Velocity.z) &&
                   IsFinite(locomotionState.MoveInput.x) &&
                   IsFinite(locomotionState.MoveInput.y) &&
                   IsFinite(locomotionState.FacingYaw) &&
                   IsFinite(locomotionState.AimYaw) &&
                   IsFinite(locomotionState.AimPitch) &&
                   IsFinite(locomotionState.InclineAngle) &&
                   IsFinite(
                       locomotionState.CameraRotationOffset);
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }
    }
}