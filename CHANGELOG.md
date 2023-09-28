# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.4.2-exp] - 2023-09-28
- change: always export root level objects / objects explictly requested to be exported

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
