using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SLS.EditorUtilities
{
    public static class EditorColors
    {
        public static Color Gray(float v) => new(v, v, v);
        public static Color Background { get; private set; } = Gray(.2196079f);
        public static Color FieldBackground { get; private set; } = Gray(.1568628f);
        public static Color Text { get; private set; } = Gray(.824f);
        public static Color ButtonText { get; private set; } = Gray(.933f);
        public static Color ButtonBack { get; private set; } = Gray(.345f);
        public static Color ButtonBorder { get; private set; } = Gray(.188f);
        public static Color ButtonBorderBottom { get; private set; } = Gray(.141f);
        public static float ButtonHoverBackRaise { get; private set; } = .404f - .345f;

        public static Color ButtonClickedBack { get; private set; } = new(.275f, .376f, .486f);
        public static Color ButtonSelectedOutline { get; private set; } = new(.482f, .682f, .980f);
        public static Color TextSelected { get; private set; } = new(.506f, .706f, 1);
        public static Color Selection { get; private set; } = new(.227451f, .4745098f, .7333333f);
        public static Color FoldoutArrow { get; private set; } = Gray(.408f);
        public static Color FoldoutArrowSelected { get; private set; } = new(.282f, .439f, .835f);

        public static Color GameObjectIconBlue { get; private set; } = new(.4980392f, .8392158f, .9882354f);
        public static Color XRed { get; private set; } = new(.9882354f, .427451f, .2509804f);
        public static Color YGreen { get; private set; } = new(.6941177f, .9882354f, .3490196f);
        public static Color ZBlue { get; private set; } = new(.4980392f, .8392158f, .9882354f);
        public static Color AudioGold { get; private set; } = new(.7843138f, .5882353f, .003921569f);
        public static Color PhysicsGreen { get; private set; } = new(.6941177f, .9882354f, .3490196f);
        public static Color TilesPurple { get; private set; } = new(.6784314f, .5647059f, .9490197f);
        public static Color AssemblyNavy { get; private set; } = new(.2941177f, .5294118f, .6705883f);
        public static Color ScriptGreen { get; private set; } = new(.1529412f, .4156863f, .1647059f);
        public static Color ScriptableRed { get; private set; } = new(.9882354f, .427451f, .2509804f);

    }
}