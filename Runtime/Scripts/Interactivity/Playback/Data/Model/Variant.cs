using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace UnityGLTF.Interactivity.Playback
{
    public enum VariantType : byte
    {
        None = 0,
        Bool,
        Int,
        Float,
        Float2,
        Float3,
        Float4,
        Float2x2,
        Float3x3,
        Float4x4,
        Ref,
        /// <summary>Custom string types (e.g. AMZN_interactivity_string), stored as a handle into <see cref="VariantStrings"/>.</summary>
        String,
    }

    /// <summary>
    /// Unmanaged runtime value of any KHR_interactivity value type. This replaces boxed <see cref="Property{T}"/>
    /// values on the playback path so evaluation never allocates, and so values can live in native memory.
    /// Vectors and matrices occupy the leading lanes of the columns of one float4x4 (float3x3 uses c0.xyz, c1.xyz, c2.xyz),
    /// which lets component-wise operations run on whole float4 columns. Unused lanes have no meaning.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct Variant : IEquatable<Variant>
    {
        [FieldOffset(0)] private VariantType _type;
        [FieldOffset(4)] private float4x4 _m;
        [FieldOffset(4)] private int _i;
        [FieldOffset(4)] private Ref _ref;

        public VariantType type => _type;
        public bool isNone => _type == VariantType.None;

        // Factories.
        public static Variant FromBool(bool v) { var r = new Variant { _type = VariantType.Bool }; r._i = v ? 1 : 0; return r; }
        public static Variant FromInt(int v) { var r = new Variant { _type = VariantType.Int }; r._i = v; return r; }
        public static Variant FromFloat(float v) { var r = new Variant { _type = VariantType.Float }; r._m.c0.x = v; return r; }
        public static Variant FromFloat2(float2 v) { var r = new Variant { _type = VariantType.Float2 }; r._m.c0.xy = v; return r; }
        public static Variant FromFloat3(float3 v) { var r = new Variant { _type = VariantType.Float3 }; r._m.c0.xyz = v; return r; }
        public static Variant FromFloat4(float4 v) { var r = new Variant { _type = VariantType.Float4 }; r._m.c0 = v; return r; }
        public static Variant FromFloat2x2(float2x2 v) { var r = new Variant { _type = VariantType.Float2x2 }; r._m.c0.xy = v.c0; r._m.c1.xy = v.c1; return r; }
        public static Variant FromFloat3x3(float3x3 v) { var r = new Variant { _type = VariantType.Float3x3 }; r._m.c0.xyz = v.c0; r._m.c1.xyz = v.c1; r._m.c2.xyz = v.c2; return r; }
        public static Variant FromFloat4x4(float4x4 v) { var r = new Variant { _type = VariantType.Float4x4 }; r._m = v; return r; }
        public static Variant FromRef(Ref v) { var r = new Variant { _type = VariantType.Ref }; r._ref = v; return r; }
        public static Variant FromString(string v) { var r = new Variant { _type = VariantType.String }; r._i = VariantStrings.GetHandle(v); return r; }

        /// <summary>A float-based value of type <paramref name="type"/> whose lanes are taken from <paramref name="columns"/>.</summary>
        public static Variant FromColumns(VariantType type, in float4x4 columns) { var r = new Variant { _type = type }; r._m = columns; return r; }

        // Accessors. They do not check the type; callers switch on <see cref="type"/> first.
        public bool Bool => _i != 0;
        public int Int => _i;
        public float Float => _m.c0.x;
        public float2 Float2 => _m.c0.xy;
        public float3 Float3 => _m.c0.xyz;
        public float4 Float4 => _m.c0;
        public float2x2 Float2x2 => new float2x2(_m.c0.xy, _m.c1.xy);
        public float3x3 Float3x3 => new float3x3(_m.c0.xyz, _m.c1.xyz, _m.c2.xyz);
        public float4x4 Float4x4 => _m;
        public Ref Ref => _ref;
        public string String => VariantStrings.Get(_i);

        /// <summary>The raw float lanes, for component-wise operations on any float-based type.</summary>
        public float4x4 columns => _m;

        public bool isFloatBased => _type >= VariantType.Float && _type <= VariantType.Float4x4;
        public bool isMatrix => _type >= VariantType.Float2x2 && _type <= VariantType.Float4x4;

        /// <summary>Number of float4 columns that carry lanes of this type: 1 for scalars and vectors, N for NxN matrices.</summary>
        public int columnCount => _type switch
        {
            VariantType.Float2x2 => 2,
            VariantType.Float3x3 => 3,
            VariantType.Float4x4 => 4,
            _ => 1,
        };

        public bool TryGet<T>(out T value) => VariantTraits<T>.tryGet(in this, out value);

        public static Variant From<T>(T value) => VariantTraits<T>.from(value);

        public static bool IsSupported<T>() => VariantTraits<T>.supported;

        public static VariantType TypeOf(Type t)
        {
            if (t == typeof(bool)) return VariantType.Bool;
            if (t == typeof(int)) return VariantType.Int;
            if (t == typeof(float)) return VariantType.Float;
            if (t == typeof(float2)) return VariantType.Float2;
            if (t == typeof(float3)) return VariantType.Float3;
            if (t == typeof(float4)) return VariantType.Float4;
            if (t == typeof(float2x2)) return VariantType.Float2x2;
            if (t == typeof(float3x3)) return VariantType.Float3x3;
            if (t == typeof(float4x4)) return VariantType.Float4x4;
            if (t == typeof(Ref)) return VariantType.Ref;
            if (t == typeof(string)) return VariantType.String;
            return VariantType.None;
        }

        public static Type SystemTypeOf(VariantType t) => t switch
        {
            VariantType.Bool => typeof(bool),
            VariantType.Int => typeof(int),
            VariantType.Float => typeof(float),
            VariantType.Float2 => typeof(float2),
            VariantType.Float3 => typeof(float3),
            VariantType.Float4 => typeof(float4),
            VariantType.Float2x2 => typeof(float2x2),
            VariantType.Float3x3 => typeof(float3x3),
            VariantType.Float4x4 => typeof(float4x4),
            VariantType.Ref => typeof(Ref),
            VariantType.String => typeof(string),
            _ => null,
        };

        public Type GetSystemType() => SystemTypeOf(_type);

        public string GetTypeSignature() => _type == VariantType.None ? "none" : Helpers.GetSignatureBySystemType(GetSystemType());

        /// <summary>Type-default values as defined by the "Custom Variable Types" section of the spec.</summary>
        public static Variant Default(VariantType t) => t switch
        {
            VariantType.Bool => FromBool(false),
            VariantType.Int => FromInt(0),
            VariantType.Float => FromFloat(float.NaN),
            VariantType.Float2 => FromFloat2(new float2(float.NaN)),
            VariantType.Float3 => FromFloat3(new float3(float.NaN)),
            VariantType.Float4 => FromFloat4(new float4(float.NaN)),
            VariantType.Float2x2 => FromFloat2x2(new float2x2(float.NaN)),
            VariantType.Float3x3 => FromFloat3x3(new float3x3(float.NaN)),
            VariantType.Float4x4 => FromFloat4x4(new float4x4(float.NaN)),
            VariantType.Ref => FromRef(Ref.Null),
            VariantType.String => FromString(string.Empty),
            _ => default,
        };

        public static Variant Default(Type t) => Default(TypeOf(t));

        /// <summary>Converts a boxed property. Allocation-free for the result; intended for load time and API boundaries.</summary>
        public static Variant FromProperty(IProperty p) => p switch
        {
            Property<bool> v => FromBool(v.value),
            Property<int> v => FromInt(v.value),
            Property<float> v => FromFloat(v.value),
            Property<float2> v => FromFloat2(v.value),
            Property<float3> v => FromFloat3(v.value),
            Property<float4> v => FromFloat4(v.value),
            Property<float2x2> v => FromFloat2x2(v.value),
            Property<float3x3> v => FromFloat3x3(v.value),
            Property<float4x4> v => FromFloat4x4(v.value),
            Property<Ref> v => FromRef(v.value),
            Property<string> v => FromString(v.value),
            _ => default,
        };

        /// <summary>Boxes the value. Allocates; only for API boundaries such as <see cref="Variable.property"/>.</summary>
        public IProperty ToProperty() => _type switch
        {
            VariantType.Bool => new Property<bool>(Bool),
            VariantType.Int => new Property<int>(Int),
            VariantType.Float => new Property<float>(Float),
            VariantType.Float2 => new Property<float2>(Float2),
            VariantType.Float3 => new Property<float3>(Float3),
            VariantType.Float4 => new Property<float4>(Float4),
            VariantType.Float2x2 => new Property<float2x2>(Float2x2),
            VariantType.Float3x3 => new Property<float3x3>(Float3x3),
            VariantType.Float4x4 => new Property<float4x4>(Float4x4),
            VariantType.Ref => new Property<Ref>(Ref),
            VariantType.String => new Property<string>(String),
            _ => null,
        };

        public bool Equals(Variant other)
        {
            if (_type != other._type)
                return false;

            return _type switch
            {
                VariantType.None => true,
                VariantType.Bool => Bool == other.Bool,
                VariantType.Int or VariantType.String => _i == other._i,
                VariantType.Float => Float.Equals(other.Float),
                VariantType.Float2 => Float2.Equals(other.Float2),
                VariantType.Float3 => Float3.Equals(other.Float3),
                VariantType.Float4 => Float4.Equals(other.Float4),
                VariantType.Float2x2 => Float2x2.Equals(other.Float2x2),
                VariantType.Float3x3 => Float3x3.Equals(other.Float3x3),
                VariantType.Float4x4 => Float4x4.Equals(other.Float4x4),
                VariantType.Ref => Ref == other.Ref,
                _ => false,
            };
        }

        public override bool Equals(object obj) => obj is Variant v && Equals(v);

        public override int GetHashCode() => HashCode.Combine(_type, _m.GetHashCode());

        /// <summary>Same text as the boxed property would produce; used by debug/log.</summary>
        public override string ToString() => _type switch
        {
            VariantType.Bool => Bool.ToString(),
            VariantType.Int => Int.ToString(),
            VariantType.Float => Float.ToString(),
            VariantType.Float2 => Float2.ToString(),
            VariantType.Float3 => Float3.ToString(),
            VariantType.Float4 => Float4.ToString(),
            VariantType.Float2x2 => Float2x2.ToString(),
            VariantType.Float3x3 => Float3x3.ToString(),
            VariantType.Float4x4 => Float4x4.ToString(),
            VariantType.Ref => Ref.ToString(),
            VariantType.String => String,
            _ => string.Empty,
        };
    }

    internal delegate bool VariantGetter<T>(in Variant v, out T value);

    /// <summary>Per-type conversions created once per T, so generic access never boxes.</summary>
    internal static class VariantTraits<T>
    {
        public static readonly VariantGetter<T> tryGet;
        public static readonly Func<T, Variant> from;
        public static readonly bool supported;

        static VariantTraits()
        {
            object get = null, make = null;
            var t = typeof(T);

            if (t == typeof(bool)) { get = (VariantGetter<bool>)((in Variant v, out bool x) => { x = v.Bool; return v.type == VariantType.Bool; }); make = (Func<bool, Variant>)Variant.FromBool; }
            else if (t == typeof(int)) { get = (VariantGetter<int>)((in Variant v, out int x) => { x = v.Int; return v.type == VariantType.Int; }); make = (Func<int, Variant>)Variant.FromInt; }
            else if (t == typeof(float)) { get = (VariantGetter<float>)((in Variant v, out float x) => { x = v.Float; return v.type == VariantType.Float; }); make = (Func<float, Variant>)Variant.FromFloat; }
            else if (t == typeof(float2)) { get = (VariantGetter<float2>)((in Variant v, out float2 x) => { x = v.Float2; return v.type == VariantType.Float2; }); make = (Func<float2, Variant>)Variant.FromFloat2; }
            else if (t == typeof(float3)) { get = (VariantGetter<float3>)((in Variant v, out float3 x) => { x = v.Float3; return v.type == VariantType.Float3; }); make = (Func<float3, Variant>)Variant.FromFloat3; }
            else if (t == typeof(float4)) { get = (VariantGetter<float4>)((in Variant v, out float4 x) => { x = v.Float4; return v.type == VariantType.Float4; }); make = (Func<float4, Variant>)Variant.FromFloat4; }
            else if (t == typeof(float2x2)) { get = (VariantGetter<float2x2>)((in Variant v, out float2x2 x) => { x = v.Float2x2; return v.type == VariantType.Float2x2; }); make = (Func<float2x2, Variant>)Variant.FromFloat2x2; }
            else if (t == typeof(float3x3)) { get = (VariantGetter<float3x3>)((in Variant v, out float3x3 x) => { x = v.Float3x3; return v.type == VariantType.Float3x3; }); make = (Func<float3x3, Variant>)Variant.FromFloat3x3; }
            else if (t == typeof(float4x4)) { get = (VariantGetter<float4x4>)((in Variant v, out float4x4 x) => { x = v.Float4x4; return v.type == VariantType.Float4x4; }); make = (Func<float4x4, Variant>)Variant.FromFloat4x4; }
            else if (t == typeof(Ref)) { get = (VariantGetter<Ref>)((in Variant v, out Ref x) => { x = v.Ref; return v.type == VariantType.Ref; }); make = (Func<Ref, Variant>)Variant.FromRef; }
            else if (t == typeof(string)) { get = (VariantGetter<string>)((in Variant v, out string x) => { x = v.type == VariantType.String ? v.String : null; return v.type == VariantType.String; }); make = (Func<string, Variant>)Variant.FromString; }

            supported = get != null;
            tryGet = (VariantGetter<T>)get ?? ((in Variant v, out T x) => { x = default; return false; });
            from = (Func<T, Variant>)make ?? (_ => default);
        }
    }

    /// <summary>
    /// Interned strings for custom string-typed values. Strings only enter at load (literals, initial values),
    /// so a growing table is fine; a handle never changes meaning.
    /// </summary>
    public static class VariantStrings
    {
        private static readonly object _lock = new();
        private static readonly List<string> _strings = new() { string.Empty };
        private static readonly Dictionary<string, int> _handles = new(StringComparer.Ordinal) { [string.Empty] = 0 };

        public static int GetHandle(string s)
        {
            s ??= string.Empty;

            lock (_lock)
            {
                if (_handles.TryGetValue(s, out var h))
                    return h;

                h = _strings.Count;
                _strings.Add(s);
                _handles.Add(s, h);
                return h;
            }
        }

        public static string Get(int handle)
        {
            lock (_lock)
            {
                return handle >= 0 && handle < _strings.Count ? _strings[handle] : string.Empty;
            }
        }
    }
}
