using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SLS.AppStateMachine
{
    public class AppSubState : AppState
    {
        new protected void DoEnter()
        {
            if (isActive) throw new Exception("This Substate is already active");
            SubStates.Add(this);
        }
        new protected void DoExit()
        {
            if (!isActive) throw new Exception("This Substate isnt active yet");
            SubStates.Remove(this);
        }
        internal override bool isActive_Internal => SubStates.Contains(this);
        internal override bool isTop_Internal => SubStates.Count > 0 && SubStates[^1] == this;

        new public virtual IEnumerator Enter()
        {
            this.DoEnter();

            yield return LoadScene();
            yield return LoadPrefab();

            yield return OnEnter();
        }
        public virtual IEnumerator Exit()
        {
            this.DoExit();
            yield return this.OnExit();
            yield break;
        }
    }
}
