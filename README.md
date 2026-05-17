ENGLISH

# FORK NOTE

This is a Fork of Hargabt's Fork of Frosty Toolsuite and that one is based on [selphea's fork of 1.0.7](https://github.com/selphea/FrostyToolsuite). Mine is specifically aimed at improving the Dead Space Remake modding experience. Report issues if you notice any.

## New in this fork

From here on out I will refer to Dead Space Remake as "DSR".
- MeshVariation Database recognition fixed for DSR so for quite a lot of meshes, texture previewing works again in Mesh Viewer. Still working on extending support for this.
- Biggest one: Automatic mod installation. It goes like this: User must set a backup location or else mods wont compile at all. Once done, on first ever backup creation it creates an appropiate, efficient backup. Mod gets compiled as usual, .symlink steps and cmd admin entirely removed. It checks if a mod is already installed, if yes, replace files with backup and remove any additional .cas files. Files automatically gets imported properly from ModData, and that's it. Works beautifully. Also applies to the Mod Manager.
- "Restore Vanilla" button added next to Compile button which will turn game back to vanilla using CAS-deletion and backup.
- Localization rework, across whole software: Add German (de-DE) and English (en-US) string resources for FrostyEditor, FrostyModManager, FrostyPlugin. All the individual plugins are not covered yet, more translations can be added as wanted
- Fixed sound importing to a major extent, included support for .opus and .snr files (EALayer3)
- Attempted fix for mod compilation inconsistencies when re-opening or re-compiling projects.
FrostySdk: EbxWriter and AssetManager fixes. Any instabilities with certain file types in Frosty Editor such as Widgets on re-opening project files or re-compiling and more should be fixed. (Still testing this)

## What's coming next?

- Bug fixes and more features

# FrostyToolsuite
The most advanced modding platform for games running on DICE's Frostbite game engine.

## Setup

1. Download the source code.
2. Open the solution (found under FrostyEditor) with Visual Studio 2022, and make sure the project is set to ``Release - Final`` and ``x64``. Close out of retarget window if prompted.
3. For applications, build only Frosty Editor or FrostyModManager, rest is the others. Only build the projects themselves, never the solution.

## License
The Content, Name, Code, and all assets are licensed under a Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
