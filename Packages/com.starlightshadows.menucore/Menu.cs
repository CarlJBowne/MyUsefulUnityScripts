using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SLS.MenuCore
{
    [RequireComponent(typeof(CanvasGroup))]
    public class Menu : MonoBehaviour
    {
        #region Config

        [SerializeField] string ID;
        [InspectorName("Auto Open")] public bool isActive;
        public float fadeInTime = 0;
        public float fadeOutTime = 0;
        public Menu parent;
        [SerializeField] Button defaultSelection;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] private UltEvents.UltEvent closeOverride;
        [SerializeField] private UltEvents.UltEvent onOpen;
        [SerializeField] private UltEvents.UltEvent onClose;

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
            else SetVisibility(false, 0);
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
        public void TrueClose() => Close(false);


        protected virtual void SetVisibility(bool value, float? overrideLength = null)
        {
            SetInteractable(value);
            float targetLength = value
                ? overrideLength ?? fadeInTime
                : overrideLength ?? fadeOutTime;
            if (value)
            {
                if (targetLength <= 0) canvasGroup.alpha = 1f;
                else Coroutine.Begin(ref fadeCo, EnumTrue(1 / targetLength), true);
                IEnumerator EnumTrue(float rate)
                {
                    while (canvasGroup.alpha < 1)
                    {
                        canvasGroup.alpha += rate * Time.unscaledDeltaTime;
                        yield return null;
                    }
                    canvasGroup.alpha = 1;
                }
            }
            else
            {
                if (targetLength <= 0) canvasGroup.alpha = 0f;
                else Coroutine.Begin(ref fadeCo, EnumFalse(1 / targetLength), true);
                IEnumerator EnumFalse(float rate)
                {
                    while (canvasGroup.alpha > 0)
                    {
                        canvasGroup.alpha -= rate * Time.unscaledDeltaTime;
                        yield return null;
                    }
                    canvasGroup.alpha = 0;
                }
            }
        }
        private Coroutine fadeCo;

        private void SetInteractable(bool value)
        {
            canvasGroup.interactable = value;
            canvasGroup.blocksRaycasts = value;
        }

        protected virtual void OnOpen()
        {
            SetVisibility(true);
            onOpen?.Invoke();
            //if (!openSound.IsNull)
            //    AudioManager.Get().PlayOneShot(openSound, transform.position);
        }

        protected virtual void OnClose()
        {
            SetVisibility(false);
            onClose?.Invoke();
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

        public static void Escape()
        {
            if (ActiveMenus.Count == 0)
            {
                EscapeCallbackMenuless?.Invoke();
                return;
            }
            EscapeCallback?.Invoke();
            ActiveMenus[^1].Close();
        }
        public static Action EscapeCallback;
        public static Action EscapeCallbackMenuless;

        #endregion
    }

    public enum UILayer
    {
        OverEVERYTHING = 50,
        OverMenus = 35,
        Menus = 30,
        OverHUD = 25,
        HUD = 10,
        UnderHUD = 5,
        UnderEVERYTHING = -5
    }
}