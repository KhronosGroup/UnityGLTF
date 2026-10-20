using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine.TestTools;

namespace UnityGLTF.Interactivity.Playback.Tests
{
    public partial class PointerNodesTests : NodeTestHelpers
    {
        private const string TEST_GLB = "pointer_test";
        private const float INTERPOLATION_DURATION = 0.5f;
        private static readonly float2 P1 = new float2(0.42f, 0f);
        private static readonly float2 P2 = new float2(0.52f, 1f);
        protected override string _subDirectory => "Pointer";

        [UnityTest]
        public IEnumerator PointerInterpolateGet_ValuesInGetAreWhatWereInterpolatedTo()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            for (int i = 0; i < MATERIAL_POINTERS.Length; i++)
            {
                var pointer = MATERIAL_POINTERS[i].pointer.Replace('/', '_').Replace("[nodeIndex]", "0");
                QueueTest("pointer/interpolate", $"InterpolateAndGetPointer{pointer}", $"Pointer Interpolate {pointer}", $"Tests that pointer/interpolate and pointer/get work for {pointer}.", CreatePointerInterpolateGraph(MATERIAL_POINTERS[i].pointer, MATERIAL_POINTERS[i].type), importer.Result);
            }
        }

        [UnityTest]
        public IEnumerator PointerInterpolate_InvalidParameter()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            var g = CreateInterpolateErrGraph(
                pointer: "/nodes/[nodeIndex]/translation",
                nodeIndex: -1,
                duration: 0.5f,
                p1: new float2(0.42f, 0f),
                p2: new float2(0.52f, 1f),
                value: new float3(1f, 2f, 3f),
                type: "float3");

