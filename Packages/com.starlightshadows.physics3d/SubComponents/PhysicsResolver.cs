using System;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using Unity.VisualScripting;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace SLS.Physics3D
{
    /// <summary>
    /// Abstract base class for movement resolvers. A resolver is responsible for translating a proposed movement vector into collisions, sliding, landing and other movement effects for its owning <see cref="PhysicsBody"/>.
    /// </summary>
    [System.Serializable, RequireComponent(typeof(PhysicsBody))]
    public abstract class PhysicsResolver : MonoBehaviour
    {
        #region Relations

        /// <summary>
        /// The owning PhysicsBody instance. Available after <see cref="Init"/> is called.
        /// </summary>
        [field: SerializeField] public PhysicsBody Body { get; private set; }

        /// <summary>
        /// Convenience properties that forward to the owning body. These provide quick
        /// access to common state frequently used by resolvers.
        /// </summary>
        protected Vector3 Position => Body.Position;
        protected Velocity stepZeroVelocity => Body.Velocity;
        protected GroundState Ground => Body.Ground;
        protected AnchorPoint anchor => Body.Ground.anchor;
        protected Direction direction => Body.Direction;
        protected PhysicsResolver Next => Body.Resolver;

        #endregion

        public virtual void Reset()
        {
            if (!TryGetComponent(out PhysicsBody pb))
            { DestroyImmediate(this); return; }
            this.Body = pb;
        }

        /// <summary>
        /// Lifecycle hooks and the main Move contract for resolvers.
        /// </summary>
        public virtual void OnStart() { }
        /// <summary>
        /// Called when this resolver becomes the active resolver for a PhysicsBody.
        /// </summary>
        public virtual void Enter() { }
        /// <summary>
        /// Called when this resolver is no longer active for a PhysicsBody.
        /// </summary>
        public virtual void Exit() { }
        /// <summary>
        /// Called before the main resolver Move invocation for per-frame setup.
        /// </summary>
        public virtual void FixedUpdateFormer() { }
        /// <summary>
        /// Called after the main resolver Move invocation for per-frame teardown.
        /// </summary>
        public virtual void FixedUpdateLatter() { }
        /// <summary>
        /// Process the supplied movement vector (<paramref name="stepVelocity"/>)
        /// for this resolver's domain. The implementation is responsible for
        /// performing collision sweeps, updating body position and optionally
        /// delegating remaining movement to the next resolver via the owning
        /// body's resolver selection.
        /// </summary>
        /// <param name="stepVelocity">The movement vector to process, typically velocity * deltaTime.</param>
        public abstract void Move(Vector3 stepVelocity);

        public bool ContinueCheck(Vector3 vel) =>
            vel.sqrMagnitude < float.Epsilon || ++Body.Step >= Body.maxPhysicsSteps;
        public bool ContinueCheck(float hitDistance) =>
            hitDistance == -1 || ++Body.Step >= Body.maxPhysicsSteps;

        public void ChooseNext() => Body.UpdateResolver();
        public void ChooseNext(PhysicsResolver target) => Body.UpdateResolver(target);

        public static implicit operator bool(PhysicsResolver P) => P != null;

        protected void Print(Func<string> value)
        {
#if UNITY_EDITOR
            if (!Body.Debug.DisplayDebugString) return;
            Body.Debug.AppendLine(value?.Invoke());
#endif
        }

        [ContextMenu("Hide")]
        public void Hide()
        {
            this.hideFlags = HideFlags.HideInInspector;
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomPropertyDrawer(typeof(PhysicsResolver), true)]
    public class Editor : UnityEditor.PropertyDrawer
    {
        static List<UnityEngine.Object> shown = new();

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Localize everything to avoid reuse issues with PropertyDrawer instances.
            var root = new VisualElement();

            var foldout = new Foldout
            {
                text = property.displayName,
                value = false
            };
            foldout.BindProperty(property);
            foldout.SetValueWithoutNotify(false);
            root.Add(foldout);

            var getButton = new Button(ButtonPress)
            {
                style =
                {
                    width = new Length(40, LengthUnit.Percent),
                    height = 14,
                    position = Position.Absolute,
                    top = 0,
                    right = 0,
                }
            };
            root.Add(getButton);

            void UpdateButtonText()
            {
                getButton.text = property.objectReferenceValue != null
                    ? property.objectReferenceValue.GetType().Name.Replace("PhysResolver", "")
                    : "Select";
            }
            UpdateButtonText();

            // Register the foldout callback after attachment to panel (mirrors previous intent).
            foldout.RegisterCallbackOnce<AttachToPanelEvent>(c =>
                foldout.schedule.Execute(() => foldout.RegisterValueChangedCallback(FoldoutChange))
            );

            void FoldoutChange(ChangeEvent<bool> e)
            {
                if (e.target != e.currentTarget) return;
                UpdateButtonText();

                if (e.newValue)
                {
                    var target = property.objectReferenceValue;
                    if (target != null && shown.Contains(target)) return;

                    if (target != null) shown.Add(target);

                    if (target != null)
                        foldout.Add(new InspectorElement(new SerializedObject(target)));
                }
                else
                {
                    var target = property.objectReferenceValue;
                    if (target != null && shown.Contains(target)) shown.Remove(target);
                    foldout.contentContainer.Clear();
                }
            }

            void ButtonPress()
            {
                var menu = new GenericMenu();

                PhysicsResolver[] existingResolvers = (property.serializedObject.targetObject as Component).GetComponents<PhysicsResolver>();
                for (int i = 0; i < existingResolvers.Length; i++)
                {
                    int t = i;
                    menu.AddItem(new($"{i + 1} : {existingResolvers[t].GetType().Name.Replace("PhysResolver", "")}"),
                        property.objectReferenceValue == existingResolvers[t],
                        () => PostMenuE(existingResolvers[t]));
                }

                menu.AddSeparator("");

                Type[] subTypes = GetSubtypes();
                for (int i = 0; i < subTypes.Length; i++)
                {
                    int t = i;
                    menu.AddItem(new($"+ {subTypes[t].Name.Replace("PhysResolver", "")}"), false, () => PostMenuN(subTypes[t]));
                }

                if (property.objectReferenceValue != null)
                {
                    menu.AddSeparator("");
                    menu.AddItem(new("Nullify"), false, PostMenuNull);
                }

                menu.ShowAsContext();
            }

            void PostMenuE(PhysicsResolver input)
            {
                property.objectReferenceValue = input;
                property.serializedObject.ApplyModifiedProperties();
                // Expand so the user can see the newly selected resolver
                foldout.value = true;
            }
            void PostMenuN(Type targetType)
            {
                PhysicsResolver newRes = (property.serializedObject.targetObject as Component).gameObject.AddComponent(targetType) as PhysicsResolver;
                property.objectReferenceValue = newRes;
                newRes.hideFlags = HideFlags.HideInInspector;
                property.serializedObject.ApplyModifiedProperties();
                foldout.value = true;
            }
            void PostMenuNull()
            {
                property.objectReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();
                foldout.value = true;
            }

            return root;
        }

        public static Type[] GetSubtypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .Where(t =>
                    !t.IsAbstract &&
                    // For interfaces, include implementers; for classes, include strict subclasses only.
                    t.IsSubclassOf(typeof(PhysicsResolver)) && (t.IsPublic || t.IsNestedPublic || t.IsNestedFamORAssem || t.IsNestedFamily)
                )
                .ToArray();
        }
    }
#endif
}