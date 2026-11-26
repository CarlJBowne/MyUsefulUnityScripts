using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
//using Utilities.Xtensions;
//

namespace SLS.Physics2D
{
    [System.Serializable]
    public class PhysicsBodyDebug : PhysicsSubComponent
    {
#if UNITY_EDITOR

        public bool DisplayDebugString
        {
            get => StringBuilder != null;
            set => StringBuilder = value ? new() : null;
        }
        public StringBuilder StringBuilder;
        public void ResetDebugString()
        {
            if (!DisplayDebugString) return;
            StringBuilder.Clear();
        }
        public void Append(string value)
        {
            if (!DisplayDebugString) return;
            StringBuilder.Append(value);
        }
        public void AppendLine(string value)
        {
            if (!DisplayDebugString) return;
            StringBuilder.AppendLine(value);
        }
        public static implicit operator string(PhysicsBodyDebug D)
            => D == null || !D.DisplayDebugString ? null : D.StringBuilder.ToString();


        public bool DisplaySweeps
        {
            get => SweepsThisUpdate != null;
            set => SweepsThisUpdate = value ? new() : null;
        }
        public List<SweepTestDisplay> SweepsThisUpdate;
        public void Add(SweepTestDisplay v)
        {
            if (!DisplaySweeps) return;
            SweepsThisUpdate.Add(v);
        }
        public void ClearSweeps() => SweepsThisUpdate.Clear();
        public static explicit operator List<SweepTestDisplay>(PhysicsBodyDebug D)
            => D == null || !D.DisplaySweeps ? null : D.SweepsThisUpdate;
        public struct SweepTestDisplay
        {
            public Vector2 origin;
            public Vector2 direction;
            public bool hit;
            public float hitDistance;
            public Vector2 hitNormal;
        }


        public bool DisplayHitNormals
        {
            get => HitNormalsThisUpdate != null;
            set => HitNormalsThisUpdate = value ? new() : null;
        }
        public int maxHitDisplays = 15;
        public List<HitNormalDisplay> HitNormalsThisUpdate;
        public void Add(HitNormalDisplay v)
        {
            if (!DisplayHitNormals) return;
            HitNormalsThisUpdate.Add(v);
            if (HitNormalsThisUpdate.Count > maxHitDisplays)
                HitNormalsThisUpdate.RemoveAt(0);
        }
        public static explicit operator List<HitNormalDisplay>(PhysicsBodyDebug D) => D == null || !D.DisplayHitNormals ? null : D.HitNormalsThisUpdate;
        public struct HitNormalDisplay
        {
            public Vector2 position;
            public Vector2 normal;
            public HitNormalDisplay(Vector2 position, Vector2 normal)
            {
                this.position = position;
                this.normal = normal;
            }
            public HitNormalDisplay(RaycastHit2D fromHit)
            {
                this.position = fromHit.point;
                this.normal = fromHit.normal;
            }
        }

        public bool DisplayJumpMarker;
        public Vector2 JumpMarkerPos1;
        public Vector2 JumpMarkerPos2;
        public Vector2 JumpMarkerPos3;
        public void PlaceJumpMarker(float targetHeight, float jumpHeight)
        {
            if (!DisplayJumpMarker) return;
            JumpMarkerPos1 = Position;
            JumpMarkerPos2 = Position + (Vector2.up * targetHeight);
            JumpMarkerPos3 = Position + (Vector2.up * jumpHeight);
        }

        public bool DisplayClosestNavEdge;

        public void DisplayGizmos()
        {
            if (!Application.isPlaying) return;
            if (DisplaySweeps)
            {
                foreach (var sweep in SweepsThisUpdate)
                {
                    Color color = sweep.hit ? Color.green : Color.red;
                    Color colorE = color.Changed(a: .5f);
                    Collider.DrawWireClone(color, sweep.origin);
                    Collider.DrawWireClone(colorE, sweep.origin +
                        (sweep.hit ? sweep.direction.normalized * sweep.hitDistance : sweep.direction));

                    if (sweep.hit)
                    {
                        Vector2 start = sweep.origin + (sweep.direction.normalized * sweep.hitDistance);
                        Vector2 end = start + sweep.hitNormal;
                        Gizmos.DrawLine(start, end);
                    }
                }
            }

            if (DisplayHitNormals)
            {
                foreach (HitNormalDisplay item in HitNormalsThisUpdate)
                    UnityEngine.Debug.DrawRay(item.position, item.normal / 10);
            }

            if (DisplayJumpMarker)
            {
                Handles.DrawWireDisc(JumpMarkerPos1, Vector2.up, 0.5f);
                Handles.DrawWireDisc(JumpMarkerPos2, Vector2.up, 0.5f);
                Handles.DrawWireDisc(JumpMarkerPos2, Vector2.up, 0.5f);
            }

            if (DisplayClosestNavEdge & NavMesh.FindClosestEdge(Position, out var hit, NavMesh.AllAreas))
                UnityEngine.Debug.DrawRay(hit.position, hit.normal, Color.yellow);
        }
#endif
    }
}