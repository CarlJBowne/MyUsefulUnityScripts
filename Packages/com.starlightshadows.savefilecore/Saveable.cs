namespace SLS.SaveFileCore
{
    [System.Serializable]
    public abstract class Saveable<T> : ICloneable<T> where T : Saveable<T>
    {
        public abstract T Clone(T source);
        public static void Clone(T from, T to) => to.Clone(from);
    }

}
