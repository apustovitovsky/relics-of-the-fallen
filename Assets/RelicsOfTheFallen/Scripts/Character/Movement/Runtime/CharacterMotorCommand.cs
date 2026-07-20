using RelicsOfTheFallen.Character;
using UnityEngine;

namespace RelicsOfTheFallen.Character.Movement
{
    /// <summary>
    /// Describes an authoritative locomotion intent in world space.
    /// It does not depend on Input System, RPC or a camera.
    /// </summary>
    public readonly struct CharacterMotorCommand
    {
        public readonly Vector3 MovementDirection;
        public readonly float FacingYaw;

        public readonly bool WantsSprint;
        public readonly bool WantsStrafe;

        public readonly bool ToggleWalk;
        public readonly bool ToggleCrouch;
        public readonly bool JumpPressed;

        public CharacterMotorCommand(
            Vector3 movementDirection,
            float facingYaw,
            bool wantsSprint,
            bool wantsStrafe,
            bool toggleWalk,
            bool toggleCrouch,
            bool jumpPressed)
        {
            MovementDirection = movementDirection;
            FacingYaw = facingYaw;
            WantsSprint = wantsSprint;
            WantsStrafe = wantsStrafe;
            ToggleWalk = toggleWalk;
            ToggleCrouch = toggleCrouch;
            JumpPressed = jumpPressed;
        }
    }

    /// <summary>
    /// Translates player-specific network input into a shared motor command.
    /// AI will later produce CharacterMotorCommand directly.
    /// </summary>
    public static class PlayerCharacterMotorCommandFactory
    {
        public static CharacterMotorCommand Create(
            in CharacterInputCommand inputCommand,
            bool alwaysStrafe)
        {
            Quaternion facingRotation = Quaternion.Euler(
                0f,
                inputCommand.LookYaw,
                0f);

            Vector3 movementDirection =
                facingRotation * Vector3.forward *
                inputCommand.Move.y +
                facingRotation * Vector3.right *
                inputCommand.Move.x;

            movementDirection = Vector3.ClampMagnitude(
                movementDirection,
                1f);

            return new CharacterMotorCommand(
                movementDirection,
                inputCommand.LookYaw,
                inputCommand.IsHeld(
                    CharacterInputHeldButtons.Sprint),
                alwaysStrafe ||
                inputCommand.IsHeld(
                    CharacterInputHeldButtons.Aim),
                inputCommand.IsPressed(
                    CharacterInputPressedButtons.WalkToggle),
                inputCommand.IsPressed(
                    CharacterInputPressedButtons.CrouchToggle),
                inputCommand.IsPressed(
                    CharacterInputPressedButtons.Jump));
        }
    }
}