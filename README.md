# Sketchfab Plugin for Unity

*Browse, import and export assets between Unity and Sketchfab*

A Sketchfab account is required for most of this plugin features (you can [create one here](https://sketchfab.com/signup))


*Based on Khronos' [UnityGLTF plugin](https://github.com/KhronosGroup/UnityGLTF)*

*Supported versions: Unity 5.6.4 or later*

## Installation

Download attached `SketchfabForUnity-vx.x.x.unitypackage` from the [latest release](https://github.com/sketchfab/UnityGLTF/releases/latest) and double click on it to import the plugin into your current Unity project.

## Report an issue
If you have any issue, please use the [Report an issue](https://help.sketchfab.com/hc/en-us/requests/new?type=exporters&subject=Unity+Exporter) link to be redirected to the support form.

## Sketchfab Asset Browser
*Available in menu: Sketchfab/Browse Sketchfab*

#### Authentication
Browse more than [150k free downloadable models from Sketchfab](https://sketchfab.com/models?features=downloadable&sort_by=-likeCount&type=models) and import them into your Unity project.

*You need a Sketchfab account to download and import assets from Sketchfab*

#### Asset Browser UI
![unity2](https://user-images.githubusercontent.com/4066133/37648425-37afe732-2c2f-11e8-9ba1-9f82eeccae06.JPG)

Click on a thumbnail to show the corresponding model page with a button to download and import the asset
![unity3](https://user-images.githubusercontent.com/4066133/37648438-3ded2682-2c2f-11e8-83f3-96087d1b26df.JPG)

You can specify a path **in your project directory** to import the asset into, set a name for the prefab that will contain the model and also choose to import it into your current scene.

Click the "Download model" button to download to proceed.


## Sketchfab Exporter
*Available in menu: Sketchfab/Publish to Sketchfab*

Export and share your current Unity scene on Sketchfab.

*You need a Sketchfab account to download and import assets from Sketchfab*

**Important note:** glTF file format is used as transport between Unity and Sketchfab.
Because of this, a few features will not be supported and will be missing on the Sketchfab result.
The plugin will not export:
* animations or object handled by custom scripts
* custom materials/shaders

Only Standard materials (including Specular setup) are supported.

For animation, only Generic or Legacy animation type are exported.

This support will be improved in the future.

![unity4](https://user-images.githubusercontent.com/4066133/37648662-e1612fa2-2c2f-11e8-8f7b-9658970da423.JPG)



## glTF Importer (editor)

Import glTF asset into Unity
*Available in menu: Sketchfab/Import glTF*

Drag and drop glTF asset on the importer window, set the import options and click import.
![unity5](https://user-images.githubusercontent.com/4066133/37648668-e72c98fe-2c2f-11e8-8e80-b228b2df82dd.JPG)
