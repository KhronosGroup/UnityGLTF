using System;

namespace UnityGLTF.Interactivity.Playback
{
    public interface IPointer
    {
        public Type GetSystemType();
        public string GetTypeSignature();
    }

    public interface IPointer<T> : IPointer
    {
        public T GetValue();
    }
    public interface IReadOnlyPointer : IPointer
    {
    }

    public interface IReadOnlyPointer<T> : IReadOnlyPointer
    {
    }

    public struct ReadOnlyPointer<T> : IReadOnlyPointer<T>
    {
        public Func<T> getter;

        public ReadOnlyPointer(Func<T> getter)
        {
            this.getter = getter;
        }

        public T GetValue()
        {
            return getter();
        }

        public static implicit operator ReadOnlyPointer<T>(Pointer<T> pointer)
        {
            return new ReadOnlyPointer<T>() { getter = pointer.getter };
        }

        public Type GetSystemType()
        {
            return typeof(T);
        }

        public string GetTypeSignature()
        {
            return Helpers.GetSignatureBySystemType(typeof(T));
        }
    }

    public struct Pointer<T> : IPointer<T>
    {
        public Action<T> setter;
        public Func<T> getter;
        public Func<T, T, float, T> evaluator;

        public T GetValue()
        {
            return getter();
        }

        public Type GetSystemType()
        {
            return typeof(T);
        }

        public string GetTypeSignature()
        {
            return Helpers.GetSignatureBySystemType(typeof(T));
        }
    }
}