#nullable enable

using System;
using System.Collections.Generic;

namespace GAS
{
    internal sealed class DisposableGroup : IDisposable
    {
        private readonly List<IDisposable>
            m_Subscriptions = new();

        private bool m_IsDisposed;

        public bool IsDisposed => m_IsDisposed;

        /// <summary>
        /// Adds a disposable resource to this composite lifetime.
        /// </summary>
        public void Add(
            IDisposable subscription)
        {
            if (subscription == null)
            {
                throw new ArgumentNullException(
                    nameof(subscription));
            }

            if (m_IsDisposed)
            {
                subscription.Dispose();

                return;
            }

            m_Subscriptions.Add(
                subscription);
        }

        /// <summary>
        /// Disposes every owned resource once in reverse registration order.
        /// </summary>
        public void Dispose()
        {
            if (m_IsDisposed)
            {
                return;
            }

            m_IsDisposed =
                true;

            for (
                int index =
                    m_Subscriptions.Count - 1;
                index >= 0;
                index--)
            {
                m_Subscriptions[index]
                    .Dispose();
            }

            m_Subscriptions.Clear();
        }
    }
}