            QueueTest("pointer/interpolate", GetCallerName(), "Interpolate Pointer w/ Negative Parameter", "Tests that a pointer with a {parameter} in it triggers the err output flow when that value is negative. Test fails if out or done flows are activated or err flow is not activated.", g, importer.Result);
        }

        [UnityTest]
        public IEnumerator PointerInterpolate_TypeMismatch()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            var g = CreateInterpolateErrGraph(
                pointer: "/nodes/[nodeIndex]/translation",
                nodeIndex: 0,
                duration: 0.5f,
                p1: new float2(0.42f, 0f),
                p2: new float2(0.52f, 1f),
                value: 4f,
                type: "float");

            QueueTest("pointer/interpolate", GetCallerName(), "Interpolate Pointer w/ Type Mismatch", "Tests that a pointer with a value/config type that does not match the object model type activates the err flow. Test fails if out or done flows are activated or err flow is not activated.", g, importer.Result);
        }

        [UnityTest]
        public IEnumerator PointerInterpolate_UnsupportedPointer()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            var g = CreateInterpolateErrGraph(
                            pointer: "/nodes/[nodeIndex]/unsupported",
                            nodeIndex: 0,
                            duration: 0.5f,
                            p1: new float2(0.42f, 0f),
                            p2: new float2(0.52f, 1f),
                            value: 4f,
                            type: "float");
            QueueTest("pointer/interpolate", GetCallerName(), "Interpolate Pointer w/ Unsupported Pointer", "Tests that an interpolate node activates the err output flow when an unsupported pointer is used. Test fails if out or done flows are activated or err flow is not activated.", g, importer.Result);
        }

        [UnityTest]
        public IEnumerator PointerInterpolate_InvalidDuration()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            var g1 = CreateInterpolateErrGraph(pointer: "/nodes/[nodeIndex]/translation", nodeIndex: 0, duration: -1f, p1: new float2(0.42f, 0f), p2: new float2(0.52f, 1f), value: new float3(1f, 1f, 1f), type: "float3");
            var g2 = CreateInterpolateErrGraph(pointer: "/nodes/[nodeIndex]/translation", nodeIndex: 0, duration: float.PositiveInfinity, p1: new float2(0.42f, 0f), p2: new float2(0.52f, 1f), value: new float3(1f, 1f, 1f), type: "float3");
            var g3 = CreateInterpolateErrGraph(pointer: "/nodes/[nodeIndex]/translation", nodeIndex: 0, duration: float.NaN, p1: new float2(0.42f, 0f), p2: new float2(0.52f, 1f), value: new float3(1f, 1f, 1f), type: "float3");

            QueueTest("pointer/interpolate", "PointerInterpolate_NegativeDuration", "Interpolate Pointer w/ Negative Duration", "Tests that an interpolate node activates the err output flow when a negative duration is used. Test fails if out or done flows are activated or err flow is not activated.", g1, importer.Result);
            QueueTest("pointer/interpolate", "PointerInterpolate_InfDuration", "Interpolate Pointer w/ Inf Duration", "Tests that an interpolate node activates the err output flow when an infinite duration is used. Test fails if out or done flows are activated or err flow is not activated.", g2, importer.Result);
            QueueTest("pointer/interpolate", "PointerInterpolate_NaNDuration", "Interpolate Pointer w/ NaN Duration", "Tests that an interpolate node activates the err output flow when duration is NaN. Test fails if out or done flows are activated or err flow is not activated.", g3, importer.Result);
        }

        [UnityTest]
        public IEnumerator PointerInterpolate_InvalidP1()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }
            var g1 = CreateInterpolateErrGraph(pointer: "/nodes/[nodeIndex]/translation", nodeIndex: 0, duration: 0.5f, p1: new float2(-1f, 0f), p2: new float2(0.52f, 1f), value: new float3(1f, 1f, 1f), type: "float3");
            var g2 = CreateInterpolateErrGraph(pointer: "/nodes/[nodeIndex]/translation", nodeIndex: 0, duration: 0.5f, p1: new float2(0f, float.PositiveInfinity), p2: new float2(0.52f, 1f), value: new float3(1f, 1f, 1f), type: "float3");
            var g3 = CreateInterpolateErrGraph(pointer: "/nodes/[nodeIndex]/translation", nodeIndex: 0, duration: 0.5f, p1: new float2(0.5f, float.NaN), p2: new float2(0.52f, 1f), value: new float3(1f, 1f, 1f), type: "float3");

            QueueTest("pointer/interpolate", "PointerInterpolate_NegativeP1", "Interpolate Pointer w/ Negative P1", "Tests that an interpolate node activates the err output flow when a negative value is used for P1. Test fails if out or done flows are activated or err flow is not activated.", g1, importer.Result);
            QueueTest("pointer/interpolate", "PointerInterpolate_InfP1", "Interpolate Pointer w/ Inf P1", "Tests that an interpolate node activates the err output flow when an infinite value is used for P1. Test fails if out or done flows are activated or err flow is not activated.", g2, importer.Result);
            QueueTest("pointer/interpolate", "PointerInterpolate_NaNP1", "Interpolate Pointer w/ NaN P1", "Tests that an interpolate node activates the err output flow when P1 contains a NaN. Test fails if out or done flows are activated or err flow is not activated.", g3, importer.Result);
        }

        [UnityTest]
        public IEnumerator PointerInterpolate_InvalidP2()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }
            var g1 = CreateInterpolateErrGraph(pointer: "/nodes/[nodeIndex]/translation", nodeIndex: 0, duration: -1f, p1: new float2(0.42f, 0f), p2: new float2(-1f, 0f), value: new float3(1f, 1f, 1f), type: "float3");
            var g2 = CreateInterpolateErrGraph(pointer: "/nodes/[nodeIndex]/translation", nodeIndex: 0, duration: float.PositiveInfinity, p1: new float2(0.42f, 0f), p2: new float2(0f, float.PositiveInfinity), value: new float3(1f, 1f, 1f), type: "float3");
            var g3 = CreateInterpolateErrGraph(pointer: "/nodes/[nodeIndex]/translation", nodeIndex: 0, duration: float.NaN, p1: new float2(0.42f, 0f), p2: new float2(0.5f, float.NaN), value: new float3(1f, 1f, 1f), type: "float3");

            QueueTest("pointer/interpolate", "PointerInterpolate_NegativeP1", "Interpolate Pointer w/ Negative P1", "Tests that an interpolate node activates the err output flow when a negative value is used for P1. Test fails if out or done flows are activated or err flow is not activated.", g1, importer.Result);
            QueueTest("pointer/interpolate", "PointerInterpolate_InfP1", "Interpolate Pointer w/ Inf P1", "Tests that an interpolate node activates the err output flow when an infinite value is used for P1. Test fails if out or done flows are activated or err flow is not activated.", g2, importer.Result);
            QueueTest("pointer/interpolate", "PointerInterpolate_NaNP1", "Interpolate Pointer w/ NaN P1", "Tests that an interpolate node activates the err output flow when P1 contains a NaN. Test fails if out or done flows are activated or err flow is not activated.", g3, importer.Result);
        }

        [UnityTest]
        public IEnumerator PointerInterpolate_ReadOnlyPointer()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }
            var g = CreateInterpolateErrGraph(
                pointer: "/extensions/KHR_interactivity/activeCamera/perspective/yfov",
                nodeIndex: 0,
                duration: 0.5f,
                p1: new float2(0.42f, 0f),
                p2: new float2(0.52f, 1f),
                value: 4f,
                type: "float");

            QueueTest("pointer/interpolate", GetCallerName(), "Interpolate Pointer w/ ReadOnly Pointer", "Tests that an interpolate node activates the err output flow when a readonly pointer is used. Test fails if out or done flows are activated or err flow is not activated.", g, importer.Result);
        }

        [UnityTest]
        public IEnumerator PointerSetGet_ValuesInGetAreWhatWereSet()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            for (int i = 0; i < MATERIAL_POINTERS.Length; i++)
            {
                var pointer = MATERIAL_POINTERS[i].pointer.Replace('/', '_').Replace("[nodeIndex]", "0");
                QueueTest("pointer/set", $"SetAndGetPointer{pointer}", $"Pointer Set/Get {pointer}", $"Tests that pointer/set and pointer/get work for {pointer}.", CreatePointerSetGraph(MATERIAL_POINTERS[i].pointer, MATERIAL_POINTERS[i].type), importer.Result);
            }
        }

        [UnityTest]
        public IEnumerator PointerSet_InvalidParameter()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            var g = CreateSetErrGraph(pointer: "/nodes/[nodeIndex]/translation", nodeIndex: -1, value: new float3(1f, 2f, 3f), type: "float3");

            QueueTest("pointer/set", GetCallerName(), "Set Pointer w/ Negative Parameter", "Tests that a pointer with a {parameter} in it triggers the err output flow when that value is negative. Test fails if out flow is activated or err flow is not activated.", g, importer.Result);
        }

        [UnityTest]
        public IEnumerator PointerSet_TypeMismatch()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            var g = CreateSetErrGraph(pointer: "/nodes/[nodeIndex]/translation", nodeIndex: 0, value: 4, type: "int");

            QueueTest("pointer/set", GetCallerName(), "Set Pointer w/ Type Mismatch", "Tests that a pointer with a value/config type that does not match the object model type activates the err flow. Test fails if out flow is activated or err flow is not activated.", g, importer.Result);
        }

        [UnityTest]
        public IEnumerator PointerSet_UnsupportedPointer()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            var g = CreateSetErrGraph(pointer: "/nodes/[nodeIndex]/unsupported", nodeIndex: 0, value: 4, type: "int");

            QueueTest("pointer/set", GetCallerName(), "Set Pointer w/ Unsupported Pointer", "Tests that a set node activates the err output flow when an unsupported pointer is used. Test fails if out flow is activated or err flow is not activated.", g, importer.Result);
        }

        [UnityTest]
        public IEnumerator PointerSet_ReadOnlyPointer()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            var g = CreateSetErrGraph(pointer: "/nodes/[nodeIndex]/weights.length", nodeIndex: 0, value: 4, type: "int");

            QueueTest("pointer/set", GetCallerName(), "Set Pointer w/ ReadOnly Pointer", "Tests that a set node activates the err output flow when a readonly pointer is used. Test fails if out flow is activated or err flow is not activated.", g, importer.Result);
        }

        [UnityTest]
        public IEnumerator PointerGet_InvalidParameter()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            var g = CreateGetErrGraph(pointer: "/materials/[nodeIndex]/alphaCutoff", nodeIndex: -1, type: "float");

            QueueTest("pointer/get", GetCallerName(), "Get Pointer w/ Invalid Parameter", "Tests a pointer/get node with an invalid node index parameter. Test fails if isValid output value is true or the value output is not the default for the given type.", g, importer.Result);
        }

        [UnityTest]
        public IEnumerator PointerGet_UnsupportedPointer()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            var g = CreateGetErrGraph(pointer: "/materials/[nodeIndex]/unsupported", nodeIndex: 0, type: "float");

            QueueTest("pointer/get", GetCallerName(), "Get Pointer w/ Unsupported Pointer", "Tests a pointer/get node with an unsupported pointer. Test fails if isValid output value is true or the value output is not the default for the given type.", g, importer.Result);
        }

        [UnityTest]
        public IEnumerator PointerGet_IsValid()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            var g = CreateGetValidGraph(pointer: "/materials/[nodeIndex]/alphaCutoff", nodeIndex: 0, type: "float");

            QueueTest("pointer/get", GetCallerName(), "Get Pointer, Check IsValid", "Tests that a pointer/get node with a supported pointer has IsValid == true. Test fails if isValid output value is false.", g, importer.Result);
        }

        private Graph CreatePointerInterpolateGraph(string pointer, string type)
        {
            var g = CreateGraphForTest();

            IProperty value = type switch
            {
                "float" => new Property<float>(0.7f),
                "float2" => new Property<float2>(new float2(0.7f, 0.5f)),
                "float3" => new Property<float3>(new float3(0.7f, 0.5f, 0.3f)),
                "float4" => new Property<float4>(new float4(0.7f, 0.5f, 0.3f, 0.2f)),
                _ => throw new InvalidOperationException(),
            };

            Util.Log($"{pointer} with type {type}");

            var typeIndex = g.IndexOfType(type);
            var onStartNode = g.CreateNode("event/onStart");
            var pointerInterpolateNode = g.CreateNode("pointer/interpolate");

            pointerInterpolateNode.AddValue(ConstStrings.NODE_INDEX, 0);
            pointerInterpolateNode.AddConfiguration(ConstStrings.TYPE, typeIndex);
            pointerInterpolateNode.AddConfiguration(ConstStrings.POINTER, pointer);
            pointerInterpolateNode.AddValue(ConstStrings.VALUE, value);
            pointerInterpolateNode.AddValue(ConstStrings.DURATION, INTERPOLATION_DURATION);
            pointerInterpolateNode.AddValue(ConstStrings.P1, P1);
            pointerInterpolateNode.AddValue(ConstStrings.P2, P2);

            var get = g.CreateNode("pointer/get");
            get.AddConfiguration(ConstStrings.POINTER, pointer);
            get.AddConfiguration(ConstStrings.TYPE, typeIndex);
            get.AddValue(ConstStrings.NODE_INDEX, 0);

            var branch = g.CreateNode("flow/branch");

            // Unity material properties may not round-trip bit-exactly (e.g. color space conversion), so compare with a tolerance.
            branch.AddConnectedValue(ConstStrings.CONDITION, CreateApproximatelyEqual(g, get, value, type));

            var complete = g.CreateNode("event/send");
            var failLog = CreateFailSubGraph(g, $"Get did not return the correct value for pointer {pointer}." + " Expected: {expected}, Actual: {actual}");
            failLog.AddConnectedValue(ConstStrings.ACTUAL, get);
            failLog.AddValue(ConstStrings.EXPECTED, value);
            failLog.AddValue(ConstStrings.NODE_INDEX, 0);

            onStartNode.AddFlow(pointerInterpolateNode);
            branch.AddFlow(complete, ConstStrings.TRUE);
            branch.AddFlow(failLog, ConstStrings.FALSE);
            complete.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            var outGet = g.CreateNode("variable/get");
            var outVar = g.AddVariable("outFlowActivated", false);
            var outVarIndex = g.IndexOfVariable(outVar);
            var outBranch = g.CreateNode("flow/branch");
            var outFail = CreateFailSubGraph(g, "Out flow did not trigger during this test.");
            var outSet = NodeTestHelpers.CreateVariableSet(g, outVarIndex, true);

            outGet.AddConfiguration(ConstStrings.VARIABLE, outVarIndex);

            outBranch.AddConnectedValue(ConstStrings.CONDITION, outGet);
            outBranch.AddFlow(branch, ConstStrings.TRUE);
            outBranch.AddFlow(outFail, ConstStrings.FALSE);

            var fail = CreateFailSubGraph(g, "Err flow should not trigger during this test.");
            pointerInterpolateNode.AddFlow(fail, ConstStrings.ERR);
            pointerInterpolateNode.AddFlow(outSet);
            pointerInterpolateNode.AddFlow(outBranch, ConstStrings.DONE);

            return g;
        }

        private static Graph CreatePointerSetGraph(string pointer, string type)
        {
            var g = CreateGraphForTest();

            IProperty value = type switch
            {
                "float" => new Property<float>(0.7f),
                "float2" => new Property<float2>(new float2(0.7f, 0.5f)),
                "float3" => new Property<float3>(new float3(0.7f, 0.5f, 0.3f)),
                "float4" => new Property<float4>(new float4(0.7f, 0.5f, 0.3f, 0.2f)),
                _ => throw new InvalidOperationException(),
            };

            Util.Log($"{pointer} with type {type}");

            var typeIndex = g.IndexOfType(type);
            var onStartNode = g.CreateNode("event/onStart");
            var pointerSetNode = g.CreateNode("pointer/set");

            pointerSetNode.AddValue(ConstStrings.NODE_INDEX, 0);
            pointerSetNode.AddConfiguration(ConstStrings.TYPE, typeIndex);
            pointerSetNode.AddConfiguration(ConstStrings.POINTER, pointer);
            pointerSetNode.AddValue(ConstStrings.VALUE, value);

            var get = g.CreateNode("pointer/get");
            get.AddConfiguration(ConstStrings.POINTER, pointer);
            get.AddConfiguration(ConstStrings.TYPE, typeIndex);
            get.AddValue(ConstStrings.NODE_INDEX, 0);

            var branch = g.CreateNode("flow/branch");

            // Unity material properties may not round-trip bit-exactly (e.g. color space conversion), so compare with a tolerance.
            branch.AddConnectedValue(ConstStrings.CONDITION, CreateApproximatelyEqual(g, get, value, type));

            var complete = g.CreateNode("event/send");
            var failLog = CreateFailSubGraph(g, $"Get did not return the correct value for pointer {pointer}." + " Expected: {expected}, Actual: {actual}");
            failLog.AddConnectedValue(ConstStrings.ACTUAL, get);
            failLog.AddValue(ConstStrings.EXPECTED, value);
            failLog.AddValue(ConstStrings.NODE_INDEX, 0);

            onStartNode.AddFlow(pointerSetNode);
            pointerSetNode.AddFlow(branch);
            branch.AddFlow(complete, ConstStrings.TRUE);
            branch.AddFlow(failLog, ConstStrings.FALSE);
            complete.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            var fail = CreateFailSubGraph(g, "Err flow should not trigger during this test.");
            pointerSetNode.AddFlow(fail, ConstStrings.ERR);
            return g;
        }

        private const float VALUE_TOLERANCE = 1e-4f;

        /// <summary>
        /// Builds a bool-valued node that is true when the "value" output of <paramref name="source"/> is within
        /// <see cref="VALUE_TOLERANCE"/> of <paramref name="expected"/>: |a - b| for float, length(a - b) for floatN.
        /// </summary>
        private static Node CreateApproximatelyEqual(Graph g, Node source, IProperty expected, string type)
        {
            var sub = g.CreateNode("math/sub");
            sub.AddConnectedValue(ConstStrings.A, source);
            sub.AddValue(ConstStrings.B, expected);

            var distance = g.CreateNode(type == "float" ? "math/abs" : "math/length");
            distance.AddConnectedValue(ConstStrings.A, sub);

            var le = g.CreateNode("math/le");
            le.AddConnectedValue(ConstStrings.A, distance);
            le.AddValue(ConstStrings.B, VALUE_TOLERANCE);

            return le;
        }

        private static Graph CreateInterpolateErrGraph<T>(string pointer, int nodeIndex, float duration, float2 p1, float2 p2, T value, string type)
        {
            const float TEST_DURATION = 1f;
            var g = CreateGraphForTest();

            var onStartNode = g.CreateNode("event/onStart");
            var interp = g.CreateNode("pointer/interpolate");

            interp.AddConfiguration(ConstStrings.TYPE, g.IndexOfType(type));
            interp.AddConfiguration(ConstStrings.POINTER, pointer);
            interp.AddValue(ConstStrings.NODE_INDEX, nodeIndex);
            interp.AddValue(ConstStrings.VALUE, value);
            interp.AddValue(ConstStrings.P1, p1);
            interp.AddValue(ConstStrings.P2, p2);
            interp.AddValue(ConstStrings.DURATION, duration);

            onStartNode.AddFlow(interp);

            var complete = g.CreateNode("event/send");
            complete.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            var failOut = CreateFailSubGraph(g, "Out flow should not activate during this test.");
            var failDone = CreateFailSubGraph(g, "Done flow should not activate during this test.");
            var failErr = CreateFailSubGraph(g, "Err flow did not activate during this test.");

            var errFlowVar = g.AddVariable("errFlowActivated", false);
            var errFlowVarIndex = g.IndexOfVariable(errFlowVar);
            var errFlowSet = NodeTestHelpers.CreateVariableSet(g, errFlowVarIndex, true);
            var errFlowGet = g.CreateNode("variable/get");
            var onTick = g.CreateNode("event/onTick");
            var ge = g.CreateNode("math/ge");
            var timeBranch = g.CreateNode("flow/branch");
            var doneBranch = g.CreateNode("flow/branch");

            errFlowGet.AddConfiguration(ConstStrings.VARIABLE, errFlowVarIndex);

            onTick.AddFlow(timeBranch);

            ge.AddConnectedValue(ConstStrings.A, onTick, ConstStrings.TIME_SINCE_START);
            ge.AddValue(ConstStrings.B, TEST_DURATION);

            timeBranch.AddConnectedValue(ConstStrings.CONDITION, ge);
            timeBranch.AddFlow(doneBranch, ConstStrings.TRUE);

            doneBranch.AddConnectedValue(ConstStrings.CONDITION, errFlowGet);
            doneBranch.AddFlow(complete, ConstStrings.TRUE);
            doneBranch.AddFlow(failErr, ConstStrings.FALSE);

            interp.AddFlow(errFlowSet, ConstStrings.ERR);
            interp.AddFlow(failOut, ConstStrings.OUT);
            interp.AddFlow(failDone, ConstStrings.DONE);
            return g;
        }

        private static Graph CreateSetErrGraph<T>(string pointer, int nodeIndex, T value, string type)
        {
            var g = CreateGraphForTest();

            var onStartNode = g.CreateNode("event/onStart");
            var pointerSetNode = g.CreateNode("pointer/set");

            pointerSetNode.AddConfiguration(ConstStrings.TYPE, g.IndexOfType(type));
            pointerSetNode.AddConfiguration(ConstStrings.POINTER, pointer);
            pointerSetNode.AddValue(ConstStrings.NODE_INDEX, nodeIndex);
            pointerSetNode.AddValue(ConstStrings.VALUE, value);

            onStartNode.AddFlow(pointerSetNode);

            var complete = g.CreateNode("event/send");
            complete.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            var failOut = CreateFailSubGraph(g, "Out flow should not activate during this test.");
            pointerSetNode.AddFlow(complete, ConstStrings.ERR);
            pointerSetNode.AddFlow(failOut);

            return g;
        }

        private static Graph CreateGetErrGraph(string pointer, int nodeIndex, string type)
        {
            var g = CreateGraphForTest();

            var onStartNode = g.CreateNode("event/onStart");
            var branch = g.CreateNode("flow/branch");
            var get = g.CreateNode("pointer/get");
            var typeIndex = g.IndexOfType(type);

            get.AddConfiguration(ConstStrings.TYPE, typeIndex);
            get.AddConfiguration(ConstStrings.POINTER, pointer);
            get.AddValue(ConstStrings.NODE_INDEX, nodeIndex);

            onStartNode.AddFlow(branch);

            var complete = g.CreateNode("event/send");
            complete.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            var failOut = CreateFailSubGraph(g, "isValid Should be false for this test.");

            var isnan = g.CreateNode("math/isNaN");
            var defaultBranch = g.CreateNode("flow/branch");
            var failDefault = CreateFailSubGraph(g, $"Output value should be the default for type {type}.");

            isnan.AddConnectedValue(ConstStrings.A, get);

            defaultBranch.AddConnectedValue(ConstStrings.CONDITION, isnan);
            defaultBranch.AddFlow(failDefault, ConstStrings.FALSE);
            defaultBranch.AddFlow(complete, ConstStrings.TRUE);

            branch.AddFlow(defaultBranch, ConstStrings.FALSE);
            branch.AddFlow(failOut, ConstStrings.TRUE);
            branch.AddConnectedValue(ConstStrings.CONDITION, get, ConstStrings.IS_VALID);

            return g;
        }

        private static Graph CreateGetValidGraph(string pointer, int nodeIndex, string type)
        {
            var g = CreateGraphForTest();

            var onStartNode = g.CreateNode("event/onStart");
            var branch = g.CreateNode("flow/branch");
            var get = g.CreateNode("pointer/get");
            var typeIndex = g.IndexOfType(type);

            get.AddConfiguration(ConstStrings.TYPE, typeIndex);
            get.AddConfiguration(ConstStrings.POINTER, pointer);
            get.AddValue(ConstStrings.NODE_INDEX, nodeIndex);

            onStartNode.AddFlow(branch);

            var complete = g.CreateNode("event/send");
            complete.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            var failOut = CreateFailSubGraph(g, "isValid Should be true for this test.");

            branch.AddConnectedValue(ConstStrings.CONDITION, get, ConstStrings.IS_VALID);
            branch.AddFlow(complete, ConstStrings.TRUE);
            branch.AddFlow(failOut, ConstStrings.FALSE);

            return g;
        }

        private const string NODE_REF = "node";
        private const string NODE_TRANSLATION_BY_REF = "/nodes/{" + NODE_REF + "}/translation";
        private const string EXTENSION_ROOT = "/extensions/KHR_interactivity";

        [UnityTest]
        public IEnumerator PointerSetGet_ReferenceParameter_ValueRoundTrips()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            QueueTest("pointer/set", GetCallerName(), "Pointer Set/Get With Reference Parameter", "Sets and gets /nodes/{node}/translation with a node reference parameter. Test fails if either operation fails or the value read differs from the value set.", CreateReferenceSetGetGraph(), importer.Result);
        }

        [UnityTest]
        public IEnumerator PointerGet_InvalidReferenceParameter_IsValidFalse()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            QueueTest("pointer/get", "PointerGet_NullReference", "Get Pointer w/ Null Reference", "Gets /nodes/{node}/translation with a null reference. Test fails if isValid is true or the value is not the type default.", CreateReferenceGetErrGraph(Ref.Null), importer.Result);
            QueueTest("pointer/get", "PointerGet_ReferenceToOtherCollection", "Get Pointer w/ Material Reference For A Node", "Gets /nodes/{node}/translation with a reference to a material. Test fails if isValid is true or the value is not the type default.", CreateReferenceGetErrGraph(Ref.Gltf("/materials", 0)), importer.Result);
        }

        [UnityTest]
        public IEnumerator PointerSet_NullReferenceParameter_ActivatesErrFlow()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            QueueTest("pointer/set", GetCallerName(), "Set Pointer w/ Null Reference", "Sets /nodes/{node}/translation with a null reference. Test fails if the out flow activates or the err flow does not.", CreateReferenceSetErrGraph(), importer.Result);
        }

        [UnityTest]
        public IEnumerator PointerGet_TypeMismatch_IsValidFalse()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            var g = CreateGetErrGraph(pointer: "/nodes/[nodeIndex]/translation", nodeIndex: 0, type: "float");
            QueueTest("pointer/get", GetCallerName(), "Get Pointer w/ Type Mismatch", "Gets a float3 property with a float type configuration. Test fails if isValid is true or the value is not the type default.", g, importer.Result);
        }

        [UnityTest]
        public IEnumerator PointerGet_AssetCapabilities()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            QueueTest("pointer/get", "PointerGet_AssetMajorVersion", "Get Asset Major Version", "Test fails if asset/majorVersion is not valid or not 2.", CreateVirtualPointerValueGraph($"{EXTENSION_ROOT}/asset/majorVersion", "int", P(2)), importer.Result);
            QueueTest("pointer/get", "PointerGet_AssetMinorVersion", "Get Asset Minor Version", "Test fails if asset/minorVersion is not valid or not 0.", CreateVirtualPointerValueGraph($"{EXTENSION_ROOT}/asset/minorVersion", "int", P(0)), importer.Result);
            QueueTest("pointer/get", "PointerGet_UsedExtensionEnabled", "Get Used Extension Enabled", "pointer_test uses KHR_materials_clearcoat. Test fails if its enabled property is not valid or not true.", CreateVirtualPointerValueGraph($"{EXTENSION_ROOT}/asset/extensions/KHR_materials_clearcoat/enabled", "bool", P(true)), importer.Result);
            QueueTest("pointer/get", "PointerGet_UnusedExtensionEnabled", "Get Unused Extension Enabled", "pointer_test does not use KHR_materials_variants. Test fails if its enabled property is valid.", CreateVirtualPointerInvalidGraph($"{EXTENSION_ROOT}/asset/extensions/KHR_materials_variants/enabled", "bool"), importer.Result);
        }

        [UnityTest]
        public IEnumerator PointerGet_RuntimeLimits_AtLeastOne()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            foreach (var limit in new[] { "maxActiveAnimations", "maxActiveDelays", "maxActivePropertyInterpolations", "maxActiveVariableInterpolations" })
            {
                QueueTest("pointer/get", $"PointerGet_Limit_{limit}", $"Get Limit {limit}", $"Test fails if limits/{limit} is not valid or is less than 1.", CreateLimitGraph($"{EXTENSION_ROOT}/limits/{limit}"), importer.Result);
            }
        }

        [UnityTest]
        public IEnumerator PointerGet_ActiveCamera_IsValid()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            QueueTest("pointer/get", "PointerGet_ActiveCameraPosition", "Get Active Camera Position", "Test fails if activeCamera/position is not valid as float3.", CreateVirtualPointerValidGraph($"{EXTENSION_ROOT}/activeCamera/position", "float3"), importer.Result);
            QueueTest("pointer/get", "PointerGet_ActiveCameraRotation", "Get Active Camera Rotation", "Test fails if activeCamera/rotation is not valid as float4.", CreateVirtualPointerValidGraph($"{EXTENSION_ROOT}/activeCamera/rotation", "float4"), importer.Result);
            QueueTest("pointer/get", "PointerGet_ActiveCameraYFov", "Get Active Camera YFov", "Test fails if activeCamera/perspective/yfov is not valid as float.", CreateVirtualPointerValidGraph($"{EXTENSION_ROOT}/activeCamera/perspective/yfov", "float"), importer.Result);
            QueueTest("pointer/get", "PointerGet_ActiveCameraWrongType", "Get Active Camera Position As float4", "Test fails if activeCamera/position is valid when read as float4.", CreateVirtualPointerInvalidGraph($"{EXTENSION_ROOT}/activeCamera/position", "float4"), importer.Result);
        }

        [UnityTest]
        public IEnumerator PointerGet_DelayReference_ValidOnlyWhileScheduled()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            QueueTest("pointer/get", GetCallerName(), "Get Delay Reference", "Test fails if /extensions/KHR_interactivity/delays/{delay} is not valid while the delay is scheduled, returns a different reference, or is valid after the delay is cancelled.", CreateDelayReferenceGraph(), importer.Result);
        }

        [UnityTest]
        public IEnumerator PointerGet_EventReference_ValidOnlyForEvents()
        {
            var importer = LoadTestModel(TEST_GLB);
            while (!importer.IsCompleted)
            {
                yield return null;
            }

            QueueTest("pointer/get", GetCallerName(), "Get Event Reference", "Test fails if /extensions/KHR_interactivity/events/{event} is not valid for the onStart event reference or is valid for a null or delay reference.", CreateEventReferenceGraph(), importer.Result);
        }

        private static Node CreatePointerGet(Graph g, string pointer, string type)
        {
            var get = g.CreateNode("pointer/get");
            get.AddConfiguration(ConstStrings.POINTER, pointer);
            get.AddConfiguration(ConstStrings.TYPE, g.IndexOfType(type));
            return get;
        }

        private static Graph CreateReferenceSetGetGraph()
        {
            var g = CreateGraphForTest();
            var value = new float3(0.7f, 0.5f, 0.3f);
            var node = Ref.Gltf("/nodes", 0);

            var start = g.CreateNode("event/onStart");
            var set = g.CreateNode("pointer/set");
            set.AddConfiguration(ConstStrings.POINTER, NODE_TRANSLATION_BY_REF);
            set.AddConfiguration(ConstStrings.TYPE, g.IndexOfType("float3"));
            set.AddValue(NODE_REF, node);
            set.AddValue(ConstStrings.VALUE, value);
            start.AddFlow(set);
            set.AddFlow(CreateFailSubGraph(g, "pointer/set with a reference parameter activated its err flow."), ConstStrings.ERR);

            var get = CreatePointerGet(g, NODE_TRANSLATION_BY_REF, "float3");
            get.AddValue(NODE_REF, node);

            var valid = CreateAssertTrue(g, get, ConstStrings.IS_VALID, "pointer/get with a reference parameter is not valid.");
            var same = CreateAssertTrue(g, CreateApproximatelyEqual(g, get, P(value), "float3"), "pointer/get with a reference parameter returned a different value than was set.");
            set.AddFlow(valid);
            valid.AddFlow(same, ConstStrings.TRUE);
            same.AddFlow(CreateCompleteNode(g), ConstStrings.TRUE);

            return g;
        }

        private static Graph CreateReferenceGetErrGraph(Ref node)
        {
            var g = CreateGraphForTest();

            var start = g.CreateNode("event/onStart");
            var get = CreatePointerGet(g, NODE_TRANSLATION_BY_REF, "float3");
            get.AddValue(NODE_REF, node);

            var invalid = CreateAssertTrue(g, CreateNot(g, get, ConstStrings.IS_VALID), "isValid should be false for this reference.");
            var extract = g.CreateNode("math/extract3");
            extract.AddConnectedValue(ConstStrings.A, get);
            var isNaN = g.CreateNode("math/isNaN");
            isNaN.AddConnectedValue(ConstStrings.A, extract, "0");
            var isDefault = CreateAssertTrue(g, isNaN, "The value should be the float3 type default (NaN).");

            start.AddFlow(invalid);
            invalid.AddFlow(isDefault, ConstStrings.TRUE);
            isDefault.AddFlow(CreateCompleteNode(g), ConstStrings.TRUE);

            return g;
        }

        private static Graph CreateReferenceSetErrGraph()
        {
            var g = CreateGraphForTest();

            var start = g.CreateNode("event/onStart");
            var set = g.CreateNode("pointer/set");
            set.AddConfiguration(ConstStrings.POINTER, NODE_TRANSLATION_BY_REF);
            set.AddConfiguration(ConstStrings.TYPE, g.IndexOfType("float3"));
            set.AddValue(NODE_REF, Ref.Null);
            set.AddValue(ConstStrings.VALUE, new float3(1f, 2f, 3f));
            start.AddFlow(set);

            set.AddFlow(CreateFailSubGraph(g, "The out flow activated for a null reference parameter."), ConstStrings.OUT);
            set.AddFlow(CreateCompleteNode(g), ConstStrings.ERR);

            return g;
        }

        private static Graph CreateVirtualPointerValueGraph(string pointer, string type, IProperty expected)
        {
            var g = CreateGraphForTest();

            var start = g.CreateNode("event/onStart");
            var get = CreatePointerGet(g, pointer, type);

            var eq = g.CreateNode("math/eq");
            eq.AddConnectedValue(ConstStrings.A, get);
            eq.AddValue(ConstStrings.B, expected);

            var valid = CreateAssertTrue(g, get, ConstStrings.IS_VALID, $"{pointer} should be valid.");
            var value = CreateAssertTrue(g, eq, $"{pointer} should be {expected}.");

            start.AddFlow(valid);
            valid.AddFlow(value, ConstStrings.TRUE);
            value.AddFlow(CreateCompleteNode(g), ConstStrings.TRUE);

            return g;
        }

        private static Graph CreateVirtualPointerValidGraph(string pointer, string type)
        {
            var g = CreateGraphForTest();

            var start = g.CreateNode("event/onStart");
            var valid = CreateAssertTrue(g, CreatePointerGet(g, pointer, type), ConstStrings.IS_VALID, $"{pointer} should be valid as {type}.");
            start.AddFlow(valid);
            valid.AddFlow(CreateCompleteNode(g), ConstStrings.TRUE);

            return g;
        }

        private static Graph CreateVirtualPointerInvalidGraph(string pointer, string type)
        {
            var g = CreateGraphForTest();

            var start = g.CreateNode("event/onStart");
            var invalid = CreateAssertTrue(g, CreateNot(g, CreatePointerGet(g, pointer, type), ConstStrings.IS_VALID), $"{pointer} should not be valid as {type}.");
            start.AddFlow(invalid);
            invalid.AddFlow(CreateCompleteNode(g), ConstStrings.TRUE);

            return g;
        }

        private static Graph CreateLimitGraph(string pointer)
        {
            var g = CreateGraphForTest();

            var start = g.CreateNode("event/onStart");
            var get = CreatePointerGet(g, pointer, "int");
            var ge = g.CreateNode("math/ge");
            ge.AddConnectedValue(ConstStrings.A, get);
            ge.AddValue(ConstStrings.B, 1);

            var valid = CreateAssertTrue(g, get, ConstStrings.IS_VALID, $"{pointer} should be valid.");
            var atLeastOne = CreateAssertTrue(g, ge, $"{pointer} should be at least 1.");
            start.AddFlow(valid);
            valid.AddFlow(atLeastOne, ConstStrings.TRUE);
            atLeastOne.AddFlow(CreateCompleteNode(g), ConstStrings.TRUE);

            return g;
        }

        private static Graph CreateDelayReferenceGraph()
        {
            const float LONG_DURATION = 30f;
            const string DELAY_REF = "delay";
            var g = CreateGraphForTest();

            var start = g.CreateNode("event/onStart");
            var sequence = g.CreateNode("flow/sequence");
            start.AddFlow(sequence);

            var delay = g.CreateNode("flow/setDelay");
            delay.AddValue(ConstStrings.DURATION, LONG_DURATION);

            var get = CreatePointerGet(g, EXTENSION_ROOT + "/delays/{" + DELAY_REF + "}", "ref");
            get.AddConnectedValue(DELAY_REF, delay, ConstStrings.LAST_DELAY);

            var validWhileScheduled = CreateAssertTrue(g, get, ConstStrings.IS_VALID, "The delay reference should be valid while the delay is scheduled.");
            var sameReference = CreateAssertTrue(g, CreateRefEquals(g, get, ConstStrings.VALUE, delay, ConstStrings.LAST_DELAY), "The delay pointer should return the delay reference.");
            validWhileScheduled.AddFlow(sameReference, ConstStrings.TRUE);
            var invalidAfterCancel = CreateAssertTrue(g, CreateNot(g, get, ConstStrings.IS_VALID), "The delay reference should not be valid after the delay is cancelled.");

            sequence.AddFlow(delay, "0");
            sequence.AddFlow(validWhileScheduled, "1");
            sequence.AddFlow(delay, "2", ConstStrings.CANCEL);
            sequence.AddFlow(invalidAfterCancel, "3");
            invalidAfterCancel.AddFlow(CreateCompleteNode(g), ConstStrings.TRUE);

            return g;
        }

        private static Graph CreateEventReferenceGraph()
        {
            const string EVENT_REF = "event";
            var pointer = EXTENSION_ROOT + "/events/{" + EVENT_REF + "}";
            var g = CreateGraphForTest();

            var start = g.CreateNode("event/onStart");
            var sequence = g.CreateNode("flow/sequence");
            start.AddFlow(sequence);

            var eventGet = CreatePointerGet(g, pointer, "ref");
            eventGet.AddConnectedValue(EVENT_REF, start, ConstStrings.EVENT);

            var nullGet = CreatePointerGet(g, pointer, "ref");
            nullGet.AddValue(EVENT_REF, Ref.Null);

            var delay = g.CreateNode("flow/setDelay");
            delay.AddValue(ConstStrings.DURATION, 30f);
            var delayGet = CreatePointerGet(g, pointer, "ref");
            delayGet.AddConnectedValue(EVENT_REF, delay, ConstStrings.LAST_DELAY);

            var eventValid = CreateAssertTrue(g, eventGet, ConstStrings.IS_VALID, "The onStart event reference should be a valid event reference.");
            var nullInvalid = CreateAssertTrue(g, CreateNot(g, nullGet, ConstStrings.IS_VALID), "A null reference should not be a valid event reference.");
            var delayInvalid = CreateAssertTrue(g, CreateNot(g, delayGet, ConstStrings.IS_VALID), "A delay reference should not be a valid event reference.");

            sequence.AddFlow(eventValid, "0");
            sequence.AddFlow(nullInvalid, "1");
            sequence.AddFlow(delay, "2");
            sequence.AddFlow(delayInvalid, "3");
            sequence.AddFlow(delay, "4", ConstStrings.CANCEL);
            sequence.AddFlow(CreateCompleteNode(g), "5");

            return g;
        }
    }
}
