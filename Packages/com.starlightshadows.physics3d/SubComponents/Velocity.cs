using System;
using UnityEngine;

namespace SLS.Physics3D
{
    /// <summary>
    /// <see cref="PhysicsBody"/> Sub-component that tracks velocity for a PhysicsBody in both local (forward/side/up) and global (x/y/z) coordinate spaces. Assigning to one representation will update the other representations automatically.
    /// </summary>
    [System.Serializable]
    public class Velocity : PhysicsSubComponent
    {
        #region Config
        public bool allowBackwards = true;
        #endregion

        /// <summary>
        /// Forward Velocity
        /// <br/> Setting this will rebuild the x and z values to match the new forward velocity.
        /// </summary>
        public float f
        {
            get => fValue;
            set
            {
                fValue = value;
                if (!allowBackwards && value < 0) fValue = 0;
                Vector3 global = transform.TransformVector(Local);
                zValue = global.z;
                xValue = global.x;
            }
        }
        float fValue;
        /// <summary>
        /// Upward Velocity (Identical to y)
        /// </summary>
        public float u { get => yValue; set => yValue = value; }
        /// <summary>
        /// Sideways Velocity
        /// <br/> Setting this will rebuild the x and z values to match the new sideways velocity.
        /// </summary>
        public float s
        {
            get => sValue;
            set
            {
                sValue = value;
                Vector3 global = transform.TransformVector(Local);
                zValue = global.z;
                xValue = global.x;
            }
        }
        float sValue;

        /// <summary>
        /// Velocity on the X global direction
        /// <br/> Setting this will rebuild the f and s values to match the new x velocity.
        /// </summary>
        public float x
        {
            get => xValue;
            set
            {
                xValue = value;
                Vector3 local = transform.InverseTransformVector(Global);
                fValue = local.z;
                sValue = local.x;
            }
        }
        float xValue;
        /// <summary>
        /// Velocity on the Y global direction (Identical to u)
        /// </summary>
        public float y
        {
            get => yValue; set => yValue = value;
        }
        float yValue;
        /// <summary>
        /// Velocity on the Z global direction
        /// <br/> Setting this will rebuild the f and s values to match the new z velocity.
        /// </summary>
        public float z
        {
            get => zValue;
            set
            {
                zValue = value;
                Vector3 local = transform.InverseTransformVector(Global);
                fValue = local.z;
                sValue = local.x;
            }
        }
        float zValue;



        public Vector3 Global
        {
            get => new(xValue, yValue, zValue);
            set
            {
                xValue = value.x;
                yValue = value.y;
                zValue = value.z;

                Vector3 local = transform.InverseTransformVector(Global);
                fValue = local.z;
                sValue = local.x;
            }
        }
        public Vector3 Local
        {
            get => new(sValue, yValue, fValue);
            set
            {
                sValue = value.x;
                yValue = value.y;
                fValue = value.z;

                Vector3 global = transform.TransformVector(Local);
                zValue = global.z;
                xValue = global.x;
            }
        }

        /// <summary>
        /// Rotational velocity around the vertical axis (Y). Positive values
        /// represent clockwise rotation when viewed from above.
        /// </summary>
        public float r
        {
            get => rValue;
            set => rValue = value;
        }
        float rValue;
        /// <summary>
        /// How much Local Velocity is carried over upon rotation. 0-1
        /// </summary>
        public float cL
        {
            get => cLValue;
            set
            {
                cLValue = Mathf.Clamp01(value);
                if (cLValue + cGValue > 1) cGValue = 1 - cLValue;
            }
        }
        float cLValue = 1f;
        /// <summary>
        /// How much Global Velocity is carried over upon rotation. 0-1
        /// </summary>
        public float cG
        {
            get => cGValue;
            set
            {
                cLValue = Mathf.Clamp01(value);
                if (cLValue + cGValue > 1) cGValue = 1 - cLValue;
            }
        }
        float cGValue;

        /// <summary>
        /// The angular velocity around the vertical axis (Y) that the <see cref="PhysicsBody"/> automatically uses to look towards the <see cref="Direction"/>'s <see cref="Direction.lookTarget"/>.
        /// </summary>
        public float lookVelocity;


