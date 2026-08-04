using System;
using UnityEngine;

namespace GAS
{
    public abstract class GameplayAbilityTargetActor :
        MonoBehaviour
    {
        private event Action<GameplayAbilityTargetDataHandle>
            TargetDataReadyDelegate;

        private event Action<GameplayAbilityTargetDataHandle>
            CanceledDelegate;

        public GameplayAbility OwningAbility
        {
            get;
            private set;
        }

        /// <summary>
        /// Registers a callback invoked whenever this target actor produces confirmed target data.
        /// </summary>
        internal IDisposable RegisterTargetDataReadyDelegate(
            Action<GameplayAbilityTargetDataHandle> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(
                    nameof(handler));
            }

            TargetDataReadyDelegate +=
                handler;

            return new DisposableSubscription(() =>
                TargetDataReadyDelegate -=
                    handler);
        }

        /// <summary>
        /// Registers a callback invoked whenever targeting is cancelled.
        /// </summary>
        internal IDisposable RegisterCanceledDelegate(
            Action<GameplayAbilityTargetDataHandle> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(
                    nameof(handler));
            }

            CanceledDelegate +=
                handler;

            return new DisposableSubscription(() =>
                CanceledDelegate -=
                    handler);
        }

        /// <summary>
        /// Associates this target actor with the ability that initiated targeting.
        /// </summary>
        public virtual void StartTargeting(
            GameplayAbility ability)
        {
            OwningAbility =
                ability ??
                throw new ArgumentNullException(
                    nameof(ability));
        }

        /// <summary>
        /// Confirms targeting and produces the resulting target data.
        /// </summary>
        public abstract void ConfirmTargetingAndContinue();

        /// <summary>
        /// Cancels targeting and notifies the waiting ability task.
        /// </summary>
        public virtual void CancelTargeting()
        {
            BroadcastTargetDataCancelled(
                new GameplayAbilityTargetDataHandle());
        }

        /// <summary>
        /// Broadcasts confirmed target data to the waiting ability task.
        /// </summary>
        protected void BroadcastTargetDataReady(
            GameplayAbilityTargetDataHandle targetData)
        {
            if (targetData == null)
            {
                throw new ArgumentNullException(
                    nameof(targetData));
            }

            TargetDataReadyDelegate?.Invoke(
                targetData);
        }

        /// <summary>
        /// Broadcasts targeting cancellation to the waiting ability task.
        /// </summary>
        protected void BroadcastTargetDataCancelled(
            GameplayAbilityTargetDataHandle targetData)
        {
            if (targetData == null)
            {
                throw new ArgumentNullException(
                    nameof(targetData));
            }

            CanceledDelegate?.Invoke(
                targetData);
        }
    }
}