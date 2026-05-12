using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextSegment : ScriptableObject
{
    public string textValue;
    public int portraitSprite;
    public Polymorph.ListOf<TextAffector> textAffectors;
}

public class TextProfile : ScriptableObject
{
    public AudioClip speechClip;
    public float textRate;
    public string textFont; //Placeholder
    public Sprite[] faceSprites;
}

[System.Serializable]
public class TextAffector : Polymorph
{
    public int PositionInText;

    [System.Serializable]
    public class Color : TextAffector
    {
        public UnityEngine.Color color;
    }
    [System.Serializable]
    public class Rate : TextAffector
    {
        public float rate;
    }
    [System.Serializable]
    public class Delay : TextAffector
    {
        public float delayTime;
    }
    [System.Serializable]
    public class Speech : TextAffector
    {
        public AudioClip soundClip;
    }
    [System.Serializable]
    public class Sound : TextAffector
    {
        public AudioClip soundClip;
    }
    [System.Serializable]
    public class Font : TextAffector
    {
        public string font; //Placeholder
    }
    [System.Serializable]
    public class Icon : TextAffector
    {
        public Sprite iconSprite;
    }
    [System.Serializable]
    public class Style : TextAffector
    {
        public bool bold;
        public bool italic;
        public bool underline;
        public bool strikethrough;
    }
    [System.Serializable]
    public class DefaultStyle
    {

    }
    [System.Serializable]
    public class HighlightStyle
    {

    }
}