Notes on Standard (Specular setup).mat and Standard (Specular setup).shadervariants
-----------------------------------------------------------------------------------

The Unity Standard and Standard (Specular setup) shaders have some extra requirements if you want to modify Materials at runtime, because - behind the scenes - it is actually many different shaders rolled into one. If you use scripting to change a Material that would cause it to use a different variant of the Standard Shader, you must enable that variant by using the Material.EnableKeyword function. 

When you use Material.EnableKeyword at runtime, the editor will compile the respective shader variant on the fly, so you will see the correct shading displayed.
When you build a standalone player however, Unity will only compile the shaders that are referenced by objects in your scene. So in case you don't already have a material either in your Scene or in the Resources folder that uses a particular shader variant, your code will call for a shader that hasn't been compiled and you'll see incorrect shading.

GLTFSceneImporter.cs makes heavy use of Material.EnableKeyword, since based on the data in the GLTF file, different features like normal maps, emmission, or alpha modes must be turned on or off in the shader at runtime. The nature of building an importer that can load *any* GLTF file that is unknown at compile-time means that we cannot anticipate which shader features will be used, and therefore must enable the variants manually.

This can be done in one of 2 ways:
1. Through the use of the ShaderVariantCollection (and add the keywords you need)
2. By adding a material to the Assets/Resources folder (and add textures to the texture fields, change Colors to be anything other than black, and change floats to anything other than 0 -you don't need to use the material on anything because as long as it's in the Resources folder Unity will presume it's going to be needed)

Both methods will cause the compiler to deliver the necessary shader variants for the standalone player.


Links:
------
https://docs.unity3d.com/Manual/MaterialsAccessingViaScript.html
https://docs.unity3d.com/ScriptReference/ShaderVariantCollection.html
https://docs.unity3d.com/ScriptReference/Material.EnableKeyword.html
http://answers.unity3d.com/questions/970290/emission-at-run-time-works-in-editor-but-not-in-bu.html