using System;

namespace GAS
{
    public sealed class AbilityTask_WaitGameplayEvent :
        AbilityTask
    {
        private readonly DisposableGroup m_Subscriptions = new();

        private readonly DisposableEvent<GameplayEventData>
            m_EventReceivedDelegate = new();

        public GameplayTag Tag
        {
            get;
        }

        public AbilitySystemComponent OptionalExternalTarget
        {
            get;
            private set;
        }

        public bool UseExternalTarget
        {
            get;
            private set;
        }

        public bool OnlyTriggerOnce
        {
            get;
        }

        public bool OnlyMatchExact
        {
            get;
        }

        private AbilityTask_WaitGameplayEvent(
            GameplayAbility owningAbility,
            GameplayTag eventTag,
            AbilitySystemComponent optionalExternalTarget,
            bool onlyTriggerOnce,
            bool onlyMatchExact)
            : base(
                owningAbility)
        {
            if (eventTag == null)
            {
                throw new ArgumentNullException(
                    nameof(eventTag));
            }

            Tag = eventTag;
            OnlyTriggerOnce = onlyTriggerOnce;
            OnlyMatchExact = onlyMatchExact;

            SetExternalTarget(
                optionalExternalTarget);
        }

        /// <summary>
        /// Creates a task that waits for a matching gameplay event.
        /// </summary>
        public static AbilityTask_WaitGameplayEvent WaitGameplayEvent(
            GameplayAbility owningAbility,
            GameplayTag eventTag,
            AbilitySystemComponent optionalExternalTarget = null,
            bool onlyTriggerOnce = false,
            bool onlyMatchExact = true)
        {
            return new AbilityTask_WaitGameplayEvent(
                owningAbility,
                eventTag,
                optionalExternalTarget,
                onlyTriggerOnce,
                onlyMatchExact);
        }

        /// <summary>
        /// Registers a callback invoked when this task receives a matching gameplay event.
        /// </summary>
        public IDisposable RegisterEventReceivedDelegate(
            Action<GameplayEventData> handler)
        {
            IDisposable subscription =
                m_EventReceivedDelegate.Subscribe(
                    handler);

            m_Subscriptions.Add(
                subscription);

            return subscription;
        }

        /// <summary>
        /// Selects an external ability system component as the gameplay event source.
        /// </summary>
        public void SetExternalTarget(
            AbilitySystemComponent abilitySystemComponent)
        {
            if (abilitySystemComponent == null)
            {
                return;
            }

            UseExternalTarget = true;
            OptionalExternalTarget = abilitySystemComponent;
        }

        /// <summary>
        /// Returns the ability system component observed by this task.
        /// </summary>
        public AbilitySystemComponent GetTargetASC()
        {
            if (UseExternalTarget)
            {
                return OptionalExternalTarget;
            }

            return AbilitySystemComponent;
        }

        /// <summary>
        /// Registers the exact or hierarchical gameplay event callback.
        /// </summary>
        protected override void Activate()
        {
            AbilitySystemComponent targetAbilitySystem =
                GetTargetASC();

            if (targetAbilitySystem == null)
            {
                return;
            }

            if (OnlyMatchExact)
            {
                m_Subscriptions.Add(
                    targetAbilitySystem.AddGenericGameplayEventCallback(
                        Tag,
                        GameplayEventCallback));

                return;
            }

            GameplayTagContainer tagFilter = new();

            tagFilter.AddTag(
                Tag);

            m_Subscriptions.Add(
                targetAbilitySystem.AddGameplayEventTagContainerDelegate(
                    tagFilter,
                    GameplayEventContainerCallback));
        }

        /// <summary>
        /// Releases gameplay event callbacks when this task ends.
        /// </summary>
        protected override void OnDestroy(
            bool abilityEnded)
        {
            m_Subscriptions.Dispose();
            m_EventReceivedDelegate.Clear();

            base.OnDestroy(
                abilityEnded);
        }

        private void GameplayEventCallback(
            GameplayEventData payload)
        {
            GameplayEventContainerCallback(
                Tag,
                payload);
        }

        private void GameplayEventContainerCallback(
            GameplayTag matchingTag,
            GameplayEventData payload)
        {
            if (IsEnded)
            {
                return;
            }

            payload.EventTag = matchingTag;

            m_EventReceivedDelegate.Invoke(
                payload);

            if (OnlyTriggerOnce)
            {
                EndTask();
            }
        }
    }
}