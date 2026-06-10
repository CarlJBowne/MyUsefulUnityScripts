using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SLS.Singletons;

namespace SLS.GameStateMachine
{
    public class GameStateRegistry : GlobalAsset<GameStateRegistry>
    {
        public List<GameStateBase> AllStates;
        public static Dictionary<string, GameStateBase> Dict;

        public static Action Setup;

        public override void OnInit()
        {
            Dict = AllStates.ToDictionary(s => s.name);
            for (int i = 0; i < AllStates.Count; i++) AllStates[i].Init();

            Setup?.Invoke();

            GameStateBase.Transition(AllStates[0]);
        }
    }
}
