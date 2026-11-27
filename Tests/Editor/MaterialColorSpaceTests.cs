using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;
using UnityGLTF;
using UnityGLTF.Plugins;
using Object = UnityEngine.Object;

/// <summary>
/// Color space of material color factors on import, export and round trips.
/// glTF stores color factors in linear space (core spec for baseColorFactor, KHR_materials_sheen for sheenColorFactor,
/// KHR_materials_specular for specularColorFactor). Unity treats non-HDR material colors as sRGB and linearizes them
/// before they reach the shader, so the material has to hold the gamma-encoded value: import applies .gamma, export applies .linear.
/// baseColorFactor is the reference that already follows this convention.
/// </summary>
public class MaterialColorSpaceTests
{
	private const float Tolerance = 1e-3f;
	private const string PbrGraph = "UnityGLTF/PBRGraph";

	// A linear glTF value and an sRGB material color that differ clearly from their converted counterparts
	private static readonly Color GltfLinear = new Color(0.5f, 0.25f, 0.1f, 1f);
	private static readonly Color MaterialSRGB = new Color(0.6f, 0.4f, 0.2f, 1f);

	public class ColorProperty
	{
		public string Name;
		/// <summary>Path of the factor in the glTF material, e.g. "extensions/KHR_materials_sheen/sheenColorFactor"</summary>
		public string GltfPath;
		/// <summary>Keyword prefix the importer sets and the exporter checks ("_SHEEN" → "_SHEEN_ON")</summary>
		public string Keyword;
		public string Extension;
		public int Components;
		public override string ToString() => Name;
	}

	private static readonly ColorProperty[] Properties =
	{
		new ColorProperty { Name = "baseColorFactor", GltfPath = "pbrMetallicRoughness/baseColorFactor", Components = 4 },
		new ColorProperty { Name = "sheenColorFactor", GltfPath = "extensions/KHR_materials_sheen/sheenColorFactor", Keyword = "_SHEEN", Extension = "KHR_materials_sheen", Components = 3 },
		new ColorProperty { Name = "specularColorFactor", GltfPath = "extensions/KHR_materials_specular/specularColorFactor", Keyword = "_SPECULAR", Extension = "KHR_materials_specular", Components = 3 },
	};

	private List<Object> objects;

	[SetUp]
	public void SetUp() => objects = new List<Object>();

	[TearDown]
	public void TearDown()
	{
		foreach (var o in objects)
			if (o) Object.DestroyImmediate(o);
	}

	// glTF → Unity: the material holds the gamma-encoded glTF value
	[UnityTest]
	public IEnumerator Import_ColorFactor_IsStoredAsSRGB([ValueSource(nameof(Properties))] ColorProperty property)
	{
		Material material = null;
		yield return Import(CreateGlb(property, GltfLinear), (importer, created) => material = created.GetComponentInChildren<Renderer>().sharedMaterial);

		Assert.IsTrue(material.HasProperty(property.Name), $"{material.shader.name} has no {property.Name}");
		AssertColor(GltfLinear.gamma, material.GetColor(property.Name), 3, "material color");
	}

	// Unity → glTF: the material color is written in linear space
	[Test]
	public void Export_MaterialColor_IsWrittenAsLinear([ValueSource(nameof(Properties))] ColorProperty property)
	{
		var root = CreateQuad(property, MaterialSRGB, out _);
		var (json, _) = Export(root, false);

		AssertColor(MaterialSRGB.linear, ReadFactor(json, property), property.Components, property.GltfPath);
	}

	// glTF → Unity → glTF: the factor comes back unchanged
	[UnityTest]
	public IEnumerator Roundtrip_GltfToUnityToGltf_KeepsFactor([ValueSource(nameof(Properties))] ColorProperty property)
	{
		JObject json = null;
		yield return Import(CreateGlb(property, GltfLinear), (importer, created) => json = Export(created, false).json);

		AssertColor(GltfLinear, ReadFactor(json, property), property.Components, property.GltfPath);
	}

	// Unity → glTF → Unity: the material color comes back unchanged
	[UnityTest]
	public IEnumerator Roundtrip_UnityToGltfToUnity_KeepsColor([ValueSource(nameof(Properties))] ColorProperty property)
	{
		var root = CreateQuad(property, MaterialSRGB, out _);
		var (_, bytes) = Export(root, false);

		Material material = null;
		yield return Import(bytes, (importer, created) => material = created.GetComponentInChildren<Renderer>().sharedMaterial);

		AssertColor(MaterialSRGB, material.GetColor(property.Name), 3, "material color");
	}

