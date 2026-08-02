using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SLS.MenuCore
{
    [RequireComponent(typeof(Image))]
    public class Overlay : MonoBehaviour
    {
        public static Overlay UnderHUD { get; private set; }
        public static Overlay BetweenUI { get; private set; }
        public static Overlay OverALL { get; private set; }

        public UILayer uiLayer;

        public static int ActiveOverlays { get; private set; }

        public static void Instantiate()
        {
            GameObject root = AssetRegistry.Prefab("Overlays").Instantiate();
            DontDestroyOnLoad(root);
            Overlay[] overlays = root.GetComponentsInChildren<Overlay>();
            for (int i = 0; i < overlays.Length; i++)
            {
                overlays[i].Awake();
                ActiveOverlays++;
            }
        }

        [SerializeField] protected Image image;
        [SerializeField] protected Animator animator;

        protected static int NullAnimationHash = Animator.StringToHash("Null");

        protected virtual void Awake()
        {
            if (uiLayer is UILayer.OverEVERYTHING) OverALL = this;
            else if (uiLayer is UILayer.OverHUD) BetweenUI = this;
            else if (uiLayer is UILayer.UnderHUD) UnderHUD = this;
            else Destroy(this.gameObject);

            if (animator == null) animator = GetComponent<Animator>();
            if (image == null) image = GetComponent<Image>();
            image.color = Color.clear;
        }

        public float Alpha
        {
            get => image.color.a;
            set
            {
                image.color = new(image.color.r, image.color.g, image.color.b, value);
                image.raycastTarget = image.color.a == 1f;
            }
        }
        public Color Color
        {
            get => image.color;
            set => image.color = value;
        }

        protected Coroutine activeRoutine; public Coroutine ActiveRoutine => activeRoutine;

        public IEnumerator FadeAlpha(float dest, float time = 1f, bool adjustByCloseness = true)
        {
            float rate = (adjustByCloseness ? Mathf.Sign(dest - Alpha) : (dest - Alpha)) / time;
            Alpha += rate * Time.unscaledDeltaTime;
            yield return null;
            while (Alpha != dest && Alpha > 0 && Alpha < 1)
            {
                Alpha += rate * Time.unscaledDeltaTime;
                yield return null;
            }
            Alpha = dest;
        }
        public IEnumerator FadeColor(Color dest, float time = 1f)
        {
            Color baseColor = Color;
            float t = 0;
            float rate = 1 / time;
            t += rate * Time.unscaledDeltaTime;
            yield return null;
            while (t < 0)
            {
                t += rate * Time.unscaledDeltaTime;
                Color = Color.Lerp(baseColor, dest, t);
                yield return null;
            }
        }

        public void DoFadeAlpha(float dest, float time = 1f, bool adjustByCloseness = true) =>
            Coroutine.Begin(ref activeRoutine, FadeAlpha(dest, time, adjustByCloseness), true);
        public void DoFadeColor(Color dest, float time = 1f) =>
            Coroutine.Begin(ref activeRoutine, FadeColor(dest, time), true);







        

        public void PlayAnimation(string name)
        {
            animator.enabled = true;
            animator.Play(name);
        }
        public void PlayAnimation(int hash)
        {
            animator.enabled = true;
            animator.Play(hash);
        }
        public void DisableAnimation()
        {
            animator.Play(NullAnimationHash);
            animator.enabled = false;
        }

        public virtual void ResetState()
        {
            if (animator && animator.enabled) DisableAnimation();
            Alpha = 0f;
            Color = Color.clear;
            Coroutine.Stop(ref activeRoutine); 
        }
    }

}