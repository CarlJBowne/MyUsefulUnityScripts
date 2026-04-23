using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SaveSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEditorInternal;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class PackagingManager : EditorWindow
{
    [MenuItem("Tools/Packaging Manager")]
    public static new void Show()
    {
        PackagingManager w = ScriptableObject.CreateInstance<PackagingManager>();
        w.titleContent = new("Debug Tools Window");
        w.ShowUtility();
    }

    public Button LoadButton;
    public Button SaveButton;
    public Button BuildPackageButton;

    JsonFile file;

    PackagedFolder AssetsNode;


    private void OnEnable()
    {
        rootVisualElement.CreateAddAndStore(out BuildPackageButton, () => new(BuildPackage)
        {
            text = "----BUILD PACKAGE----",
            style =
            {
                minHeight = 32
            }
        });

        rootVisualElement.CreateAddAndStore(out VisualElement loadSaveRow, () => new()
        {
            style = { flexDirection = FlexDirection.Row, }
        });
        loadSaveRow.CreateAddAndStore(out LoadButton, () => new(Load)
        {
            text = "Load",
            style =
            {
                borderBottomRightRadius = 0,
                borderTopRightRadius = 0,
                color = Color.lightCyan,
                backgroundColor = Color.darkCyan,
                marginRight = 0,
                borderRightWidth = 0,
                flexGrow = 1
            }
        });
        LoadButton.Highlighter(0.2f);
        loadSaveRow.CreateAddAndStore(out SaveButton, () => new(Save)
        {
            text = "Save",
            style =
            {
                borderBottomLeftRadius = 0,
                borderTopLeftRadius = 0,
                color = Color.orange,
                backgroundColor = Color.brown,
                marginLeft = 0,
                borderLeftWidth = 0,
                flexGrow = 1
            }
        });
        SaveButton.Highlighter(0.2f);

        // Drag & drop from Project window
        rootVisualElement.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
        rootVisualElement.RegisterCallback<DragPerformEvent>(OnDragPerform);

        Load();
    }

    public void Load()
    {
        if (AssetsNode != null)
        {
            AssetsNode.Clear();
            rootVisualElement.Remove(AssetsNode);
            AssetsNode = null;
        }
        rootVisualElement.CreateAddAndStore(out AssetsNode, () => new("Assets", false));

        file = new($"{Application.dataPath}/_Packaging", "ActiveData");
        file.LoadFromFile();
        rootVisualElement.style.marginLeft = 1;
        JArray level1Array = file.Data["children"] as JArray;
        for (int i = 0; i < level1Array.Count; i++)
            JToNode(level1Array[i] as JObject, AssetsNode);

        void JToNode(JObject input, PackagedFolder parent)
        {
            if (input["isFolder"].ToObject<bool>())
            {
                PackagedFolder res = new(input["name"].ToObject<string>());
                parent.Add(res);
                JArray array = input["children"] as JArray;
                for (int i = 0; i < array.Count; i++)
                    JToNode(array[i] as JObject, res);
            }
            else
            {
                PackagedAsset res = new(input["name"].ToObject<string>());
                parent.Add(res); 
            }
        }
    }
    public void Save()
    {
        file.Data = AssetsNode.WriteToJ();
        file.SaveToFile();
    }
    public void BuildPackage()
    {
        if (AssetsNode == null)
        {
            EditorUtility.DisplayDialog("Build Package", "No assets to package.", "OK");
            return;
        }
        List<string> paths = new();
        HashSet<string> requiredFolders = new(StringComparer.OrdinalIgnoreCase);

        void Recurse(PackagedNode node, string currentPath)
        {
            if (node is PackagedFolder folder)
            {
                string folderPath = currentPath == "" ? folder.name : Path.Combine(currentPath, folder.name).Replace("\\", "/");

                // Record folder for potential placeholder creation
                requiredFolders.Add(folderPath);

                // If first child is a PackagedAsset named ALL -> include entire folder
                if (folder.children != null && folder.children.Count > 0 && folder.children[0] is PackagedAsset firstAsset && firstAsset.name == "ALL")
                {
                    // Find all assets under this folder (recursive)
                    string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });
                    foreach (var guid in guids)
                    {
                        string p = AssetDatabase.GUIDToAssetPath(guid);
                        if (!paths.Contains(p))
                            paths.Add(p);
                    }

                    // Also collect all subfolders so empty subfolders can get placeholders
                    CollectSubfolders(folderPath, requiredFolders);
                }
                else if (folder.children != null && folder.children.Count > 0)
                {
                    // Recurse into children
                    foreach (var c in folder.children)
                        Recurse(c, folderPath);
                }
                else
                {
                    // Empty folder node - ensure it's recorded so we can create a placeholder later
                    requiredFolders.Add(folderPath);
                }
            }
            else if (node is PackagedAsset asset)
            {
                // asset under currentPath
                string p = (currentPath == "") ? asset.name : (currentPath + "/" + asset.name);
                // If path exists, add. Otherwise try to find by name in the folder
                if (AssetDatabase.LoadMainAssetAtPath(p) != null)
                {
                    if (!paths.Contains(p)) paths.Add(p);
                }
                else
                {
                    // Try to find by name under currentPath
                    string folderSearch = currentPath == "" ? "Assets" : currentPath;
                    var guids = AssetDatabase.FindAssets(asset.name, new[] { folderSearch });
                    foreach (var g in guids)
                    {
                        var candidate = AssetDatabase.GUIDToAssetPath(g);
                        if (Path.GetFileNameWithoutExtension(candidate) == Path.GetFileNameWithoutExtension(asset.name) || Path.GetFileName(candidate) == asset.name)
                        {
                            if (!paths.Contains(candidate)) paths.Add(candidate);
                            break;
                        }
                    }
                }
            }
        }

        // Collect subfolders recursively using AssetDatabase.GetSubFolders
        void CollectSubfolders(string rootFolder, HashSet<string> collector)
        {
            try
            {
                var subs = AssetDatabase.GetSubFolders(rootFolder);
                foreach (var s in subs)
                {
                    collector.Add(s.Replace("\\", "/"));
                    CollectSubfolders(s, collector);
                }
            }
            catch { }
        }

        // Start recursion from children of AssetsNode (AssetsNode itself represents "Assets")
        foreach (var c in AssetsNode.children)
            Recurse(c, "Assets");

        // Now ensure placeholders for empty folders
        List<string> createdPlaceholders = new();
        try
        {
            foreach (var folderPath in requiredFolders)
            {
                // Skip invalid folders
                if (!AssetDatabase.IsValidFolder(folderPath))
                    continue;

                // If there are any assets (including in subfolders) this folder will be represented; skip
                var guids = AssetDatabase.FindAssets("", new[] { folderPath });
                if (guids.Length == 0)
                {
                    // create unique placeholder file
                    string placeholderName = $".packaging_placeholder_{Guid.NewGuid()}.txt";
                    string rel = folderPath.Substring("Assets".Length).TrimStart('/');
                    string fullPath = Path.Combine(Application.dataPath, rel, placeholderName);
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                        File.WriteAllText(fullPath, "Packaging placeholder file");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to create placeholder file '{fullPath}': {ex}");
                        continue;
                    }

                    string assetPath = (folderPath + "/" + placeholderName).Replace("\\", "/");
                    AssetDatabase.ImportAsset(assetPath);
                    createdPlaceholders.Add(assetPath);
                    paths.Add(assetPath);
                }
            }

            if (paths.Count == 0)
            {
                EditorUtility.DisplayDialog("Build Package", "No valid asset paths were found to export.", "OK");
                return;
            }

            string savePath = EditorUtility.SaveFilePanel("Export Package", Application.dataPath, "Package.unitypackage", "unitypackage");
            if (string.IsNullOrEmpty(savePath)) return;

            AssetDatabase.SaveAssets();

            AssetDatabase.ExportPackage(paths.ToArray(), savePath, ExportPackageOptions.Recurse);
            EditorUtility.RevealInFinder(savePath);
            EditorUtility.DisplayDialog("Build Package", $"Exported {paths.Count} assets to:\n{savePath}", "OK");
        }
        finally
        {
            // Cleanup placeholders
            if (createdPlaceholders.Count > 0)
            {
                foreach (var ph in createdPlaceholders)
                {
                    AssetDatabase.DeleteAsset(ph);
                }
                AssetDatabase.Refresh();
            }
        }
    }

    private void OnDragUpdated(DragUpdatedEvent e)
    {
        // Only accept drags that contain project assets
        if (DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0)
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        else
            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
    }

    private void OnDragPerform(DragPerformEvent e)
    {
        if (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0) return;

        foreach (var obj in DragAndDrop.objectReferences)
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(assetPath)) continue;
            AddAssetPathAsNodes(assetPath);
        }

        DragAndDrop.AcceptDrag();
    }

    private void AddAssetPathAsNodes(string assetPath)
    {
        // Ensure AssetsNode exists
        if (AssetsNode == null)
            rootVisualElement.CreateAddAndStore(out AssetsNode, () => new("Assets", false));

        // Normalize
        assetPath = assetPath.Replace("\\", "/");
        if (!assetPath.StartsWith("Assets"))
            return;

        string rel = assetPath.Substring("Assets".Length).TrimStart('/');
        var parts = rel.Split(new[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries);

        PackagedFolder parent = AssetsNode;
        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i];
            bool isLast = i == parts.Length - 1;
            string cumulativePath = "Assets" + (i >= 0 ? "/" + string.Join("/", parts, 0, i + 1) : "");

            if (isLast && AssetDatabase.IsValidFolder(cumulativePath))
            {
                // It's a folder dragged. Create folder node and add ALL asset to include folder contents
                PackagedFolder found = parent.children?.Find(x => x is PackagedFolder pf && pf.name == part) as PackagedFolder;
                if (found == null)
                {
                    found = new PackagedFolder(part);
                    parent.Add(found);
                }

                // Add ALL asset if not already present as first child
                if (found.children == null || found.children.Count == 0 || !(found.children[0] is PackagedAsset pa && pa.name == "ALL"))
                {
                    found.Add(new PackagedAsset("ALL"));
                }

                parent = found;
            }
            else if (isLast)
            {
                // It's a file
                // create/find parent folders along the way
                PackagedFolder found = parent.children?.Find(x => x is PackagedFolder pf && pf.name == part) as PackagedFolder;
                if (found == null && parts.Length > 1)
                {
                    // if there's at least one folder level before file, ensure folders are created (handled in loop earlier)
                }

                // Add asset node under parent
                // Avoid duplicates
                if (parent.children == null || parent.children.Find(x => x is PackagedAsset pa && pa.name == part) == null)
                {
                    parent.Add(new PackagedAsset(part));
                }
            }
            else
            {
                // intermediate folder
                PackagedFolder found = parent.children?.Find(x => x is PackagedFolder pf && pf.name == part) as PackagedFolder;
                if (found == null)
                {
                    found = new PackagedFolder(part);
                    parent.Add(found);
                }
                parent = found;
            }
        }
    }

    
}


