using System;
using System.Collections.Generic;
using System.Text;

namespace SLS.GeneralUtilities
{
    [Serializable]
    public class Updatable<T>
    {
        public T Value
        {
            get => value;
            set
            {
                this.value = value;
                Update?.Invoke(value);
            }
        }

        private T value = default;

        public event Action<T> Update;

        public Updatable(T value = default) => this.value = value;

        public static implicit operator T(Updatable<T> updatable) => updatable.Value;
        public static implicit operator Updatable<T>(T source) => new(source);
    }
    [Serializable]
    public class UpdatableWrapper<T>
    {
        private Func<T> valueGetter;
        private Action<T> valueSetter;

        public Updatable<T> Value
        {
            get => valueGetter();
            set
            {
                valueSetter(value);
                Update?.Invoke(value);
            }
        }

        public UpdatableWrapper(Func<T> getter, Action<T> setter)
        {
            valueGetter = getter;
            valueSetter = setter;
        }


        public event Action<T> Update;
        public static implicit operator T(UpdatableWrapper<T> updatable) => updatable.Value;
    }
}
