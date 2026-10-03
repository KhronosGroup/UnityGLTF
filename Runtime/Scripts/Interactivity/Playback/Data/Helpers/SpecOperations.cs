using System.Collections.Generic;

namespace UnityGLTF.Interactivity.Playback
{
    /// <summary>
    /// Operation identifiers defined by the KHR_interactivity specification itself, and the
    /// operations from additional interactivity extensions that this runtime implements.
    /// </summary>
    public static class SpecOperations
    {
        public static readonly HashSet<string> Core = new()
        {
            "animation/start", "animation/stop", "animation/stopAt",
            "debug/log",
            "event/onStart", "event/onTick", "event/receive", "event/send", "event/stopPropagation",
            "flow/branch", "flow/cancelDelay", "flow/doN", "flow/for", "flow/multiGate", "flow/sequence",
            "flow/setDelay", "flow/switch", "flow/throttle", "flow/waitAll", "flow/while",
            "math/E", "math/Inf", "math/NaN", "math/Pi", "math/Tau",
            "math/abs", "math/acos", "math/acosh", "math/add", "math/and", "math/asin", "math/asinh", "math/asr",
            "math/atan", "math/atan2", "math/atanh", "math/cbrt", "math/ceil", "math/clamp", "math/clz",
            "math/combine2", "math/combine2x2", "math/combine3", "math/combine3x3", "math/combine4", "math/combine4x4",
            "math/cos", "math/cosh", "math/cross", "math/ctz", "math/deg", "math/determinant", "math/div", "math/dot",
            "math/eq", "math/exp",
            "math/extract2", "math/extract2x2", "math/extract3", "math/extract3x3", "math/extract4", "math/extract4x4",
            "math/floor", "math/fract", "math/ge", "math/gt", "math/inverse", "math/isInf", "math/isNaN", "math/le",
            "math/length", "math/log", "math/log10", "math/log2", "math/lsl", "math/lt",
            "math/matCompose", "math/matDecompose", "math/matMul", "math/max", "math/min", "math/mix", "math/mul",
            "math/neg", "math/normalize", "math/not", "math/or", "math/popcnt", "math/pow",
            "math/quatAngleBetween", "math/quatConjugate", "math/quatFromAngles", "math/quatFromAxisAngle",
            "math/quatFromDirections", "math/quatFromUpForward", "math/quatMul", "math/quatSlerp", "math/quatToAxisAngle",
            "math/rad", "math/random", "math/rem", "math/rgbFromOkLCh", "math/rgbToOkLCh", "math/rotate2D", "math/rotate3D",
            "math/round", "math/saturate", "math/select", "math/sign", "math/sin", "math/sinh", "math/slerp",
            "math/smoothStep", "math/sqrt", "math/sub", "math/switch", "math/tan", "math/tanh", "math/transform",
            "math/transpose", "math/trunc", "math/xor",
            "pointer/get", "pointer/interpolate", "pointer/set",
            "ref/eq",
            "type/boolToFloat", "type/boolToInt", "type/floatToBool", "type/floatToInt", "type/intToBool", "type/intToFloat",
            "variable/get", "variable/interpolate", "variable/set",
        };

        /// <summary>
        /// Extension operations implemented by this runtime, keyed by "extension:op".
        /// Socket signatures of these declarations are not compared yet; see the update plan.
        /// </summary>
        private static readonly HashSet<string> _supportedExtensionOps = new()
        {
            "KHR_node_selectability:event/onSelect",
            "KHR_node_hoverability:event/onHoverIn",
            "KHR_node_hoverability:event/onHoverOut",
        };

        public static bool IsCore(string op) => op != null && Core.Contains(op);

        public static bool IsSupportedExtensionOp(string extension, string op)
        {
            return _supportedExtensionOps.Contains($"{extension}:{op}");
        }

        /// <summary>
        /// A declaration is supported when it is a core operation without an extension,
        /// or an implemented operation of a supported extension. Unsupported declarations
        /// demote their nodes to no-ops.
        /// </summary>
        public static bool IsSupported(Declaration declaration)
        {
            if (string.IsNullOrEmpty(declaration.extension))
                return IsCore(declaration.op);

            return IsSupportedExtensionOp(declaration.extension, declaration.op);
        }

