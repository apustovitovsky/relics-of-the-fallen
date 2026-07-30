using System;
using UnityEngine;

namespace GAS
{
    public class GameplayEffectContext
    {
        public GameObject Instigator
        {
            get;
            private set;
        }

        public GameObject EffectCauser
        {
            get;
            private set;
        }

        private GameObject OriginalInstigator
        {
            get;
            set;
        }

        private UnityEngine.Object SourceObject
        {
            get;
            set;
        }

        private AbilitySystemComponent OriginalInstigatorAbilitySystemComponent
        {
            get;
        }

        private GameplayAbilitySO Ability
        {
            get;
            set;
        }

        private GameplayAbility AbilityInstanceNotReplicated
        {
            get;
            set;
        }

        private int AbilityLevel
        {
            get;
            set;
        } = 1;

        /// <summary>
        /// Creates an empty gameplay effect context for later initialization.
        /// </summary>
        public GameplayEffectContext()
        {
        }

        /// <summary>
        /// Creates an independent gameplay effect context from existing context state.
        /// </summary>
        protected GameplayEffectContext(
            GameplayEffectContext source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(
                    nameof(source));
            }

            Instigator = source.Instigator;
            EffectCauser = source.EffectCauser;
            OriginalInstigator = source.OriginalInstigator;
            SourceObject = source.SourceObject;
            OriginalInstigatorAbilitySystemComponent =
                source.OriginalInstigatorAbilitySystemComponent;
            Ability = source.Ability;
            AbilityInstanceNotReplicated =
                source.AbilityInstanceNotReplicated;
            AbilityLevel = source.AbilityLevel;
        }

        /// <summary>
        /// Creates an independent copy of this gameplay effect context.
        /// </summary>
        public virtual GameplayEffectContext Duplicate()
        {
            return new GameplayEffectContext(
                this);
        }

        /// <summary>
        /// Creates gameplay effect context from initialized ability actor information.
        /// </summary>
        internal GameplayEffectContext(
            GameplayAbilityActorInfo actorInfo)
        {
            if (actorInfo == null)
            {
                throw new ArgumentNullException(
                    nameof(actorInfo));
            }

            AddInstigator(
                actorInfo.OwnerActor,
                actorInfo.AvatarActor);

            OriginalInstigatorAbilitySystemComponent =
                actorInfo.AbilitySystemComponent;
        }

        /// <summary>
        /// Sets the immediate instigator and physical effect-causing actor.
        /// </summary>
        public virtual void AddInstigator(
            GameObject instigator,
            GameObject effectCauser)
        {
            Instigator =
                instigator;

            EffectCauser =
                effectCauser;

            if (OriginalInstigator == null)
            {
                OriginalInstigator =
                    instigator;
            }
        }

        /// <summary>
        /// Returns the immediate actor that instigated this gameplay effect.
        /// </summary>
        public virtual GameObject GetInstigator()
        {
            return Instigator;
        }

        /// <summary>
        /// Returns the original instigator that started the gameplay effect chain.
        /// </summary>
        public virtual GameObject GetOriginalInstigator()
        {
            return OriginalInstigator;
        }

        /// <summary>
        /// Returns the physical actor responsible for causing this gameplay effect.
        /// </summary>
        public virtual GameObject GetEffectCauser()
        {
            return EffectCauser;
        }

        /// <summary>
        /// Sets the gameplay ability that created this gameplay effect context.
        /// </summary>
        public virtual void SetAbility(
            GameplayAbility ability)
        {
            if (ability == null)
            {
                Ability =
                    null;

                AbilityInstanceNotReplicated =
                    null;

                AbilityLevel =
                    1;

                return;
            }

            if (ability.DefinitionAsset == null)
            {
                throw new InvalidOperationException(
                    "A gameplay effect context requires a persistent ability definition.");
            }

            Ability =
                ability.DefinitionAsset;

            AbilityInstanceNotReplicated =
                ability;

            AbilityLevel =
                ability.Level;
        }

        /// <summary>
        /// Returns the persistent definition of the ability that created this context.
        /// </summary>
        public GameplayAbilitySO GetAbility()
        {
            return Ability;
        }

        /// <summary>
        /// Returns the non-replicated runtime ability instance that created this context.
        /// </summary>
        public GameplayAbility GetAbilityInstance_NotReplicated()
        {
            return AbilityInstanceNotReplicated;
        }

        /// <summary>
        /// Returns the ability level captured when this context was initialized.
        /// </summary>
        public int GetAbilityLevel()
        {
            return AbilityLevel;
        }

        /// <summary>
        /// Sets the object that provided the gameplay effect source.
        /// </summary>
        public virtual void AddSourceObject(
            UnityEngine.Object sourceObject)
        {
            SourceObject =
                sourceObject;
        }

        /// <summary>
        /// Returns the object that provided the gameplay effect source.
        /// </summary>
        public virtual UnityEngine.Object GetSourceObject()
        {
            return SourceObject;
        }

        /// <summary>
        /// Returns the ability system component associated with the original effect instigator.
        /// </summary>
        public virtual AbilitySystemComponent
            GetOriginalInstigatorAbilitySystemComponent()
        {
            return OriginalInstigatorAbilitySystemComponent;
        }
    }
}