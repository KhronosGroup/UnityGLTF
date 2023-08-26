# UnityGLTF <!-- omit from toc -->

![Good coverage of glTF 2.0.](https://img.shields.io/badge/glTF%20Spec-2.0-brightgreen)
![Unity 2021.3+ and URP recommended](https://img.shields.io/badge/Unity-2021.3%E2%80%932023.1%2B-brightgreen)
![Support for 2018.4–2019.4 is not actively maintained](https://img.shields.io/badge/Unity-2019.4%E2%80%932020.3-yellow)

![URP supported](https://img.shields.io/badge/Render%20Pipeline-URP-brightgreen)
![BiRP supported with better support on 2021.3+](https://img.shields.io/badge/Render%20Pipeline-Built--in-brightgreen)
![HDRP support is not actively maintained](https://img.shields.io/badge/Render%20Pipeline-HDRP-yellow)

Unity3D library for importing and exporting [glTF 2.0](https://github.com/KhronosGroup/glTF/) assets.

|        | Editor | Runtime  |
|--------|--------|----------|
| Import | ✅     | ✅       |
| Export | ✅     | ✅       |

UnityGLTF doesn't have any native dependencies (pure C#) and thus works on all platforms that Unity supports. This includes WebGL import and export.

The library is designed to be easy to extend with additional extensions to the glTF specification. Both import and export allow attaching custom callbacks and can be heavily modified to fit into specific pipelines. Many glTF extensions are supported.

## Contents <!-- omit from toc -->
- [Installation](#installation)
- [Unity Version and Render Pipeline Compatibility](#unity-version-and-render-pipeline-compatibility)
- [UnityGLTF and glTFast](#unitygltf-and-gltfast)
- [Supported Features and Extensions](#supported-features-and-extensions)
  - [Import and Export](#import-and-export)
  - [Export only](#export-only)
  - [Import only](#import-only)
- [glTF Materials](#gltf-materials)
  - [Material Conversions](#material-conversions)
  - [Configure for Refractive Materials (Transmission and Volume)](#configure-for-refractive-materials-transmission-and-volume)
    - [Material Setup](#material-setup)
    - [URP](#urp)
    - [Built-In](#built-in)
    - [HDRP](#hdrp)
  - [Material and Shader Export Compatibility](#material-and-shader-export-compatibility)
  - [Legacy](#legacy)
- [Exporting glTF Files](#exporting-gltf-files)
  - [Testing, debugging, compatibility](#testing-debugging-compatibility)
- [Animation Export](#animation-export)
  - [Animator Controller](#animator-controller)
  - [GLTFRecorder](#gltfrecorder)
  - [Timeline Recorder](#timeline-recorder)
  - [Legacy: Animation Component](#legacy-animation-component)
  - [KHR\_animation\_pointer](#khr-animation-pointer)
- [Blendshape Export](#blendshape-export)
- [Importing glTF files](#importing-gltf-files)
  - [Editor Import](#editor-import)
  - [Default Importer Selection](#default-importer-selection)
- [Animation Import](#animation-import)
- [Extensibility](#extensibility)
- [Known Issues](#known-issues)
- [Contributing](#contributing)
  - [Unity Package](#unity-package)
  - [GLTFSerializer](#gltfserializer)
  - [Tests](#tests)

## Installation

You can install this package from git, compatible with UPM (Unity Package Manager).
1. Open `Window > Package Manager`
2. Click <kbd>+</kbd>
3. Select <kbd>Add Package from git URL</kbd>
4. Paste
   ```
   https://github.com/prefrontalcortex/UnityGLTF.git
   ```
4. Click <kbd>Add</kbd>.

> **Note**: If you want to target a specific version, append `#release/<some-tag>` or a specific commit to the URL above.

## Unity Version and Render Pipeline Compatibility

The best results for import and export workflows with material extensions can be achieved on Unity 2021.3.8f1+ with URP.

**Recommended:**
- Unity 2021.3+
- Linear colorspace
- Universal Render Pipeline (URP) and Built-In Render Pipeline (BiRP)

**Supported:**
- Unity 2020.3+
- Linear colorspace
- Universal Render Pipeline (URP) and Built-In Render Pipeline (BiRP)

**Legacy:**  
These configurations have been working in the past. They will not be updated with material extensions or new features. Also, issues in these configurations will most likely not be addressed if they're not also happening on later versions.
- Unity 2018.4–2019.4
- Gamma colorspace

> **Note:** Issues on non-LTS Unity versions (not on 2020.3, 2021.3, 2022.3, ...) will most likely not be addressed. Please use LTS (Long-Term Support) versions where possible.

## UnityGLTF and glTFast

UnityGLTF hasn't received official support since early 2020. However, a number of forks have fixed issues and improved several key areas, especially animation support,export workflows, color spaces and extendibility. These forks have now been merged back into main so that everyone can benefit from then, and to enable further work.

A separate glTF implementation for Unity, [glTFast](https://github.com/atteneder/glTFast), is on its path towards becoming feature complete.  
At the time of writing glTFast has better _import support_ and UnityGLTF has better _export support_.

glTFast and UnityGLTF can coexist in the same project; you can for example use glTFast for import and UnityGLTF for export, where each of these shine. For imported assets, you can choose which importer to use with a dropdown. glTFast import has precedence if both are in the same project.

Key differences of glTFast include:
- It leverages modern Unity features such as Burst and Jobs and thus has better performance in some cases
- Better import support: allows importing compressed textures and meshes
  – Slightly better coverage of the glTF Spec: texture transforms for other textures than baseMap and occlusionMap are supported
- HDRP import support.

> **TL;DR:**
> - UnityGLTF has very good export support (runtime, editor, animations, materials).
> - glTFast has better general import support (compressed meshes and textures, HDRP support).
> - If you're using custom extensions, UnityGLTF might still be the right choice for import for its extensibility.

## Supported Features and Extensions
The lists below are non-conclusive and in no particular order. Note that there are gaps where features could easily be supported for im- and export but currently aren't. PRs welcome!

### Import and Export

- Animations, Skinned Mesh Renderers, Blend Shapes
- Linear and Gamma colorspace support (Gamma won't be maintained anymore)
- Vertex Colors
- Cameras
- URP and Built-In Render Pipeline [Learn More](#material-and-shader-export-compatibility)
- KHR_lights_punctual (Point, Spot, Directional Lights)
- KHR_texture_transform
- KHR_materials_unlit
- KHR_materials_transmission
- KHR_materials_volume
- KHR_materials_ior
- KHR_materials_emissive_strength
- KHR_materials_iridescence
- KHR_materials_clearcoat
- KHR_materials_sheen (partial support)
- KHR_materials_specular (partial support)

### Import only

- MSFT_LODExtension
- KHR_mesh_quantization
- KHR_draco_mesh_compression (requires com.atteneder.draco)
- KHR_texture_basisu (requires com.atteneder.ktx)
- EXT_meshopt_compression (requires com.unity.meshopt.decompress)

### Export only

- KHR_animation_pointer
- Sparse accessors for Blend Shapes
- Timeline recorder track for exporting animations at runtime
- Lossless keyframe optimization on export (animation is baked but redundant keyframes are removed)
- All 2D textures can be exported, no matter if readable or not, RenderTextures included – they're baked at export.

## glTF Materials

To leverage the extended material model of glTF in Unity, use the `UnityGLTF/PBRGraph` material.  
It allows the use of various glTF material extensions for import, export, and inside Unity.

### Material Conversions

UnityGLTF contains helpers to make converting to UnityGLTF/PBRGraph easy.  
When you switch a material from any shader to PBRGraph, an automatic conversion can run to bring the properties over in the best possible way.  
Some shaders already come with automatic conversions:
- `Standard`
- `URP/Lit`
- `URP/Unlit`

When a shader doesn't have a converter yet, UnityGLTF will ask if you want to create a *Conversion Script*.  These scripts contain all properties of the source shader and the target shader, but no specified mapping yet (as that dependends on the intent of the shader author).  
After the conversion script has been created, you can edit it to correctly map from the source shader's properties to PBRGraph properties.   
When you switch such a shader to PBRGraph the next time, your conversion script will run and automatically translate the materials in the specified way.

> **Note:** Currently, conversion scripts aren't used automatically on glTF export. Convert materials at edit time for best results.

### Configure for Refractive Materials (Transmission and Volume)

Transmission and Volume allow rendering materials like glass, that are fully transparent but still show reflections, as well as volume attenuation (e.g. colored jelly) and rough refraction (e.g. brushed glass).

> **Note**: Fully metallic materials are never transparent.  See [the KHR_materials_transmission spec](https://github.com/KhronosGroup/glTF/blob/main/extensions/2.0/Khronos/KHR_materials_transmission/README.md#transparent-metals) for more info.

#### Material Setup
1. On a PBRGraph material, check "Enable Transmission" and optionally "Enable Volume"
2. Change the Transmission,  Thickness, Index of Refraction and Attenuation values as desired.

#### URP
To see transmissive and volume materials correctly in Unity,
1. Select your URP Renderer Asset
2. Under the "Renderer Features" section, add a "Opaque Texture (Rough Refraction)" feature

#### Built-In
To see transmissive and volume materials correctly in Unity,
1. Add the "RoughRefraction" component to your Main Camera.

#### HDRP
HDRP has its own rough refraction support. There's currently no automatic import / export support to convert to that. Use glTFast if you need this.

### Material and Shader Export Compatibility

If you want to design for glTF export, it's recommended to use Unity 2021.3+ with URP and the **UnityGLTF/PBRGraph** material. It comes with support for modern material extensions like refraction and iridescence, and allows for perfect roundtrips. Great for building glTF pipelines in and out of Unity.

| Render Pipeline                        | Shader                                                       | Notes                                  | Source             | 
|----------------------------------------|--------------------------------------------------------------|----------------------------------------|--------------------| 
| URP on 2020.3+<br/>Built-In on 2021.3+ | **UnityGLTF/PBRGraph** <br/>☝️ *Use this if you're not sure* | Perfect roundtrip, Material Extensions | UnityGLTF          |
|                                        | UnityGLTF/UnlitGraph                                         | Perfect roundtrip                      | UnityGLTF          |
|                                        | ShaderGraphs/glTF-pbrMetallicRoughness                       |                                        | glTFast            |
|                                        | ShaderGraphs/glTF-unlit                                      |                                        | glTFast            |
| URP                                    | URP/Lit                                                      |                                        | Unity              |
|                                        | URP/Unlit                                                    |                                        | Unity              |
| Built-In                               | Standard                                                     |                                        | Unity              | 
|                                        | GLTF/PbrMetallicRoughness                                    |                                        | UnityGLTF (legacy) |
|                                        | GLTF/Unlit                                                   |                                        | UnityGLTF (legacy) |
|                                        | glTF/PbrMetallicRoughness                                    |                                        | glTFast (legacy)   |
|                                        | glTF/Unlit                                                   |                                        | glTFast (legacy)   |
| HDRP (limited support)                 | HDRP/Lit                                                     |                                        | Unity              |
|                                        | HDRP/Unlit                                                   |                                        | Unity              |

### Legacy
These extensions and shaders worked in the past but are not actively supported anymore.

- KHR_materials_pbrSpecularGlossiness  
  *This extension has been deprecated by Khronos. Please use pbrMetallicRoughness instead.*
  - GLTF/PbrSpecularGlossiness (legacy shader from UnityGLTF)
  - ShaderGraphs/glTF-pbrSpecularGlossiness (legacy shader from glTFast)
  - glTF/PbrSpecularGlossiness (legacy shader from glTFast)

## Exporting glTF Files

To quickly export an object from a scene or your project,
1. Select the object
2. Use the menu items under `Assets > UnityGLTF > Export selected as GLB` / `Export selected as glTF` to export

> **Tip:** You can set shortcuts for quick export in Unity's Shortcut Manager.  
<kbd>Ctrl + Space</kbd> for GLB export and <kbd>Ctrl + Shift + Space</kbd> for glTF export allow for super quick iteration.

### Testing, debugging, compatibility

The various glTF viewers in existence have varying feature sets. Only a select few have full coverage of the glTF spec, most only support a subset.   
Notable features with limited support:

- setting `textureCoord` per texture.
- setting `textureRotation`. Many viewers simply ignore it.
- sparse accessors. Some viewers support sparse accessors only for blend shapes, others don't support it at all.
- vertex colors.
-
To test your files, here's a number of other viewers you can use:

| Name                                                                   | Notes                                                                                                                            |
|------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------|
| [gltf.report](https://gltf.report)<br/>(based on three.js)             | Great for inspecting file size, meshes, textures                                                                                 | 
| [model-viewer](https://modelviewer.dev/editor)<br/>(based on three.js) | Support for KHR_materials_variants with custom code                                                                              |
| [Gestaltor]()                                                          | Full glTF Spec Compliance<br/>Support for KHR_animation_pointer<br/>Support for KHR_audio<br/>Support for KHR_materials_variants | 
| [Babylon.js Sandbox](https://sandbox.babylonjs.com/)                   | Support for KHR_animation_pointer                                                                                                |
| UnityGLTF<br/>(this project!)                                          | Simply drop the exported glb file back into Unity.                                                                               |
| [glTFast](https://github.com/atteneder/glTFast)                        | Add the glTFast package to your project.<br/>You can switch the used importer on glTF files between glTFast and UnityGLTF.       |

To further process files after exporting them with UnityGLTF, you can use

| Name                                                     | Notes                                                                                                                     | 
|----------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------|
| [gltf-transform](https://gltf-transform.donmccurdy.com/) | Compress meshes with draco or meshopt<br/>Compress textures to ktx2<br/>Optimize Files<br/>Convert between .gltf and .glb |

## Animation Export

Animations can be exported both in the Editor and at runtime.

### Animator Controller

You can export entire Animators and their clips as glTF files with multiple animations.  
Animation clips will be named after each Motion State in the Animator Controller.  
The "speed" property of each Motion will be baked into the exported clip. Ensure the speed is 1 when you want to export unchanged.    
Any number of Animators in a hierarchy is supported, as is any number of clips in those.

Both Humanoid and Generic animations will be exported. Humanoid animations are baked onto the target rig at export time.

> **Note**: Animator export only works in the Editor. For runtime export, use the GLTFRecorder capabilities or the Timeline Recorder.

### GLTFRecorder

For creating and/or recording animations at runtime, you can use the GLTFRecorder. It allows to *capture* the state of entire hierarchies and complex animations and export them directly as glTF file, with or without KHR_animation_pointer support.  
An example is given as GLTFRecorderComponent.

### Timeline Recorder

Timelines or sections of them can be recorded with a GltfRecorderTrack and one or more GltfRecorderClips.

### Legacy Animation Component

> **Note**: Animation Component export only works in the Editor. For runtime export, use the GLTFRecorder capabilities.

Animation components and their legacy clips can also be exported.

### KHR_animation_pointer

UnityGLTF supports exporting animations with the KHR_animation_pointer extension. The core glTF spec only allows animation node transforms and blendshape weights, while this extension allows animating arbitrary properties in the glTF file – including material properties, and even in custom extensions.

Exporting with KHR_animation_pointer can be turned on in `Project Settings > UnityGLTF > Use Animation Pointer`.

> **Note:** The exported files can be viewed with Gestaltor, Babylon Sandbox, and Needle Engine, but currently not with three.js / model-viewer. See https://github.com/mrdoob/three.js/pull/24108. They can also not be reimported into UnityGLTF at this point.

## Blendshape Export

Morph Targets / Blend Shapes are supported, including animations.  
To create smaller files for complex blendshape animations (e.g. faces with dozens of shapes), you can export with "Sparse Accessors" on.

> **Note**: While exporting with sparse accessors works, importing blend shapes with sparse accessors is currently not supported.

## Importing glTF files

### Editor Import

For importing `.gltf` or `.glb` files in the editor, place them in your Asset Database as usual. Make sure to bring bin/textures along for `.gltf`; `.glb` is usually self-contained.

When moving `.gltf` files inside Unity, make sure to move their bin/texture files as well, to not break the path references between them.

### Default Importer Selection

UnityGLTF uses Unity's `ScriptedImporter` interface. For any given file format (file extension) there has to be one default importer and there can be additional, alternative importers.

UnityGLTF will register itself as the default importer for the .gltf and .glb extensions.  
If the [glTFast package](https://github.com/atteneder/glTFast) is also present in a project, **glTFast gets precedence** and UnityGLTF is available as Importer Override, which can be selected from a dropdown on each glTF asset.

You can make UnityGLTF the default importer and de-prioritize glTFast by adding the following settings to your project's scripting defines:

```
GLTFAST_FORCE_DEFAULT_IMPORTER_OFF
UNITYGLTF_FORCE_DEFAULT_IMPORTER_ON
```

Care has been taken to align glTFast's and UnityGLTF's importers, so that in most cases you can switch between them without breaking prefab references. That being said, switching between importers can change material references, mesh references etc., so some manual adjustments may be needed after switching.

## Animation Import

Animations can be imported both in the Editor and at runtime.  
On the importer, you can choose between "Legacy" or "Mecanim" clips.

At runtime, if you're importing "Mecanim" clips, you need to make sure to add them to a playable graph (e.g. Animator Controller or Timeline) to play them back.

## Extensibility

There's lots of attachment points for exporting and importing glTF files with UnityGLTF. These allow modifying node structures, extension data, materials and more as part of the regular export and import process both in the Editor and at runtime.

> 🏗️ Under construction. You can take a look at `MaterialExtensions.cs` for an example in the meantime.

## Known Issues

Known issues reproduce with specific [glTF Sample Models](https://github.com/KhronosGroup/glTF-Sample-Models/tree/master/2.0):

- **khronos-SimpleSparseAccessor**: sparse accessors not supported on import (can be exported though)
- **khronos-TriangleWithoutIndices**: meshes without indices import with wrong winding order
- **khronos-MultiUVTest**: UV per texture is imported but not supported in the GLTF-Builtin shader
- **khronos-MorphPrimitivesTest**: isn't correctly importing at runtime (in some cases?)
- **khronos-NormalTangentTest**: import results don't match expected look

## Contributing

This section is dedicated to those who wish to contribute to the project.

<details>
<summary>More Details</summary>

### [Unity Package](https://github.com/KhronosGroup/UnityGLTF/tree/master/)

- **Unity Version**
  Be sure that the Unity release you have installed on your local machine is *at least* the version configured for the project (using a newer version is supported). You can download the free version [here](https://unity3d.com/get-unity/download/archive). You can run this project simply by opening the directory as a project on Unity.
- **Project Components**
  The Unity project offers two main functionalities: importing and exporting GLTF assets. These functionalities are primarily implemented in `GLTFSceneImporter` and `GLTFSceneExporter`.

### [GLTFSerializer](https://github.com/KhronosGroup/UnityGLTF/tree/master/Runtime/Plugins/GLTFSerialization)

- **Basic Rundown**: The GLTFSerializer facilitates serialization of the Unity asset model, and deserialization of GLTF assets.

- **Structure**:
  - Each GLTF schemas (Buffer, Accessor, Camera, Image...) extends the basic class: GLTFChildOfRootProperty. Through this object model, each schema can have its own defined serialization/deserialization functionalities, which imitate the JSON file structure as per the GLTF specification.
  - Each schema can then be grouped under the GLTFRoot object, which represents the underlying GLTF Asset. Serializing the asset is then done by serializing the root object, which recursively serializes all individual schemas. Deserializing a GLTF asset is done similarly: instantiate a GLTFRoot, and parse the required schemas.

### Tests

To run tests with UnityGLTF as package, you'll have to add UnityGLTF to the "testables" array in manifest.json:

```
"testables": [
	"org.khronos.unitygltf"
]
```

</details>