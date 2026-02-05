# Duckov Custom Model

English | [中文](README.md)

[Changelog](CHANGELOG_EN.md)

## ⚠️ Redistribution Warning

If you plan to redistribute this mod, please note the following requirements:

1. **Remove loading validation**: You must remove the loading validation logic from the mod (located in the `OnAfterSetup` method in `DuckovCustomModel.ModLoader/ModBehaviour.cs`). This validation is used to prevent unauthorized redistribution. Please modify it according to your needs.
2. **Modify or remove update logic**: You must modify the update check source (located in `DuckovCustomModel/Managers/UpdateChecker.cs`), or completely remove the update checking functionality. Redistributed versions should not use this repository's data source for update checks.
3. **Adjust info.ini description text**: You must modify the `description` field in the `DuckovCustomModel/info.ini` file and adjust the description text to meet your distribution needs. The description text in this file contains warnings about unauthorized redistribution. Please modify or remove it according to your actual situation.
4. **Comply with MIT License requirements**: You must comply with the MIT License requirements specified in the [LICENSE](LICENSE) file, including retaining copyright notices and license text

A custom player model mod for Duckov game.

## Basic Features

- **Custom Model Replacement**: Allows players to use custom models to replace character models in the game
- **Model Selection Interface**: Provides a graphical interface for browsing and selecting available models
- **Model Search**: Supports searching models by name, ID, and other keywords
- **Model Management**: Automatically scans and loads model bundles, supports multiple model bundles simultaneously
- **Incremental Updates**: Uses hash caching mechanism to only update changed model bundles, improving refresh efficiency
- **Multi-Object Support**: Each model target type can correspond to multiple game objects, applying changes uniformly to all objects when switching
- **Quick Switch**: Supports quick model switching in-game without restarting

## Configuration Files

Configuration files are located at: `<Game Installation Path>/ModConfigs/DuckovCustomModel`

**⚠️ Important Note**: Starting from v1.10.0, all configuration files have been upgraded to v2, and many old format APIs have been marked as obsolete. The system will automatically migrate from old format to new format, but it is recommended that developers migrate to new APIs as soon as possible. For detailed obsolete API list and migration guide, please refer to [docs/OBSOLETE_APIS_v1.10.0_EN.md](docs/OBSOLETE_APIS_v1.10.0_EN.md)

**Note**: If the game installation directory is read-only (such as certain installation methods on macOS), the mod will automatically switch the configuration file path to ModConfigs in the parent directory of the game save directory (Windows: `AppData\LocalLow\TeamSoda\Duckov\ModConfigs\DuckovCustomModel`, macOS/Linux: corresponding user data directory). The mod will automatically detect and handle this situation without manual configuration.

### UIConfig.json

UI interface related configuration.

```json
{
  "ToggleKey": "Backslash",
  "AnimatorParamsToggleKey": "None",
  "EmotionModifierKey1": "Comma",
  "EmotionModifierKey2": "Period",
  "ShowDCMButton": true,
  "DCMButtonAnchor": "TopLeft",
  "DCMButtonOffsetX": 10.0,
  "DCMButtonOffsetY": -10.0
}
```

