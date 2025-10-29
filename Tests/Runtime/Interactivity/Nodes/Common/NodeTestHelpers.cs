#define GENERATE_TEST_FILES
#define WRITE_TO_TEST_MANIFEST

using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;
using UnityGLTF.Interactivity.Playback.Extensions;
using UnityGLTF.Loader;

namespace UnityGLTF.Interactivity.Playback.Tests
{
    public abstract class NodeTestHelpers
    {
        public struct TestValues
        {
            public ReadOnlyDictionary<string, Value> values;
            public ReadOnlyDictionary<string, IProperty> expectedResults;
            public Dictionary<string, IProperty> config;
        }

        protected struct ManifestEntry
        {
            public string nodeName;
            public string fileName;
            public string title;
            public string description;
            public string dependencies;
        }

        protected struct TestData
        {
            public string nodeName;
            public string fileName;
            public Graph graph;
            public float executionTimeoutDuration;
            public GLTFSceneImporter importer;
            public string title;
            public string description;
            public TestValues values;

            public ManifestEntry CreateManifestEntry()
            {
                var typeIndexByType = TypesSerializer.GetSystemTypeByIndexDictionary(graph);
                var declarations = DeclarationsSerializer.GetDeclarations(graph.nodes, typeIndexByType);

                var dependencies = new ValueStringBuilder();

                foreach (var declaration in declarations)
                {
                    dependencies.Append(declaration.Key);
                    dependencies.Append(',');
                    dependencies.Append(' ');
                }

                return new ManifestEntry()
                {
                    nodeName = nodeName,
                    fileName = fileName,
                    title = title,
                    description = description,
                    dependencies = dependencies.ToString()
                };
            }
        }

        protected const int FAIL_EVENT_INDEX = 0;
        protected const int COMPLETED_EVENT_INDEX = 1;
        protected const float DEFAULT_MULTI_FRAME_EXECUTION_TIMEOUT = 10f;
        protected const string TEST_GRAPH_SAVE_DIRECTORY = "TestGraphJson";

        protected bool _testSuccessful = false;
        private readonly GraphSerializer _serializer = new(Newtonsoft.Json.Formatting.None);

        protected string _testGraphDirectory;
        protected abstract string _subDirectory { get; }

        protected static readonly List<TestData> _testQueue = new();

        protected string _manifestPath;

        public static readonly ISubGraph[] subGraphs = new ISubGraph[]
        {
            new ApproximatelySubGraph(0.001f),
            new EqualSubGraph(),
            new IsNaNSubGraph(),
            new IsInfSubGraph()
        };

        protected void OnCustomEventFired(int eventIndex, Dictionary<string, IProperty> outValues)
        {
            switch (eventIndex)
            {
                case 0:
                    Assert.Fail("Failure case was triggered by this test.");
                    break;
                case 1:
                    Util.Log("Test was successful.");
                    _testSuccessful = true;
                    break;
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, @"..\"));
            if (!string.IsNullOrWhiteSpace(_subDirectory))
                _testGraphDirectory = Path.Combine(dir, TEST_GRAPH_SAVE_DIRECTORY, _subDirectory);
            else
                _testGraphDirectory = Path.Combine(dir, TEST_GRAPH_SAVE_DIRECTORY);

            Directory.CreateDirectory(_testGraphDirectory);

            _manifestPath = $"{_testGraphDirectory}/manifest.md";

            if (!File.Exists(_manifestPath))
                File.WriteAllText(_manifestPath, "|Filename|Node Type|Title|Description|Dependencies|\n|---|---|---|---|---|\n");
        }

        [SetUp]
        public void SetUp()
        {
            _testQueue.Clear();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var test in _testQueue)
            {
                var endTime = Time.time + test.executionTimeoutDuration;
                _testSuccessful = false;
#if GENERATE_TEST_FILES
#if WRITE_TO_TEST_MANIFEST
                var e = test.CreateManifestEntry();

                File.AppendAllText(_manifestPath, $"|{e.fileName}|{e.nodeName}|{e.title}|{e.description}|{e.dependencies}|\n");
#endif
                Task.Run(() =>
                {
                    // Record graph to json file.
                    if (!string.IsNullOrWhiteSpace(test.fileName))
                    {
                        var extension = new KHR_interactivity()
                        {
                            graphs = new List<Graph>() { test.graph },
                            defaultGraphIndex = 0
                        };
                        using (var stream = File.OpenWrite($"{_testGraphDirectory}/{test.fileName}.json"))
                        using (var writer = new StreamWriter(stream))
                        {
                            writer.NewLine = "\n"; // Use Unix line endings
                            writer.Write(_serializer.Serialize(extension));
                            writer.WriteLine(); // Adds newline at the end
                        }
                    }
                });
#endif

                // Actually run test.
                var eng = CreateBehaviourEngineForGraph(test.graph, OnCustomEventFired, test.importer, startPlayback: true);

                while (!_testSuccessful && Time.time < endTime)
                {
                    eng.Tick();
                    yield return null;
                }

                Assert.IsTrue(_testSuccessful);
            }
        }

