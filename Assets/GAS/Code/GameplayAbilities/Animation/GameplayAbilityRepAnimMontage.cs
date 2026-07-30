namespace GAS
{
    public struct GameplayAbilityRepAnimMontage
    {
        public GameplayAbilityMontage Animation
        {
            get;
            internal set;
        }

        public byte PlayInstanceId
        {
            get;
            internal set;
        }

        public float PlayRate
        {
            get;
            internal set;
        }

        public float Position
        {
            get;
            internal set;
        }

        public float BlendTime
        {
            get;
            internal set;
        }

        public bool IsStopped
        {
            get;
            internal set;
        }

        public PredictionKey PredictionKey
        {
            get;
            internal set;
        }

        /// <summary>
        /// Creates the replicated state of one gameplay ability montage playback.
        /// </summary>
        public GameplayAbilityRepAnimMontage(
            GameplayAbilityMontage animation,
            byte playInstanceId,
            float playRate,
            float position,
            float blendTime,
            bool isStopped,
            PredictionKey predictionKey)
        {
            Animation = animation;
            PlayInstanceId = playInstanceId;
            PlayRate = playRate;
            Position = position;
            BlendTime = blendTime;
            IsStopped = isStopped;
            PredictionKey = predictionKey;
        }
    }
}