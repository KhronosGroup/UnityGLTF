# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.17.6] - 2025-08-06
- fix: order of export checks was wrong for `emissiveFactor` / `_EmissionColor`
- fix: update to latest KHR_interactivity specification changes from July 2025
- fix: prevent endless loop in KHR_interactivity type conversion in some edge cases
- fix: correct input types for `p1` and `p2` in `pointer/interpolate` schema
- add: support for member interpolation for `Material.mainColor`

## [2.17.5] - 2025-07-25
- fix: Compiler error when TMP is not installed

## [2.17.4] - 2025-07-17
- fix: Exporting animation should not abort export for unknown properties
- fix: Warn when UnityGLTF/PBRGraph can't be imported due to insufficient shader variant limit in Unity

## [2.17.3] - 2025-07-13
- fix: Restore accidentally removed using
- fix: Prevent NullReferenceException when exporting textures via callback without other textures in the file

## [2.17.2] - 2025-07-07
- add: Better redundant node cleanup and precomputation for KHR_interactivity export
- add: Interactivity nodes `Color.Create()`, `Random.insideUnitSphere`, `Random.onUnitSphere`
- add: Interactivity nodes `Vector3.MoveTowards()`, `Vector3.Reflect()`, `Vector3.sqrMagnitude`
- add: Interactivity nodes `Transform.forward`, `Transform.right`, `Transform.up`
- add: Interactivity nodes `Transform.TransformPoint()`, `Transform.InverseTransformPoint()`, `Transform.GetPositionAndRotation()`, `Transform.SetPositionAndRotation()`
- change: Interactivity specification changes from June 2025 (renaming of `rotate2d` and `rotate3d` inputs)
- fix: Add Iridescence properties to material property map for roundtrip support
- fix: Generate mipmaps for TextMeshPro textures on export
- fix: Remove baseVertex offset from submesh descriptors when generating lightmap UVs (#668)
- fix: Order of operations for `Transform.Rotate()` interactivity export
- fix: `Matrix4x4` index access order for interactivity export
- fix: Added `isValid` output for `math/inverse` and `math/normalize` schema

## [2.17.1] - 2025-06-12
- fix: define UnityStereoTransformScreenSpaceTex when it doesn't exist (e.g. some HDRP configurations)

## [2.17.0] - 2025-06-12
- add: Quaternion nodes for KHR_interactivity visual scripting export
- add: Importing PNG files without alpha channel in the Editor now sets them as RGB instead of RGBA, leading to better compression and less memory usage (#858)
- add: Add more bone names to Humanoid import for better compatibility with Unity's FBX importer (#862)
- change: Implemented specification changes for KHR_interactivity from May 2025
- change: Enabled `applyRootMotion` for humanoid animator to make it consistent with FBX importer (#868)
- fix: Sprite Mesh export threw an exception in non-Simple sprite draw mode. Use Unity 2023.2+ for other sprite modes than "Simple".
- fix: Audio plugin did not correctly check for existance of built-in Unity modules
- fix: Runtime errors with serialization of editor-only components in some newer Unity versions
- fix: Improved performance for extracting all materials from an editor-imported glTF file (#855)
- fix: Marked baseColor and baseColorTexture as main color and main map so they work with Unity's `Material.color` and `Material.mainTexture` properties (#864)
- fix: Use correct `Auto` queue value for imported materials (#866)

## [2.16.1] - 2025-05-20
- add: Visual Scripting variables work across multiple scenes now
- add: partial `KHR_materials_anisotropy` roundtrip support. There is no visual support for it yet, but data is imported and exported correctly from `PBRGraph`.
- add: experimental `KHR_audio_emitter support`. Please note that this extension is not yet ratified and implementation details may change. The plugin is disabled by default, enable it in `UnityGLTFSettings`.
- add: Sprite mesh export support through "Bake to Mesh" plugin.
- add: `KHR_interactivity` plugin now has a button to install the `com.unity.visualscripting` package
- add: visual badges for non-ratified and experimental extension plugins
- change: the Export API for `KHR_interactivity` is still undergoing heavy changes while the extension is being finalized.
- change: removed the `KHR_audio` sample since it's now shipping with this package
- fix: generated shader converter code doesn't use obsolete API anymore
- fix: various fixes to visual scripting export support through `KHR_interactivity`
- fix: fixed incorrect namespace that led to compilation errors in some specific cases
- fix: wrong asset import check for textures, which led to non-compressed textures at runtime (Fixes #846)

## [2.16.0-pre.3] - 2025-04-15
- fix: workaround for URP error in Unity 6+ with new Render Graph API (uncatchable and incorrect exception during on-demand rendering)
- fix: issue when deduplicating shared meshes with different materials (#836)
- change: numerous KHR_interactivity API changes to make runtime usage and extensibility easier
- change: remove KHR_audio sample from package samples. If you're interested in KHR_audio_emitter support, please follow the progress of adding it as properly supported extension here: https://github.com/KhronosGroup/UnityGLTF/pull/835.

## [2.16.0-pre.2] - 2025-04-03
- fix: regression when batch exporting material-only files
- fix nullreference when loading a materials-only file with `GLTFSceneImporter` or `GLTFComponent`
- fix GLTFComponent failing when the loaded file is a web URL but "Load from streaming assets" is on (the default). If the URI starts with `http://` or `https://`, we now automatically load from the web.
- fix: GPU instancing can't be enabled on Shader Graph-based materials when the built-in render pipeline is active. Unity does not support GPU instancing with Shader Graph.

## [2.16.0-pre.1] - 2025-04-03
- add: Interactivity Export API now has `AddLog` method that takes log settings into account, so app-specific logging can be used
- add: Transform Modes for batch exporting from the UnityGLTF menu (`Auto`, `Local`, `World`, `Reset`), with options covering various use cases
- add: editor setting for export type (GLB vs. glTF) from the UnityGLTF menu
- add: improvements to HDRP material export support (#826)
- change: API cleanup for Interactivity node export
- change: removed legacy `GLTFSettings.requireExtensions` option that wasn't really used

## [2.16.0-pre] - 2025-04-02

- add: Editor export support for [`KHR_interactivity`](https://github.com/KhronosGroup/glTF/blob/220ca407a2ce1f8463855803778edf73a885b7e9/extensions/2.0/Khronos/KHR_interactivity/Specification.adoc), [`KHR_node_hoverability`](https://github.com/KhronosGroup/glTF/pull/2426) and [`KHR_node_selectability`](https://github.com/KhronosGroup/glTF/pull/2422).Support for these extensions is based on the draft specification as of Mar 10 2024. Please note that these extensions are not yet ratified and implementation details may change.
- add: convert `Unity Visual Scripting` Units to `KHR_interactivity` nodes on export, including virtual properties from the [`glTF Object Model`](https://github.com/KhronosGroup/glTF/blob/main/specification/2.0/ObjectModel.adoc). 
- add: visual hints for which nodes can be exported from Visual Scripting to `KHR_interactivity`
- add: `KHR_interactivity` export plugin.
  - This plugin is disabled by default. Enable it in `UnityGLTFSettings`, add a Script Machine component to an object, and author the logic there.
  - Currently, you can find some samples for interactive scenes here: https://github.com/needle-tools/UnityGLTF-Interactivity-Sample-Assets
- add: batch export mode is now an option when exporting via the UnityGLTF menu items. This allows exporting many scene objects, prefabs, or scenes as individual or merged files.
- fix: Remove duplicate `GLTFLoadException` type
- fix: added `AlphaToMask` and `BlendModePreserveSpecular` float values to material setup and material mappers. This works around a Unity issue where some runtime-created materials have incorrect keywords until refresh of their keywords.
- fix: Removed synchronous wait in GLB json loading causing spikes
- change: GLTFComponent: changed `loadOnStart` from private to public
- fix: added `TextMeshPro.ForceMeshUpdate` to TMP exporter plugin to ensure exports of scenes immediately after loading have correct mesh data for 3D text
- fix: selecting multiple scenes and exporting via the menu now exports each selected scene as individual file. Previously, it would export the first selected scene only.
- fix: exporting meshes with zero materials was throwing an exception, now correctly does not export the mesh

## [2.15.0] - 2025-03-06

- fix: assets with absolute URLs or paths containing URL encoded information now load correctly
- fix: improve shader dependencies in editor importer to work around some edge cases at first library import
- fix: check all sub-meshes of used attributes instead of only the first one when creating attribute arrays
- fix: ensure progress callback is triggered before `onLoadComplete` in scene loader
- fix: prevent null reference exception when plugin is not loaded/defined
- fix: work around API change in Unity 6000.0.38f1 that caused compilation error
- fix: clamped `Sheen Roughness` to 0..1 range in PBRGraph shader
- fix: `RoughRefractionFeature` now supports Unity 6 and new Render Graph API
- fix: set volume and transmission materials to `AlphaMode.BLEND` on VisionOS to ensure proper rendering
- fix: update render pipeline-related methods for Unity 6000.0+
- change: mark package as compatible with Unity 2021.3+. Legacy support for 2020 and earlier will be removed in future updates.
- change: renamed `GLTFComponent` setting `AppendStreamingAssets` to `LoadFromStreamingAssetsFolder`
- add: MaterialX branches to `UnityGLTF/PBRGraph` shader with conditional compilation for VisionOS
- add: `MaterialXColor` and `MaterialXFloat` shadersubgraphs for platform-specific material settings
- add: new runtime texture compression option (`None`, `LowQuality`, `HighQuality`) to improve runtime memory usage
- add: exposed import settings to keep CPU copy of mesh/textures to `GLTFComponent`
- add: include UnityGLTF package version, Unity version and current render pipeline in `assets.extras` on export
- add: better code snippets for the readme
- add: export plugin hook for `ShouldNodeExport` (https://github.com/KhronosGroup/UnityGLTF/pull/767)
- add: [`KHR_node_visibility`](https://github.com/KhronosGroup/glTF/pull/2410) import and export support, currently disabled by default. Enable in `UnityGLTFSettings`. This is a preparative step for `KHR_interactivity`, which will be added in a later release. Please note that this extension is not yet ratified and implementation details may change.
- add: schema and serialization support for [`KHR_node_hoverability`](https://github.com/KhronosGroup/glTF/pull/2426) and [`KHR_node_selectability`](https://github.com/KhronosGroup/glTF/pull/2422). Please note that these extensions are not yet ratified and implementation details may change.

## [2.14.1] - 2024-10-28

- fix: compiler error with `ParticleSystemBakeMeshOptions` before 2022.3.11f1 since that's where the API was introduced
- fix: prevent incorrect warning when meshes don't have UV1/UV2 attributes
- fix: don't call export multiple times from context menu for multi-selections
- fix: remove unused property from `GLTFComponent`

## [2.14.0] - 2024-10-06

- fix: potential NullReferenceExceptions when importing material-only, mesh-only or texture-only glTF files
- fix: check for missing primitives on mesh import instead of throwing
- fix: GLTFSceneImporter reference counting properly tracks Animation data now
- fix: incorrect callback subscription in glTF Material editing
- fix: CanvasExport plugin was not working correctly in WebGL builds
- fix: rare case of incorrect texture export with invalid texture content hash (thanks @Vaso64)
- fix: ensure materials created with Create > UnityGLTF > Material use UnityGLTF as importer instead of glTFast
- fix: Canvas export plugin was not correctly updating the canvas mesh in builds
- fix: Particle Bake export plugin was not working correctly in 2022.3
- fix: Import/Export plugin enabled/disabled state was not correctly serialized in some cases
- fix: TMPro export plugin uses UnityGLTF shaders for export now instead of adding another dependency
- fix: GLTFSceneImporter can be called without external data loader, but will warn that external data will not be loaded. Previously, a data loader had to be added even when it was not needed.
- change: log warning when exporting UV0 and UV1 with more than 2 components, as glTF only supports 2-component UVs
- change: simplify sampler usage in PBRGraph where possible to prevent warnings in later Unity versions
- change: simplify PBRGraph variants to reduce shader variant count. This removes a separate option to use vertex color.
- change: material-only files are now imported as MaterialLibrary assets with Material sub-assets, even when only a single material is present
- add: log more meaningful exception messages on import
- add: sheen support for PBRGraph
- add: PBRGraph UI properly shows sheen and dispersion properties
- add: allow flipping humanoid root bone on import to support more avatar types (like Meta Avatars)
- add: new "Info" tab in GLTFImporter for asset information (generator, copyright, etc.) about the imported file
- add: ShaderOverride is now public on GLTFComponent
- add: new GLTFSceneImporter constructor overload for easier loading of files from streams
- add: meshes and textures can now be deduplicated on import, since many exporters don't properly instance them
- add: UV2 is exported as 2-, 3-, or 4-component texture coordinate. 3- and 4-component texture coordinates are not in the core glTF spec, but widely supported by implementations.
- add: complete PBRGraph and UnlitGraph Shader Variant Collections for easier runtime import and export of glTF files
- add: options for shader pass stripping in builds to reduce variant count and compilation times
- add: ability to export a set of materials as material-only glTF or glb file
- add: option to add new materials to glTF Material Libraries (material-only files)

## [2.13.0] - 2024-07-23

- fix: empty or invalid root transforms should not be exported
- fix: when no root transforms are exported, no scene should be created by default
- fix: image filenames were sometimes incorrectly exported when exporting .gltf files (#737)
- fix: set wrap mode for legacy animation import mode
- fix: added missing ExtTransform for some textures in animationpointer import/export, fixes when only offset exist (#740)
- fix: added uv-starts-at-top check for transmission support on WebGL and other platforms (#746)
- fix: added missing generate mitmaps to ktx texture load (#752)
- fix: bone name mapping for exact names was not applied on humanoid import (#751)
- fix: added missing using system for uwp target (#757)
- add: allow importing non-standard VEC3 and VEC4 TEXCOORD_n data and convert it to VEC2
- add: exposed texture readwrite enabled and generate mipmaps option to importer
- change: removed log for loaded node count mismatch, extensions can modify node counts so the log was misleading
- change: removed loaded texture count error log: when multiple samplers are used for one texture, the textures will be duplicated

## [2.12.0] - 2024-05-06

- fix: PolySpatial support now also includes emission textures and colors
  - Unity bug IN-72885 is fixed in PolySpatial 1.2.3+, please update
- fix: avoid overwriting texture files with identical names on glTF export
- fix: avoid NullReferenceException when read values from array are not a number (invalid glTF but encountered in the wild)
- fix: rough refraction feature sometimes had incorrect cameraColorTarget as source
- fix: better math for index of refraction – improves visual correctness of transmission, volume, dispersion
- fix: stripping empty roots now respects when some of those roots are animated and doesn't strip them
- fix: import of default unspecified materials was sometimes not handled correctly
- fix: incorrect duplicated texture tiling regression due to PolySpatial support changes
- add: [KHR_animation_pointer]() import support. Export has been supported for a couple years.
- add: [KHR_materials_dispersion](https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_dispersion) import, export, and shader support

## [2.11.0-rc] - 2024-04-14

- fix: correct disposal of toktx textures after API change in upstream dependency (#710)
- fix: incorrect blendshape tangents when using sparse accessor export
- fix: wrong texture import offset when importing KTX2 textures without texture transforms (#709)
- fix: use of draco with tangents and creating the draco attribute map with all the other mesh attributes (#726)
- fix: export of multiple instances of the same skinned mesh not reusing attributes correctly
- fix: extra memory usage due to unnecessary duplication of the first imported instance of a texture (#713)
- fix: one case of default texture import filter mode being set to TriLinear instead of Bilinear
- change: outdated warning about occlusion textures not being on the UV1 channel (support has improved)
- add: visual indicator which extensions are required for a given file when the file has import errors
- add: support for humanoid import for Meta avatar bone hierarchies
- add: initial support for PolySpatial/MaterialX conversion for `PBRGraph` and `UnlitGraph` (#725)
  - due to a Unity bug (IN-72885) emission colors and textures are currently not working correctly
- add: Camera import support (#706)

## [2.10.2-rc] - 2024-03-21
- fix: Exception caused by animation targeting a missing material

## [2.10.1-rc] - 2024-03-11
- add: `OnAfterMeshExport` callback hook which allows adding extensions to the exported gltf mesh

## [2.10.0-rc.2] - 2024-03-05
- fix: add `com.unity.mathematics` as required dependency
- fix: added null check in mesh data preparation code to prevent exception for files without nodes (e.g. just materials)

## [2.10.0-rc] - 2024-03-04
- fix: GLTFRecorder issue where resulting animation would have linear interpolation for cases where a jump was expected
- fix: GLTFSettings toolbar active index correctly stored in session now
- fix: don't export empty buffers with length 0
- fix: check for valid Humanoid avatar before export (#681)
- fix: work around SRP issue with invalid camera data in render passes affecting rough refractions
- fix: issue when morph targets have varying normals and tangents data (#682)
- fix: prevent exception in earlier 2022.x versions with `isDataSRGB` not being available
- fix: missing normalization checks for quantized accessor data (#693)
- fix: make sure topology are triangles for calculating normals/tangents (#133)
- fix: KTX2 textures were not checking for linear for "Fix All" importer button
- fix: MAOS maps (combined metallic/ambient occlusion/roughness) were not exported correctly
- fix: wrong accessor `UBYTE` > `BYTE` and `BYTE` > `SBYTE` conversion when reading data in some 
- fix: restore multithreading support and improve performance
- feat: import plugin for `EXT_mesh_gpu_instancing` extension
- feat: added blend shape frame weight import option for easier animation retargeting
- feat: show failing filenames more clearly when exceptions occur during import
- feat: add option to hide scene obj during loading in `GLTFComponent`
- feat: add import support for glTF `LineLoop`, `TriangleStrip`, `TriangleFan` topologies
- feat: performance improvements in name resolution for importing files with many nodes
- feat: performance improvements by using `NativeArray` and `Mathematics` types 

## [2.9.1-rc] - 2024-02-16
- fix: spritesheet animation keyframes should be constant
- change: log warning if spritesheet used for animation contains only one image/sprite (currently all images must be part of the same spritesheet)

## [2.9.0-rc] - 2024-01-23
### Release Candidate
UnityGLTF has been maintained in a fork since the end of 2019.  
Hundreds of fixes have been made, numerous features have been added, and the library has been brought up-to-speed on latest glTF developments,
modern material extensions, and Unity versions. We're happy to bring these changes back to the main repository so everyone can benefit.  

UnityGLTF has turned into an extremely versatile glTF exporter _and_ importer, which excellent support for data roundtrips and glTF-first workflows in Unity.  

Please [open an issue](https://github.com/KhronosGroup/UnityGLTF/issues) if you find any problems with the release candidate.   

- change: Readme updates with links changed back to KhronosGroup/UnityGLTF
- change: Mark as release candidate

## [2.8.1-exp] - 2024-01-18
- fix: tangent recalculation was not working when importing draco meshes without tangents
- fix: DXT5nm conversion for non-readable textures
- fix: color space when loading KTX2 normals was incorrect
- fix: tiling and offset properties were displayed even when TEXTURE_TRANSFORMS was disabled on 2022.3+
- fix: nullref in texture format validation when texture is missing
- fix: ifdefs to support Draco package 5.x from Unity Registry (`com.unity.cloud.draco`)
- add: show UI for adding/removing optional compression packages in GLTFSettings

## [2.8.0-exp] - 2024-01-17
- fix: bone weights were not properly imported from Draco compressed meshes due to bug in Unity's `CombineMeshes`
- fix: data loader was preventing multi-threaded imports from working
- fix: compilation error when `TMPro` package is not present
- fix: TMPro detection when `com.unity.ugui@2.0.0` is present which has TMPro embedded
- fix: normal map color space was wrong on non-standalone target platforms when normal import settings is set to DXT5nm, which is default on Unity 2021.3+ on some platforms
- fix: normal maps were not marked as Normal on editor import which is required for DXT5nm support
- fix: normal maps imported at runtime now set a `_NormalMapFormatXYZ` flag on their materials to ensure correct display
- fix: assets were not reimported when normal map setting changed between XYZ and DXT5nm (requires domain reload)
- fix: prepare for changed package declarations due to draco/ktx packages moving registries
- fix: GLTFRecorder should respect specified `UseAnimationPointer` setting
- fix: warn after editor import when textures on disk have incorrect linear/normal settings
- change: incorrectly named PBRGraph material option `_AutoSurfaceMode` is now called `_OverrideSurfaceMode`
- change: display texture settings warning above tabbed inspector for better visibility

## [2.7.1-exp] - 2024-01-08
- fix: default property deserializer was missing for nested extras objects in `MeshPrimitive`
- fix potential `ImportContext` NullRef
- change: move blend shape target names to mesh, according to https://github.com/KhronosGroup/glTF/pull/1631
- add: selection export options are also in the GameObject menu now for right-click > export support

## [2.7.0-exp] - 2024-01-03
- fix: import scale was not applied to position animation curves
- fix: make sure `GLTFImporter` uses the default plugin import settings
- change: refactored import/export plugins for better control of what's enabled and what's not. This allows shipping experimental/optional plugins earlier.
- change: mark `GLTFSceneExporter.*` static callbacks obsolete. Use plugins instead
- add: allow overrides for which editor import plugins are used
- add: export plugin for `MSFT_lod`
- add: export plugin for `KHR_materials_variants`. Add the `MaterialVariants` component to your root to configure variants.
- add: export plugin to bake particle systems to meshes
- add: export plugin to bake canvas to meshes
- add: export plugin to bake TMPro GameObjects to meshes
- add: per-import settings for which plugins are applied
- add: `BeforeNodeExport` callback for export plugins
- add: warning icon for plugins that e.g. have missing package dependencies

## [2.6.0-exp] - 2023-12-13
- fix: verify tangent.w component on animation export (should be exactly -1 or 1)
- fix: recalculate mesh bounds when changing import scale
- fix: ensure correct quaternion continuity on animation import
- fix: sanitize Animator state names on import
- add: allow enabling GPU Instancing for materials on editor import
- add: ability to export files and buffer views, useful for e.g. KHR_audio

## [2.5.2-exp] - 2023-11-13
- fix: animation curve sorting running before validation
- add: support to animate camera background color

## [2.5.1-exp] - 2023-11-08
- fix: issue where importer context root object was not set
- fix: wrong flipped triangles when it's required to generate them ("Fox"-Test model) + fixed wrong imported vertex data on submeshes
- fix: draco ifdef compiler error
- fix: import reuse joints and weights for submeshes
- fix: import normals when tangents are required
- fix: humanoid importer inspector not being shown when model doesn't have animation data
- fix: animation export for identical clip+node
- fix: exporting glTF with external EXR texture falsely being encoded as PNG
- change: importer exposes node- and mesh-cache

## [2.5.0-exp] - 2023-10-20
- fix: default dataloader is now UnityWebRequestLoader
- fix: importing animations at runtime did not work in specific settings combinations
- fix: create default import context when importing files at runtime
- fix: meshes without submeshes but multiple materials behave the same as in Unity now (results in multiple draw calls)
- change: remove animation bounds nodes from GLTFRecorder again, can be added back via callbacks if needed
- feat: add option to import blend shape names or defaults
- feat: add OnBeforeAddAnimationData callback to GLTFRecorder
- feat: expose ExportAccessor(byte[]) and configurable overload for easier arbitrary data export

## [2.4.2-exp] - 2023-09-28
- change: always export root level objects / objects explicitly requested to be exported

## [2.4.1-exp] - 2023-09-11
- fix: GPU Instancing option was missing in 2021.x
- fix: Prevent ArgumentException when animating same property name on multiple components on the same GameObject
- change: Make glTF material editing opt-in per material
- add: glb textures can now be compressed to platform-default settings at import

## [2.4.0-exp] - 2023-09-08
- fix: improvements to light color and value export
- fix: light import color space
- fix: sanitize alpha cutoff value before writing
- fix: convert light and material color spaces when exporting for animation
- add: allow choice between implicit and explicit queue and surface type control (experimental)

## [2.3.1-exp] - 2023-08-26
- fix: restore ShouldExportTransform functionality on latest dev branch

## [2.3.0-exp] - 2023-08-26
- fix: removed incorrect smoothness property from PBRGraph
- fix: wrong file extension when extracting textures (was ".mat", now ".asset")
- fix: ensure default glTF sampling settings on loaded textures (in case there is no sampling information in the gltf)
- change: warn when importing legacy KHR_materials_pbrSpecularGlossiness extension
- change: remove obsolete GLTFSceneImporter constructors
- change: adjusted repository structure for modern package formats
- change: removed legacy test files and legacy samples that were used for testing (tests should come back at a later point)
- change: removed legacy folders from repository
- add: gltf filename log output when textures or extensions can't be imported

## [2.2.0-exp] - 2023-08-16
- fix: serialize/deserialize ExtTextureTransform for textures in material extensions
- fix: compiler warnings in unity 2023.1
- fix: animation interpolation on import (unwanted smoothing of animation curves)
- fix: blendshapes with sparse accessors on import (results in distorted meshes)
- fix: quantize decoding (wrong data type conversion)
- fix: when exporting baked humanoid animation data with KHR_animation_pointer enabled, additional tracks were created for the already baked animation
- fix: relative paths containing ".." were not resolved correctly when importing in the Editor
- change: add external images to import dependencies (was conflicting with the ktx2 importer)
- change: import non referenced materials and textures (Use GLTFSceneImporter.LoadUnreferencedImagesAndMaterials to enable it) - In editor import: default is true, runtime: false
- change: don't export submeshes with 0 vertices
- change: RoughRefractionFeature (RenderFeature) to support 2023.1
- add: uv channel support for export
- add: textures without names on import get a temp. name (will get removed on export again)
- add: clearcoat support (Normals are still not supported in pbrgraph shader)
- add: log error when trying to load exr textures (not supported)
- add: per texture transforms in pbrgraph (uv, rotation, scale/offset)
- add: stream length check in LoadBufferView, to prevent infinite loop when trying to read more data
- add: new import options in Gltf loading at runtime: Normals, Tangents, SwapUVs (see ImportOptions)
- add: meshOpt import support (requires package: com.unity.meshopt.decompress)

## [2.1.2-exp] - 2023-08-03
- fix: export of humanoid animations where both transforms as well as blendshapes are animated

## [2.1.1-exp] - 2023-07-31
- fix: export texture transform for metallicRoughness

## [2.1.0-exp] - 2023-06-19
- add: draco import support (requires package: com.atteneder.draco)
- add: KTX2 import support (requires package: com.atteneder.ktx)

## [2.0.4-exp] - 2023-06-02
- fix: sampled animations need individual GLTFAnimations since sampling another avatar is not transferrable
- fix: switching from URP/Lit or Standard to PBRGraph should set cutoff correctly so it matches the UI / shader expectations (#68)
- fix: conversion with _EMISSION keyword off was still setting emissive values (#86)
- fix: typo in TMPro shader (#83)
- fix: animated property validation was incorrect for 3-component color values and camera properties

## [2.0.3-exp] - 2023-05-15
- fix: import path retargeting exception in some cases
- fix: Range shader property type was missing from propery validation
- feat: allow changing GLTFRecorderClip animation output name

## [2.0.2-exp] - 2023-05-08
- remove: unnecessary forced texture transform setting
- fix: make sure default animation state is first in clip list on export
- fix: regarget animation clip paths when stripping empty hierarchy nodes
- fix: watch current render pipeline for changes so that we can trigger reimports
- fix: texture_ST properties aren't properly saved since ShaderGraph marks the textures as [NoScaleOffset} (IN-16486)
- fix: ArgumentException when animated property doesnt exist anymore on assigned shader where the PropertyType can not be resolved then

## [2.0.1-exp] - 2023-05-05
- add: sorting of animation clips
- change: dont export animation if expected channel count is not met (e.g. Vector2 has only one channel)
- fix: compilation warning
- fix: extracting multiple materials would throw error in serialized object access
- fix: "Calculate Mikktspace" tangents can be NaN in some cases
- internal: remove GradientSkybox shader from tests

## [2.0.0-exp.2] - 2023-04-20
- fix: fresh project reimport was breaking ImportPlugin API
- fix: editor import in 2021.x, added ShaderGraph dependency now
- fix: blendshape frame weight was incorrectly calculated on export
- fix: player compilation
- change: bump Unity dependency to 2020.3

## [2.0.0-exp] - 2023-04-18
- add: ImportPlugin API with ``GltfImportPlugin`` and ``GltfImportPluginContext`` for receiving callbacks during import

## [1.24.1-pre] - 2023-04-17
- add: option to disable baking of AnimatorState speed value into animationclip
- fix: doublesided import when shader already had that set explicitly to false

## [1.24.0-pre.3] - 2023-03-31
- fix: sampling animation rigging wasn't working anymore
- fix: added warning about obsolete code and what to do
- fix: exposed `GLTFImporterHelper.TextureImportSettingsAreCorrect` and `GLTFImporterHelper.FixTextureImportSettings` again

## [1.24.0-pre.2] - 2023-03-28
- fix: nullref when importing files without animation in the editor

## [1.24.0-pre] - 2023-03-27
- feat: allow Mecanim Humanoid import in Editor with Avatar creation
- feat: better structure for glTF importer with tabs
- fix: don't try to generate UV coordinates for non-triangle meshes
- fix: don't attempt to recalculate normals/tangents for non-triangle meshes
- fix: wrap material extraction in StartEditing/StopEditing calls (fixes #81)
- fix: imports that failed on first try (e.g. missing textures, other errors) would keep using the old asset identifier instead of defaulting to the new one
- fix: runtime recording of SkinnedMeshRenderers without blend shapes was failing in some cases (fixes #80)
- fix: disabling Volume from PBRGraph would still use volume values
- fix: imported animation blendshape frame weights were not roundtripping well
- fix: some animations not imported in Mecanim mode
- fix: compilation issues with ShaderGraph package not present

## [1.23.1-pre] - 2023-03-15
- fix: ExportMesh not exporting submeshes in some cases
- fix: missing export mesh marker

## [1.23.0-pre] - 2023-03-03
- fix: revert other possible modifications when recording animations
- fix: exporting animation curves failed for blendshape animations on missing targets
- fix: better info for which object has missing curves on animation export
- feat: transparent/double-sided materials can now be upgraded from 2020.x to 2021.x+

## [1.22.4-pre] - 2023-02-25
- fix: revert prefab modifications when recording humanoid animation from prefab assets to work around AnimationMode limitations
- fix: creation of duplicated keyframe when there is only one keyframe
- change: clarify log for rotation animation export with wrong number of curves

## [1.22.3-pre] - 2023-02-09
- fix: specular extension factor roundtrip was incorrect
- fix: MSFT_lods import created the wrong hierarchy and the culling option wasn't working
- fix: edge case in node/mesh imports in extensions that could lead to a stack overflow
- fix: calling `GLTFImporterInspector.FixTextureImportSettings` could result in an infite loop when called from an AssetPostprocessor

## [1.22.2-pre] - 2023-02-04
- fix: set morph target names from mesh extras on import (#70, thanks @emperorofmars)

## [1.22.1-pre] - 2023-02-04
- fix: broken texture references prevented files from loading entirely
- fix: .bin file was not properly registered as dependency for the imported .gltf asset
- fix: shader issue with instance ID transfer on 2021.x (#71, thanks @Jerem-35)
- fix: normal map colorspace was wrong on Mac in some cases (#74, thanks @robertlong)
- fix: ORM maps were exported with empty/unneeded alpha channel (#77, thanks @robertlong)

## [1.22.0-pre] - 2023-02-01
- feat: expose ExportNode API
- feat: optionally calculate and place bounds markers in GltfRecorder for viewers that don't caclulate bounds from animated skinned meshes properly (experimental)

## [1.21.1-pre] - 2023-01-20
- fix: animator being disabled after exporting humanoid animationclip
- fix: ignore MotionT and MotionQ on animator, can't be resolved for KHR_animation, seems to be a magic unity name
- fix: build error by accessing imageContentHash
- change: export HDR textures with zip compression by default

## [1.21.0-pre] - 2023-01-14
- feat: allow aborting export when not in Play Mode and meshes are not readable - seems to be a random Unity synchronization context issue
- fix: don't export unsupported light types (e.g. area light has type "rectangle" which is not supported in glTF)
- fix: Export human motion translation
- fix: prevent exporting the same baked humanoid clip for different avatars, needs individual clips since we're baking them (not retargeting at runtime)
- fix: reusing animation clips between objects with different hierarchies caused some targets to be missing, depending on export order
- change: remove warning for KHR_animation_pointer resolving when the unresolved object is a transform, that's part of core

## [1.20.3-pre] - 2023-01-13
- fix: wrong texture name in Texture Transform check
- fix: better check if a texture is a normal map and needs the right import settings
- fix: uris with escaped characters didn't correctly import in the editor

## [1.20.2-pre] - 2023-01-12
- fix: export of color animations where only one channel is animated
- fix: order of animationcurve properties when exporing animated colors in component where e.g. a user started by animating a single channel first (e.g. alpha) and later added keyframes for other channels

## [1.20.1-pre] - 2023-01-10
- fix: issue with exporting shared texture samplers in some cases
- fix: weights on skinned meshes shouldn't be resolved by custom KHR_animation_pointer resolvers as they're part of the core spec
- change: remove logs when caching data
- change: move cache clear button into settings
- add: KHR_materials_clearcoat roundtrip support (no in-editor visualization yet)

## [1.20.0-pre] - 2023-01-04
- add: caching of texture bytes on disc for faster export (can be disabled in UnityGLTFSettings)

## [1.19.1-pre] - 2023-01-03
- add: AfterPrimitiveExport event

## [1.19.0-pre] - 2022-12-14
- add: spritesheet keyframe animation export
- change: print warning when animation pointer cant be resolved and add filtering of objects that cant be resolved before building animation data
- fix: index discrepancy between _exportedMaterials and _root.Materials lead to wrong texture indices being returned in some cases
- fix: don't write and declare IOR when it's at the default value
- fix: importing textures with names resulted in those names not being used (only image names were used)
- fix: importing files without baseMap but using texture transforms for normal or emissive would result in texture transforms not being used

## [1.18.5-pre] - 2022-12-02
- fix: default material was missing in build

## [1.18.4-pre] - 2022-12-02
- fix: GLTFRecorder didn't properly record with animation pointer off anymore
- fix: unified access to SkinnedMeshRenderer weights on export
- fix: nullrefs in ExportPlan with missing SkinnedMeshRenderer
- feat: allow passing custom settings into GLTFRecorder

## [1.18.3-pre] - 2022-11-14
- fix: disabled MeshRenderers and SkinnedMeshRenderers were not exported despite ExportDisabledGameObjects being on
- fix: sample root transform as well in Humanoid export to prevent shifting it around when recording

## [1.18.2-pre] - 2022-11-10
- fix: exception thrown when trying to add a material instance id twice

## [1.18.1-pre] - 2022-11-09
- fix: ExportMeshes was not exporting new meshes since the internal material check didn't let it through

## [1.18.0-pre] - 2022-11-07
- fix: blend shape weight animation wasn't properly exported in some cases
- fix: IOR extension may end up as null when IOR was animated
- fix: rough refracton LOD access is now affected by alpha blending
- feat: HDR render textures can be exported now
- feat: add ExportMesh(Mesh) API as convenience helper
- feat: add material remapping to glTF/GLB importer
- chore: code cleanup for rough refraction and material inspector
- change: animation export code paths have been consolidated, less differences between KHR_animation_pointer and regular export
- change: bumped min Unity version to 2019.4

## [1.17.2-pre] - 2022-10-21
- fix: set linear export setting before accessing texture exporter settings for unknown textures

## [1.17.1-pre] - 2022-10-19
- fix: glb export now using utf8 (no BOM)
- fix: simplify and fix normals export

## [1.17.0-pre] - 2022-10-12
- add: import options for animation loop and re-enable by default for Mecanim
- fix: reimport assets when colorspace changes
- fix: texture map type needs to be passed into UniqueTexture, otherwise textures used for different things don't get exported correctly
- fix: bake smoothness values into roughness map if there's no good conversion
- fix: alpha cutout export on BiRP
- fix: base color warning should only be shown for transparent objects
- fix: improve alpha cutout inspector for 2020.3
- fix: smoothness was inverted / removed even if no smoothness map existed
- change: refactor TextureMapType to contain options for conversion to clean up export and introduce ability to convert smoothness
- change: refactor textureSlots, usage of textureSlot names and how they should be called

## [1.16.3-pre] - 2022-10-06
- fix: KHR_animation_pointer bug with re-used animations targeting properties on nodes

## [1.16.2-pre] - 2022-09-30
- fix: blend shapes and blend shape animations were not imported (note: sparse accessor import not working yet)
- fix: allow doublesided and transparent import on 2020.3 URP with PBRGraph
- fix: make RegisterPrimitivesWithNode API public, belongs to ExportMesh
- fix: UnlitGraph used wrong texture transform in some cases
- fix: BiRP texture import on legacy shaders in < 2020.3 had flipped texture transforms
- fix: ExporterMaterials compilation issue on 2018.x
- fix: some TMPro materials were exported incorrectly

## [1.16.1-pre] - 2022-09-25
- fix: first-time import was failing to find shaders on 2020.x in some cases
- fix: TMPro texture conversion one export was blurry at the bottom of the texture
- fix: transmission/volume mat was incorrectly imported
- fix: transparency was incorrectly imported on 2020.x
- fix: material validation on 2021.x wasn't properly turning transparency on for some imports
- fix: no compilation errors on 2018.4 + 2019.4

## [1.16.0-pre] - 2022-09-21
- fix: checking materials for _ST shader properties was failing on specific Unity versions
- fix: AnimationPointerResolver was warning in cases that are allowed / no warning needed
- fix: build compilation issues on 2022+
- fix: default scene name differed from glTFast, which broke switching importers. Both use "Scene" now
- fix: import warnings for metallicRoughness texture swizzling only print when a metallicRoughness texture is actually used
- feat: tangents are recalculated on import now
- feat: TMPro meshes export with baked texture now instead of SDF texture
- feat: added AfterTextureExportDelegate/BeforeTextureExportDelegate and UniqueTexture hash for modifying textures on export
- change: explicit bool option to turn KHR_materials_volume export on and off
- change: removed outdated samples from package
- change: if a mesh in the glTF doesn't specify a name it will import without name now instead of using a default name

## [1.15.0-pre] - 2022-09-13
- fix: workaround for ShaderGraph bug on 2021.2+ that breaks defining baseColorTexture_ST manually
- fix: workaround for Unity regression in 2022.1+ where checking material properties returns wrong results in some cases
- feat: allow exporting of Humanoid animation clips (get baked to generic)
- change: texture aniso values >= 1 now result in LinearMipmapLinear filtering to match visual result in Unity better

## [1.14-0-pre] - 2022-09-11
- fix: log error instead of exception for missing textures on export
- feat: allow referencing GameObjects for `KHR_animation_pointer`
- change: animated fields with `KHR_animation_pointer` now try to find their correct Unity properties instead of the serialized field (m_ prefix disappears)

## [1.13.0-pre] - 2022-09-05
- add: preliminary HDR texture export in EXT_texture_exr extension
- fix: Unity Editor module related issues
- fix: wrongly exporting two keyframes for animations with only one keyframe
- fix: children of lights and cameras were inverted

## [1.12.2-pre] - 2022-08-29
- fix: KHR_animation_pointer export where member is declared on base type

## [1.12.1-pre] - 2022-08-23
- fix: nullref in export of missing mesh

## [1.12.0-pre] - 2022-08-23
- fix: emissive color alpha was set to 1 in some roundtrip cases when it should have been 0
- fix: nullref in import when glTF had null textures (against the spec, but other viewers tolerate it)
- fix: unlit double sided was incorrectly imported in BiRP
- fix: PBRGraphUI didn't properly draw infos for SkinnedMeshRenderer
- fix: added safeguards against Shader.Find not working in first imports (fixes #51)
- fix: textures without mipmaps should export closer to intended now
- fix: missing pbrMetallicRoughness property was treated incorrectly
- fix: BeforeSceneExport was missing from gltf+bin exports
- feat: warn in PBRGraphGUI when UV0 isn't present (fixes #52)
- feat: texture channel swizzling on URP/Lit > PBRGraph material conversion in 2022.1+
- feat: expose ExportMesh API using UniquePrimitive array to export arbitrary meshes from extensions (#55, thanks @robertlong)
- feat: GLTFSettings can now be passed into GLTFSceneExporter directly, falls back to project settings if none are provided
- remove: public settings API on GLTFSceneExporter is now gone. Pass in custom settings via ExportOptions if needed.

## [1.11.0-pre] - 2022-07-27
- fix: converting PBRGraph and UnlitGraph to each other shouldn't warn
- fix: multiple animators referencing the same clip exported animations incorrectly with KHR_animation_pointer on
- fix: implemented partial animation target removal when some bindings animate missing objects
- change: asset identifier for imported assets is now an explicit option
- change: removed compilation flag to use new asset identifier, use the explicit option instead

## [1.10.1-pre] - 2022-07-18
- fix: accessor submesh primtives were incorrectly assigned after internal duplication
- fix: don't add import dependency on shaders, load them by GUID instead
- fix: URI-escaped file names weren't always resolved correctly
- fix: some KHR_animation_pointer export type fixes (vec3 colors vs. vec4 colors)
- feat: add color helpers to PBRGraphGUI
- feat: add UnlitGraph for 2021.2+
- feat: new API, GetPrimitivesForMesh to add extensions to exported meshes (e.g. KHR_materials_variants)

## [1.10.0-pre] - 2022-07-06
- fix: PBRGraph assignable again from shader dropdown
- fix: various KHR_animation_pointer fixes to extension usage and property names
- fix: compilation fixes for Unity 2018/2019
- feat: add helper, callbacks and script generator for shader conversion
- change: Importer/Exporter are now multiple partials
- change: change GetAnimationTargetIdFrom[..] to Get[..]Index to clarify what it does

## [1.9.0-pre] - 2022-06-22
- fix: properly set BiRP ShaderGraph transparency keywords
- feat: PBRGraph property names now match glTF names directly (breaking change from 1.7.0+)
- feat: PBRGraph now has a custom shader GUI that also validates keywords

## [1.8.1-pre] - 2022-06-22
- fixed: same animation used on different objects should result in different pointer values with KHR_animation_pointer used
- fixed: nullref when adding animation data for null object (e.g. unused property clip)
- fixed: too many nodes get overwritten by duplicate animations with animation pointer
- changed: GetAnimationId now takes root transform parameter
- fixed: previously seen KHR_animation_pointer is now added to resolve list (e.g. when using animations on multiple objects and animating component values)

## [1.8.0-pre.2] - 2022-06-14
- fixed: OcclusionTexture tiling now defaults to (1,1)
- fixed: build errors on certain platforms

## [1.8.0-pre] - 2022-06-10
- fixed: fixed package version, color space changes require minor version bump, not just patch
- fixed: C# version error on 2020.3

## [1.7.1-pre.3] - 2022-06-10
- feat: show extensions and textures in importer inspector
- feat: export texture coord (UV0 / UV1) separately for "occlusionTexture" and "everything else", same as three.js
- fixed: editor import of .gltf files with textures now uses those textures directly instead of creating new ones
- fixed: exporting only includes extensions that are actually used or explicitly enabled
- fixed: no more differences in color space between exporting glTF + textures or glb with embedded textures
- fixed: importer properly declares shaders as dependencies, fixes library reimport errors

## [1.7.1-pre.2] - 2022-06-08
- fixed: animation clip import in Editor working again

## [1.7.1-pre] - 2022-06-07
- feat: sparse accessors import (partial)
- feat: UV rotation import/export (for baseColorTexture only right now)
- fixed: GLTFRecorderComponent error in play mode when only new input system is present
- fixed: animation export with KHR_animation_pointer and reused nodes
- fixed: passing invalid file names to Export could result in wrong buffer paths in JSON

## [1.7.0-pre] - 2022-06-01
- feat: experimental support for KHR_animation_pointer
- feat: experimental URP (2020.3+) & BiRP (2021.2+) Shader Graph for export and import, `UnityGLTF/PBRGraph`
- feat: approximated support for exporting and importing KHR_materials_transmission, KHR_materials_volume, KHR_materials_ior, KHR_materials_iridescence, best used with `UnityGLTF/PBRGraph`
- feat: renderer features and post effect for rough refraction / transmission (for URP and BiRP)
- fixed: light and camera directions were flipped when animated
- fixed: normal textures were exported with wrong color space in .gltf
- fixed: WebRequestLoader edge case with relative paths
- improved: better heuristic for PBR material export and generally better glTF-related material property export

## [1.6.1-pre.3] - 2022-05-10
- feat: allow replacing logger for GLTFSceneExporter with a custom one, allows to reduce number of logs
- feat: added more ProfilerMarkers
- removed: removed submodules from repository to make usage as submodule in other projects easier
- fixed: bad performance in GLTFRecorder when recording lots of animation and/or Blendshape weights
- fixed: some mismatched ProfilerMarker.Begin/End calls
- fixed: less allocations when writing accessors

## [1.6.1-pre.2] - 2022-05-06
- feat: allow recording root object in worldspace in GLTFRecorder

## [1.6.1-pre] - 2022-05-06
- feat: added experimental support for KHR_animation_pointer in-editor animation export (for select properties), can be turned on in `ProjectSettings/UnityGltf`
- feat: added scene export as GLB (fixes #22)
- fixed: roundtrip issues with glTFast when alpha testing is used
- fixed: no build errors in Samples anymore
- fixed: allow exporting skinned mesh animations even when the mesh isn't readable (bone animation is then still exported)

## [1.6.0-pre] - 2022-04-28
- feat: added WebGL import support (export was already supported)
- feat: added WebGL animation export support
- feat: added onLoadComplete action to GLTFComponent
- feat: added ProfilerMarkers for export
- feat: import support for KHR_materials_emissive_strength
- changed: replaced FileLoader/WebRequestLoader with simpler UnityWebRequestLoader for better platform support. Use the previous ones if you need streaming.
- removed: removed unused shader variants from BiRP shaders
- fixed: sparse accessor JSON is properly parsed (no sparse accessor import support yet though)
- fixed: no more validation errors regarding minMag filters, bufferView.byteStride for animation samplers, bufferView.target for index/vertex data
- fixed: only export KHR_materials_emissive_strength if emissive intensity > 1
- fixed: URP roundtrip now works with glTFast imports (#42)
- fixed: better handling of filenames for glTF + bin + textures export (#41, #40)
- fixed: add vertex color alpha support, add _EmissionMap_ST support to BiRP shader
- fixed: exporting Prefab assets directly from Project Window wasn't working properly when ExportDisabled was off
- fixed: regression with serialization and roundtrip behaviour of spotlights

## [1.5.0-pre.2] - 2022-04-20
- fixed: incorrect UV offset for tiled textures on export in some cases
- feat: show exported glTF/GLB in explorer after exporting via Menu Item
- feat: expose API for `GetAnimationId` for custom export logic

## [1.5.0-pre] - 2022-04-08

- added: dialogue window will ask when meshes are not readable at runtime in the editor
- added: more samples regarding custom material extension export
- changed: more GltfSceneExporter methods are now  public to allow for custom extension exporting
- changed: moving back to proper versioning; big jump from 1.0 to 1.5 to show that lots of changes have happened
- fixed: no more ScriptedImporter collisions between UnityGltf, glTFast and Siccity glTF
- fixed: corrected some export callback orders
- fixed: random hangs when importing .gltf files via ScriptedImporter
- fixed: correctly importing wrapModeS and wrapModeT now
- fixed: correctly importing Mask mode and cutoff now

## [1.0.4-preview.35] - 2022-03-11
- added better logs for unsupported animation tracks on export
- added public methods in GLTFSceneExporter to allow for custom animation export
- added first batch of samples for custom extensions export
- fixed: Auto Referenced is back on on AsmDefs to keep legacy behaviour
- fixed: first timeline keyframe was exported incorrectly in some cases
- fixed: skins should be exported even when animation export is off
- fixed: animation clips shouldn't be exported when exporting from Timeline recorder
- fixed: MetallicGlossMap scale was incorrectly exported in some cases

## [1.0.4-preview.34] - 2022-02-23
- added Accessor reuse between exported animations when they come from the same AnimationClip/speed pair
- added optional project setting to export animation clips with unique names (lots of viewers don't support that)
- fixed animation clips being merged when the same clip was used in multiple Animators

## [1.0.4-preview.33] - 2022-02-21
- added export option to merge animator states with identical names into one animation
- fixed export of multiple animations when they had the same name (was implicitly merged before, now explicit)
- fixed export of multiple animations when they had the same animator state name but different speeds

## [1.0.4-preview.32] - 2022-02-10
- fixed compilation issues on older Unity versions
- fixed exporting in memory to GLB byte array
- fixed dependency to Timeline and some modules not being clear / not guarded
- fixed Readme containing outdated install instructions
- fixed compiler errors when building to some platforms

## [1.0.4-preview.31] - 2022-02-08
- added ability to record and export blend shapes at runtime
- fixed settings not being loaded from Resources correctly at runtime
- fixed another case of duplicate recorded keyframes

## [1.0.4-preview.30] - 2022-02-08
- added start events to recorder, made key methods virtual
- fixed GLTF recorder component global shortcuts; now uses regular keycode during play mode

## [1.0.4-preview.29] - 2022-02-07
- added GLTFRecorderComponent to record at runtime
- renamed recorder files to follow existing filenames
- fixed compilation warning in GLTFSceneImporter
- fixed: create export directory if it doesn't exist
- fixed: blendshape export was missing flag to export no blendshapes at all
- fixed: in some cases would attempt to record duplicate timestamps which is not allowed
- fixed build error from GLTFSceneExporter

## [1.0.4-preview.28] - 2022-02-07
- added glTF Timeline exporter track that allows for both editor and runtime animation export
- added ability to export RenderTextures instead of erroring out, these can be exported for a while now
- added export callbacks and made an initial set of export methods public to enable custom export extensions (similar to three.js export callbacks)
- added export generator name
- fixed URP/Unlit color not being exported
- fixed light intensity values being incorrect depending on Unity settings
- fixed removeEmptyRootObjects on import not actually removing empty root objects
- fixed warning when exporting texture transforms if _MainTex_ST is present but _MainTex isn't
- fixed non-specific textures using the wrong export type in some cases, asking the AssetImporter now if one exists
- changed menu items, export options are now in "Assets/UnityGltf/" instead of a toplevel "GLTF" menu

## [1.0.4-preview.27] - 2021-12-02
- fixed `_scaleFactor` not being applied to child transform positions for editor import
- fixed roundtrip issues when exporting models that have been imported by UnityGLTF or glTFast
- fixed metallicGlossMap being in the wrong color space on export if emission texture was also used
- fixed light/occlusion map being in the wrong color space during roundtrip

## [1.0.4-preview.26] - 2021-11-16
- fixed objects with EditorOnly tag being exported, are skipped now

## [1.0.4-preview.25] - 2021-11-12
- fixed imported object names potentially not being unique
- fixed normal map export format when build target is Android
- fixed export of animations when multiple exported objects share the same animation name (gets merged on export now)
- moved GLTF serialization from DLL to package to improve platform support (Unity will compile this for all platforms, no need for special DLLs)
- added sparse accessor export for blendshape positions/normals
- added ability to generate secondary UVs in UnityGLTF importer
- added warning when trying to use KTX2 textures which is currently not supported

## [1.0.4-preview.24] - 2021-06-14
- fix: don't attempt to export blendshape normals/tangents when mesh doesn't have them
- fix: warn and skip null bones in SkinnedMeshRenderer export
- added: settings for determining texture export type (png/jpeg depending on alpha channel) and JPEG quality
- added: setting to determine if vertex colors should be exported

## [1.0.4-preview.23] - 2021-05-28
- fixed animation export from legacy Animation component
- fixed exporting blendshapes for meshes that don't have bones
- fixed empty textures being generated on import
- fixed emission keyword missing on material import when only color but no texture was set
- fixed metallic and fade export not being correct in some cases
- added option to select which properties (normals/tangents) to export with blendshapes

## [1.0.4-preview.22] - 2021-05-04
- fixed ScriptedImporter not importing meshes from SkinnedMeshRenderers
- fixed ScriptedImporter not importing AnimationClips
- fixed ScriptedImporter changing material names on import
- fixed blend shape data being exported multiple times when meshes have multiple submeshes
- added blendshape animation export

## [1.0.4-preview.21] - 2021-04-26
- fixed settings file directory not being created on settings file creation

## [1.0.4-preview.20] - 2021-04-24
- added settings provider to change GLTF settings from `Project Settings/UnityGLTF`
- added settings for control over object export based on visibility (`Camera.cullingMask`) and active state (`GameObject.activeInHierarchy`)
- changed: experimental texture-from-disk export is now disabled by default
- fixed exporting GameObjects with names that contain invalid filename characters
- fixed normal sampling in built-in pipeline which most likely was never correct with scaled normals
- fixed texture sampling for export/import that resulted in incorrect Point sampling in some cases
- fixed some issues with exporting skinned mesh renderers
- fixed regression with multi material/submesh export

## [1.0.4-preview.18] - 2021-04-16
- CHANGED: package name is now back to org.khronos.unitygltf
- fixed missing mesh logging an error, is now a warning
- fixed property export order for URP shaders that also have built-in property fallbacks
- fixed export path omitting extensions for cases such as 15-1.0.3.glb
- fixed issue with shared meshes that have different materials not being properly exported (https://github.com/prefrontalcortex/UnityGLTF/issues/15)
- added MenuItem validation methods to disable them if no valid object for export is selected
- added ability to export selected Prefabs from the Project window directly
- changed the GLTFImporter to overrideExt for 2020.1+ so that importer for .glb files can be chosen when multiple are present

## [1.0.4-preview.17] - 2021-03-23
- fixed ACCESSOR_INDEX_PRIMITIVE_RESTART for meshes that have exactly 256 or 65536 vertices and used the wrong buffer type
- fixed vertex color import color space and alpha usage
- fixed incorrect keyframe values causing import abort, just warns now
- fixed GLB/GLTF export paths sometimes being incorrect (double or no extension)
- changed: GLB importer now embeds all meshes and textures as sub assets instead of putting them in the project; needs better importer inspector to allow for asset remapping.

## [1.0.4-preview.16] - 2021-03-15
- fixed color space issues with vertex colors and emissive
- fixed export of UV offset/tiling for non-main textures
- fixed exporting materials as PBR when they have _Metallic and _Glossiness (required only _MetallicGlossMap before)

## [1.0.4-preview.15] - 2021-02-23
- fixed issues with material export in URP where PBR wasn't properly detected and exported

## [1.0.4-preview.14] - 2021-02-10
- fixed issues with KHR_materials_pbrSpecularGlossinessExtension and materials that don't have textures

## [1.0.4-preview.13] - 2021-01-30
- fixed missing references in materials and prefabs
- fixed issue preventing build with UnityGLTF in project
- moved samples into subfolder
- added support for animation clip speed being set inside Animator States

## [1.0.4-preview.12] - 2020-12-15
- fix compilation errors in GLTFSerialization.dll on recompiling
- fix Newtonsoft.Json being copied to output directory
- fix a number of animation export issues
- add KHR_materials_unlit extension import and export

## [1.0.4-preview.11] - 2020-11-27
- fix KHR_lights_punctual extension always being set, even if no lights were exported
- fix invalid primitives being exported for meshes with 0 vertices
- removed export rotation offset that caused incorrect rotation in other softwares

## [1.0.4-preview.10] - 2020-11-16
- fix error thrown when vertex arrays have 0 elements (this is valid in FBX)
- add light export/import extensions
- merged a number of PRs that fix nullrefs on export/import

## [1.0.4-preview.9] - 2020-10-03
- merged in a number of material fix and null ref PRs
- add namespaces to scripts that didn't have them

## [1.0.4-preview.8] - 2020-09-10
- built GLTFSerialization.dll against Newtonsoft.JSON 12.0.3
- removed Newtonsoft.JSON.dll from package
- added dependency on com.unity.nuget.newtonsoft-json@2.0.0
- added 2020.2 compatibility

## [1.0.4-preview.7] - 2020-07-24
- fix sRGB/linear conversion for normal maps depending on Unity color space

## [1.0.4-preview.6] - 2020-07-15
- fix sRGB/linear conversion for colors exported from Unity
- fix rotation order for 180° hack
- note: please use Linear color space, normal maps break in Gamma color space right now

## [1.0.4-preview.5] - 2020-07-13
- hack: added 180° rotation on export (experimental), needs to be tested with animations, only works with a single root transform on export
- changed back to "preview" instead of "pfc" tag because stupid PackMan

## [1.0.4-pfc.4] - 2020-07-12
- fix meta files for Tests folder

## [1.0.4-pfc.2] - 2020-07-07
- fix build errors preventing builds
- fix PBR texture roundtrip with incorrect red channel

## [1.0.4-pfc] - 2020-04-24
- lots of improvements to animation export
- automatic keyframe reduction
- BREAKING: disabled UV2 and vertex color export for web
- fixed texture export from memory instead of from disk
- fix breaking animations in SceneViewer due to omitted LINEAR sampler specification

## [1.0.0-pfc.4] - 2020-01-16
- rebased all pfc feature branches on latest master
- back to org.khronos scope for easier switching between versions
- added animation export
- added PNG/JPG texture export from disk where available

## [1.0.1] - 2019-04-03
- Upgraded to 2017.4
- Included various fixes (will update)
- Includes UPM package

## [1.0.0] - 2018-09-21
- first built unity package release of UnityGLTF
