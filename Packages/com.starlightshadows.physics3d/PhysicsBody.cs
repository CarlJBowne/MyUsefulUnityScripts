using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Cinemachine.Utility;
using RageRooster.Systems.SaveSystem;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using Utilities.Xtensions;
using Utilities.Xtensions.Unity;
using static UnityEngine.Rendering.DebugUI;

namespace RageRooster.Physics
{
    /// <summary>
    /// Core physics body component that owns per-entity physics state and delegates movement
    /// resolution to modular <see cref="PhysicsResolver"/> implementations. <br/>
    /// This component centralizes the Rigidbody/Collider/NavMeshAgent integration, exposes
    /// the high-level physical concepts (velocity, ground state, facing direction), and
    /// coordinates resolver selection and invocation each FixedUpdate.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(NavMeshAgent))]
    public class PhysicsBody : MonoBehaviour
    {
        protected virtual void FixedUpdate()
        {
            if (Gameplay.GameState != Gameplay.GameStates.Active || BodyState != BodyStates.Enabled) return;

            if (Debug.DisplayDebugString)
            {
                Debug.ResetDebugString();
                DebugRR.DebugTextOverlay.ClearText();
            }
            if (Debug.DisplaySweeps) Debug.ClearSweeps();

            RB.linearVelocity = Vector3.zero;
            RB.angularVelocity = Vector3.zero;

            Resolvers.Active?.FixedUpdateFormer();

            if (Velocity.r != 0f) Direction.RotationY += Velocity.r * Time.fixedDeltaTime;

            Vector3 stepZeroVelocity = Velocity.Global * Time.fixedDeltaTime;

            Step = 0;
            if (stepZeroVelocity.IsNaN() || stepZeroVelocity.sqrMagnitude > 300)
                stepZeroVelocity = Vector3.zero;

            Resolvers.Active?.Move(stepZeroVelocity);

            Resolvers.Active?.FixedUpdateLatter();

            //if (Velocity.y <= 0)
            //{
            //    if (Ground.Check(out AnchorPoint groundHit))
            //    {
            //        if (!Ground)
            //        {
            //            Ground.Land(groundHit);
            //            Velocity.y = 0;
            //        }
            //    }
            //    else if (Ground) Ground.UnLand(GroundState.Hangtime);
            //}

            if (Debug.DisplayDebugString) DebugRR.DebugTextOverlay.SetText(Debug);

        }

        /// <summary>
        /// The Serialized Tree of <see cref="PhysicsResolver"/>s available for this body. The <see cref="ResolverTree"/>
        /// </summary>
        [field: SerializeField] public ResolverTree Resolvers { get; private set; }

        /// <summary>
        /// The current velocity container for this body. Contains both local (f/s/u) and
        /// global (x/y/z) representations and helper methods to keep them in sync.
        /// </summary>
        [field: SerializeField] public Velocity Velocity { get; private set; }

        /// <summary>
        /// Current ground state for this body. Tracks whether the body is grounded, the
        /// anchor point (surface normal/point/collider) and exposes checks for ledges and
        /// slope limits.
        /// </summary>
        [field: SerializeField] public GroundState Ground { get; private set; }

        /// <summary>
        /// Direction helper that represents the local forward vector used for local
        /// velocity computations and rotation helpers.
        /// </summary>
        [field: SerializeField] public Direction Direction { get; private set; }
        /// <summary>
        /// Debug Data container for this body. Used to store and display useful debug information
        /// </summary>
        public PhysicsBodyDebug Debug { get; private set; } = new();


        #region Resolvers

        [Tooltip("The maximum amount of steps this resolver allows.")]
        [SerializeField] public int maxPhysicsSteps = 6;


        /// <summary>
        /// The amount of MoveSteps this <see cref="PhysicsBody"/> has gone through in this FixedUpdate sharedacross //all of its <see cref="PhysicsResolver"/>s
        /// </summary>
        public int Step { get; internal set; } = 0;

        #endregion

