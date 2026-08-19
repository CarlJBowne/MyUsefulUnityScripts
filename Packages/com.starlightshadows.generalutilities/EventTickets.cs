using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using UltEvents;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using CTX = UnityEngine.InputSystem.InputAction.CallbackContext;

namespace SLS.GeneralUtilities.EventTickets
{
    /// <summary>
    /// Represents a subscription ticket that can be placed in a list and used to subscribe and unsubscribe subscribers from various publishers in an easy & consistent manner.
    /// <br/> This class specifically contains all the static helper methods for creating EventTickets from various types of events. Though Extension methods are also provided for most types found on the publishing event.
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
        /// Protected parameterless constructor used by derived types.
        /// </summary>
        protected EventTicket() { }

        /// <summary>
        /// Creates an <see cref="EventTicket"/> for simple parameterless <see cref="Action"/> publishers.
        /// </summary>
        /// <param name="publisher">Publisher delegate captured for add/remove operations.</param>
        /// <param name="subscriber">Subscriber to add or remove from the publisher.</param>
        /// <param name="subscribeNow">Whether to subscribe immediately.</param>
        public EventTicket(Action publisher, Action subscriber, bool subscribeNow = true)
        {
            _adder = () => publisher += subscriber;
            _remover = () => publisher -= subscriber;

            if (subscribeNow) Subscribe();
        }

        /// <summary>
        /// Factory for creating a basic <see cref="EventTicket"/> for parameterless actions.
        /// </summary>
        public static EventTicket Action(Action publisher, Action subscriber, bool subscribeNow = true) => new(publisher, subscriber, subscribeNow);

        /// <summary>
        /// Factory for creating a basic <see cref="EventTicket"/> for actions with 1 parameter.
        /// </summary>
        public static EventTicket Action<T>(Action<T> publisher, Action<T> subscriber, bool subscribeNow = true) => new EventTicket<Action<T>>(h => publisher += h, h => publisher -= h, subscriber, subscribeNow);

        /// <summary>
        /// Factory for creating a basic <see cref="EventTicket"/> for actions with 2 parameters.
        /// </summary>
        public static EventTicket Action<T, T1>(Action<T, T1> publisher, Action<T, T1> subscriber, bool subscribeNow = true) => new EventTicket<Action<T, T1>>(h => publisher += h, h => publisher -= h, subscriber, subscribeNow);

        /// <summary>
        /// Factory for creating a basic <see cref="EventTicket"/> for actions with 3 parameters.
        /// </summary>
        public static EventTicket Action<T, T1, T2>(Action<T, T1, T2> publisher, Action<T, T1, T2> subscriber, bool subscribeNow = true) => new EventTicket<Action<T, T1, T2>>(h => publisher += h, h => publisher -= h, subscriber, subscribeNow);

        /// <summary>
        /// Creates a manual typed <see cref="EventTicket{T}"/> when custom add/remove actions are required.
        /// </summary>
        public static EventTicket<T> Manual<T>(Action<T> adder, Action<T> remover, T subscriber, bool subscribeNow = true) where T : System.Delegate => new(adder, remover, subscriber, subscribeNow);

        /// <summary>
        /// Creates an <see cref="EventTicket{UnityAction}"/> from a <see cref="UnityEvent"/>.
        /// </summary>
        public static EventTicket<UnityAction> Unity(UnityEvent unityEvent, UnityAction subscriber, bool subscribeNow = true) => new(h => unityEvent.AddListener(h), h => unityEvent.RemoveListener(h), subscriber, subscribeNow);

        /// <summary>
        /// Creates an <see cref="EventTicket{UnityAction{T}}"/> from a generic <see cref="UnityEvent{T}"/>.
        /// </summary>
        public static EventTicket<UnityAction<T>> Unity<T>(UnityEvent<T> unityEvent, UnityAction<T> subscriber, bool subscribeNow = true) where T : System.Delegate => new(h => unityEvent.AddListener(h), h => unityEvent.RemoveListener(h), subscriber, subscribeNow);

