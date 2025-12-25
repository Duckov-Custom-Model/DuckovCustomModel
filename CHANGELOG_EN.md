# Changelog

English | [中文](CHANGELOG.md)

## v1.9.3

- Refactored AI character list management to support dynamic addition and replacement
  - Modified Core-side `AICharacters` class, changed static readonly collection to mutable `HashSet` to support dynamic character addition
  - Added `AddAICharacter` and `AddAICharacters` methods to support adding single or multiple characters dynamically
  - Added `Contains` method to check if a character is in the supported list
  - Created `AICharactersManager` manager on non-Core side to dynamically collect all AI characters from game's preset system
  - Retrieves all character presets from `GameplayDataSettings.CharacterRandomPresetData.presets` and automatically adds them to the supported list
- Added AI character model replacement warning
  - When selecting an AI character starting with `Character_` or the "All AI Characters" option in the target settings panel, a warning message is displayed

## v1.9.2

- Optimized UI scrollbar implementation
  - Unified use of UIFactory utility methods for creating and configuring scrollbars, improving code consistency
  - Added right-side scrollbars to TargetListScrollView, ModelListScrollView, and TargetSettingsScrollView
  - Added left-side scrollbar to SettingsScrollView
  - Added scrollbar to changelog scroll view
- Fixed UI layout issues
  - Fixed button width issue in TargetListPanel by adjusting content area padding to make room for scrollbar
  - Optimized scrollbar parent object setup logic to ensure scrollbars display correctly inside scroll views
  - Unified all RectTransform operations to use UIFactory utility methods, avoiding direct manipulation

## v1.9.1

- Refactored animator parameter update system using registration mechanism
  - Created `IAnimatorParameterUpdater` interface and `AnimatorParameterUpdaterManager` manager to unify animator parameter updater management
  - Split all updaters into independent classes, separated from `CustomAnimatorControl` for better code maintainability
  - Created `AnimatorUpdateContext` context class to unify data management for updates
- Added ShoulderSurfing mod extension support
  - Added `Mod:ShoulderSurfing:CameraPitch` animator parameter providing camera pitch value from ShoulderSurfing mod

## v1.8.13

- Added Buff-driven animator parameter configuration feature
  - Added `BuffAnimatorParams` field in `ModelInfo` of `bundleinfo.json` to configure animator parameters based on Buff status
  - Supports matching Buffs by Buff ID or DisplayNameKey, triggering parameters when any condition is met
  - Configured Buff parameters will be displayed in the debug interface, after custom parameters and animator parameters
  - Optimized animator parameter setting performance by caching valid parameter lists to avoid invalid calls for non-existent parameters

## v1.8.12

- Added model feature `SkipShowBackMaterial` to skip ShowBack material attachment logic in model definitions
  - Added `SkipShowBackMaterial` feature constant in `ModelFeatures`
  - Add `"SkipShowBackMaterial"` to the `Features` array in the model's `bundleinfo.json` to skip ShowBack material attachment logic

## v1.8.11

- Fixed cleanup logic when dropping carriable items
  - Changed Harmony Patch for `Carriable.Drop` from Postfix to Prefix to ensure proper cleanup before dropping
  - Automatically removes `CustomSocketMarker` and `DontHideAsEquipment` components when dropping items
  - Added `restore` parameter to `UnregisterCustomSocketObject` method, allowing items to not restore position when dropped

## v1.8.10

- Added model change event subscription functionality
  - Added `OnModelChanged` static event in `ModelListManager` for subscribing to model change notifications
  - Added `ModelChangedEventArgs` event argument class containing target type, model ID, model name, restoration status, success status, handler count, and other information
  - Automatically triggers events during model switching, restoration, and other operations, including both success and failure cases
  - Supports separately listening to model change events for characters, pets, and AI characters

## v1.8.9

