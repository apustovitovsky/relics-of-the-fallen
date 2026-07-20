namespace RelicsOfTheFallen.Character
{
    /// <summary>
    /// Stores input commands sent by the owning client until the server
    /// confirms that it has simulated their sequence.
    /// </summary>
    public sealed class CharacterInputHistory
    {
        private readonly CharacterInputCommand[] m_Commands;

        private int m_ReadIndex;
        private int m_Count;

        public int Count => m_Count;

        public CharacterInputHistory(int capacity)
        {
            m_Commands = new CharacterInputCommand[capacity];
        }

        public bool TryRecord(
            in CharacterInputCommand command)
        {
            if (m_Count == m_Commands.Length)
            {
                return false;
            }

            if (m_Count > 0)
            {
                int lastIndex =
                    (m_ReadIndex + m_Count - 1) %
                    m_Commands.Length;

                if (command.Sequence <=
                    m_Commands[lastIndex].Sequence)
                {
                    return false;
                }
            }

            int writeIndex =
                (m_ReadIndex + m_Count) %
                m_Commands.Length;

            m_Commands[writeIndex] = command;
            m_Count++;

            return true;
        }

        public void DiscardThrough(uint sequence)
        {
            while (m_Count > 0 &&
                   m_Commands[m_ReadIndex].Sequence <=
                   sequence)
            {
                m_ReadIndex =
                    (m_ReadIndex + 1) %
                    m_Commands.Length;

                m_Count--;
            }
        }

        public bool TryGet(
            int index,
            out CharacterInputCommand command)
        {
            if (index < 0 || index >= m_Count)
            {
                command = default;
                return false;
            }

            int commandIndex =
                (m_ReadIndex + index) %
                m_Commands.Length;

            command = m_Commands[commandIndex];
            return true;
        }

        public void Clear()
        {
            m_ReadIndex = 0;
            m_Count = 0;
        }
    }
}