public static class Helpers
{
    public static Color Up(this Color color, float f) => new(color.r + f, color.g + f, color.b + f, 1);

    public static void CreateAddAndStore<T>(this VisualElement target, out T result, Func<T> process) where T : VisualElement
    {
        result = process();
        target.Add(result);
    }

    public static void Highlighter(this VisualElement V, float factor)
    {
        Color initialColor = V.style.color.value;
        Color initialColorBack = V.style.backgroundColor.value;
        Color initialColorBorder = V.style.borderTopColor.value;

        V.RegisterCallback<MouseOverEvent>(Hover);
        V.RegisterCallback<MouseLeaveEvent>(UnHover);

        Color H(Color IN) => new(IN.r + factor, IN.g + factor, IN.b + factor);
        void Hover(MouseOverEvent E)
        {
            V.style.color = H(initialColor);
            V.style.backgroundColor = H(initialColorBack);
            //V.style.borderBottomColor = H(initialColorBorder);
            //V.style.borderLeftColor = H(initialColorBorder);
            //V.style.borderRightColor = H(initialColorBorder);
            //V.style.borderTopColor = H(initialColorBorder);
        }
        void UnHover(MouseLeaveEvent E)
        {
            V.style.color = initialColor;
            V.style.backgroundColor = initialColorBack;
            //V.style.borderBottomColor = initialColorBorder;
            //V.style.borderLeftColor = initialColorBorder;
            //V.style.borderRightColor = initialColorBorder;
            //V.style.borderTopColor = initialColorBorder;
        }
    }
}