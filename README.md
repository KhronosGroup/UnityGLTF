# UnityGLTF

Unity3D library for importing and exporting [GLTF 2.0](https://github.com/KhronosGroup/glTF/) assets.

The goal of this library is to support the full glTF 2.0 specification and enable the following scenarios:
- Run-time import
- Run-time export
- Design-time import
- Design-time export
- Export of custom extension data

The library allows to be extended with additional capabilities in Unity or support additional extensions to the glTF specification.  
The library was originally designed to work with Unity 5.6 and above, but is currently tested with Unity 2018.4 and above.

## Installation

You can install this package from git, compatible with UPM (Unity Package Manager).
1. Open `Window > Package Manager`
2. In Package Manager, click <kbd>+</kbd> and select <kbd>Add Package from git URL</kbd>
3. Paste ```https://github.com/prefrontalcortex/UnityGLTF.git?path=/UnityGLTF/Assets/UnityGLTF```
4. Click <kbd>Add</kbd>.  
   
Done! 

If you want to target a specific version, append `#release/<some-tag>` or another specific tag from the Release section.

UnityGLTF is now available in the Packages/ folder. You can import a number of samples:
1. Open `Window > Package Manager`
2. Select `UnityGLTF`
3. Select `Samples` and import the desired ones.

## Current Status

UnityGLTF hasn't received official support since early 2020. However, a number of forks have fixed issues and improved several key areas, especially animation support,export workflows, color spaces and extendibility. These forks have now been merged back into main so that everyone can benefit from then, and to enable further work.  

A separate glTF implementation for Unity, [glTFast](https://github.com/atteneder/glTFast), is on its path towards becoming feature complete, with import already being complete. It leverages modern Unity features such as Burst and Jobs, has better compression support (importing compressed textures and meshes), and also has wider Render Pipeline support, notably supporting URP and HDRP import (and partial export).  

glTFast and UnityGLTF can coexist in the same project; you can for example use glTFast for import and UnityGLTF for export, where each of these shine.  

> **TL;DR:**
> - UnityGLTF has very good export support (runtime, editor, animations).
> - glTFast has better general import support (more extensions supported, wider SRP support).
> - If you're playing with custom extensions, UnityGLTF might still be the right choice for import.  

## Supported Features
The lists below are non-conclusive and in no particular order. Note that there are gaps where features could easily be supported for im- and export but currently aren't. PRs welcome!

### Import and Export

- Animations, Skinned Mesh Renderers, Blend Shapes
- Linear and Gamma colorspace support
- Vertex Colors
- KHR_materials_unlit
- KHR_texture_transform (limitation: not full flexibility for transforms per texture)
- Lights and including KHR_lights_punctual
- KHR_materials_pbrSpecularGlossiness

### Export only

- Cameras
- Sparse accessors for Blend Shapes
- Multiple animator clips and speeds
- Timeline recorder track for exporting animations at runtime
- Lossless keyframe optimization on export (animation is baked but redundant keyframes are removed)
- All 2D textures can be exported, no matter if readable or not, RenderTextures included
- KHR_materials_emissive_strength
- URP materials
- (partial) HDRP materials
- glTFast materials

### Import only

- MSFT_LODExtension

### As extension customization sample, export only
 These extensions can be configured for export, but don't have a visual representation right now.

- KHR_materials_transmission
- KHR_materials_ior
- KHR_materials_volume
- KHR_materials_clearcoat
- KHR_materials_sheen

## Known Issues

Each known issue can be reproduced from a specific [glTF Sample Model](https://github.com/KhronosGroup/glTF-Sample-Models/tree/master/2.0):

- khronos-SimpleSparseAccessor: sparse accessors not supported on import (can be exported though)  
- khronos-TriangleWithoutIndices: meshes without indices import with wrong winding order  
- khronos-MultiUVTest: UV per texture is imported but not supported in the GLTF-Builtin shader  
- khronos-MorphPrimitivesTest: isn't correctly importing at runtime (in some cases?)  
- khronos-NormalTangentTest: import results don't match expected look  

PRs welcome!  

## Contributing

This section is dedicated to those who wish to contribute to the project. This should clarify the main project structure without flooding you with too many details.
UnityGLTF project is divided into two parts: the GLTFSerializer assembly, which doesn't have a dependency on UnityEngine/UnityEditor, and the UnityGltf assembly.

<details>
<summary>More Details</summary>

### [GLTFSerializer](https://github.com/KhronosGroup/UnityGLTF/tree/master/GLTFSerialization)

- **Basic Rundown**: The GLTFSerializer facilitates serialization of the Unity asset model, and deserialization of GLTF assets.

- **Structure**: 
	- Each GLTF schemas (Buffer, Accessor, Camera, Image...) extends the basic class: GLTFChildOfRootProperty. Through this object model, each schema can have its own defined serialization/deserialization functionalities, which imitate the JSON file structure as per the GLTF specification.
	- Each schema can then be grouped under the GLTFRoot object, which represents the underlying GLTF Asset. Serializing the asset is then done by serializing the root object, which recursively serializes all individual schemas. Deserializing a GLTF asset is done similarly: instantiate a GLTFRoot, and parse the required schemas.

### [The Unity Project](https://github.com/KhronosGroup/UnityGLTF/tree/master/UnityGLTF)

- **Unity Version**
	Be sure that the Unity release you have installed on your local machine is *at least* the version configured for the project (using a newer version is supported). You can download the free version [here](https://unity3d.com/get-unity/download/archive). You can run this project simply by opening the directory as a project on Unity.
- **Project Components**
	The Unity project offers two main functionalities: importing and exporting GLTF assets. These functionalities are primarily implemented in `GLTFSceneImporter` and `GLTFSceneExporter`.

### Tests
To run tests with UnityGLTF as package, you'll have to add UnityGLTF to the "testables" array in manifest.json.

### The Server-Side Build

For details on the automated server-side builds and how to update them, see [\scripts\ServerBuilds.md](https://github.com/KhronosGroup/UnityGLTF/blob/master/scripts/ServerBuilds.md).

</details>

## Samples

1. Add the package to your project as described above
2. Open Package Manager and select UnityGLTF
3. Import any sample.