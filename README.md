🇬🇧 ENGLISH | [🇷🇺 РУССКИЙ](README_RU.md)

# FORK NOTE

This fork of Frosty Tool Suite 1.0.7 is based on [selphea's fork of 1.0.7](https://github.com/selphea/FrostyToolsuite) and is aimed at bringing some of the features from 1.0.6.x and existing forks like [Veilguard fork by J-Lyt](https://github.com/J-Lyt/FrostyToolsuite) if possible, along with keeping NFS Unbound's SDK file up-to-date. This fork serves as an opportunity to mod newer games Frosty Tool Suite 1.0.6.3 doesn't support and will be discontinued in favour of an upcoming 2.0.0 version of Tool Suite, which as of now is still a WIP, and which has no estimated time of release.
<br>The backport commits in this fork bear the "throw and see what sticks and doesn't fail during build" philosophy and you have all the rights to take all of the actions here with a grain of sea salt, so _do_ report of issues if there are any.

If you are to ask for help with Frosty Tool Suite 1.0.7, create an `Issue` so ~we~ I can try taking a look into it. But keep in mind that [the original developers of Frosty Tool Suite 1.0.7 have discontinued it and no longer provide help for it](https://images2.imgbox.com/a6/a1/CqTQvcGL_o.png), which means if something goes wrong and we can't help you with it, you will be on your own.

## What's new in this fork and in what ways it is different?

- InitFS modding (both Heat and Unbound are supported)
- Improved mesh importing:
  - Added support for importing meshes that require tangent space compression (both Heat and Unbound are supported)
  - More detailed exceptions if something is wrong with imported mesh
- Template and Blueprint modding
- Fixed Mod Manager exit, meaning you don't have to close it manually in Task Manager anymore
- Shadercache symlinking, meaning it should help with performance when running mods
- Fixed `Object reference not set to an instance of an object` `IterateSubKeys` type crash at launch
- Mod Manager now features more advanced filtering functionality: you can show or hide applied mods in `Available Mod(s)` section
- Fixed `ealayer3.dll` type crash when attempting to open audio assets
- Some of the new plugins that expand Editor functionality
- Mod Manager doesn't identify itself as Editor anymore
- Fixed splash screen not showing banner art
- Mod Manager now shows for what game version (Volume) mod was made

## In plans / In progress

- Battlefield 6 complete support
- Skate.™️ support
- Dead Space support. I believe modding of this game is already available, the game just lacks the feature of loading custom data path, despite having mention of the launch parameter
- Bugs fixing if possible
- Audio importing in `NewWaveAsset` assets support. The importer 1.0.7 has is broken (but it is there, at least)

# FrostyToolsuite
The most advanced modding platform for games running on DICE's Frostbite game engine.

## Setup

1. Download the source code.
2. Open the solution (found under FrostyEditor) with Visual Studio 2022, and make sure the project is set to ``Release - Final`` and ``x64``. Close out of retarget window if prompted.
3. Only build the projects themselves, never the solution.

## License
The Content, Name, Code, and all assets are licensed under a Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
