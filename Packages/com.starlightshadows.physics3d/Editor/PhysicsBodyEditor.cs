using SLS.EditorUtilities.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
//using Utilities.Xtensions;
//using Utilities.Xtensions.Unity;

namespace SLS.Physics
{

    [CustomEditor(typeof(PhysicsBody))]
    public class PhysicsBodyEditor : UnityEditor.Editor
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
                (nameof(PhysicsBody.Ground).BackingField()).FindPropertyRelative(nameof(GroundState.groundCheckBuffer).BackingField()));
            MaxSlopeAngleField = new(serializedObject.FindProperty
                (nameof(PhysicsBody.Ground).BackingField()).FindPropertyRelative(nameof(GroundState.maxSlopeNormalAngle).BackingField()));
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

}