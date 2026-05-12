using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using Utilities.Singletons;

public class DialogueGlobalData : GlobalAsset<DialogueGlobalData>
{
    public DialogueProfile DefaultProfile;
    public DialogueProfile NarratorProfile;
    public DialogueProfile UnknownProfile;
    public List<DialogueProfile> characterProfiles;
    public SerializedDictionary<string, AudioClip> audioClips;

    public static Dictionary<string, DialogueProfile> CharacterProfiles;

    public override void OnInit()
    {
        CharacterProfiles = characterProfiles.ToDictionary(profile => profile.displayName, profile => profile);
    }
}