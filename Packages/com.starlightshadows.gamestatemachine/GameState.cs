using System;
using System.Collections.Generic;
using SLS.ListUtilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SLS.GameStateMachine
{
    [DefaultExecutionOrder(-150)]
    [CreateAssetMenu(fileName = "GameState", menuName = "Scriptable Objects/GameState")]
    public partial class GameState : ScriptableObject
    {
        #region Manager
        public static GameState PrimaryState {  get; internal set; }
        internal static List<GameState> AdditiveStates = new();
        public static GameState TopState => AdditiveStates.Count > 0 ? AdditiveStates[^1] : PrimaryState;

        internal static void TransitionPrimaryState(GameState next)
        {
            if (next.Additive) throw new Exception("Additive State treated as Primary State.");

            GameState prev = PrimaryState;

            if (prev != null)
            {
                prev.OnExitLogic();
                prev.OnExit.Invoke();
            }

            next.TransitionLogic(() => PrimaryState = next, Finish);
            void Finish()
            {
                next.OnEnterLogic();
                next.OnEnter.Invoke();
            }

        }
        internal static void AddAdditiveState(GameState state)
        {
            if (!state.Additive) throw new Exception("Primary State treated as Additive State.");
            if (AdditiveStates.Contains(state)) throw new Exception("Additive State already in the stack.");

            AdditiveStates.Add(state);
            state.OnEnterLogic();
            state.OnEnter.Invoke();
        }
        internal static void RemoveAdditiveState(GameState state)
        {
            if (!state.Additive) throw new Exception("Primary State treated as Additive State.");
            if (AdditiveStates.Contains(state)) throw new Exception("Additive State not found in stack.");

            state.OnExitLogic();
            state.OnExit.Invoke();
            AdditiveStates.Remove(state);
        }
        #endregion

        public void Enter()
        {
            if (!Additive) TransitionPrimaryState(this);
            else AddAdditiveState(this);
        }
        public void Exit()
        {
            if (Additive) RemoveAdditiveState(this);
            else throw new Exception("Primary State cannot be exited with Exit(). Use TransitionPrimaryState() to transition to another Primary State.");
        }

        [field: SerializeField] public virtual bool Additive { get; private set; }
        [field: SerializeField] public SceneReference Scene { get; private set; }
        [field: SerializeField] public Prefab Prefab { get; private set; }
        [field: SerializeField] public virtual bool FreezeTillSceneLoad { get; private set; }

        [field: SerializeField] public DualEvent OnExit { get; private set; } = new();
        [field: SerializeField] public DualEvent OnEnter { get; private set; } = new();
        [field: SerializeField] public DictionaryS<string, string> Parameters { get; private set; } = new();

        protected virtual void OnEnterLogic() { }
        protected virtual void OnExitLogic() { }
        protected virtual void TransitionLogic(Action SetCurrent, Action PostAction)
        {
            SetCurrent();
            if (Scene)
            {
                if (FreezeTillSceneLoad)
                {
                    SceneManager.LoadScene(Scene, Additive ? LoadSceneMode.Additive : LoadSceneMode.Single);
                    PostAction();
                }
                else
                {
                    AsyncOperation syn = SceneManager.LoadSceneAsync(Scene, Additive ? LoadSceneMode.Additive : LoadSceneMode.Single);
                    syn.completed += syn => { PostAction(); };
                }
            }
            else if (Prefab)
            {
                Prefab.Instantiate();
                PostAction();
            }
            else PostAction();
        }

        public virtual void OnEnable()
        {
            //#if UNITY_EDITOR
            //            if (!GameStateRegistry.Get.AllStates.Contains(this))
            //                GameStateRegistry.Get.AllStates.Add(this);
            //#else
            //            Destroy(this);
            //#endif
        }

        public bool isActive => !Additive ? PrimaryState == this : AdditiveStates.Contains(this);
        public bool isTop => this == TopState;

        public static implicit operator bool(GameState This) => This != null && This.isActive;

        public class Exception : System.Exception
        {
            public Exception(string message) : base(message) { }
        }
    }

}
