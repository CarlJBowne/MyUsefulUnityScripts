using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Utilities.Xtensions;
using Utilities.Xtensions.Unity;

namespace RageRooster.Physics
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
            public Vector3 origin;
            public Vector3 direction;
            public bool hit;
            public float hitDistance;
            public Vector3 hitNormal;
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
            public Vector3 position;
            public Vector3 normal;
            public HitNormalDisplay(Vector3 position, Vector3 normal)
            {
                this.position = position;
                this.normal = normal;
            }
            public HitNormalDisplay(RaycastHit fromHit)
            {
                this.position = fromHit.point;
                this.normal = fromHit.normal;
            }
        }

        public bool DisplayJumpMarker;
        public Vector3 JumpMarkerPos1;
        public Vector3 JumpMarkerPos2;
        public Vector3 JumpMarkerPos3;
        public void PlaceJumpMarker(float targetHeight, float jumpHeight)
        {
            if (!DisplayJumpMarker) return;
            JumpMarkerPos1 = body.Position;
            JumpMarkerPos2 = body.Position + (Vector3.up * targetHeight);
            JumpMarkerPos2 = body.Position + (Vector3.up * jumpHeight);
        }

        public bool DisplayClosestNavEdge;

        public void DisplayGizmos()
        {
            if (DisplaySweeps)
            {
                foreach (var sweep in SweepsThisUpdate)
                {
                    Color color = sweep.hit ? Color.green : Color.red;
                    Color colorE = color.Changed(a: .5f);
                    body.Collider.DrawWireClone(color, sweep.origin);
                    body.Collider.DrawWireClone(colorE, sweep.origin +
                        (sweep.hit ? sweep.direction.normalized * sweep.hitDistance : sweep.direction));

                    if (sweep.hit)
                    {
                        Vector3 start = sweep.origin + (sweep.direction.normalized * sweep.hitDistance);
                        Vector3 end = start + sweep.hitNormal;
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
                Handles.DrawWireDisc(JumpMarkerPos1, Vector3.up, 0.5f);
                Handles.DrawWireDisc(JumpMarkerPos2, Vector3.up, 0.5f);
                Handles.DrawWireDisc(JumpMarkerPos3, Vector3.up, 0.5f);
            }

            if (DisplayClosestNavEdge & NavMesh.FindClosestEdge(body.Position, out var hit, NavMesh.AllAreas))
                UnityEngine.Debug.DrawRay(hit.position, hit.normal, Color.yellow);
        }
#endif
    }
}