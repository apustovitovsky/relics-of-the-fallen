using System;

namespace GAS
{
    internal abstract class DisposableEventBase<TDelegate>
    where TDelegate : Delegate
    {
        private TDelegate m_Handlers;
        private uint m_Version;

        protected TDelegate Handlers =>
            m_Handlers;

        protected IDisposable SubscribeHandler(
            TDelegate handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            m_Handlers = (TDelegate)Delegate.Combine(
                m_Handlers,
                handler);

            uint subscriptionVersion = m_Version;

            return new DisposableSubscription(() =>
                RemoveHandler(
                    handler,
                    subscriptionVersion));
        }

        protected void ClearHandlers()
        {
            m_Handlers = null;
            m_Version++;
        }

        private void RemoveHandler(
            TDelegate handler,
            uint subscriptionVersion)
        {
            if (subscriptionVersion != m_Version)
            {
                return;
            }

            m_Handlers = (TDelegate)Delegate.Remove(
                m_Handlers,
                handler);
        }
    }

    internal sealed class DisposableEvent :
        DisposableEventBase<Action>
    {
        /// <summary>
        /// Registers a handler and returns an idempotent subscription for its lifetime.
        /// </summary>
        public IDisposable Subscribe(
            Action handler)
        {
            return SubscribeHandler(handler);
        }

        public void Invoke()
        {
            Action handlers = Handlers;

            handlers?.Invoke();
        }

        public void Clear()
        {
            ClearHandlers();
        }
    }

    internal sealed class DisposableEvent<T> :
        DisposableEventBase<Action<T>>
    {
        /// <summary>
        /// Registers a handler and returns an idempotent subscription for its lifetime.
        /// </summary>
        public IDisposable Subscribe(
            Action<T> handler)
        {
            return SubscribeHandler(handler);
        }

        public void Invoke(
            T value)
        {
            Action<T> handlers = Handlers;

            handlers?.Invoke(value);
        }

        public void Clear()
        {
            ClearHandlers();
        }
    }

    internal sealed class DisposableEvent<T1, T2> :
        DisposableEventBase<Action<T1, T2>>
    {
        /// <summary>
        /// Registers a handler and returns an idempotent subscription for its lifetime.
        /// </summary>
        public IDisposable Subscribe(
            Action<T1, T2> handler)
        {
            return SubscribeHandler(handler);
        }

        public void Invoke(
            T1 value1,
            T2 value2)
        {
            Action<T1, T2> handlers = Handlers;

            handlers?.Invoke(
                value1,
                value2);
        }

        public void Clear()
        {
            ClearHandlers();
        }
    }

    internal sealed class DisposableEvent<T1, T2, T3> :
    DisposableEventBase<Action<T1, T2, T3>>
    {
        /// <summary>
        /// Registers a handler and returns an idempotent subscription for its lifetime.
        /// </summary>
        public IDisposable Subscribe(
            Action<T1, T2, T3> handler)
        {
            return SubscribeHandler(handler);
        }

        public void Invoke(
            T1 value1,
            T2 value2,
            T3 value3)
        {
            Action<T1, T2, T3> handlers = Handlers;

            handlers?.Invoke(
                value1,
                value2,
                value3);
        }

        public void Clear()
        {
            ClearHandlers();
        }
    }
}