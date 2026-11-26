using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using UnityGLTF;
using UnityGLTF.Plugins;
using Object = UnityEngine.Object;

/// <summary>
/// Export of emission colors above 1, which are split into emissiveFactor and KHR_materials_emissive_strength.
/// Emission colors are HDR color properties, which Unity passes to shaders without color space conversion,
/// so the exported factor * strength has to be exactly the material's color.
/// </summary>
public class EmissionExportTests
{
	private const float Tolerance = 1e-4f;
	private const string PbrGraph = "UnityGLTF/PBRGraph";
	// The render pipeline's default material (Standard or URP Lit), with an [HDR] _EmissionColor
	private const string DefaultMaterial = "Default";

	[TestCase(PbrGraph, 0.4f, 2f, 3.6f, 0.4f / 3.6f, 2f / 3.6f, 1f, 3.6f)]
	[TestCase(PbrGraph, 0.1f, 0.5f, 0.9f, 0.1f, 0.5f, 0.9f, 1f)]
	[TestCase(PbrGraph, 16f, 8f, 1.6f, 1f, 0.5f, 0.1f, 16f)]
	[TestCase(DefaultMaterial, 0.4f, 2f, 3.6f, 0.4f / 3.6f, 2f / 3.6f, 1f, 3.6f)]
	[TestCase(DefaultMaterial, 0.1f, 0.5f, 0.9f, 0.1f, 0.5f, 0.9f, 1f)]
	public void Export_Emission_SplitsIntoFactorAndStrength(string shader, float r, float g, float b, float factorR, float factorG, float factorB, float strength)
	{
		var objects = new List<Object>();
		try
		{
			var root = CreateQuadWithEmission(shader, new Color(r, g, b, 1), objects, out _);
			var (json, _) = Export(root, objects, false);

			var material = json["materials"].Single(m => (string)m["name"] == "Emission");
			var factor = material["emissiveFactor"].Select(x => (float)x).ToArray();
			Assert.AreEqual(factorR, factor[0], Tolerance, "emissiveFactor r");
			Assert.AreEqual(factorG, factor[1], Tolerance, "emissiveFactor g");
			Assert.AreEqual(factorB, factor[2], Tolerance, "emissiveFactor b");
			var exportedStrength = (float?)material["extensions"]?["KHR_materials_emissive_strength"]?["emissiveStrength"] ?? 1;
			Assert.AreEqual(strength, exportedStrength, Tolerance, "emissiveStrength");
		}
		finally
		{
			foreach (var o in objects) Object.DestroyImmediate(o);
		}
	}

