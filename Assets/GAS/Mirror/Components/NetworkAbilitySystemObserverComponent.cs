using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

namespace GAS.Mirror
{
    // / <summary> An additional component to be attached to the asc's GameObject to enable networking replication and prediction. </summary>
    public class NetworkAbilitySystemObserverComponent : NetworkBehaviour
    {

        [SerializeField]
        private AssetRegistry m_AssetRegistry;

        [SyncVar(
            hook = nameof(OnReplicatedAnimMontageChanged))]
        private GameplayAbilityRepAnimMontageReplicationState m_ReplicatedAnimMontage;

        public AbilitySystemComponent asc;
        public static AbilitySystemComponent localPlayerAsc;

        [SerializeReference] public readonly SyncDictionary<string, float> syncAttributes = new();
        public GenericDictionary<string, Queue<float>> localAttributesBuffer = new(); // Buffers the sequence of changes to attributes. If the sequence is different from sequence of changes received from server the. Clear it and reset to server value.

        /// <summary>
        /// Resolves and applies authoritative montage state on a simulated client proxy.
        /// </summary>
        private void OnReplicatedAnimMontageChanged(
            GameplayAbilityRepAnimMontageReplicationState _,
            GameplayAbilityRepAnimMontageReplicationState newValue)
        {
            if (isServer ||
                isOwned ||
                !newValue.IsValid)
            {
                return;
            }

            GameplayAbilityMontage montage =
                m_AssetRegistry.GetAsset<GameplayAbilityMontage>(
                    newValue.AnimationId);

            GameplayAbilityRepAnimMontage repAnimMontageInfo =
                new(
                    montage,
                    newValue.PlayInstanceId,
                    newValue.PlayRate,
                    newValue.Position,
                    newValue.BlendTime,
                    newValue.IsStopped,
                    newValue.PredictionKey);

            asc.OnRepReplicatedAnimMontage(
                repAnimMontageInfo);
        }

        [ServerCallback]
        private void LateUpdate()
        {
            SynchronizeReplicatedAnimMontage();
        }

        /// <summary>
        /// Copies the authoritative core montage state into its Mirror transport state.
        /// </summary>
        private void SynchronizeReplicatedAnimMontage()
        {
            asc.AnimMontageUpdateReplicatedData();

            GameplayAbilityRepAnimMontage repAnimMontageInfo =
                asc.RepAnimMontageInfo;

            GameplayAbilityMontage montage =
                repAnimMontageInfo.Animation;

            if (montage == null)
            {
                return;
            }

            AssetId montageId =
                m_AssetRegistry.GetAssetId(
                    montage);

            m_ReplicatedAnimMontage =
                new GameplayAbilityRepAnimMontageReplicationState(
                    montageId,
                    repAnimMontageInfo.PlayInstanceId,
                    repAnimMontageInfo.PlayRate,
                    repAnimMontageInfo.Position,
                    repAnimMontageInfo.BlendTime,
                    repAnimMontageInfo.IsStopped,
                    repAnimMontageInfo.PredictionKey);
        }

        /// <summary>
        /// Initializes observer replication without granting abilities to simulated proxies.
        /// </summary>
        private void Start()
        {
            name = name + " " + (isLocalPlayer ? "[LocalPlayer]" : "[Server]") + " netId=" + netId;

            if (asc == null)
            {
                throw new ArgumentException(
                    "AbilitySystemComponent must not be null.",
                    nameof(asc));
            }

            if (isLocalPlayer)
            {
                localPlayerAsc = asc;
            }

            if (!isClientOnly)
            {
                asc.attributes.ForEach(
                    attribute =>
                        syncAttributes.TryAdd(
                            attribute.attributeName.name,
                            attribute.GetValue()));

                asc.OnAttributeChanged +=
                    (
                        attributeName,
                        oldValue,
                        newValue,
                        gameplayEffect) =>
                    {
                        syncAttributes[attributeName.name] = newValue;
                    };

                return;
            }

            if (!isOwned)
            {
                asc.ClearAllAbilities();
            }

            asc.attributes.ForEach(
                attribute =>
                    localAttributesBuffer.TryAdd(
                        attribute.attributeName.name,
                        new Queue<float>()));

            asc.OnAttributeChanged +=
                AddAttributeToPredictionBuffer;

            syncAttributes.OnChange +=
                SynchronizeAttributes;

            foreach (
                KeyValuePair<string, float> attributeEntry
                in syncAttributes)
            {
                SynchronizeAttributes(
                    SyncDictionary<string, float>.Operation.OP_ADD,
                    attributeEntry.Key,
                    attributeEntry.Value);
            }
        }

        private void SynchronizeAttributes(
            SyncDictionary<string, float>.Operation operation,
            string attributeName,
            float callbackValue)
        {
            if (!isClientOnly)
            {
                return;
            }

            StartCoroutine(
                SynchronizeAttributesCoroutine(
                    operation,
                    attributeName));
        }

        /// <summary>
        /// Reconciles a replicated attribute base value after the prediction frame completes.
        /// </summary>
        private IEnumerator SynchronizeAttributesCoroutine(
            SyncDictionary<string, float>.Operation operation,
            string attributeName)
        {
            yield return new WaitForEndOfFrame();

            if (
                operation ==
                SyncDictionary<string, float>.Operation.OP_REMOVE ||
                operation ==
                SyncDictionary<string, float>.Operation.OP_CLEAR)
            {
                yield break;
            }

            if (
                !syncAttributes.TryGetValue(
                    attributeName,
                    out float authoritativeValue))
            {
                Debug.LogWarning(
                    $"Cannot synchronize attribute '{attributeName}': " +
                    "it is absent from the replicated attribute dictionary.",
                    this);

                yield break;
            }

            if (
                !asc.AttributesDictionary.TryGetValue(
                    attributeName,
                    out GAS.Attribute attribute))
            {
                Debug.LogWarning(
                    $"Cannot synchronize attribute '{attributeName}': " +
                    "it is absent from the client ASC.",
                    this);

                yield break;
            }

            if (
                localAttributesBuffer.TryGetValue(
                    attributeName,
                    out Queue<float> predictionBuffer) &&
                predictionBuffer.Count > 0 &&
                predictionBuffer.Contains(
                    authoritativeValue))
            {
                while (predictionBuffer.Count > 0)
                {
                    float predictedValue =
                        predictionBuffer.Dequeue();

                    if (
                        Mathf.Approximately(
                            predictedValue,
                            authoritativeValue))
                    {
                        break;
                    }
                }

                yield break;
            }

            predictionBuffer?.Clear();

            asc.SetNumericAttributeBase(
                attribute.attributeName,
                authoritativeValue);
        }

        // ATTRIBUTE PREDICTION BUFFER
        private void AddAttributeToPredictionBuffer(AttributeName attName, float oldValue, float newValue, GameplayEffect ge)
        {// Buffers the sequence of changes to attributes. If the sequence is different from sequence of changes receivedfrom server. Clear it and reset to server value.
            if (ge == null || ge.source == null)
            {
                return; // !!! important. SynchronizeAttributes shouldn't trigger an addition to prediction buffer. (It comes from the server, not a client prediction). When invoking AttributeChange from there, we send a null ge.
            }

            if (ge != null && ge.source != null && ge.source != localPlayerAsc)
            {
                return; // Do not predict if not activated by local player. In other words, predict only when local player activated it.
            }

            localAttributesBuffer[attName.name].Enqueue(newValue);
        }
    }
}

