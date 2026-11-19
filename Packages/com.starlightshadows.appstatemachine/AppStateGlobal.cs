using SLS.ListUtilities;
using SLS.Singletons;
using UnityEngine;

namespace SLS.AppStateMachine
{
    [DefaultExecutionOrder(-155)]
    public abstract class AppStateGlobal<T> : AppState where T : AppState
    {
        /// Backing field for the late object singleton instance.
        /// </summary>
        static readonly Singleton<T> S = new();

        /// <summary>
        /// Gets the registered singleton instance, attempting any configured creation paths if necessary.
        /// </summary>
        public static T Get => S.Get;

        /// <summary>
        /// Whether an instance of this Singleton Type is Active.
        /// </summary>
        public static bool Present => S.Active;

        public static bool Active => S.Active && S.Get.isActive;

        /// <summary>
        /// Attempts to get the currently registered singleton instance.
        /// </summary>
        /// <param name="instance">Out parameter that receives the instance if present.</param>
        /// <returns>True if an instance is present; otherwise false.</returns>
        public static bool TryGet(out T instance) => S.TryGet(out instance);

        public override void OnEnable()
        {
            base.OnEnable();
            Singleton.OperationMessage res = S.Register(this as T);
            if (res != Singleton.OperationMessage.Success) return;
        }
    }

    [DefaultExecutionOrder(-155)]
    public abstract class AppSubStateGlobal<T> : AppSubState where T : AppSubState
    {
        /// Backing field for the late object singleton instance.
        /// </summary>
        static readonly Singleton<T> S = new();

        /// <summary>
        /// Gets the registered singleton instance, attempting any configured creation paths if necessary.
        /// </summary>
        public static T Get => S.Get;

        /// <summary>
        /// Whether an instance of this Singleton Type is Active.
        /// </summary>
        public static bool Present => S.Active;

        public static bool Active => S.Get.isActive;

        /// <summary>
        /// Attempts to get the currently registered singleton instance.
        /// </summary>
        /// <param name="instance">Out parameter that receives the instance if present.</param>
        /// <returns>True if an instance is present; otherwise false.</returns>
        public static bool TryGet(out T instance) => S.TryGet(out instance);

        public override void OnEnable()
        {
            base.OnEnable();
            Singleton.OperationMessage res = S.Register(this as T);
            if (res != Singleton.OperationMessage.Success) return;
        }
    }
}