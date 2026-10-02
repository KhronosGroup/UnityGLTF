using System;

namespace UnityGLTF.Interactivity.Playback
{
    public interface IPointer
    {
        public bool invalid { get; }
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
        public bool invalid { get; set; }
        public Func<T> getter;

        public ReadOnlyPointer(Func<T> getter)
        {
            invalid = false;
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

    /// <summary>
    /// Read-only Object Model property holding the index of another glTF object, such as /nodes/{}/children/{}.
    /// The Object Model types these as int; a pointer/get configured with the ref type reads them as a reference
    /// into <see cref="collection"/>, so the result can feed {} template parameters directly.
    /// </summary>
    public struct ObjectIndexPointer : IReadOnlyPointer<int>
    {
        public bool invalid { get; set; }
        /// <summary>The JSON pointer of the array the index refers to, e.g. "/nodes".</summary>
        public string collection;
        public int index;

        public ObjectIndexPointer(string collection, int index)
        {
            invalid = false;
            this.collection = collection;
            this.index = index;
        }

        public ReadOnlyPointer<Ref> AsRef()
        {
            var r = Ref.Gltf(collection, index);
            return new ReadOnlyPointer<Ref>(() => r);
        }

        public Type GetSystemType()
        {
            return typeof(int);
        }

        public string GetTypeSignature()
        {
            return Helpers.GetSignatureBySystemType(typeof(int));
        }
    }

    public struct Pointer<T> : IPointer<T>
    {
        public Action<T> setter;
        public Func<T> getter;
        public Func<T, T, float, T> evaluator;
        public bool invalid { get; set; }

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