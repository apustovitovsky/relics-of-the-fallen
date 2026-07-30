namespace GAS
{
    /// <summary>
    /// Describes the network prediction state of one gameplay ability activation.
    /// </summary>
    public enum GameplayAbilityActivationMode
    {
        Authority,
        NonAuthority,
        Predicting,
        Confirmed,
        Rejected
    }

    /// <summary>
    /// Stores the prediction identity and current mode of one gameplay ability activation.
    /// </summary>
    public struct GameplayAbilityActivationInfo
    {
        private PredictionKey m_PredictionKey;

        public GameplayAbilityActivationMode ActivationMode
        {
            get; internal set;
        }

        /// <summary>
        /// Creates activation state with an optional owner-scoped prediction key.
        /// </summary>
        public GameplayAbilityActivationInfo(
            GameplayAbilityActivationMode activationMode,
            PredictionKey predictionKey = default)
        {
            ActivationMode =
                activationMode;

            m_PredictionKey =
                predictionKey;
        }

        /// <summary>
        /// Returns the prediction key associated with this activation.
        /// </summary>
        public readonly PredictionKey GetActivationPredictionKey()
        {
            return m_PredictionKey;
        }

        /// <summary>
        /// Marks a predicted ability activation as confirmed by the authoritative server.
        /// </summary>
        public void SetActivationConfirmed()
        {
            ActivationMode =
                GameplayAbilityActivationMode.Confirmed;
        }

        /// <summary>
        /// Marks a predicted ability activation as rejected by the authoritative server.
        /// </summary>
        public void SetActivationRejected()
        {
            ActivationMode =
                GameplayAbilityActivationMode.Rejected;
        }

        /// <summary>
        /// Replaces the prediction key while preserving the current activation mode.
        /// </summary>
        internal void SetActivationPredictionKey(
            PredictionKey predictionKey)
        {
            m_PredictionKey =
                predictionKey;
        }
    }
}