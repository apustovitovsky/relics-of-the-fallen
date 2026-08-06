using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Carries contextual metadata for one gameplay cue invocation.
    /// </summary>
    public sealed class GameplayCueParameters
    {
        public float NormalizedMagnitude
        {
            get; set;
        }

        public float RawMagnitude
        {
            get; set;
        }

        public GameplayEffectContextHandle EffectContext
        {
            get; set;
        }

        public GameplayTag MatchedTagName
        {
            get; set;
        }

        public GameplayTag OriginalTag
        {
            get; set;
        }

        public GameplayTagContainer AggregatedSourceTags
        {
            get;
        }

        public GameplayTagContainer AggregatedTargetTags
        {
            get;
        }

        public Vector3 Location
        {
            get; set;
        }

        public Vector3 Normal
        {
            get; set;
        }

        public GameObject Instigator
        {
            get; set;
        }

        public GameObject EffectCauser
        {
            get; set;
        }

        public Object SourceObject
        {
            get; set;
        }

        public PhysicsMaterial PhysicalMaterial
        {
            get; set;
        }

        public int GameplayEffectLevel
        {
            get; set;
        }

        public int AbilityLevel
        {
            get; set;
        }

        public Transform TargetAttachComponent
        {
            get; set;
        }

        public bool ReplicateLocationWhenUsingMinimalRepProxy
        {
            get; set;
        }

        public bool IsGameplayEffectActive
        {
            get; set;
        }

        /// <summary>
        /// Creates gameplay cue parameters with initialized tag containers and default levels.
        /// </summary>
        public GameplayCueParameters()
        {
            AggregatedSourceTags = new GameplayTagContainer();

            AggregatedTargetTags = new GameplayTagContainer();

            GameplayEffectLevel = 1;

            AbilityLevel = 1;

            IsGameplayEffectActive = true;
        }

        /// <summary>
        /// Creates gameplay cue parameters backed by an existing gameplay effect context.
        /// </summary>
        public GameplayCueParameters(
            GameplayEffectContextHandle effectContext)
            : this()
        {
            EffectContext = effectContext;
        }

        /// <summary>
        /// Returns the explicit instigator or falls back to the gameplay effect context.
        /// </summary>
        public GameObject GetInstigator()
        {
            if (Instigator != null)
            {
                return Instigator;
            }

            return EffectContext.GetInstigator();
        }

        /// <summary>
        /// Returns the explicit effect causer or falls back to the gameplay effect context.
        /// </summary>
        public GameObject GetEffectCauser()
        {
            if (EffectCauser != null)
            {
                return EffectCauser;
            }

            return EffectContext.GetEffectCauser();
        }

        /// <summary>
        /// Returns the explicit source object or falls back to the gameplay effect context.
        /// </summary>
        public Object GetSourceObject()
        {
            if (SourceObject != null)
            {
                return SourceObject;
            }

            return EffectContext.GetSourceObject();
        }
    }
}