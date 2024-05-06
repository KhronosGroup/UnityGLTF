# UnityGLTF <!-- omit from toc -->

![Great coverage of glTF 2.0.](https://img.shields.io/badge/glTF%20Spec-2.0-brightgreen)
![Unity 2021.3+ and URP recommended](https://img.shields.io/badge/Unity-2021.3%E2%80%932023.1%2B-brightgreen)
![Support for 2020.3 is not actively maintained](https://img.shields.io/badge/Unity-2020.3-yellow)

![URP supported](https://img.shields.io/badge/Render%20Pipeline-URP-brightgreen)
![BiRP supported with better support on 2021.3+](https://img.shields.io/badge/Render%20Pipeline-Built--in-brightgreen)
![HDRP support is not actively maintained](https://img.shields.io/badge/Render%20Pipeline-HDRP-yellow)

Unity3D library for importing and exporting [glTF 2.0](https://github.com/KhronosGroup/glTF/) assets.

|        | Editor | Runtime |
|--------|--------|---------|
| Import | ✅      | ✅       |
| Export | ✅      | ✅       |

UnityGLTF doesn't have any native dependencies (pure C#) and thus works on all platforms that Unity supports. This includes WebGL import and export.

The library is designed to be easy to extend with additional extensions to the glTF specification. Both import and export allow attaching custom plugins and callbacks and can be heavily modified to fit into specific pipelines. Many glTF extensions are supported.

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
- [Exporting glTF Files](#exporting-gltf-files)
  - [Testing, debugging, compatibility](#testing-debugging-compatibility)
- [Animation Export](#animation-export)
  - [Animator Controller](#animator-controller)
  - [GLTFRecorder](#gltfrecorder-api)
  - [Timeline Recorder](#timeline-recorder)
  - [Legacy: Animation Component](#legacy-animation-component)
  - [KHR\_animation\_pointer](#khr_animation_pointer-support)
- [Blendshape Export](#blend-shape-export)
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

You can install this package from the Needle Package Registry with a one-click installer:  

1. Download [UnityGLTF Package Installer](https://package-installer.glitch.me/v1/installer/Needle/org.khronos.unitygltf?registry=https://packages.needle.tools)
2. Drop the downloaded .unitypackage into Unity and follow the steps.

You can also install this package from git, compatible with UPM (Unity Package Manager).
1. Open `Window > Package Manager`
2. Click <kbd>+</kbd>
3. Select <kbd>Add Package from git URL</kbd>
4. Paste
   ```
   https://github.com/KhronosGroup/UnityGLTF.git
   ```
5. Click <kbd>Add</kbd>.

> **Note**: If you want to target a specific version, append `#release/<some-tag>` or a specific commit to the URL above.
> Example: `https://github.com/KhronosGroup/UnityGLTF.git#release/2.9.0-rc`.

## Unity Version and Render Pipeline Compatibility

Please use Long-Term Support versions of Unity (2021.3+, 2022.3+, 2023.3+).

**Recommended:**
- Unity 2021.3+, Unity 2022.3+, Unity 2023.3+
- Linear colorspace
- Universal Render Pipeline (URP) and Built-In Render Pipeline (BiRP)

**Tested:**
- Unity 2020.3+
- Linear colorspace
- Universal Render Pipeline (URP) and Built-In Render Pipeline (BiRP)

**Legacy:**  
These configurations have been working in the past. They will not be updated with material extensions or new features. Also, issues in these configurations will most likely not be addressed if they're not also happening on later versions.
- Unity 2018.4–2019.4
- Gamma colorspace

**HDRP**:
- Currently limited functionality.

> **Note:** Issues on non-LTS Unity versions (not on 2020.3, 2021.3, 2022.3, ...) will most likely not be addressed. Please use LTS (Long-Term Support) versions where possible.

## UnityGLTF and glTFast

A separate glTF implementation for Unity, [glTFast](https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@latest), is available from the Unity Registry.  
glTFast being supported by Unity means, in a nutshell, that it sticks to standards pretty strictly and can't easily ship non-ratified extensions or experimental features that work for the majority, but not all, of users.  
- UnityGLTF aims to be the more _flexible_ framework, with extensive import/export plugin support and useful plugins out of the box.  
- glTFast aims to be the more _performant_ framework, with a focus on leveraging Unity-specific features such as Burst and Jobs.  
- UnityGLTF has a versatile plugin/extension infrastructure. This allows for a lot of flexibility during import/export.
- UnityGLTF enables the use of and ships with non-ratified extensions such as `KHR_animation_pointer`, `KHR_audio`, and `KHR_materials_variants`.
- glTFast leverages Unity-specific features such as Burst and Jobs and thus can have better performance in some cases.
- glTFast has better HDRP support.

glTFast and UnityGLTF can **coexist in the same project**; you can for example use glTFast for import and UnityGLTF for export.  
For imported assets, you can choose which importer to use with a dropdown.  
glTFast import has precedence if both are in the same project. See also [Default Importer Selection](#default-importer-selection).

## Supported Features and Extensions
The lists below are non-conclusive and in no particular order. Note that there are gaps where features could easily be supported for im- and export but currently aren't. PRs welcome!

### Import and Export

- Animations
- Skinned Mesh Renderers
- Blend Shapes
  - Sparse accessors for Blend Shapes
- Linear and Gamma colorspace support (Gamma won't be maintained anymore)
- Vertex Colors
- Cameras (perspective, orthographic)
- URP and Built-In Render Pipeline [Learn More](#material-and-shader-export-compatibility)
- KHR_lights_punctual (point, spot, and directional lights)
- KHR_texture_transform (UV offset, scale, rotation)
- KHR_materials_unlit
- KHR_materials_transmission (glass-like materials)
- KHR_materials_volume (refractive materials)
- KHR_materials_ior (for transmission and volume)
- KHR_materials_emissive_strength (emissive values greater than 1)
- KHR_materials_iridescence (thin-film interference, like oil on water)
- KHR_materials_clearcoat (secondary specular layer, like a coat of varnish)
- KHR_materials_specular (partial support)
- KHR_materials_dispersion (refractive index dispersion)
- MSFT_lods (level of detail)
- KHR_animation_pointer (arbitrary property animations)

### Import only

- KHR_mesh_quantization (smaller buffers)
- EXT_mesh_gpu_instancing (instance data)
- KHR_draco_mesh_compression (requires [`com.unity.cloud.draco`](https://docs.unity3d.com/Packages/com.unity.cloud.draco@latest))
- KHR_texture_basisu (requires [`com.unity.cloud.ktx`](https://docs.unity3d.com/Packages/com.unity.cloud.ktx@latest))
- EXT_meshopt_compression (requires [`com.unity.meshopt.decompress`](https://docs.unity3d.com/Packages/com.unity.meshopt.decompress@latest))

### Export only

- KHR_materials_variants
- Timeline recorder track for exporting animations in the editor and at runtime
- Lossless keyframe optimization on export
- All 2D textures can be exported, RenderTextures included – they're baked at export.
- Optional plugin: Bake TMPro 3D objects to meshes on export
- Optional plugin: Bake Particle Systems to meshes on export
- Optional plugin: Bake Canvas to meshes on export
- Included plugin sample: KHR_audio

## glTF Materials

To leverage the extended material model of glTF in Unity, use the `UnityGLTF/PBRGraph` material.  
It allows the use of various glTF material extensions for import, export, and inside Unity.  
This includes features that URP is missing, such as transmission, volume, and per-texture UV control.  

### Material Conversions

UnityGLTF contains helpers to make converting to UnityGLTF/PBRGraph easy.  
When you switch a material from any shader to PBRGraph, an automatic conversion can run to bring the properties over.  
Some shaders already come with automatic conversions, please see the table below.

When a shader doesn't have a converter yet, UnityGLTF will ask if you want to create a *Conversion Script*.  These scripts contain all properties of the source shader and the target shader, but no specified mapping yet (as that depends on the intent of the shader author).  
After the conversion script has been created, you can edit it to correctly map from the source shader's properties to PBRGraph properties.   
When you switch such a shader to PBRGraph the next time, your conversion script will run and automatically translate the materials in the specified way.

> **Note:** Currently, conversion scripts aren't used automatically on glTF export. Convert materials at edit time for best results.

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

### Configure for Refractive Materials (Transmission and Volume)

Transmission and Volume allow rendering materials like glass, that are fully transparent but still show reflections, as well as volume attenuation (e.g. colored jelly) and rough refraction (e.g. brushed glass).
To use these material features, you need to do some setup on the material and your render pipeline. 

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

> **Note**: Fully metallic materials are never transparent.  See [the KHR_materials_transmission spec](https://github.com/KhronosGroup/glTF/blob/main/extensions/2.0/Khronos/KHR_materials_transmission/README.md#transparent-metals) for more info.

## Exporting glTF Files

To export an object from a scene or your project,
1. Select the object
2. Use the menu items under `Assets > UnityGLTF > Export selected as GLB` / `Export selected as glTF` to export

> **Tip:** You can set shortcuts for quick export in Unity's Shortcut Manager. For example,
<kbd>Ctrl + Space</kbd> for GLB export and <kbd>Ctrl + Shift + Space</kbd> for glTF export allow for very fast iteration.

### Testing, debugging, compatibility

The various glTF viewers in existence have varying feature sets. Only a select few have full coverage of the glTF spec, most only support a subset.   
Notable features with limited support:

- setting `textureCoord` per texture.
- setting `textureRotation`. Many viewers simply ignore it.
- sparse accessors. Some viewers support sparse accessors only for blend shapes, others don't support it at all.
- vertex colors.

To view your glTF files, here's a number of tools you can use:

| Name                                                                                 | Notes                                                                                                                            |
|--------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------|
| [Khronos glTF Sample Viewer](https://github.khronos.org/glTF-Sample-Viewer-Release/) | Full support for ratified extensions                                                                                             |
| [gltf.report](https://gltf.report)                           | Inspect file size, meshes, textures                                                                                              | 
| [model-viewer](https://modelviewer.dev/editor)               | Support for KHR_materials_variants with custom code                                                                              |
| [Gestaltor](https://gestaltor.com/)                                                  | Full glTF Spec Compliance<br/>Support for KHR_animation_pointer<br/>Support for KHR_audio<br/>Support for KHR_materials_variants | 
| [Needle Viewer](https://viewer.needle.tools)                 | Support for KHR_animation_pointer<br/>Inspect hierarchy, textures, cameras, lights, warnings                                     |
| [Babylon.js Sandbox](https://sandbox.babylonjs.com/)                                 | Support for KHR_animation_pointer                                                                                                |
| UnityGLTF<br/>(this project!)                                                        | Simply drop the exported glb file back into Unity.                                                                               |
| [glTFast](https://github.com/Unity-Technologies/com.unity.cloud.gltfast)             | Add the glTFast package to your project.<br/>You can switch the used importer on glTF files between glTFast and UnityGLTF.       |

To further process files after exporting them with UnityGLTF, you can use:

| Name                                                     | Notes                                                                                                                     | 
|----------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------|
| [gltf-transform](https://gltf-transform.donmccurdy.com/) | Compress meshes with draco or meshopt<br/>Compress textures to ktx2<br/>Optimize Files<br/>Convert between .gltf and .glb |
| [Blender](https://www.blender.org/)                                             | Import/export glTF files with good feature coverage                                                                       |

## Animation Export

Animations can be exported both in the Editor and at runtime. 
Editor export works with Animator (multiple clips), Animation (multiple clips), and Timeline (GLTFRecorderTrack).

### Animator Controller

You can export entire Animators and their clips as glTF files with multiple animations.  
Animation clips will be named after each Motion State in the Animator Controller.  
The "speed" property of each Motion will be baked into the exported clip. Ensure the speed is 1 when you want to export unchanged.    
Any number of Animators in a hierarchy is supported, as is any number of clips in those.

Both Humanoid and Generic animations will be exported. Humanoid animations are baked onto the target rig at export time.

> **Note**: Animator export only works in the Editor. For runtime export, use the GLTFRecorder capabilities or the Timeline Recorder.

### GLTFRecorder API

For creating and/or recording animations at runtime, you can use the GLTFRecorder API. It allows to capture the state of entire hierarchies and complex animations and export them directly as glTF file, 
optionally with KHR_animation_pointer support for complex material and property animations.  
See `GLTFRecorderComponent` for an example implementation.  

### Timeline Recorder

Timelines or sections of them can be recorded with a `GltfRecorderTrack` and one or more `GltfRecorderClips`. 
Timeline recording uses the GLTFRecorder API under the hood. 

### Legacy Animation Component

> **Note**: Animation Component export only works in the Editor. For runtime export, use the GLTFRecorder capabilities.

Animation components and their legacy clips can also be exported.

### KHR_animation_pointer support

UnityGLTF supports exporting animations with the KHR_animation_pointer extension. 
The core glTF spec only allows animation node transforms and blend shape weights, while this extension allows animating arbitrary properties in the glTF file – 
including material and object properties, even in custom extensions and script components.

Exporting with KHR_animation_pointer can be turned on in `Project Settings > UnityGLTF` by enabling the KHR_animation_pointer plugin.

> **Note:** The exported files can be viewed with Gestaltor, Babylon Sandbox, and Needle Engine, but currently not with three.js / model-viewer. See https://github.com/mrdoob/three.js/pull/24108. They can also not be reimported into UnityGLTF at this point.

## Blend Shape Export

Morph Targets / Blend Shapes / Shape Keys are supported, including animations.  
To create smaller files for complex blend shape animations (e.g. faces with dozens of shapes), export with the "Sparse Accessors" setting enabled.

## Importing glTF files

### Editor Import

For importing `.gltf` or `.glb` files in the editor, place them in your project as usual (Assets or Packages). 
Make sure to bring .bin/textures along for `.gltf` files with the correct relative paths;`.glb` is usually self-contained.

When moving `.gltf` files inside Unity, make sure to move their .bin/texture files as well, to not break the path references between them.

### Default Importer Selection

UnityGLTF will register itself as the default importer for the `.gltf` and .`glb` extensions.  
If the [glTFast package](https://github.com/atteneder/glTFast) is also present in a project, **glTFast gets precedence** and UnityGLTF is available as Importer Override, which can be selected from a dropdown on each glTF asset.

UnityGLTF uses Unity's `ScriptedImporter` system. For any given file format (file extension) there has to be one default importer and there can be additional, alternative importers.  
You can make UnityGLTF the default importer and de-prioritize glTFast by adding the following settings to your project's `Scripting Defines`:

```
GLTFAST_FORCE_DEFAULT_IMPORTER_OFF
UNITYGLTF_FORCE_DEFAULT_IMPORTER_ON
```

Care has been taken to align glTFast's and UnityGLTF's importers, so that in most cases you can switch between them without breaking prefab references. That being said, switching between importers can change material references, mesh references etc., so some manual adjustments may be needed.
You may have to adjust the root stripping settings of each importer (glTFast and UnityGLTF do that slightly differently).

## Animation Import

Animations can be imported both in the Editor and at runtime.  
On the importer, you can choose between "Legacy", "Mecanim" or "Humanoid" clips.

At runtime, if you're importing "Mecanim" clips, you need to make sure to add them to a playable graph via script (e.g. Animator Controller or Timeline) to play them back.

## Extensibility

UnityGLTF has import and export plugins. These have callbacks for modifying node structures, extension data, materials and more as part of the regular export and import process. They are used both in the Editor and at runtime.
You can make your own plugins and enable them in the `Project Settings > UnityGLTF` menu.  
Plugins are ScriptableObjects that can have settings; they're serialized as part of the GLTFSettings asset. 
Plugins create concrete instances of import/export handlers.  

To create a plugin, follow these steps:
1. Make a class that inherits from `GLTFImportPlugin` or `GLTFExportPlugin`. This is the ScriptableObject that contains plugin settings.
2. Also, make a class that inherits from `GLTFImportPluginContext` or `GLTFExportPluginContext`. This class has the actual callbacks.
3. Implement `CreateInstance` in your plugin to return a new instance of your plugin context.
4. Override the callbacks you want to use in your plugin context.

If your plugin reads/writes custom extension data, you need to also implement `GLTF.Schema.IExtension` for serialization and deserialization.

> 🏗️ Under construction. You can take a look at `MaterialVariantsPlugin.cs` for an example.

## Known Issues

- Blend shapes with in-betweens are currently not supported. 
- Support for glTF files with multiple scenes is limited. 
- Some material options (e.g. MAOS maps) are currently not exported correctly. 

## Contributing

> **Note**: As of 20240129 the default branch of this repository has been renamed from `master` to `main`.

UnityGLTF is an open-source project. Well-tested PRs are welcome.  

It is currently maintained by
- [prefrontal cortex](https://prefrontalcortex.de), member of the Khronos Group and 
- [Needle](https://needle.tools), member of the Metaverse Standards Forum.

<details>
<summary>More Details (legacy)</summary>

> 🏗️ Under construction. Feel free to raise an issue if you have questions.

### [Unity Package](https://github.com/KhronosGroup/UnityGLTF/tree/master/)

- **Unity Version**
  Be sure that the Unity release you have installed on your local machine is *at least* 2021.3.
- **Project Components**
  The Unity project offers two main functionalities: importing and exporting GLTF assets. These functionalities are primarily implemented in `GLTFSceneImporter` and `GLTFSceneExporter`.

### [GLTFSerializer](https://github.com/KhronosGroup/UnityGLTF/tree/master/Runtime/Plugins/GLTFSerialization)

- **Basic Rundown**: The GLTFSerializer facilitates serialization of the Unity asset model, and deserialization of GLTF assets.

- **Structure**:
  - Each GLTF schemas (Buffer, Accessor, Camera, Image...) extends the basic class: GLTFChildOfRootProperty. Through this object model, each schema can have its own defined serialization/deserialization functionalities, which imitate the JSON file structure as per the GLTF specification.
  - Each schema can then be grouped under the GLTFRoot object, which represents the underlying GLTF Asset. Serializing the asset is then done by serializing the root object, which recursively serializes all individual schemas. Deserializing a GLTF asset is done similarly: instantiate a GLTFRoot, and parse the required schemas.

### Tests

> 🏗️ Under construction. Tests are currently in a separate (private) repository due to test asset licensing reasons.

To run tests with UnityGLTF as package, you'll have to add UnityGLTF to the "testables" array in manifest.json:

```
"testables": [
	"org.khronos.unitygltf"
]
```

</details>
