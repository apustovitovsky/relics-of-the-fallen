using System;
using UnityEngine;

namespace GAS
{
    public class GameplayAbilityActorInfo
    {
        public GameObject OwnerActor
        {
            get;
            private set;
        }

        public GameObject AvatarActor
        {
            get;
            private set;
        }

        public AbilitySystemComponent AbilitySystemComponent
        {
            get;
            private set;
        }

        public AnimInstance AnimInstance
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes cached ability data from the logical owner and physical avatar.
        /// </summary>
        public virtual void InitFromActor(
            GameObject ownerActor,
            GameObject avatarActor,
            AbilitySystemComponent abilitySystemComponent)
        {
            if (ownerActor == null)
            {
                throw new ArgumentNullException(
                    nameof(ownerActor));
            }

            if (avatarActor == null)
            {
                throw new ArgumentNullException(
                    nameof(avatarActor));
            }

            if (abilitySystemComponent == null)
            {
                throw new ArgumentNullException(
                    nameof(abilitySystemComponent));
            }

            OwnerActor = ownerActor;
            AvatarActor = avatarActor;
            AbilitySystemComponent = abilitySystemComponent;
            AnimInstance = avatarActor.GetComponentInChildren<AnimInstance>();
        }
    }
}