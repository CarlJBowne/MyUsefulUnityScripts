using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;

abstract class PackagedNode : VisualElement
{
    new public string name;
    public int depth;
    public string literalName
    {
        get => base.name;
        set => base.name = value;
    }

    public abstract JObject WriteToJ();

    public void Delete()
    {
        if (parent is not PackagedFolder folder) return;
        folder.Remove(this);
    }
}

class PackagedFolder : PackagedNode
{
    public PackagedFolder(string name, bool haveDeleteButton = true)
    {
        this.name = name;
        literalName = name;
        this.CreateAddAndStore(out VisualElement LabelRow, () => new()
        {
            style = { flexDirection = FlexDirection.Row }
        });
        LabelRow.CreateAddAndStore(out _, () => new Label($"● {name}")
        {
            style = { flexGrow = 1 }
        });
        if (haveDeleteButton)
        {
            LabelRow.CreateAddAndStore(out Button removeButton, () => new(Delete)
            {
                text = "-",
                style =
            {
                backgroundColor = Color.darkRed,
                color = Color.red,
                maxHeight = 12
            }
            });
            removeButton.Highlighter(.2f);
        }

        LabelRow.CreateAddAndStore(out _, () =>
        {
            Button res = new(AddButton)
            {
                text = "+",
                style =
                {
                    backgroundColor = Color.darkGreen,
                    color = Color.green,
                    maxHeight = 12
                }
            };
            res.Highlighter(.2f);
            return res;
        });

        style.marginLeft = 16;
        children = new();
    }

    public List<PackagedNode> children;

    public void Add(PackagedNode node)
    {
        children.Add(node);
        base.Add(node);
    }
    public void Remove(PackagedNode node)
    {
        children.Remove(node);
        base.Remove(node);
    }
    new public void Clear()
    {
        children.Clear();
        base.Clear();
    }

    public override JObject WriteToJ()
    {
        JArray array = new();
        for (int i = 0; i < children.Count; i++)
        {
            array.Add(children[i].WriteToJ());
        }
        JObject This = new()
        {
            ["isFolder"] = true,
            ["name"] = name,
            ["children"] = array
        };
        return This;
    }

    private void AddButton()
    {
        this.CreateAddAndStore(out VisualElement AddProcess, () => new()
        {
            style = { flexDirection = FlexDirection.Row }
        });
        AddProcess.CreateAddAndStore(out TextField nameInput, () => new()
        {
            value = "Input Name Here",
            style =
            {
                height = 19,
                minWidth = 150
            }
        });
        nameInput.Focus();
        AddProcess.CreateAddAndStore(out Toggle isFolder, () => new()
        {
            label = "Folder?",
            labelElement =
            {
                style =
                {
                    minWidth = 43,
                    maxWidth = 43
                }
            }
        });
        AddProcess.CreateAddAndStore(out Button FinishButton, () => new(Finish)
        {
            text = "Confirm",
            style =
            {
                height = 19
            }
        });
        AddProcess.CreateAddAndStore(out Button CancelButton, () => new(Cancel)
        {
            text = "Cancel",
            style =
            {
                height = 19
            }
        });
        void Finish()
        {
            this.Add(isFolder.value ? new PackagedFolder(nameInput.value) : new PackagedAsset(nameInput.value));
            this.Remove(AddProcess);
        }
        void Cancel() => this.Remove(AddProcess);
    }
}

class PackagedAsset : PackagedNode
{
    public PackagedAsset(string name)
    {
        this.name = name;
        literalName = name;
        this.CreateAddAndStore(out VisualElement LabelRow, () => new()
        {
            style = { flexDirection = FlexDirection.Row }
        });
        LabelRow.CreateAddAndStore(out _, () => new Label($"● {name}")
        {
            style = { flexGrow = 1 }
        });
        LabelRow.CreateAddAndStore(out Button removeButton, () => new(Delete)
        {
            text = "-",
            style =
            {
                backgroundColor = Color.darkRed,
                color = Color.red,
                maxHeight = 12
            }
        });
        style.marginLeft = 16;
    }


    public override JObject WriteToJ() => new()
    {
        ["isFolder"] = false,
        ["name"] = name,
    };
}