        /// <summary>
        /// Input value socket ids that every node of a core operation must define, generated from the
        /// operation tables of the specification. Configuration-generated sockets (pointer template
        /// parameters, variable/set indices, event values, switch cases, log parameters) are not listed.
        /// </summary>
        public static readonly Dictionary<string, string[]> StaticInputValueSockets = new()
        {
            ["animation/start"] = new string[] { ConstStrings.ANIMATION, ConstStrings.START_TIME, ConstStrings.END_TIME, ConstStrings.SPEED },
            ["animation/stop"] = new string[] { ConstStrings.ANIMATION },
            ["animation/stopAt"] = new string[] { ConstStrings.ANIMATION, ConstStrings.STOP_TIME },
            ["debug/log"] = new string[] { },
            ["event/onStart"] = new string[] { },
            ["event/onTick"] = new string[] { },
            ["event/receive"] = new string[] { },
            ["event/send"] = new string[] { },
            ["event/stopPropagation"] = new string[] { ConstStrings.STOP_IMMEDIATE, ConstStrings.EVENT },
            ["flow/branch"] = new string[] { ConstStrings.CONDITION },
            ["flow/cancelDelay"] = new string[] { ConstStrings.DELAY },
            ["flow/doN"] = new string[] { ConstStrings.N },
            ["flow/for"] = new string[] { ConstStrings.START_INDEX, ConstStrings.END_INDEX },
            ["flow/multiGate"] = new string[] { },
            ["flow/sequence"] = new string[] { },
            ["flow/setDelay"] = new string[] { ConstStrings.DURATION },
            ["flow/switch"] = new string[] { ConstStrings.SELECTION },
            ["flow/throttle"] = new string[] { ConstStrings.DURATION },
            ["flow/waitAll"] = new string[] { },
            ["flow/while"] = new string[] { ConstStrings.CONDITION },
            ["math/E"] = new string[] { },
            ["math/Inf"] = new string[] { },
            ["math/NaN"] = new string[] { },
            ["math/Pi"] = new string[] { },
            ["math/Tau"] = new string[] { },
            ["math/abs"] = new string[] { ConstStrings.A },
            ["math/acos"] = new string[] { ConstStrings.A },
            ["math/acosh"] = new string[] { ConstStrings.A },
            ["math/add"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/and"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/asin"] = new string[] { ConstStrings.A },
            ["math/asinh"] = new string[] { ConstStrings.A },
            ["math/asr"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/atan2"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/atan"] = new string[] { ConstStrings.A },
            ["math/atanh"] = new string[] { ConstStrings.A },
            ["math/cbrt"] = new string[] { ConstStrings.A },
            ["math/ceil"] = new string[] { ConstStrings.A },
            ["math/clamp"] = new string[] { ConstStrings.A, ConstStrings.B, ConstStrings.C },
            ["math/clz"] = new string[] { ConstStrings.A },
            ["math/combine2"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/combine2x2"] = new string[] { ConstStrings.A, ConstStrings.B, ConstStrings.C, ConstStrings.D },
            ["math/combine3"] = new string[] { ConstStrings.A, ConstStrings.B, ConstStrings.C },
            ["math/combine3x3"] = new string[] { ConstStrings.A, ConstStrings.B, ConstStrings.C, ConstStrings.D, ConstStrings.E, ConstStrings.F, ConstStrings.G, ConstStrings.H, ConstStrings.I },
            ["math/combine4"] = new string[] { ConstStrings.A, ConstStrings.B, ConstStrings.C, ConstStrings.D },
            ["math/combine4x4"] = new string[] { ConstStrings.A, ConstStrings.B, ConstStrings.C, ConstStrings.D, ConstStrings.E, ConstStrings.F, ConstStrings.G, ConstStrings.H, ConstStrings.I, ConstStrings.J, ConstStrings.K, ConstStrings.L, ConstStrings.M, ConstStrings.N, ConstStrings.O, ConstStrings.P },
            ["math/cos"] = new string[] { ConstStrings.A },
            ["math/cosh"] = new string[] { ConstStrings.A },
            ["math/cross"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/ctz"] = new string[] { ConstStrings.A },
            ["math/deg"] = new string[] { ConstStrings.A },
            ["math/determinant"] = new string[] { ConstStrings.A },
            ["math/div"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/dot"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/eq"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/exp"] = new string[] { ConstStrings.A },
            ["math/extract2"] = new string[] { ConstStrings.A },
            ["math/extract2x2"] = new string[] { ConstStrings.A },
            ["math/extract3"] = new string[] { ConstStrings.A },
            ["math/extract3x3"] = new string[] { ConstStrings.A },
            ["math/extract4"] = new string[] { ConstStrings.A },
            ["math/extract4x4"] = new string[] { ConstStrings.A },
            ["math/floor"] = new string[] { ConstStrings.A },
            ["math/fract"] = new string[] { ConstStrings.A },
            ["math/ge"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/gt"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/inverse"] = new string[] { ConstStrings.A },
            ["math/isInf"] = new string[] { ConstStrings.A },
            ["math/isNaN"] = new string[] { ConstStrings.A },
            ["math/le"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/length"] = new string[] { ConstStrings.A },
            ["math/log10"] = new string[] { ConstStrings.A },
            ["math/log2"] = new string[] { ConstStrings.A },
            ["math/log"] = new string[] { ConstStrings.A },
            ["math/lsl"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/lt"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/matCompose"] = new string[] { ConstStrings.TRANSLATION, ConstStrings.ROTATION, ConstStrings.SCALE },
            ["math/matDecompose"] = new string[] { ConstStrings.A },
            ["math/matMul"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/max"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/min"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/mix"] = new string[] { ConstStrings.A, ConstStrings.B, ConstStrings.C },
            ["math/mul"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/neg"] = new string[] { ConstStrings.A },
            ["math/normalize"] = new string[] { ConstStrings.A },
            ["math/not"] = new string[] { ConstStrings.A },
            ["math/or"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/popcnt"] = new string[] { ConstStrings.A },
            ["math/pow"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/quatAngleBetween"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/quatConjugate"] = new string[] { ConstStrings.A },
            ["math/quatFromAngles"] = new string[] { ConstStrings.X, ConstStrings.Y, ConstStrings.Z },
            ["math/quatFromAxisAngle"] = new string[] { ConstStrings.AXIS, ConstStrings.ANGLE },
            ["math/quatFromDirections"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/quatFromUpForward"] = new string[] { ConstStrings.UP, ConstStrings.FORWARD },
            ["math/quatMul"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/quatSlerp"] = new string[] { ConstStrings.A, ConstStrings.B, ConstStrings.C },
            ["math/quatToAxisAngle"] = new string[] { ConstStrings.A },
            ["math/rad"] = new string[] { ConstStrings.A },
            ["math/random"] = new string[] { },
            ["math/rem"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/rgbFromOkLCh"] = new string[] { ConstStrings.L, ConstStrings.C, ConstStrings.H },
            ["math/rgbToOkLCh"] = new string[] { ConstStrings.R, ConstStrings.G, ConstStrings.B },
            ["math/rotate2D"] = new string[] { ConstStrings.A, ConstStrings.ANGLE },
            ["math/rotate3D"] = new string[] { ConstStrings.A, ConstStrings.ROTATION },
            ["math/round"] = new string[] { ConstStrings.A },
            ["math/saturate"] = new string[] { ConstStrings.A },
            ["math/select"] = new string[] { ConstStrings.CONDITION, ConstStrings.A, ConstStrings.B },
            ["math/sign"] = new string[] { ConstStrings.A },
            ["math/sin"] = new string[] { ConstStrings.A },
            ["math/sinh"] = new string[] { ConstStrings.A },
            ["math/slerp"] = new string[] { ConstStrings.A, ConstStrings.B, ConstStrings.C },
            ["math/smoothStep"] = new string[] { ConstStrings.A, ConstStrings.B, ConstStrings.C },
            ["math/sqrt"] = new string[] { ConstStrings.A },
            ["math/sub"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/switch"] = new string[] { ConstStrings.SELECTION, ConstStrings.DEFAULT },
            ["math/tan"] = new string[] { ConstStrings.A },
            ["math/tanh"] = new string[] { ConstStrings.A },
            ["math/transform"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["math/transpose"] = new string[] { ConstStrings.A },
            ["math/trunc"] = new string[] { ConstStrings.A },
            ["math/xor"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["pointer/get"] = new string[] { },
            ["pointer/interpolate"] = new string[] { ConstStrings.VALUE, ConstStrings.DURATION, ConstStrings.P1, ConstStrings.P2 },
            ["pointer/set"] = new string[] { ConstStrings.VALUE },
            ["ref/eq"] = new string[] { ConstStrings.A, ConstStrings.B },
            ["type/boolToFloat"] = new string[] { ConstStrings.A },
            ["type/boolToInt"] = new string[] { ConstStrings.A },
            ["type/floatToBool"] = new string[] { ConstStrings.A },
            ["type/floatToInt"] = new string[] { ConstStrings.A },
            ["type/intToBool"] = new string[] { ConstStrings.A },
            ["type/intToFloat"] = new string[] { ConstStrings.A },
            ["variable/get"] = new string[] { },
            ["variable/interpolate"] = new string[] { ConstStrings.VALUE, ConstStrings.DURATION, ConstStrings.P1, ConstStrings.P2 },
            ["variable/set"] = new string[] { },
        };
    }
}
