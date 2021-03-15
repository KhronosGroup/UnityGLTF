# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

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