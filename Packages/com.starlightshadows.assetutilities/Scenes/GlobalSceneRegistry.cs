using System.Collections.Generic;
using SLS.ListUtilities;
using SLS.Singletons;

public class GlobalSceneRegistry : GlobalAsset<GlobalSceneRegistry>
{
    public DictionaryS<string, SceneReference> NamedScenes;

    public override void OnInit()
    {
        for (int i = 0; i < NamedScenes.Count; i++)
        {
            IGlobalScene.RegisterScene(NamedScenes.ValueFromIndex(i), NamedScenes.KeyFromIndex(i));
        }
    }
}
