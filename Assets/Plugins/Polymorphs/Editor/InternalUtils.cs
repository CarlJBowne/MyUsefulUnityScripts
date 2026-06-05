using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UIElements;

internal static class InternalUtils
{
    public static T AddTo<T>(this T input, VisualElement target, Action<T> PostMake = null) where T : VisualElement
    {
        if (input == null) return null;
        target?.Add(input);
        PostMake?.Invoke(input);
        return input;
    }
    public static T AddTo<T>(this T input, VisualElement.Hierarchy target, Action<T> PostMake = null) where T : VisualElement
    {
        if (input == null) return null;
        target.Add(input);
        PostMake?.Invoke(input);
        return input;
    }
    public static void DelayedBuild(this VisualElement V, Action result) =>
            V.RegisterCallbackOnce<AttachToPanelEvent>(_ => V.schedule.Execute(result));
    public static void RegisterHoverEvents(this VisualElement V, Action<bool> hovered)
    {
        V.RegisterCallback<MouseOverEvent>(Do);
        V.RegisterCallback<MouseLeaveEvent>(Do);
        void Do(EventBase E) => hovered?.Invoke(E is MouseOverEvent);
    }
}
