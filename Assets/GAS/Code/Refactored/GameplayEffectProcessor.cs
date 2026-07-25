using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GAS
{
    public sealed class GameplayEffectProcessor
    {
        public bool RemoveEffect(
            AbilitySystemComponent target,
            string effectId,
            GameplayEffectNotificationOptions notifications)
        {
            if (target == null ||
                string.IsNullOrEmpty(effectId))
            {
                return false;
            }

            GameplayEffect runtimeEffect =
                target.appliedGameplayEffects.Find(
                    effect =>
                        effect.guid == effectId);

            return RemoveEffect(
                target,
                runtimeEffect,
                notifications);
        }

        public GameplayEffect ApplyEffect(
            AbilitySystemComponent target,
            AbilitySystemComponent source,
            GameplayEffect effectSpec,
            string applicationId,
            GameplayEffectApplicationOptions options)
        {
            if (target == null ||
                effectSpec == null)
            {
                return null;
            }

            GameplayEffect runtimeEffect =
                effectSpec.Instantiate();

            runtimeEffect.source = source;
            runtimeEffect.target = target;
            runtimeEffect.applicationGUID =
                applicationId;

            if (!CanApplyEffect(
                    target,
                    runtimeEffect,
                    options))
            {
                return null;
            }

            CommitEffect(
                target,
                runtimeEffect,
                options
                    .NotificationOptions
                    .NotifyEffectEvents);

            NotifyEffectApplied(
                target,
                runtimeEffect,
                options.NotificationOptions);

            StartLifetime(
                target,
                runtimeEffect);

            return runtimeEffect;
        }



        public bool RemoveEffect(
            AbilitySystemComponent target,
            GameplayEffect runtimeEffect,
            GameplayEffectNotificationOptions notifications)
        {
            if (target == null ||
                runtimeEffect == null)
            {
                return false;
            }

            if (!target.appliedGameplayEffects.Remove(
                    runtimeEffect))
            {
                return false;
            }

            target.RefreshAttributesModifiers(
                runtimeEffect,
                notifications.NotifyEffectEvents);

            RefreshTags(
                target,
                runtimeEffect,
                notifications.NotifyTagEvents);

            if (notifications.NotifyEffectEvents)
            {
                target.OnGameplayEffectsChanged?.Invoke(
                    target.appliedGameplayEffects);
            }

            if (notifications.NotifyCueEvents)
            {
                GameplayCueManager.RemoveEffectCue(
                    target,
                    runtimeEffect);
            }

            if (notifications.NotifyEffectEvents)
            {
                target.OnGameplayEffectRemoved?.Invoke(
                    runtimeEffect);
            }

            return true;
        }

        private static bool CanApplyEffect(
            AbilitySystemComponent target,
            GameplayEffect effectSpec,
            GameplayEffectApplicationOptions options)
        {
            if (!options.IgnoreTagRequirements &&
                !TagProcessor
                    .CheckApplicationTagRequirementsGE(
                        target,
                        effectSpec,
                        target.tags))
            {
                return false;
            }

            if (options.IgnoreChanceRoll ||
                effectSpec.chanceToApply >= 1f)
            {
                return true;
            }

            return UnityEngine.Random.Range(
                       0f,
                       1f) <=
                   effectSpec.chanceToApply;
        }

        /// <summary>
        /// Commits a runtime effect to the target ability system component.
        /// </summary>
        private static void CommitEffect(
            AbilitySystemComponent target,
            GameplayEffect runtimeEffect,
            bool notifyAttributeEvents)
        {
            switch (runtimeEffect.durationType)
            {
                case GameplayEffectDurationType.Instant:
                    ApplyInstantModifiers(
                        target,
                        runtimeEffect,
                        notifyAttributeEvents);
                    break;

                case GameplayEffectDurationType.Duration:
                case GameplayEffectDurationType.Infinite:
                    target.appliedGameplayEffects.Add(
                        runtimeEffect);

                    target.RefreshAttributesModifiers(
                        runtimeEffect,
                        notifyAttributeEvents);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(runtimeEffect.durationType),
                        runtimeEffect.durationType,
                        "Unknown GameplayEffect duration type.");
            }
        }

        public bool SuspendEffect(
            AbilitySystemComponent target,
            string effectId)
        {
            GameplayEffect runtimeEffect =
                FindEffect(
                    target,
                    effectId);

            if (runtimeEffect == null)
                return false;

            runtimeEffect.suspensionCount++;

            if (runtimeEffect.suspensionCount > 1)
                return true;

            target.RefreshAttributesModifiers(
                runtimeEffect,
                notifyEvents: false);

            RefreshTags(
                target,
                runtimeEffect,
                notifyTagEvents: false);

            return true;
        }

        public bool ResumeEffect(
            AbilitySystemComponent target,
            string effectId)
        {
            GameplayEffect runtimeEffect =
                FindEffect(
                    target,
                    effectId);

            if (runtimeEffect == null ||
                runtimeEffect.suspensionCount == 0)
            {
                return false;
            }

            runtimeEffect.suspensionCount--;

            if (!runtimeEffect.IsEnabled)
                return true;

            target.RefreshAttributesModifiers(
                runtimeEffect,
                notifyEvents: false);

            RefreshTags(
                target,
                runtimeEffect,
                notifyTagEvents: false);

            return true;
        }

        private static GameplayEffect FindEffect(
            AbilitySystemComponent target,
            string effectId)
        {
            if (target == null ||
                string.IsNullOrEmpty(effectId))
            {
                return null;
            }

            return target.appliedGameplayEffects.Find(
                effect =>
                    effect != null &&
                    effect.guid == effectId);
        }

        /// <summary>
        /// Applies every matching instant modifier to the target attributes.
        /// </summary>
        private static void ApplyInstantModifiers(
            AbilitySystemComponent target,
            GameplayEffect runtimeEffect,
            bool notifyAttributeEvents)
        {
            foreach (Attribute attribute
                     in target.attributes)
            {
                foreach (Modifier modifier
                         in runtimeEffect.modifiers)
                {
                    if (attribute.attributeName !=
                        modifier.attributeName)
                    {
                        continue;
                    }

                    attribute.ApplyModifierAsResource(
                        modifier,
                        runtimeEffect,
                        notifyAttributeEvents);
                }
            }
        }

        private static void NotifyEffectApplied(
            AbilitySystemComponent target,
            GameplayEffect runtimeEffect,
            GameplayEffectNotificationOptions notifications)
        {
            if (notifications.NotifyEffectEvents)
            {
                target.OnGameplayEffectsChanged?.Invoke(
                    target.appliedGameplayEffects);
            }

            RefreshTags(
                target,
                runtimeEffect,
                notifications.NotifyTagEvents);

            if (notifications.NotifyTagEvents &&
                runtimeEffect.durationType ==
                GameplayEffectDurationType.Instant)
            {
                target.TriggerOnTagsAdded(
                    runtimeEffect);
            }

            if (notifications.NotifyCueEvents)
            {
                GameplayCueManager.ApplyEffectCue(
                    target,
                    runtimeEffect);
            }

            if (notifications.NotifyEffectEvents)
            {
                target.OnGameplayEffectApplied?.Invoke(
                    runtimeEffect);
            }
        }

        private static void RefreshTags(
            AbilitySystemComponent target,
            GameplayEffect causeEffect,
            bool notifyTagEvents)
        {
            Action<
                List<GameplayTag>,
                AbilitySystemComponent,
                AbilitySystemComponent,
                string> tagChanged =
                    notifyTagEvents
                    ? target.OnTagsChanged
                    : null;

            TagProcessor.UpdateTags(
                causeEffect.source,
                target,
                ref target.tags,
                target.appliedGameplayEffects,
                target.grantedGameplayAbilities,
                tagChanged,
                causeEffect.applicationGUID);
        }

        private void StartLifetime(
            AbilitySystemComponent target,
            GameplayEffect runtimeEffect)
        {
            if (runtimeEffect.durationType !=
                GameplayEffectDurationType.Duration)
            {
                return;
            }

            ScheduleDurationRemoval(
                target,
                runtimeEffect)
            .Forget();
        }

        private async UniTaskVoid ScheduleDurationRemoval(
            AbilitySystemComponent target,
            GameplayEffect runtimeEffect)
        {
            float durationSeconds =
                runtimeEffect.durationValue;

            if (float.IsNaN(durationSeconds) ||
                float.IsInfinity(durationSeconds) ||
                durationSeconds <= 0f)
            {
                RemoveEffect(
                    target,
                    runtimeEffect,
                    GameplayEffectNotificationOptions.All);

                return;
            }

            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(
                        durationSeconds),
                    cancellationToken:
                        target
                            .GetCancellationTokenOnDestroy());
            }
            catch (OperationCanceledException)
            {
                return;
            }

            RemoveEffect(
                target,
                runtimeEffect,
                GameplayEffectNotificationOptions.All);
        }
    }
}