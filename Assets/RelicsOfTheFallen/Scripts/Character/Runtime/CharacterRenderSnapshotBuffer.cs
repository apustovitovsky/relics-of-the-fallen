namespace RelicsOfTheFallen.Character
{
    /// <summary>
    /// Local-only chronological buffer for observer rendering.
    /// It is not network-serialized and never affects simulation.
    /// </summary>
    public sealed class CharacterRenderSnapshotBuffer
    {
        readonly CharacterRenderSnapshot[] m_Snapshots;

        int m_ReadIndex;
        int m_Count;

        public CharacterRenderSnapshotBuffer(int capacity)
        {
            m_Snapshots =
                new CharacterRenderSnapshot[capacity];
        }

        public void Add(CharacterRenderSnapshot snapshot)
        {
            uint newTick =
                snapshot.LocomotionState.ServerTick;

            if (m_Count > 0)
            {
                CharacterRenderSnapshot latest =
                    GetAt(m_Count - 1);

                if (newTick <=
                    latest.LocomotionState.ServerTick)
                {
                    return;
                }
            }

            if (m_Count == m_Snapshots.Length)
            {
                m_ReadIndex =
                    (m_ReadIndex + 1) %
                    m_Snapshots.Length;

                m_Count--;
            }

            int writeIndex =
                (m_ReadIndex + m_Count) %
                m_Snapshots.Length;

            m_Snapshots[writeIndex] = snapshot;
            m_Count++;
        }

        public bool TrySample(
            uint renderTick,
            out CharacterRenderSnapshot previous,
            out CharacterRenderSnapshot next,
            out float interpolation)
        {
            if (m_Count == 0)
            {
                previous = default;
                next = default;
                interpolation = 0f;
                return false;
            }

            CharacterRenderSnapshot oldest = GetAt(0);

            if (renderTick <=
                oldest.LocomotionState.ServerTick)
            {
                previous = oldest;
                next = oldest;
                interpolation = 0f;
                return true;
            }

            CharacterRenderSnapshot latest =
                GetAt(m_Count - 1);

            if (renderTick >=
                latest.LocomotionState.ServerTick)
            {
                previous = latest;
                next = latest;
                interpolation = 1f;
                return true;
            }

            for (int index = 0;
                 index < m_Count - 1;
                 index++)
            {
                CharacterRenderSnapshot candidate =
                    GetAt(index);

                CharacterRenderSnapshot following =
                    GetAt(index + 1);

                uint candidateTick =
                    candidate.LocomotionState.ServerTick;

                uint followingTick =
                    following.LocomotionState.ServerTick;

                if (renderTick < candidateTick ||
                    renderTick > followingTick)
                {
                    continue;
                }

                previous = candidate;
                next = following;

                interpolation =
                    followingTick == candidateTick
                        ? 1f
                        : (float)(renderTick - candidateTick) /
                          (followingTick - candidateTick);

                return true;
            }

            previous = latest;
            next = latest;
            interpolation = 1f;
            return true;
        }

        public void Clear()
        {
            m_ReadIndex = 0;
            m_Count = 0;
        }

        CharacterRenderSnapshot GetAt(int index)
        {
            int bufferIndex =
                (m_ReadIndex + index) %
                m_Snapshots.Length;

            return m_Snapshots[bufferIndex];
        }
    }
}