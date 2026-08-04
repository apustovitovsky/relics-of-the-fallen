namespace GAS
{
    public interface IAbilitySystemReplicationTransport
    {
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
    }
}