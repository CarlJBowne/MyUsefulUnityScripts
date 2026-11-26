using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using UnityEngine.Events;
#if ULT_EVENTS
using UltEvents;
#endif
#if INPUT_SYSTEM
using UnityEngine.InputSystem;
using CTX = UnityEngine.InputSystem.InputAction.CallbackContext;
#endif

namespace SLS.GeneralUtilities.Events
{
    /// <summary>
    /// Represents a subscription ticket that can be placed in a list and used to subscribe and unsubscribe subscribers from various publishers in an easy and consistent manner.
    /// </summary>
    public class EventTicket
    {
        /// <summary>
        /// Indicates whether this ticket is currently subscribed.
        /// </summary>
        /// <remarks> This value cannot be trusted in situations where the Publishing event has, through any other means, been cleared. </remarks>
        public bool Subscribed { get; protected set; }

        /// <summary>
        /// The adder function.
        /// </summary>
        readonly Action _adder;
        /// <summary>
        /// The remover function.
        /// </summary>
        readonly Action _remover;

        /// <summary>
        /// Subscribes the subscriber to the publisher if not already subscribed.
        /// </summary>
        public virtual void Subscribe()
        {
            if (Subscribed) return;
            _adder();
            Subscribed = true;
        }

        /// <summary>
        /// Unsubscribes the subscriber from the publisher if currently subscribed.
        /// </summary>
        public virtual void UnSubscribe()
        {
            if (!Subscribed) return;
            _remover();
            Subscribed = false;
        }

        /// <summary>
        /// Creates an <see cref="EventTicket"/> with an adder and remover action.
        /// </summary>
        /// <param name="adder">Publisher delegate captured for add/remove operations.</param>
        /// <param name="remover">Subscriber to add or remove from the publisher.</param>
        /// <param name="now">Whether to subscribe immediately.</param>
        public EventTicket(Action adder, Action remover, bool now = true)
        {
            _adder = adder;
            _remover = remover;

            if (now) Subscribe();
        }

        public static EventTicket New(Action adder, Action remover, bool now = true) => new(adder, remover, now);
        public static EventTicket New<T>(T handler, Action<T> adder, Action<T> remover, bool now = true) where T : System.Delegate 
            => new(() => adder(handler), () => remover(handler), now);

        public static void Set(ref EventTicket slot, EventTicket New)
        {
            if (slot != null) return;
            slot = New;
        }
        public static void Destroy(ref EventTicket slot)
        {
            if (slot == null) return;
            slot.UnSubscribe();
            slot = null;
        }
    }

    public static class Xtensions_EventTickets
    {
        #region Helpers
        /// <summary> Subscribe all Event Tickets in this list to their target publisher. </summary>
        public static void SubscribeAll(this List<EventTicket> list)
        {
            if (list == null) return;
            for (int i = 0; i < list.Count; i++) list[i].Subscribe();
        }
        /// <summary> UnSubscribes all Event Tickets in this list from their target publisher </summary>
        public static void UnSubscribeAll(this List<EventTicket> list)
        {
            if (list == null) return;
            for (int i = 0; i < list.Count; i++) list[i].UnSubscribe();
        }
        /// <summary> Unsubscribes all Event Tickets in this list from their target publisher and then clears the list. </summary>
        public static void DestroyAll(this List<EventTicket> list)
        {
            if (list == null) return;
            for (int i = 0; i < list.Count; i++) list[i].UnSubscribe();
            list.Clear();
        }

        #endregion

        #region UnityEvents

        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a parameterless function to this <see cref="UnityAction"/>. 
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="subscribeNow">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket Subscribe(this UnityEvent unityEvent, UnityAction subscriber, bool subscribeNow = true)
            => new(() => unityEvent.AddListener(subscriber), () => unityEvent.RemoveListener(subscriber), subscribeNow);
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function with 1 parameter to this <see cref="UnityAction{T0}"/>. 
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="subscribeNow">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket Subscribe<T>(this UnityEvent<T> unityEvent, UnityAction<T> subscriber, bool subscribeNow = true)
            => new(() => unityEvent.AddListener(subscriber), () => unityEvent.RemoveListener(subscriber), subscribeNow);

