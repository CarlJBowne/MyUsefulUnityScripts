using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using Utilities.Singletons;

public class DialogueGlobalData : GlobalAsset<DialogueGlobalData>
{
    [SerializeField] private int textBoxLineCount=3;
    [SerializeField] private float textBoxWidth=200;
    public static int TextBoxLineCount => Get.textBoxLineCount;
    public static float TextBoxWidth => Get.textBoxWidth;

    public DialogueProfile DefaultProfile;
    public DialogueProfile NarratorProfile;
    public DialogueProfile UnknownProfile;
    public List<DialogueProfile> characterProfiles;
    public SerializedDictionary<string, AudioClip> audioClips;

    public override void OnInit()
    {
        DialogueProfile.Init(this);
    }
}