- Added `ModelSoundStopTrigger` component, supporting stopping sound playback in animation state machines
  - Supports stopping sounds by specified event name
  - Supports stopping all currently playing sounds
  - Supports using built-in event names (e.g., `idle`) or custom trigger event names
  - Supports triggering stop operation on state enter or exit
  - Provides Unity editor custom interface with conditional display, warning prompts, and help information

## v1.8.8-fix1

- Fixed ModelParameterDriver parameter recording logic to resolve editor data locking issues and improve data conciseness

## v1.8.8

- Added version update checking functionality, automatically detects if a new version is available
- Added manual update check mechanism, can manually trigger update check in settings interface
- Added new version notification feature, displays notification information at version display when a new version is detected

## v1.8.7

- Added custom dialogue system, supporting dialogue triggering in animation state machines
  - Added `ModelDialogueTrigger` component that can trigger dialogue when animation state enters
  - Added `CustomDialogueManager` manager to uniformly manage dialogue loading and playback
  - Supports multilingual dialogue files, automatically loads corresponding files based on current language
  - Supports multiple dialogue playback modes: Sequential, Random, RandomNoRepeat, Continuous
- Added `ModelSoundTrigger` component, supporting direct sound effect triggering in animation state machines
  - Can play sound effects based on configured sound tags when animation state enters
  - Supports random or sequential selection from multiple sound tags
  - Supports configuring sound playback modes (Normal, StopPrevious, SkipIfPlaying, UseTempObject)
- Enhanced sound effect triggering functionality, added multiple combat-related sound tags
  - Added `trigger_on_hit_target`: triggers when hitting a target
  - Added `trigger_on_kill_target`: triggers when killing a target
  - Added `trigger_on_crit_hurt`: triggers when receiving critical damage
  - Added `trigger_on_crit_dead`: triggers when dying from critical damage
  - Added `trigger_on_crit_hit_target`: triggers when critically hitting a target
  - Added `trigger_on_crit_kill_target`: triggers when critically killing a target
- Enhanced animator control functionality, added multiple combat-related triggers
  - Added `Hurt`, `Dead`, `HitTarget`, `KillTarget` triggers
  - Added `CritHurt`, `CritDead`, `CritHitTarget`, `CritKillTarget` triggers
  - Added corresponding trigger methods in `CustomAnimatorControl`
- Enhanced `ModelHandler` sound effect triggering functionality
  - Supports determining whether damage is critical based on damage information, automatically triggering corresponding sound effects and animator triggers
  - Supports listening to global damage and death events, triggering sound effects and animator triggers when hitting or killing targets
  - Added sound playback probability configuration (`SoundTagPlayChance`), supporting playback probability configuration for different sound tags
- Optimized sound tag processing, removed tag validation restrictions, allowing use of any custom tags

## v1.8.6-fix1

- Fixed random integer generation logic in AnimatorParameterDriverManager
- Fixed random integer generation range in AnimatorParameterDriverManager to ensure maximum value is included
- Fixed range conversion logic for copy parameter operation
- Updated documentation comments for ModelParameterDriver and BlueprintID components to improve readability and understanding

## v1.8.6

- Added `ModelParameterDriver` component, supporting custom parameter control in animation state machines
  - Supports multiple parameter operation types:
    - `Set`: Directly set parameter value
    - `Add`: Add specified value to existing value
    - `Random`: Randomly set parameter value (supports range randomization and probability triggering)
    - `Copy`: Copy value from source parameter to target parameter (supports range conversion)
  - Supports all Animator parameter types (Float, Int, Bool, Trigger)
  - Automatically applies parameter driver when animation state enters
  - Supports parameter validation, ensuring target and source parameters exist before applying driver
- Added `AnimatorParameterDriverManager` manager to uniformly manage parameter driver initialization and application logic
- Enhanced animator parameter display functionality:
  - Added parameter caching mechanism to optimize parameter retrieval performance and reduce redundant calculations
  - Supports displaying external parameters defined in Animator controller, defaulting to the end of the list
