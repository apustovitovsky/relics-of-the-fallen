using System;
using GAS;
using UnityEngine;

namespace RelicsOfTheFallen.AbilitySystem
{
    [Serializable]
    public sealed class NotifyGameplayAbility
        : GameplayAbility
    {
        [SerializeField]
        string m_AnimationTrigger;

        [SerializeField]
        GameplayTag m_ImpactEventTag;

        [SerializeField]
        GameplayTag m_EndEventTag;

        [SerializeField]
        [HideInInspector]
        string m_ImpactEventTagName;

        [SerializeField]
        [HideInInspector]
        string m_EndEventTagName;

        [NonSerialized]
        bool m_ImpactConsumed;

        public string AnimationTrigger =>
            m_AnimationTrigger;

        public override GameplayAbility Instantiate(
            AbilitySystemComponent owner)
        {
            var instance =
                (NotifyGameplayAbility)base.Instantiate(
                    owner);

            instance.m_AnimationTrigger =
                m_AnimationTrigger;

            instance.m_ImpactEventTag =
                m_ImpactEventTag;

            instance.m_EndEventTag =
                m_EndEventTag;

            instance.m_ImpactEventTagName =
                m_ImpactEventTagName;

            instance.m_EndEventTagName =
                m_EndEventTagName;

            return instance;
        }

        public override void SerializeAdditionalData()
        {
            base.SerializeAdditionalData();

            m_ImpactEventTagName =
                m_ImpactEventTag != null
                    ? m_ImpactEventTag.name
                    : string.Empty;

            m_EndEventTagName =
                m_EndEventTag != null
                    ? m_EndEventTag.name
                    : string.Empty;
        }

        public override void DeserializeAdditionalData()
        {
            base.DeserializeAdditionalData();

            m_ImpactEventTag =
                ResolveGameplayTag(
                    m_ImpactEventTagName);

            m_EndEventTag =
                ResolveGameplayTag(
                    m_EndEventTagName);
        }

        public override bool CanActivateAbility(
            AbilitySystemComponent source,
            AbilitySystemComponent target,
            string activationGUID,
            bool sendFailedEvent)
        {
            if (source == null ||
                target == null ||
                string.IsNullOrWhiteSpace(
                    m_AnimationTrigger) ||
                m_ImpactEventTag == null ||
                m_EndEventTag == null)
            {
                if (sendFailedEvent && source != null)
                {
                    source
                        .OnGameplayAbilityFailedActivation
                        ?.Invoke(
                            this,
                            activationGUID,
                            ActivationFailure.OTHER);
                }

                return false;
            }

            return base.CanActivateAbility(
                source,
                target,
                activationGUID,
                sendFailedEvent);
        }

        public override void Activate(
            AbilitySystemComponent source,
            AbilitySystemComponent target,
            string activationGUID)
        {
            base.Activate(
                source,
                target,
                activationGUID);

            m_ImpactConsumed = false;

            source.OnGameplayEvent -=
                HandleGameplayEvent;

            source.OnGameplayEvent +=
                HandleGameplayEvent;
        }

        public override void DeactivateAbility(
            string activationGUID = null)
        {
            if (source != null)
            {
                source.OnGameplayEvent -=
                    HandleGameplayEvent;
            }

            m_ImpactConsumed = false;

            string resolvedActivationGUID =
                string.IsNullOrEmpty(activationGUID)
                    ? this.activationGUID
                    : activationGUID;

            base.DeactivateAbility(
                resolvedActivationGUID);
        }

        void HandleGameplayEvent(
            GameplayEventData gameplayEvent)
        {
            if (!isActive ||
                gameplayEvent.Tag == null ||
                !string.Equals(
                    gameplayEvent.ActivationGUID,
                    activationGUID,
                    StringComparison.Ordinal))
            {
                return;
            }

            if (gameplayEvent.Tag == m_ImpactEventTag)
            {
                HandleImpact();

                return;
            }

            if (gameplayEvent.Tag == m_EndEventTag)
            {
                DeactivateAbility(
                    gameplayEvent.ActivationGUID);
            }
        }

        void HandleImpact()
        {
            if (m_ImpactConsumed)
            {
                return;
            }

            m_ImpactConsumed = true;

            foreach (GameplayEffect effect in effects)
            {
                target.ApplyGameplayEffect(
                    source,
                    target,
                    effect,
                    activationGUID);
            }
        }

        static GameplayTag ResolveGameplayTag(
            string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return null;
            }

            return GameplayTagLibrary
                .Instance
                .GetByName(tagName);
        }
    }
}