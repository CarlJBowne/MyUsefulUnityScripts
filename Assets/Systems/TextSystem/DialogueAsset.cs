using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueAsset", menuName = "Dialogue/Asset")]
public class DialogueAsset : ScriptableObject
{
    public string rawData;
    public Sprite[] icons;
    public Dictionary<string, AudioClip> sfx = new();
    public UltEvents.UltEvent[] events;
    public JArray Value;

    public void Load() => Value = JArray.Parse(rawData);

    public static DialogueAsset Current { get; protected set; }
    public void Focus() => Current = this;
}

/*
// Example rawData content
[
    ["This is Dialogue Box 1", "Narrator"],
    ["This is Dialogue Box 2", "Narrator"],
    ["Is this text really necessary?", "OtherCharacter"],
    ["This is for-", "Narrator", {"skip":0}],
    ["I mean come on, this is a waste of time! Right?", "OtherCharacter", 
       {choice: {
           "As I was saying" : 5,
           "Shut up" : 6
       }}
    ],
    ["As I was saying. . .", "Narrator", go : 7],
    ["Shut up and let me finish", "Narrator" go : 7],
    [". . .Okay, now I'm finished.", "Narrator"],
    {"End":0}
]
//The anatomy of a dialogue segment:
0 = text. Actual display text.
1 = profile. This can either be a string or a JObject. If it's a string, it will be used to look up the character profile in DialogueGlobalData. If it's a string that does not exist in DialogueGlobalData, said string is used as the display name along with the default text voice.
  If it's a JObject, it will try to parse for "displayName" and "textVoice".
2 = postActons. A JArray that will contain any number of actions to enact at the end of the segment. Which should be a string or a JObject.
Instead of a dialogue segment, a Dialogue action can also be slotted in the sequence, which should contain a single string/JObject or array of such.
(How this will be identified for Parsing is still yet to be determined.)

 */