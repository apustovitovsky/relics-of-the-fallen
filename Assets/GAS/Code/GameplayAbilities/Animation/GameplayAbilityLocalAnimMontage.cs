namespace GAS
{
    public struct GameplayAbilityLocalAnimMontage
    {
        public GameplayAbility AnimatingAbility
        {
            get;
            internal set;
        }

        public GameplayAbilityMontage AnimMontage
        {
            get;
            internal set;
        }

        public byte PlayInstanceId
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
        /// Creates the local state of one gameplay ability montage playback.
        /// </summary>
        internal GameplayAbilityLocalAnimMontage(
            GameplayAbility animatingAbility,
            GameplayAbilityMontage animMontage,
            byte playInstanceId,
            PredictionKey predictionKey)
        {
            AnimatingAbility = animatingAbility;
            AnimMontage = animMontage;
            PlayInstanceId = playInstanceId;
            PredictionKey = predictionKey;
        }
    }
}