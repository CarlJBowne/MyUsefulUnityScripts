#if UNITY_EDITOR
#endif

namespace SLS.Physics3D
{
    public interface ICollisionHandler
    {
        void Collide(SweepPayload collision);
    }
}