	// Unity → glTF with KHR_animation_pointer: every sample is written in linear space, with the component count of the glTF property
	[Test]
	public void Export_AnimatedColor_IsWrittenAsLinear([ValueSource(nameof(Properties))] ColorProperty property)
	{
		var (start, end) = AnimationColors();
		var root = CreateAnimatedQuad(property, start, end);
		var (json, bytes) = Export(root, true);

		var sampler = PointerSampler(json, property);
		Assert.AreEqual(property.Components == 4 ? "VEC4" : "VEC3", (string)json["accessors"][(int)sampler["output"]]["type"], "accessor type");

		var times = GltfFile.ReadAccessor(bytes, json, (int)sampler["input"]);
		var values = GltfFile.ReadAccessor(bytes, json, (int)sampler["output"]);
		for (var i = 0; i < times.Length; i++)
		{
			var t = times[i][0];
			AssertColor(Color.Lerp(start, end, t).linear, values[i], 3, $"sample at {t:F3}s");
		}
	}

	// Unity → glTF → Unity with KHR_animation_pointer: the imported curves animate the same material color
	[UnityTest]
	public IEnumerator Roundtrip_AnimatedColor_KeepsCurves([ValueSource(nameof(Properties))] ColorProperty property)
	{
		var (start, end) = AnimationColors();
		var root = CreateAnimatedQuad(property, start, end);
		var (_, bytes) = Export(root, true);

		AnimationClip[] clips = null;
		yield return Import(bytes, (importer, created) => clips = importer.CreatedAnimationClips);

		Assert.IsNotNull(clips, "no animation imported");
		var bindings = clips.SelectMany(c => AnimationUtility.GetCurveBindings(c).Select(b => (clip: c, binding: b))).ToArray();
		foreach (var (channel, index) in new[] { ("r", 0), ("g", 1), ("b", 2) })
		{
			var propertyName = $"material.{property.Name}.{channel}";
			var match = bindings.FirstOrDefault(b => b.binding.propertyName == propertyName);
			Assert.IsNotNull(match.clip, $"No curve for {propertyName}, found: {string.Join(", ", bindings.Select(b => b.binding.propertyName))}");
			var curve = AnimationUtility.GetEditorCurve(match.clip, match.binding);
			foreach (var t in new[] { 0f, 0.5f, 1f })
				Assert.AreEqual(Color.Lerp(start, end, t)[index], curve.Evaluate(t), 1e-2f, $"{propertyName} at {t}s");
		}
	}

	private static (Color start, Color end) AnimationColors() => (new Color(0.2f, 0.4f, 0.6f, 1f), new Color(0.8f, 0.5f, 0.1f, 1f));

	private static void AssertColor(Color expected, Color actual, int components, string message) =>
		AssertColor(expected, new[] { actual.r, actual.g, actual.b, actual.a }, components, message);

	private static void AssertColor(Color expected, float[] actual, int components, string message)
	{
		Assert.GreaterOrEqual(actual.Length, components, $"{message}: component count");
		var actualColor = new Color(actual[0], actual[1], actual[2], actual.Length > 3 ? actual[3] : 1);
		for (var i = 0; i < components; i++)
			Assert.AreEqual(expected[i], actual[i], Tolerance, $"{message}: expected {expected}, got {actualColor} (component {"rgba"[i]})");
	}

	private static float[] ReadFactor(JObject json, ColorProperty property)
	{
		var material = json["materials"]?.FirstOrDefault();
		Assert.IsNotNull(material, "no material exported");
		var token = property.GltfPath.Split('/').Aggregate((JToken)material, (current, part) => current?[part]);
		Assert.IsNotNull(token, $"{property.GltfPath} not exported: {material}");
		return token.Select(x => (float)x).ToArray();
	}

	private static JToken PointerSampler(JObject json, ColorProperty property)
	{
		var animation = json["animations"]?.Single();
		Assert.IsNotNull(animation, "no animation exported");
		var pointerEnd = "/" + property.GltfPath;
		var channel = animation["channels"].SingleOrDefault(c =>
			((string)c["target"]?["extensions"]?["KHR_animation_pointer"]?["pointer"])?.EndsWith(pointerEnd) == true);
		Assert.IsNotNull(channel, $"No animation pointer ending in {pointerEnd}, found: " +
			string.Join(", ", animation["channels"].Select(c => (string)c["target"]?["extensions"]?["KHR_animation_pointer"]?["pointer"] ?? (string)c["target"]?["path"])));
		return animation["samplers"][(int)channel["sampler"]];
	}

