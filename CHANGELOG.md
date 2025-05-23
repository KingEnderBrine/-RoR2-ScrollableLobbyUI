**1.10.3**

* Undo `1.10.2` since `HookGenPatcher` was fixed and updated cased breaking change for `SkinTuner`.

**1.10.2**

* Fixes for new `HookGenPatcher` version

**1.10.1**

* Fixed an issue where selecting skills/skins in a grid view would highlight wrong cell.
* Fixed grid view button extending past its boundaries.

**1.10.0**

* Added a button for rows in loadout that opens up all skills/skins in a grid for easier selection.
* Added a config option for the amount of skill/skins needed to show a button to open grid view.

**1.9.1**

* Added a config option for skills/skins row height.

**1.9.0**

* Fixes for `Seekers of the Storm` update.

**1.8.0**

* Added a config options for different scrolling in the character selection screen. (Implemented by `Zenithrium`)

**1.7.6**

* Fixed an issue where having the exact amount of survivors to fit in one page would create 2 pages instead of 1

**1.7.5**

* Rebuilt for game update `1.2.3.1`

**1.7.4**

* Fixed an issue where joining a lobby wouldn't select your last picked character

**1.7.3**

* Fixed cyclic dependency with `InLobbyConfig` resulting in mods not loading

**1.7.2**

* Added configuration for the amount of rows in the character select screen (2 by default, supports `InLobbyConfig`)
* Now arrows are not shown if all survivors can fit into a single page without arrows.

**1.7.1**

* Added dragging to loadout, overview and skills panels

**1.7.0**

* Fixes for `Survivors of the Void` update.
* The amount of characters on a single page now scales with the available space for them, which means you can have more than 14 survivors on wide monitors.

**1.6.3**

* Added scrolling for artifact selection pop-up.

**1.6.2**

* Added scrolling to survivor Overview panel. (Request by `tymmey`)

**1.6.1**

* Repackaged zip with lowercase `plugins` folder (to avoid a bug in `r2modman` for linux).

**1.6.0**

* Removed r2api dependency.

**1.5.1**

* Changed row arrows background. Now it's a blured out scene (like whole loadout panel) instead of black color.

**1.5.0**

* Added arrows to loadout rows that can be used istead of dragging.

**1.4.3**

* Fixed an issue where you couldn't look at skill description when using a gamepad.

**1.4.2**

* Added `SurvivorBlacklist` in `CharacterSelectBarControllerReplacement` if mod developers want to exclude some survivors from list

**1.4.1**

* Added support for `FloodWarning`.

**1.4.0**

* Added better scrolling to difficulty selection.

**1.3.3**

* Minor update to allow `RandomCharacterSelection` to open page with selected character.

**1.3.2**

* Removed ability to select characters in an eclipse lobby (This was not intended behavior)

**1.3.1**

* Fixed typo in icon (it was there from first release, lol)

**1.3.0**

* Fixed skills overview.
* Fixed `Eclipse` run character select paging.

**1.2.1**

* Added missing R2API submodule dependency

**1.2.0**

* Added horizontal scrolling to individual loadout rows. This will allow developers to add much more skills/skins, because you will be able to select them.

**1.1.0**

* Added pagination to character select. So you can add as many character mods as you want and be able to select them.

**1.0.0**

* Mod release.