        protected static void TestNodeWithAllFloatNInputVariants(string fileName, string testName, string testDescription, string nodeName, float4 a, float4 expected, ComparisonType subGraphType = ComparisonType.Approximately)
        {
            QueueTest(nodeName, $"{fileName}-float-x", $"{testName} (float-x)", $"{testDescription} Uses the x-component of the float4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.x), Out(expected.x), subGraphType));
            QueueTest(nodeName, $"{fileName}-float-y", $"{testName} (float-y)", $"{testDescription}Uses the y-component of the float4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.y), Out(expected.y), subGraphType));
            QueueTest(nodeName, $"{fileName}-float-z", $"{testName} (float-z)", $"{testDescription}Uses the z-component of the float4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.z), Out(expected.z), subGraphType));
            QueueTest(nodeName, $"{fileName}-float-w", $"{testName} (float-w)", $"{testDescription}Uses the w-component of the float4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.w), Out(expected.w), subGraphType));

            QueueTest(nodeName, $"{fileName}-float2-xy", $"{testName} (float2-xy)", $"{testDescription}Uses the xy-components of the float4 version of this test to test this node's float2 operation.", CreateSelfContainedTestGraph(nodeName, In(new float2(a.x, a.y)), Out(expected.xy), subGraphType));
            QueueTest(nodeName, $"{fileName}-float2-zw", $"{testName} (float2-zw)", $"{testDescription}Uses the zw-components of the float4 version of this test to test this node's float2 operation.", CreateSelfContainedTestGraph(nodeName, In(new float2(a.z, a.w)), Out(expected.zw), subGraphType));

            QueueTest(nodeName, $"{fileName}-float3-xyz", $"{testName} (float3-xyz)", $"{testDescription}Uses the xyz-components of the float4 version of this test to test this node's float3 operation.", CreateSelfContainedTestGraph(nodeName, In(new float3(a.x, a.y, a.z)), Out(expected.xyz), subGraphType));
            QueueTest(nodeName, $"{fileName}-float3-yzw", $"{testName} (float3-yzw)", $"{testDescription}Uses the yzw-components of the float4 version of this test to test this node's float3 operation.", CreateSelfContainedTestGraph(nodeName, In(new float3(a.y, a.z, a.w)), Out(expected.yzw), subGraphType));

            QueueTest(nodeName, $"{fileName}-float4", $"{testName} (float4)", $"{testDescription}Tests this node's float4 operation.", CreateSelfContainedTestGraph(nodeName, In(a), Out(expected), subGraphType));
        }

        protected static void TestNodeWithAllFloatNInputVariants(string fileName, string testName, string testDescription, string nodeName, float4 a, float4 b, float4 expected, ComparisonType subGraphType = ComparisonType.Approximately)
        {
            QueueTest(nodeName, $"{fileName}-float-x", $"{testName} (float-x)", $"{testDescription}Uses the x-components of the float4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.x, b.x), Out(expected.x), subGraphType));
            QueueTest(nodeName, $"{fileName}-float-y", $"{testName} (float-y)", $"{testDescription}Uses the y-components of the float4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.y, b.y), Out(expected.y), subGraphType));
            QueueTest(nodeName, $"{fileName}-float-z", $"{testName} (float-z)", $"{testDescription}Uses the z-components of the float4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.z, b.z), Out(expected.z), subGraphType));
            QueueTest(nodeName, $"{fileName}-float-w", $"{testName} (float-w)", $"{testDescription}Uses the w-components of the float4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.w, b.w), Out(expected.w), subGraphType));

            QueueTest(nodeName, $"{fileName}-float2-xy", $"{testName} (float2-xy)", $"{testDescription}Uses the xy-components of the float4 version of this test to test this node's float2 operation.", CreateSelfContainedTestGraph(nodeName, In(new float2(a.x, a.y), new float2(b.x, b.y)), Out(expected.xy), subGraphType));
            QueueTest(nodeName, $"{fileName}-float2-zw", $"{testName} (float2-zw)", $"{testDescription}Uses the zw-components of the float4 version of this test to test this node's float2 operation.", CreateSelfContainedTestGraph(nodeName, In(new float2(a.z, a.w), new float2(b.z, b.w)), Out(expected.zw), subGraphType));

            QueueTest(nodeName, $"{fileName}-float3-xyz", $"{testName} (float3-xyz)", $"{testDescription}Uses the xyz-components of the float4 version of this test to test this node's float3 operation.", CreateSelfContainedTestGraph(nodeName, In(new float3(a.x, a.y, a.z), new float3(b.x, b.y, b.z)), Out(expected.xyz), subGraphType));
            QueueTest(nodeName, $"{fileName}-float3-yzw", $"{testName} (float3-yzw)", $"{testDescription}Uses the yzw-components of the float4 version of this test to test this node's float3 operation.", CreateSelfContainedTestGraph(nodeName, In(new float3(a.y, a.z, a.w), new float3(b.y, b.z, b.w)), Out(expected.yzw), subGraphType));

            QueueTest(nodeName, $"{fileName}-float4", $"{testName} (float4)", $"{testDescription}Tests this node's float4 operation.", CreateSelfContainedTestGraph(nodeName, In(a, b), Out(expected), subGraphType));
        }


        protected static void TestNodeWithAllFloatNInputVariants(string fileName, string testName, string testDescription, string nodeName, float4 a, float4 b, float4 c, float4 expected, ComparisonType subGraphType = ComparisonType.Approximately)
        {
            QueueTest(nodeName, $"{fileName}-float-x", $"{testName} (float-x)", $"{testDescription}Uses the x-components of the float4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.x, b.x, c.x), Out(expected.x), subGraphType));
            QueueTest(nodeName, $"{fileName}-float-y", $"{testName} (float-y)", $"{testDescription}Uses the y-components of the float4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.y, b.y, c.y), Out(expected.y), subGraphType));
            QueueTest(nodeName, $"{fileName}-float-z", $"{testName} (float-z)", $"{testDescription}Uses the z-components of the float4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.z, b.z, c.z), Out(expected.z), subGraphType));
            QueueTest(nodeName, $"{fileName}-float-w", $"{testName} (float-w)", $"{testDescription}Uses the w-components of the float4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.w, b.w, c.w), Out(expected.w), subGraphType));

            QueueTest(nodeName, $"{fileName}-float2-xy", $"{testName} (float2-xy)", $"{testDescription}Uses the xy-components of the float4 version of this test to test this node's float2 operation.", CreateSelfContainedTestGraph(nodeName, In(new float2(a.x, a.y), new float2(b.x, b.y), new float2(c.x, c.y)), Out(expected.xy), subGraphType));
            QueueTest(nodeName, $"{fileName}-float2-zw", $"{testName} (float2-zw)", $"{testDescription}Uses the zw-components of the float4 version of this test to test this node's float2 operation.", CreateSelfContainedTestGraph(nodeName, In(new float2(a.z, a.w), new float2(b.z, b.w), new float2(c.z, c.w)), Out(expected.zw), subGraphType));

            QueueTest(nodeName, $"{fileName}-float3-xyz", $"{testName} (float3-xyz)", $"{testDescription}Uses the xyz-components of the float4 version of this test to test this node's float3 operation.", CreateSelfContainedTestGraph(nodeName, In(new float3(a.x, a.y, a.z), new float3(b.x, b.y, b.z), new float3(c.x, c.y, c.z)), Out(expected.xyz), subGraphType));
            QueueTest(nodeName, $"{fileName}-float3-yzw", $"{testName} (float3-yzw)", $"{testDescription}Uses the yzw-components of the float4 version of this test to test this node's float3 operation.", CreateSelfContainedTestGraph(nodeName, In(new float3(a.y, a.z, a.w), new float3(b.y, b.z, b.w), new float3(c.y, c.z, c.w)), Out(expected.yzw), subGraphType));

            QueueTest(nodeName, $"{fileName}-float4", $"{testName} (float4)", $"{testDescription}Tests this node's float4 operation.", CreateSelfContainedTestGraph(nodeName, In(a, b, c), Out(expected), subGraphType));
        }

        protected static void TestNodeWithAllFloatNxNInputVariants(string fileName, string testName, string testDescription, string nodeName, float4x4 a, float4x4 expected, ComparisonType subGraphType = ComparisonType.Approximately)
        {
            QueueTest(nodeName, $"{fileName}-float-x", $"{testName} (float-x)", $"{testDescription} Uses the x-component of the first column of the float4x4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.c0.x), Out(expected.c0.x), subGraphType));
            QueueTest(nodeName, $"{fileName}-float-y", $"{testName} (float-y)", $"{testDescription}Uses the y-component of the first column of the float4x4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.c0.y), Out(expected.c0.y), subGraphType));
            QueueTest(nodeName, $"{fileName}-float-z", $"{testName} (float-z)", $"{testDescription}Uses the z-component of the first column of the float4x4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.c0.z), Out(expected.c0.z), subGraphType));
            QueueTest(nodeName, $"{fileName}-float-w", $"{testName} (float-w)", $"{testDescription}Uses the w-component of the first column of the float4x4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.c0.w), Out(expected.c0.w), subGraphType));

            QueueTest(nodeName, $"{fileName}-float2-xy", $"{testName} (float2-xy)", $"{testDescription}Uses the xy-components of the first column of the float4x4 version of this test to test this node's float2 operation.", CreateSelfContainedTestGraph(nodeName, In(new float2(a.c0.x, a.c0.y)), Out(expected.c0.xy), subGraphType));
            QueueTest(nodeName, $"{fileName}-float2-zw", $"{testName} (float2-zw)", $"{testDescription}Uses the zw-components of the first column of the float4x4 version of this test to test this node's float2 operation.", CreateSelfContainedTestGraph(nodeName, In(new float2(a.c0.z, a.c0.w)), Out(expected.c0.zw), subGraphType));

            QueueTest(nodeName, $"{fileName}-float3-xyz", $"{testName} (float3-xyz)", $"{testDescription}Uses the xyz-components of the first column of the float4x4 version of this test to test this node's float3 operation.", CreateSelfContainedTestGraph(nodeName, In(new float3(a.c0.x, a.c0.y, a.c0.z)), Out(expected.c0.xyz), subGraphType));
            QueueTest(nodeName, $"{fileName}-float3-yzw", $"{testName} (float3-yzw)", $"{testDescription}Uses the yzw-components of the first column of the float4x4 version of this test to test this node's float3 operation.", CreateSelfContainedTestGraph(nodeName, In(new float3(a.c0.y, a.c0.z, a.c0.w)), Out(expected.c0.yzw), subGraphType));

            QueueTest(nodeName, $"{fileName}-float4", $"{testName} (float4)", $"{testDescription}Uses the first column of the float4x4 version of this test to test tis node's float4 operation.", CreateSelfContainedTestGraph(nodeName, In(a.c0), Out(expected.c0), subGraphType));

            QueueTest(nodeName, $"{fileName}-float2x2", $"{testName} (float2x2)", $"{testDescription}Creates a 2x2 matrix from the upper left of the float4x4 matrix version of this test.", CreateSelfContainedTestGraph(nodeName, In(new float2x2(a.c0.xy, a.c1.xy)), Out(new float2x2(expected.c0.xy, expected.c1.xy)), subGraphType));
            QueueTest(nodeName, $"{fileName}-float3x3", $"{testName} (float3x3)", $"{testDescription}Creates a 3x3 matrix from the upper left of the float4x4 matrix version of this test.", CreateSelfContainedTestGraph(nodeName, In(new float3x3(a.c0.xyz, a.c1.xyz, a.c2.xyz)), Out(new float3x3(expected.c0.xyz, expected.c1.xyz, expected.c2.xyz)), subGraphType));
            QueueTest(nodeName, $"{fileName}-float4x4", $"{testName} (float4x4)", $"{testDescription}Float4x4 version of this test.", CreateSelfContainedTestGraph(nodeName, In(a), Out(expected), subGraphType));
        }

        protected static void TestNodeWithAllFloatNxNInputVariants(string fileName, string testName, string testDescription, string nodeName, float4x4 a, float4x4 b, float4x4 expected, ComparisonType subGraphType = ComparisonType.Approximately)
        {
            QueueTest(nodeName, $"{fileName}-float-x", $"{testName} (float-x)", $"{testDescription}Uses the x-components of the first columns of the float4x4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.c0.x, b.c0.x), Out(expected.c0.x), subGraphType));
            QueueTest(nodeName, $"{fileName}-float-y", $"{testName} (float-y)", $"{testDescription}Uses the y-components of the first columns of the float4x4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.c0.y, b.c0.y), Out(expected.c0.y), subGraphType));
            QueueTest(nodeName, $"{fileName}-float-z", $"{testName} (float-z)", $"{testDescription}Uses the z-components of the first columns of the float4x4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.c0.z, b.c0.z), Out(expected.c0.z), subGraphType));
            QueueTest(nodeName, $"{fileName}-float-w", $"{testName} (float-w)", $"{testDescription}Uses the w-components of the first columns of the float4x4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.c0.w, b.c0.w), Out(expected.c0.w), subGraphType));

            QueueTest(nodeName, $"{fileName}-float2-xy", $"{testName} (float2-xy)", $"{testDescription}Uses the xy-components of the first columns of the float4x4 version of this test to test this node's float2 operation.", CreateSelfContainedTestGraph(nodeName, In(new float2(a.c0.x, a.c0.y), new float2(b.c0.x, b.c0.y)), Out(expected.c0.xy), subGraphType));
            QueueTest(nodeName, $"{fileName}-float2-zw", $"{testName} (float2-zw)", $"{testDescription}Uses the zw-components of the first columns of the float4x4 version of this test to test this node's float2 operation.", CreateSelfContainedTestGraph(nodeName, In(new float2(a.c0.z, a.c0.w), new float2(b.c0.z, b.c0.w)), Out(expected.c0.zw), subGraphType));

            QueueTest(nodeName, $"{fileName}-float3-xyz", $"{testName} (float3-xyz)", $"{testDescription}Uses the xyz-components of the first columns of the float4x4 version of this test to test this node's float3 operation.", CreateSelfContainedTestGraph(nodeName, In(new float3(a.c0.x, a.c0.y, a.c0.z), new float3(b.c0.x, b.c0.y, b.c0.z)), Out(expected.c0.xyz), subGraphType));
            QueueTest(nodeName, $"{fileName}-float3-yzw", $"{testName} (float3-yzw)", $"{testDescription}Uses the yzw-components of the first columns of the float4x4 version of this test to test this node's float3 operation.", CreateSelfContainedTestGraph(nodeName, In(new float3(a.c0.y, a.c0.z, a.c0.w), new float3(b.c0.y, b.c0.z, b.c0.w)), Out(expected.c0.yzw), subGraphType));

            QueueTest(nodeName, $"{fileName}-float4", $"{testName} (float4)", $"{testDescription}Uses the first columns of the float4x4 version of this test to test this node's float4 operation.", CreateSelfContainedTestGraph(nodeName, In(a.c0, b.c0), Out(expected.c0), subGraphType));

            QueueTest(nodeName, $"{fileName}-float2x2", $"{testName} (float2x2)", $"{testDescription}Creates a 2x2 matrix from the upper left of the float4x4 matrices.", CreateSelfContainedTestGraph(nodeName, In(new float2x2(a.c0.xy, a.c1.xy), new float2x2(b.c0.xy, b.c1.xy)), Out(new float2x2(expected.c0.xy, expected.c1.xy)), subGraphType));
            QueueTest(nodeName, $"{fileName}-float3x3", $"{testName} (float3x3)", $"{testDescription}Creates a 3x3 matrix from the upper left of the float4x4 matrices.", CreateSelfContainedTestGraph(nodeName, In(new float3x3(a.c0.xyz, a.c1.xyz, a.c2.xyz), new float3x3(b.c0.xyz, b.c1.xyz, b.c2.xyz)), Out(new float3x3(expected.c0.xyz, expected.c1.xyz, expected.c2.xyz)), subGraphType));
            QueueTest(nodeName, $"{fileName}-float4x4", $"{testName} (float4x4)", $"{testDescription}Float4x4 version of this test.", CreateSelfContainedTestGraph(nodeName, In(a, b), Out(expected), subGraphType));
        }

        protected static void TestNodeWithAllFloatNxNInputVariants(string fileName, string testName, string testDescription, string nodeName, float4x4 a, float4x4 b, float4x4 c, float4x4 expected, ComparisonType subGraphType = ComparisonType.Approximately)
        {
            QueueTest(nodeName, $"{fileName}-float-x", $"{testName} (float-x)", $"{testDescription}Uses the x-components of the first columns of the float4x4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.c0.x, b.c0.x, c.c0.x), Out(expected.c0.x), subGraphType));
            QueueTest(nodeName, $"{fileName}-float-y", $"{testName} (float-y)", $"{testDescription}Uses the y-components of the first columns of the float4x4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.c0.y, b.c0.y, c.c0.y), Out(expected.c0.y), subGraphType));
            QueueTest(nodeName, $"{fileName}-float-z", $"{testName} (float-z)", $"{testDescription}Uses the z-components of the first columns of the float4x4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.c0.z, b.c0.z, c.c0.z), Out(expected.c0.z), subGraphType));
            QueueTest(nodeName, $"{fileName}-float-w", $"{testName} (float-w)", $"{testDescription}Uses the w-components of the first columns of the float4x4 version of this test to test this node's float operation.", CreateSelfContainedTestGraph(nodeName, In(a.c0.w, b.c0.w, c.c0.w), Out(expected.c0.w), subGraphType));

            QueueTest(nodeName, $"{fileName}-float2-xy", $"{testName} (float2-xy)", $"{testDescription}Uses the xy-components of the first columns of the float4x4 version of this test to test this node's float2 operation.", CreateSelfContainedTestGraph(nodeName, In(new float2(a.c0.x, a.c0.y), new float2(b.c0.x, b.c0.y), new float2(c.c0.x, c.c0.y)), Out(expected.c0.xy), subGraphType));
            QueueTest(nodeName, $"{fileName}-float2-zw", $"{testName} (float2-zw)", $"{testDescription}Uses the zw-components of the first columns of the float4x4 version of this test to test this node's float2 operation.", CreateSelfContainedTestGraph(nodeName, In(new float2(a.c0.z, a.c0.w), new float2(b.c0.z, b.c0.w), new float2(c.c0.z, c.c0.w)), Out(expected.c0.zw), subGraphType));

            QueueTest(nodeName, $"{fileName}-float3-xyz", $"{testName} (float3-xyz)", $"{testDescription}Uses the xyz-components of the first columns of the float4x4 version of this test to test this node's float3 operation.", CreateSelfContainedTestGraph(nodeName, In(new float3(a.c0.x, a.c0.y, a.c0.z), new float3(b.c0.x, b.c0.y, b.c0.z), new float3(c.c0.x, c.c0.y, c.c0.z)), Out(expected.c0.xyz), subGraphType));
            QueueTest(nodeName, $"{fileName}-float3-yzw", $"{testName} (float3-yzw)", $"{testDescription}Uses the yzw-components of the first columns of the float4x4 version of this test to test this node's float3 operation.", CreateSelfContainedTestGraph(nodeName, In(new float3(a.c0.y, a.c0.z, a.c0.w), new float3(b.c0.y, b.c0.z, b.c0.w), new float3(c.c0.y, c.c0.z, c.c0.w)), Out(expected.c0.yzw), subGraphType));

            QueueTest(nodeName, $"{fileName}-float4", $"{testName} (float4)", $"{testDescription}Uses the first columns of the float4x4 version of this test to test this node's float4 operation.", CreateSelfContainedTestGraph(nodeName, In(a.c0, b.c0, c.c0), Out(expected.c0), subGraphType));

            QueueTest(nodeName, $"{fileName}-float2x2", $"{testName} (float2x2)", $"{testDescription}Creates a 2x2 matrix from the upper left of the float4x4 matrices.", CreateSelfContainedTestGraph(nodeName, In(new float2x2(a.c0.xy, a.c1.xy), new float2x2(b.c0.xy, b.c1.xy), new float2x2(c.c0.xy, c.c1.xy)), Out(new float2x2(expected.c0.xy, expected.c1.xy)), subGraphType));
            QueueTest(nodeName, $"{fileName}-float3x3", $"{testName} (float3x3)", $"{testDescription}Creates a 3x3 matrix from the upper left of the float4x4 matrices.", CreateSelfContainedTestGraph(nodeName, In(new float3x3(a.c0.xyz, a.c1.xyz, a.c2.xyz), new float3x3(b.c0.xyz, b.c1.xyz, b.c2.xyz), new float3x3(c.c0.xyz, c.c1.xyz, c.c2.xyz)), Out(new float3x3(expected.c0.xyz, expected.c1.xyz, expected.c2.xyz)), subGraphType));
            QueueTest(nodeName, $"{fileName}-float4x4", $"{testName} (float4x4)", $"{testDescription}Float4x4 version of this test.", CreateSelfContainedTestGraph(nodeName, In(a, b, c), Out(expected), subGraphType));
        }


        protected static (Graph, TestValues) CreateSelfContainedTestGraph(string nodeStr, Dictionary<string, Value> values, Dictionary<string, IProperty> expectedResults, ComparisonType subGraphType)
        {
            Graph g = CreateGraphForTest();

            var opNode = g.CreateNode(nodeStr);
            var subGraph = subGraphs[(int)subGraphType];

            foreach (var value in values)
            {
                opNode.AddValue(value.Key, value.Value);
            }

            GenerateGraphByExpectedValueType(expectedResults, g, opNode, subGraph);

            var testValues = new TestValues()
            {
                values = new(values),
                expectedResults = new(expectedResults),
            };

            return (g, testValues);
        }

        private static void GenerateGraphByExpectedValueType(Dictionary<string, IProperty> expectedResults, Graph g, Node opNode, ISubGraph requestedSubGraph)
        {
            Value value;
            Node node;

            var onStart = g.CreateNode("event/onStart");
            var pass = g.CreateNode("event/send");
            pass.AddConfiguration(ConstStrings.EVENT, COMPLETED_EVENT_INDEX);

            Node firstBranch = null;
            Node lastBranch = null;
            Node iteratorStartBranch = null;
            Node iteratorEndBranch = null;

            var count = 0;

            ISubGraph subGraph;

            foreach (var expected in expectedResults)
            {
                subGraph = expected.Value switch
                {
                    Property<int> or Property<bool> => subGraphs[(int)ComparisonType.Equals],
                    _ => requestedSubGraph,
                };

                if (subGraph is EqualSubGraph)
                {
                    iteratorStartBranch = CreateSingleValueTestSubGraph(g, opNode, expected.Key, expected.Value, subGraph);
                    iteratorEndBranch = iteratorStartBranch;
                }
                else
                {
                    switch (expected.Value)
                    {
                        default:
                            iteratorStartBranch = CreateSingleValueTestSubGraph(g, opNode, expected.Key, expected.Value, subGraph);
                            iteratorEndBranch = iteratorStartBranch;
                            break;
                        case Property<float2>:
                            node = CreateExtractNode(g, "math/extract2", opNode, out value, out node, expected);
                            var f2Val = ((Property<float2>)expected.Value).value;

                            iteratorStartBranch = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(0), f2Val[0], subGraph);
                            iteratorEndBranch = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(1), f2Val[1], subGraph);

                            iteratorStartBranch.AddFlow(iteratorEndBranch, ConstStrings.TRUE);

                            break;
                        case Property<float3>:
                            node = CreateExtractNode(g, "math/extract3", opNode, out value, out node, expected);
                            var f3Val = ((Property<float3>)expected.Value).value;

                            iteratorStartBranch = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(0), f3Val[0], subGraph);
                            var b1 = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(1), f3Val[1], subGraph);
                            iteratorEndBranch = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(2), f3Val[2], subGraph);

                            iteratorStartBranch.AddFlow(b1, ConstStrings.TRUE);
                            b1.AddFlow(iteratorEndBranch, ConstStrings.TRUE);

                            break;
                        case Property<float4>:
                            node = CreateExtractNode(g, "math/extract4", opNode, out value, out node, expected);
                            var f4Val = ((Property<float4>)expected.Value).value;
                            iteratorStartBranch = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(0), f4Val[0], subGraph);
                            var f4b1 = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(1), f4Val[1], subGraph);
                            var f4b2 = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(2), f4Val[2], subGraph);

                            iteratorEndBranch = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(3), f4Val[3], subGraph);

                            iteratorStartBranch.AddFlow(f4b1, ConstStrings.TRUE);
                            f4b1.AddFlow(f4b2, ConstStrings.TRUE);
                            f4b2.AddFlow(iteratorEndBranch, ConstStrings.TRUE);

                            break;
                        case Property<float2x2>:
                            node = CreateExtractNode(g, "math/extract2x2", opNode, out value, out node, expected);
                            var f2x2Val = ((Property<float2x2>)expected.Value).value;

                            var branches = new Node[4];

                            branches[0] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(0), f2x2Val.c0.x, subGraph);
                            branches[1] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(1), f2x2Val.c0.y, subGraph);
                            branches[2] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(2), f2x2Val.c1.x, subGraph);
                            branches[3] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(3), f2x2Val.c1.y, subGraph);

                            branches[0].AddFlow(branches[1], ConstStrings.TRUE);
                            branches[1].AddFlow(branches[2], ConstStrings.TRUE);
                            branches[2].AddFlow(branches[3], ConstStrings.TRUE);

                            iteratorStartBranch = branches[0];
                            iteratorEndBranch = branches[3];
                            break;

                        case Property<float3x3>:
                            node = CreateExtractNode(g, "math/extract3x3", opNode, out value, out node, expected);
                            var f3x3Val = ((Property<float3x3>)expected.Value).value;

                            var f3x3branches = new Node[9];

                            f3x3branches[0] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(0), f3x3Val.c0.x, subGraph);
                            f3x3branches[1] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(1), f3x3Val.c0.y, subGraph);
                            f3x3branches[2] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(2), f3x3Val.c0.z, subGraph);

                            f3x3branches[3] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(3), f3x3Val.c1.x, subGraph);
                            f3x3branches[4] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(4), f3x3Val.c1.y, subGraph);
                            f3x3branches[5] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(5), f3x3Val.c1.z, subGraph);

                            f3x3branches[6] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(6), f3x3Val.c2.x, subGraph);
                            f3x3branches[7] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(7), f3x3Val.c2.y, subGraph);
                            f3x3branches[8] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(8), f3x3Val.c2.z, subGraph);

                            for (int i = 0; i < f3x3branches.Length - 1; i++)
                            {
                                f3x3branches[i].AddFlow(f3x3branches[i + 1], ConstStrings.TRUE);
                            }

                            iteratorStartBranch = f3x3branches[0];
                            iteratorEndBranch = f3x3branches[8];
                            break;


                        case Property<float4x4>:
                            node = CreateExtractNode(g, "math/extract4x4", opNode, out value, out node, expected);
                            var f4x4Val = ((Property<float4x4>)expected.Value).value;

                            var f4x4branches = new Node[16];

                            f4x4branches[0] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(0), f4x4Val.c0.x, subGraph);
                            f4x4branches[1] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(1), f4x4Val.c0.y, subGraph);
                            f4x4branches[2] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(2), f4x4Val.c0.z, subGraph);
                            f4x4branches[3] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(3), f4x4Val.c0.w, subGraph);

                            f4x4branches[4] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(4), f4x4Val.c1.x, subGraph);
                            f4x4branches[5] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(5), f4x4Val.c1.y, subGraph);
                            f4x4branches[6] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(6), f4x4Val.c1.z, subGraph);
                            f4x4branches[7] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(7), f4x4Val.c1.w, subGraph);

                            f4x4branches[8] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(8), f4x4Val.c2.x, subGraph);
                            f4x4branches[9] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(9), f4x4Val.c2.y, subGraph);
                            f4x4branches[10] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(10), f4x4Val.c2.z, subGraph);
                            f4x4branches[11] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(11), f4x4Val.c2.w, subGraph);

                            f4x4branches[12] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(12), f4x4Val.c3.x, subGraph);
                            f4x4branches[13] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(13), f4x4Val.c3.y, subGraph);
                            f4x4branches[14] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(14), f4x4Val.c3.z, subGraph);
                            f4x4branches[15] = CreateSingleValueTestSubGraph(g, node, ConstStrings.GetNumberString(15), f4x4Val.c3.w, subGraph);

                            for (int i = 0; i < f4x4branches.Length - 1; i++)
                            {
                                f4x4branches[i].AddFlow(f4x4branches[i + 1], ConstStrings.TRUE);
                            }

                            iteratorStartBranch = f4x4branches[0];
                            iteratorEndBranch = f4x4branches[15];
                            break;
                    }
                }

                if (count == 0)
                {
                    firstBranch = iteratorStartBranch;
                }
                else
                {
                    lastBranch.AddFlow(iteratorStartBranch, ConstStrings.TRUE);
                }
                lastBranch = iteratorEndBranch;
                count++;
            }

            onStart.AddFlow(firstBranch);
            lastBranch.AddFlow(pass, ConstStrings.TRUE);

            static Node CreateExtractNode(Graph g, string nodeName, Node opNode, out Value value, out Node node, KeyValuePair<string, IProperty> expected)
            {
                node = g.CreateNode(nodeName);
                value = node.AddValue(ConstStrings.A, 0);
                value.TryConnectToSocket(opNode, expected.Key);

                return node;
            }
        }

        private static Node CreateSingleValueTestSubGraph<T>(Graph g, Node outNode, string outSocket, T expected, ISubGraph subGraph)
        {
            return CreateSingleValueTestSubGraph(g, outNode, outSocket, (IProperty)new Property<T>(expected), subGraph);
        }

        private static Node CreateSingleValueTestSubGraph(Graph g, Node outNode, string outSocket, IProperty expected, ISubGraph subGraph)
        {
            var fail = g.CreateNode("event/send");
            var failLog = g.CreateNode("debug/log");

            fail.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);

            failLog.AddConfiguration(ConstStrings.MESSAGE, $"Output {outSocket}" + ", Expected: {expected}, Actual: {actual}");
            (var subIn, var subOut) = subGraph.CreateSubGraph(g);

            var subAValue = subIn.AddValue(ConstStrings.A, 0);
            subAValue.TryConnectToSocket(outNode, outSocket);

            if (subGraph.hasBValue) subIn.AddValue(ConstStrings.B, expected);

            subOut.AddFlow(failLog, ConstStrings.FALSE, ConstStrings.IN);

            failLog.AddFlow(fail, ConstStrings.OUT, ConstStrings.IN);
            failLog.AddValue(ConstStrings.EXPECTED, expected);
            var failLogActualValue = failLog.AddValue(ConstStrings.ACTUAL, 0);
            failLogActualValue.TryConnectToSocket(outNode, outSocket);

            return subOut;
        }

        protected static BehaviourEngine CreateBehaviourEngineForGraph(Graph g, Action<int, Dictionary<string, IProperty>> onEventFired, GLTFSceneImporter importer, bool startPlayback)
        {
            // Because we are adding interactivity graphs to non-interactive glbs in the tests for animations/pointers we need to build the pointer resolver manually.
            var pointerResolver = CreatePointerResolver(importer);

            BehaviourEngine eng = new BehaviourEngine(g, pointerResolver);

            // Animation wrapper/pointers need to be manually built as well, usually happens in the InteractivityImporterContext.
            AddAnimationSupportIfApplicable(eng, importer);

            if (onEventFired != null)
                eng.onCustomEventFired += onEventFired;

            if (startPlayback)
                eng.StartPlayback();

            return eng;
        }

        private static PointerResolver CreatePointerResolver(GLTFSceneImporter importer)
        {
            if (importer == null)
                return null;

            var pointerResolver = new PointerResolver();

            pointerResolver.RegisterSceneData(importer.Root);

            var meshes = importer.MeshCache;
            var materials = importer.MaterialCache;
            var nodes = importer.NodeCache;

            for (int i = 0; i < meshes.Length; i++)
            {
                pointerResolver.RegisterMesh(importer.Root.Meshes[i], i, meshes[i].LoadedMesh);
            }

            for (int i = 0; i < materials.Length; i++)
            {
                pointerResolver.RegisterMaterial(importer.Root.Materials[i], i, materials[i].UnityMaterialWithVertexColor);
            }

            var cameraIndex = 0;

            for (int i = 0; i < nodes.Length; i++)
            {
                pointerResolver.RegisterNode(importer.Root.Nodes[i], i, nodes[i]);

                if (nodes[i].TryGetComponent(out Camera camera))
                {
                    pointerResolver.RegisterCamera(importer.Root.Cameras[cameraIndex], cameraIndex, camera);
                    cameraIndex++;
                }
            }

            pointerResolver.CreatePointers();

            return pointerResolver;
        }

        private static void AddAnimationSupportIfApplicable(BehaviourEngine eng, GLTFSceneImporter importer)
        {
            if (importer == null || importer.AnimationCache.IsNullOrEmpty())
                return;

            var animationWrapper = importer.SceneParent.gameObject.AddComponent<GLTFInteractivityAnimationWrapper>();
            eng.SetAnimationWrapper(animationWrapper, importer.LastLoadedScene.GetComponents<Animation>()[0]);
        }

        protected static async Task<GLTFSceneImporter> LoadTestModel(string modelName, Action<GameObject, ExceptionDispatchInfo, GLTFSceneImporter> onLoadComplete = null)
        {
            ImporterFactory _importerFactory = ScriptableObject.CreateInstance<DefaultImporterFactory>();
            ImportOptions _importOptions = new ImportOptions()
            {
                ImportNormals = GLTFImporterNormals.Import,
                ImportTangents = GLTFImporterNormals.Import,
            };

            _importOptions.DataLoader = new ResourcesLoader();

            var importer = _importerFactory.CreateSceneImporter(
                modelName,
                _importOptions
            );

            var sceneParent = new GameObject(modelName).transform;

            importer.SceneParent = sceneParent;
            importer.Collider = GLTFSceneImporter.ColliderType.Box;
            importer.MaximumLod = 300;
            importer.Timeout = 8;
            importer.IsMultithreaded = true;
            importer.CustomShaderName = null;

            // for logging progress
            await importer.LoadSceneAsync(
                showSceneObj: true,
                onLoadComplete: (go, e) => onLoadComplete?.Invoke(go, e, importer)
            );

            return importer;
        }

        protected static Graph CreateGraphForTest()
        {
            var graph = new Graph();
            graph.AddDefaultTypes();
            graph.AddEvent("Failed");
            graph.AddEvent("Completed");
            return graph;
        }

        protected static string GetCallerName([CallerMemberName] string caller = null)
        {
            return caller;
        }

        protected static void QueueTest(string nodeName, string fileName, string testName, string testDescription, Graph graph, GLTFSceneImporter importer)
        {
            _testQueue.Add(new TestData()
            {
                nodeName = nodeName,
                fileName = fileName,
                graph = graph,
                executionTimeoutDuration = DEFAULT_MULTI_FRAME_EXECUTION_TIMEOUT,
                importer = importer,
                title = testName,
                description = testDescription
            });
        }

        protected static void QueueTest(string nodeName, string fileName, string testName, string testDescription, Graph graph)
        {
            _testQueue.Add(new TestData()
            {
                nodeName = nodeName,
                fileName = fileName,
                graph = graph,
                executionTimeoutDuration = DEFAULT_MULTI_FRAME_EXECUTION_TIMEOUT,
                title = testName,
                description = testDescription
            });
        }

        protected static void QueueTest(string nodeName, string fileName, string testName, string testDescription, (Graph graph, TestValues values) graphTuple)
        {
            _testQueue.Add(new TestData()
            {
                nodeName = nodeName,
                fileName = fileName,
                graph = graphTuple.graph,
                executionTimeoutDuration = DEFAULT_MULTI_FRAME_EXECUTION_TIMEOUT,
                title = testName,
                description = testDescription,
                values = graphTuple.values
            });
        }

        protected static Dictionary<string, Value> In<T>(params T[] inputValues)
        {
            Dictionary<string, Value> inputs = new();

            for (int i = 0; i < inputValues.Length; i++)
            {
                inputs.Add(ConstStrings.Letters[i], new Value()
                {
                    id = ConstStrings.Letters[i],
                    property = new Property<T>(inputValues[i])
                });
            }

            return inputs;
        }

        protected static Dictionary<string, IProperty> Out<T>(T expected)
        {
            Dictionary<string, IProperty> outputs = new();

            outputs.Add(ConstStrings.VALUE, new Property<T>(expected));

            return outputs;
        }

        protected static Dictionary<string, Value> In<T>(params KeyValuePair<string, T>[] inputValues)
        {
            Dictionary<string, Value> inputs = new();

            for (int i = 0; i < inputValues.Length; i++)
            {
                inputs.Add(inputValues[i].Key, new Value()
                {
                    id = inputValues[i].Key,
                    property = new Property<T>(inputValues[i].Value)
                });
            }

            return inputs;
        }

        protected static Dictionary<string, IProperty> Out<T>(params KeyValuePair<string, T>[] expectedOutputs)
        {
            Dictionary<string, IProperty> outputs = new();

            for (int i = 0; i < expectedOutputs.Length; i++)
            {
                outputs.Add(expectedOutputs[i].Key, new Property<T>(expectedOutputs[i].Value));
            }

            return outputs;
        }

        protected static Node CreateFailSubGraph(Graph g, string failMessage)
        {
            var fail = g.CreateNode("event/send");
            fail.AddConfiguration(ConstStrings.EVENT, FAIL_EVENT_INDEX);

            var log = g.CreateNode("debug/log");
            log.AddConfiguration(ConstStrings.MESSAGE, failMessage);
            log.AddFlow(fail);

            return log;
        }
    }
}