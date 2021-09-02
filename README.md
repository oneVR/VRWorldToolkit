# VRWorld Toolkit

<img src="https://user-images.githubusercontent.com/4764355/114099005-d2a86880-98ca-11eb-8de1-f77360503d6d.png">

<div align="center">

[![GitHub stars](https://img.shields.io/github/stars/oneVR/VRWorldToolkit?style=for-the-badge)](https://github.com/oneVR/VRWorldToolkit/stargazers)
[![GitHub all releases](https://img.shields.io/github/downloads/oneVR/VRWorldToolkit/total?style=for-the-badge)](https://github.com/oneVR/VRWorldToolkit/releases)
[![GitHub release (latest SemVer)](https://img.shields.io/github/v/release/oneVR/VRWorldToolkit?sort=semver&style=for-the-badge)](https://github.com/oneVR/VRWorldToolkit/releases/latest)
[![Project License](https://img.shields.io/badge/license-MIT-brightgreen?style=for-the-badge)](https://github.com/oneVR/VRWorldToolkit/blob/master/LICENSE)
![GitHub repo size](https://img.shields.io/github/repo-size/oneVR/VRWorldToolkit?style=for-the-badge)

</div>

**VRWorld Toolkit** is a Unity Editor extension made to make VRChat world creation more accessible and lower the entry-level to make a good performing world. Currently, the tool is aimed only at people making VRChat worlds, and most functions only work with the VRCSDK imported, but this might change in the future.

To report problems, you can either join my [Discord server](https://discord.com/invite/FCm28DM) or create [a new issue](https://github.com/oneVR/VRWorldToolkit/issues/new/choose).

## Setup

### Requirements
* Unity 2019.4.29f1
* Latest VRChat SDK [SDK2 / SDK3 Worlds or Avatars](https://vrchat.com/home/download)

### Getting Started
* Import the latest release from [here](https://github.com/oneVR/VRWorldToolkit/releases) into your Unity project
*  After, if everything went well, you will see the VRWorld Toolkit dropdown appear at the top bar if not check [Troubleshooting](#troubleshooting)

### Troubleshooting
First, make sure you are running the latest SDK (SDK2 / SDK3 Worlds or Avatars) if not [update](https://docs.vrchat.com/docs/updating-the-sdk). This project is kept up to date, supporting the latest SDK versions. Support for older versions is not guaranteed.

Start by opening the Unity Console either by using `Ctrl + Shift + C` or from `Window > General > Console`. Afterward, make sure red errors are enabled from the top right corner of the window. Finally, press `Clear` in the top left corner, which will narrow the view down to only compilation stopping errors.

If the errors that are left mention Post Processing or Bakery when the project * does not* have these in it currently, see the following paragraphs.

The most common issue is when the project previously had `Post Processing` or `Bakery` but has since been removed. This will leave behind a Scripting Define Symbol that the assets automatically add, making VRWorld Toolkit think they still exist in the project.

This can be manually removed from `Edit > Project Settings > Player > Other Settings > Scripting Define Symbols`

* For Bakery: `BAKERY_INCLUDED`
* For Post Processing: `UNITY_POST_PROCESSING_STACK_V2`

These symbols' primary function is to load parts of code only when they are set in the project. However, they do not automatically get removed with the asset.

A more rare issue is also caused by having a `Bloom.cs` script or just `Bloom` class in the global namespace in your project conflicting with Post Processing. This can usually be seen in the console by having repeated errors for Post Processing bloom not being able to be accessed from VRWorld Toolkit scripts. The easiest solution is finding and removing the offending script often found just by searching for `Bloom` in your assets.

## Main features

<img align="right" width="400" margin="20" src="https://user-images.githubusercontent.com/4764355/114107922-9bda4e80-98da-11eb-8024-ad2bde3c5b6b.png">

### World Debugger
Goes through the scene and checks for common issues, and makes suggestions on what to improve. Includes over 90 different tips, warnings, errors, and general messages!

It also allows viewing the stats of the latest builds SDK has done for an easily accessible overview of what the build consists of. It also saves the latest Windows and Android builds separately for easy comparison between the two.

### Disable On Build
After the setup is run from `VRWorld Toolkit > Disable On Build > Setup` a new tag is added `DisableOnBuild` that automatically disables all GameObjects marked with it before a build happens. The most significant use case for this is easier to manage trigger-based occlusion.

### Post Processing
Offers a one-click solution to having a working Post Processing setup with a simple example profile for further editing.

### Quick Functions

#### Copy World ID
Helps you to quickly copy the current scenes world ID to clipboard without having to fumble finding the Scene Descriptor.

#### Mass Texture Importer
Batch processes textures to quickly apply crunch compression and other settings to all textures in the current scene or all assets in the project.

### Custom Editors
Adds more features to the pre-existing VRChat components to make them easier to use and provide quality of life improvements. If not needed, they can also be easily disabled from `VRWorld Toolkit > Custom Editor > Disable`.

Includes additions to:

* VRC Mirror Reflection
  * Quick set layers to commonly used setups
  * Warnings and messages for common problems people run into with mirrors
  * Explanations for VRChat specific layers
* VRC Avatar Pedestal
  * Adds a feature to mass copy and set IDs to pedestals while having multiple selected
  * Draws outlines to where the pedestal image will appear in-game when you select the GameObject with the pedestal component on it

### Useful Links
Quick access to commonly used links related to VRChat content creation.

## Special Thanks to

* [Pumkin](https://github.com/rurre/PumkinsAvatarTools) - For helping me a lot to get started and creating the original Disable On Upload feature that got me started on this project
* [Silent](http://s-ilent.gitlab.io/index.html) - For making my texts more clear and for help on Post Processing features
* [Metamaniac (Table)](https://twitter.com/Metamensa) - Checked through my texts and found all the stupid typos I made

**Disclaimer:** This extension is still a work in progress. Even though I try to test it thoroughly, things can break. *Remember to make backups of your projects and use this at your own risk!*