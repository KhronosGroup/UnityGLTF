# Sketchfab Plugin for Unity

Unity plugin to browse, import and export asset between Unity and Sketchfab.
*Based on khronos' [UnityGLTF plugin](https://github.com/KhronosGroup/UnityGLTF)*

A Sketchfab account is required for most of this plugin features (you can [create one here](https://sketchfab.com/signup))

*Supported versions: Unity 5.6.4 or later*

## Sketchfab Asset Browser
*Available in menu: Sketchfab/Browse Sketchfab*

Browse more than [nb] free downloadable models from Sketchfab and import them into your Unity project.
*You need a Sketchfab account to download and import assets from Sketchfab*
(browser image)

Click on a thubnail to show the corresponding model page with a button to download and import the asset
(model page)

You can specify a directory in your project to import the asset into, and also choose to add the model to your current scene.
Click the "Download model" button to download to proceed.


## Sketchfab Exporter
*Available in menu: Sketchfab/Publish to Sketchfab*

Export and share your current Unity scene on Sketchfab.
*You need a Sketchfab account to download and import assets from Sketchfab*

Important note: glTF file format is used as transport between Unity and Sketchfab.
Because of this, a few features will not be supported and will be missing on the Sketchfab result.

The plugin will not export:
* animations or object handled by custom scripts
* custom materials/shaders

Only Standard materials (including Specular setup) are supported.
For animation, only Generic or Legacy animation type are exported.

This support will be improved in the future.

(exporter image)


## glTF Importer (editor)

Import glTF asset into Unity
*Available in menu: Sketchfab/Import glTF*

(import image)


## Installation

Download the Unity package of the [latest release](https://github.com/sketchfab/UnityGLTF/releases) and double click on it to import the plugin into your current Unity project.


## Report an issue
If you have any issue, please use the [Report an issue](https://help.sketchfab.com/hc/en-us/requests/new?type=exporters&subject=Unity+Exporter) link to be redirected to the support form.
