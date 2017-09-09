# UnityGLTF

[![Join the chat at https://gitter.im/KhronosGroup/UnityGLTF](https://badges.gitter.im/KhronosGroup/UnityGLTF.svg)](https://gitter.im/KhronosGroup/UnityGLTF?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
Unity3D library for exporting, loading, parsing, and rendering assets stored in the [GLTF 2.0](https://github.com/KhronosGroup/glTF/tree/2.0) file format at runtime.

The specification isn't finalized yet and this loader is a work in progress. Feel free to check it out and contribute, but don't use it for anything serious yet.

## Examples

1. Clone or download the repository.
2. Open up the Unity project and run any of the example scenes in `Assets/GLTF/Examples`

You should see something like this:

![GLTF Lantern](/Screenshots/Lantern.png)

## Features

- [x] Unity Component for rendering a GLTF model at runtime ([GLTFComponent](Assets/GLTF/Scripts/GLTFComponent.cs))
- [x] Parsing GLTF files into an easy to use C# class ([GLTFParser](Assets/GLTF/Scripts/GLTFParser.cs))
- [x] Loading Meshes
- [ ] Loading Materials
    - [x] Base Color/Diffuse texture
    - [x] Normal/Bumpmap
    - [x] Metallic Roughness Physically Based Rendering
    - [ ] Specular Glossiness Physically Based Rendering (extension)
    - [x] Occlusion map
    - [ ] Transparent materials
- [ ] Loading animations
- [ ] Loading cameras
- [ ] Sparse array storage
- [x] Loading binary GLTF files
- [ ] Downloadable as a UnityPackage
- [ ] Published in the Unity Asset Store