- `ToggleKey`: Key to open/close the model selection interface (default: `Backslash`, i.e., backslash key `\`)
  - Supported key values can refer to Unity KeyCode enum
- `AnimatorParamsToggleKey`: Key to open/close the animator parameters window (default: `None`, i.e., no key)
  - Users need to actively set it in the settings interface
  - Supported key values can refer to Unity KeyCode enum
  - When set to `None`, the hotkey feature will be disabled
- `EmotionModifierKey1`: Emotion shortcut modifier key 1 (default: `Comma`, i.e., comma key `,`)
  - Used for emotion shortcut functionality, hold this key + F1-F8 to set `EmotionValue1` parameter (value 0-7)
  - Supported key values can refer to Unity KeyCode enum
  - Can be set by clicking the button in the settings interface
- `EmotionModifierKey2`: Emotion shortcut modifier key 2 (default: `Period`, i.e., period key `.`)
  - Used for emotion shortcut functionality, hold this key + F1-F8 to set `EmotionValue2` parameter (value 0-7)
  - Supported key values can refer to Unity KeyCode enum
  - Can be set by clicking the button in the settings interface
- `ShowDCMButton`: Whether to show the DCM button in the main menu and inventory interface (default: `true`)
  - When set to `true`, the DCM button will automatically appear in the main menu or inventory interface
  - Can be toggled in the settings interface
- `DCMButtonAnchor`: Anchor position of the DCM button (default: `"TopLeft"`)
  - Valid values: `"TopLeft"`, `"TopCenter"`, `"TopRight"`, `"MiddleLeft"`, `"MiddleCenter"`, `"MiddleRight"`, `"BottomLeft"`, `"BottomCenter"`, `"BottomRight"`
  - Can be selected through a dropdown menu in the settings interface
- `DCMButtonOffsetX`: X-axis offset value of the DCM button (default: `10.0`)
  - X-axis offset relative to the anchor position (in pixels)
  - Can be set through an input field in the settings interface
- `DCMButtonOffsetY`: Y-axis offset value of the DCM button (default: `-10.0`)
  - Y-axis offset relative to the anchor position (in pixels)
  - Can be set through an input field in the settings interface

### HideEquipmentConfig.json

Hide equipment configuration. **⚠️ Upgraded to v2, old format is obsolete.**

```json
{
  "Version": 2,
  "TargetTypeHideEquipment": {
    "built-in:Character": false,
    "built-in:Pet": false,
    "built-in:AICharacter_*": false
  }
}
```

- `Version`: Configuration file version (currently `2`)
- `TargetTypeHideEquipment`: Dictionary type, where keys are target type IDs (string format, such as `"built-in:Character"`, `"built-in:Pet"`, `"built-in:AICharacter_*"` or `"built-in:AICharacter_<character name>"`), and values are boolean
  - `built-in:Character`: Whether to hide character's original equipment (default: `false`)
    - When set to `true`, the character model's Animator's `HideOriginalEquipment` parameter will be set to `true`
    - Can be toggled in the settings area of the model selection interface
  - `built-in:Pet`: Whether to hide pet's original equipment (default: `false`)
    - When set to `true`, the pet model's Animator's `HideOriginalEquipment` parameter will be set to `true`
    - Can be toggled in the settings area of the model selection interface
  - `built-in:AICharacter_*`: Default hide equipment setting for all AI characters
  - `built-in:AICharacter_<character name>`: Hide equipment setting for specific AI characters

**⚠️ Obsolete Format (v1)**:
- `HideEquipment` (Dictionary<ModelTarget, bool>) - Obsolete, use `TargetTypeHideEquipment` instead
- `HideAICharacterEquipment` (Dictionary<string, bool>) - Obsolete, use `TargetTypeHideEquipment` instead

**Compatibility Note**:
- The system will automatically migrate from v1 format to v2 format

### UsingModel.json

Current model configuration in use. **⚠️ Upgraded to v2, old format is obsolete.**

```json
{
  "Version": 2,
  "TargetTypeModelIDs": {
    "built-in:Character": "",
    "built-in:Pet": "",
    "built-in:AICharacter_*": "",
    "built-in:AICharacter_Cname_Wolf": "",
    "built-in:AICharacter_Cname_Scav": ""
  }
}
```

- `Version`: Configuration file version (currently `2`)
- `TargetTypeModelIDs`: Dictionary type, where keys are target type IDs (string format, such as `"built-in:Character"`, `"built-in:Pet"`, `"built-in:AICharacter_*"` or `"built-in:AICharacter_<character name>"`), and values are model IDs (string, uses original model when empty)
  - `built-in:Character`: Currently used character model ID
    - After setting, the game will automatically apply this model to all character objects when loading levels
    - Can be modified through the model selection interface, changes will be automatically saved to this file
  - `built-in:Pet`: Currently used pet model ID
    - After setting, the game will automatically apply this model to all pet objects when loading levels
    - Can be modified through the model selection interface, changes will be automatically saved to this file
  - `built-in:AICharacter_*`: Default model for all AI characters
    - When an AI character doesn't have an individual model configured, this default model will be used
    - If this key is also not configured, the original model will be used
  - `built-in:AICharacter_<character name>`: Model configuration for specific AI characters
    - Can configure models for each AI character individually
    - Can be modified through the model selection interface, changes will be automatically saved to this file

**⚠️ Obsolete Format (v1)**:
- `ModelIDs` (Dictionary<ModelTarget, string>) - Obsolete, use `TargetTypeModelIDs` instead
- `AICharacterModelIDs` (Dictionary<string, string>) - Obsolete, use `TargetTypeModelIDs` instead

**Compatibility Note**:
- The system will automatically migrate from v1 format to v2 format
- If old `ModelID` or `PetModelID` fields exist in the configuration file, the system will automatically migrate them to the new `TargetTypeModelIDs` dictionary format

### IdleAudioConfig.json

Idle audio automatic playback interval configuration. **⚠️ Upgraded to v2, old format is obsolete.**

```json
{
  "Version": 2,
  "TargetTypeIdleAudioIntervals": {
    "built-in:Character": { "Min": 30.0, "Max": 45.0 },
    "built-in:Pet": { "Min": 30.0, "Max": 45.0 },
    "built-in:AICharacter_*": { "Min": 30.0, "Max": 45.0 },
    "built-in:AICharacter_Cname_Wolf": { "Min": 20.0, "Max": 30.0 }
  },
  "TargetTypeEnableIdleAudio": {
    "built-in:Character": false,
    "built-in:Pet": true,
    "built-in:AICharacter_*": true
  }
}
```

- `Version`: Configuration file version (currently `2`)
- `TargetTypeIdleAudioIntervals`: Dictionary type, where keys are target type IDs (string format), and values are objects containing `Min` and `Max`
  - `built-in:Character`: Idle audio playback interval for player characters (in seconds, default: `Min: 30.0, Max: 45.0`)
  - `built-in:Pet`: Idle audio playback interval for pet characters (in seconds, default: `Min: 30.0, Max: 45.0`)
  - `built-in:AICharacter_*`: Default interval for all AI characters
  - `built-in:AICharacter_<character name>`: Interval configuration for specific AI characters
    - Can configure idle audio playback interval for each AI character individually
    - If not configured, the interval for `built-in:AICharacter_*` will be used
  - `Min`: Minimum interval time (default: `30.0`, cannot be less than `0.1`)
  - `Max`: Maximum interval time (default: `45.0`, cannot be less than `Min`)
  - The system will randomly select an interval time between the minimum and maximum values

- `TargetTypeEnableIdleAudio`: Dictionary type, where keys are target type IDs (string format), and values are boolean values that control whether the target type is allowed to automatically play idle audio
  - `built-in:Character`: Whether player characters are allowed to automatically play idle audio (default: `false`)
  - `built-in:Pet`: Whether pet characters are allowed to automatically play idle audio (default: `true`)
  - `built-in:AICharacter_*`: Default value for all AI characters (default: `true`)
  - `built-in:AICharacter_<character name>`: Configuration for specific AI characters
    - Can configure whether each AI character is allowed to automatically play idle audio individually
    - If not configured, the value for `built-in:AICharacter_*` will be used

**⚠️ Obsolete Format (v1)**:
- `IdleAudioIntervals` (Dictionary<ModelTarget, IdleAudioInterval>) - Obsolete, use `TargetTypeIdleAudioIntervals` instead
- `AICharacterIdleAudioIntervals` (Dictionary<string, IdleAudioInterval>) - Obsolete, use `TargetTypeIdleAudioIntervals` instead
- `EnableIdleAudio` (Dictionary<ModelTarget, bool>) - Obsolete, use `TargetTypeEnableIdleAudio` instead
- `AICharacterEnableIdleAudio` (Dictionary<string, bool>) - Obsolete, use `TargetTypeEnableIdleAudio` instead

**Notes**:
- Minimum interval time cannot be less than 0.1 seconds
- Maximum interval time cannot be less than minimum interval time
- Only models with `"idle"` tagged sounds will automatically play idle sounds
- Only target types with automatic playback enabled will automatically play idle sounds (controlled by `TargetTypeEnableIdleAudio` configuration)
- Player characters are not allowed to automatically play idle sounds by default, but can be enabled through configuration

### ModelAudioConfig.json

Model audio toggle configuration. **⚠️ Upgraded to v2, old format is obsolete.**

```json
{
  "Version": 2,
  "TargetTypeEnableModelAudio": {
    "built-in:Character": true,
    "built-in:Pet": true,
    "built-in:AICharacter_*": true
  },
  "TargetTypeModelAudioVolume": {
    "built-in:Character": 1.0,
    "built-in:Pet": 1.0,
    "built-in:AICharacter_*": 1.0,
    "built-in:AICharacter_Cname_Wolf": 0.8
  }
}
```

- `Version`: Configuration file version (currently `2`)
- `TargetTypeEnableModelAudio`: Dictionary type, where keys are target type IDs (string format, such as `"built-in:Character"`, `"built-in:Pet"`, `"built-in:AICharacter_*"` or `"built-in:AICharacter_<character name>"`), and values are boolean values that control whether the target type uses model audio
  - `built-in:Character`: Whether player characters use model audio (default: `true`)
    - When set to `false`, all model audio for player characters will not play (including key press triggers and idle audio)
    - Can be toggled in the target settings area of the model selection interface
  - `built-in:Pet`: Whether pet characters use model audio (default: `true`)
    - When set to `false`, all model audio for pet characters will not play (including AI automatic triggers and idle audio)
    - Can be toggled in the target settings area of the model selection interface
  - `built-in:AICharacter_*`: Default value for all AI characters (default: `true`)
  - `built-in:AICharacter_<character name>`: Configuration for specific AI characters
    - Can configure whether each AI character uses model audio individually
    - **Configuration Selection Logic**: Audio settings will be selected based on the actually used model
      - If the AI character uses its own model configuration (a model is individually configured for that AI character in `UsingModel.json`), the audio setting for that AI character will be used
      - If the AI character uses the fallback model (`*`, i.e., the default model for "all AI characters"), the audio setting for `*` will be used
    - Can be toggled in the target settings area of the model selection interface
- `TargetTypeModelAudioVolume`: Dictionary type, where keys are target type IDs (string format), and values are float numbers (0-1) that control the model audio volume for the target type
  - `built-in:Character`: Model audio volume for player characters (default: `1.0`, i.e., 100%)
    - Can be adjusted via slider in the target settings area of the model selection interface
  - `built-in:Pet`: Model audio volume for pet characters (default: `1.0`, i.e., 100%)
    - Can be adjusted via slider in the target settings area of the model selection interface
  - `built-in:AICharacter_*`: Default volume for all AI characters (default: `1.0`, i.e., 100%)
  - `built-in:AICharacter_<character name>`: Volume configuration for specific AI characters
    - Can configure volume for each AI character individually
    - If an AI character has no specific configuration, it will fall back to `built-in:AICharacter_*` configuration
    - Can be adjusted via slider in the target settings area of the model selection interface

**⚠️ Obsolete Format (v1)**:
- `EnableModelAudio` (Dictionary<ModelTarget, bool>) - Obsolete, use `TargetTypeEnableModelAudio` instead
- `AICharacterEnableModelAudio` (Dictionary<string, bool>) - Obsolete, use `TargetTypeEnableModelAudio` instead

**Notes**:
- When model audio is disabled, all model audio for the corresponding character will not play, including:
  - Player key press triggered audio (`"normal"` tag)
  - AI automatically triggered audio (`"normal"`, `"surprise"`, `"death"` tags)
  - Idle audio (`"idle"` tag)
- This configuration is independent from the `EnableIdleAudio` configuration in `IdleAudioConfig.json`:
  - `ModelAudioConfig.json` controls whether to use model audio (master switch)
  - `EnableIdleAudio` in `IdleAudioConfig.json` controls whether to allow automatic playback of idle audio (only affects automatic playback of idle audio)
  - If model audio is disabled in `ModelAudioConfig.json`, idle audio will not play even if `EnableIdleAudio` is `true`
    - If `"*"` is also not configured, default value will be used (`true`)
  - **Configuration Selection Logic**: Audio settings will select configuration based on the actually used model
    - If an AI character uses its own model configuration (a model is individually configured for that AI character in `UsingModel.json`), it will use that AI character's audio settings
    - If an AI character uses the fallback model (`*`, i.e., the default model for "All AI Characters"), it will use `*` audio settings
  - Default value: `true` (use model audio)
  - Can be toggled in the target settings area of the model selection interface

**Notes**:
- When model audio is disabled, all model audio for the corresponding character will not play, including:
  - Player key press triggered audio (`"normal"` tag)
  - AI automatic triggered audio (`"normal"`, `"surprise"`, `"death"` tags)
  - Idle audio (`"idle"` tag)
- This configuration is independent from the `EnableIdleAudio` configuration in `IdleAudioConfig.json`:
  - `ModelAudioConfig.json` controls whether to use model audio (master switch)
  - `EnableIdleAudio` in `IdleAudioConfig.json` controls whether to allow automatic playback of idle audio (only affects automatic playback of idle audio)
  - If model audio is disabled in `ModelAudioConfig.json`, idle audio will not play even if `EnableIdleAudio` is `true`

## Model Selection Interface

The model selection interface provides the following features:

- **Target Type Switching**: Switch between "Character", "Pet", and "AI Character" to manage character models, pet models, and AI character models separately
- **Model Browsing**: Scroll to view all available models (filtered based on the currently selected target type)
- **Model Search**: Quickly search models by name, ID, and other keywords
- **Model Selection**: Click the model button to apply the model to all objects of that target type
- **Model Information**: Each model card displays the model name, ID, author, version, and the Bundle name it belongs to
- **AI Character Selection**: When selecting "AI Character" target type, first select a specific AI character, then select a model for that character
  - **Settings Options**: In the settings tab, you can configure the following options:
  - **Hotkey**: Configure the hotkey to open/close the model selection interface
    - Can be set by clicking the button in the settings interface
  - **Animator Parameters Hotkey**: Configure the hotkey to open/close the animator parameters window
    - Default value is no key, users need to actively set it
    - Can be set by clicking the button in the settings interface
  - **Animator Parameters Display Interface**: Used to view and monitor Animator parameter values in real-time
    - **Opening Method**:
      - Click the "Show Animator Parameters" toggle in the settings interface, or use the configured hotkey to open
      - Press `ESC` key or click the close button in the top-right corner of the window to close the interface
    - **Character Switching**: Select different characters from the dropdown at the top to view their Animator parameters
      - The dropdown displays all available characters (including main character, pet, and AI characters)
      - Each character displays its name, hash value, and distance information
    - **Type Filtering**: Filter parameter types through the type filter dropdown
      - Supports multi-select filtering: `float`, `int`, `bool`, `trigger`
      - Multiple types can be selected simultaneously for filtering
    - **Usage Status Filtering**: Filter parameter usage status through the usage status filter dropdown
      - Supports multi-select filtering: `Used`, `Unused`
      - "Used" means the parameter is used in the Animator Controller
      - "Unused" means the parameter is not used in the Animator Controller
    - **Search Functionality**: Quickly find parameters through the search box
      - **Normal Search**: Directly enter keywords of parameter names, supports case-insensitive matching
        - By default, all input is treated as normal search
        - Example: Entering `Move` matches all parameters containing "Move" (such as "MoveSpeed", "MoveDirX", etc.)
      - **Regular Expression Search**: Use JavaScript-style regular expression format for more precise matching of parameter names
        - **Format**: Use `/pattern/` format to enable regular expression search
        - **Note**: Regular expression search is case-insensitive by default
        - **Examples**:
          - `/^Move.*/` matches all parameters starting with "Move" (such as "MoveSpeed", "MoveDirX")
          - `/.*Speed$/` matches all parameters ending with "Speed" (such as "MoveSpeed", "AmmoRate" won't match)
          - `/(Move|Run|Dash).*/` matches parameters containing "Move", "Run", or "Dash"
          - `/^[A-Z].*/` matches all parameters starting with an uppercase letter
        - **Note**: Regular expression search is only enabled when using `/pattern/` format, otherwise treated as normal search
      - Search filters the parameter list in real-time and can be combined with type filtering and usage status filtering
    - **Parameter Display**:
      - Parameters are displayed in a grid layout, showing each parameter's name, type, and current value
      - Parameter values update in real-time, showing the actual values in the Animator
      - Parameter colors change based on status:
        - **White**: Parameter value is the same as the initial value
        - **Yellow**: Parameter value differs from the initial value (changed)
        - **Orange**: Parameter value is currently changing
    - **Window Operations**:
      - Supports dragging the window title bar to move the window position
      - Supports dragging window edges to resize the window
      - Window size has a minimum limit (400x300 pixels)
  - **Emotion Shortcut Modifier Keys**: Configure two modifier keys for the emotion shortcut functionality
    - Modifier key 1 (default: Left Shift): Hold this key + F1-F8 to set `EmotionValue1` parameter (value 0-7)
    - Modifier key 2 (default: Right Shift): Hold this key + F1-F8 to set `EmotionValue2` parameter (value 0-7)
    - Can be set by clicking the button in the settings interface
    - Operation: Hold the modifier key, then press F1-F8 to set the corresponding emotion parameter value
  - **Hide Original Equipment**: Separate options for "Hide Character Equipment" and "Hide Pet Equipment"
    - These options are immediately saved to the configuration file
    - Affect the Animator's `HideOriginalEquipment` parameter value
  - **Show Animator Parameters**: Toggle whether to show the animator parameters window
  - **Show DCM Button in Main Menu and Inventory**: Control whether to show the DCM button when in the main menu or inventory interface
  - **DCM Button Position**: Configure the anchor position and offset values of the DCM button
    - Anchor Position: Select one of 9 positions through a dropdown menu (top-left, top-center, top-right, middle-left, middle-center, middle-right, bottom-left, bottom-center, bottom-right)
    - Offset Values: Set X and Y axis offset values (in pixels)
    - Configuration changes will be immediately applied to the button position
- **Target Settings**: In the target settings area, you can configure the following options (displayed based on the currently selected target type):
  - **Enable Model Audio**: Control whether to use model-provided audio
    - When disabled, all model audio for the corresponding character will not play (including key press triggers, AI automatic triggers, and idle audio)
    - Supports separate configuration for characters, pets, and AI characters
    - This option is immediately saved to `ModelAudioConfig.json`
  - **Enable Idle Audio**: Control whether to allow automatic playback of idle audio
    - This option is immediately saved to `IdleAudioConfig.json`
  - **Idle Audio Interval**: Configure the playback interval for idle audio (minimum and maximum values)
    - This option is immediately saved to `IdleAudioConfig.json`
  - **Hide Equipment**: Control whether to hide original equipment
    - This option is immediately saved to `HideEquipmentConfig.json`
    - Affects the Animator's `HideOriginalEquipment` parameter value
- **AI Character Equipment Settings**: When selecting "AI Character" target type and a specific AI character, a "Hide Equipment" toggle option for that AI character will be displayed at the top of the model list page
  - Each AI character has an independent hide equipment setting
  - This option is immediately saved to the configuration file
  - Affects the Animator's `HideOriginalEquipment` parameter value

### Opening the Model Selection Interface

- Default key: `\` (backslash key)
- Can be changed by modifying `ToggleKey` in `UIConfig.json`
- Press `ESC` key to close the interface
- **DCM Button**: When in the main menu or inventory interface, a fixed DCM button will appear on the screen (default position: top-left)
  - Click the button to quickly open/close the model selection interface
  - Button position and visibility can be configured in the settings interface
  - Button position supports 9 anchor positions (top-left, top-center, top-right, middle-left, middle-center, middle-right, bottom-left, bottom-center, bottom-right) and custom offset values
  - Configuration changes will be immediately applied to the button position without restarting the game

## Model Installation

Place model bundles at: `<Game Installation Path>/ModConfigs/DuckovCustomModel/Models`

**Note**: If the game installation directory is read-only, the mod will automatically switch the model path to ModConfigs in the parent directory of the game save directory (Windows: `AppData\LocalLow\TeamSoda\Duckov\ModConfigs\DuckovCustomModel\Models`, macOS/Linux: corresponding user data directory). The mod will automatically detect and handle this situation without manual configuration.

Each model bundle should be placed in a separate folder, containing model resource files and configuration information.

### Model Bundle Structure

Each model bundle folder should contain the following files:

```
Model Bundle Folder/
├── bundleinfo.json          # Model bundle configuration file (required)
├── modelbundle.assetbundle  # Unity AssetBundle file (required)
└── thumbnail.png            # Thumbnail file (optional)
```

### bundleinfo.json Format

```json
{
  "BundleName": "Model Bundle Name",
  "BundlePath": "modelbundle.assetbundle",
  "Models": [
    {
      "ModelID": "unique_model_id",
      "Name": "Model Display Name",
      "Author": "Author Name",
      "Description": "Model Description",
      "Version": "1.0.0",
      "ThumbnailPath": "thumbnail.png",
      "PrefabPath": "Assets/Model.prefab",
      "DeathLootBoxPrefabPath": "Assets/DeathLootBox.prefab",
      "TargetTypes": ["built-in:Character", "built-in:AICharacter_*", "built-in:AICharacter_Cname_Wolf", "built-in:AICharacter_Cname_Scav"],
      "CustomSounds": [
        {
          "Path": "sounds/normal1.wav",
          "Tags": ["normal"]
        },
        {
          "Path": "sounds/surprise.wav",
          "Tags": ["surprise", "normal"]
        },
        {
          "Path": "sounds/death.wav",
          "Tags": ["death"]
        },
        {
          "Path": "sounds/idle1.wav",
          "Tags": ["idle"]
        }
      ],
      "BuffAnimatorParams": {
        "HasBuff1": [
          { "Id": 123 },
          { "DisplayNameKey": "buff_key_1" }
        ],
        "HasBuff2": [
          { "Id": 456 }
        ]
      }
    }
  ]
}
```

#### Field Descriptions

**BundleName** (required): Model bundle name, used for identification and display

**BundlePath** (required): AssetBundle file path, relative to the model bundle folder

**Models** (required): Model information array, can contain multiple models

**ModelInfo Fields**:

- `ModelID` (required): Unique identifier for the model, used to reference the model in configuration files
- `Name` (optional): Name displayed in the interface
- `Author` (optional): Model author
- `Description` (optional): Model description information
- `Version` (optional): Model version number
- `ThumbnailPath` (optional): Thumbnail path, external file path relative to the model bundle folder (e.g., `"thumbnail.png"`)
- `PrefabPath` (required): Model Prefab resource path inside the AssetBundle (e.g., `"Assets/Model.prefab"`)
- `DeathLootBoxPrefabPath` (optional): Death loot box Prefab resource path inside the AssetBundle (e.g., `"Assets/DeathLootBox.prefab"`)
  - When a character using this model dies, if this field is configured, the death loot box will use the custom Prefab to replace the default model
  - If this field is not configured, the death loot box will use the default model
- `TargetTypes` (optional): Array of target type IDs the model applies to (default: `["built-in:Character"]`)
  - Uses string-format target type IDs, supporting built-in types and extension types
  - Built-in type examples: `"built-in:Character"` (character), `"built-in:Pet"` (pet), `"built-in:AICharacter_*"` (all AI characters), `"built-in:AICharacter_<character name>"` (specific AI character)
  - Extension type examples: `"extension:CustomType"` (custom type registered by third-party extensions)
  - Can contain multiple values, indicating the model is compatible with multiple target types
  - The model selection interface will filter and display compatible models based on the currently selected target type
  - **Examples**:
    - For character and all AI characters: `["built-in:Character", "built-in:AICharacter_*"]`
    - For specific AI characters: `["built-in:AICharacter_Cname_Wolf", "built-in:AICharacter_Cname_Scav"]`
    - For character, pet, and all AI characters: `["built-in:Character", "built-in:Pet", "built-in:AICharacter_*"]`

**⚠️ Obsolete Fields (obsolete since v1.10.0, but still supported for backward compatibility)**:
- `Target` (optional): Array of target types the model applies to (obsolete, use `TargetTypes` instead)
  - Valid values: `"Character"` (character), `"Pet"` (pet), `"AICharacter"` (AI character marker)
  - System will automatically migrate from `Target` and `SupportedAICharacters` to `TargetTypes`
  - **Note**: `"AICharacter"` is just a marker indicating that `SupportedAICharacters` needs to be processed, it is not converted to a target type itself
- `SupportedAICharacters` (optional): Array of supported AI character name keys (obsolete, use `TargetTypes` instead)
  - Only effective when `Target` contains `"AICharacter"`
  - Can specify which AI characters this model applies to
  - Special value `"*"`: Indicates the model applies to all AI characters
  - If empty array and `Target` contains `"AICharacter"`, the model will not be applied to any AI character
  - **Important**: If `Target` does not contain the `"AICharacter"` marker, even if `SupportedAICharacters` has values, they will not be processed
  - System will automatically convert `Target` and `SupportedAICharacters` to `TargetTypes` format (e.g., `"built-in:AICharacter_*"` or `"built-in:AICharacter_<character name>"`)
- `CustomSounds` (optional): Array of custom sound information, supports configuring tags for sounds
  - Each sound can be configured with multiple tags (`normal`, `surprise`, `death`)
  - Defaults to `["normal"]` when no tags are specified
  - The same sound file can be used for multiple scenarios
  - Sound file paths are specified in `Path`, relative to the model bundle folder
- `SoundTagPlayChance` (optional): Sound tag playback probability configuration
  - Dictionary type, where keys are sound tags (case-insensitive) and values are playback probabilities (0-100)
  - When a sound with this tag is triggered, whether it plays is determined by the configured probability
  - If not configured or probability is 0, always plays (default behavior)
- `WalkSoundFrequency` (optional): Footstep trigger frequency per second when walking
  - Used to control the playback frequency of footstep sounds when the character walks
  - If not specified, will automatically use the original character's walk footstep frequency setting
- `RunSoundFrequency` (optional): Footstep trigger frequency per second when running
  - Used to control the playback frequency of footstep sounds when the character runs
  - If not specified, will automatically use the original character's run footstep frequency setting
- `BuffAnimatorParams` (optional): Buff-driven animator parameter configuration
  - Dictionary type, key is animator parameter name (bool type), value is array of Buff matching conditions
  - When the character has matching Buffs, the corresponding animator parameter will be set to `true`, otherwise `false`
  - Each condition can specify `Id` (Buff ID) or `DisplayNameKey` (Buff display name key), satisfying any one condition is sufficient
  - Example configuration:
    ```json
    "BuffAnimatorParams": {
      "HasBuff1": [
        { "Id": 123 },
        { "DisplayNameKey": "buff_key_1" }
      ],
      "HasBuff2": [
        { "Id": 456 }
      ],
      "HasAnyBuff": [
        { "DisplayNameKey": "buff_key_1" },
        { "DisplayNameKey": "buff_key_2" }
      ]
    }
    ```
  - Configured Buff parameters will be displayed in the debug interface, after custom parameters and animator parameters

## Locator Points

To ensure that equipment (weapons, armor, backpacks, etc.) in the game can be correctly bound to custom models, the model Prefab needs to include corresponding locator point GameObjects.

### Required Locator Points

The model Prefab's child objects need to include Transforms with the following names as locator points:

- `LeftHandLocator`: Left hand locator point for binding left hand equipment
- `RightHandLocator`: Right hand locator point for binding right hand equipment
- `ArmorLocator`: Armor locator point for binding armor equipment
- `HelmetLocator`: Helmet locator point for binding helmet equipment
- `FaceLocator`: Face locator point for binding face equipment
- `BackpackLocator`: Backpack locator point for binding backpack equipment
- `MeleeWeaponLocator`: Melee weapon locator point for binding melee weapon equipment
- `PopTextLocator`: Pop text locator point for displaying pop text
- `VehicleLocator`: Vehicle locator point for indicating the player's position when riding a vehicle

### Optional Locator Points

In addition to the required locator points, models can also include the following optional locator points:

- `PaperBoxLocator`: Paper box locator point for binding paper boxes
  - When a custom model includes this locator point, paper boxes spawned in the game will automatically attach to this point
  - Paper boxes will follow the custom model's position and rotation
  - If the model does not include this locator point, paper boxes will use the original model's locator point
- `CarriableLocator`: Carriable item locator point for binding carriable items
  - When a custom model includes this locator point, carriable items will automatically attach to this point when the character carries them
  - Carriable items will follow the custom model's position and rotation
  - When carrying an item, the original position, rotation, and scale information are saved and will be restored when the item is dropped
  - If the model does not include this locator point, carriable items will use the original model's locator point

### Function of Locator Points

- The mod automatically searches for these locator points in custom models
- Found locator points will be set as binding points for the game's equipment system
- Equipment will be bound according to the position and rotation of the locator points
- If a locator point does not exist, the corresponding equipment will not display correctly on the custom model

### Notes

- Locator point names must **exactly match** (case-sensitive)
- Locator points can be empty GameObjects, just need to set the correct position and rotation
- It is recommended to set locator point positions based on the original model's equipment positions

## Animator Configuration

Custom model Prefabs need to include an `Animator` component and configure the corresponding Animator Controller.

### Animator Controller Parameters

The Animator Controller can use the following parameters:

#### Bool Type Parameters

- `Grounded`: Whether the character is on the ground
- `Die`: Whether the character is dead
- `Moving`: Whether the character is moving
- `Running`: Whether the character is running
- `Dashing`: Whether the character is dashing
- `GunReady`: Whether the gun is ready
- `Loaded`: Whether the gun is loaded (updated by `OnLoadedEvent` when holding `ItemAgent_Gun`)
- `Reloading`: Whether reloading
- `RightHandOut`: Whether the right hand is extended
- `ActionRunning`: Whether an action is currently running (determined by `CharacterMainControl.CurrentAction`)
- `Hidden`: Whether the character is in hidden state
- `ThermalOn`: Whether thermal imaging is enabled
- `InAds`: Whether aiming down sights (ADS)
- `HideOriginalEquipment`: Whether to hide original equipment (controlled by the corresponding target type ID configuration in `HideEquipmentConfig.json`)
- `LeftHandEquip`: Whether there is equipment in the left hand slot (determined by equipment TypeID, `true` when TypeID > 0)
- `RightHandEquip`: Whether there is equipment in the right hand slot (determined by equipment TypeID, `true` when TypeID > 0)
- `ArmorEquip`: Whether there is equipment in the armor slot (determined by equipment TypeID, `true` when TypeID > 0)
- `HelmetEquip`: Whether there is equipment in the helmet slot (determined by equipment TypeID, `true` when TypeID > 0)
- `HeadsetEquip`: Whether there is equipment in the headset slot (determined by equipment TypeID, `true` when TypeID > 0)
- `FaceEquip`: Whether there is equipment in the face slot (determined by equipment TypeID, `true` when TypeID > 0)
- `BackpackEquip`: Whether there is equipment in the backpack slot (determined by equipment TypeID, `true` when TypeID > 0)
- `MeleeWeaponEquip`: Whether there is equipment in the melee weapon slot (determined by equipment TypeID, `true` when TypeID > 0)
- `HavePopText`: Whether there is pop text (checks if the pop text slot has child objects)
- `Sleeping`: Whether the character is in sleeping state
- `IsVehicle`: Whether the character is a vehicle
- `IsControllingOtherCharacter`: Whether the character is controlling another character
- `IsControllingVehicle`: Whether the character is controlling a vehicle (when `true`, `IsControllingOtherCharacter` is always `true`)
- `IsPlayerControlling`: Whether the player is currently controlling this character.

#### Float Type Parameters

- `MoveSpeed`: Movement speed ratio (normal walk 0~1, running can reach 2)
- `MoveDirX`: Movement direction X component (-1.0 ~ 1.0, character local coordinate system)
- `MoveDirY`: Movement direction Y component (-1.0 ~ 1.0, character local coordinate system)
- `VelocityX`: Velocity X component
- `VelocityY`: Velocity Y component
- `VelocityZ`: Velocity Z component
- `VelocityMagnitude`: Velocity magnitude (length of velocity vector)
- `AimDirX`: Aim direction X component
- `AimDirY`: Aim direction Y component
- `AimDirZ`: Aim direction Z component
- `AdsValue`: Aim down sights value (0.0 - 1.0, aiming progress)
- `AmmoRate`: Ammo ratio (0.0 - 1.0, current ammo / max ammo capacity)
- `HealthRate`: Health ratio (0.0 - 1.0, current health / max health)
- `WaterRate`: Water ratio (0.0 - 1.0, current water / max water)
- `WeightRate`: Weight ratio (current total weight / max carrying capacity, may exceed 1.0)
- `ActionProgress`: Action progress percentage (0.0 - 1.0, current action progress, obtained from `IProgress.GetProgress().progress`)
- `Time`: Current 24-hour time (0.0 - 24.0, obtained from `TimeOfDayController.Instance.Time`, -1.0 when unavailable)
- `Mod:ShoulderSurfing:CameraPitch`: Camera pitch value from ShoulderSurfing mod (requires ShoulderSurfing mod to be installed, defaults to 0.0 when not installed)

#### Int Type Parameters

- `CurrentCharacterType`: Current character type
  - `0`: Character
  - `1`: Pet
  - `2`: AI Character (AICharacter)
  - `-1`: Custom type (Extension, target types registered by third-party extensions)
- `CustomCharacterTypeID`: Custom type ID (only effective when `CurrentCharacterType` is `-1`)
  - Hash value generated from `targetTypeId` string, used to uniquely identify custom target types
  - When `CurrentCharacterType` is not `-1`, this parameter value is `0`
- `HandState`: Hand state
  - `0`: Default state
  - `1`: Normal
  - `2`: Gun
  - `3`: Melee weapon
  - `4`: Bow
  - `-1`: Carrying state
- `ShootMode`: Shoot mode (determined by gun's `triggerMode` when holding `ItemAgent_Gun`)
  - `0`: Auto
  - `1`: Semi-automatic
  - `2`: Bolt-action
- `GunState`: Gun state (determined by gun's `GunState` when holding `ItemAgent_Gun`)
  - `0`: Shoot cooling (shootCooling)
  - `1`: Ready
  - `2`: Fire
  - `3`: Burst each shot cooling (burstEachShotCooling)
  - `4`: Empty
  - `5`: Reloading
- `AimType`: Aim type (determined by `CharacterMainControl.AimType`)
  - `0`: Normal aim
  - `1`: Character skill
  - `2`: Handheld skill
- `WeightState`: Weight state (only effective in Raid maps)
  - `0`: Light (WeightRate ≤ 0.25)
  - `1`: Normal (0.25 < WeightRate ≤ 0.75)
  - `2`: Heavy (0.75 < WeightRate ≤ 1.0)
  - `3`: Overloaded (WeightRate > 1.0)
- `WeaponInLocator`: Current locator type where the weapon is placed (0 when no weapon)
  - `0`: No weapon
  - `1`: Right hand locator (`normalHandheld`)
  - `2`: Melee weapon locator (`meleeWeapon`)
  - `3`: Left hand locator (`leftHandSocket`)
  - When weapon type is left hand but the model doesn't have a left hand locator, it will automatically use the right hand locator (value is `1`)
- `LeftHandTypeID`: TypeID of equipment in left hand (0 when no equipment)
- `RightHandTypeID`: TypeID of equipment in right hand (0 when no equipment)
- `ArmorTypeID`: TypeID of armor equipment (0 when no equipment)
- `HelmetTypeID`: TypeID of helmet equipment (0 when no equipment)
- `HeadsetTypeID`: TypeID of headset equipment (0 when no equipment)
- `FaceTypeID`: TypeID of face equipment (0 when no equipment)
- `BackpackTypeID`: TypeID of backpack equipment (0 when no equipment)
- `MeleeWeaponTypeID`: TypeID of melee weapon equipment (0 when no equipment)
- `ActionPriority`: Action priority (determined by `CharacterMainControl.CurrentAction.ActionPriority()`)
  - `0`: Whatever
  - `1`: Reload
  - `2`: Attack
  - `3`: usingItem
  - `4`: Dash
  - `5`: Skills
  - `6`: Fishing
  - `7`: Interact
  - When `ActionRunning` is `true`, the action priority can be used to approximately determine what action the character is performing
- `ActionType`: Action type ID (defined by `CharacterActionDefinitions`, -1 when no action)
  - `1`: Action_Fishing
  - `2`: Action_FishingV2
  - `3`: CA_Attack
  - `4`: CA_Carry
  - `5`: CA_Dash
  - `6`: CA_Interact
  - `7`: CA_Reload
  - `8`: CA_Skill
  - `9`: CA_UseItem
  - `10`: CA_ControlOtherCharacter (control other character)
  - When `ActionRunning` is `true`, the action type can precisely determine what action type the character is performing
  - The action type definition library supports extensions, new action types can be registered via `CharacterActionDefinitions.RegisterActionType<T>(id)`
- `ActionFishingRodTypeID`: TypeID of fishing rod used in fishing action (only valid when `ActionType` is `1` or `2`, otherwise `0`)
- `ActionBaitTypeID`: TypeID of bait used in fishing action (only valid when `ActionType` is `1` or `2`, otherwise `0`)
- `ActionUseItemTypeID`: TypeID of item used in use item action (only valid when `ActionType` is `9`, otherwise `0`)
- `Weather`: Current weather state (obtained from `TimeOfDayController.Instance.CurrentWeather`, -1 when unavailable)
  - `0`: Sunny
  - `1`: Cloudy
  - `2`: Rainy
  - `3`: Stormy_I
  - `4`: Stormy_II
- `TimePhase`: Current time phase (obtained from `TimeOfDayController.Instance.CurrentPhase.timePhaseTag`, -1 when unavailable)
  - `0`: Day
  - `1`: Dawn
  - `2`: Night
- `EmotionValue1`: Emotion parameter value 1 (int type, initial value 0)
  - Can be set via emotion shortcut functionality: Hold modifier key 1 (default comma key `,`) + F1-F8 to set (value 0-7)
  - Modifier key 1 can be configured in the settings interface (`EmotionModifierKey1`)
- `EmotionValue2`: Emotion parameter value 2 (int type, initial value 0)
  - Can be set via emotion shortcut functionality: Hold modifier key 2 (default period key `.`) + F1-F8 to set (value 0-7)
  - Modifier key 2 can be configured in the settings interface (`EmotionModifierKey2`)
- `RidingVehicleType`: The type of vehicle the character is riding (int type, initial value 0)

#### Mod Extension Parameters

The mod supports adding additional animator parameters through extension modules. These parameters use the `Mod:ExtensionName:ParameterName` naming format with colon separators to clearly identify them as mod extension parameters:

- `Mod:ShoulderSurfing:CameraPitch`: Camera pitch value from ShoulderSurfing mod (float type)
  - Requires ShoulderSurfing mod to be installed
  - When ShoulderSurfing mod is not installed or not active, the parameter value is `0.0`
  - Parameter value reflects the current camera pitch angle in real-time

#### Trigger Type Parameters

- `Attack`: Attack trigger (used to trigger melee attack animations)
- `Shoot`: Shoot trigger (triggered by `OnShootEvent` when holding `ItemAgent_Gun`)
- `Hurt`: Hurt trigger (automatically triggered when the character takes damage)
- `Dead`: Death trigger (automatically triggered when the character dies)
- `HitTarget`: Hit target trigger (automatically triggered when the character hits a target)
- `KillTarget`: Kill target trigger (automatically triggered when the character kills a target)
- `CritHurt`: Critical hurt trigger (automatically triggered when the character takes critical damage)
- `CritDead`: Critical death trigger (automatically triggered when the character dies from critical damage)
- `CritHitTarget`: Critical hit target trigger (automatically triggered when the character critically hits a target)
- `CritKillTarget`: Critical kill target trigger (automatically triggered when the character critically kills a target)

### Optional Animation Layer

If the model contains melee attack animations, you can add an animation layer named `"MeleeAttack"`:

- Layer name must be `"MeleeAttack"`
- This layer is used to play melee attack animations
- Layer weight will be automatically adjusted based on attack state

### Animator State Machine Behaviour Components

The mod provides three state machine behaviour components that can trigger sounds, dialogue, or control parameters when animation states are entered:

#### ModelParameterDriver

Automatically controls Animator parameters when animation states are entered, similar to Unity's built-in Animator Parameter Driver.

- `parameters`: Array of parameter operations, can configure multiple parameter operations
  - `type`: Operation type
    - `Set`: Directly set parameter value
    - `Add`: Add specified value to existing value
    - `Random`: Randomly set parameter value (supports range randomization and probability triggering)
    - `Copy`: Copy value from source parameter to target parameter (supports range conversion)
  - `name`: Target parameter name (parameter to be written to)
  - `source`: Source parameter name (for Copy operation, parameter to be read from)
  - `value`: Value used for the operation (for Set and Add operations)
  - `valueMin` / `valueMax`: Minimum and maximum values for randomization (for Random operation)
  - `chance`: Trigger probability (0.0 - 1.0), controls whether the operation executes
  - `convertRange`: Whether to perform range conversion (for Copy operation)
  - `sourceMin` / `sourceMax`: Source parameter range (for Copy operation range conversion)
  - `destMin` / `destMax`: Target parameter range (for Copy operation range conversion)
- `debugString`: Debug message (optional), will be output in logs for debugging
- Supports all Animator parameter types (Float, Int, Bool, Trigger)
- Automatically applies parameter driver when animation state enters
- Supports parameter validation, ensuring target and source parameters exist before applying driver

#### ModelSoundTrigger

Triggers sound effect playback when animation state enters.

- `soundTags`: Array of sound tags, can configure multiple tags
- `playOrder`: Tag selection method (Random: random selection, Sequential: sequential selection)
- `playMode`: Sound playback mode (Normal, StopPrevious, SkipIfPlaying, UseTempObject)
- `eventName`: Event name for sound playback management (optional, uses default name if empty)

#### ModelSoundStopTrigger

Stops sound effect playback when animation state enters or exits.

- `stopAllSounds`: Whether to stop all currently playing sounds (true: stop all, false: stop sound by specified event name)
- `useBuiltInEventName`: Whether to use built-in event name (true: use built-in event name like `idle` directly, false: use custom trigger event name)
- `eventName`: Event name
  - When `stopAllSounds` is false and `useBuiltInEventName` is false: Custom trigger event name (optional, uses default name `CustomModelSoundTrigger` if empty)
  - When `stopAllSounds` is false and `useBuiltInEventName` is true: Built-in event name (required, e.g., `idle`)
- `stopOnEnter`: Whether to stop on state enter (true: stop on enter, false: stop on exit)

**Notes**:
- When `useBuiltInEventName` is true, `eventName` must be specified, otherwise a warning will be displayed
- Custom trigger event names use the format `CustomModelSoundTrigger:{eventName}`, consistent with `ModelSoundTrigger`
- Built-in event names (e.g., `idle`) are used directly without prefix

#### ModelDialogueTrigger

Triggers dialogue playback when animation state enters.

- `fileName`: Dialogue definition file name (without extension)
- `dialogueId`: Dialogue ID, corresponding to the dialogue ID in the dialogue configuration file
- `defaultLanguage`: Default language, used when the current language file is missing

### Animator Workflow

1. The mod automatically reads game state and updates Animator parameters
2. Movement, jumping, dashing, and other states are synchronized in real-time to the custom model's Animator
3. Actions like attacking and reloading trigger corresponding animation parameters
4. If the `MeleeAttack` layer exists, its weight will be automatically adjusted during attacks to play attack animations
5. When the character holds `ItemAgent_Gun`, the mod automatically subscribes to the gun's `OnShootEvent` and `OnLoadedEvent` events
   - `OnShootEvent`: Sets the `Shoot` trigger when triggered
   - `OnLoadedEvent`: Updates the `Loaded` boolean value when triggered
6. When the character switches held items (`OnHoldAgentChanged` event), related subscriptions are automatically updated
7. Combat-related triggers are automatically triggered when corresponding events occur:
   - Hurt/Death: Automatically triggers `Hurt`/`Dead` or `CritHurt`/`CritDead` when the character takes damage or dies (based on whether it's a critical hit)
   - Hit/Kill: Automatically triggers `HitTarget`/`KillTarget` or `CritHitTarget`/`CritKillTarget` when the character hits or kills a target (based on whether it's a critical hit)

## Custom Sounds

The mod supports configuring sounds for custom models, including both player key press triggers and AI automatic triggers.

### Sound Configuration

Sounds can be configured in `ModelInfo` within `bundleinfo.json`:

```json
{
  "ModelID": "unique_model_id",
  "Name": "Model Display Name",
  "CustomSounds": [
    {
      "Path": "sounds/normal1.wav",
      "Tags": ["normal"]
    },
    {
      "Path": "sounds/normal2.wav",
      "Tags": ["normal"]
    },
    {
      "Path": "sounds/surprise.wav",
      "Tags": ["surprise", "normal"]
    },
    {
      "Path": "sounds/death.wav",
      "Tags": ["trigger_on_death"]
    }
  ]
}
```

#### SoundInfo Field Descriptions

- `Path` (required): Sound file path, relative to the model bundle folder
- `Tags` (optional): Array of sound tags, used to specify sound usage scenarios
  - `"normal"`: Normal sound, used for player key press triggers (F1 quack) and AI automatic triggers (normal state and surprise state)
  - `"surprise"`: Surprise sound, used for AI surprise state (shares the same interrupt group with `"normal"` tag)
  - `"idle"`: Idle sound, used for automatic playback by characters (can be controlled through configuration to determine which character types are allowed to automatically play)
  - `"trigger_on_hurt"`: Hurt trigger sound, automatically plays when the character takes damage (skips if a hurt sound is already playing)
  - `"trigger_on_death"`: Death trigger sound, automatically plays when the character dies (stops all currently playing sounds before playing)
  - `"trigger_on_hit_target"`: Hit target trigger sound, automatically plays when the character hits a target (skips if a hit sound is already playing)
  - `"trigger_on_kill_target"`: Kill target trigger sound, automatically plays when the character kills a target (skips if a kill sound is already playing)
  - `"trigger_on_crit_hurt"`: Critical hurt trigger sound, automatically plays when the character takes critical damage (skips if a hurt sound is already playing)
  - `"trigger_on_crit_dead"`: Critical death trigger sound, automatically plays when the character dies from critical damage (stops all currently playing sounds before playing)
  - `"trigger_on_crit_hit_target"`: Critical hit target trigger sound, automatically plays when the character critically hits a target (skips if a hit sound is already playing)
  - `"trigger_on_crit_kill_target"`: Critical kill target trigger sound, automatically plays when the character critically kills a target (skips if a kill sound is already playing)
  - `"search_found_item_quality_xxx"`: Plays when a searched item of the specified quality is revealed; `xxx` can be `none`, `white`, `green`, `blue`, `purple`, `orange`, `red`, `q7`, or `q8`
  - `"footstep_organic_walk_light"`, `"footstep_organic_walk_heavy"`, `"footstep_organic_run_light"`, `"footstep_organic_run_heavy"`: Organic material footstep sounds (light/heavy walk, light/heavy run)
  - `"footstep_mech_walk_light"`, `"footstep_mech_walk_heavy"`, `"footstep_mech_run_light"`, `"footstep_mech_run_heavy"`: Mechanical material footstep sounds (light/heavy walk, light/heavy run)
  - `"footstep_danger_walk_light"`, `"footstep_danger_walk_heavy"`, `"footstep_danger_run_light"`, `"footstep_danger_run_heavy"`: Danger material footstep sounds (light/heavy walk, light/heavy run)
  - `"footstep_nosound_walk_light"`, `"footstep_nosound_walk_heavy"`, `"footstep_nosound_run_light"`, `"footstep_nosound_run_heavy"`: No sound material footstep sounds (light/heavy walk, light/heavy run)
  - Can contain multiple tags, indicating the sound can be used in multiple scenarios
  - Defaults to `["normal"]` when no tags are specified


### Sound Trigger Methods

#### Player Key Press Trigger

- When a character model has sounds configured, pressing the `Quack` key (F1) in-game will trigger a sound
- Only sounds tagged with `"normal"` will be played
- Randomly selects one sound from all sounds tagged with `"normal"`
- Only player characters respond to key presses, pets do not trigger
- When playing a sound, it also creates an AI sound, allowing other AIs to hear the player's sound
- **Sound Interrupt Mechanism**: Player key press triggered sounds share the same interrupt group with AI automatic triggered sounds (`"normal"` and `"surprise"` tags), newly played sounds will interrupt sounds playing in the same group

#### AI Automatic Trigger

- AI will automatically trigger sounds with corresponding tags based on game state
- `"normal"`: Triggered during AI normal state
- `"surprise"`: Triggered during AI surprise state
- **Sound Interrupt Mechanism**: AI automatic triggered sounds (`"normal"` and `"surprise"` tags) share the same interrupt group with player key press triggered sounds, newly played sounds will interrupt sounds playing in the same group
- `"trigger_on_hurt"`: Automatically plays when the character takes damage (applies to all character types)
  - **Sound Interrupt Mechanism**: If a hurt sound is already playing, the new hurt sound will be skipped to avoid duplicate playback
- `"trigger_on_hit_target"`: Automatically plays when the character hits a target (applies to all character types)
  - **Sound Interrupt Mechanism**: If a hit sound is already playing, the new hit sound will be skipped to avoid duplicate playback
- `"trigger_on_kill_target"`: Automatically plays when the character kills a target (applies to all character types)
  - **Sound Interrupt Mechanism**: If a kill sound is already playing, the new kill sound will be skipped to avoid duplicate playback
- `"trigger_on_crit_hurt"`: Automatically plays when the character takes critical damage (applies to all character types)
  - **Sound Interrupt Mechanism**: If a hurt sound is already playing, the new hurt sound will be skipped to avoid duplicate playback
- `"trigger_on_crit_dead"`: Automatically plays when the character dies from critical damage (applies to all character types)
  - **Sound Interrupt Mechanism**: Before playing the death sound, all currently playing sounds will be stopped, then the death sound will play
- `"trigger_on_crit_hit_target"`: Automatically plays when the character critically hits a target (applies to all character types)
  - **Sound Interrupt Mechanism**: If a hit sound is already playing, the new hit sound will be skipped to avoid duplicate playback
- `"trigger_on_crit_kill_target"`: Automatically plays when the character critically kills a target (applies to all character types)
  - **Sound Interrupt Mechanism**: If a kill sound is already playing, the new kill sound will be skipped to avoid duplicate playback
- `"idle"`: Characters with automatic playback enabled will automatically play idle sounds at random intervals
  - Play interval can be configured in `IdleAudioConfig.json`
  - Default interval is 30-45 seconds (random)
  - Will not play when the character is dead
  - Which character types are allowed to automatically play can be controlled through `EnableIdleAudio` and `AICharacterEnableIdleAudio` configurations
  - By default, AI characters and pets are allowed to automatically play, while player characters are not (can be enabled through configuration)
- `"trigger_on_death"`: Automatically plays when the character dies (applies to all character types)
  - **Sound Interrupt Mechanism**: Before playing the death sound, all currently playing sounds will be stopped, then the death sound will play
- If a sound with the specified tag doesn't exist, the original game event will be used (no fallback to other tags)

#### Footstep Trigger

- Footstep sounds are automatically triggered based on ground material and movement state when characters move
- Supports four ground materials: organic, mech, danger, and no sound
- Supports four movement states: light walk (walkLight), heavy walk (walkHeavy), light run (runLight), and heavy run (runHeavy)
- The system automatically selects the corresponding sound tag based on the character's `footStepMaterialType` and `FootStepTypes`
- **Sound Interrupt Mechanism**: Footsteps have their own independent interrupt group, newly played footsteps will interrupt footsteps playing in the same group
- If the model doesn't have footstep sounds configured for the corresponding material and state, vanilla footstep sounds will be used

#### Search Discovery Trigger

- When the player finishes searching or inspecting an item and its quality is revealed, the corresponding sound tag is triggered
- Use `search_found_item_quality_xxx`, where `xxx` matches the same quality suffix listed in `Tags`: `none`, `white`, `green`, `blue`, `purple`, `orange`, `red`, `q7`, `q8`
- If no sound is configured for that quality, nothing plays and the vanilla behavior remains unchanged

#### Animation State Machine Trigger

- Can use `ModelSoundTrigger` component in animation state machines to trigger sounds when states are entered
  - Supports configuring multiple sound tags, can choose random or sequential playback
  - Supports configuring sound playback modes for finer audio control
  - Sound tags can be any custom tags, no longer restricted to predefined tags
- Can use `ModelSoundStopTrigger` component in animation state machines to stop sound playback
  - Supports stopping sounds by specified event name (custom triggers or built-in event names like `idle`)
  - Supports stopping all currently playing sounds
  - Supports triggering stop operation on state enter or exit
  - Provides user-friendly configuration interface in Unity editor with conditional display and warning prompts

### Sound File Requirements

- Sound files should be placed inside the model bundle folder
- Supports audio formats used by the game (typically WAV, OGG, etc.)
- Sound file paths are specified in `Path`, relative to the model bundle folder
- Example: If the model bundle folder is `MyModel/` and the sound file is `MyModel/sounds/voice.wav`, then `Path` should be set to `"sounds/voice.wav"`

### Sound Playback Probability Configuration

In `ModelInfo` within `bundleinfo.json`, you can configure playback probability for sound tags:

```json
{
  "ModelID": "unique_model_id",
  "Name": "Model Display Name",
  "SoundTagPlayChance": {
    "trigger_on_hurt": 50.0,
    "trigger_on_hit_target": 30.0
  }
}
```

- `SoundTagPlayChance` (optional): Dictionary type, where keys are sound tags (case-insensitive) and values are playback probabilities (0-100)
  - Probability values are automatically converted to floats between 0-1 (divided by 100)
  - When a sound with this tag is triggered, whether it plays is determined by the configured probability
  - If not configured, always plays (default behavior)
  - If probability is 0, never plays
  - If probability is less than 100, there's a chance the sound won't play

### Notes

- If a model has no sounds configured, it will not affect other functionality
- Sound tags are no longer restricted to predefined tags, any custom tags can be used
- Custom tags can be triggered through the `ModelSoundTrigger` component in animation state machines

## Custom Dialogue

The mod supports configuring dialogue for custom models, allowing dialogue bubbles to be displayed when triggered in animation state machines.

### Dialogue Configuration

Dialogue configuration files should be placed inside the model bundle folder, with file naming format: `{filename}_{language}.json`

Examples:
- `dialogue_English.json`: English dialogue file
- `dialogue_Chinese.json`: Chinese dialogue file

Dialogue configuration file format:

```json
[
  {
    "Id": "dialogue_id_1",
    "Texts": [
      "Dialogue text 1",
      "Dialogue text 2",
      "Dialogue text 3"
    ],
    "Mode": "Sequential",
    "Duration": 2.0
  },
  {
    "Id": "dialogue_id_2",
    "Texts": [
      "Random dialogue 1",
      "Random dialogue 2"
    ],
    "Mode": "Random",
    "Duration": 3.0
  }
]
```

#### DialogueDefinition Field Descriptions

- `Id` (required): Unique identifier for the dialogue, used to reference in `ModelDialogueTrigger`
- `Texts` (required): Array of dialogue texts, containing all possible texts for this dialogue
- `Mode` (optional): Dialogue playback mode (default: `Sequential`)
  - `Sequential`: Sequential playback, plays in array order, restarts from the beginning after the last one
  - `Random`: Random playback, randomly selects one text from the array each time
  - `RandomNoRepeat`: Random no-repeat playback, randomly selects texts until all have been played, then restarts
  - `Continuous`: Continuous playback, plays all texts sequentially in order
- `Duration` (optional): Dialogue display duration in seconds (default: `2.0`)

### Dialogue Trigger Methods

#### Animation State Machine Trigger

- Use `ModelDialogueTrigger` component in animation state machines to trigger dialogue when states are entered
- Configure `fileName` (dialogue file name without extension) and `dialogueId` (dialogue ID)
- Configure `defaultLanguage` (default language), used when the current language file is missing
- Dialogue bubbles will automatically appear above the character, position automatically adjusts based on the character model

### Multilingual Support

- Dialogue files support multiple languages, the system automatically loads the corresponding language file based on the current game language
- If the current language's dialogue file doesn't exist, it will fall back to the language file specified by `defaultLanguage`
- Language file naming rules: `{filename}_{language}.json`
  - Chinese (Simplified/Traditional): `Chinese`
  - Other languages: Use the string form of `SystemLanguage` enum values (e.g., `English`, `Japanese`, etc.)

### Notes

- If a model has no dialogue configured, it will not affect other functionality
- Dialogue files must contain valid JSON format and at least one dialogue definition
- Dialogue IDs must be unique, duplicate IDs will be overwritten (uses the last one)

## AI Character Adaptation

The mod supports configuring custom models for AI characters in the game, allowing different models to be configured for different AI characters.

### Configuring AI Character Models

#### Configuring in bundleinfo.json

In the model's `bundleinfo.json`, you need to:

1. Include `"AICharacter"` in the `Target` array, indicating the model applies to AI characters
2. Specify supported AI character name keys in the `SupportedAICharacters` array

Example:

```json
{
  "ModelID": "ai_model_id",
  "Name": "AI Model",
  "Target": ["AICharacter"],
  "SupportedAICharacters": ["Cname_Wolf", "Cname_Scav", "*"]
}
```

- If `SupportedAICharacters` contains `"*"`, it means the model applies to all AI characters
- If `SupportedAICharacters` contains specific AI character name keys, the model only applies to those AI characters
- If `SupportedAICharacters` is an empty array, the model will not be applied to any AI character

#### Configuring in UsingModel.json

In `UsingModel.json`, you can configure models for each AI character individually:

**Recommended Method (v1.10.0+)**:

```json
{
  "Version": 2,
  "TargetTypeModelIDs": {
    "built-in:AICharacter_Cname_Wolf": "wolf_model_id",
    "built-in:AICharacter_Cname_Scav": "scav_model_id",
    "built-in:AICharacter_*": "default_ai_model_id"
  }
}
```

**Obsolete Method (Backward Compatible)**:

```json
{
  "AICharacterModelIDs": {
    "Cname_Wolf": "wolf_model_id",
    "Cname_Scav": "scav_model_id",
    "*": "default_ai_model_id"
  }
}
```

Configuration priority:

1. First check if the AI character has an individually configured model (`built-in:AICharacter_<character name>` or obsolete `AICharacterModelIDs[<character name>]`)
2. If not, check the default model corresponding to `"built-in:AICharacter_*"` or obsolete `AICharacterModelIDs["*"]`
3. If neither exists, use the original model

#### Finding AI Character Name Keys

The keys for AI unit targets (such as `"Cname_Wolf"`, `"Cname_Scav"`) can be found in the game's localization files:

- File location: CSV files in the `<Game Installation Directory>/Duckov_Data/StreamingAssets/Localization` directory
- How to find: Open any language CSV file (e.g., `ChineseSimplified.csv`), find the `Characters` sheet (worksheet), and the key column contains the AI character name keys
- These keys can be used in the `SupportedAICharacters` array and `AICharacterModelIDs` dictionary

### Using the Model Selection Interface

1. Open the model selection interface (default key: `\`)
2. Select "AI Character" from the target type dropdown menu
3. Select the AI character to configure (or select "All AI Characters" to set default configuration items)
4. Browse and select the model to apply
5. Configuration will be automatically saved to `UsingModel.json`

### Hiding AI Character Equipment

You can configure whether to hide original equipment for each AI character individually:

- In the model selection interface, select "AI Character" target type
- Select a specific AI character (or select "All AI Characters" to set the default value)
- A "Hide Equipment" toggle option for that AI character will be displayed in the target settings area
- Each AI character has an independent hide equipment setting, and you can also set a default value for "All AI Characters"
- **Configuration Selection Logic**: Audio, equipment hiding, and other settings will select configuration based on the actually used model
  - If an AI character uses its own model configuration (a model is individually configured for that AI character in `UsingModel.json`), it will use that AI character's settings
  - If an AI character uses the fallback model (`*`, i.e., the default model for "All AI Characters"), it will use `*` settings
- Configuration will be automatically saved to `HideEquipmentConfig.json`

### Notes

- AI character models need to meet the same requirements as character models (locator points, Animator configuration, etc.)
- Models must explicitly declare support for AI characters in their `bundleinfo.json`
  - **Recommended method (v1.10.0+)**: Include `"built-in:AICharacter_*"` or `"built-in:AICharacter_<character name>"` in `TargetTypes`
  - **Obsolete method (backward compatible)**: Include `"AICharacter"` in `Target` and declare supported AI characters in `SupportedAICharacters`
- If the model is not properly configured, AI characters will use the original model

