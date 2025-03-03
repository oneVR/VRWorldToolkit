# VRWorld Toolkit

<img src="https://github.com/oneVR/VRWorldToolkit/assets/4764355/0672bef5-0aa4-42b4-b388-1a47bc1ba998">

<div align="center">

[![GitHub stars](https://img.shields.io/github/stars/oneVR/VRWorldToolkit?style=for-the-badge)](https://github.com/oneVR/VRWorldToolkit/stargazers)
[![GitHub all releases](https://img.shields.io/github/downloads/oneVR/VRWorldToolkit/total?style=for-the-badge)](https://github.com/oneVR/VRWorldToolkit/releases)
[![GitHub release (latest SemVer)](https://img.shields.io/github/v/release/oneVR/VRWorldToolkit?sort=semver&style=for-the-badge)](https://github.com/oneVR/VRWorldToolkit/releases/latest)
[![Project License](https://img.shields.io/badge/license-MIT-brightgreen?style=for-the-badge)](https://github.com/oneVR/VRWorldToolkit/blob/master/LICENSE)
![GitHub repo size](https://img.shields.io/github/repo-size/oneVR/VRWorldToolkit?style=for-the-badge)

</div>

**VRWorld Toolkit** is a Unity Editor extension with the purpose of making VRChat world creation more accessible and making it easier to create a good-performing world. The main supported use case is for VRChat world projects, but avatar projects and projects without the VRChat SDK are supported in a limited capacity.

To report problems, you can either join my [Discord server](https://discord.com/invite/FCm28DM) or create [a new issue](https://github.com/oneVR/VRWorldToolkit/issues/new/choose). Pull requests are also welcome.

## Setup

### Requirements
* Unity 2022.3.x

### Getting Started
* If you are not using VRChat Creator Companion, you can import the Unity Package from the latest release from [here](https://github.com/oneVR/VRWorldToolkit/releases) into your Unity project
* When using VRChat Creator Companion, you can find the VRWorld Toolkit from the built-in Curated repositories.
* After importing, you will see the VRWorld Toolkit dropdown appear in the toolbar if not check [Troubleshooting](#troubleshooting)

### Troubleshooting
> [!IMPORTANT]  
> First, if you are working on a VRChat project, make sure you are running the latest SDK version if not [update](https://creators.vrchat.com/sdk/updating-the-sdk/). This project is kept up to date, supporting the latest SDK versions. Support for older versions is not guaranteed.

Start by opening the Unity Console either by using `Ctrl + Shift + C` or from `Window > General > Console`. Afterward, make sure red errors are enabled from the top right corner of the window. Finally, press `Clear` in the top left corner, which will narrow the view down to only compilation-stopping errors.

If the errors that are left mention Post Processing or Bakery when the project *does not* currently have these, see the following paragraphs.

The most common issue is when the project previously had `Post Processing` or `Bakery` but has since been removed. This will leave behind a Scripting Define Symbol that the assets automatically add, making VRWorld Toolkit think they still exist in the project.

This can be manually removed from `Edit > Project Settings > Player > Other Settings > Scripting Define Symbols`

* For Bakery: `BAKERY_INCLUDED`
* For Post Processing: `UNITY_POST_PROCESSING_STACK_V2`

These symbols' primary function is to load parts of code only when they are set in the project. However, they do not automatically get removed with the asset that added them to your project.

A rare issue can also be caused by having a `Bloom.cs` script or just `Bloom` class in the global namespace in your project conflicting with Post Processing. This can usually be seen in the console by having repeated errors for Post Processing bloom not being able to be accessed from VRWorld Toolkit scripts. The easiest solution is finding and removing the offending script often found just by searching for `Bloom` in your assets.

## Main features

<img align="right" width="400" margin="20" src="https://github.com/oneVR/VRWorldToolkit/assets/4764355/52c0c25c-c3e9-4b73-8e88-b4e10c884040">

### World Debugger
Goes through the scene, checks for common issues, and makes suggestions on what to improve. Includes over 90 different tips, warnings, errors, and general messages!

It also allows viewing the stats of the latest builds SDK has done for an easily accessible overview of what the build consists of. It also saves the latest Windows and Android builds separately for easy comparison between the two.

### Disable On Build
After the setup is run from `VRWorld Toolkit > Disable On Build > Setup` a new tag is added `DisableOnBuild` that automatically disables all GameObjects marked with it before a build happens. The most significant use case for this is easier to manage trigger-based occlusion.

### Post Processing
Offers a one-click solution to having a working Post Processing setup with a simple example profile for further editing.

### Quick Functions

#### Copy World ID
Helps you to quickly copy the current scene's world ID to the clipboard without having to fumble trying to find the Scene Descriptor.

#### Mass Texture Importer
Batch processes textures to quickly apply crunch compression and other settings to all textures in the current scene or all assets in the project.

### Custom Editors
Adds more features to the pre-existing VRChat components to make them easier to use and provide quality-of-life improvements. If not needed, they can also be easily disabled from `VRWorld Toolkit > Custom Editor > Disable`.

Includes additions to:

* VRC Mirror Reflection
  * Quick-set layers to commonly used setups
  * Warnings and messages for common problems people run into with mirrors
  * Explanations for VRChat-specific layers
* VRC Avatar Pedestal
  * Adds a feature to mass copy and set IDs to pedestals while having multiple selected
  * Draws outlines of where the pedestal image will appear in-game when you select the GameObject with the pedestal component on it

## Special Thanks to

* [Pumkin](https://github.com/rurre/PumkinsAvatarTools) - For helping me a lot to get started and creating the original Disable On Upload feature that got me started on this project
* [Silent](http://s-ilent.gitlab.io/index.html) - For making my texts more clear and for help with Post Processing features
* [Metamaniac (Table)](https://twitter.com/Metamensa) - Checked through my texts and found all the stupid typos I made

**Disclaimer:** This extension is still a work in progress. Even though I try to test it thoroughly, things can break. *Remember to make backups of your projects and use this at your own risk!*
