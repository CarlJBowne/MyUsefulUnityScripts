using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class Menu : MonoBehaviour
{
    #region Config

    [SerializeField] string ID;
    [DisableInPlayMode] public bool isActive;
    [DisableInPlayMode] public Menu parent;
    [SerializeField] Button defaultSelection;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Selectable[] allButtons;
    [SerializeField] private UnityEvent closeOverride;

    #endregion

    #region Instance Data

    public bool isCurrent => Menu.CurrentMenu == this;
    public bool isAvailable => Menu.AvailableMenus.ContainsValue(this);
    public bool isSubMenu => parent != null;
    public bool isLabeled => !string.IsNullOrWhiteSpace(ID);

    #endregion

    #region Instance Behavior

    protected virtual void Awake()
    {
        if (isLabeled)
        {
            if (AvailableMenus.TryGetValue(ID, out Menu existing) && existing != this)
                Debug.LogWarning($"Menu with ID {ID} already exists in AvailableMenus. Overwriting with new instance.");
            AvailableMenus[ID] = this;
        }

        if (isActive) Menu.Open(this, true);
        else gameObject.SetActive(false);
    }

    protected virtual void OnDestroy()
    {
        Menu.Close(this);

        if (isLabeled && AvailableMenus.ContainsKey(ID) && AvailableMenus[ID] == this)
            AvailableMenus.Remove(ID);

        isActive = false;
    }

    public void Open() => Menu.Open(this);

    public void Close(bool allowOverride = true)
    {
        if (allowOverride || closeOverride == null) Menu.Close(this);
        else closeOverride.Invoke();
    }


    private void SetInteractable(bool value)
    {
        if (allButtons == null) return;
        for (int i = 0; i < allButtons.Length; i++)
            if (allButtons[i] != null) allButtons[i].interactable = value;

        canvasGroup.blocksRaycasts = value;
    }

    protected virtual void OnOpen()
    {
        //if (!openSound.IsNull)
        //    AudioManager.Get().PlayOneShot(openSound, transform.position);
    }

    protected virtual void OnClose()
    {
        //if (!closeSound.IsNull)
        //    AudioManager.Get().PlayOneShot(closeSound, transform.position);
    }

    #endregion

    #region Static Data

    public static Dictionary<string, Menu> AvailableMenus { get; } = new();
    public static List<Menu> ActiveMenus { get; } = new();
    public static Menu CurrentMenu => ActiveMenus.Count > 0 ? ActiveMenus[^1] : null;

    #endregion

    #region Static Behavior

    public static void Open(Menu menu, bool overrideRedundancyCheck = false)
    {
        if (menu == null) return;
        if (menu.isActive && !overrideRedundancyCheck) return;

        // avoid duplicates; if already present move to top
        if (ActiveMenus.Contains(menu)) ActiveMenus.Remove(menu);

        ActiveMenus.Add(menu);

        menu.isActive = true;
        menu.gameObject.SetActive(true);
        menu.SetInteractable(true);

        if (menu.defaultSelection != null) menu.defaultSelection.Select();

        ActiveMenus[^2]?.SetInteractable(false);

        menu.OnOpen();
    }

    public static void Close(Menu menu)
    {
        if (menu == null) return;
        if (!menu.isActive)
        {
            // still try to remove if somehow present
            if (ActiveMenus.Contains(menu)) ActiveMenus.Remove(menu);
            return;
        }

        ActiveMenus.Remove(menu);

        menu.isActive = false;
        menu.gameObject.SetActive(false);

        ActiveMenus[^1]?.SetInteractable(true);
        menu.OnClose();
    }

    public static void CloseAllMenus()
    {
        for (int i = ActiveMenus.Count - 1; i >= 0; i--) Close(ActiveMenus[i]);
    }

    #endregion
}