- Added `BlueprintID` component for assigning unique identifiers to game objects (currently no actual functionality)
- Updated dependency version: `DuckovGameLibs` updated from 1.1.6-Steam to 1.2.5-Steam

## v1.8.5

- Improved audio playback system, unified use of `ModelHandler.PlaySound` method to manage all audio playback
- Added audio playback mode support (Normal, StopPrevious, SkipIfPlaying, UseTempObject), providing finer audio control
- Added audio instance management functionality, supporting stopping specific sounds or all sounds
- Improved sound interrupt mechanism:
  - Player key press triggers (F1 quack) and AI automatic triggers (`normal`, `surprise` tags) share the same interrupt group, newly played sounds will interrupt sounds playing in the same group
  - Footsteps have their own independent interrupt group, newly played footsteps will interrupt footsteps playing in the same group
  - Hurt sounds (`trigger_on_hurt`) skip if a hurt sound is already playing to avoid duplicate playback
  - Death sounds (`trigger_on_death`) stop all currently playing sounds before playing the death sound
- Improved `AudioUtils.PlayAudioWithTempObject` method to return `EventInstance` for better audio lifecycle management
- Removed unused `SoundTags.Death` constant, unified use of `trigger_on_death` tag

## v1.8.4

- Added footstep sound tag support, allowing custom footstep sounds for different materials (organic, mech, danger, no sound) and different states (light/heavy walk, light/heavy run)
- Added 16 footstep sound tags including `footstep_organic_walk_light`, `footstep_organic_walk_heavy`, `footstep_organic_run_light`, `footstep_organic_run_heavy`, etc.
- Added `AudioUtils` utility class for playing audio and automatically cleaning up temporary objects
- Improved death sound playback logic, using temporary objects to ensure correct audio playback and cleanup

## v1.8.3

- Fixed audio playback logic to ensure correct game object references are included when playing sounds

## v1.8.2

- Optimized UI component layout and initialization logic, simplified code using UIFactory methods, improved readability
- Improved event subscription cleanup logic to prevent memory leaks

## v1.8.1

- Added sound tag `trigger_on_hurt` for automatically playing sounds when a character is hurt
- Added `search_found_item_quality_xxx` tag series so different sounds can play after finishing a search based on the revealed item quality (supports none/white/green/blue/purple/orange/red/q7/q8)
- Improved event subscription cleanup logic to prevent memory leaks

## v1.8.0-fix1

- Fixed issue where multilingual loading failed due to changes in loading method

## v1.8.0

- Split some logic into different assemblies to facilitate future SDK development
- Adjusted loading logic, added additional Mod loading wrapper to reduce issues caused by Harmony loading order
- Under normal working conditions, it should no longer be necessary to consider whether this mod is loaded before Harmony

## v1.7.12

- Added sound tag `trigger_on_death` for automatically playing sound effects when a character dies
- Fixed issue where health bar height was not refreshed when switching models

## v1.7.11

