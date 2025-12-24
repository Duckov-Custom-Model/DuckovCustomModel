# Duckov Custom Model

[English](README_EN.md) | 中文

[更新日志](CHANGELOG.md)

一个用于 Duckov 游戏的自定义玩家模型模组。

## 基本功能

- **自定义模型替换**：允许玩家使用自定义模型替换游戏中的角色模型
- **模型选择界面**：提供图形化界面，方便玩家浏览和选择可用模型
- **模型搜索**：支持通过模型名称、ID 等关键词搜索模型
- **模型管理**：自动扫描并加载模型包，支持多个模型包同时存在
- **增量更新**：使用哈希缓存机制，只更新变更的模型包，提高刷新效率
- **多对象支持**：每个模型目标类型（ModelTarget）可对应多个游戏对象，切换时统一应用到所有对象
- **快速切换**：支持在游戏中快速切换模型，无需重启游戏

## 配置文件

配置文件位于：`游戏安装路径/ModConfigs/DuckovCustomModel`

**注意**：如果游戏安装目录为只读环境（如 macOS 上的某些安装方式），模组会自动将配置文件路径切换到游戏存档的上一级目录的 ModConfigs（Windows: `AppData\LocalLow\TeamSoda\Duckov\ModConfigs\DuckovCustomModel`，macOS/Linux: 对应的用户数据目录）。模组会自动检测并处理这种情况，无需手动配置。

### UIConfig.json

UI 界面相关配置。

```json
{
  "ToggleKey": "Backslash",
  "AnimatorParamsToggleKey": "None",
  "ShowDCMButton": true,
  "DCMButtonAnchor": "TopLeft",
  "DCMButtonOffsetX": 10.0,
  "DCMButtonOffsetY": -10.0
}
```

