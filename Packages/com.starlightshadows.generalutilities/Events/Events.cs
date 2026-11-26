using System;
using System.Collections.Generic;
using System.Text;

namespace SLS.GeneralUtilities.Events
{
    public class Event
    {
        public event Action Listeners;

        public void Invoke() => Listeners?.Invoke();

        public EventTicket Subscribe(Action listener, bool subscribeNow = true) => EventTicket.New(listener, Add, Remove, subscribeNow);

        public void Add(Action listener) => Listeners += listener;
        public void Remove(Action listener) => Listeners -= listener;
        public System.Delegate[] GetInvocationList() => Listeners.GetInvocationList();
    }
    public class Event<T>
    {
        public event Action<T> Listeners;

        public void Invoke(T v) => Listeners?.Invoke(v);

        public EventTicket Subscribe(Action<T> listener, bool subscribeNow = true) => EventTicket.New(listener, Add, Remove, subscribeNow);

        public void Add(Action<T> listener) => Listeners += listener;
        public void Remove(Action<T> listener) => Listeners -= listener;
        public System.Delegate[] GetInvocationList() => Listeners.GetInvocationList();
    }
    public class Event<A, B>
    {
        public event Action<A, B> Listeners;

        public void Invoke(A a, B b) => Listeners?.Invoke(a, b);

        public EventTicket Subscribe(Action<A, B> listener, bool subscribeNow = true) => EventTicket.New(listener, Add, Remove, subscribeNow);

        public void Add(Action<A, B> listener) => Listeners += listener;
        public void Remove(Action<A, B> listener) => Listeners -= listener;
        public System.Delegate[] GetInvocationList() => Listeners.GetInvocationList();
    }
    public class Event<A, B, C>
    {
        public event Action<A, B, C> Listeners;

        public void Invoke(A a, B b, C c) => Listeners?.Invoke(a, b, c);

        public EventTicket Subscribe(Action<A, B, C> listener, bool subscribeNow = true) => EventTicket.New(listener, Add, Remove, subscribeNow);

        public void Add(Action<A, B, C> listener) => Listeners += listener;
        public void Remove(Action<A, B, C> listener) => Listeners -= listener;
        public System.Delegate[] GetInvocationList() => Listeners.GetInvocationList();
    }
    public class Event<A, B, C, D>
    {
        public event Action<A, B, C, D> Listeners;

        public void Invoke(A a, B b, C c, D d) => Listeners?.Invoke(a, b, c, d);

        public EventTicket Subscribe(Action<A, B, C, D> listener, bool subscribeNow = true) => EventTicket.New(listener, Add, Remove, subscribeNow);

        public void Add(Action<A, B, C, D> listener) => Listeners += listener;
        public void Remove(Action<A, B, C, D> listener) => Listeners -= listener;
        public System.Delegate[] GetInvocationList() => Listeners.GetInvocationList();
    }
    public class Event<A, B, C, D, E>
    {
        public event Action<A, B, C, D, E> Listeners;

        public void Invoke(A a, B b, C c, D d, E e) => Listeners?.Invoke(a, b, c, d, e);

        public EventTicket Subscribe(Action<A, B, C, D, E> listener, bool subscribeNow = true) => EventTicket.New(listener, Add, Remove, subscribeNow);

        public void Add(Action<A, B, C, D, E> listener) => Listeners += listener;
        public void Remove(Action<A, B, C, D, E> listener) => Listeners -= listener;
        public System.Delegate[] GetInvocationList() => Listeners.GetInvocationList();
    }
}
