namespace UnityEngine
{
    public static class LookAtDirectionalExtension
    {
        /// <summary>
        ///     Rotates the transform so the specified vector points at a targets current position.
        /// </summary>
        public static void LookAt(this Transform trans, Vector3 target, LookDirection direction)
        {
            trans.LookAt(target);
            switch (direction)
            {
                case LookDirection.Forward:
                    trans.eulerAngles += new Vector3(0, 0, 0);
                    break;
                case LookDirection.Backward:
                    trans.eulerAngles += new Vector3(180, 0, 0);
                    break;
                case LookDirection.Upward:
                    trans.eulerAngles += new Vector3(90, 0, 0);
                    break;
                case LookDirection.Downward:
                    trans.eulerAngles += new Vector3(-90, 0, 0);
                    break;
                case LookDirection.Rightward:
                    trans.eulerAngles += new Vector3(-trans.eulerAngles.x, -90, -trans.eulerAngles.x);
                    break;
                case LookDirection.Leftward:
                    trans.eulerAngles += new Vector3(-trans.eulerAngles.x, 90, trans.eulerAngles.x);
                    break;
            }
        }

        /// <summary>
        ///     Rotates the transform so the specified vector points at a targets current position.
        /// </summary>
        public static void LookAt(this Transform trans, Transform target, LookDirection direction)
        {
            trans.LookAt(target);
            switch (direction)
            {
                case LookDirection.Forward:
                    trans.eulerAngles += new Vector3(0, 0, 0);
                    break;
                case LookDirection.Backward:
                    trans.eulerAngles += new Vector3(180, 0, 0);
                    break;
                case LookDirection.Upward:
                    trans.eulerAngles += new Vector3(90, 0, 0);
                    break;
                case LookDirection.Downward:
                    trans.eulerAngles += new Vector3(-90, 0, 0);
                    break;
                case LookDirection.Rightward:
                    trans.eulerAngles += new Vector3(-trans.eulerAngles.x, -90, -trans.eulerAngles.x);
                    break;
                case LookDirection.Leftward:
                    trans.eulerAngles += new Vector3(-trans.eulerAngles.x, 90, trans.eulerAngles.x);
                    break;
            }
        }
    }

    /// <summary>
    ///     The Direction pointed at the target
    /// </summary>
    public enum LookDirection
    {
        Forward = 1,
        Backward = 2,
        Upward = 3,
        Downward = 4,
        Rightward = 5,
        Leftward = 6
    };
}
