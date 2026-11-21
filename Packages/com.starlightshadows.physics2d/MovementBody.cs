using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SLS.EditorUtilities.ComponentHeaders;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace SLS.Physics2D
{
    /// <summary>
    /// Core Moving Body component that owns per-entity physics state and delegates movement
    /// resolution to modular <see cref="PhysicsResolver"/> implementations. <br/>
    /// This component centralizes the Rigidbody2D/Collider2D/NavMeshAgent integration, exposes
    /// the high-level physical concepts (velocity, ground state, facing direction), and
    /// coordinates resolver selection and invocation each FixedUpdate.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(NavMeshAgent))]
    public partial class MovementBody : MonoBehaviour
    {
        protected virtual void FixedUpdate()
        {
            if (DISABLE_PHYSICS_BODIES) return;

#if UNITY_EDITOR
            if (Debug.DisplayDebugString)
            {
                Debug.ResetDebugString();
                //DebugRR.DebugTextOverlay.ClearText();
            }
            if (Debug.DisplaySweeps) Debug.ClearSweeps();
#endif

            CancelResolverContinuance();

            RB.linearVelocity = Vector2.zero;
            RB.angularVelocity = 0;

            Resolver?.FixedUpdateFormer();

            if (Direction.lookTarget != null && Velocity.lookVelocity > 0f)
                Direction.Set(Direction.lookTarget.position.To2() - Position, Velocity.lookVelocity * Time.fixedDeltaTime);
            else if (Velocity.r != 0f) Direction.RotationY += Velocity.r * Time.fixedDeltaTime;

            Vector2 stepZeroVelocity = Velocity.Global * Time.fixedDeltaTime;

            Step = 0;
            if (stepZeroVelocity.sqrMagnitude > 500) stepZeroVelocity = Vector2.zero;

            Resolver?.Move(stepZeroVelocity);

            Resolver?.FixedUpdateLatter();

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

            //if (Debug.DisplayDebugString) DebugRR.DebugTextOverlay.SetText(Debug);

        }

        [field: SerializeField] public List<PhysicsResolver> resolvers { get; private set; } = new();
        /// <summary>
        /// The default grounded <see cref="PhysicsResolver"/>, the first one it will attempt to use in any grounded situation.
        /// </summary>
        [field: SerializeField] public PhysicsResolver.GroundType groundResolver { get; private set; }
        /// <summary>
        /// The root Airborne PhysicsResolver, the first one it will attempt to use in airborne situations.
        /// </summary>
        [field: SerializeField] public PhysicsResolver.AirType airResolver { get; private set; }

        [Tooltip("The maximum amount of steps this resolver allows.")]
        [SerializeField] public int maxPhysicsSteps = 6;
        /// <summary>
        /// The buffer (in world units) used when performing a downwards sweep to
        /// determine whether the body is grounded. Small positive values help
        /// tolerate minor geometry gaps and numerical jitter.
        /// </summary>
        [field: SerializeField] public float defaultCheckBuffer { get; private set; } = 0.1f;

        #region Resolvers

        public PhysicsResolver Resolver => resolver;
        private PhysicsResolver resolver = null;
        public void SelectGroundResolver() => SelectResolver(groundResolver);
        public void SelectAirResolver() => SelectResolver(airResolver);
        public void SelectResolver(bool airborne) => SelectResolver(airborne ? airResolver : groundResolver);
        public void SelectResolver(PhysicsResolver newResolver)
        {
            if (newResolver == Resolver || newResolver == null) return;

            PhysicsResolver prev = Resolver;
            newResolver.TryEnter(ref resolver);
            if (prev == resolver) return;
            prev?.OnExit();
        }

        /// <summary>
        /// The amount of MoveSteps this <see cref="MovementBody"/> has gone through in this FixedUpdate sharedacross / all of its <see cref="PhysicsResolver"/>s
        /// </summary>
        public int Step { get; internal set; } = 0;

        #endregion

        #region Sweeps

        /// <summary>
        /// Performs a sweep test using the internal Rigidbody2D to determine whether this
        /// body would collide when translated by <paramref name="offset"/>. Optionally
        /// supports a temporary origin and a buffer distance to shrink the effective start
        /// location for the sweep.
        /// </summary>
        /// <param name="offset">The desired translation vector to sweep along.</param>
        /// <param name="hit">Outputs the first RaycastHit2D detected by the sweep (if any).</param>
        /// <param name="buffer">A small buffer to back the test origin up along <paramref name="offset"/>. Defaults to 0.</param>
        /// <param name="tempOrigin">An optional temporary origin to perform the sweep from instead of the current RB position.</param>
        /// <param name="queryTriggerInteraction">Whether the sweep should hit trigger Collider2Ds. Defaults to Ignore.</param>
        /// <returns>True if the sweep detected a Collider2D, otherwise false.</returns>
        public PhysicsSweep Sweep(Vector2 offset, float? buffer = null, Vector2? tempOrigin = null, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
        {
            buffer ??= defaultCheckBuffer;
            Vector2 originalPos = RB.position;

            Vector2? startPos = null;
            if (tempOrigin.HasValue && buffer > 0) startPos = (tempOrigin.Value - (offset.normalized * buffer.Value));
            else if (tempOrigin.HasValue) startPos = (tempOrigin.Value);
            else if (buffer > 0) startPos = (RB.position - (offset.normalized * buffer.Value));

            bool didHit = !startPos.HasValue
                ? RB.Cast(offset.normalized, filter, sweepHits, offset.magnitude) > 0
                : RB.Cast(startPos.Value, RB.rotation, offset.normalized, filter, sweepHits, offset.magnitude) > 0;

#if UNITY_EDITOR
            if (Debug.DisplaySweeps)
            {
                var display = new PhysicsBodyDebug.SweepTestDisplay()
                {
                    origin = Center,
                    direction = offset,
                    hit = didHit,
                    hitDistance = didHit ? sweepHits[0].distance : -1,
                    hitNormal = didHit ? sweepHits[0].normal : default
                };
                Debug.Add(display);
            }
#endif

            return didHit ? new()
            {
                hit = true,
                distance = sweepHits[0].distance,
                normal = sweepHits[0].normal,
                angle = Direction.Angle(sweepHits[0].normal),
                collisions = sweepHits,
                input = offset,
                leftover = offset.normalized * (offset.magnitude - sweepHits[0].distance),
                anchorPoint = new(sweepHits[0].point, sweepHits[0].normal, sweepHits[0].collider)
            }
            : new()
            {
                hit = false,
                distance = -1,
                normal = Vector2.zero,
                angle = AnchorPoint.Angle.Null,
                collisions = { },
                input = offset,
                leftover = default,
                anchorPoint = default
            };
        }

        public bool Sweep(Vector2 offset, out PhysicsSweep result, bool relative = false, float? buffer = null, Vector2? tempOrigin = null, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
        {
            result = Sweep(offset, buffer, tempOrigin, queryTriggerInteraction);
            return result.hit;
        }

        private readonly List<RaycastHit2D> sweepHits = new();
        ContactFilter2D filter = new()
        {
            useLayerMask = false,
            useDepth = false,
            useNormalAngle = false,

        };

        #endregion

        #region LifeCycle and Components

        [field: SerializeField, HeaderItem(true)] public Rigidbody2D RB { get; internal set; }
        [field: SerializeField, HeaderItem(true)] public Collider2D Collider { get; internal set; }

        /// <summary>
        /// The current velocity container for this body. Contains both local (f/s/u) and
        /// global (x/y/z) representations and helper methods to keep them in sync.
        /// </summary>
        [field: SerializeField] public Velocity Velocity { get; private set; }
        /// <summary>
        /// Current <see cref="AnchorState"/> for this body. Tracks whether the body is grounded, the
        /// anchor point (surface normal/point/Collider2D) and exposes checks for ledges and
        /// slope limits.
        /// </summary>
        [field: SerializeField] public AnchorState Anchor { get; private set; } = new();
        /// <summary>
        /// Direction helper that represents the local forward vector used for local
        /// velocity computations and rotation helpers.
        /// </summary>
        [field: SerializeField] public Direction Direction { get; private set; }
        /// <summary>
        /// Debug Data container for this body. Used to store and display useful debug information
        /// </summary>
        [field: SerializeField] public PhysicsBodyDebug Debug { get; private set; } = new();


        /// <summary>
        /// Unity Reset callback used to initialize related components when the component
        /// is first added or when Reset is invoked in the editor.
        /// </summary>
        protected virtual void Reset() => HeaderItemAttribute.Reset(this);

        /// <summary>
        /// Unity Awake lifecycle event. Ensures required components exist, initializes
        /// subcomponents and resolves any initial ground snap.
        /// </summary>
        protected virtual void Awake()
        {
            if (RB == null) RB = GetComponent<Rigidbody2D>();
            if (Collider == null) Collider = GetComponent<Collider2D>();

            Anchor.Init(this);

            Direction.Init(this);
            Velocity.Init(this);
            Debug.Init(this);
            if (resolvers.Count == 0) resolvers = gameObject.GetComponents<PhysicsResolver>().ToList();
            for (int i = 0; i < resolvers.Count; i++) if (resolvers[i]) resolvers[i].OnStart();

            if (groundResolver && groundResolver.TryEnter(ref resolver)) { }
            else if (airResolver && airResolver.TryEnter(ref resolver)) { }
            else if (resolvers.Count > 0 && resolvers[0] && resolvers[0].TryEnter(ref resolver)) { }
            else
            {
                resolver = null;
                enabled = false;
            }
        }

        void OnEnable()
        {
            RB.bodyType = RigidbodyType2D.Dynamic;
            RB.simulated = true;
            RB.gravityScale = 0;
            Collider.enabled = true;
            SelectGroundResolver();
        }
        void OnDisable()
        {
            RB.bodyType = RigidbodyType2D.Kinematic;
            RB.simulated = false;
            RB.gravityScale = 0;
            Collider.enabled = false;
            SelectResolver(null);
        }

        public void Enable(Vector2? atPosition, Vector2? withDirection)
        {
            if (atPosition.HasValue) Position = atPosition.Value;
            if (withDirection.HasValue) Direction.Set(withDirection.Value);
            enabled = true;
        }


        #endregion LifeCycle

        #region Physicals

        /// <summary>
        /// Gets or sets the position of the character.
        /// </summary>
        public Vector2 Position
        {
            get => enabled
                ? RB.position
                : transform.position;
            set
            {
                if (enabled) RB.MovePosition(value);
                else
                {
                    transform.position = value;
                    RB.position = value;
                    RB.MovePosition(value);
                }

            }
        }

        /// <summary>
        /// The center of the Collider2D for this body.
        /// </summary>
        public Vector2 Center => Position +
            (Collider is CapsuleCollider2D cap ? cap.offset
            : Collider is BoxCollider2D box ? box.offset
            : Collider is CircleCollider2D sph ? sph.offset
            : Vector2.zero
            );
        public Vector2 Offset =>
              Collider is CapsuleCollider2D cap ? cap.offset
            : Collider is BoxCollider2D box ? box.offset
            : Collider is CircleCollider2D sph ? sph.offset
            : Vector2.zero
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
        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            ContactPoint2D contact = collision.GetContact(0);
            if (Anchor.Airborne && Anchor.Rising && Direction.Angle(contact.normal) is AnchorPoint.Angle.Ceiling)
                Velocity.y = 0;
            else Anchor.Land(contact);
        }

        /// <summary>
        /// Called by <see cref="AnchorState"/> when this body lands on a surface.
        /// Override to perform game-specific landing behavior. The default implementation
        /// will re-evaluate the active resolver.
        /// </summary>
        /// <param name="wasntGrounded">True if the body was previously not grounded.</param>
        /// <param name="objectChange">True if the Collider2D surface changed since last ground.</param>
        public virtual void OnAnchor(bool wasntGrounded, bool objectChange) => SelectGroundResolver();

        /// <summary>
        /// Called by <see cref="AnchorState"/> when this body leaves the ground. Override
        /// to perform game-specific airborne entry behavior. The default implementation
        /// will re-evaluate the active resolver.
        /// </summary>
        /// <param name="newValue">The new ground state value being transitioned to.</param>
        public virtual void OnDeanchor() => SelectGroundResolver();

        public virtual void WalkOff() => Anchor.DeAnchor();

        protected bool cancelResolverContinuance = false;
        public bool CancelResolverContinuance(bool set = false)
        {
            if (set) cancelResolverContinuance = true;
            return cancelResolverContinuance;
        }

        public virtual void CollisionOverride(ref PhysicsSweep payload)
        { for (int i = 0; i < collisionOverrides.Count; i++) collisionOverrides[i].CollisionOverride(ref payload); }

        public List<ICollisionOverride> collisionOverrides { get; private set; } = new();

        #endregion

        public static bool DISABLE_PHYSICS_BODIES = false;

#if UNITY_EDITOR
        private void OnDrawGizmos() => Debug.DisplayGizmos();

        [CustomEditor(typeof(MovementBody), true)]
        public class Editor : UnityEditor.Editor
        {
            MovementBody This;

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

            protected bool generateFurtherDefaults = true;

            private bool _subscribedToUpdate = false;

            public override VisualElement CreateInspectorGUI()
            {
                This = (MovementBody)target;

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

                ConfigTab.Add(new PropertyField(serializedObject.FindBackingField(nameof(groundResolver))));
                ConfigTab.Add(new PropertyField(serializedObject.FindBackingField(nameof(airResolver))));
                ConfigTab.Add(new PropertyField(serializedObject.FindBackingField(nameof(resolvers))));
                ConfigTab.Add(new PropertyField(serializedObject.FindBackingField(nameof(maxPhysicsSteps))));
                ConfigTab.Add(new PropertyField(serializedObject.FindBackingField(nameof(defaultCheckBuffer))));
                ConfigTab.Add(new PropertyField(serializedObject.FindBackingField(nameof(Velocity))
                    .FindPropertyRelative(nameof(Velocity.allowBackwards))));

                Foldout AnglesFoldout = new()
                {
                    text = "Angle Thresholds",
                    value = false,
                };
                ConfigTab.Add(AnglesFoldout);
                SerializedProperty direction = serializedObject.FindBackingField(nameof(Direction));
                AnglesFoldout.Add(new PropertyField(direction.FindBackingFieldRelative(nameof(Direction.angleSlope)))
                { tooltip = "The minimum Angle from this Body's UP direction that is considered a slope rather than just a floor." });
                AnglesFoldout.Add(new PropertyField(direction.FindBackingFieldRelative(nameof(Direction.angleStandable)))
                { tooltip = "The maximum angle from this Body's UP direction that is considered standable." });
                AnglesFoldout.Add(new PropertyField(direction.FindBackingFieldRelative(nameof(Direction.angleWall)))
                { tooltip = "The minimum Angle from this Body's UP direction that is considered a wall rather than a slope." });
                AnglesFoldout.Add(new PropertyField(direction.FindBackingFieldRelative(nameof(Direction.angleInvertedSlope)))
                { tooltip = "The minimum Angle from this Body's UP direction that is considered an inverted slope rather than a wall." });
                AnglesFoldout.Add(new PropertyField(direction.FindBackingFieldRelative(nameof(Direction.angleCeiling)))
                { tooltip = "The minimum Angle from this Body's UP direction that is considered a ceiling." });

                if (!generateFurtherDefaults) return;
                SerializedProperty find = serializedObject.GetIterator();
                find.NextVisible(true);
                while (find.NextVisible(false)) if (find.name == nameof(Debug).BackingField()) break;
                while (find.NextVisible(false)) ConfigTab.Add(new PropertyField(find));
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
                if (!This.enabled) return;

                // Update textual info; guard with try/catch to avoid throwing during domain reloads
                try
                {
                    ResolverLabel.text = This.Resolver.GetType().Name.Replace("PhysResolver", "");
                    LVelocityLabel.text = $" F:{This.Velocity.f}, U:{This.Velocity.u}";
                    GVelocityLabel.text = $" X:{This.Velocity.x}, Y:{This.Velocity.y}";
                    DirectionLabel.text = This.Direction.Value.ToString("F2");
                    RotationLabel.text = This.Direction.Rotation.ToString();
                    GroundStateLabel.text = This.Anchor.ToString();
                    AnchorLabel.text = This.Anchor.AnchorPoint.Collider2D != null
                        ? $"{This.Anchor.AnchorPoint.normal.ToString("F2")}({This.Anchor.AnchorPoint.Collider2D.gameObject.name})"
                        : This.Anchor.AnchorPoint.normal.ToString("F2");
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