        /// <summary>
        /// Casts the Rigidbody in a direction to check for collision using SweepTest. (Includes optional buffer)
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="hit">The resulting Hit.</param>
        /// <param name="buffer">A buffer that the Rigidbody is temporarily moved backwards by before the Sweep Test.</param>
        /// <param name="tempOrigin">An optional temporary origin to move the Rigidbody to before the Sweep Test.</param>
        /// <param name="queryTriggerInteraction">Override to include trigger colliders in the Sweep Test.</param>
        /// <returns>Whether anything was Hit.</returns>
        /// <summary>
        /// Performs a sweep test using the internal Rigidbody to determine whether this
        /// body would collide when translated by <paramref name="offset"/>. Optionally
        /// supports a temporary origin and a buffer distance to shrink the effective start
        /// location for the sweep.
        /// </summary>
        /// <param name="offset">The desired translation vector to sweep along.</param>
        /// <param name="hit">Outputs the first RaycastHit detected by the sweep (if any).</param>
        /// <param name="buffer">A small buffer to back the test origin up along <paramref name="offset"/>. Defaults to 0.</param>
        /// <param name="tempOrigin">An optional temporary origin to perform the sweep from instead of the current RB position.</param>
        /// <param name="queryTriggerInteraction">Whether the sweep should hit trigger colliders. Defaults to Ignore.</param>
        /// <returns>True if the sweep detected a collider, otherwise false.</returns>
        public bool Sweep(Vector3 offset, out RaycastHit hit, float buffer = 0, Vector3? tempOrigin = null, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
        {
            Vector3 originalPos = RB.position;

            if (tempOrigin.HasValue && buffer > 0) RB.MovePosition(tempOrigin.Value - (offset.normalized * buffer));
            else if (tempOrigin.HasValue) RB.MovePosition(tempOrigin.Value);
            else if (buffer > 0) RB.MovePosition(RB.position - (offset.normalized * buffer));

            bool result = RB.SweepTest(offset.normalized, out hit, offset.magnitude + buffer, queryTriggerInteraction);

            if (tempOrigin.HasValue || buffer > 0) RB.MovePosition(originalPos);

            hit.distance = (hit.distance - buffer).Min(0);


            if (Debug.DisplaySweeps)
            {
                var display = new PhysicsBodyDebug.SweepTestDisplay()
                {
                    origin = !tempOrigin.HasValue ? Center : tempOrigin.Value + Offset,
                    direction = offset,
                    hit = result,
                    hitDistance = hit.distance,
                    hitNormal = hit.normal
                };
                Debug.Add(display);
            }

            return result;
        }

        #region LifeCycle and Components

        [field: SerializeField, RelatedComponent(true)] public Rigidbody RB { get; protected set; }
        /// <summary>
        /// The <see cref="CapsuleCollider"/> component attached to this <see cref="CharacterMovementBody"/>.
        /// </summary>
        [field: SerializeField, RelatedComponent(true)] public Collider Collider { get; protected set; }

        /// <summary>
        /// Unity Reset callback used to initialize related components when the component
        /// is first added or when Reset is invoked in the editor.
        /// </summary>
        protected virtual void Reset()
        {
            RB = GetComponent<Rigidbody>();
            Collider = GetComponent<CapsuleCollider>();
        }

        /// <summary>
        /// Unity Awake lifecycle event. Ensures required components exist, initializes
        /// subcomponents and resolves any initial ground snap.
        /// </summary>
        protected virtual void Awake()
        {
            if (RB == null) RB = GetComponent<Rigidbody>();
            if (Collider == null) Collider = GetComponent<Collider>();

            Ground.Init(this);

            Direction.Init(this);
            Velocity.Init(this);
            Debug.Init(this);
            Resolvers.Init(this);
            for (int i = 0; i < Resolvers.ResolverCount; i++)
            {
                Resolvers[i]?.Init(this);
                Resolvers[i]?.Start();
            }

            if (Resolvers.defaultGroundedIndex != -1 && Ground.InstantSnapToFloor(out RaycastHit hit))
            {
                Ground.Land(hit);
                Resolvers.Update(Resolvers.defaultGroundedIndex);
            }
            else if (Resolvers.defaultAirIndex != -1)
            {
                Resolvers.Update(Resolvers.defaultAirIndex);
            }
            else enabled = false; //WTF.
        }

        /// <summary>
        /// Called when the component is enabled.
        /// </summary>
        /// <summary>
        /// Unity OnEnable lifecycle event. Restores the active physics state if the body
        /// was previously turned off.
        /// </summary>
        void OnEnable() { if (_rbState == BodyStates.OFF) BodyState = BodyStates.Enabled; }
        /// <summary>
        /// Called when the component is disabled.
        /// </summary>
        /// <summary>
        /// Unity OnDisable lifecycle event. Puts the body into the OFF state which makes
        /// the Rigidbody kinematic and disables collision checks.
        /// </summary>
        void OnDisable() => BodyState = BodyStates.OFF;


        /// <summary>
        /// The possible states for a <see cref="CharacterMovementBody"/>.
        /// </summary>
        public enum BodyStates
        {
            Enabled,
            Ragdoll,
            OFF
        }

        /// <summary>
        /// The current state of this <see cref="CharacterMovementBody"/>.
        /// </summary>
        public BodyStates BodyState
        {
            get => _rbState;
            set
            {
                _rbState = value;
                switch (value)
                {
                    case BodyStates.Enabled:
                        RB.isKinematic = false;
                        RB.detectCollisions = true;
                        RB.useGravity = false;
                        Collider.enabled = true;
                        break;
                    case BodyStates.Ragdoll:
                        RB.isKinematic = false;
                        RB.detectCollisions = true;
                        RB.useGravity = true;
                        Collider.enabled = false;
                        break;
                    case BodyStates.OFF:
                        RB.isKinematic = true;
                        RB.detectCollisions = false;
                        RB.useGravity = false;
                        Collider.enabled = false;
                        break;
                }
            }
        }
        BodyStates _rbState = BodyStates.Enabled;

        #endregion LifeCycle

        #region Physicals

        /// <summary>
        /// Gets or sets the position of the character.
        /// </summary>
        public Vector3 Position
        {
            get => BodyState == BodyStates.Enabled
                ? Resolvers.Active is not PhysicsResolver.NavMesh N
                    ? RB.position
                    : N.NavAgent.nextPosition
                : transform.position;
            set
            {
                if (BodyState != BodyStates.Enabled) return;

                if (Resolvers.Active is PhysicsResolver.NavMesh N) N.NavAgent.nextPosition = value;
                else RB.MovePosition(value);
            }
        }

        /// <summary>
        /// Sets the position even if the Rigidbody is kinematic.
        /// </summary>
        /// <param name="newPosition">The new position.</param>
        public Vector3 PositionForce
        {
            set
            {
                transform.position = value;
                RB.position = value;
                RB.MovePosition(value);
            }
        }

        /// <summary>
        /// The center of the collider for this body.
        /// </summary>
        public Vector3 Center => Position +
            (Collider is CapsuleCollider cap ? cap.center
            : Collider is BoxCollider box ? box.center
            : Collider is SphereCollider sph ? sph.center
            : Vector3.zero
            );
        public Vector3 Offset =>
            Collider is CapsuleCollider cap ? cap.center
            : Collider is BoxCollider box ? box.center
            : Collider is SphereCollider sph ? sph.center
            : Vector3.zero
            ;

        /// <summary>
        /// Handles collision events with other objects.
        /// </summary>
        /// <param name="collision">The collision information.</param>
        /// <summary>
        /// Unity collision callback. Used to detect immediate contacts that should
        /// influence vertical velocity and potential landing when coming into contact
        /// with a surface during an airborne state.
        /// </summary>
        /// <param name="collision">Collision information provided by Unity.</param>
        void OnCollisionEnter(Collision collision)
        {
            Vector3 contactNormal = collision.GetContact(0).normal;
            if (!Ground && Velocity.y > .1f && Vector3.Dot(contactNormal, Vector3.up) < -0.75f) Velocity.y = 0;
            else if (!Ground && Ground.WithinSlopeAngle(contactNormal))
                Ground.Land(collision.GetContact(0));

        }

        #region LandPlugs

        public void Land() => Ground.Land();
        public void Land(AnchorPoint anchor) => Ground.Land(anchor);
        public void UnLand(GroundState.Values newValue = GroundState.Values.Falling) => Ground.UnLand(newValue);
        #endregion

        /// <summary>
        /// Called by <see cref="GroundState"/> when this body lands on a surface.
        /// Override to perform game-specific landing behavior. The default implementation
        /// will re-evaluate the active resolver.
        /// </summary>
        /// <param name="wasntGrounded">True if the body was previously not grounded.</param>
        /// <param name="objectChange">True if the collider surface changed since last ground.</param>
        public virtual void OnLand(bool wasntGrounded, bool objectChange) => Resolvers.Update();

        /// <summary>
        /// Called by <see cref="GroundState"/> when this body leaves the ground. Override
        /// to perform game-specific airborne entry behavior. The default implementation
        /// will re-evaluate the active resolver.
        /// </summary>
        /// <param name="newValue">The new ground state value being transitioned to.</param>
        public virtual void OnUnLand(GroundState.Values newValue) => Resolvers.Update();

        public virtual void WalkOff() => Ground.UnLand(GroundState.Values.Hangtime);
        public virtual bool LastChanceStopper(Vector3 velocity, Vector3 normal) => false;


        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmos() => Debug.DisplayGizmos();

        [CustomEditor(typeof(PhysicsBody))]
        public class Editor : UnityEditor.Editor
        {
            PhysicsBody This;

            public PropertyField ResolverField;
            public PropertyField GroundResolverField;
            public PropertyField AirResolverField;
            public PropertyField GroundCheckBufferField;
            public PropertyField MaxSlopeAngleField;
            public PropertyField AllowBackwardsVelocityField;

            public TabView TabView;
            public Tab ConfigTab;
            public Tab ActiveTab;
            public Tab DebugTab;

            public Label ResolverLabel;
            public Label LVelocityLabel;
            public Label GVelocityLabel;
            public Label DirectionLabel;
            public Label RotationLabel;
            public Label RotationQLabel;
            public Label GroundStateLabel;
            public Label AnchorLabel;

            private bool _subscribedToUpdate = false;

            public override VisualElement CreateInspectorGUI()
            {
                This = (PhysicsBody)target;

                TabView = new();
                MakeConfigTab();
                MakeActiveTab();
                MakeDebugTab();

                // Setup update loop for runtime info when in Play Mode
                void SubscribeUpdate()
                {
                    if (_subscribedToUpdate) return;
                    EditorApplication.update += EditorUpdate;
                    _subscribedToUpdate = true;
                }
                void UnsubscribeUpdate()
                {
                    if (!_subscribedToUpdate) return;
                    EditorApplication.update -= EditorUpdate;
                    _subscribedToUpdate = false;
                }

                // Initial subscription if playing
                if (EditorApplication.isPlaying) SubscribeUpdate();
                else UnsubscribeUpdate();

                // When inspector is created, also ensure we react to play mode changes to start/stop updating
                EditorApplication.playModeStateChanged += (state) =>
                {
                    if (state == PlayModeStateChange.EnteredPlayMode)
                    {
                        if (ActiveTab == null) MakeActiveTab();
                        if (DebugTab == null) MakeDebugTab();
                        SubscribeUpdate();
                    }
                    else if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
                    {
                        if (ActiveTab != null)
                        {
                            TabView.Remove(ActiveTab);
                            ActiveTab = null;
                        }
                        if (DebugTab != null)
                        {
                            TabView.Remove(DebugTab);
                            DebugTab = null;
                        }
                        UnsubscribeUpdate();
                    }
                };

                return TabView;
            }

            public virtual void MakeConfigTab()
            {
                ConfigTab = new("Config");
                ConfigTab.tabHeader.style.flexGrow = 1;
                TabView.Add(ConfigTab);

                ResolverField = new(serializedObject.FindProperty(nameof(PhysicsBody.Resolvers).BackingField()).FindPropertyRelative("resolvers"));
                GroundResolverField = new(serializedObject.FindProperty(nameof(PhysicsBody.Resolvers).BackingField()).FindPropertyRelative(nameof(ResolverTree.defaultGroundedIndex).BackingField()));
                AirResolverField = new(serializedObject.FindProperty(nameof(PhysicsBody.Resolvers).BackingField()).FindPropertyRelative(nameof(ResolverTree.defaultAirIndex).BackingField()));

                GroundCheckBufferField = new(serializedObject.FindProperty
                    (nameof(Ground).BackingField()).FindPropertyRelative(nameof(GroundState.groundCheckBuffer).BackingField()));
                MaxSlopeAngleField = new(serializedObject.FindProperty
                    (nameof(Ground).BackingField()).FindPropertyRelative(nameof(GroundState.maxSlopeNormalAngle).BackingField()));
                AllowBackwardsVelocityField = new(serializedObject.FindProperty(nameof(Velocity).BackingField()).FindPropertyRelative(nameof(Velocity.allowBackwards)));

                ConfigTab.Add(ResolverField);
                ConfigTab.Add(GroundResolverField);
                ConfigTab.Add(AirResolverField);
                ConfigTab.Add(GroundCheckBufferField);
                ConfigTab.Add(MaxSlopeAngleField);
                ConfigTab.Add(AllowBackwardsVelocityField);
            }
            public virtual void MakeActiveTab()
            {
                if (!Application.isPlaying) return;
                ActiveTab = new("Active");
                ActiveTab.tabHeader.style.flexGrow = 1;
                TabView.Add(ActiveTab);

                ResolverLabel = CreateDisplayRow("Resolver:");
                LVelocityLabel = CreateDisplayRow("Local Velocity:");
                GVelocityLabel = CreateDisplayRow("Global Velocity:");
                DirectionLabel = CreateDisplayRow("Direction:");
                RotationLabel = CreateDisplayRow("Rotation:");
                RotationQLabel = CreateDisplayRow("Quaternion:");
                GroundStateLabel = CreateDisplayRow("Ground State:");
                AnchorLabel = CreateDisplayRow("Current Anchor:");
            }
            public virtual void MakeDebugTab()
            {
                if (!Application.isPlaying) return;
                DebugTab = new("Debug");
                DebugTab.tabHeader.style.flexGrow = 1;
                TabView.Add(DebugTab);

                Toggle String = new("Debug String Builder");
                String.RegisterValueChangedCallback(ev => This.Debug.DisplayDebugString = ev.newValue);
                DebugTab.Add(String);

                Toggle Sweeps = new("Body Sweeps");
                String.RegisterValueChangedCallback(ev => This.Debug.DisplaySweeps = ev.newValue);
                DebugTab.Add(Sweeps);

                Toggle Hits = new("Collision Normals");
                String.RegisterValueChangedCallback(ev => This.Debug.DisplayHitNormals = ev.newValue);
                DebugTab.Add(Hits);

                Toggle Jumps = new("Jump Marker");
                String.RegisterValueChangedCallback(ev => This.Debug.DisplayJumpMarker = ev.newValue);
                DebugTab.Add(Jumps);

                Toggle Nav = new("Closest Nav Mesh Edge");
                String.RegisterValueChangedCallback(ev => This.Debug.DisplayClosestNavEdge = ev.newValue);
                DebugTab.Add(Nav);
            }

            public Label CreateDisplayRow(string name)
            {
                VisualElement row = new();
                row.style.flexDirection = FlexDirection.Row;
                Label label = new(name);
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.width = new Length(30, LengthUnit.Percent);
                Label result = new("Value");
                label.style.unityFontStyleAndWeight = FontStyle.Italic;
                result.style.width = new Length(70, LengthUnit.Percent);
                row.Add(label);
                row.Add(result);
                ActiveTab.Add(row);
                return result;
            }

            private void OnDisable()
            {
                if (_subscribedToUpdate)
                {
                    EditorApplication.update -= EditorUpdate;
                    _subscribedToUpdate = false;
                }
            }

            private void EditorUpdate()
            {
                if (serializedObject == null) return;
                if (This == null) return;

                // Update textual info; guard with try/catch to avoid throwing during domain reloads
                try
                {
                    ResolverLabel.text = $"{This.Resolvers.IndexOf(This.Resolvers.Active)} ({This.Resolvers.Active.GetType().Name})";
                    LVelocityLabel.text = $" F:{This.Velocity.f}, U:{This.Velocity.u}, S:{This.Velocity.s}";
                    GVelocityLabel.text = $" X:{This.Velocity.x}, Y:{This.Velocity.y}, Z:{This.Velocity.z}";
                    DirectionLabel.text = This.Direction.value.ToString("F3");
                    RotationLabel.text = This.Direction.Rotation.ToString("F3");
                    RotationQLabel.text = This.Direction.RotationQ.ToString("F2");
                    GroundStateLabel.text = This.Ground.value.ToString();
                    AnchorLabel.text = This.Ground.anchor.collider != null
                        ? $"{This.Ground.anchor.normal.ToString("F2")}({This.Ground.anchor.collider.gameObject.name})"
                        : This.Ground.anchor.normal.ToString("F2");
                }
                catch
                {
                    // swallow exceptions during assembly reloads / domain changes
                }
            }

        }
#endif
    }
}