        #endregion

#if ULT_EVENTS

        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a parameterless function to this <see cref="UltEvent"/>. 
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="now">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket Subscribe(this UltEvent ultEvent, Action subscriber, bool now = true) => new(() => ultEvent.DynamicCalls += subscriber, () => ultEvent.DynamicCalls -= subscriber, now);
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function with 1 parameter to this <see cref="UltEvent{T0}"/>. 
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="now">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket Subscribe<T>(this UltEvent<T> ultEvent, Action<T> subscriber, bool now = true) => new(() => ultEvent.DynamicCalls += subscriber, () => ultEvent.DynamicCalls -= subscriber, now);
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function with 2 parameters to this <see cref="UltEvent{T0, T1}"/>. 
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="now">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket Subscribe<T, T1>(this UltEvent<T, T1> ultEvent, Action<T, T1> subscriber, bool now = true) => new(() => ultEvent.DynamicCalls += subscriber, () => ultEvent.DynamicCalls -= subscriber, now);
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function with 3 parameters to this <see cref="UltEvent{T0, T1, T2}"/>. 
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="now">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket Subscribe<T, T1, T2>(this UltEvent<T, T1, T2> ultEvent, Action<T, T1, T2> subscriber, bool now = true) => new(() => ultEvent.DynamicCalls += subscriber, () => ultEvent.DynamicCalls -= subscriber, now);
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function with 4 parameters to this <see cref="UltEvent{T0, T1, T2}"/>. 
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="now">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket Subscribe<T, T1, T2, T3>(this UltEvent<T, T1, T2, T3> ultEvent, Action<T, T1, T2, T3> subscriber, bool now = true) => new(() => ultEvent.DynamicCalls += subscriber, () => ultEvent.DynamicCalls -= subscriber, now);

#endif

#if INPUT_SYSTEM

        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function to this Action's <see cref="InputAction.performed"/> event. 
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="now">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket SubscribePerformed(this InputAction a, Action<CTX> subscriber, bool now = true)
            => new(() => a.performed += subscriber, () => a.performed -= subscriber, now);
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function to this Action's <see cref="InputAction.performed"/> event. (Drops the Callback Context)
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="now">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket SubscribePerformed(this InputAction a, Action subscriber, bool now = true)
        {
            void truesubscriber(CTX _) => subscriber?.Invoke();
            return new(() => a.performed += truesubscriber, () => a.performed -= truesubscriber, now);
        }

        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function to this Action's <see cref="InputAction.started"/> event. 
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="now">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket SubscribeStarted(this InputAction a, Action<CTX> subscriber, bool now = true)
            => new(() => a.performed += subscriber, () => a.performed -= subscriber, now);
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function to this Action's <see cref="InputAction.started"/> event. (Drops the Callback Context)
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="now">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket SubscribeStarted(this InputAction a, Action subscriber, bool now = true)
        {
            void truesubscriber(CTX _) => subscriber?.Invoke();
            return new(() => a.started += truesubscriber, () => a.started -= truesubscriber, now);
        }

        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function to this Action's <see cref="InputAction.canceled"/> event. 
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="now">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket SubscribeCancelled(this InputAction a, Action<CTX> subscriber, bool now = true)
            => new(() => a.performed += subscriber, () => a.performed -= subscriber, now);
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function to this Action's <see cref="InputAction.canceled"/> event. (Drops the Callback Context)
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="now">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket SubscribeCancelled(this InputAction a, Action subscriber, bool now = true)
        {
            void truesubscriber(CTX _) => subscriber?.Invoke();
            return new(() => a.canceled += truesubscriber, () => a.canceled -= truesubscriber, now);
        }

        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing one function to this Action's <see cref="InputAction.started"/> event and another to this Action's <see cref="InputAction.canceled"/> event. 
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="now">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket SubscribeBoth(this InputAction a, Action<CTX> subscriberPress, Action<CTX> subscriberRelease, bool now = true) => new(() => { a.started += subscriberPress; a.canceled += subscriberRelease; }, 
              () => { a.started -= subscriberPress; a.canceled -= subscriberRelease; }, now);
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing one function to this Action's <see cref="InputAction.started"/> event and another to this Action's <see cref="InputAction.canceled"/> event. (Drops the Callback Context)
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="now">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket SubscribeBoth(this InputAction a, Action subscriberPress, Action subscriberRelease, bool now = true)
        {
            void hp(CTX _) => subscriberPress?.Invoke();
            void hr(CTX _) => subscriberRelease?.Invoke();

            return new (() => { a.started += hp; a.canceled += hr; },
                        () => { a.started -= hp; a.canceled -= hr; }, now );
        }

#endif
    }
}
