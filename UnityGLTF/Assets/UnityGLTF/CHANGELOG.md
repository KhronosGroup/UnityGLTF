# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

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