        /// <summary>
        /// Creates an <see cref="EventTicket"/> for an <see cref="UltEvent"/>.
        /// </summary>
        public static EventTicket Ult(UltEvent ultEvent, Action subscriber, bool subscribeNow = true) => new(() => ultEvent.AddPersistentCall(subscriber), () => ultEvent.RemovePersistentCall(subscriber), subscribeNow);

        /// <summary>
        /// Creates an <see cref="EventTicket"/> for a generic <see cref="UltEvent{T}"/>.
        /// </summary>
        public static EventTicket Ult<T>(UltEvent<T> ultEvent, Action<T> subscriber, bool subscribeNow = true) => new(() => ultEvent.AddPersistentCall(subscriber), () => ultEvent.RemovePersistentCall(subscriber), subscribeNow);

        /// <summary>
        /// Creates an <see cref="EventTicket"/> for a generic <see cref="UltEvent{T,T1}"/>.
        /// </summary>
        public static EventTicket Ult<T, T1>(UltEvent<T, T1> ultEvent, Action<T, T1> subscriber, bool subscribeNow = true) => new(() => ultEvent.AddPersistentCall(subscriber), () => ultEvent.RemovePersistentCall(subscriber), subscribeNow);

        /// <summary>
        /// Creates an <see cref="EventTicket"/> for a generic <see cref="UltEvent{T,T1,T2}"/>.
        /// </summary>
        public static EventTicket Ult<T, T1, T2>(UltEvent<T, T1, T2> ultEvent, Action<T, T1, T2> subscriber, bool subscribeNow = true) => new(() => ultEvent.AddPersistentCall(subscriber), () => ultEvent.RemovePersistentCall(subscriber), subscribeNow);
    }

    /// <summary>
    /// Represents a subscription ticket that can be placed in a list and used to subscribe and unsubscribe subscribers from various publishers in an easy and consistent manner. <br/>
    /// Passes a unique subscriber into adder and remover actions.
    /// </summary>
    public class EventTicket<T> : EventTicket where T : System.Delegate
    {
        /// <summary>
        /// Unique adder function, passes the stored subscriber into its method.
        /// </summary>
        readonly Action<T> _adder;
        /// <summary>
        /// Unique remover function, passes the stored subscriber into its method.
        /// </summary>
        readonly Action<T> _remover;
        /// <summary>
        /// The subscriber method.
        /// </summary>
        public readonly T _subscriber;

        public override void Subscribe()
        {
            if (Subscribed) return;
            _adder(_subscriber);
            Subscribed = true;
        }

        public override void UnSubscribe()
        {
            if (!Subscribed) return;
            _remover(_subscriber);
            Subscribed = false;
        }

        // Optional: expose the subscriber if you need it
        public T subscriber => _subscriber;

        /// <summary>
        /// Manually creates an <see cref="EventTicket{T}"/> via an adder method, a remover method, and a subscriber.
        /// </summary>
        /// <param name="adder"> The adder function. Provides the subscriber as <see cref="T"/></param>
        /// <param name="remover">The remover function. Provides the subscriber as <see cref="T"/></param>
        /// <param name="subscriber"> The subscriber (resulting method) to be called when the event is raised.</param>
        /// <param name="subscribeNow">Whether you want to immediately subscribe the subscriber to the publisher. (True by Default)</param>
        public EventTicket(Action<T> adder, Action<T> remover, T subscriber, bool subscribeNow = true)
        {
            _adder = adder ?? throw new ArgumentNullException(nameof(adder));
            _remover = remover ?? throw new ArgumentNullException(nameof(remover));
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));

            if (subscribeNow) Subscribe();
        }
    }
    /// <summary>
    /// Represents a subscription ticket that can be placed in a list and used to subscribe and unsubscribe subscribers from various publishers in an easy and consistent manner. <br/>
    /// Passes two unique subscribers into adder and remover actions. Generally used for Press/Release or Begin/End like event pairs.
    /// </summary>
    public class EventTicketDual<T> : EventTicket where T : System.Delegate
    {
        /// <summary>
        /// Unique Adder method. Passes both subscribers into its method.
        /// </summary>
        readonly Action<T, T> _adder;
        /// <summary>
        /// Unique Remover method. Passes both subscribers into its method.
        /// </summary>
        readonly Action<T, T> _remover;
        /// <summary>
        /// The first Subscriber method handled by this ticket. Generally used for Begin/Press events.
        /// </summary>
        readonly T _subscriber1;
        /// <summary>
        /// The second Subscriber method handled by this ticket. Generally used for End/Release events.
        /// </summary>
        readonly T _subscriber2;

        public override void Subscribe()
        {
            if (Subscribed) return;
            _adder(_subscriber1, _subscriber2);
            Subscribed = true;
        }

        public override void UnSubscribe()
        {
            if (!Subscribed) return;
            _remover(_subscriber1, _subscriber2);
            Subscribed = false;
        }

        // Optional: expose the subscriber if you need it
        public T subscriber1 => _subscriber1;
        public T subscriber2 => _subscriber2;

        /// <summary>
        /// Manually creates an <see cref="EventTicket{T}"/> via an adder method, a remover method, and a subscriber.
        /// </summary>
        /// <param name="adder"> The adder function. Provides both subscribers as <see cref="T"/></param>
        /// <param name="remover">The remover function. Provides both subscribers as <see cref="T"/></param>
        /// <param name="subscriber1"> The 1st subscriber (resulting method) to be called when the event is raised.</param>
        /// <param name="subscriber2"> The 2nd subscriber (resulting method) to be called when the event is raised.</param>
        /// <param name="subscribeNow">Whether you want to immediately subscribe the subscriber to the publisher. (True by Default)</param>
        public EventTicketDual(Action<T, T> adder, Action<T, T> remover, T subscriber1, T subscriber2, bool subscribeNow = true)
        {
            _adder = adder ?? throw new ArgumentNullException(nameof(adder));
            _remover = remover ?? throw new ArgumentNullException(nameof(remover));
            _subscriber1 = subscriber1 ?? throw new ArgumentNullException(nameof(subscriber1));
            _subscriber2 = subscriber2 ?? throw new ArgumentNullException(nameof(subscriber2));

            if (subscribeNow) Subscribe();
        }
    }

    public static class Xtensions_EventTickets
    {
        /// <summary> Subscribe all Event Tickets in this list to their target publisher. </summary>
        public static void SubscribeAll(this List<EventTicket> list)
        { for (int i = 0; i < list.Count; i++) list[i].Subscribe(); }
        /// <summary> UnSubscribes all Event Tickets in this list from their target publisher </summary>
        public static void UnSubscribeAll(this List<EventTicket> list)
        { for (int i = 0; i < list.Count; i++) list[i].UnSubscribe(); }
        /// <summary> Unsubscribes all Event Tickets in this list from their target publisher and then clears the list. </summary>
        public static void DestroyAll(this List<EventTicket> list)
        {
            for (int i = 0; i < list.Count; i++) list[i].UnSubscribe();
            list.Clear();
        }

        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a parameterless function to this <see cref="Action"/>.
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="subscribeNow">Whether this should immediately subscribe.</param>
        /// <returns> The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket Subscribe(this Action publisher, Action subscriber, bool subscribeNow = true) => new(publisher, subscriber, subscribeNow);
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function with 1 parameter to this <see cref="Action{T}"/>.
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="subscribeNow">Whether this should immediately subscribe.</param>
        /// <returns> The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket Subscribe<T>(this Action<T> publisher, Action<T> subscriber, bool subscribeNow = true) => new EventTicket<Action<T>>(h => publisher += h, h => publisher -= h, subscriber, subscribeNow);
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function with 2 parameters to this <see cref="Action{T1, T2}"/>.
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="subscribeNow">Whether this should immediately subscribe.</param>
        /// <returns> The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket Subscribe<T, T1>(this Action<T, T1> publisher, Action<T, T1> subscriber, bool subscribeNow = true) => new EventTicket<Action<T, T1>>(h => publisher += h, h => publisher -= h, subscriber, subscribeNow);
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function with 3 parameter to this <see cref="Action{T1, T2, T3}"/>.
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="subscribeNow">Whether this should immediately subscribe.</param>
        /// <returns> The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket Subscribe<T, T1, T2>(this Action<T, T1, T2> publisher, Action<T, T1, T2> subscriber, bool subscribeNow = true) => new EventTicket<Action<T, T1, T2>>(h => publisher += h, h => publisher -= h, subscriber, subscribeNow);

        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing this function to an event via an adder and remover. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="adder">The subscription function, generally involving a += operation</param>
        /// <param name="remover">The unsubscription function, generally involving a -= operation</param>
        /// <param name="subscribeNow">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        /// <remarks>May totally not work. Idk how Method groups or anonymous methods work with Extension Methods if at all.</remarks>
        public static EventTicket<T> SubscribeTo<T>(this T subscriber, Action<T> adder, Action<T> remover, bool subscribeNow = true) where T : System.Delegate => new(adder, remover, subscriber, subscribeNow);

        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a parameterless function to this <see cref="UnityAction"/>. 
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="subscribeNow">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket<UnityAction> Subscribe(this UnityEvent unityEvent, UnityAction subscriber, bool subscribeNow = true) => new(h => unityEvent.AddListener(h), h => unityEvent.RemoveListener(h), subscriber, subscribeNow);
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function with 1 parameter to this <see cref="UnityAction{T0}"/>. 
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="subscribeNow">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket<UnityAction<T>> Subscribe<T>(this UnityEvent<T> unityEvent, UnityAction<T> subscriber, bool subscribeNow = true) where T : System.Delegate => new(h => unityEvent.AddListener(h), h => unityEvent.RemoveListener(h), subscriber, subscribeNow);

        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a parameterless function to this <see cref="UltEvent"/>. 
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="subscribeNow">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket Subscribe(this UltEvent ultEvent, Action subscriber, bool subscribeNow = true) => new(() => ultEvent.AddPersistentCall(subscriber), () => ultEvent.RemovePersistentCall(subscriber), subscribeNow);
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function with 1 parameter to this <see cref="UltEvent{T0}"/>. 
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="subscribeNow">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket Subscribe<T>(this UltEvent<T> ultEvent, Action<T> subscriber, bool subscribeNow = true) => new(() => ultEvent.AddPersistentCall(subscriber), () => ultEvent.RemovePersistentCall(subscriber), subscribeNow);
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function with 2 parameters to this <see cref="UltEvent{T0, T1}"/>. 
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="subscribeNow">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket Subscribe<T, T1>(this UltEvent<T, T1> ultEvent, Action<T, T1> subscriber, bool subscribeNow = true) => new(() => ultEvent.AddPersistentCall(subscriber), () => ultEvent.RemovePersistentCall(subscriber), subscribeNow);
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function with 3 parameters to this <see cref="UltEvent{T0, T1, T2}"/>. 
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="subscribeNow">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket Subscribe<T, T1, T2>(this UltEvent<T, T1, T2> ultEvent, Action<T, T1, T2> subscriber, bool subscribeNow = true) => new(() => ultEvent.AddPersistentCall(subscriber), () => ultEvent.RemovePersistentCall(subscriber), subscribeNow);

        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function to this Action's <see cref="InputAction.performed"/> event. 
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="subscribeNow">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket SubscribePerformed(this InputAction a, Action<CTX> subscriber, bool subscribeNow = true) => new EventTicket<Action<CTX>>(h => a.performed += h, h => a.performed -= h, subscriber, subscribeNow);
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function to this Action's <see cref="InputAction.performed"/> event. (Drops the Callback Context)
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="subscribeNow">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket SubscribePerformed(this InputAction a, Action subscriber, bool subscribeNow = true)
        {
            Action<CTX> truesubscriber = _ => subscriber?.Invoke();
            return new EventTicket<Action<CTX>>(h => a.performed += h, h => a.performed -= h, truesubscriber, subscribeNow);
        }
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function to this Action's <see cref="InputAction.started"/> event. 
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="subscribeNow">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket SubscribeStarted(this InputAction a, Action<CTX> subscriber, bool subscribeNow = true) => new EventTicket<Action<CTX>>(h => a.started += h, h => a.started -= h, subscriber, subscribeNow);
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function to this Action's <see cref="InputAction.started"/> event. (Drops the Callback Context)
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="subscribeNow">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket SubscribeStarted(this InputAction a, Action subscriber, bool subscribeNow = true)
        {
            Action<CTX> truesubscriber = _ => subscriber?.Invoke();
            return new EventTicket<Action<CTX>>(h => a.started += h, h => a.started -= h, truesubscriber, subscribeNow);
        }
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function to this Action's <see cref="InputAction.canceled"/> event. 
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="subscribeNow">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket SubscribeCancelled(this InputAction a, Action<CTX> subscriber, bool subscribeNow = true) => new EventTicket<Action<CTX>>(h => a.canceled += h, h => a.canceled -= h, subscriber, subscribeNow);
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing a function to this Action's <see cref="InputAction.canceled"/> event. (Drops the Callback Context)
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="subscribeNow">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket SubscribeCancelled(this InputAction a, Action subscriber, bool subscribeNow = true)
        {
            Action<CTX> truesubscriber = _ => subscriber?.Invoke();
            return new EventTicket<Action<CTX>>(h => a.canceled += h, h => a.canceled -= h, truesubscriber, subscribeNow);
        }
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing one function to this Action's <see cref="InputAction.started"/> event and another to this Action's <see cref="InputAction.canceled"/> event. 
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="subscribeNow">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket SubscribeBoth(this InputAction a, Action<CTX> subscriberPress, Action<CTX> subscriberRelease, bool subscribeNow = true)
            => new EventTicketDual<Action<CTX>>(
                (h1, h2) => { a.started += h1; a.canceled += h2; },
                (h1, h2) => { a.started -= h1; a.canceled -= h2; },
                subscriberPress, subscriberRelease, subscribeNow
                );
        /// <summary>
        /// Creates an <see cref="EventTicket"/> subscribing one function to this Action's <see cref="InputAction.started"/> event and another to this Action's <see cref="InputAction.canceled"/> event. (Drops the Callback Context)
        /// </summary>
        /// <param name="subscriber">The subscribing function </param>
        /// <param name="subscribeNow">Whether this should immediately subscribe.</param>
        /// <returns>The <see cref="EventTicket"/> representing this subscripton</returns>
        public static EventTicket SubscribeBoth(this InputAction a, Action subscriberPress, Action subscriberRelease, bool subscribeNow = true)
        {
            Action<CTX> hp = _ => subscriberPress?.Invoke();
            Action<CTX> hr = _ => subscriberRelease?.Invoke();

            return new EventTicketDual<Action<CTX>>(
                (h1, h2) => { a.started += hp; a.canceled += hr; },
                (h1, h2) => { a.started -= hp; a.canceled -= hr; },
                hp, hr, subscribeNow
                );
        }


    }

    // Example usage:
    // var t = EventTicketHelpers.FromEvent<Action<int>>(h => publisher.SomeEvent += h, h => publisher.SomeEvent -= h, (i) => Debug.Log(i));
    // t.UnSubscribe();

    // UnityEvent:
    // var ut = EventTicketHelpers.FromUnityEvent(myUnityEvent, new UnityAction(Mysubscriber));
    // ut.UnSubscribe();
}
