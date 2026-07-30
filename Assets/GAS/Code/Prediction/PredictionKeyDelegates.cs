#nullable enable

using System;
using System.Collections.Generic;

namespace GAS
{
    /// <summary>
    /// Dispatches one-shot lifecycle events for owner-scoped prediction keys.
    /// </summary>
    public sealed class PredictionKeyDelegates
    {
        private readonly Dictionary<
            PredictionKey,
            Action> m_RejectOrCaughtUpDelegates = new();

        private readonly Dictionary<
            PredictionKey,
            Action> m_RejectedDelegates = new();

        /// <summary>
        /// Registers a callback invoked when the prediction is rejected or caught up.
        /// </summary>
        public IDisposable RegisterRejectOrCaughtUpDelegate(
            PredictionKey predictionKey,
            Action handler)
        {
            if (!predictionKey.IsValid)
            {
                throw new ArgumentException(
                    "A prediction delegate requires a valid prediction key.",
                    nameof(predictionKey));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(
                    nameof(handler));
            }

            m_RejectOrCaughtUpDelegates.TryGetValue(
                predictionKey,
                out Action? handlers);

            m_RejectOrCaughtUpDelegates[predictionKey] =
                handlers +
                handler;

            return
                new DisposableSubscription(() =>
                    UnregisterRejectOrCaughtUpDelegate(
                        predictionKey,
                        handler));
        }

        /// <summary>
        /// Registers a callback invoked only when the prediction is rejected.
        /// </summary>
        public IDisposable RegisterRejectedDelegate(
            PredictionKey predictionKey,
            Action handler)
        {
            if (!predictionKey.IsValid)
            {
                throw new ArgumentException(
                    "A prediction delegate requires a valid prediction key.",
                    nameof(predictionKey));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(
                    nameof(handler));
            }

            m_RejectedDelegates.TryGetValue(
                predictionKey,
                out Action? handlers);

            m_RejectedDelegates[predictionKey] = handlers + handler;

            return new DisposableSubscription(() =>
                UnregisterRejectedDelegate(
                    predictionKey,
                    handler));
        }

        /// <summary>
        /// Rejects a prediction key and invokes all rejection and resolution callbacks.
        /// </summary>
        public void Reject(
            PredictionKey predictionKey)
        {
            ResolveRejected(
                predictionKey);

            Resolve(
                predictionKey);
        }

        /// <summary>
        /// Catches up a prediction key without invoking its rejection-only callbacks.
        /// </summary>
        public void CatchUpTo(
            PredictionKey predictionKey)
        {
            if (!predictionKey.IsValid)
            {
                throw new ArgumentException(
                    "Prediction resolution requires a valid prediction key.",
                    nameof(predictionKey));
            }

            m_RejectedDelegates.Remove(
                predictionKey);

            Resolve(
                predictionKey);
        }

        /// <summary>
        /// Resolves and removes callbacks registered exclusively for prediction rejection.
        /// </summary>
        private void ResolveRejected(
            PredictionKey predictionKey)
        {
            if (!predictionKey.IsValid)
            {
                throw new ArgumentException(
                    "Prediction resolution requires a valid prediction key.",
                    nameof(predictionKey));
            }

            if (!m_RejectedDelegates.TryGetValue(
                    predictionKey,
                    out Action handlers))
            {
                return;
            }

            m_RejectedDelegates.Remove(
                predictionKey);

            handlers.Invoke();
        }

        /// <summary>
        /// Resolves one prediction key and removes its callbacks before invocation.
        /// </summary>
        private void Resolve(
            PredictionKey predictionKey)
        {
            if (!predictionKey.IsValid)
            {
                throw new ArgumentException(
                    "Prediction resolution requires a valid prediction key.",
                    nameof(predictionKey));
            }

            if (
                !m_RejectOrCaughtUpDelegates.TryGetValue(
                    predictionKey,
                    out Action handlers))
            {
                return;
            }

            m_RejectOrCaughtUpDelegates.Remove(
                predictionKey);

            handlers.Invoke();
        }

        /// <summary>
        /// Removes one unresolved prediction callback.
        /// </summary>
        private void UnregisterRejectOrCaughtUpDelegate(
            PredictionKey predictionKey,
            Action handler)
        {
            if (
                !m_RejectOrCaughtUpDelegates.TryGetValue(
                    predictionKey,
                    out Action? handlers))
            {
                return;
            }

            handlers -=
                handler;

            if (handlers == null)
            {
                m_RejectOrCaughtUpDelegates.Remove(
                    predictionKey);

                return;
            }

            m_RejectOrCaughtUpDelegates[predictionKey] =
                handlers;
        }

        /// <summary>
        /// Removes one unresolved rejection-only prediction callback.
        /// </summary>
        private void UnregisterRejectedDelegate(
            PredictionKey predictionKey,
            Action handler)
        {
            if (!m_RejectedDelegates.TryGetValue(
                    predictionKey,
                    out Action? handlers))
            {
                return;
            }

            handlers -= handler;

            if (handlers == null)
            {
                m_RejectedDelegates.Remove(
                    predictionKey);

                return;
            }

            m_RejectedDelegates[predictionKey] =
                handlers;
        }
    }
}