        /// <summary>
        /// Call this after the transform or direction has been rotated. This method
        /// reconciles local/global velocity components according to the configured
        /// carry-over parameters (<see cref="cL"/> and <see cref="cG"/>).
        /// </summary>
        public void CallThisPostRotation()
        {
            if (cLValue == 0 && cGValue == 0) { xValue = 0; zValue = 0; fValue = 0; sValue = 0; return; }

            Vector3 adjustedGlobalValues = transform.InverseTransformVector(Global);

            float fFinal = (fValue * cLValue) + (adjustedGlobalValues.z * cGValue),
                  sFinal = (sValue * cLValue) + (adjustedGlobalValues.x * cGValue);

            Vector3 finalGlobalValues = transform.TransformVector(new(sFinal, 0, fFinal));

            fValue = fFinal;
            sValue = sFinal;
            xValue = finalGlobalValues.x;
            zValue = finalGlobalValues.z;
        }

        /// <summary>
        /// Zeros velocity components selectively.
        /// </summary>
        /// <param name="horizontal">Zero horizontal components (f and s / x and z).</param>
        /// <param name="vertical">Zero vertical component (y).</param>
        /// <param name="rotational">Zero rotational component (r).</param>
        public void ZeroOut(bool horizontal = true, bool vertical = true, bool rotational = true)
        {
            if (horizontal)
            {
                fValue = 0;
                sValue = 0;
                xValue = 0;
                zValue = 0;
            }
            if (vertical) yValue = 0;
            if (rotational) rValue = 0;
        }

        /// <summary>
        /// The current horizontal Magnitude of the current velocity.
        /// </summary>
        public float magnitudeH =>
            sValue != 0 ? MathF.Sqrt((fValue * fValue) + (sValue * sValue))
            : fValue;
        /// <summary>
        /// The current horizontal Squared Magnitude of the current velocity.
        /// </summary>
        public float sqrMagnitudeH =>
            sValue != 0 ? (fValue * fValue) + (sValue * sValue)
            : fValue * fValue;
        /// <summary>
        /// The current Magnitude of the current velocity.
        /// </summary>
        public float magnitude =>
            fValue != 0 && sValue != 0 && yValue != 0 ? Mathf.Sqrt((fValue * fValue) + (sValue * sValue) + (yValue * yValue)) //All 3 NonZero
                  : fValue != 0 && sValue == 0 && yValue == 0 ? Mathf.Sqrt(fValue * fValue) //Only F
                  : fValue == 0 && sValue == 0 && yValue != 0 ? Mathf.Sqrt(yValue * yValue) //Only Y
                  : fValue == 0 && sValue != 0 && yValue == 0 ? Mathf.Sqrt(sValue * sValue) //Only S
                  : fValue != 0 && sValue == 0 && yValue != 0 ? Mathf.Sqrt((fValue * fValue) + (yValue * yValue)) // F+Y
                  : fValue != 0 && sValue != 0 && yValue == 0 ? Mathf.Sqrt((fValue * fValue) + (sValue * sValue)) // F+S
                  : fValue == 0 && sValue != 0 && yValue != 0 ? Mathf.Sqrt((sValue * sValue) + (yValue * yValue)) // S+Y
            : 0; //All 3 Zero

        /// <summary>
        /// The current Squared Magnitude of the current velocity.
        /// </summary>
        public float sqrMagnitude =>
            fValue != 0 && sValue != 0 && yValue != 0 ? (fValue * fValue) + (sValue * sValue) + (yValue * yValue) //All 3 NonZero
                  : fValue != 0 && sValue == 0 && yValue == 0 ? fValue * fValue //Only F
                  : fValue == 0 && sValue == 0 && yValue != 0 ? yValue * yValue //Only Y
                  : fValue == 0 && sValue != 0 && yValue == 0 ? sValue * sValue //Only S
                  : fValue != 0 && sValue == 0 && yValue != 0 ? (fValue * fValue) + (yValue * yValue) // F+Y
                  : fValue != 0 && sValue != 0 && yValue == 0 ? (fValue * fValue) + (sValue * sValue) // F+S
                  : fValue == 0 && sValue != 0 && yValue != 0 ? (sValue * sValue) + (yValue * yValue) // S+Y
            : 0; //All 3 Zero
    }
}