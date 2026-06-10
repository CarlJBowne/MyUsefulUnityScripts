using SLS.AssetUtilties;
using SLS.ListUtilities;
using UnityEngine;

namespace SLS.GameStateMachine
{
    [CreateAssetMenu(fileName = "GameState", menuName = "Scriptable Objects/GameState")]
    public class GameStateBase : ScriptableObject
    {
        public bool IsCurrent => Current == this;
        public static GameStateBase Current { get; internal set; }
        public static void Transition(GameStateBase next)
        {
            if(Current == null)
            {
                next.PreEnter.Invoke();
                Current = next;
                next.OnEnter.Invoke();
                return;
            }

            GameStateBase prev = Current;

            prev.PreExitLogic();
            prev.PreExit.Invoke();
            next.PreEnterLogic();
            next.PreEnter.Invoke();
            if (next.Scene == null) Finish();
            else
            {
                next.Scene.Load();
                next.Scene.OnUnLoad += Finish;
            }
            void Finish()
            {
                Current = next;
                prev.OnExitLogic();
                prev.OnExit.Invoke();
                next.OnEnterLogic();
                next.OnEnter.Invoke();
                next.Scene.OnUnLoad -= Finish;
            }
        }
        public void Enter() => Transition(this);

        [field: SerializeField] public SceneAsset Scene { get; private set; }

        [field: SerializeField] public DualEvent PreExit {get; private set;} = new();
        [field: SerializeField] public DualEvent OnExit {get; private set;}= new();
        [field: SerializeField] public DualEvent PreEnter {get; private set;}= new();
        [field: SerializeField] public DualEvent OnEnter {get; private set;}= new();
        [SerializeField] private DictionaryS<string, string> Parameters;

        protected virtual void PreEnterLogic() { }
        protected virtual void PreExitLogic() { }
        protected virtual void OnEnterLogic() { }
        protected virtual void OnExitLogic() { }

        private void OnEnable()
        {
            #if UNITY_EDITOR
            if(!GameStateRegistry.Get.AllStates.Contains(this)) 
                GameStateRegistry.Get.AllStates.Add(this);
            #else
            Destroy(this);
            #endif
        }
        public virtual void Init() { }
    }
}
