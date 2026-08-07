using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GAS
{
    public class GameplayCueManager
    {
        private sealed class ActiveGameplayCue
        {
            public int Count
            {
                get; set;
            }

            public GameObject Instance
            {
                get; set;
            }
        }

        private readonly struct GameplayCueNotifyKey :
      IEquatable<GameplayCueNotifyKey>
        {
            private readonly EntityId m_TargetId;

            private readonly EntityId m_NotifyId;

            private readonly EntityId m_InstigatorId;

            private readonly EntityId m_SourceObjectId;

            public GameplayCueNotifyKey(
                GameObject target,
                GameplayCueNotify notify,
                GameplayCueParameters parameters)
            {
                m_TargetId =
                    target.GetEntityId();

                m_NotifyId =
                    notify.GetEntityId();

                GameObject instigator =
                    notify.UniqueInstancePerInstigator
                        ? parameters.GetInstigator()
                        : null;

                Object sourceObject =
                    notify.UniqueInstancePerSourceObject
                        ? parameters.GetSourceObject()
                        : null;

                m_InstigatorId =
                    instigator != null
                        ? instigator.GetEntityId()
                        : EntityId.None;

                m_SourceObjectId =
                    sourceObject != null
                        ? sourceObject.GetEntityId()
                        : EntityId.None;
            }

            public bool Equals(
                GameplayCueNotifyKey other)
            {
                return
                    m_TargetId == other.m_TargetId &&
                    m_NotifyId == other.m_NotifyId &&
                    m_InstigatorId == other.m_InstigatorId &&
                    m_SourceObjectId == other.m_SourceObjectId;
            }

            public override bool Equals(
                object obj)
            {
                return
                    obj is GameplayCueNotifyKey other &&
                    Equals(
                        other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    m_TargetId,
                    m_NotifyId,
                    m_InstigatorId,
                    m_SourceObjectId);
            }
        }

        private readonly GameplayCueSet m_RuntimeCueSet;

        private readonly Dictionary<
            GameplayCueNotifyKey,
            ActiveGameplayCue> m_ActiveGameplayCues = new();

        private readonly DisposableEvent<
            AbilitySystemComponent,
            GameplayTag,
            GameplayCueParameters> m_GameplayCueExecuted = new();

        public GameplayCueManager(
            GameplayCueSet runtimeCueSet)
        {
            m_RuntimeCueSet =
                runtimeCueSet != null
                    ? runtimeCueSet
                    : throw new ArgumentNullException(
                        nameof(runtimeCueSet));
        }

        /// <summary>
        /// Returns the gameplay cue set used for runtime event routing.
        /// </summary>
        public GameplayCueSet GetRuntimeCueSet()
        {
            return m_RuntimeCueSet;
        }

        /// <summary>
        /// Registers a handler for standalone executed gameplay cues.
        /// </summary>
        public IDisposable RegisterGameplayCueExecuted(
            Action<
                AbilitySystemComponent,
                GameplayTag,
                GameplayCueParameters> handler)
        {
            return m_GameplayCueExecuted.Subscribe(
                handler);
        }

        /// <summary>
        /// Dispatches an executed gameplay cue from its owning ability system.
        /// </summary>
        public virtual void InvokeGameplayCueExecuted(
            AbilitySystemComponent owningComponent,
            GameplayTag gameplayCueTag,
            GameplayCueParameters parameters)
        {
            if (owningComponent == null)
            {
                throw new ArgumentNullException(
                    nameof(owningComponent));
            }

            if (gameplayCueTag == null)
            {
                throw new ArgumentNullException(
                    nameof(gameplayCueTag));
            }

            if (parameters == null)
            {
                throw new ArgumentNullException(
                    nameof(parameters));
            }

            m_GameplayCueExecuted.Invoke(
                owningComponent,
                gameplayCueTag,
                parameters);

            owningComponent.InvokeGameplayCueEvent(
                gameplayCueTag,
                GameplayCueEvent.Executed,
                parameters);
        }

        /// <summary>
        /// Handles a gameplay cue event through the configured runtime cue set.
        /// </summary>
        public virtual void HandleGameplayCue(
            GameObject target,
            GameplayTag gameplayCueTag,
            GameplayCueEvent eventType,
            GameplayCueParameters parameters)
        {
            if (gameplayCueTag == null)
            {
                throw new ArgumentNullException(
                    nameof(gameplayCueTag));
            }

            if (parameters == null)
            {
                throw new ArgumentNullException(
                    nameof(parameters));
            }

            RouteGameplayCue(
                target,
                gameplayCueTag,
                eventType,
                parameters);
        }

        /// <summary>
        /// Applies one gameplay cue event to its immutable notify definition.
        /// </summary>
        internal void HandleGameplayCueNotify(
            GameplayCueNotify notify,
            GameObject target,
            GameplayCueEvent eventType,
            GameplayCueParameters parameters)
        {
            if (notify == null)
            {
                throw new ArgumentNullException(
                    nameof(notify));
            }

            if (target == null)
            {
                return;
            }

            GameplayCueNotifyKey key =
                new(
                    target,
                    notify,
                    parameters);

            switch (eventType)
            {
                case GameplayCueEvent.OnActive:
                    HandleOnActive(
                        key,
                        notify,
                        target,
                        parameters);
                    break;

                case GameplayCueEvent.WhileActive:
                    HandleWhileActive(
                        key,
                        notify,
                        target,
                        parameters);
                    break;

                case GameplayCueEvent.Executed:
                    SpawnPrefab(
                        notify.ExecutedPrefab,
                        notify,
                        target,
                        parameters);
                    break;

                case GameplayCueEvent.Removed:
                    HandleRemoved(
                        key,
                        notify,
                        target,
                        parameters);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(eventType),
                        eventType,
                        "Unsupported gameplay cue event.");
            }
        }

        private void HandleOnActive(
            GameplayCueNotifyKey key,
            GameplayCueNotify notify,
            GameObject target,
            GameplayCueParameters parameters)
        {
            bool wasAlreadyActive =
                m_ActiveGameplayCues.TryGetValue(
                    key,
                    out ActiveGameplayCue activeCue);

            if (!wasAlreadyActive)
            {
                activeCue =
                    new ActiveGameplayCue();

                m_ActiveGameplayCues.Add(
                    key,
                    activeCue);
            }

            activeCue.Count++;

            if (
                !wasAlreadyActive ||
                notify.AllowMultipleOnActiveEvents)
            {
                SpawnPrefab(
                    notify.OnActivePrefab,
                    notify,
                    target,
                    parameters);
            }
        }

        private void HandleWhileActive(
            GameplayCueNotifyKey key,
            GameplayCueNotify notify,
            GameObject target,
            GameplayCueParameters parameters)
        {
            if (
                !m_ActiveGameplayCues.TryGetValue(
                    key,
                    out ActiveGameplayCue activeCue))
            {
                activeCue =
                    new ActiveGameplayCue
                    {
                        Count = 1
                    };

                m_ActiveGameplayCues.Add(
                    key,
                    activeCue);
            }

            if (activeCue.Instance != null)
            {
                return;
            }

            activeCue.Instance =
                SpawnPrefab(
                    notify.WhileActivePrefab,
                    notify,
                    target,
                    parameters);
        }

        private void HandleRemoved(
            GameplayCueNotifyKey key,
            GameplayCueNotify notify,
            GameObject target,
            GameplayCueParameters parameters)
        {
            if (
                !m_ActiveGameplayCues.TryGetValue(
                    key,
                    out ActiveGameplayCue activeCue))
            {
                return;
            }

            activeCue.Count--;

            if (activeCue.Count > 0)
            {
                return;
            }

            m_ActiveGameplayCues.Remove(
                key);

            if (activeCue.Instance != null)
            {
                Object.Destroy(
                    activeCue.Instance);
            }

            SpawnPrefab(
                notify.RemovedPrefab,
                notify,
                target,
                parameters);
        }

        private static GameObject SpawnPrefab(
            GameObject prefab,
            GameplayCueNotify notify,
            GameObject target,
            GameplayCueParameters parameters)
        {
            if (prefab == null)
            {
                return null;
            }

            Vector3 position =
                parameters.Location != Vector3.zero
                    ? parameters.Location
                    : target.transform.position;

            Quaternion rotation =
                parameters.Normal.sqrMagnitude > Mathf.Epsilon
                    ? Quaternion.FromToRotation(
                        Vector3.up,
                        parameters.Normal.normalized)
                    : target.transform.rotation;

            Transform parent =
                notify.AttachToTarget
                    ? target.transform
                    : null;

            return Object.Instantiate(
                prefab,
                position,
                rotation,
                parent);
        }

        /// <summary>
        /// Routes a validated gameplay cue event to its runtime cue set.
        /// </summary>
        protected virtual void RouteGameplayCue(
            GameObject target,
            GameplayTag gameplayCueTag,
            GameplayCueEvent eventType,
            GameplayCueParameters parameters)
        {
            if (parameters.OriginalTag == null)
            {
                parameters.OriginalTag =
                    gameplayCueTag;
            }

            m_RuntimeCueSet.HandleGameplayCue(
                this,
                target,
                gameplayCueTag,
                eventType,
                parameters);
        }
    }
}