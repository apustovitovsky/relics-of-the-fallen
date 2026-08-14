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
        private readonly DisposableEvent<GameplayAbilityMontage>
            m_MontageBlendedInDelegate = new();

        private readonly DisposableEvent<
            GameplayAbilityMontage,
            bool> m_MontageBlendingOutDelegate = new();

        private readonly DisposableEvent<
            GameplayAbilityMontage,
            bool> m_MontageEndedDelegate = new();
        private bool m_IsBlendedInPending;
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

        private void Update()
        {
            if (!m_MontagePlayable.IsValid())
            {
                return;
            }

            if (m_IsBlendedInPending)
            {
                m_IsBlendedInPending = false;

                m_MontageBlendedInDelegate.Invoke(
                    CurrentMontage);
            }

            if (m_MontagePlayable.IsValid() &&
                MontageGetIsStopped())
            {
                FinishCurrentMontage(
                    false);
            }
        }

        private void OnDisable()
        {
            if (m_MontagePlayable.IsValid())
            {
                FinishCurrentMontage(
                    true);
            }

            if (m_PlayableGraph.IsValid())
            {
                m_PlayableGraph.Destroy();
            }

            m_MontagePlayable = default;
            CurrentMontage = null;
            m_IsBlendedInPending = false;
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
                FinishCurrentMontage(
                    true);
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

            CurrentMontage = animMontage;
            m_IsBlendedInPending = true;

            float remainingDuration =
                animation.length - startTimeSeconds;

            return remainingDuration / playRate;
        }

        /// <summary>
        /// Registers a callback for successful blending into the selected montage.
        /// </summary>
        public IDisposable MontageSetBlendedInDelegate(
            Action<GameplayAbilityMontage> handler,
            GameplayAbilityMontage montage)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(
                    nameof(handler));
            }

            if (montage == null)
            {
                throw new ArgumentNullException(
                    nameof(montage));
            }

            return m_MontageBlendedInDelegate.Subscribe(
                HandleMontageBlendedIn);

            void HandleMontageBlendedIn(
                GameplayAbilityMontage blendedInMontage)
            {
                if (blendedInMontage == montage)
                {
                    handler(
                        blendedInMontage);
                }
            }
        }

        /// <summary>
        /// Registers a callback for blending out of the selected montage.
        /// </summary>
        public IDisposable MontageSetBlendingOutDelegate(
            Action<GameplayAbilityMontage, bool> handler,
            GameplayAbilityMontage montage)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(
                    nameof(handler));
            }

            if (montage == null)
            {
                throw new ArgumentNullException(
                    nameof(montage));
            }

            return m_MontageBlendingOutDelegate.Subscribe(
                HandleMontageBlendingOut);

            void HandleMontageBlendingOut(
                GameplayAbilityMontage blendingOutMontage,
                bool wasInterrupted)
            {
                if (blendingOutMontage == montage)
                {
                    handler(
                        blendingOutMontage,
                        wasInterrupted);
                }
            }
        }

        /// <summary>
        /// Registers a callback for termination of the selected montage.
        /// </summary>
        public IDisposable MontageSetEndDelegate(
            Action<GameplayAbilityMontage, bool> handler,
            GameplayAbilityMontage montage)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(
                    nameof(handler));
            }

            if (montage == null)
            {
                throw new ArgumentNullException(
                    nameof(montage));
            }

            return m_MontageEndedDelegate.Subscribe(
                HandleMontageEnded);

            void HandleMontageEnded(
                GameplayAbilityMontage endedMontage,
                bool wasInterrupted)
            {
                if (endedMontage == montage)
                {
                    handler(
                        endedMontage,
                        wasInterrupted);
                }
            }
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
                m_IsBlendedInPending = false;

                return;
            }

            FinishCurrentMontage(
                true);
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

            if (!animation.isLooping)
            {
                return Mathf.Min(
                    position,
                    animation.length);
            }

            return Mathf.Repeat(
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

            return
                !animation.isLooping &&
                m_MontagePlayable.GetTime() >=
                animation.length;
        }

        /// <summary>
        /// Terminates the current montage and broadcasts its lifecycle delegates.
        /// </summary>
        private void FinishCurrentMontage(
            bool wasInterrupted)
        {
            GameplayAbilityMontage finishedMontage =
                CurrentMontage;

            m_IsBlendedInPending = false;

            if (finishedMontage != null)
            {
                m_MontageBlendingOutDelegate.Invoke(
                    finishedMontage,
                    wasInterrupted);
            }

            if (m_MontagePlayable.IsValid())
            {
                m_LayerMixer.SetInputWeight(
                    1,
                    0f);

                m_LayerMixer.DisconnectInput(
                    1);

                m_PlayableGraph.DestroyPlayable(
                    m_MontagePlayable);
            }

            m_MontagePlayable = default;
            CurrentMontage = null;

            if (finishedMontage != null)
            {
                m_MontageEndedDelegate.Invoke(
                    finishedMontage,
                    wasInterrupted);
            }
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

            AnimationPlayableOutput output =
                AnimationPlayableOutput.Create(
                    m_PlayableGraph,
                    "Animation",
                    Animator);

            output.SetSourcePlayable(
                m_LayerMixer);

            RuntimeAnimatorController controller =
                Animator.runtimeAnimatorController;

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