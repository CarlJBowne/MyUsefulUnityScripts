using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;

public abstract class DialoguePiece
{
    public static DialoguePiece Parse(JToken input)
        => input.Type == JTokenType.Array ? DialogueSegment.Parse(input as JArray)
         : input.Type == JTokenType.String ? new DialogueSegment()
         {
             text = (string)input,
             profile = DialogueProfile.Current,
             postActions = null
         }
         : input.Type == JTokenType.Object ? DialogueActions.Parse(input as JObject)
         : throw new DialogueSerializationException("Invalid DialoguePiece input. Must be either a string or an array.");
}

public class DialogueSegment : DialoguePiece
{
    public string text;
    public DialogueProfile profile;
    public DialogueActions postActions;

    public static DialogueSegment Current { get; protected set; }
    public void Focus() => Current = this;

    public static DialogueSegment Parse(JArray input)
    {
        string text = (string)input[0];
        JToken profileToken = input.Count > 1 ? input[1] : null;
        JObject postActionsToken = input.Count > 2 ? input[2] as JObject : null;
        return new DialogueSegment()
        {
            text = text,
            profile = DialogueProfile.Parse(profileToken),
            postActions = postActionsToken != null ? DialogueActions.Parse(postActionsToken) : null
        };
    }
}
public class DialogueActions : DialoguePiece
{
    public Dictionary<Type, DialogueAction> actions = new();
    public DialogueAction this[Type t] => actions[t];

    public void RemoveAction(Type t) => actions.Remove(t);
    public void RemoveAction<T>() => actions.Remove(typeof(T));
    public void HasAction<T>() => actions.ContainsKey(typeof(T));
    public void AddAction(DialogueAction action) => actions.Add(action.GetType(), action);

    public static DialogueActions Parse(JObject input)
    {
        DialogueActions result = new();

        if (input.TryGetValue("go", out JToken goToValue) ||
            input.TryGetValue("goto", out goToValue) ||
            input.TryGetValue("Go", out goToValue) ||
            input.TryGetValue("GoTo", out goToValue) ||
            input.TryGetValue("next", out goToValue) ||
            input.TryGetValue("Next", out goToValue)
            )
            result.AddAction(new DialogueAction.GoTo((int)goToValue));

        if (input.TryGetValue("choice", out JToken choiceValue) ||
            input.TryGetValue("Choice", out choiceValue))
        {
            if (choiceValue.Type != JTokenType.Object)
                throw new DialogueSerializationException("Invalid choice action. Choice actions must be an object with option text as keys and segment indices as values.");
            result.AddAction(new DialogueAction.Choice()
            {
                options = ((JObject)choiceValue).ToObject<Dictionary<string, int>>()
            });
        }

        if (input.TryGetValue("event", out JToken eventValue) ||
           input.TryGetValue("Event", out eventValue))
        {
            if (eventValue.Type != JTokenType.Integer)
                throw new DialogueSerializationException("Invalid event action. Event actions must be an integer representing the event ID.");
            result.AddAction(new DialogueAction.Event((int)eventValue));
        }

        if (input.TryGetValue("hideBox", out JToken hideBoxValue) ||
           input.TryGetValue("HideBox", out hideBoxValue))
        {
            if (hideBoxValue.Type != JTokenType.Boolean)
                throw new DialogueSerializationException("Invalid hideBox action. HideBox actions must be a boolean representing whether the dialogue box should be hidden or not.");
            result.AddAction(new DialogueAction.HideBox((bool)hideBoxValue));
        }

        if(input.TryGetValue("autoSkip", out JToken _) ||
           input.TryGetValue("skip", out _) ||
           input.TryGetValue("Skip", out _) ||
           input.TryGetValue("AutoSkip", out _))
            result.AddAction(new DialogueAction.AutoSkip());

        if (input.TryGetValue("end", out _) ||
           input.TryGetValue("End", out _))
            result.AddAction(new DialogueAction.End());

        return result;
    }
}

public abstract class DialogueAction
{
    public class GoTo : DialogueAction
    {
        public int segmentIndex;
        public GoTo(int segmentIndex) => this.segmentIndex = segmentIndex;
    }
    public class Choice : DialogueAction
    {
        public Dictionary<string, int> options;
    }
    public class Event : DialogueAction
    {
        public int eventID;
        public Event(int eventID) => this.eventID = eventID;
    }
    public class HideBox : DialogueAction
    {
        public bool hidden;
        public HideBox(bool hidden) => this.hidden = hidden;
    }
    public class AutoSkip : DialogueAction { }
    public class End : DialogueAction { }
}

public class DialogueSerializationException : Exception
{
    public DialogueSerializationException(string message) : base(message) { }
}