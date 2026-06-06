using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

[System.Serializable]
public class DialogueProfile
{
    public string ID;
    public string displayName;
    public AudioClip voiceClip;
    public float textSpeed;
    public bool hideNameBox;

    public void Focus() => Current = this;
    public static DialogueProfile Current { get; protected set; }
    public static DialogueProfile Default;
    public static DialogueProfile Narrator;
    public static DialogueProfile Unknown;
    public static Dictionary<string, DialogueProfile> Profiles;
    public static void Init(DialogueGlobalData globalInput)
    {
        Default = globalInput.DefaultProfile;
        Narrator = globalInput.NarratorProfile;
        Unknown = globalInput.UnknownProfile;
        Profiles = globalInput.characterProfiles.ToDictionary(profile => profile.ID, profile => profile);
    }





    public static DialogueProfile Parse(JToken input)
    {
        if (input == null) return Current;
        if (input.Type == JTokenType.String)
        {
            string value = (string)input;
            return
                Profiles.TryGetValue(value, out DialogueProfile namedProfile)
                ? namedProfile

                : value is "" or "Prev" or "prev" or "Current" or "current" or "Previous" or "previous"
                ? Current

                : value is " " or "Def" or "def" or "Default" or "default"
                ? Default

                : value is "?" or "???" or "Unknown" or "unknown"
                ? Unknown

                : value is "N" or "Narrate" or "Narrator" or "Narration" or "n" or "narrate" or "narration" or "narrator"
                ? Narrator

                : new DialogueProfile()
                {
                    ID = "Custom",
                    displayName = value,
                    voiceClip = Default.voiceClip,
                    textSpeed = Default.textSpeed,
                    hideNameBox = Default.hideNameBox
                };
        }
        if (input.Type == JTokenType.Object)
        {
            JObject o = (JObject)input;
            DialogueProfile result = new()
            {
                ID = "Custom",
                displayName = (string)o["name"],
                voiceClip = ParseVoice(o["voice"]),
                textSpeed = (int?)o["speed"] ?? Default.textSpeed,
                hideNameBox = (bool?)o["hideNameplate"] ?? false
            };

            static AudioClip ParseVoice(JToken input)
            {
                if (input == null) return DialogueGlobalData.Get.DefaultProfile.voiceClip;

                if (input.Type == JTokenType.String)
                {
                    string S = (string)input;
                    return
                        DialogueAsset.Current.sfx.TryGetValue(S, out AudioClip namedSound) ? namedSound
                        : DialogueGlobalData.Get.audioClips.TryGet(S, out namedSound) ? namedSound

                        : Profiles.TryGetValue(S, out DialogueProfile namedProfile) ? namedProfile.voiceClip

                        : S is "" or "Prev" or "prev" or "Previous" or "previous"
                        ? Current.voiceClip

                        : S is "n" or "N" or "None" or "none" or "Null" or "null" or "NULL"
                        ? null

                        : S is " " or "Def" or "def" or "Default" or "default"
                        ? Default.voiceClip

                        : S is "?" or "???" or "Unknown" or "unknown"
                        ? Unknown.voiceClip

                        : S is "Narrate" or "Narrator" or "Narration" or "n" or "narrate" or "narration" or "narrator"
                        ? Narrator.voiceClip

                        : null;
                }
                //else if (input.Type == JTokenType.Integer)
                //{
                //    int i = (int)input;
                //    return
                //        i == -1 ? Current.voiceClip
                //        : i == -2 ? DialogueGlobalData.Get.DefaultProfile.voiceClip
                //        : i == -3 ? DialogueGlobalData.Get.NarratorProfile.voiceClip
                //        : i == -4 ? DialogueGlobalData.Get.UnknownProfile.voiceClip
                //        : i == -5 ? null
                //        : i >= DialogueAsset.Current.sfx.Length ? DialogueAsset.Current.sfx[i]
                //        : null;
                //}
                else return null;
            }

            return result;
        }
        return null;
    }
}