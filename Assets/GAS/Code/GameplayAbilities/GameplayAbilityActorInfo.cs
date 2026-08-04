using System;
using UnityEngine;

namespace GAS
{
    public class GameplayAbilityActorInfo
    {
        private bool m_IsNetAuthority = true;
        private bool m_IsLocallyControlled = true;

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
        /// Returns whether the owning actor has authoritative execution control.
        /// </summary>
        public bool IsNetAuthority()
        {
            return m_IsNetAuthority;
        }

        /// <summary>
        /// Returns whether this ability actor is controlled by the local process.
        /// </summary>
        public bool IsLocallyControlled()
        {
            return m_IsLocallyControlled;
        }

        /// <summary>
        /// Updates the local-control state supplied by the platform adapter.
        /// </summary>
        public void SetLocallyControlled(
            bool isLocallyControlled)
        {
            m_IsLocallyControlled =
                isLocallyControlled;
        }

        /// <summary>
        /// Updates the authoritative execution state supplied by the platform adapter.
        /// </summary>
        public void SetNetAuthority(
            bool isNetAuthority)
        {
            m_IsNetAuthority =
                isNetAuthority;
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