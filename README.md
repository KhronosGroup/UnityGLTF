# UnityGLTF

 [![Build Status](https://travis-ci.org/KhronosGroup/UnityGLTF.svg?branch=master)](https://travis-ci.org/KhronosGroup/UnityGLTF) [![Join the chat at https://gitter.im/KhronosGroup/UnityGLTF](https://badges.gitter.im/KhronosGroup/UnityGLTF.svg)](https://gitter.im/KhronosGroup/UnityGLTF?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

Unity3D library for importing and exporting [GLTF 2.0](https://github.com/KhronosGroup/glTF/) assets. 

The goal of this library is to support the full glTF 2.0 specification and enable the following scenarios:  
- Run-time import of glTF 2.0 files
- Run-time export of glTF 2.0 files
- Design-time import of glTF 2.0 files
- Design-time export of glTF 2.0 files

The library will be modularized such that it can be extended to support additional capabilities in Unity or support additional extensions to the glTF specification.  The library is designed to work with Unity 5.6 and above.

## Current Status

Work Items and Issues targeting a 1.0 release of the library can be found in
[Road to 1.0](https://github.com/KhronosGroup/UnityGLTF/projects/1)
	
## Getting Started
- This section is dedicated to those who wish to contribute to the project. This should clarify the main project structure without flooding you with too many details.
- UnityGLTF project is divided into two parts: the GLTFSerializer (Visual Studio Solution), and the Unity Project (which is the package we wish to make available to users)

### [GLTFSerializer](https://github.com/KhronosGroup/UnityGLTF/tree/master/GLTFSerialization)
- **Basic Rundown**: The GLTFSerializer is a C# DLL implemented to faciliate serialization of the Unity asset model, and deserialization of GLTF assets.

- **Structure**: 
	- Each GLTF schemas (Buffer, Accessor, Camera, Image...) extends the basic class: GLTFChildOfRootProperty. Through this object model, each schema can have its own defined serialization/deserialization functionalities, which imitate the JSON file structure as per the GLTF specification.
	- Each schema can then be grouped under the GLTFRoot object, which represents the underlying GLTF Asset. Serializing the asset is then done by serializing the root object, which recursively serializes all individual schemas. Deserializing a GLTF asset is done similarly: instantiate a GLTFRoot, and parse the required schemas.

- **Building**: You will need to build this library into the Plugins folder of the core Unity project: 
	1. Open `GLTFSerialization\GLTFSerialization.sln` and compile for release. This will put the binaries in `UnityGLTF\Assets\UnityGLTF\Plugins`
	2. Open the Unity project located in `UnityGLTF\`
		* If the meta file gets overridden, the binaries in `UnityGLTF\Assets\UnityGLTF\Plugins` should be configured for everything but UWP. The binaries in `UnityGLTF\Assets\UnityGLTF\Plugins\UWP` should be configured for UWP

### [The Unity Project](https://github.com/KhronosGroup/UnityGLTF/tree/master/UnityGLTF)
- #### Unity Version
	Although the UnityGLTF Project is maintained at Unity version 5.6.5f1, make sure that the Unity release you have installed on your local machine is up to date. This is so the Exporter can export the latest features, but the Importer can support importing on older Unity builds. You can download the free version [here](https://unity3d.com/get-unity/download/archive). You can run this project simply by opening the directory as a project on Unity.
- ##### Project Components
	The Unity project offers two main functionalities: importing and exporting GLTF assets. These functionalities are primarily implemented in [GLTFSceneImporter] and [GLTFSceneExporter]. 

## Examples
1. Clone or download the repository.
2. Open up the Unity project and run any of the example scenes in `Assets/GLTF/Examples`

You should see something like this:

![GLTF Lantern](/Screenshots/Lantern.png)
