namespace RelicsOfTheFallen.Character
{
    /// <summary>
    /// Stores received player input commands in sequence order until
    /// they are consumed by the authoritative server simulation.
    /// </summary>
    public sealed class CharacterInputBuffer
    {
        private readonly CharacterInputCommand[] m_Commands;

        private int m_ReadIndex;
        private int m_WriteIndex;
        private int m_Count;

        private uint m_LastAcceptedSequence;
        private bool m_HasAcceptedSequence;

        public int Count => m_Count;

        public CharacterInputBuffer(int capacity)
        {
            m_Commands = new CharacterInputCommand[capacity];
        }

        public bool TryEnqueue(
            in CharacterInputCommand command)
        {
            if (m_HasAcceptedSequence &&
                command.Sequence <= m_LastAcceptedSequence)
            {
                return false;
            }

            if (m_Count == m_Commands.Length)
            {
                return false;
            }

            m_Commands[m_WriteIndex] = command;

            m_WriteIndex =
                (m_WriteIndex + 1) % m_Commands.Length;

            m_Count++;
            m_LastAcceptedSequence = command.Sequence;
            m_HasAcceptedSequence = true;

            return true;
        }

        public bool TryDequeue(
            out CharacterInputCommand command)
        {
            if (m_Count == 0)
            {
                command = default;
                return false;
            }

            command = m_Commands[m_ReadIndex];

            m_ReadIndex =
                (m_ReadIndex + 1) % m_Commands.Length;

            m_Count--;

            return true;
        }

        public void Clear()
        {
            m_ReadIndex = 0;
            m_WriteIndex = 0;
            m_Count = 0;

            m_LastAcceptedSequence = 0;
            m_HasAcceptedSequence = false;
        }
    }
}