- `ToggleKey`：打开/关闭模型选择界面的按键（默认：`Backslash`，即反斜杠键 `\`）
  - 支持的按键值可参考 Unity KeyCode 枚举
- `AnimatorParamsToggleKey`：打开/关闭参数显示界面的按键（默认：`None`，即没有按键）
  - 需要用户主动在设置界面中设置
  - 支持的按键值可参考 Unity KeyCode 枚举
  - 设置为 `None` 时，该快捷键功能将被禁用
- `ShowDCMButton`：是否在主菜单和背包界面显示 DCM 按钮（默认：`true`）
  - 设置为 `true` 时，在主菜单或背包界面会自动显示 DCM 按钮
  - 可在设置界面中切换此选项
- `DCMButtonAnchor`：DCM 按钮的锚点位置（默认：`"TopLeft"`）
  - 可选值：`"TopLeft"`（左上）、`"TopCenter"`（上中）、`"TopRight"`（右上）、`"MiddleLeft"`（左中）、`"MiddleCenter"`（中央）、`"MiddleRight"`（右中）、`"BottomLeft"`（左下）、`"BottomCenter"`（下中）、`"BottomRight"`（右下）
  - 可在设置界面中通过下拉菜单选择
- `DCMButtonOffsetX`：DCM 按钮的 X 轴偏移值（默认：`10.0`）
  - 相对于锚点位置的 X 轴偏移（像素）
  - 可在设置界面中通过输入框设置
- `DCMButtonOffsetY`：DCM 按钮的 Y 轴偏移值（默认：`-10.0`）
  - 相对于锚点位置的 Y 轴偏移（像素）
  - 可在设置界面中通过输入框设置

### HideEquipmentConfig.json

隐藏装备配置。使用 `ModelTarget` 作为键，便于后续扩展新的模型目标类型。

```json
{
  "HideEquipment": {
    "Character": false,
    "Pet": false
  }
}
```

- `HideEquipment`：字典类型，键为 `ModelTarget` 枚举值（如 `"Character"`、`"Pet"`），值为布尔类型
  - `Character`：是否隐藏角色原有装备（默认：`false`）
    - 设置为 `true` 时，角色模型的 Animator 的 `HideOriginalEquipment` 参数会被设置为 `true`
    - 可在模型选择界面的设置区域中切换此选项
  - `Pet`：是否隐藏宠物原有装备（默认：`false`）
    - 设置为 `true` 时，宠物模型的 Animator 的 `HideOriginalEquipment` 参数会被设置为 `true`
    - 可在模型选择界面的设置区域中切换此选项
  - 当添加新的 `ModelTarget` 类型时，配置会自动包含该类型（默认值为 `false`）

**兼容性说明**：如果 `UIConfig.json` 中存在旧的 `HideCharacterEquipment` 或 `HidePetEquipment` 配置，系统会自动迁移到新的 `HideEquipmentConfig.json` 文件中。

### UsingModel.json

当前使用的模型配置。使用 `ModelTarget` 作为键，便于后续扩展新的模型目标类型。

```json
{
  "ModelIDs": {
    "Character": "",
    "Pet": ""
  },
  "AICharacterModelIDs": {
    "Cname_Wolf": "",
    "Cname_Scav": "",
    "*": ""
  }
}
```

- `ModelIDs`：字典类型，键为 `ModelTarget` 枚举值（如 `"Character"`、`"Pet"`），值为模型 ID（字符串，为空时使用原始模型）
  - `Character`：当前使用的角色模型 ID
    - 设置后，游戏会在关卡加载时自动应用该模型到所有角色对象
    - 可通过模型选择界面修改，修改后会自动保存到此文件
  - `Pet`：当前使用的宠物模型 ID
    - 设置后，游戏会在关卡加载时自动应用该模型到所有宠物对象
    - 可通过模型选择界面修改，修改后会自动保存到此文件
  - 当添加新的 `ModelTarget` 类型时，配置会自动支持该类型

- `AICharacterModelIDs`：字典类型，键为 AI 角色名称键（如 `"Cname_Wolf"`、`"Cname_Scav"`），值为模型 ID（字符串，为空时使用原始模型）
  - 可以为每个 AI 角色单独配置模型
  - 特殊键 `"*"`：为所有 AI 角色设置默认模型
    - 当某个 AI 角色没有单独配置模型时，会使用 `"*"` 对应的模型
    - 如果 `"*"` 也没有配置，则使用原始模型
  - 可通过模型选择界面修改，修改后会自动保存到此文件

**兼容性说明**：如果配置文件中存在旧的 `ModelID` 或 `PetModelID` 字段，系统会自动迁移到新的 `ModelIDs` 字典格式。迁移完成后，配置文件将只包含 `ModelIDs` 字典。

### IdleAudioConfig.json

待机音频自动播放间隔配置。用于配置不同角色的自动播放间隔（最小值和最大值）。

```json
{
  "IdleAudioIntervals": {
    "Pet": {
      "Min": 30.0,
      "Max": 45.0
    }
  },
  "AICharacterIdleAudioIntervals": {
    "Cname_Wolf": {
      "Min": 20.0,
      "Max": 30.0
    },
    "*": {
      "Min": 30.0,
      "Max": 45.0
    }
  },
  "EnableIdleAudio": {
    "Character": false,
    "Pet": true
  },
  "AICharacterEnableIdleAudio": {
    "*": true
  }
}
```

- `IdleAudioIntervals`：字典类型，键为 `ModelTarget` 枚举值（如 `"Character"`、`"Pet"`），值为包含 `Min` 和 `Max` 的对象
  - `Pet`：宠物角色的待机音频播放间隔（秒）
    - `Min`：最小间隔时间（默认：`30.0`）
    - `Max`：最大间隔时间（默认：`45.0`）
    - 系统会在最小值和最大值之间随机选择间隔时间
  - 当添加新的 `ModelTarget` 类型时，配置会自动包含该类型（默认值：`Min: 30.0, Max: 45.0`）

- `AICharacterIdleAudioIntervals`：字典类型，键为 AI 角色名称键（如 `"Cname_Wolf"`、`"Cname_Scav"`），值为包含 `Min` 和 `Max` 的对象
  - 可以为每个 AI 角色单独配置待机音频播放间隔
  - 特殊键 `"*"`：为所有 AI 角色设置默认间隔
    - 当某个 AI 角色没有单独配置间隔时，会使用 `"*"` 对应的间隔
    - 如果 `"*"` 也没有配置，则使用默认值（`Min: 30.0, Max: 45.0`）
  - `Min`：最小间隔时间（默认：`30.0`）
  - `Max`：最大间隔时间（默认：`45.0`）
  - 系统会在最小值和最大值之间随机选择间隔时间

- `EnableIdleAudio`：字典类型，键为 `ModelTarget` 枚举值（如 `"Character"`、`"Pet"`），值为布尔值，控制该角色类型是否允许自动播放待机音频
  - `Character`：玩家角色是否允许自动播放待机音频（默认：`false`）
  - `Pet`：宠物角色是否允许自动播放待机音频（默认：`true`）
  - 当添加新的 `ModelTarget` 类型时，配置会自动包含该类型（默认值：`Character` 为 `false`，其他为 `true`）

- `AICharacterEnableIdleAudio`：字典类型，键为 AI 角色名称键（如 `"Cname_Wolf"`、`"Cname_Scav"`），值为布尔值，控制该 AI 角色是否允许自动播放待机音频
  - 可以为每个 AI 角色单独配置是否允许自动播放待机音频
  - 特殊键 `"*"`：为所有 AI 角色设置默认值
    - 当某个 AI 角色没有单独配置时，会使用 `"*"` 对应的值
    - 如果 `"*"` 也没有配置，则使用默认值（`true`）
  - 默认值：`true`（允许自动播放）

**注意事项**：
- 最小间隔时间不能小于 0.1 秒
- 最大间隔时间不能小于最小间隔时间
- 只有配置了 `"idle"` 标签音效的模型才会自动播放待机音效
- 只有启用了自动播放的角色类型才会自动播放待机音效（通过 `EnableIdleAudio` 和 `AICharacterEnableIdleAudio` 配置控制）
- 玩家角色默认不允许自动播放待机音效，可以通过配置启用

### ModelAudioConfig.json

模型音频开关配置。用于控制是否使用模型提供的音频（包括按键触发、AI 自动触发和待机音频）。

```json
{
  "EnableModelAudio": {
    "Character": true,
    "Pet": true
  },
  "AICharacterEnableModelAudio": {
    "*": true
  }
}
```

- `EnableModelAudio`：字典类型，键为 `ModelTarget` 枚举值（如 `"Character"`、`"Pet"`），值为布尔值，控制该角色类型是否使用模型音频
  - `Character`：玩家角色是否使用模型音频（默认：`true`）
    - 设置为 `false` 时，玩家角色的所有模型音频都不会播放（包括按键触发和待机音频）
    - 可在模型选择界面的目标设置区域中切换此选项
  - `Pet`：宠物角色是否使用模型音频（默认：`true`）
    - 设置为 `false` 时，宠物角色的所有模型音频都不会播放（包括 AI 自动触发和待机音频）
    - 可在模型选择界面的目标设置区域中切换此选项
  - 当添加新的 `ModelTarget` 类型时，配置会自动包含该类型（默认值：`true`）

- `AICharacterEnableModelAudio`：字典类型，键为 AI 角色名称键（如 `"Cname_Wolf"`、`"Cname_Scav"`），值为布尔值，控制该 AI 角色是否使用模型音频
  - 可以为每个 AI 角色单独配置是否使用模型音频
  - 特殊键 `"*"`：为所有 AI 角色设置默认值
    - 当某个 AI 角色没有单独配置时，会使用 `"*"` 对应的值
    - 如果 `"*"` 也没有配置，则使用默认值（`true`）
  - **配置选择逻辑**：音频设置会根据实际使用的模型来选择配置
    - 如果 AI 角色使用的是自己的模型配置（在 `UsingModel.json` 中为该 AI 角色单独配置了模型），则使用该 AI 角色的音频设置
    - 如果 AI 角色使用的是回退模型（`*`，即"所有 AI 角色"的默认模型），则使用`*`的音频设置
  - 默认值：`true`（使用模型音频）
  - 可在模型选择界面的目标设置区域中切换此选项

**注意事项**：
- 当禁用模型音频时，对应角色的所有模型音频都不会播放，包括：
  - 玩家按键触发的音频（`"normal"` 标签）
  - AI 自动触发的音频（`"normal"`、`"surprise"`、`"death"` 标签）
  - 待机音频（`"idle"` 标签）
- 此配置与 `IdleAudioConfig.json` 中的 `EnableIdleAudio` 配置是独立的：
  - `ModelAudioConfig.json` 控制是否使用模型音频（总开关）
  - `IdleAudioConfig.json` 中的 `EnableIdleAudio` 控制是否允许自动播放待机音频（仅影响待机音频的自动播放）
  - 如果 `ModelAudioConfig.json` 中禁用了模型音频，即使 `EnableIdleAudio` 为 `true`，待机音频也不会播放

## 模型选择界面

模型选择界面提供了以下功能：

- **目标类型切换**：可以在"角色"、"宠物"和"AI角色"之间切换，分别管理角色模型、宠物模型和 AI 角色模型
- **模型浏览**：滚动查看所有可用的模型（会根据当前选择的目标类型过滤显示）
- **模型搜索**：通过模型名称、ID 等关键词快速搜索模型
- **模型选择**：点击模型按钮即可应用该模型到所有属于该目标类型的对象
- **模型信息**：每个模型卡片显示模型名称、ID、作者、版本以及所属的 Bundle 名称
- **AI 角色选择**：当选择"AI角色"目标类型时，需要先选择具体的 AI 角色，然后为该角色选择模型
  - **设置选项**：在设置标签页中可以配置以下选项：
  - **快捷键**：配置打开/关闭模型选择界面的快捷键
    - 可在设置界面中点击按钮进行设置
  - **动画器参数快捷键**：配置打开/关闭参数显示界面的快捷键
    - 默认值为没有按键，需要用户主动设置
    - 可在设置界面中点击按钮进行设置
  - **隐藏原有装备**：分别有"隐藏角色装备"和"隐藏宠物装备"选项
    - 此选项会立即保存到配置文件
    - 影响 Animator 的 `HideOriginalEquipment` 参数值
  - **显示动画器参数**：切换是否显示动画器参数窗口
  - **在主菜单和背包界面显示 DCM 按钮**：控制是否在主菜单或背包界面时显示 DCM 按钮
  - **DCM 按钮位置**：配置 DCM 按钮的锚点位置和偏移值
    - 锚点位置：通过下拉菜单选择 9 个位置之一（左上、上中、右上、左中、中央、右中、左下、下中、右下）
    - 偏移值：设置 X 和 Y 轴的偏移值（像素）
    - 配置更改后会立即应用到按钮位置
- **目标设置**：在目标设置区域可以配置以下选项（根据当前选择的目标类型显示）：
  - **使用模型音频**：控制是否使用模型提供的音频
    - 禁用后，对应角色的所有模型音频都不会播放（包括按键触发、AI 自动触发和待机音频）
    - 支持为角色、宠物和 AI 角色分别配置
    - 此选项会立即保存到 `ModelAudioConfig.json`
  - **是否允许播放IDLE音频**：控制是否允许自动播放待机音频
    - 此选项会立即保存到 `IdleAudioConfig.json`
  - **IDLE音频播放间隔**：配置待机音频的播放间隔（最小值和最大值）
    - 此选项会立即保存到 `IdleAudioConfig.json`
  - **隐藏装备**：控制是否隐藏原有装备
    - 此选项会立即保存到 `HideEquipmentConfig.json`
    - 影响 Animator 的 `HideOriginalEquipment` 参数值
- **AI 角色装备设置**：当选择"AI角色"目标类型并选择具体 AI 角色后，在模型列表页面会显示针对该 AI 角色的"隐藏装备"选项
  - 每个 AI 角色都有独立的隐藏装备设置
  - 此选项会立即保存到配置文件
  - 影响 Animator 的 `HideOriginalEquipment` 参数值

### 打开模型选择界面

- 默认按键：`\`（反斜杠键）
- 可通过修改 `UIConfig.json` 中的 `ToggleKey` 来更改按键
- 按 `ESC` 键可关闭界面
- **DCM 按钮**：在主菜单或背包界面时，屏幕会显示一个固定的 DCM 按钮（默认位置：左上）
  - 点击按钮可以快速打开/关闭模型选择界面
  - 按钮位置和显示状态可在设置界面中配置
  - 按钮位置支持 9 个锚点位置（左上、上中、右上、左中、中央、右中、左下、下中、右下）和自定义偏移值
  - 配置更改后会立即应用到按钮位置，无需重启游戏

## 模型安装

将模型包放置在：`游戏安装路径/ModConfigs/DuckovCustomModel/Models`

**注意**：如果游戏安装目录为只读环境，模组会自动将模型路径切换到游戏存档的上一级目录的 ModConfigs（Windows: `AppData\LocalLow\TeamSoda\Duckov\ModConfigs\DuckovCustomModel\Models`，macOS/Linux: 对应的用户数据目录）。模组会自动检测并处理这种情况，无需手动配置。

每个模型包应放在独立的文件夹中，包含模型资源文件和配置信息。

### 模型包结构

每个模型包文件夹应包含以下文件：

```
模型包文件夹/
├── bundleinfo.json          # 模型包配置文件（必需）
├── modelbundle.assetbundle  # Unity AssetBundle 文件（必需）
└── thumbnail.png            # 缩略图文件（可选）
```

### bundleinfo.json 格式

```json
{
  "BundleName": "模型包名称",
  "BundlePath": "modelbundle.assetbundle",
  "Models": [
    {
      "ModelID": "unique_model_id",
      "Name": "模型显示名称",
      "Author": "作者名称",
      "Description": "模型描述",
      "Version": "1.0.0",
      "ThumbnailPath": "thumbnail.png",
      "PrefabPath": "Assets/Model.prefab",
      "DeathLootBoxPrefabPath": "Assets/DeathLootBox.prefab",
      "Target": ["Character", "AICharacter"],
      "SupportedAICharacters": ["Cname_Wolf", "Cname_Scav", "*"],
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

#### 字段说明

**BundleName**（必需）：模型包名称，用于标识和显示

**BundlePath**（必需）：AssetBundle 文件路径，相对于模型包文件夹的路径

**Models**（必需）：模型信息数组，可包含多个模型

**ModelInfo 字段**：

- `ModelID`（必需）：模型的唯一标识符，用于在配置文件中引用模型
- `Name`（可选）：模型在界面中显示的名称
- `Author`（可选）：模型作者
- `Description`（可选）：模型描述信息
- `Version`（可选）：模型版本号
- `ThumbnailPath`（可选）：缩略图路径，相对于模型包文件夹的外部文件路径（如 `"thumbnail.png"`）
- `PrefabPath`（必需）：模型 Prefab 在 AssetBundle 内的资源路径（如 `"Assets/Model.prefab"`）
- `DeathLootBoxPrefabPath`（可选）：死亡战利品箱 Prefab 在 AssetBundle 内的资源路径（如 `"Assets/DeathLootBox.prefab"`）
  - 当角色使用该模型并死亡时，如果配置了此字段，死亡战利品箱会使用自定义的 Prefab 替换默认模型
  - 如果未配置此字段，死亡战利品箱将使用默认模型
- `Target`（可选）：模型适用的目标类型数组（默认：`["Character"]`）
  - 可选值：`"Character"`（角色）、`"Pet"`（宠物）、`"AICharacter"`（AI 角色）
  - 可以同时包含多个值，表示该模型同时适用于多个目标类型
  - 模型选择界面会根据当前选择的目标类型过滤显示兼容的模型
- `SupportedAICharacters`（可选）：支持的 AI 角色名称键数组（仅在 `Target` 包含 `"AICharacter"` 时有效）
  - 可以指定该模型适用于哪些 AI 角色
  - 特殊值 `"*"`：表示该模型适用于所有 AI 角色
  - 如果为空数组且 `Target` 包含 `"AICharacter"`，则该模型不会应用于任何 AI 角色
- `CustomSounds`（可选）：自定义音效信息数组，支持为音效配置标签
  - 每个音效可以配置多个标签（`normal`、`surprise`、`death`）
  - 未指定标签时，默认为 `["normal"]`
  - 同一音效文件可以同时用于多个场景
  - 音效文件路径在 `Path` 中指定，相对于模型包文件夹
- `SoundTagPlayChance`（可选）：音效标签播放概率配置
  - 字典类型，键为音效标签（不区分大小写），值为播放概率（0-100）
  - 当触发该标签的音效时，会根据配置的概率决定是否播放
  - 如果未配置或概率为 0，则始终播放（默认行为）
- `WalkSoundFrequency`（可选）：走路时每秒的脚步声触发频率
  - 用于控制角色走路时脚步声的播放频率
  - 如果未指定，将自动使用原始角色的走路脚步声频率设置
- `RunSoundFrequency`（可选）：跑步时每秒的脚步声触发频率
  - 用于控制角色跑步时脚步声的播放频率
  - 如果未指定，将自动使用原始角色的跑步脚步声频率设置
- `BuffAnimatorParams`（可选）：Buff 驱动的动画器参数配置
  - 字典类型，键为动画器参数名称（bool 类型），值为 Buff 匹配条件数组
  - 当角色拥有匹配的 Buff 时，对应的动画器参数会被设置为 `true`，否则为 `false`
  - 每个条件可以指定 `Id`（Buff ID）或 `DisplayNameKey`（Buff 显示名称键），满足任意一个条件即可
  - 示例配置：
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
  - 配置的 Buff 参数会在调试界面中显示，位于自定义参数和动画器参数之后

## 定位锚点

为了确保游戏中的装备（武器、护甲、背包等）能够正确绑定到自定义模型上，模型 Prefab 需要包含相应的定位锚点（Locator）GameObject。

### 必需的定位锚点

模型 Prefab 的子对象中需要包含以下名称的 Transform 作为定位锚点：

- `LeftHandLocator`：左手定位锚点，用于绑定左手装备
- `RightHandLocator`：右手定位锚点，用于绑定右手装备
- `ArmorLocator`：护甲定位锚点，用于绑定护甲装备
- `HelmetLocator`：头盔定位锚点，用于绑定头盔装备
- `FaceLocator`：面部定位锚点，用于绑定面部装备
- `BackpackLocator`：背包定位锚点，用于绑定背包装备
- `MeleeWeaponLocator`：近战武器定位锚点，用于绑定近战武器装备
- `PopTextLocator`：弹出文本定位锚点，用于显示弹出文本

### 可选的定位锚点

除了必需的定位锚点外，模型还可以包含以下可选的定位锚点：

- `PaperBoxLocator`：纸箱定位锚点，用于绑定纸箱
  - 当自定义模型包含此定位锚点时，游戏中生成的纸箱会自动附加到此定位点
  - 纸箱会跟随自定义模型的位置和旋转
  - 如果模型不包含此定位锚点，纸箱将使用原始模型的定位点
- `CarriableLocator`：可搬运物品定位锚点，用于绑定可搬运物品
  - 当自定义模型包含此定位锚点时，角色搬运物品时会自动附加到此定位点
  - 可搬运物品会跟随自定义模型的位置和旋转
  - 搬运物品时会保存原始的位置、旋转和缩放信息，放下物品时会恢复
  - 如果模型不包含此定位锚点，可搬运物品将使用原始模型的定位点

### 定位锚点的作用

- 模组会自动在自定义模型中搜索这些定位锚点
- 找到的定位锚点会被设置为游戏装备系统的绑定点
- 装备会按照定位锚点的位置和旋转进行绑定
- 如果某个定位锚点不存在，对应的装备将无法正确显示在自定义模型上

### 注意事项

- 定位锚点的名称必须**完全匹配**（区分大小写）
- 定位锚点可以是空的 GameObject，只需要设置正确的位置和旋转
- 建议根据原始模型的装备位置来设置定位锚点的位置

## 动画器配置

自定义模型 Prefab 需要包含 `Animator` 组件，并配置相应的 Animator Controller。

### Animator Controller 参数

Animator Controller 可以使用以下参数：

#### Bool 类型参数

- `Grounded`：角色是否在地面上
- `Die`：角色是否死亡
- `Moving`：角色是否正在移动
- `Running`：角色是否正在奔跑
- `Dashing`：角色是否正在冲刺
- `GunReady`：枪械是否准备就绪
- `Loaded`：枪械是否已装弹（当持有 `ItemAgent_Gun` 时，由 `OnLoadedEvent` 事件更新）
- `Reloading`：是否正在装弹
- `RightHandOut`：右手是否伸出
- `ActionRunning`：是否正在执行动作（由 `CharacterMainControl.CurrentAction` 决定）
- `Hidden`：角色是否处于隐藏状态
- `ThermalOn`：热成像是否开启
- `InAds`：是否正在瞄准（ADS - Aim Down Sights）
- `HideOriginalEquipment`：是否隐藏原有装备（由 `HideEquipmentConfig.json` 中对应 `ModelTarget` 的配置控制）
- `LeftHandEquip`：左手槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `RightHandEquip`：右手槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `ArmorEquip`：护甲槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `HelmetEquip`：头盔槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `HeadsetEquip`：耳机槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `FaceEquip`：面部槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `BackpackEquip`：背包槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `MeleeWeaponEquip`：近战武器槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `HavePopText`：是否有弹出文本（检测弹出文本槽位是否有子对象）

#### Float 类型参数

- `MoveSpeed`：移动速度比例（正常走 0~1，跑步可达 2）
- `MoveDirX`：移动方向 X 分量（-1.0 ~ 1.0，角色本地坐标系）
- `MoveDirY`：移动方向 Y 分量（-1.0 ~ 1.0，角色本地坐标系）
- `VelocityX`：速度 X 分量
- `VelocityY`：速度 Y 分量
- `VelocityZ`：速度 Z 分量
- `VelocityMagnitude`：速度大小（速度向量的长度）
- `AimDirX`：瞄准方向 X 分量
- `AimDirY`：瞄准方向 Y 分量
- `AimDirZ`：瞄准方向 Z 分量
- `AdsValue`：瞄准值（0.0 - 1.0，瞄准进度）
- `AmmoRate`：弹药比率（0.0 - 1.0，当前弹药数 / 最大弹药容量）
- `HealthRate`：生命值比率（0.0 - 1.0，当前生命值 / 最大生命值）
- `WaterRate`：水分比率（0.0 - 1.0，当前水分 / 最大水分）
- `WeightRate`：重量比率（当前总重量 / 最大负重，可能大于 1.0）
- `ActionProgress`：动作进度百分比（0.0 - 1.0，当前动作的进度，由 `IProgress.GetProgress().progress` 获取）
- `Time`：当前 24 小时时间（0.0 - 24.0，由 `TimeOfDayController.Instance.Time` 获取，不可用时为 -1.0）
- `Mod:ShoulderSurfing:CameraPitch`：ShoulderSurfing mod 的相机俯仰角值（需要安装 ShoulderSurfing mod 才能使用，未安装时为 0.0）

#### Int 类型参数

- `CurrentCharacterType`：当前角色类型
  - `0`：角色（Character）
  - `1`：宠物（Pet）
- `HandState`：手部状态
  - `0`：默认状态
  - `1`：正常（normal）
  - `2`：枪械（gun）
  - `3`：近战武器（meleeWeapon）
  - `4`：弓（bow）
  - `-1`：搬运状态
- `ShootMode`：射击模式（当持有 `ItemAgent_Gun` 时，由枪械的 `triggerMode` 决定）
  - `0`：自动（auto）
  - `1`：半自动（semi）
  - `2`：栓动（bolt）
- `GunState`：枪械状态（当持有 `ItemAgent_Gun` 时，由枪械的 `GunState` 决定）
  - `0`：射击冷却（shootCooling）
  - `1`：就绪（ready）
  - `2`：开火（fire）
  - `3`：连发每发冷却（burstEachShotCooling）
  - `4`：空弹（empty）
  - `5`：装弹中（reloading）
- `AimType`：瞄准类型（由 `CharacterMainControl.AimType` 决定）
  - `0`：正常瞄准（normalAim）
  - `1`：角色技能（characterSkill）
  - `2`：手持技能（handheldSkill）
- `WeightState`：重量状态（仅在Raid地图中生效）
  - `0`：轻量（WeightRate ≤ 0.25）
  - `1`：正常（0.25 < WeightRate ≤ 0.75）
  - `2`：超重（0.75 < WeightRate ≤ 1.0）
  - `3`：过载（WeightRate > 1.0）
- `WeaponInLocator`：武器当前所在的定位点类型（无武器时为 `0`）
  - `0`：无武器
  - `1`：右手定位点（`normalHandheld`）
  - `2`：近战武器定位点（`meleeWeapon`）
  - `3`：左手定位点（`leftHandSocket`）
  - 当武器类型为左手但模型没有左手定位点时，会自动使用右手定位点（值为 `1`）
- `LeftHandTypeID`：左手装备的 TypeID（无装备时为 `0`）
- `RightHandTypeID`：右手装备的 TypeID（无装备时为 `0`）
- `ArmorTypeID`：护甲装备的 TypeID（无装备时为 `0`）
- `HelmetTypeID`：头盔装备的 TypeID（无装备时为 `0`）
- `HeadsetTypeID`：耳机装备的 TypeID（无装备时为 `0`）
- `FaceTypeID`：面部装备的 TypeID（无装备时为 `0`）
- `BackpackTypeID`：背包装备的 TypeID（无装备时为 `0`）
- `MeleeWeaponTypeID`：近战武器装备的 TypeID（无装备时为 `0`）
- `ActionPriority`：动作优先级（由 `CharacterMainControl.CurrentAction.ActionPriority()` 决定）
  - `0`：Whatever（任意）
  - `1`：Reload（装弹）
  - `2`：Attack（攻击）
  - `3`：usingItem（使用物品）
  - `4`：Dash（冲刺）
  - `5`：Skills（技能）
  - `6`：Fishing（钓鱼）
  - `7`：Interact（交互）
  - 当 `ActionRunning` 为 `true` 时，动作优先级可以近似用于判断角色正在执行什么动作
- `Weather`：当前天气状态（由 `TimeOfDayController.Instance.CurrentWeather` 获取，不可用时为 -1）
  - `0`：晴天（Sunny）
  - `1`：多云（Cloudy）
  - `2`：雨天（Rainy）
  - `3`：风暴 I（Stormy_I）
  - `4`：风暴 II（Stormy_II）
- `TimePhase`：当前时间阶段（由 `TimeOfDayController.Instance.CurrentPhase.timePhaseTag` 获取，不可用时为 -1）
  - `0`：白天（day）
  - `1`：黄昏（dawn）
  - `2`：夜晚（night）

#### Mod 扩展参数

模组支持通过扩展模块添加额外的动画参数。这些参数使用 `Mod:扩展名:参数名` 的命名格式，使用冒号分隔以明确标识为 mod 扩展参数：

- `Mod:ShoulderSurfing:CameraPitch`：ShoulderSurfing mod 的相机俯仰角值（float 类型）
  - 需要安装 ShoulderSurfing mod 才能使用
  - 当 ShoulderSurfing mod 未安装或未激活时，该参数值为 `0.0`
  - 参数值实时反映当前相机的俯仰角度

#### Trigger 类型参数

- `Attack`：攻击触发（用于触发近战攻击动画）
- `Shoot`：射击触发（当持有 `ItemAgent_Gun` 时，由 `OnShootEvent` 事件触发）
- `Hurt`：受伤触发（角色受到伤害时自动触发）
- `Dead`：死亡触发（角色死亡时自动触发）
- `HitTarget`：命中目标触发（角色命中目标时自动触发）
- `KillTarget`：击杀目标触发（角色击杀目标时自动触发）
- `CritHurt`：暴击受伤触发（角色受到暴击伤害时自动触发）
- `CritDead`：暴击死亡触发（角色暴击死亡时自动触发）
- `CritHitTarget`：暴击命中目标触发（角色暴击命中目标时自动触发）
- `CritKillTarget`：暴击击杀目标触发（角色暴击击杀目标时自动触发）

### 可选动画层

如果模型包含近战攻击动画，可以添加名为 `"MeleeAttack"` 的动画层：

- 层名称必须为 `"MeleeAttack"`
- 该层用于播放近战攻击动画
- 层的权重会根据攻击状态自动调整

### 动画器状态机行为组件

模组提供了三个状态机行为组件，可以在动画状态进入时触发音效、对话或控制参数：

#### ModelParameterDriver

在动画状态进入时自动控制 Animator 参数，类似于 Unity 内置的 Animator Parameter Driver。

- `parameters`：参数操作数组，可配置多个参数操作
  - `type`：操作类型
    - `Set`：直接设置参数值
    - `Add`：在现有值基础上增加指定值
    - `Random`：随机设置参数值（支持范围随机和概率触发）
    - `Copy`：从源参数复制值到目标参数（支持范围转换）
  - `name`：目标参数名称（将被写入的参数）
  - `source`：源参数名称（用于 Copy 操作，将被读取的参数）
  - `value`：操作使用的值（用于 Set 和 Add 操作）
  - `valueMin` / `valueMax`：随机值的最小值和最大值（用于 Random 操作）
  - `chance`：触发概率（0.0 - 1.0），用于控制操作是否执行
  - `convertRange`：是否进行范围转换（用于 Copy 操作）
  - `sourceMin` / `sourceMax`：源参数的范围（用于 Copy 操作的范围转换）
  - `destMin` / `destMax`：目标参数的范围（用于 Copy 操作的范围转换）
- `debugString`：调试信息（可选），会在日志中输出，便于调试
- 支持所有 Animator 参数类型（Float、Int、Bool、Trigger）
- 在动画状态进入时自动应用参数驱动
- 支持参数验证，确保目标参数和源参数存在后才应用驱动

#### ModelSoundTrigger

在动画状态进入时触发音效播放。

- `soundTags`：音效标签数组，可配置多个标签
- `playOrder`：标签选择方式（Random：随机选择，Sequential：顺序选择）
- `playMode`：音效播放模式（Normal、StopPrevious、SkipIfPlaying、UseTempObject）
- `eventName`：事件名称，用于音效播放管理（可选，为空时使用默认名称）

#### ModelSoundStopTrigger

在动画状态进入或退出时停止音效播放。

- `stopAllSounds`：是否停止所有正在播放的音效（true：停止所有，false：停止指定事件名称的音效）
- `useBuiltInEventName`：是否使用内置事件名称（true：直接使用内置事件名称如 `idle`，false：使用自定义触发器事件名称）
- `eventName`：事件名称
  - 当 `stopAllSounds` 为 false 且 `useBuiltInEventName` 为 false 时：自定义触发器事件名称（可选，为空时使用默认名称 `CustomModelSoundTrigger`）
  - 当 `stopAllSounds` 为 false 且 `useBuiltInEventName` 为 true 时：内置事件名称（必需，如 `idle`）
- `stopOnEnter`：是否在状态进入时停止（true：进入时停止，false：退出时停止）

**注意事项**：
- 当 `useBuiltInEventName` 为 true 时，必须指定 `eventName`，否则会显示警告
- 自定义触发器的事件名称格式为 `CustomModelSoundTrigger:{eventName}`，与 `ModelSoundTrigger` 保持一致
- 内置事件名称（如 `idle`）直接使用，不添加前缀

#### ModelDialogueTrigger

在动画状态进入时触发对话播放。

- `fileName`：对话定义文件名（不含扩展名）
- `dialogueId`：对话 ID，对应对话配置文件中的对话 ID
- `defaultLanguage`：默认语言，当当前语言文件不存在时使用

### 动画器工作流程

1. 模组会自动读取游戏状态并更新 Animator 参数
2. 移动、跳跃、冲刺等状态会实时同步到自定义模型的 Animator
3. 攻击、装弹等动作会触发相应的动画参数
4. 如果存在 `MeleeAttack` 层，攻击时会自动调整该层的权重以播放攻击动画
5. 当角色持有 `ItemAgent_Gun` 时，会自动订阅枪械的 `OnShootEvent` 和 `OnLoadedEvent` 事件
   - `OnShootEvent`：触发时设置 `Shoot` 触发器
   - `OnLoadedEvent`：触发时更新 `Loaded` 布尔值
6. 当角色切换持有物品时（`OnHoldAgentChanged` 事件），会自动更新相关订阅
7. 战斗相关触发器会在相应事件发生时自动触发：
   - 受伤/死亡：角色受到伤害或死亡时自动触发 `Hurt`/`Dead` 或 `CritHurt`/`CritDead`（根据是否为暴击）
   - 命中/击杀：角色命中或击杀目标时自动触发 `HitTarget`/`KillTarget` 或 `CritHitTarget`/`CritKillTarget`（根据是否为暴击）

## 自定义音效

模组支持为自定义模型配置音效，包括玩家按键触发和 AI 自动触发两种方式。

### 音效配置

在 `bundleinfo.json` 的 `ModelInfo` 中可以配置音效：

```json
{
  "ModelID": "unique_model_id",
  "Name": "模型显示名称",
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

#### SoundInfo 字段说明

- `Path`（必需）：音效文件路径，相对于模型包文件夹
- `Tags`（可选）：音效标签数组，用于指定音效的使用场景
  - `"normal"`：普通音效，用于玩家按键触发（F1 嘎嘎）和 AI 自动触发（普通状态和惊讶状态）
  - `"surprise"`：惊讶音效，用于 AI 惊讶状态（与 `"normal"` 标签共享同一打断组）
  - `"idle"`：待机音效，用于角色自动播放（可通过配置控制哪些角色类型允许自动播放）
  - `"trigger_on_hurt"`：受伤触发音效，用于角色受到伤害时自动播放（如果已有受伤音效正在播放则跳过）
  - `"trigger_on_death"`：死亡触发音效，用于角色死亡时自动播放（会打断所有正在播放的音效）
  - `"trigger_on_hit_target"`：命中目标触发音效，用于角色命中目标时自动播放（如果已有命中音效正在播放则跳过）
  - `"trigger_on_kill_target"`：击杀目标触发音效，用于角色击杀目标时自动播放（如果已有击杀音效正在播放则跳过）
  - `"trigger_on_crit_hurt"`：暴击受伤触发音效，用于角色受到暴击伤害时自动播放（如果已有受伤音效正在播放则跳过）
  - `"trigger_on_crit_dead"`：暴击死亡触发音效，用于角色暴击死亡时自动播放（会打断所有正在播放的音效）
  - `"trigger_on_crit_hit_target"`：暴击命中目标触发音效，用于角色暴击命中目标时自动播放（如果已有命中音效正在播放则跳过）
  - `"trigger_on_crit_kill_target"`：暴击击杀目标触发音效，用于角色暴击击杀目标时自动播放（如果已有击杀音效正在播放则跳过）
  - `"search_found_item_quality_xxx"`：搜索完成时发现指定品质物品会触发音效，`xxx` 可为 `none`、`white`、`green`、`blue`、`purple`、`orange`、`red`、`q7`、`q8`
  - `"footstep_organic_walk_light"`、`"footstep_organic_walk_heavy"`、`"footstep_organic_run_light"`、`"footstep_organic_run_heavy"`：有机材质脚步声（轻/重步行、轻/重跑步）
  - `"footstep_mech_walk_light"`、`"footstep_mech_walk_heavy"`、`"footstep_mech_run_light"`、`"footstep_mech_run_heavy"`：机械材质脚步声（轻/重步行、轻/重跑步）
  - `"footstep_danger_walk_light"`、`"footstep_danger_walk_heavy"`、`"footstep_danger_run_light"`、`"footstep_danger_run_heavy"`：危险材质脚步声（轻/重步行、轻/重跑步）
  - `"footstep_nosound_walk_light"`、`"footstep_nosound_walk_heavy"`、`"footstep_nosound_run_light"`、`"footstep_nosound_run_heavy"`：无声材质脚步声（轻/重步行、轻/重跑步）
  - 可以同时包含多个标签，表示该音效可用于多个场景
  - 未指定标签时，默认为 `["normal"]`


### 音效触发方式

#### 玩家按键触发

- 当角色模型配置了音效时，玩家按下游戏中的 `Quack` 键（F1）会触发音效
- 只会播放标签为 `"normal"` 的音效
- 从所有 `"normal"` 标签的音效中随机选择一个播放
- 只有玩家角色会响应按键，宠物不会触发
- 播放音效时会同时创建 AI 声音，使其他 AI 能够听到玩家发出的声音
- **音效打断机制**：玩家按键触发的音效与 AI 自动触发的音效（`"normal"` 和 `"surprise"` 标签）共享同一打断组，新播放的音效会打断同组内正在播放的音效

#### AI 自动触发

- AI 会根据游戏状态自动触发相应标签的音效
- `"normal"`：AI 普通状态时触发
- `"surprise"`：AI 惊讶状态时触发
- **音效打断机制**：AI 自动触发的音效（`"normal"` 和 `"surprise"` 标签）与玩家按键触发的音效共享同一打断组，新播放的音效会打断同组内正在播放的音效
- `"trigger_on_hurt"`：角色受到伤害时自动播放（适用于所有角色类型）
  - **音效打断机制**：如果已有受伤音效正在播放，则跳过新的受伤音效播放，避免重复播放
- `"trigger_on_hit_target"`：角色命中目标时自动播放（适用于所有角色类型）
  - **音效打断机制**：如果已有命中音效正在播放，则跳过新的命中音效播放，避免重复播放
- `"trigger_on_kill_target"`：角色击杀目标时自动播放（适用于所有角色类型）
  - **音效打断机制**：如果已有击杀音效正在播放，则跳过新的击杀音效播放，避免重复播放
- `"trigger_on_crit_hurt"`：角色受到暴击伤害时自动播放（适用于所有角色类型）
  - **音效打断机制**：如果已有受伤音效正在播放，则跳过新的受伤音效播放，避免重复播放
- `"trigger_on_crit_dead"`：角色暴击死亡时自动播放（适用于所有角色类型）
  - **音效打断机制**：播放死亡音效前会先停止所有正在播放的音效，然后播放死亡音效
- `"trigger_on_crit_hit_target"`：角色暴击命中目标时自动播放（适用于所有角色类型）
  - **音效打断机制**：如果已有命中音效正在播放，则跳过新的命中音效播放，避免重复播放
- `"trigger_on_crit_kill_target"`：角色暴击击杀目标时自动播放（适用于所有角色类型）
  - **音效打断机制**：如果已有击杀音效正在播放，则跳过新的击杀音效播放，避免重复播放
- `"idle"`：启用了自动播放的角色会在随机间隔时间自动播放待机音效
  - 播放间隔可在 `IdleAudioConfig.json` 中配置
  - 默认间隔为 30-45 秒（随机）
  - 角色死亡时不会播放
  - 哪些角色类型允许自动播放可通过 `EnableIdleAudio` 和 `AICharacterEnableIdleAudio` 配置控制
  - 默认情况下，AI 角色和宠物允许自动播放，玩家角色不允许（可通过配置启用）
- `"trigger_on_death"`：角色死亡时自动播放（适用于所有角色类型）
  - **音效打断机制**：播放死亡音效前会先停止所有正在播放的音效，然后播放死亡音效
- 如果指定标签的音效不存在，将使用原版事件（不会回退到其他标签）

#### 脚步声触发

- 角色移动时会根据地面材质和移动状态自动触发相应的脚步声
- 支持四种地面材质：有机（organic）、机械（mech）、危险（danger）、无声（no sound）
- 支持四种移动状态：轻步行（walkLight）、重步行（walkHeavy）、轻跑步（runLight）、重跑步（runHeavy）
- 系统会根据角色的 `footStepMaterialType` 和 `FootStepTypes` 自动选择对应的音效标签
- **音效打断机制**：脚步声拥有独立的打断组，新播放的脚步声会打断同组内正在播放的脚步声
- 如果模型未配置对应材质和状态的脚步声，将使用原版脚步声

#### 搜索发现触发

- 玩家完成物品搜索或检查后（UI 展示品质信息的那一刻）会触发对应品质的音效
- 使用 `search_found_item_quality_xxx` 标签，`xxx` 与 `Tags` 描述相同：`none`、`white`、`green`、`blue`、`purple`、`orange`、`red`、`q7`、`q8`
- 若模型未配置对应品质的音效，则不会播放，保持与原版一致

#### 动画状态机触发

- 可以在动画状态机中使用 `ModelSoundTrigger` 组件在状态进入时触发音效
  - 支持配置多个音效标签，可选择随机或顺序播放
  - 支持配置音效播放模式，提供更精细的音效控制
  - 音效标签可以是任意自定义标签，不再限制于预定义标签
- 可以在动画状态机中使用 `ModelSoundStopTrigger` 组件停止音效播放
  - 支持停止指定事件名称的音效（自定义触发器或内置事件名称如 `idle`）
  - 支持停止所有正在播放的音效
  - 支持在状态进入或退出时触发停止操作
  - 在 Unity 编辑器中提供友好的配置界面，包含条件显示和警告提示

### 音效文件要求

- 音效文件应放置在模型包文件夹内
- 支持游戏使用的音频格式（通常为 WAV、OGG 等）
- 音效文件路径在 `Path` 中指定，相对于模型包文件夹
- 例如：如果模型包文件夹为 `MyModel/`，音效文件为 `MyModel/sounds/voice.wav`，则 `Path` 应设置为 `"sounds/voice.wav"`

### 音效播放概率配置

在 `bundleinfo.json` 的 `ModelInfo` 中可以配置音效标签的播放概率：

```json
{
  "ModelID": "unique_model_id",
  "Name": "模型显示名称",
  "SoundTagPlayChance": {
    "trigger_on_hurt": 50.0,
    "trigger_on_hit_target": 30.0
  }
}
```

- `SoundTagPlayChance`（可选）：字典类型，键为音效标签（不区分大小写），值为播放概率（0-100）
  - 概率值会被自动转换为 0-1 之间的浮点数（除以 100）
  - 当触发该标签的音效时，会根据配置的概率决定是否播放
  - 如果未配置，则始终播放（默认行为）
  - 如果概率为 0，则始终不播放
  - 如果概率小于 100，则有一定几率不播放该音效

### 注意事项

- 如果模型没有配置音效，不会影响其他功能
- 音效标签不再限制于预定义标签，可以使用任意自定义标签
- 自定义标签可以通过 `ModelSoundTrigger` 组件在动画状态机中触发

## 自定义对话

模组支持为自定义模型配置对话，可以在动画状态机中触发对话气泡显示。

### 对话配置

对话配置文件应放置在模型包文件夹内，文件命名格式为：`{文件名}_{语言}.json`

例如：
- `dialogue_English.json`：英语对话文件
- `dialogue_Chinese.json`：中文对话文件

对话配置文件格式：

```json
[
  {
    "Id": "dialogue_id_1",
    "Texts": [
      "对话文本 1",
      "对话文本 2",
      "对话文本 3"
    ],
    "Mode": "Sequential",
    "Duration": 2.0
  },
  {
    "Id": "dialogue_id_2",
    "Texts": [
      "随机对话 1",
      "随机对话 2"
    ],
    "Mode": "Random",
    "Duration": 3.0
  }
]
```

#### DialogueDefinition 字段说明

- `Id`（必需）：对话的唯一标识符，用于在 `ModelDialogueTrigger` 中引用
- `Texts`（必需）：对话文本数组，包含该对话的所有可能文本
- `Mode`（可选）：对话播放模式（默认：`Sequential`）
  - `Sequential`：顺序播放，按数组顺序依次播放，播放完最后一个后重新开始
  - `Random`：随机播放，每次从数组中随机选择一个文本
  - `RandomNoRepeat`：随机不重复播放，随机选择文本，直到所有文本都播放过后重新开始
  - `Continuous`：连续播放，按顺序连续播放所有文本
- `Duration`（可选）：对话显示持续时间（秒，默认：`2.0`）

### 对话触发方式

#### 动画状态机触发

- 在动画状态机中使用 `ModelDialogueTrigger` 组件在状态进入时触发对话
- 配置 `fileName`（对话文件名，不含扩展名）和 `dialogueId`（对话 ID）
- 配置 `defaultLanguage`（默认语言），当当前语言文件不存在时使用
- 对话气泡会自动显示在角色上方，位置会根据角色模型自动调整

### 多语言支持

- 对话文件支持多语言，系统会根据当前游戏语言自动加载对应语言的对话文件
- 如果当前语言的对话文件不存在，会回退到 `defaultLanguage` 指定的语言文件
- 语言文件命名规则：`{文件名}_{语言}.json`
  - 中文（简体/繁体）：`Chinese`
  - 其他语言：使用 `SystemLanguage` 枚举值的字符串形式（如 `English`、`Japanese` 等）

### 注意事项

- 如果模型没有配置对话，不会影响其他功能
- 对话文件必须包含有效的 JSON 格式和至少一个对话定义
- 对话 ID 必须唯一，重复的 ID 会被覆盖（使用最后一个）

## AI 角色适配

模组支持为游戏中的 AI 角色配置自定义模型，可以为不同的 AI 角色配置不同的模型。

### 配置 AI 角色模型

#### 在 bundleinfo.json 中配置

在模型的 `bundleinfo.json` 中，需要：

1. 在 `Target` 数组中包含 `"AICharacter"`，表示该模型适用于 AI 角色
2. 在 `SupportedAICharacters` 数组中指定支持的 AI 角色名称键

示例：

```json
{
  "ModelID": "ai_model_id",
  "Name": "AI 模型",
  "Target": ["AICharacter"],
  "SupportedAICharacters": ["Cname_Wolf", "Cname_Scav", "*"]
}
```

- 如果 `SupportedAICharacters` 包含 `"*"`，表示该模型适用于所有 AI 角色
- 如果 `SupportedAICharacters` 包含具体的 AI 角色名称键，表示该模型仅适用于这些 AI 角色
- 如果 `SupportedAICharacters` 为空数组，则该模型不会应用于任何 AI 角色

#### 在 UsingModel.json 中配置

在 `UsingModel.json` 中，可以为每个 AI 角色单独配置模型：

```json
{
  "AICharacterModelIDs": {
    "Cname_Wolf": "wolf_model_id",
    "Cname_Scav": "scav_model_id",
    "*": "default_ai_model_id"
  }
}
```

配置优先级：

1. 首先检查该 AI 角色是否有单独配置的模型
2. 如果没有，则检查 `"*"` 对应的默认模型
3. 如果都没有，则使用原始模型

#### 查找 AI 角色名称键

AI 单位目标的 key（如 `"Cname_Wolf"`、`"Cname_Scav"`）可以从游戏本地化文件中查找：

- 文件位置：`游戏安装目录/Duckov_Data/StreamingAssets/Localization` 目录下的 CSV 文件
- 查找方法：打开任意语言的 CSV 文件（如 `ChineseSimplified.csv`），找到 `Characters` 这个 sheet（工作表），其中的 key 列就是 AI 角色名称键
- 这些 key 可以用于 `SupportedAICharacters` 数组和 `AICharacterModelIDs` 字典中

### 使用模型选择界面

1. 打开模型选择界面（默认按键：`\`）
2. 在目标类型下拉菜单中选择"AI角色"
3. 选择要配置的 AI 角色（或选择"所有 AI 角色"来设置默认配置项）
4. 浏览并选择要应用的模型
5. 配置会自动保存到 `UsingModel.json`

### 隐藏 AI 角色装备

可以为每个 AI 角色单独配置是否隐藏原有装备：

- 在模型选择界面中，选择"AI角色"目标类型
- 选择具体的 AI 角色（或选择"所有 AI 角色"来设置默认值）
- 在目标设置区域会显示针对该 AI 角色的"隐藏装备"切换选项
- 每个 AI 角色都有独立的隐藏装备设置，也可以为"所有 AI 角色"设置默认值
- **配置选择逻辑**：音频、装备隐藏等设置会根据实际使用的模型来选择配置
  - 如果 AI 角色使用的是自己的模型配置（在 `UsingModel.json` 中为该 AI 角色单独配置了模型），则使用该 AI 角色的设置
  - 如果 AI 角色使用的是回退模型（`*`，即"所有 AI 角色"的默认模型），则使用`*`的设置
- 配置会自动保存到 `HideEquipmentConfig.json`

### 注意事项

- AI 角色模型需要满足与角色模型相同的要求（定位锚点、Animator 配置等）
- 模型必须在其 `bundleinfo.json` 中明确声明支持 AI 角色（`Target` 包含 `"AICharacter"`）
- 模型必须在其 `SupportedAICharacters` 中声明支持该 AI 角色，或包含 `"*"` 表示支持所有 AI 角色
- 如果模型没有正确配置，AI 角色将使用原始模型



