using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityGLTF;

public class AssetImportTests
{
	private AnimationMethod originalAnimationMethod;
	private static string guid = "941ea53b9d7aafa4886b8bdd2c0f2963";

	[SetUp]
	public void Setup()
	{
		var path = AssetDatabase.GUIDToAssetPath(guid);
		var importer = AssetImporter.GetAtPath(path) as GLTFImporter;

		var so = new SerializedObject(importer);
		originalAnimationMethod = (AnimationMethod) so.FindProperty("_importAnimations").intValue;
	}

	[TearDown]
	public void Teardown()
	{
		SetAnimationMethod(originalAnimationMethod);
	}

	private string SetAnimationMethod(AnimationMethod method)
	{
		var path = AssetDatabase.GUIDToAssetPath(guid);
		var importer = AssetImporter.GetAtPath(path) as GLTFImporter;

		var so = new SerializedObject(importer);
		so.FindProperty("_importAnimations").intValue = (int) method;
		so.ApplyModifiedPropertiesWithoutUndo();

		importer.SaveAndReimport();
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
		return path;
	}


    [UnityTest]
    public IEnumerator ImportOptionLegacyCreatesAnimations()
    {
		var path = SetAnimationMethod(AnimationMethod.Legacy);
	    yield return null;

	    // access result
	    var asset = AssetDatabase.LoadAssetAtPath<Transform>(path);
	    var animator = asset.GetComponent<Animator>();
	    var animation = asset.GetComponent<Animation>();
	    Assert.IsFalse(animator);
	    Assert.IsTrue(animation);
	    var clips = AssetDatabase
		    .LoadAllAssetsAtPath(path)
		    .Where(x => x is AnimationClip)
		    .Where(x => x)
		    .ToList();
	    Assert.AreEqual(2, clips.Count);
    }

    [UnityTest]
    public IEnumerator ImportOptionMecanimCreatesAnimations()
    {
	    var path = SetAnimationMethod(AnimationMethod.Mecanim);
	    yield return null;

	    // access result
	    var asset = AssetDatabase.LoadAssetAtPath<Transform>(path);
	    var animator = asset.GetComponent<Animator>();
	    var animation = asset.GetComponent<Animation>();
	    Assert.IsTrue(animator);
	    Assert.IsFalse(animation);

	    // need to load these directly as we can't persist the animator controller / state machine.
	    var clips = AssetDatabase
		    .LoadAllAssetsAtPath(path)
		    .Where(x => x is AnimationClip)
		    .Where(x => x)
		    .ToList();
	    Assert.AreEqual(2, clips.Count);
	    Assert.IsTrue(clips.TrueForAll(x => !((AnimationClip)x).legacy));
    }

    [UnityTest]
    public IEnumerator ImportOptionNoneDoesNotCreateAnimations()
    {
	    var path = SetAnimationMethod(AnimationMethod.None);
	    yield return null;

	    // access result
	    var asset = AssetDatabase.LoadAssetAtPath<Transform>(path);
	    var animator = asset.GetComponent<Animator>();
	    var animation = asset.GetComponent<Animation>();
	    Assert.IsFalse(animator);
	    Assert.IsFalse(animation);
	    var clips = AssetDatabase
		    .LoadAllAssetsAtPath(path)
		    .Where(x => x is AnimationClip)
		    .Where(x => x)
		    .ToList();
	    Assert.AreEqual(0, clips.Count);
    }
}