	[Test]
	public void Export_AnimatedEmission_SplitsIntoFactorAndStrength()
	{
		var objects = new List<Object>();
		try
		{
			// From below 1 to above 1, so some baked frames need a strength and some don't
			var start = new Color(0.1f, 0.5f, 0.9f, 1);
			var end = new Color(0.4f, 2f, 3.6f, 1);
			var root = CreateQuadWithEmission(PbrGraph, start, objects, out var quad);

			var clip = new AnimationClip { name = "Glow" };
			objects.Add(clip);
			foreach (var (channel, a, b) in new[] { ("r", start.r, end.r), ("g", start.g, end.g), ("b", start.b, end.b), ("a", 1f, 1f) })
				clip.SetCurve(quad.name, typeof(MeshRenderer), "material.emissiveFactor." + channel, AnimationCurve.Linear(0, a, 1, b));
			var controller = new AnimatorController { name = "Glow" };
			objects.Add(controller);
			controller.AddLayer("Base Layer");
			controller.layers[0].stateMachine.AddState(clip.name).motion = clip;
			root.AddComponent<Animator>().runtimeAnimatorController = controller;

			var (json, bytes) = Export(root, objects, true);

			var animation = json["animations"].Single();
			JObject Channel(string pointerEnd) => (JObject)animation["channels"].SingleOrDefault(c =>
				((string)c["target"]?["extensions"]?["KHR_animation_pointer"]?["pointer"])?.EndsWith(pointerEnd) == true);
			var colorChannel = Channel("/emissiveFactor");
			var strengthChannel = Channel("/emissiveStrength");
			Assert.IsNotNull(colorChannel, "No emissiveFactor animation");
			Assert.IsNotNull(strengthChannel, "No emissiveStrength animation, although the emission goes above 1");

			var colorSampler = animation["samplers"][(int)colorChannel["sampler"]];
			var strengthSampler = animation["samplers"][(int)strengthChannel["sampler"]];
			var times = GltfFile.ReadAccessor(bytes, json, (int)colorSampler["input"]);
			var colors = GltfFile.ReadAccessor(bytes, json, (int)colorSampler["output"]);
			var strengthTimes = GltfFile.ReadAccessor(bytes, json, (int)strengthSampler["input"]);
			var strengths = GltfFile.ReadAccessor(bytes, json, (int)strengthSampler["output"]);
			CollectionAssert.AreEqual(times.Select(t => t[0]), strengthTimes.Select(t => t[0]), "Color and strength are sampled at different times");

			for (var i = 0; i < times.Length; i++)
			{
				var t = times[i][0];
				var expected = Color.Lerp(start, end, t);
				var strength = strengths[i][0];
				Assert.LessOrEqual(colors[i].Take(3).Max(), 1 + Tolerance, $"emissiveFactor above 1 at {t:F3}s");
				Assert.AreEqual(expected.r, colors[i][0] * strength, 1e-3f, $"Emission r at {t:F3}s");
				Assert.AreEqual(expected.g, colors[i][1] * strength, 1e-3f, $"Emission g at {t:F3}s");
				Assert.AreEqual(expected.b, colors[i][2] * strength, 1e-3f, $"Emission b at {t:F3}s");
			}
		}
		finally
		{
			foreach (var o in objects) Object.DestroyImmediate(o);
		}
	}

	private static GameObject CreateQuadWithEmission(string shader, Color emission, List<Object> objects, out GameObject quad)
	{
		var root = new GameObject("Root");
		objects.Add(root);
		quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
		quad.name = "Quad";
		quad.transform.SetParent(root.transform, false);
		var renderer = quad.GetComponent<Renderer>();

		Material material;
		if (shader == DefaultMaterial)
		{
			material = new Material(renderer.sharedMaterial);
			Assert.IsTrue(material.HasProperty("_EmissionColor"), $"{material.shader.name} has no _EmissionColor");
			material.SetColor("_EmissionColor", emission);
			material.EnableKeyword("_EMISSION");
		}
		else
		{
			var found = Shader.Find(shader);
			Assert.IsNotNull(found, $"Shader {shader} not found");
			material = new Material(found);
			material.SetColor("emissiveFactor", emission);
		}
		material.name = "Emission";
		renderer.sharedMaterial = material;
		objects.Add(material);
		return root;
	}

	private static (JObject json, byte[] bytes) Export(GameObject root, List<Object> objects, bool animationPointer)
	{
		var settings = ScriptableObject.CreateInstance<GLTFSettings>();
		objects.Add(settings);
		settings.UseMainCameraVisibility = false;
		settings.ExportAnimations = true;
		if (animationPointer)
		{
			var plugin = ScriptableObject.CreateInstance<AnimationPointerExport>();
			objects.Add(plugin);
			settings.ExportPlugins.Add(plugin);
		}
		Assert.IsTrue(settings.ExportPlugins.OfType<MaterialExtensionsExport>().Any(p => p.Enabled && p.KHR_materials_emissive_strength),
			"KHR_materials_emissive_strength export is not enabled in new GLTFSettings");

		var exporter = new GLTFSceneExporter(root.transform, new ExportContext(settings));
		var bytes = exporter.SaveGLBToByteArray("Emission");
		return (GltfFile.Parse(bytes), bytes);
	}
}