	private static byte[] CreateGlb(ColorProperty property, Color linear)
	{
		var factor = new JArray(linear.r, linear.g, linear.b);
		if (property.Components == 4) factor.Add(linear.a);

		var material = new JObject { ["name"] = "Color" };
		var parts = property.GltfPath.Split('/');
		var parent = material;
		foreach (var part in parts.Take(parts.Length - 1))
		{
			if (!(parent[part] is JObject child)) parent[part] = child = new JObject();
			parent = child;
		}
		parent[parts.Last()] = factor;

		var json = new JObject
		{
			["asset"] = new JObject { ["version"] = "2.0" },
			["scene"] = 0,
			["scenes"] = new JArray(new JObject { ["nodes"] = new JArray(0) }),
			["nodes"] = new JArray(new JObject { ["mesh"] = 0 }),
			["meshes"] = new JArray(new JObject { ["primitives"] = new JArray(new JObject { ["attributes"] = new JObject { ["POSITION"] = 0 }, ["material"] = 0 }) }),
			["materials"] = new JArray(material),
			["accessors"] = new JArray(JObject.Parse("{\"bufferView\":0,\"componentType\":5126,\"count\":3,\"type\":\"VEC3\",\"min\":[0,0,0],\"max\":[1,2,0]}")),
			["bufferViews"] = new JArray(JObject.Parse("{\"buffer\":0,\"byteLength\":36}")),
			["buffers"] = new JArray(JObject.Parse("{\"byteLength\":36}")),
		};
		if (property.Extension != null) json["extensionsUsed"] = new JArray(property.Extension);

		var triangle = new float[] { 0, 0, 0, 1, 0, 0, 0, 2, 0 };
		var bin = new byte[triangle.Length * 4];
		Buffer.BlockCopy(triangle, 0, bin, 0, bin.Length);
		return GlbTestFile.Write(2, GlbTestFile.Json(json.ToString(Newtonsoft.Json.Formatting.None)), GlbTestFile.Bin(bin));
	}

	private GameObject CreateQuad(ColorProperty property, Color color, out GameObject quad)
	{
		var root = new GameObject("Root");
		objects.Add(root);
		quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
		quad.name = "Quad";
		quad.transform.SetParent(root.transform, false);

		var shader = Shader.Find(PbrGraph);
		Assert.IsNotNull(shader, $"Shader {PbrGraph} not found");
		var material = new Material(shader) { name = "Color" };
		objects.Add(material);
		Assert.IsTrue(material.HasProperty(property.Name), $"{PbrGraph} has no {property.Name}");
		material.SetColor(property.Name, color);
		if (property.Keyword != null)
		{
			material.EnableKeyword(property.Keyword + "_ON");
			if (material.HasProperty(property.Keyword)) material.SetFloat(property.Keyword, 1);
		}
		quad.GetComponent<Renderer>().sharedMaterial = material;
		return root;
	}

	private GameObject CreateAnimatedQuad(ColorProperty property, Color start, Color end)
	{
		var root = CreateQuad(property, start, out var quad);
		var clip = new AnimationClip { name = "Color" };
		objects.Add(clip);
		foreach (var (channel, a, b) in new[] { ("r", start.r, end.r), ("g", start.g, end.g), ("b", start.b, end.b), ("a", start.a, end.a) })
			clip.SetCurve(quad.name, typeof(MeshRenderer), $"material.{property.Name}.{channel}", AnimationCurve.Linear(0, a, 1, b));
		var controller = new AnimatorController { name = "Color" };
		objects.Add(controller);
		controller.AddLayer("Base Layer");
		controller.layers[0].stateMachine.AddState(clip.name).motion = clip;
		root.AddComponent<Animator>().runtimeAnimatorController = controller;
		return root;
	}

	private GLTFSettings CreateSettings(bool animationPointer)
	{
		var settings = ScriptableObject.CreateInstance<GLTFSettings>();
		objects.Add(settings);
		objects.AddRange(settings.ImportPlugins);
		objects.AddRange(settings.ExportPlugins);
		settings.UseMainCameraVisibility = false;
		settings.ExportAnimations = true;

		var materialExport = settings.ExportPlugins.OfType<MaterialExtensionsExport>().Single();
		materialExport.Enabled = true;
		materialExport.KHR_materials_sheen = materialExport.KHR_materials_specular = true;
		var materialImport = settings.ImportPlugins.OfType<MaterialExtensionsImport>().Single();
		materialImport.Enabled = true;
		materialImport.KHR_materials_sheen = materialImport.KHR_materials_specular = true;
		settings.ExportPlugins.OfType<AnimationPointerExport>().Single().Enabled = animationPointer;
		settings.ImportPlugins.OfType<AnimationPointerImport>().Single().Enabled = true;
		return settings;
	}

	private (JObject json, byte[] bytes) Export(GameObject root, bool animationPointer)
	{
		var exporter = new GLTFSceneExporter(root.transform, new ExportContext(CreateSettings(animationPointer)));
		var bytes = exporter.SaveGLBToByteArray("Color");
		return (GltfFile.Parse(bytes), bytes);
	}

	private IEnumerator Import(byte[] glb, Action<GLTFSceneImporter, GameObject> onLoaded)
	{
		var options = new ImportOptions
		{
			ImportContext = new GLTFImportContext(null, CreateSettings(false)),
			AnimationMethod = AnimationMethod.Legacy,
		};
		var importer = new GLTFSceneImporter(new MemoryStream(glb), options);
		try
		{
			var task = importer.LoadSceneAsync();
			while (!task.IsCompleted) yield return null;
			Assert.IsNull(task.Exception, task.Exception?.ToString());

			var created = importer.CreatedObject;
			Assert.IsTrue(created, "no object created");
			objects.Add(created);
			objects.AddRange(created.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials));
			if (importer.CreatedAnimationClips != null) objects.AddRange(importer.CreatedAnimationClips);
			onLoaded(importer, created);
		}
		finally
		{
			importer.Dispose();
		}
	}
}
