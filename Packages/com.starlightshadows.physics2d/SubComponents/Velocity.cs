using System;
using UnityEngine;

namespace SLS.Physics2D
{
    /// <summary>
    /// <see cref="MovementBody"/> Sub-component that tracks velocity for a PhysicsBody in both local (forward/side/up) and global (x/y/z) coordinate spaces. Assigning to one representation will update the other representations automatically.
    /// </summary>
    [System.Serializable]
    public class Velocity : PhysicsSubComponent
    {
        #region Config
        public bool allowBackwards = true;
        #endregion

        /// <summary>
        /// Forward Velocity
        /// <br/> Setting this will rebuild the global velocity values. (xyz)
        /// </summary>
        public float f
        {
            get => fValue;
            set
            {
                fValue = value;
                if (!allowBackwards && value < 0) fValue = 0;
                Vector2 global = transform.TransformVector(Local);
                xValue = global.x;
                yValue = global.y;
            }
        } float fValue;
        /// <summary>
        /// Upward Velocity 
        /// <br/> Setting this will rebuild the global velocity values. (xyz)
        /// </summary>
        public float u
        {
            get => uValue;
            set
            {
                uValue = value;
                Vector2 global = transform.TransformVector(Local);
                xValue = global.x;
                yValue = global.y;
            }
        } float uValue;
        /// <summary>
        /// Sideways Velocity
        /// <br/> Setting this will rebuild the global velocity values. (xyz)
        /// </summary>

        /// <summary>
        /// Velocity on the X global direction
        /// <br/> Setting this will rebuild the local velocity values. (fus)
        /// </summary>
        public float x
        {
            get => xValue;
            set
            {
                xValue = value;
                Vector2 local = transform.InverseTransformVector(Global);
                fValue = local.x;
                uValue = local.y;
            }
        } float xValue;
        /// <summary>
        /// Velocity on the Y global direction 
        /// <br/> Setting this will rebuild the local velocity values. (fus)
        /// </summary>
        public float y
        {
            get => yValue; set
            {
                yValue = value;
                Vector2 local = transform.InverseTransformVector(Global);
                fValue = local.x;
                uValue = local.y;
            }
        } float yValue;
        /// <summary>
        /// Velocity on the Z global direction
        /// <br/> Setting this will rebuild the local velocity values. (fus)
        /// </summary>



        public Vector2 Global
        {
            get => new(xValue, yValue);
            set
            {
                Print(()=>$"Velocity.Global set to {value}");
                xValue = value.x;
                yValue = value.y;

                Vector2 local = transform.InverseTransformVector(Global);
                fValue = local.x;
                uValue = local.y;
            }
        }
        public Vector2 Local
        {
            get => new(fValue, uValue);
            set
            {
                Print(() => $"Velocity.Local set to {value}");
                fValue = value.x;
                yValue = value.y;

                Vector2 global = transform.TransformVector(Local);
                xValue = global.x;
                yValue = global.y;
            }
        }

        /// <summary>
        /// How much Local Velocity is carried over upon rotation. 0-1
        /// </summary>
        public float carryLocal
        {
            get => cLValue;
            set
            {
                cLValue = Mathf.Clamp01(value);
                if (cLValue + cGValue > 1) cGValue = 1 - cLValue;
            }
        } float cLValue = 1f;
        /// <summary>
        /// How much Global Velocity is carried over upon rotation. 0-1
        /// </summary>
        public float carryGlobal
        {
            get => cGValue;
            set
            {
                cLValue = Mathf.Clamp01(value);
                if (cLValue + cGValue > 1) cGValue = 1 - cLValue;
            }
        } float cGValue;

        /// <summary>
        /// The angular velocity around the vertical axis (U) that the <see cref="MovementBody"/> automatically uses to look towards the <see cref="Direction"/>'s <see cref="Direction.lookTarget"/>.
        /// </summary>
        public float lookVelocity;


        /// <summary>
        /// Call this after the transform or direction has been rotated. This method
        /// reconciles local/global velocity components according to the configured
        /// carry-over parameters (<see cref="carryLocal"/> and <see cref="carryGlobal"/>).
        /// </summary>
        public void CallThisPostRotation()
        {
            if (cLValue == 0 && cGValue == 0) { xValue = 0; fValue = 0; return; }
            Print(() => $"Velocity.CallThisPostRotation() - cL: {cLValue}, cG: {cGValue}");

            Vector2 localAdjusted = transform.TransformVector(Local);

            float xFinal = (xValue * cLValue) + (localAdjusted.x * cGValue),
                  yFinal = (yValue * cLValue) + (localAdjusted.y * cGValue);

            Global = new(xFinal, yFinal);
        }

        /// <summary>
        /// Zeros velocity components selectively.
        /// </summary>
        /// <param name="horizontal">Zero horizontal components (f and s / x and z).</param>
        /// <param name="vertical">Zero vertical component (y).</param>
        /// <param name="rotational">Zero rotational component (r).</param>
        public void ZeroOut(bool horizontal = true, bool vertical = true)
        {
            if (horizontal)
            {
                fValue = 0;
                xValue = 0;
            }
            if (vertical) yValue = 0;
        }
    }
}