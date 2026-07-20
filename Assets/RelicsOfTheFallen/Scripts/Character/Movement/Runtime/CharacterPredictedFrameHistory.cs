using RelicsOfTheFallen.Character;
using UnityEngine;

namespace RelicsOfTheFallen.Character.Movement
{
    /// <summary>
    /// Stores the locally predicted result for each input command.
    /// A server owner snapshot is compared with the frame that has the
    /// same acknowledged input sequence before a rollback is allowed.
    /// </summary>
    public readonly struct CharacterPredictedFrame
    {
        public readonly uint Sequence;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public readonly CharacterLocomotionSimulationState
            LocomotionState;

        public readonly bool IsGrounded;

        public CharacterPredictedFrame(
            uint sequence,
            Vector3 position,
            Quaternion rotation,
            CharacterLocomotionSimulationState locomotionState,
            bool isGrounded)
        {
            Sequence = sequence;
            Position = position;
            Rotation = rotation;
            LocomotionState = locomotionState;
            IsGrounded = isGrounded;
        }
    }

    /// <summary>
    /// Fixed-capacity sequence history for predicted locomotion frames.
    /// It is local-only and never serialized.
    /// </summary>
    public sealed class CharacterPredictedFrameHistory
    {
        private readonly CharacterPredictedFrame[] m_Frames;

        private int m_ReadIndex;
        private int m_Count;

        public int Count => m_Count;

        public CharacterPredictedFrameHistory(
            int capacity)
        {
            m_Frames =
                new CharacterPredictedFrame[capacity];
        }

        public bool TryRecord(
            in CharacterPredictedFrame frame)
        {
            if (m_Count == m_Frames.Length)
            {
                return false;
            }

            if (m_Count > 0)
            {
                int lastIndex =
                    (m_ReadIndex + m_Count - 1) %
                    m_Frames.Length;

                if (frame.Sequence <=
                    m_Frames[lastIndex].Sequence)
                {
                    return false;
                }
            }

            int writeIndex =
                (m_ReadIndex + m_Count) %
                m_Frames.Length;

            m_Frames[writeIndex] = frame;
            m_Count++;

            return true;
        }

        public bool TryGet(
            uint sequence,
            out CharacterPredictedFrame frame)
        {
            for (int index = 0;
                 index < m_Count;
                 index++)
            {
                int frameIndex =
                    (m_ReadIndex + index) %
                    m_Frames.Length;

                CharacterPredictedFrame candidate =
                    m_Frames[frameIndex];

                if (candidate.Sequence == sequence)
                {
                    frame = candidate;
                    return true;
                }
            }

            frame = default;
            return false;
        }

        public void DiscardThrough(
            uint sequence)
        {
            while (m_Count > 0 &&
                   m_Frames[m_ReadIndex].Sequence <=
                   sequence)
            {
                m_ReadIndex =
                    (m_ReadIndex + 1) %
                    m_Frames.Length;

                m_Count--;
            }
        }

        public void Clear()
        {
            m_ReadIndex = 0;
            m_Count = 0;
        }
    }
}