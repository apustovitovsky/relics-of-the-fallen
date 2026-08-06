using System;
using UnityEngine;

namespace GAS
{
    [CreateAssetMenu(
        menuName = "GAS/Ability System Globals",
        fileName = "AbilitySystemGlobals")]
    public sealed class AbilitySystemGlobals :
        ScriptableObject
    {
        private const string k_ResourcePath =
            "AbilitySystem/AbilitySystemGlobals";

        private const string k_ActivateFailCooldownTagName =
            "Ability.ActivateFail.Cooldown";

        private const string k_ActivateFailCostTagName =
            "Ability.ActivateFail.Cost";

        private static AbilitySystemGlobals s_Instance;

        private GameplayCueManager m_GameplayCueManager;

        [field: SerializeField]
        public GameplayCueSet GameplayCueSet
        {
            get;
            private set;
        }

        public static AbilitySystemGlobals Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance =
                        Resources.Load<AbilitySystemGlobals>(
                            k_ResourcePath);
                }

                if (s_Instance == null)
                {
                    throw new InvalidOperationException(
                        $"Create '{k_ResourcePath}.asset' inside a Resources folder.");
                }

                return s_Instance;
            }
        }

        public static GameplayTag ActivateFailCooldownTag =>
            GameplayTagLibrary.Instance.GetByName(
                k_ActivateFailCooldownTagName);

        public static GameplayTag ActivateFailCostTag =>
            GameplayTagLibrary.Instance.GetByName(
                k_ActivateFailCostTagName);

        private void OnEnable()
        {
            m_GameplayCueManager = null;
        }

        /// <summary>
        /// Returns the global gameplay cue manager and initializes it when first requested.
        /// </summary>
        public GameplayCueManager GetGameplayCueManager()
        {
            if (m_GameplayCueManager == null)
            {
                if (GameplayCueSet == null)
                {
                    throw new InvalidOperationException(
                        $"{name} requires a runtime gameplay cue set.");
                }

                m_GameplayCueManager =
                    new GameplayCueManager(
                        GameplayCueSet);
            }

            return m_GameplayCueManager;
        }

        /// <summary>
        /// Finds the ability system associated with the requested gameplay actor root.
        /// </summary>
        public static bool TryGetAbilitySystemComponentFromActor(
            GameObject actor,
            out AbilitySystemComponent abilitySystem)
        {
            abilitySystem = null;

            if (actor == null)
            {
                return false;
            }

            abilitySystem =
                actor.GetComponentInChildren<AbilitySystemComponent>(
                    true);

            return abilitySystem != null;
        }
    }
}