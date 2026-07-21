using Mirror;

namespace RelicsOfTheFallen.Character
{
    public static class CharacterLocomotionStateSerialization
    {
        public static void WriteCharacterLocomotionState(
            this NetworkWriter writer,
            CharacterLocomotionState locomotionState)
        {
            writer.WriteUShort(
                locomotionState.LocomotionStartSequence);

            writer.WriteVector3(locomotionState.Velocity);
            writer.WriteVector2(locomotionState.MoveInput);

            writer.WriteFloat(locomotionState.FacingYaw);
            writer.WriteFloat(locomotionState.AimYaw);
            writer.WriteFloat(locomotionState.AimPitch);

            writer.WriteFloat(
                locomotionState.InclineAngle);

            writer.WriteFloat(
                locomotionState.CameraRotationOffset);

            writer.WriteByte((byte)locomotionState.Gait);

            writer.WriteBool(
                locomotionState.IsGrounded);

            writer.WriteBool(
                locomotionState.IsStrafing);

            writer.WriteBool(
                locomotionState.IsTurningInPlace);

            writer.WriteBool(
                locomotionState.IsJumping);

            writer.WriteBool(
                locomotionState.IsCrouching);
        }

        public static CharacterLocomotionState
            ReadCharacterLocomotionState(
                this NetworkReader reader)
        {
            return new CharacterLocomotionState
            {
                LocomotionStartSequence =
                    reader.ReadUShort(),
                Velocity = reader.ReadVector3(),
                MoveInput = reader.ReadVector2(),
                FacingYaw = reader.ReadFloat(),
                AimYaw = reader.ReadFloat(),
                AimPitch = reader.ReadFloat(),
                InclineAngle = reader.ReadFloat(),
                CameraRotationOffset =
                    reader.ReadFloat(),
                Gait = (CharacterGait)reader.ReadByte(),
                IsGrounded = reader.ReadBool(),
                IsStrafing = reader.ReadBool(),
                IsTurningInPlace = reader.ReadBool(),
                IsJumping = reader.ReadBool(),
                IsCrouching = reader.ReadBool()
            };
        }
    }
}