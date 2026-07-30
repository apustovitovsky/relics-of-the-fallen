using System;

namespace GAS
{
    public abstract class AbilityTask
    {
        protected AbilitySystemComponent AbilitySystemComponent =>
            Ability.owner;

        public GameplayAbility Ability
        {
            get;
        }

        public bool IsActive
        {
            get;
            private set;
        }

        public bool IsEnded
        {
            get;
            private set;
        }

        protected AbilityTask(
            GameplayAbility ability)
        {
            Ability = ability ??
                throw new ArgumentNullException(
                    nameof(ability));
        }

        /// <summary>
        /// Activates this task after its callbacks and configuration have been prepared.
        /// </summary>
        public void ReadyForActivation()
        {
            if (IsEnded)
            {
                throw new InvalidOperationException(
                    "An ended ability task cannot be activated.");
            }

            if (IsActive)
            {
                return;
            }

            IsActive = true;

            Ability.OnGameplayTaskActivated(
                this);

            try
            {
                Activate();
            }
            catch
            {
                EndTask();

                throw;
            }
        }

        /// <summary>
        /// Starts the task-specific asynchronous operation.
        /// </summary>
        protected virtual void Activate()
        {
        }

        /// <summary>
        /// Cancels this task in response to an external request.
        /// </summary>
        public virtual void ExternalCancel()
        {
            EndTask();
        }

        /// <summary>
        /// Ends this task and performs its task-specific cleanup.
        /// </summary>
        public void EndTask()
        {
            DestroyTask(
                false);
        }

        /// <summary>
        /// Ends this task because its owning gameplay ability has ended.
        /// </summary>
        internal void TaskOwnerEnded()
        {
            DestroyTask(
                true);
        }

        /// <summary>
        /// Performs task-specific cleanup before the task becomes inactive.
        /// </summary>
        protected virtual void OnDestroy(
            bool abilityEnded)
        {
        }

        private void DestroyTask(
            bool abilityEnded)
        {
            if (IsEnded)
            {
                return;
            }

            IsActive = false;
            IsEnded = true;

            try
            {
                OnDestroy(
                    abilityEnded);
            }
            finally
            {
                Ability.OnGameplayTaskDeactivated(
                    this);
            }
        }
    }
}