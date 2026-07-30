using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GAS
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class AnimInstance
        : MonoBehaviour
    {
        private PlayableGraph m_PlayableGraph;

        private AnimationLayerMixerPlayable m_LayerMixer;

        private AnimationClipPlayable m_MontagePlayable;

        [field: SerializeField]
        public Animator Animator
        {
            get;
            private set;
        }

        [field: NonSerialized]
        public GameplayAbilityMontage CurrentMontage
        {
            get;
            private set;
        }

        private void Reset()
        {
            Animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            InitializeGraph();
        }

        private void OnDisable()
        {
            if (m_PlayableGraph.IsValid())
            {
                m_PlayableGraph.Destroy();
            }

            m_MontagePlayable = default;
            CurrentMontage = null;
        }

        /// <summary>
        /// Starts a montage on the override layer and returns its remaining playback duration.
        /// </summary>
        public float MontagePlay(
            GameplayAbilityMontage animMontage,
            float playRate,
            float startTimeSeconds)
        {
            if (animMontage == null)
            {
                throw new ArgumentNullException(
                    nameof(animMontage));
            }

            AnimationClip animation = animMontage.Animation;

            if (animation == null)
            {
                throw new InvalidOperationException(
                    $"Montage '{animMontage.name}' has no animation.");
            }

            if (playRate <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playRate),
                    "Montage play rate must be greater than zero.");
            }

            if (startTimeSeconds < 0f ||
                startTimeSeconds > animation.length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startTimeSeconds),
                    "Montage start time must be within the animation duration.");
            }

            InitializeGraph();

            if (m_MontagePlayable.IsValid())
            {
                m_LayerMixer.DisconnectInput(
                    1);

                m_PlayableGraph.DestroyPlayable(
                    m_MontagePlayable);
            }

            m_MontagePlayable = AnimationClipPlayable.Create(
                m_PlayableGraph,
                animation);

            m_MontagePlayable.SetTime(
                startTimeSeconds);

            m_MontagePlayable.SetSpeed(
                playRate);

            m_PlayableGraph.Connect(
                m_MontagePlayable,
                0,
                m_LayerMixer,
                1);

            m_LayerMixer.SetInputWeight(
                1,
                1f);

            CurrentMontage =
                animMontage;

            float remainingDuration = animation.length - startTimeSeconds;

            return remainingDuration / playRate;
        }

        /// <summary>
        /// Sets the playback position of the requested current montage.
        /// </summary>
        public void MontageSetPosition(
            GameplayAbilityMontage animMontage,
            float position)
        {
            if (animMontage == null)
            {
                throw new ArgumentNullException(
                    nameof(animMontage));
            }

            if (CurrentMontage != animMontage ||
                !m_MontagePlayable.IsValid())
            {
                return;
            }

            AnimationClip animation = m_MontagePlayable.GetAnimationClip();
            float clampedPosition = Mathf.Clamp(
                position,
                0f,
                animation.length);

            m_MontagePlayable.SetTime(
                clampedPosition);
        }

        /// <summary>
        /// Sets the playback rate of the requested current montage.
        /// </summary>
        public void MontageSetPlayRate(
            GameplayAbilityMontage animMontage,
            float playRate)
        {
            if (animMontage == null)
            {
                throw new ArgumentNullException(
                    nameof(animMontage));
            }

            if (playRate <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playRate),
                    "Montage play rate must be greater than zero.");
            }

            if (CurrentMontage != animMontage ||
                !m_MontagePlayable.IsValid())
            {
                return;
            }

            m_MontagePlayable.SetSpeed(
                playRate);
        }

        /// <summary>
        /// Stops the requested current montage and restores the base animation layer.
        /// </summary>
        public void MontageStop(
            float blendOutTime,
            GameplayAbilityMontage animMontage = null)
        {
            if (blendOutTime < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(blendOutTime),
                    "Montage blend-out time cannot be negative.");
            }

            if (blendOutTime > 0f)
            {
                throw new NotSupportedException(
                    "Montage blend-out is not implemented.");
            }

            if (animMontage != null &&
                CurrentMontage != animMontage)
            {
                return;
            }

            if (!m_MontagePlayable.IsValid())
            {
                CurrentMontage = null;

                return;
            }

            m_LayerMixer.SetInputWeight(
                1,
                0f);

            m_LayerMixer.DisconnectInput(
                1);

            m_PlayableGraph.DestroyPlayable(
                m_MontagePlayable);

            m_MontagePlayable = default;
            CurrentMontage = null;
        }

        /// <summary>
        /// Returns the current montage position in seconds.
        /// </summary>
        public float MontageGetPosition()
        {
            if (!m_MontagePlayable.IsValid())
            {
                return 0f;
            }

            AnimationClip animation = m_MontagePlayable.GetAnimationClip();
            float position = (float)m_MontagePlayable.GetTime();

            return Mathf.Min(
                position,
                animation.length);
        }

        /// <summary>
        /// Returns the current montage playback rate.
        /// </summary>
        public float MontageGetPlayRate()
        {
            if (!m_MontagePlayable.IsValid())
            {
                return 0f;
            }

            return (float)m_MontagePlayable.GetSpeed();
        }

        /// <summary>
        /// Returns whether the current montage has stopped or reached its end.
        /// </summary>
        public bool MontageGetIsStopped()
        {
            if (!m_MontagePlayable.IsValid())
            {
                return true;
            }

            AnimationClip animation = m_MontagePlayable.GetAnimationClip();

            return m_MontagePlayable.GetTime() >=
                animation.length;
        }

        /// <summary>
        /// Creates the animation graph containing the base controller and montage layer.
        /// </summary>
        private void InitializeGraph()
        {
            if (m_PlayableGraph.IsValid())
            {
                return;
            }

            if (Animator == null)
            {
                Animator = GetComponent<Animator>();
            }

            m_PlayableGraph = PlayableGraph.Create(
                $"{name}.AnimInstance");

            m_PlayableGraph.SetTimeUpdateMode(
                DirectorUpdateMode.GameTime);

            m_LayerMixer = AnimationLayerMixerPlayable.Create(
                m_PlayableGraph,
                2);

            AnimationPlayableOutput output = AnimationPlayableOutput.Create(
                m_PlayableGraph,
                "Animation",
                Animator);

            output.SetSourcePlayable(
                m_LayerMixer);

            RuntimeAnimatorController controller = Animator.runtimeAnimatorController;

            if (controller != null)
            {
                AnimatorControllerPlayable controllerPlayable =
                    AnimatorControllerPlayable.Create(
                        m_PlayableGraph,
                        controller);

                m_PlayableGraph.Connect(
                    controllerPlayable,
                    0,
                    m_LayerMixer,
                    0);

                m_LayerMixer.SetInputWeight(
                    0,
                    1f);
            }

            m_LayerMixer.SetInputWeight(
                1,
                0f);

            m_PlayableGraph.Play();
        }
    }
}