using System;
using GAS;
using Mirror;
using UnityEngine;

namespace RelicsOfTheFallen.Networking
{
    [DisallowMultipleComponent]
    public sealed class NetworkAbilitySystemComponent : NetworkBehaviour
    {
        [SerializeField]
        AbilitySystemComponent m_AbilitySystem;

        [Header("Debug")]
        [SerializeField]
        bool m_LogAttributeSynchronization;

        public readonly SyncDictionary<string, float> Attributes = new();

        bool m_IsObservingServerAbilitySystem;

        public event Action<AttributeName, float> AttributeChanged;

        void Awake()
        {
            if (m_AbilitySystem == null)
            {
                Debug.LogError(
                    $"{nameof(NetworkAbilitySystemComponent)} requires an Ability System reference.",
                    this);

                enabled = false;
            }
        }

        public override void OnStartServer()
        {
            StartObservingServerAbilitySystem();
        }

        public override void OnStopServer()
        {
            StopObservingServerAbilitySystem();
        }

        public override void OnStartClient()
        {
            Attributes.OnChange += OnAttributesChanged;

            if (isClientOnly)
            {
                foreach (var attribute in Attributes)
                {
                    ApplyReplicatedAttribute(
                        attribute.Key,
                        attribute.Value);
                }

                Log(
                    $"Client received {Attributes.Count} replicated attributes.");
            }
        }

        public override void OnStopClient()
        {
            Attributes.OnChange -= OnAttributesChanged;
        }

        public bool TryGetAttributeValue(
            AttributeName attributeName,
            out float value)
        {
            if (attributeName == null)
            {
                value = default;
                return false;
            }

            return Attributes.TryGetValue(
                attributeName.name,
                out value);
        }

        void StartObservingServerAbilitySystem()
        {
            if (m_IsObservingServerAbilitySystem ||
                m_AbilitySystem == null)
            {
                return;
            }

            m_AbilitySystem.OnAttributeChanged +=
                OnServerAttributeChanged;

            m_IsObservingServerAbilitySystem = true;

            foreach (var attribute in m_AbilitySystem.attributes)
            {
                Attributes[attribute.attributeName.name] =
                    attribute.GetValue();

                Log(
                    $"Server initial attribute: " +
                    $"{attribute.attributeName.name} = {attribute.GetValue()}");
            }
        }

        void StopObservingServerAbilitySystem()
        {
            if (!m_IsObservingServerAbilitySystem ||
                m_AbilitySystem == null)
            {
                return;
            }

            m_AbilitySystem.OnAttributeChanged -=
                OnServerAttributeChanged;

            m_IsObservingServerAbilitySystem = false;
        }

        void OnServerAttributeChanged(
            AttributeName attributeName,
            float previousValue,
            float currentValue,
            GameplayEffect gameplayEffect)
        {
            Attributes[attributeName.name] = currentValue;

            Log(
                $"Server GAS event: {attributeName.name} " +
                $"{previousValue} → {currentValue}");
        }

        void OnAttributesChanged(
            SyncDictionary<string, float>.Operation operation,
            string attributeName,
            float value)
        {
            if (!isClientOnly ||
                operation != SyncDictionary<string, float>.Operation.OP_ADD &&
                operation != SyncDictionary<string, float>.Operation.OP_SET)
            {
                return;
            }

            ApplyReplicatedAttribute(
                attributeName,
                value);
        }

        void ApplyReplicatedAttribute(
            string attributeName,
            float value)
        {
            if (!m_AbilitySystem.attributesDictionary.TryGetValue(
                    attributeName,
                    out var attribute))
            {
                Debug.LogError(
                    $"Client Ability System has no attribute named {attributeName}.",
                    this);

                return;
            }

            if (attribute.attributeName.attributeType ==
                AttributeType.RESOURCE)
            {
                attribute.baseValue = value;
            }
            else
            {
                attribute.currentValue = value;
            }

            AttributeChanged?.Invoke(
                attribute.attributeName,
                value);

            Log(
                $"Client applied replicated attribute: " +
                $"{attributeName} = {value}");
        }

        void Log(string message)
        {
            if (m_LogAttributeSynchronization)
            {
                Debug.Log(
                    $"[{nameof(NetworkAbilitySystemComponent)}] {message}",
                    this);
            }
        }
    }
}