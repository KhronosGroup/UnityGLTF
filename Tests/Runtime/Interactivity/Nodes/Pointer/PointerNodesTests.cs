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
                var pointer = MATERIAL_POINTERS[i].pointer.Replace('/', '_');
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
                pointer: "/nodes/{nodeIndex}/translation",
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
                pointer: "/nodes/{nodeIndex}/translation",
                nodeIndex: 0,
                duration: 0.5f,
                p1: new float2(0.42f, 0f),
                p2: new float2(0.52f, 1f),
                value: 4,
                type: "int");

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
                            pointer: "/nodes/{nodeIndex}/unsupported",
                            nodeIndex: 0,
                            duration: 0.5f,
                            p1: new float2(0.42f, 0f),
                            p2: new float2(0.52f, 1f),
                            value: 4,
                            type: "int");
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

            var g1 = CreateInterpolateErrGraph(pointer: "/nodes/{nodeIndex}/translation", nodeIndex: 0, duration: -1f, p1: new float2(0.42f, 0f), p2: new float2(0.52f, 1f), value: new float3(1f, 1f, 1f), type: "float3");
            var g2 = CreateInterpolateErrGraph(pointer: "/nodes/{nodeIndex}/translation", nodeIndex: 0, duration: float.PositiveInfinity, p1: new float2(0.42f, 0f), p2: new float2(0.52f, 1f), value: new float3(1f, 1f, 1f), type: "float3");
            var g3 = CreateInterpolateErrGraph(pointer: "/nodes/{nodeIndex}/translation", nodeIndex: 0, duration: float.NaN, p1: new float2(0.42f, 0f), p2: new float2(0.52f, 1f), value: new float3(1f, 1f, 1f), type: "float3");

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
            var g1 = CreateInterpolateErrGraph(pointer: "/nodes/{nodeIndex}/translation", nodeIndex: 0, duration: 0.5f, p1: new float2(-1f, 0f), p2: new float2(0.52f, 1f), value: new float3(1f, 1f, 1f), type: "float3");
            var g2 = CreateInterpolateErrGraph(pointer: "/nodes/{nodeIndex}/translation", nodeIndex: 0, duration: 0.5f, p1: new float2(0f, float.PositiveInfinity), p2: new float2(0.52f, 1f), value: new float3(1f, 1f, 1f), type: "float3");
            var g3 = CreateInterpolateErrGraph(pointer: "/nodes/{nodeIndex}/translation", nodeIndex: 0, duration: 0.5f, p1: new float2(0.5f, float.NaN), p2: new float2(0.52f, 1f), value: new float3(1f, 1f, 1f), type: "float3");

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
            var g1 = CreateInterpolateErrGraph(pointer: "/nodes/{nodeIndex}/translation", nodeIndex: 0, duration: -1f, p1: new float2(0.42f, 0f), p2: new float2(-1f, 0f), value: new float3(1f, 1f, 1f), type: "float3");
            var g2 = CreateInterpolateErrGraph(pointer: "/nodes/{nodeIndex}/translation", nodeIndex: 0, duration: float.PositiveInfinity, p1: new float2(0.42f, 0f), p2: new float2(0f, float.PositiveInfinity), value: new float3(1f, 1f, 1f), type: "float3");
            var g3 = CreateInterpolateErrGraph(pointer: "/nodes/{nodeIndex}/translation", nodeIndex: 0, duration: float.NaN, p1: new float2(0.42f, 0f), p2: new float2(0.5f, float.NaN), value: new float3(1f, 1f, 1f), type: "float3");

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
                pointer: "/nodes/{nodeIndex}/weights.length",
                nodeIndex: 0,
                duration: 0.5f,
                p1: new float2(0.42f, 0f),
                p2: new float2(0.52f, 1f),
                value: 4,
                type: "int");

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
                var pointer = MATERIAL_POINTERS[i].pointer.Replace('/', '_');
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

            var g = CreateSetErrGraph(pointer: "/nodes/{nodeIndex}/translation", nodeIndex: -1, value: new float3(1f, 2f, 3f), type: "float3");

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

            var g = CreateSetErrGraph(pointer: "/nodes/{nodeIndex}/translation", nodeIndex: 0, value: 4, type: "int");

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

            var g = CreateSetErrGraph(pointer: "/nodes/{nodeIndex}/unsupported", nodeIndex: 0, value: 4, type: "int");

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

            var g = CreateSetErrGraph(pointer: "/nodes/{nodeIndex}/weights.length", nodeIndex: 0, value: 4, type: "int");

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

            var g = CreateGetErrGraph(pointer: "/materials/{nodeIndex}/alphaCutoff", nodeIndex: -1, type: "float");

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

            var g = CreateGetErrGraph(pointer: "/materials/{nodeIndex}/unsupported", nodeIndex: 0, type: "float");

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

            var g = CreateGetValidGraph(pointer: "/materials/{nodeIndex}/alphaCutoff", nodeIndex: 0, type: "float");

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
            var eq = g.CreateNode("math/eq");

            branch.AddConnectedValue(ConstStrings.CONDITION, eq);

            eq.AddConnectedValue(ConstStrings.A, get);
            eq.AddValue(ConstStrings.B, value);

            var complete = g.CreateNode("event/send");
            var failLog = CreateFailSubGraph(g, $"Get did not return the correct value for pointer {pointer}." + " Expected: {expected}, Actual: {actual}");
            failLog.AddConnectedValue(ConstStrings.ACTUAL, get);
            failLog.AddValue(ConstStrings.EXPECTED, value);
            failLog.AddValue(ConstStrings.NODE_INDEX, 0);

            onStartNode.AddFlow(pointerInterpolateNode);
            branch.AddFlow(complete, ConstStrings.TRUE);
            branch.AddFlow(failLog, ConstStrings.FALSE);
            complete.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            var outSet = g.CreateNode("variable/set");
            var outGet = g.CreateNode("variable/get");
            var outVar = g.AddVariable("outFlowActivated", false);
            var outVarIndex = g.IndexOfVariable(outVar);
            var outBranch = g.CreateNode("flow/branch");
            var outFail = CreateFailSubGraph(g, "Out flow did not trigger during this test.");

            outSet.AddConfiguration(ConstStrings.VARIABLE, outVarIndex);
            outGet.AddConfiguration(ConstStrings.VARIABLE, outVarIndex);
            outSet.AddValue(ConstStrings.VALUE, true);

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
            var eq = g.CreateNode("math/eq");

            branch.AddConnectedValue(ConstStrings.CONDITION, eq);

            eq.AddConnectedValue(ConstStrings.A, get);
            eq.AddValue(ConstStrings.B, value);

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
            var errFlowSet = g.CreateNode("variable/set");
            var errFlowGet = g.CreateNode("variable/get");
            var onTick = g.CreateNode("event/onTick");
            var ge = g.CreateNode("math/ge");
            var timeBranch = g.CreateNode("flow/branch");
            var doneBranch = g.CreateNode("flow/branch");

            errFlowSet.AddValue(ConstStrings.VALUE, true);
            errFlowSet.AddConfiguration(ConstStrings.VARIABLE, errFlowVarIndex);
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
    }
}