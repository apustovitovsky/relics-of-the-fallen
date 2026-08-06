namespace GAS
{
    public interface IAbilitySystemReplicationTransport
    {
        /// <summary>
        /// Sends a predicted gameplay ability activation request to authoritative execution.
        /// </summary>
        void CallServerTryActivateAbility(
            GameplayAbilitySpecHandle abilityToActivate,
            bool inputPressed,
            PredictionKey predictionKey);

        /// <summary>
        /// Sends a generic replicated ability event to authoritative execution.
        /// </summary>
        void ServerSetReplicatedEvent(
            GameplayAbilityGenericReplicatedEvent eventType,
            GameplayAbilitySpecHandle abilityHandle,
            PredictionKey abilityOriginalPredictionKey,
            PredictionKey currentPredictionKey);

        /// <summary>
        /// Sends confirmed target data to authoritative ability execution.
        /// </summary>
        void CallServerSetReplicatedTargetData(
            GameplayAbilitySpecHandle abilityHandle,
            PredictionKey abilityOriginalPredictionKey,
            GameplayAbilityTargetDataHandle replicatedTargetDataHandle,
            GameplayTag applicationTag,
            PredictionKey currentPredictionKey);

        /// <summary>
        /// Sends target-data cancellation to authoritative ability execution.
        /// </summary>
        void ServerSetReplicatedTargetDataCancelled(
            GameplayAbilitySpecHandle abilityHandle,
            PredictionKey abilityOriginalPredictionKey,
            PredictionKey currentPredictionKey);

        /// <summary>
        /// Replicates a normal or cancelled gameplay ability ending to the remote execution side.
        /// </summary>
        void ReplicateEndOrCancelAbility(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActivationInfo activationInfo,
            bool wasCancelled);
    }
}