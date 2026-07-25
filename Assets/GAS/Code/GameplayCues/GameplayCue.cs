using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace GAS
{

    public static class GameplayCueManager
    {
        public static void RegisterAbilityEventBridge(
            AbilitySystemComponent target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(
                    nameof(target));
            }

            target.OnGameplayAbilityActivated +=
                (ability, activationId) =>
                    ApplyAbilityCue(
                        target,
                        ability);

            target.OnGameplayAbilityDeactivated +=
                (ability, activationId) =>
                    RemoveAbilityCue(
                        target,
                        ability);
        }

        public static void ApplyEffectCue(
            AbilitySystemComponent target,
            GameplayEffect runtimeEffect)
        {
            if (target == null)
            {
                throw new ArgumentNullException(
                    nameof(target));
            }

            if (runtimeEffect == null)
            {
                throw new ArgumentNullException(
                    nameof(runtimeEffect));
            }

            if (!HasCueTags(runtimeEffect.cuesTags))
                return;

            ApplyCues(
                runtimeEffect.cuesTags,
                target,
                instantDestroy:
                    runtimeEffect.durationType ==
                    GameplayEffectDurationType.Instant,
                gameplayAbility: null,
                gameplayEffect: runtimeEffect);
        }

        public static void RemoveEffectCue(
            AbilitySystemComponent target,
            GameplayEffect runtimeEffect)
        {
            if (target == null)
            {
                throw new ArgumentNullException(
                    nameof(target));
            }

            if (runtimeEffect == null)
            {
                throw new ArgumentNullException(
                    nameof(runtimeEffect));
            }

            if (!HasCueTags(runtimeEffect.cuesTags))
                return;

            RemoveCues(
                runtimeEffect.cuesTags,
                target,
                gameplayAbility: null,
                gameplayEffect: runtimeEffect);
        }

        public static void ApplyAbilityCue(
            AbilitySystemComponent target,
            GameplayAbility gameplayAbility)
        {
            if (target == null)
            {
                throw new ArgumentNullException(
                    nameof(target));
            }

            if (gameplayAbility == null)
            {
                throw new ArgumentNullException(
                    nameof(gameplayAbility));
            }

            if (!HasCueTags(gameplayAbility.cuesTags))
                return;

            ApplyCues(
                gameplayAbility.cuesTags,
                target,
                instantDestroy:
                    !gameplayAbility.isActive,
                gameplayAbility,
                gameplayEffect: null);
        }

        public static void RemoveAbilityCue(
            AbilitySystemComponent target,
            GameplayAbility gameplayAbility)
        {
            if (target == null)
            {
                throw new ArgumentNullException(
                    nameof(target));
            }

            if (gameplayAbility == null)
            {
                throw new ArgumentNullException(
                    nameof(gameplayAbility));
            }

            if (!HasCueTags(gameplayAbility.cuesTags))
                return;

            RemoveCues(
                gameplayAbility.cuesTags,
                target,
                gameplayAbility,
                gameplayEffect: null);
        }

        private static void ApplyCues(
            IReadOnlyList<GameplayTag> cueTags,
            AbilitySystemComponent target,
            bool instantDestroy,
            GameplayAbility gameplayAbility,
            GameplayEffect gameplayEffect)
        {
            foreach (GameplayTag cueTag in cueTags)
            {
                List<GameplayCue> cues =
                    CuesLibrary.Instance.CreateCues(
                        cueTag);

                foreach (GameplayCue cue in cues)
                {
                    if (cue == null)
                        continue;

                    cue.AddCue(
                        target,
                        instantDestroy,
                        new GameplayCueApplicationData(
                            gameplayAbility,
                            gameplayEffect,
                            target,
                            null));
                }
            }
        }

        private static void RemoveCues(
            IReadOnlyList<GameplayTag> cueTags,
            AbilitySystemComponent target,
            GameplayAbility gameplayAbility,
            GameplayEffect gameplayEffect)
        {
            for (int index =
                     target.instancedCues.Count - 1;
                 index >= 0;
                 index--)
            {
                GameplayCue cue =
                    target.instancedCues[index];

                if (cue == null ||
                    cue.applicationData == null)
                {
                    continue;
                }

                if (!ContainsTag(
                        cueTags,
                        cue.tag))
                {
                    continue;
                }

                if (!cue.applicationData.IsOrigin(
                        gameplayAbility,
                        gameplayEffect))
                {
                    continue;
                }

                cue.RemoveCue(target);
            }
        }

        private static bool HasCueTags(
            IReadOnlyCollection<GameplayTag> cueTags)
        {
            return cueTags != null &&
                   cueTags.Count > 0;
        }

        private static bool ContainsTag(
            IReadOnlyList<GameplayTag> cueTags,
            GameplayTag tag)
        {
            for (int index = 0;
                 index < cueTags.Count;
                 index++)
            {
                if (cueTags[index] == tag)
                    return true;
            }

            return false;
        }
    }


    public class GameplayCueApplicationData
    {
        public GameplayAbility ga;
        public GameplayEffect ge;
        public AbilitySystemComponent src, tgt;
        public string originName;

        public GameplayCueApplicationData(GameplayAbility ga, GameplayEffect ge, AbilitySystemComponent src, AbilitySystemComponent tgt)
        {
            this.ga = ga;
            this.ge = ge;
            this.src = src;
            this.tgt = tgt;
            originName = ga == null ? ge.name : ga.name;
        }

        public bool IsOrigin(GameplayAbility gaToCheck, GameplayEffect geToCheck)
        {
            if (gaToCheck == ga) return true;
            if (geToCheck == ge) return true;
            return false;
        }
    }

    [System.Serializable]
    public class GameplayCue
    {
        public GameObject prefab; //Can be a looping SFX or VFX
        public GameObject instance; //instantiated cue go
        public GameplayTag tag;
        public Vector3 offset;

        public GameplayCueApplicationData applicationData;

        public virtual void AddCue(AbilitySystemComponent asc, bool instantDestroy, GameplayCueApplicationData appData)
        {
            if (prefab == null) { Debug.Log($"AddCue with NULL prefab"); return; }
            applicationData = appData;
            PlaceCue(asc);
            if (instantDestroy)
            {
                RemoveCue(asc);
            }
        }

        public virtual async void RemoveCue(AbilitySystemComponent asc)
        {
            // Debug.Log($"RemoveCue - cue tag: {tag.name}");
            if (instance != null) instance.SendMessage("OnDestroySoon", SendMessageOptions.DontRequireReceiver);
            await Task.Delay(3_000);
            // Debug.Log($"RemoveCue - cue tag: {tag.name} AFTER DELAY");
            asc.instancedCues.Remove(this);
            if (instance == null) return;
            // Debug.Log($"RemoveCue: instance.name {instance.name}");
            GameObject.Destroy(instance);
        }

        public void PlaceCue(AbilitySystemComponent asc)
        {
            instance = GameObject.Instantiate(prefab);
            instance.name = "cueInstance_" + prefab.name;
            // Debug.Log($"PlaceCue: place {spawnPlace} src {src} target {target} ");

            instance.transform.SetParent(asc.transform);
            instance.transform.position = asc.transform.position + asc.transform.forward * offset.z + asc.transform.right * offset.x + asc.transform.up * offset.y;
            asc.instancedCues.Add(this);

        }


        //Examples: Melee vfx (trail and impact), Projectile impact, Spell cooldown failed.

        // We trigger GameplayCues by sending a corresponding GameplayTag with the mandatory parent name of GameplayCue. e.g. CueEvent<GameplayTag> = GameplayTag.Cue_XYZ
        // public Action OnActive, WhileActive, Removed, Executed; unreal has those... do we need them?

        // AggregatedSourceTags
        // AggregatedTargetTags
        // GameplayEffectLevel
        // AbilityLevel
        // EffectContext
        // Magnitude (if the GameplayEffect has an Attribute for magnitude selected in the dropdown above the GameplayCue tag container and a corresponding Modifier that affects that Attribute)
    }
}
