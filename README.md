# FORK NOTE

This fork of Frosty Tool Suite 1.0.7 is based on [selphea's fork of 1.0.7](https://github.com/selphea/FrostyToolsuite) and is aimed at bringing some of the features from 1.0.6.3 (and 1.0.6.4 as well) if possible, along with keeping NFS Unbound's SDK file up-to-date.
<br>The backport commits in this fork bear the "throw and see what sticks and doesn't fail during build" philosophy and you have all the rights to take all of the actions here with a grain of sea salt.

And please, don't ask for help for this fork of 1.0.7 or 1.0.7 as whole at all. [Original developers of Frosty Tool Suite 1.0.7 have discontinued it and no longer provide help for it](https://images2.imgbox.com/a6/a1/CqTQvcGL_o.png), which means you are on your own if something goes wrong.

## What's new in this fork and in what ways it is different?

- InitFS modding (both Heat and Unbound are supported)
- Template and Blueprint modding (RimeWidgetBlueprint modding is somewhat broken, would really like to fix that if I knew how)
- Fixed Mod Manager exit, meaning you don't have to close it manually in Task Manager anymore
- Shadercache symlinking, meaning it should help with performance when running mods
- Fixed `Object reference not set to an instance of an object` `IterateSubKeys` type crash at launch
- Mod Manager now features more advanced filtering functionality: you can show or hide applied mods in `Available Mod(s)` section
- Fixed `ealayer3.dll` type crash when attempting to open audio assets
- Some of the new plugins that expand Editor functionality
- Mod Manager doesn't identify itself as Editor anymore
- Fixed splash screen not showing banner art
- Mod Manager now shows for what game version (Volume) mod was made

# FrostyToolsuite
The most advanced modding platform for games running on DICE's Frostbite game engine.

## Setup

1. Download the source code.
2. Open the solution (found under FrostyEditor) with Visual Studio 2022, and make sure the project is set to ``Release - Final`` and ``x64``. Close out of retarget window if prompted.
3. Only build the projects themselves, never the solution.

## License
The Content, Name, Code, and all assets are licensed under a Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
