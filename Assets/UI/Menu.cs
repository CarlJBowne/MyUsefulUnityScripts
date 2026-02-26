using EditorAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A Base Menu class intended to form an easy-to-use extensible Menu system. (Admittedly not up to snuff.)
/// </summary>
public class Menu : MonoBehaviour
{
    //Config
    [DisableInPlayMode] public bool isActive;
    [DisableInPlayMode] public Menu parent;
    [SerializeField] private string dictionaryName;
    [SerializeField] private bool closeOverride;
    [SerializeField, ShowField(nameof(closeOverride))] private UnityEvent closeEvent;

    //Data
    public bool isCurrent => Manager.currentMenu == this;
    public bool isSubMenu => parent != null;

    protected virtual void Awake()
    {
        if (isActive) Manager.Open(this);
        else
        {
            gameObject.SetActive(false);
        }

        if (!string.IsNullOrEmpty(dictionaryName)) Manager.menuDictionary.Add(dictionaryName, this);
    }

    protected virtual void OnDestroy()
    {
        if (!string.IsNullOrEmpty(dictionaryName)) Manager.menuDictionary.Remove(dictionaryName);
        isActive = false;
        Manager.Close(this);
    }

    /// <summary>
    /// Opens the menu
    /// </summary>
    public void Open() => Manager.Open(this);

    /// <summary>
    /// Closes the menu (Invokes Override if present.)
    /// </summary>
    public void Close()
    {
        if (!closeOverride) Manager.Close(this);
        else closeEvent?.Invoke();
    }

    /// <summary>
    /// Closes the menu (Closes even if Override is present.)
    /// </summary>
    public void TrueClose() => Manager.Close(this);

    protected virtual void OnOpen()
    {

    }

    protected virtual void OnClose()
    {

    }

    /// <summary>
    /// The Global Manager in charge of Menus.
    /// </summary>
    public static class Manager
    {
        public static Menu currentMenu => currentMenus[^1];
        public static List<Menu> currentMenus = new();
        public static Dictionary<string, Menu> menuDictionary = new();
        public static bool disableEscape;

        /// <summary>
        /// Opens the specified menu
        /// </summary>
        /// <param name="menu">The Menu to be opened.</param>
        public static void Open(Menu menu)
        {
            if (menu.isActive) return;

            currentMenus.Add(menu);

            menu.isActive = true;
            menu.gameObject.SetActive(true);
            menu.OnOpen();
        }

        /// <summary>
        /// Closes the specified menu
        /// </summary>
        /// <param name="menu">The Menu to be closed.</param>
        public static void Close(Menu menu)
        {
            if (!menu.isActive) return;

            currentMenus.Remove(menu);

            menu.gameObject.SetActive(false);
            menu.isActive = false;
            menu.OnClose();
        }

        /// <summary>
        /// Handles the escape action (Bound to Escape / Start Button by Default.)
        /// </summary>
        public static void Escape()
        {
            //if (PauseMenu.Loaded && !PauseMenu.Active)
            //    PauseMenu.Get().Open();
            //else if (currentMenus.Count > 0)
            //    currentMenus[^1].Close();
        }
    }
}