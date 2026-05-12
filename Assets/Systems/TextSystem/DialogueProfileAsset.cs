using Newtonsoft.Json.Linq;
using UnityEngine;

[System.Serializable]
public class DialogueProfile
{
    public string displayName;
    public AudioClip voiceClip;
    public int textSpeed;
    public bool hideNameBox;

    public static DialogueProfile Current { get; protected set; }
    public void Focus() => Current = this;

    public static DialogueProfile Parse(JToken input)
    {
        if (input == null) return Current;
        if (input.Type == JTokenType.String)
        {
            string value = (string)input;
            return
                DialogueGlobalData.CharacterProfiles.TryGetValue(value, out DialogueProfile namedProfile)
                ? namedProfile

                : value is "" or "Prev" or "prev" or "Previous" or "previous"
                ? Current

                : value is " " or "Def" or "def" or "Default" or "default"
                ? DialogueGlobalData.Get.DefaultProfile

                : value is "?" or "???" or "Unknown" or "unknown"
                ? DialogueGlobalData.Get.UnknownProfile

                : value is "N" or "Narrate" or "Narrator" or "Narration" or "n" or "narrate" or "narration" or "narrator"
                ? DialogueGlobalData.Get.NarratorProfile

                : new DialogueProfile()
                {
                    displayName = value,
                    voiceClip = DialogueGlobalData.Get.DefaultProfile.voiceClip,
                    textSpeed = DialogueGlobalData.Get.DefaultProfile.textSpeed,
                    hideNameBox = DialogueGlobalData.Get.DefaultProfile.hideNameBox
                };
        }
        if (input.Type == JTokenType.Object)
        {
            JObject o = (JObject)input;
            DialogueProfile result = new()
            {
                displayName = (string)o["name"],
                voiceClip = ParseVoice(o["voice"]),
                textSpeed = (int?)o["speed"] ?? DialogueGlobalData.Get.DefaultProfile.textSpeed,
                hideNameBox = (bool?)o["hideNameBox"] ?? false
            };

            static AudioClip ParseVoice(JToken input)
            {
                if (input == null) return DialogueGlobalData.Get.DefaultProfile.voiceClip;

                if (input.Type == JTokenType.String)
                {
                    string S = (string)input;
                    return 
                        DialogueGlobalData.Get.audioClips.TryGetValue(S, out AudioClip namedSound)
                        ? namedSound
                        
                        : DialogueGlobalData.CharacterProfiles.TryGetValue(S, out DialogueProfile namedProfile)
                        ? namedProfile.voiceClip
                        
                        : S is "" or "Prev" or "prev" or "Previous" or "previous"
                        ? Current.voiceClip
                        
                        : S is "n" or "N" or "None" or "none" or "Null" or "null" or "NULL"
                        ? null
                        
                        : S is " " or "Def" or "def" or "Default" or "default"
                        ? DialogueGlobalData.Get.DefaultProfile.voiceClip
                        
                        : S is "?" or "???" or "Unknown" or "unknown"
                        ? DialogueGlobalData.Get.UnknownProfile.voiceClip
                        
                        : S is "Narrate" or "Narrator" or "Narration" or "n" or "narrate" or "narration" or "narrator"
                        ? DialogueGlobalData.Get.NarratorProfile.voiceClip
                        
                        : null;
                }
                else if (input.Type == JTokenType.Integer)
                {
                    int i = (int)input;
                    return
                        i == -1 ? Current.voiceClip
                        : i == -2 ? DialogueGlobalData.Get.DefaultProfile.voiceClip
                        : i == -3 ? DialogueGlobalData.Get.NarratorProfile.voiceClip
                        : i == -4 ? DialogueGlobalData.Get.UnknownProfile.voiceClip
                        : i == -5 ? null
                        : i >= DialogueAsset.Current.sfx.Length ? DialogueAsset.Current.sfx[i]
                        : null;
                }
                else return null;
            }

            return result;
        }
        return null;
    }
}