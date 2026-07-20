namespace RelicsOfTheFallen.Character
{
    /// <summary>
    /// Stores the latest continuous player intent and the accumulated
    /// one-shot input events. Continuous input never waits behind old
    /// packets. Pressed events are consumed exactly once on the next
    /// server simulation tick.
    /// </summary>
    public sealed class CharacterInputInbox
    {
        private CharacterInputCommand m_LatestCommand;
        private CharacterInputPressedButtons
            m_PendingPressedButtons;

        private bool m_HasLatestCommand;

        private uint m_LastAcceptedSequence;
        private bool m_HasAcceptedSequence;

        public bool HasInput => m_HasLatestCommand;

        public bool TryPush(
            in CharacterInputCommand command)
        {
            if (m_HasAcceptedSequence &&
                command.Sequence <= m_LastAcceptedSequence)
            {
                return false;
            }

            m_LatestCommand = command;
            m_LatestCommand.PressedButtons =
                CharacterInputPressedButtons.None;

            m_PendingPressedButtons |=
                command.PressedButtons;

            m_HasLatestCommand = true;
            m_LastAcceptedSequence = command.Sequence;
            m_HasAcceptedSequence = true;

            return true;
        }

        public bool TryConsume(
            out CharacterInputCommand command)
        {
            if (!m_HasLatestCommand)
            {
                command = default;
                return false;
            }

            command = m_LatestCommand;
            command.PressedButtons =
                m_PendingPressedButtons;

            m_PendingPressedButtons =
                CharacterInputPressedButtons.None;

            return true;
        }

        /// <summary>
        /// Clears movement and unconsumed events after input timeout,
        /// but retains the last accepted sequence so delayed old packets
        /// cannot reactivate a timed-out character.
        /// </summary>
        public void ClearPendingInput()
        {
            m_LatestCommand = default;
            m_PendingPressedButtons =
                CharacterInputPressedButtons.None;

            m_HasLatestCommand = false;
        }

        public void Clear()
        {
            ClearPendingInput();

            m_LastAcceptedSequence = 0;
            m_HasAcceptedSequence = false;
        }
    }
}