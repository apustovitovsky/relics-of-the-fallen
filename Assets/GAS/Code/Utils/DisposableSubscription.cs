#nullable enable

using System;

namespace GAS
{
    internal sealed class DisposableSubscription : IDisposable
    {
        private Action? m_Unsubscribe;

        public bool IsDisposed => m_Unsubscribe == null;

        /// <summary>
        /// Creates an idempotent subscription that invokes the supplied cleanup action once.
        /// </summary>
        public DisposableSubscription(
            Action unsubscribe)
        {
            m_Unsubscribe =
                unsubscribe ??
                throw new ArgumentNullException(
                    nameof(unsubscribe));
        }

        /// <summary>
        /// Removes the associated event handler once.
        /// </summary>
        public void Dispose()
        {
            Action? unsubscribeAction =
                m_Unsubscribe;

            m_Unsubscribe = null;

            unsubscribeAction?.Invoke();
        }
    }
}