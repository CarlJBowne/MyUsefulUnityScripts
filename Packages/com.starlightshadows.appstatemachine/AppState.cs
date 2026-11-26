using System;
using System.Collections;
using System.Collections.Generic;
using SLS.GeneralUtilities.Events;
using SLS.ListUtilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace SLS.AppStateMachine
{
    [DefaultExecutionOrder(-150)]
    [CreateAssetMenu(fileName = "GameState", menuName = "Scriptable Objects/GameState")]
    public class AppState : ScriptableObject
    {
        #region Manager
        public static AppState State { get; internal set; }
        internal static List<AppSubState> SubStates = new();
        public static AppState TopState => SubStates.Count > 0 ? SubStates[^1] : State;
        public static bool Transitioning { get; protected set; }
        public static AppState PrevState { get; internal set; }
        #endregion

        #region Serialized
        // Additive behavior moved to AppSubState; primary AppState instances are non-additive by design.
        [field: SerializeField] public SceneReference Scene { get; private set; }
        [field: SerializeField] public Prefab Prefab { get; private set; }
        [field: SerializeField] public virtual bool FreezeTillSceneLoad { get; private set; }
        [field: SerializeField] public virtual bool UnloadOnExit { get; private set; }
        [field: SerializeField] public DictionaryS<string, string> Parameters { get; private set; } = new();

        #endregion

        #region Primary Logic

        public virtual IEnumerator Enter()
        {
            yield return ExitPrevious();

            this.DoEnter();

            if (Scene) yield return LoadScene();
            if (Prefab) yield return LoadPrefab();

            yield return OnEnter();
        }

        protected void DoEnter()
        {
            PrevState = State;
            State = this;
        }
        protected void DoExit()
        {
            PrevState = State;
            State = null;
        }

        protected virtual IEnumerator LoadScene()
        {
            if (!Scene) yield break;

            if (!FreezeTillSceneLoad)
            {
                AsyncOperation syn = Scene.LoadSceneAsync();
                yield return new WaitUntil(() => syn.isDone);
            }
            else Scene.LoadScene();

            LoadedScene = SceneManager.GetSceneByName(Scene);
            yield break;
        }
        protected virtual IEnumerator UnloadScene()
        {
            if (!Scene) yield break;

            if (!FreezeTillSceneLoad)
            {
                AsyncOperation syn = Scene.UnloadSceneAsync();
                yield return new WaitUntil(() => syn.isDone);
            }
            else Scene.UnloadScene();

            LoadedScene = default;
            yield break;
        }
        protected virtual IEnumerator LoadPrefab()
        {
            if (!Prefab) yield break;
            LoadedPrefab = Prefab.Instantiate();
            yield break;
        }//Could use more in-depth functionality.
        protected virtual IEnumerator UnloadPrefab()
        {
            if (LoadedPrefab == null) yield break;
            Destroy(LoadedPrefab);
            LoadedPrefab = null;
            yield break;
        }

        protected virtual IEnumerator ExitPrevious()
        {
            if (State == null) yield break;
            State.DoExit();
            if (UnloadOnExit && !Scene.additive)
            {
                yield return UnloadScene();
            }
            yield return UnloadPrefab();
            yield return PrevState.OnExit();
        }

        public virtual void OnEnable()
        {
#if UNITY_EDITOR
            if (!AppStateMachine.Get.AllStates.Contains(this))
                AppStateMachine.Get.AllStates.Add(this);
#else
                        Destroy(this);
#endif
        }

        #endregion

        #region Callbacks
        protected virtual IEnumerator OnEnter()
        {
            onEnter?.Invoke();
            yield break;
        }
        protected virtual IEnumerator OnExit()
        {
            onExit?.Invoke();
            yield break;
        }

#if ULT_EVENTS
        [SerializeField] UltEvents.UltEvent onEnter;
        [SerializeField] UltEvents.UltEvent onExit;

        public EventTicket SubscribeOnEnter(Action callback) => onEnter.Subscribe(callback);
        public void UnsubscribeOnEnter(Action callback) => onEnter.DynamicCalls -= callback;
        public EventTicket SubscribeOnExit(Action callback) => onExit.Subscribe(callback);
        public void UnsubscribeOnExit(Action callback) => onEnter.DynamicCalls -= callback;

#else
        [SerializeField] UnityEngine.Events.UnityEvent onEnter;
        [SerializeField] UnityEngine.Events.UnityEvent onExit;

        public EventTicket RegisterOnEnter(UnityAction callback) => onEnter.Subscribe(callback);
        public void DeregisterOnEnter(UnityAction callback) => onEnter.RemoveListener(callback);
        public EventTicket RegisterOnExit(UnityAction callback) => onExit.Subscribe(callback);
        public void DeregisterOnExit(UnityAction callback) => onExit.RemoveListener(callback);
#endif
        #endregion

        public bool isActive => isActive_Internal;
        internal virtual bool isActive_Internal => State == this;
        public bool isTop => isTop_Internal;
        internal virtual bool isTop_Internal => SubStates.Count == 0;

        public GameObject LoadedPrefab { get; protected set; }
        public Scene LoadedScene { get; protected set; }

        public static implicit operator bool(AppState This) => This != null && This.isActive;

        public class Exception : System.Exception
        {
            public Exception(string message) : base(message) { }
        }
    }
}
