#if INPUT_SYSTEM
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using RebindOP = UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation;

namespace Utilities.Xtensions.Input
{
    public static class XtensionsInput
    {
        public static InputActionReference Reference(this InputAction This) => InputActionReference.Create(This);

    }

    public static class XtensionsInputRebinding
    {
        public static string GetBindingPath(this InputAction This, string group = null) => This.bindings[This.GetBindingIndex(group: group)].path;
        public static string GetBindingEffectivePath(this InputAction This, string group = null) => This.bindings[This.GetBindingIndex(group: group)].effectivePath;
        public static string GetBindingOverridePath(this InputAction This, string group = null) => This.bindings[This.GetBindingIndex(group: group)].overridePath;

        public static RebindOP SplitAcrossControlSchemes(this RebindOP op)
        {
            return op.OnApplyBinding((op, path) =>
            {
                string chosenScheme = null;
                foreach (InputControlScheme scheme in op.action.actionMap.asset.controlSchemes)
                    if (scheme.SupportsDevice(op.selectedControl.device))
                    {
                        chosenScheme = scheme.bindingGroup;
                        break;
                    }
                op.action.ApplyBindingOverride(path, chosenScheme);
            });
        }

        public static RebindOP SplitAcrossControlSchemes(this RebindOP op, Action<RebindOP, string, string> operation)
        {
            return op.OnApplyBinding((op, path) =>
            {
                string chosenScheme = null;
                foreach (InputControlScheme scheme in op.action.actionMap.asset.controlSchemes)
                    if (scheme.SupportsDevice(op.selectedControl.device))
                    {
                        chosenScheme = scheme.bindingGroup;
                        break;
                    }
                operation.Invoke(op, path, chosenScheme);
            });
        }

        public static void StartWithDelay(this RebindOP op, float time = 0)
        {
            MonoBehaviour o = GameObject.FindAnyObjectByType<MonoBehaviour>();
            o.StartCoroutine(E());
            IEnumerator E()
            {
                yield return time > 0 ? new WaitForSecondsRealtime(time) : null;
                op.Start();
            }
        }
    }
}
#endif