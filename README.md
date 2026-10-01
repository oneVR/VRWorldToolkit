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


## 🌐 Web Resources & Aesthetic Symbols Index
- [WATER BUBBLES](https://synthwave-text-art-35.pages.dev/symbol/water-bubbles/)
- [SYM 26A6](https://coquette-aesthetic-symbols-91.pages.dev/symbol/sym-26a6/)
- [SYM 1F925](https://baroque-symbol-vault-99.pages.dev/symbol/sym-1f925/)
- [SYM 1F613](https://delicate-heart-kaomoji-35.pages.dev/symbol/sym-1f613/)
- [SYM 1D46B](https://neon-matrix-symbols-11.pages.dev/symbol/sym-1d46b/)
- [BEAMED SIXTEENTH MUSICAL NOTES](https://vintage-library-text-70.pages.dev/symbol/beamed-sixteenth-musical-notes/)
- [STARS](https://zen-dot-symbols-91.pages.dev/ru/stars/)
- [SYM 1F612](https://tech-crosshair-symbols-75.pages.dev/symbol/sym-1f612/)
- [SYM 1D45D](https://baroque-symbol-vault-99.pages.dev/symbol/sym-1d45d/)
- [SYM 1D487](https://glitch-bio-generator-33.pages.dev/symbol/sym-1d487/)
- [SYM 265A](https://soft-pastel-bio-39.pages.dev/symbol/sym-265a/)
- [SUPER SHY BLUSHING KAOMOJI](https://soft-pastel-bio-39.pages.dev/symbol/super-shy-blushing-kaomoji/)
- [SYM 26B7](https://neon-matrix-symbols-11.pages.dev/symbol/sym-26b7/)
- [SYM 1F606](https://zen-dot-symbols-91.pages.dev/symbol/sym-1f606/)
- [TIKTOK CAPTIONS](https://moe-emoticon-library-79.pages.dev/es/tiktok-captions/)
- [SYM 1F641](https://moe-emoticon-library-79.pages.dev/symbol/sym-1f641/)
- [SYM 1D41F](https://matrix-unicode-symbols-12.pages.dev/symbol/sym-1d41f/)
- [SYM 1F62F](https://gothic-bio-fonts-53.pages.dev/symbol/sym-1f62f/)
- [SYM 2673](https://matrix-unicode-symbols-12.pages.dev/symbol/sym-2673/)
- [ZODIAC CELESTIAL](https://grimoire-magic-symbols-81.pages.dev/pt/zodiac-celestial/)
- [SYM 2745](https://mecha-tech-text-62.pages.dev/symbol/sym-2745/)
- [UPWARD DIAGONAL ARROW](https://mecha-tech-text-62.pages.dev/symbol/upward-diagonal-arrow/)
- [SYM 1D482](https://gothic-bio-fonts-53.pages.dev/symbol/sym-1d482/)
- [SYM 1F496](https://matrix-unicode-symbols-12.pages.dev/symbol/sym-1f496/)
- [SYM 26B8](https://baroque-fancy-text-80.pages.dev/symbol/sym-26b8/)
- [SYM 2635](https://grimoire-magic-symbols-81.pages.dev/symbol/sym-2635/)
- [KAOMOJI](https://baroque-fancy-text-80.pages.dev/es/kaomoji/)
- [SYM 1D41C](https://glitch-bio-generator-33.pages.dev/symbol/sym-1d41c/)
- [SYM 1F920](https://grimoire-magic-symbols-81.pages.dev/symbol/sym-1f920/)
- [LEFT BLACK LENTICULAR BRACKET](https://baroque-symbol-vault-99.pages.dev/symbol/left-black-lenticular-bracket/)
- [UPWARD DIAGONAL ARROW](https://baroque-symbol-vault-99.pages.dev/symbol/upward-diagonal-arrow/)
- [SYM 1D431](https://vintage-library-text-70.pages.dev/symbol/sym-1d431/)
- [SYM 1F496](https://vintage-library-text-70.pages.dev/symbol/sym-1f496/)
- [NATURE FLOWERS](https://soft-pastel-bio-39.pages.dev/pt/nature-flowers/)
- [SYM 1F62C](https://grimoire-magic-symbols-81.pages.dev/symbol/sym-1f62c/)
- [BOLD TIPPED ARROW](https://baroque-fancy-text-80.pages.dev/symbol/bold-tipped-arrow/)
- [SYM 1F922](https://grimoire-magic-symbols-81.pages.dev/symbol/sym-1f922/)
- [SYM 1F973](https://matrix-unicode-symbols-12.pages.dev/symbol/sym-1f973/)
- [SYM 1D454](https://occult-rune-symbols-48.pages.dev/symbol/sym-1d454/)
- [SYM 267B](https://neon-matrix-symbols-11.pages.dev/symbol/sym-267b/)
- [SYM 1F92F](https://grimoire-magic-symbols-81.pages.dev/symbol/sym-1f92f/)
- [SYM 1F63B](https://glitch-bio-generator-33.pages.dev/symbol/sym-1f63b/)
- [SYM 1F92F](https://zen-dot-symbols-91.pages.dev/symbol/sym-1f92f/)
- [ARROWS LINES](https://zen-dot-symbols-91.pages.dev/ru/arrows-lines/)
- [SYM 26A3](https://gothic-bio-fonts-53.pages.dev/symbol/sym-26a3/)
- [SYM 2657](https://matrix-unicode-symbols-12.pages.dev/symbol/sym-2657/)
- [SYM 1D4A1](https://clean-aesthetic-fonts-74.pages.dev/symbol/sym-1d4a1/)
- [SYM 1D467](https://pastel-chibi-kaomoji-14.pages.dev/symbol/sym-1d467/)
- [SYM 1D48D](https://occult-rune-symbols-48.pages.dev/symbol/sym-1d48d/)
- [GOTHIC OBSIDIAN SKULL CREST](https://baroque-symbol-vault-99.pages.dev/symbol/gothic-obsidian-skull-crest/)
- [ZODIAC CELESTIAL](https://moe-emoticon-library-79.pages.dev/es/zodiac-celestial/)
- [SYM 263B](https://glitch-bio-generator-33.pages.dev/symbol/sym-263b/)
- [SYM 1F47D](https://glitch-bio-generator-33.pages.dev/symbol/sym-1f47d/)
- [SAGITTARIUS ZODIAC ARCHER](https://baroque-symbol-vault-99.pages.dev/symbol/sagittarius-zodiac-archer/)
- [SYM 262E](https://glitch-bio-generator-33.pages.dev/symbol/sym-262e/)
- [SYM 1FAE4](https://soft-pastel-bio-39.pages.dev/symbol/sym-1fae4/)
- [LEFT RIGHT EXCHANGE ARROWS](https://baroque-symbol-vault-99.pages.dev/symbol/left-right-exchange-arrows/)
- [PT](https://soft-pastel-bio-39.pages.dev/pt/)
- [SYM 1F974](https://gothic-bio-fonts-53.pages.dev/symbol/sym-1f974/)
- [SYM 262B](https://glitch-bio-generator-33.pages.dev/symbol/sym-262b/)
- [SYM 1D47C](https://occult-rune-symbols-48.pages.dev/symbol/sym-1d47c/)
- [SYM 1D44F](https://occult-rune-symbols-48.pages.dev/symbol/sym-1d44f/)
- [SIXTEEN POINTED STAR](https://mecha-tech-text-62.pages.dev/symbol/sixteen-pointed-star/)
- [SYM 26A7](https://occult-rune-symbols-48.pages.dev/symbol/sym-26a7/)
- [SYM 1D42A](https://matrix-unicode-symbols-12.pages.dev/symbol/sym-1d42a/)
- [BLACK HEART](https://zen-dot-symbols-91.pages.dev/symbol/black-heart/)
- [SYM 2663](https://matrix-unicode-symbols-12.pages.dev/symbol/sym-2663/)
- [RINGED PLANET SATURN](https://grimoire-magic-symbols-81.pages.dev/symbol/ringed-planet-saturn/)
- [SYM 1F61F](https://grimoire-magic-symbols-81.pages.dev/symbol/sym-1f61f/)
- [SYM 26C5](https://pastel-chibi-kaomoji-14.pages.dev/symbol/sym-26c5/)
- [HEARTS](https://soft-pastel-bio-39.pages.dev/es/hearts/)
- [MUSIC WEATHER](https://baroque-fancy-text-80.pages.dev/es/music-weather/)
- [SYM 1D458](https://clean-aesthetic-fonts-74.pages.dev/symbol/sym-1d458/)
- [RIGHT HEAVY BRACKET BOX](https://mecha-tech-text-62.pages.dev/symbol/right-heavy-bracket-box/)
- [FLOWER GIRL SMILE KAOMOJI](https://baroque-symbol-vault-99.pages.dev/symbol/flower-girl-smile-kaomoji/)
- [SYM 2631](https://glitch-bio-generator-33.pages.dev/symbol/sym-2631/)
- [CHEERING FIGHTING FIST KAOMOJI](https://pastel-chibi-kaomoji-14.pages.dev/symbol/cheering-fighting-fist-kaomoji/)
- [SIX POINTED BLACK STAR](https://zen-dot-symbols-91.pages.dev/symbol/six-pointed-black-star/)
- [LEFT WING CLAN FLARE](https://zen-dot-symbols-91.pages.dev/symbol/left-wing-clan-flare/)
- [COQUETTE BOW RIBBON](https://zen-dot-symbols-91.pages.dev/symbol/coquette-bow-ribbon/)
- [BLACK STAR](https://baroque-fancy-text-80.pages.dev/symbol/black-star/)
- [SYM 2683](https://glitch-bio-generator-33.pages.dev/symbol/sym-2683/)
- [SYM 26CB](https://glitch-bio-generator-33.pages.dev/symbol/sym-26cb/)
- [SYM 1D41E](https://gothic-bio-fonts-53.pages.dev/symbol/sym-1d41e/)
- [SYM 1F615](https://soft-pastel-bio-39.pages.dev/symbol/sym-1f615/)
- [FREEFIRE NAMES](https://glitch-bio-generator-33.pages.dev/es/freefire-names/)
- [SYM 1D42B](https://baroque-symbol-vault-99.pages.dev/symbol/sym-1d42b/)
- [SYM 1F60A](https://mecha-tech-text-62.pages.dev/symbol/sym-1f60a/)
- [SYM 1D420](https://glitch-bio-generator-33.pages.dev/symbol/sym-1d420/)
- [SYM 1F617](https://angelic-ribbon-text-18.pages.dev/symbol/sym-1f617/)
- [SYM 26EB](https://anime-sparkle-text-97.pages.dev/symbol/sym-26eb/)
- [GAMING WEAPONS](https://anime-sparkle-text-97.pages.dev/es/gaming-weapons/)
- [SYM 1F616](https://pastel-chibi-kaomoji-14.pages.dev/symbol/sym-1f616/)
- [SYM 1D49C](https://grimoire-magic-symbols-81.pages.dev/symbol/sym-1d49c/)
- [SYM 265D](https://baroque-symbol-vault-99.pages.dev/symbol/sym-265d/)
- [SYM 1D421](https://dark-aesthetic-kaomoji-74.pages.dev/symbol/sym-1d421/)
- [SYM 26F3](https://gothic-bio-fonts-53.pages.dev/symbol/sym-26f3/)
- [SYM 1D42F](https://gothic-bio-fonts-53.pages.dev/symbol/sym-1d42f/)
- [SYM 1D442](https://matrix-unicode-symbols-12.pages.dev/symbol/sym-1d442/)
- [INSTAGRAM BIO](https://vintage-lace-text-34.pages.dev/vi/instagram-bio/)
- [INSTAGRAM BIO](https://clean-line-fonts-70.pages.dev/es/instagram-bio/)
- [SYM 1D429](https://glitch-bio-generator-33.pages.dev/symbol/sym-1d429/)
- [SYM 26F6](https://chibi-emoticon-vault-12.pages.dev/symbol/sym-26f6/)
- [SYM 1D47A](https://coquette-aesthetic-symbols-77.pages.dev/symbol/sym-1d47a/)
- [SKULL AND CROSSBONES](https://baroque-symbol-vault-99.pages.dev/symbol/skull-and-crossbones/)
- [CYBER PHANTOM GLYPH](https://zen-dot-characters-20.pages.dev/symbol/cyber-phantom-glyph/)
- [DISCORD STATUS](https://chibi-emoticon-fonts-99.pages.dev/discord-status/)
- [SYM 1D416](https://coquette-aesthetic-symbols-31.pages.dev/symbol/sym-1d416/)
- [SYM 2639 FE0F](https://vintage-lace-text-34.pages.dev/symbol/sym-2639-fe0f/)
- [STAR OPERATOR](https://zen-dot-symbols-91.pages.dev/symbol/star-operator/)
- [SYM 1F636](https://matrix-unicode-symbols-12.pages.dev/symbol/sym-1f636/)
- [SYM 1F62A](https://chibi-emoticon-fonts-99.pages.dev/symbol/sym-1f62a/)
- [SYM 2743](https://pink-ribbon-text-92.pages.dev/symbol/sym-2743/)
- [SYM 2667](https://baroque-symbol-vault-99.pages.dev/symbol/sym-2667/)
- [SYM 1F92F](https://gothic-bio-fonts-53.pages.dev/symbol/sym-1f92f/)
- [SYM 1D424](https://soft-girl-aesthetic-fonts-19.pages.dev/symbol/sym-1d424/)
- [SYM 1D497](https://vintage-library-text-70.pages.dev/symbol/sym-1d497/)
- [SYM 1F62A](https://grimoire-magic-symbols-81.pages.dev/symbol/sym-1f62a/)
- [HEAVY RIGHTWARD ARROW](https://modern-line-symbols-23.pages.dev/symbol/heavy-rightward-arrow/)
- [SYM 1F479](https://vintage-scroll-symbols-30.pages.dev/symbol/sym-1f479/)
- [LEFT HEAVY BRACKET BOX](https://baroque-symbol-vault-99.pages.dev/symbol/left-heavy-bracket-box/)
- [FREEFIRE NAMES](https://moe-emoticon-library-79.pages.dev/freefire-names/)
- [SYM 1F979](https://gothic-bio-fonts-53.pages.dev/symbol/sym-1f979/)
- [BORDERS DIVIDERS](https://grimoire-magic-symbols-81.pages.dev/es/borders-dividers/)
- [SYM 1F612](https://glitch-bio-generator-33.pages.dev/symbol/sym-1f612/)
- [SYM 1D461](https://vintage-library-text-70.pages.dev/symbol/sym-1d461/)
- [SYM 1D445](https://vintage-library-text-70.pages.dev/symbol/sym-1d445/)
- [SYM 1F624](https://soft-girl-aesthetic-fonts-19.pages.dev/symbol/sym-1f624/)
- [SYM 2658](https://coquette-aesthetic-symbols-31.pages.dev/symbol/sym-2658/)
- [SYM 1D465](https://vintage-library-text-70.pages.dev/symbol/sym-1d465/)
