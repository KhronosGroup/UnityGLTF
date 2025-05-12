using System;

namespace UnityGLTF.Interactivity.Playback
{
    public interface IProperty
    {
        public string ToString();

        public Type GetSystemType();

        public string GetTypeSignature();
    }

    public struct Property<T> : IProperty
    {
        public Property(T value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public Type GetSystemType()
        {
            return typeof(T);
        }

        public string GetTypeSignature()
        {
            return Helpers.GetSignatureBySystemType(typeof(T));
        }

        public T value;
    }
}