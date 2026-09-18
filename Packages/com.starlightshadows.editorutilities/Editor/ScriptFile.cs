using UnityEngine;
using SLS.GeneralUtilities;

namespace SLS.EditorUtilities.Editor
{
    public class ScriptFile : TextFile
    {
        public ScriptFile(string path, string filename) : base(path, filename) { }
        public override string extension { get => ".cs"; set { } }
    }

    public class ClassTemplate
    {
        public string Text;
    }
}