- Added footstep sound frequency configuration feature, supports configuring walk and run footstep trigger frequency per model in `bundleinfo.json`
- Added `WalkSoundFrequency` field (optional), used to configure footstep trigger frequency per second when walking (if not specified, will automatically use the original character's walk footstep frequency setting)
- Added `RunSoundFrequency` field (optional), used to configure footstep trigger frequency per second when running (if not specified, will automatically use the original character's run footstep frequency setting)

## v1.7.10

- Added target switching feature to animator parameters window, can switch between viewing character's own or pet's animator parameters within the window
- Default view is character's own, can switch using two side-by-side buttons (Character/Pet)
- Automatically clears parameter state when switching targets, recalculating parameter changes from scratch

## v1.7.9

- Fixed issue where ModConfigs directory could not be created in read-only environments (such as macOS), causing functionality to fail
- When the installation directory is read-only, automatically switches to ModConfigs in the parent directory of the game save directory (Windows: `AppData\LocalLow\TeamSoda\Duckov\ModConfigs`, macOS/Linux: corresponding user data directory)

## v1.7.8-fix2

- Improved Harmony Patch application and removal mechanism, added fallback methods to improve fault tolerance, automatically tries fallback method when primary method fails

## v1.7.8

- Added animator parameters window hotkey feature, can configure a hotkey in the settings interface to toggle the animator parameters window
- Default value is no key, users need to actively set it in the settings interface
- Fixed the display determination logic for custom models; models now strictly follow the game's native view range logic for display switching

## v1.7.7

- Added "All AI Characters" target settings feature for uniformly setting default configuration items for all AI characters
- Optimized model lookup logic: prioritize individual settings, if not found then look for `*` settings, finally use original model
- Optimized some collision detection calculation logic when using custom models
- Fixed issue where headshot detection could not be triggered when character equipment was hidden
- Fixed issue where some slots would become invisible when switching models
- Fixed fallback logic for AI character configurations; audio, equipment hiding, and other settings now correctly select configuration based on the actually used model: if using fallback model (`*`), use `*` settings; if using own model configuration, use own settings

## v1.7.6

- Fixed the logic for obtaining the original model of character units; should now correctly match and process the original models for various character types
- Added occlusion display handling for character models; character models should now correctly display outlines when occluded
- Fixed issue where bundleinfo.json parsing errors would cause subsequent logic to be lost
- Now when a bundle's JSON file has format errors, it will log the error and skip that bundle, continuing to process other normal bundles
- Enhanced error handling during model loading to ensure errors in a single bundle do not affect overall functionality
- Fixed issue where error messages (such as loading errors, missing prefabs, etc.) were not correctly displayed in the model list interface
- Error messages are now correctly displayed in model list items, not just highlighted in red

## v1.7.5

- Added model audio toggle feature, can configure whether to use model-provided audio in target settings
- When disabled, all model audio (including key press triggers, AI automatic triggers, and idle audio) for the corresponding character will not play
- Supports separate configuration for characters, pets, and AI characters

## v1.7.4

- Optimized interface structure and logic to ensure proper display across all languages
- Added button to open model folder

## v1.7.3

- Added `Weather` Animator parameter (int type) to get current weather state (Sunny, Cloudy, Rainy, Stormy_I, Stormy_II)
- Added `Time` Animator parameter (float type) to get current 24-hour time
- Added `TimePhase` Animator parameter (int type) to get current time phase (day, dawn, night)
- When `TimeOfDayController.Instance` is unavailable, these three parameters will be set to -1

## v1.7.2

- Added InitialValue property to AnimatorParamInfo for storing parameter initial values
- Fixed initial value of ShootMode parameter (changed from 0 to -1)
- Enhanced animator parameter window display with parameter value change detection and color highlighting
  - Changed parameter values are displayed in yellow
  - Currently changing parameter values are displayed in orange
  - Unchanged parameters are displayed in white
- Adjusted animator parameter window height (increased from 800 to 1000) and font size (increased from 13 to 16)

## v1.7.1

- Modified animator parameter update logic to allow viewing animator parameter updates even when not using custom models (parameters are not applied to the animator)
- Changed CustomAnimatorControl to a persistent instance attached to the same object as ModelHandler, only changing the target Animator, avoiding frequent component creation and destruction
- Fixed issue where animator parameter window would not display when not using custom models

## v1.7.0

- Added a fixed settings button in the top-left corner of the screen that can be clicked to open/close the settings interface
- The settings button automatically appears when in the main menu or inventory interface

## v1.6.8

- Now automatically sets the layer of objects containing Renderer in the model to "Character"

## v1.6.7

- Fixed issue where hand-held equipment related parameters (such as `LeftHandEquip`, `RightHandEquip`, etc.) would not update correctly in certain situations
- Fixed issue where some Animator parameters (such as `GunState`, `WeaponInLocator`) would not display in the parameter window; now all defined parameters will display correctly

## v1.6.6

- Added `GunState` Animator parameter to indicate the current state of the gun (shoot cooling, ready, fire, burst each shot cooling, empty, reloading)

## v1.6.5

- Fixed the working logic of paper box locator and carriable locator

## v1.6.4

- Added `WeaponInLocator` Animator parameter to indicate the current locator type where the weapon is placed (left hand, right hand, or melee weapon)

## v1.6.3

- Added `DeathLootBoxPrefabPath` field to models for specifying death loot box style (optional, defaults to original model)
- Death loot box models can now be customized

## v1.6.2

- Removed idle audio feature restrictions for player characters; players can now enable automatic audio
- Added idle audio feature settings for each target character; each target can now be individually configured for automatic audio and audio intervals
- Refactored the model manager UI interface to improve user comfort, aesthetics, and structure

## v1.6.1

- Added idle audio automatic playback feature, supporting configuration of idle audio playback intervals for non-player characters (AI characters and pets)
- Added idle sound tag support; model sounds can be configured with "idle" tag for idle audio playback

## v1.6.0

- Optimized some logic processing
- Added paper box locator support; custom models can include `PaperBoxLocator` locator point, paper boxes will automatically attach to this locator and follow the model
- Hiding equipment no longer causes paper boxes to be hidden
- Added carriable item locator support; custom models can include `CarriableLocator` locator point, carriable items will automatically attach and save original position information when carried

## v1.5.1-fix2

- Fixed issue where audio settings were not correctly applied when reloading models

## v1.5.1-fix1

- Fixed issue where interface did not refresh when selecting models

## v1.5.1

- Added `AICharacter` target for AI characters that are not players or pets (NPCs are not supported as they are actually part of buildings, not conventional model structures)
- Added one-click reset invalid model settings button to quickly reset characters using incompatible or non-existent models back to default model
- Added audio playback related interfaces, supporting replacement of player quacks and AI automatic quacks

## v1.4.1

- Added extensive Animator parameter support to enhance animation control capabilities
- Added firearm-related parameters: `Loaded` (reload state), `Shoot` (shoot trigger), `ShootMode` (shoot mode), `AmmoRate` (ammo ratio)
- Added aiming-related parameters: `InAds` (aiming state), `AdsValue` (aiming progress), `AimType` (aiming type), `AimDirX/Y/Z` (aiming direction)
- Added action-related parameters: `ActionRunning` (action execution state), `ActionProgress` (action progress), `ActionPriority` (action priority)
- Added state-related parameters: `Hidden` (hidden state), `ThermalOn` (thermal imaging state), `VelocityX/Y/Z` (velocity components)
- Optimized some code logic and performance
- Added Animator parameter window to view supported Animator parameter values in real-time

## v1.2.0

- Refactored configuration system:
  - `UsingModel.json` changed to dictionary format
  - Added `HideEquipmentConfig.json` and removed related configurations from `UIConfig.json`
  - Supports automatic migration of old configurations
- Performance optimization: Implemented incremental update mechanism using runtime hash caching, only reloading when Bundle files or configurations change
- Multi-object support: Each ModelTarget can correspond to multiple game objects, uniformly applying changes to all objects when switching models
- Refresh process optimization: Skip Bundle loading and state checks when there are no changes, avoiding unnecessary repeated loading
- Temporary model restoration: Automatically temporarily switch back to original model when Bundle updates, automatically restore after update completes
- User selection protection: User model changes during refresh process will not be overwritten
- UI improvements: Model information displays Bundle name
- Fixed issue where models were loaded multiple times during refresh

## v1.1.2

- Optimized some logic
- Added `CurrentCharacterType` parameter to determine whether it applies to player character (0) or pet (1)

