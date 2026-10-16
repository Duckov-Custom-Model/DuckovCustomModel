# Duckov Custom Model

[English](README_EN.md) | 中文

[更新日志](CHANGELOG.md)

## ⚠️ 自行分发警告

如果您计划自行分发本模组，请注意以下要求：

1. **自行去除加载校验**：必须修改模组中的加载校验逻辑（位于 `DuckovCustomModel.ModLoader/ModBehaviour.cs` 的 `OnAfterSetup` 方法中）。该校验用于避免盗传，请根据您的需求进行修改。
2. **修改或去除更新逻辑**：必须修改更新检查功能的检查源（位于 `DuckovCustomModel/Managers/UpdateChecker.cs`），或完全移除该功能。自行分发版本不应使用本仓库的数据源进行更新检查。
3. **调整 info.ini 说明文本**：必须修改 `DuckovCustomModel/info.ini` 文件中的 `description` 字段，调整其中的说明文本以符合您的分发需求。该文件中的描述文本包含关于盗传的警告信息，请根据您的实际情况进行修改或删除。
4. **遵守 MIT 协议的要求**：必须遵守 [LICENSE](LICENSE) 文件中规定的 MIT 协议要求，包括保留版权声明和许可证文本

一个用于 Duckov 游戏的自定义玩家模型模组。

## 基本功能

- **自定义模型替换**：允许玩家使用自定义模型替换游戏中的角色模型
- **模型选择界面**：提供图形化界面，方便玩家浏览和选择可用模型
- **模型搜索**：支持通过模型名称、ID 等关键词搜索模型
- **模型管理**：自动扫描并加载模型包，支持多个模型包同时存在
- **增量更新**：使用哈希缓存机制，只更新变更的模型包，提高刷新效率
- **多对象支持**：每个模型目标类型可对应多个游戏对象，切换时统一应用到所有对象
- **快速切换**：支持在游戏中快速切换模型，无需重启游戏

## 配置文件

配置文件位于：`游戏安装路径/ModConfigs/DuckovCustomModel`

**⚠️ 重要提示**：从 v1.10.0 开始，所有配置文件已升级至 v2 版本，大量旧格式 API 已标记为过时。系统会自动从旧格式迁移到新格式，但建议开发者尽快迁移到新 API。详细的过时 API 列表和迁移指南请参考 [docs/OBSOLETE_APIS_v1.10.0.md](docs/OBSOLETE_APIS_v1.10.0.md)

**注意**：如果游戏安装目录为只读环境（如 macOS 上的某些安装方式），模组会自动将配置文件路径切换到游戏存档的上一级目录的 ModConfigs（Windows: `AppData\LocalLow\TeamSoda\Duckov\ModConfigs\DuckovCustomModel`，macOS/Linux: 对应的用户数据目录）。模组会自动检测并处理这种情况，无需手动配置。

### UIConfig.json

UI 界面相关配置。

```json
{
  "ToggleKey": "Backslash",
  "AnimatorParamsToggleKey": "None",
  "YsmRadialMenuKey": "Z",
  "EmotionModifierKey1": "Comma",
  "EmotionModifierKey2": "Period",
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
- `YsmRadialMenuKey`：所有支持轮盘的模型共用的开关按键（默认：`Z`）；字段名保留原有命名，适用于 Prefab、VRM 和 YSM。
- `EmotionModifierKey1`：表情快捷键修饰键1（默认：`Comma`，即逗号键 `,`）
  - 用于表情快捷键功能，按住此键 + F1-F8 可设置 `EmotionValue1` 参数（值为 0-7）
  - 支持的按键值可参考 Unity KeyCode 枚举
  - 可在设置界面中点击按钮进行设置
- `EmotionModifierKey2`：表情快捷键修饰键2（默认：`Period`，即句号键 `.`）
  - 用于表情快捷键功能，按住此键 + F1-F8 可设置 `EmotionValue2` 参数（值为 0-7）
  - 支持的按键值可参考 Unity KeyCode 枚举
  - 可在设置界面中点击按钮进行设置
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

隐藏装备配置。**⚠️ 已升级至 v2 版本，旧格式已过时。**

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

- `Version`：配置文件版本（当前为 `2`）
- `TargetTypeHideEquipment`：字典类型，键为目标类型 ID（字符串格式，如 `"built-in:Character"`、`"built-in:Pet"`、`"built-in:AICharacter_*"` 或 `"built-in:AICharacter_<角色名>"`），值为布尔类型
  - `built-in:Character`：是否隐藏角色原有装备（默认：`false`）
    - 设置为 `true` 时，角色模型的 Animator 的 `HideOriginalEquipment` 参数会被设置为 `true`
    - 可在模型选择界面的设置区域中切换此选项
  - `built-in:Pet`：是否隐藏宠物原有装备（默认：`false`）
    - 设置为 `true` 时，宠物模型的 Animator 的 `HideOriginalEquipment` 参数会被设置为 `true`
    - 可在模型选择界面的设置区域中切换此选项
  - `built-in:AICharacter_*`：所有 AI 角色的默认隐藏装备设置
  - `built-in:AICharacter_<角色名>`：特定 AI 角色的隐藏装备设置

**⚠️ 过时格式（v1）**：
- `HideEquipment` (Dictionary<ModelTarget, bool>) - 已过时，使用 `TargetTypeHideEquipment` 替代
- `HideAICharacterEquipment` (Dictionary<string, bool>) - 已过时，使用 `TargetTypeHideEquipment` 替代

**兼容性说明**：
- 系统会自动从 v1 格式迁移到 v2 格式

### UsingModel.json

当前使用的模型配置。**⚠️ 已升级至 v2 版本，旧格式已过时。**

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

- `Version`：配置文件版本（当前为 `2`）
- `TargetTypeModelIDs`：字典类型，键为目标类型 ID（字符串格式，如 `"built-in:Character"`、`"built-in:Pet"`、`"built-in:AICharacter_*"` 或 `"built-in:AICharacter_<角色名>"`），值为模型 ID（字符串，为空时使用原始模型）
  - `built-in:Character`：当前使用的角色模型 ID
    - 设置后，游戏会在关卡加载时自动应用该模型到所有角色对象
    - 可通过模型选择界面修改，修改后会自动保存到此文件
  - `built-in:Pet`：当前使用的宠物模型 ID
    - 设置后，游戏会在关卡加载时自动应用该模型到所有宠物对象
    - 可通过模型选择界面修改，修改后会自动保存到此文件
  - `built-in:AICharacter_*`：所有 AI 角色的默认模型
    - 当某个 AI 角色没有单独配置模型时，会使用此默认模型
    - 如果此键也没有配置，则使用原始模型
  - `built-in:AICharacter_<角色名>`：特定 AI 角色的模型配置
    - 可以为每个 AI 角色单独配置模型
    - 可通过模型选择界面修改，修改后会自动保存到此文件

**⚠️ 过时格式（v1）**：
- `ModelIDs` (Dictionary<ModelTarget, string>) - 已过时，使用 `TargetTypeModelIDs` 替代
- `AICharacterModelIDs` (Dictionary<string, string>) - 已过时，使用 `TargetTypeModelIDs` 替代

**兼容性说明**：
- 系统会自动从 v1 格式迁移到 v2 格式
- 如果配置文件中存在旧的 `ModelID` 或 `PetModelID` 字段，系统会自动迁移到新的 `TargetTypeModelIDs` 字典格式

### IdleAudioConfig.json

待机音频自动播放间隔配置。**⚠️ 已升级至 v2 版本，旧格式已过时。**

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

- `Version`：配置文件版本（当前为 `2`）
- `TargetTypeIdleAudioIntervals`：字典类型，键为目标类型 ID（字符串格式），值为包含 `Min` 和 `Max` 的对象
  - `built-in:Character`：玩家角色的待机音频播放间隔（秒）
    - `Min`：最小间隔时间（默认：`30.0`）
    - `Max`：最大间隔时间（默认：`45.0`）
  - `built-in:Pet`：宠物角色的待机音频播放间隔（秒）
    - `Min`：最小间隔时间（默认：`30.0`）
    - `Max`：最大间隔时间（默认：`45.0`）
  - `built-in:AICharacter_*`：所有 AI 角色的默认间隔
  - `built-in:AICharacter_<角色名>`：特定 AI 角色的间隔配置
  - 系统会在最小值和最大值之间随机选择间隔时间

- `TargetTypeEnableIdleAudio`：字典类型，键为目标类型 ID（字符串格式），值为布尔值，控制该目标类型是否允许自动播放待机音频
  - `built-in:Character`：玩家角色是否允许自动播放待机音频（默认：`false`）
  - `built-in:Pet`：宠物角色是否允许自动播放待机音频（默认：`true`）
  - `built-in:AICharacter_*`：所有 AI 角色的默认值（默认：`true`）
  - `built-in:AICharacter_<角色名>`：特定 AI 角色的配置

**⚠️ 过时格式（v1）**：
- `IdleAudioIntervals` (Dictionary<ModelTarget, IdleAudioInterval>) - 已过时，使用 `TargetTypeIdleAudioIntervals` 替代
- `AICharacterIdleAudioIntervals` (Dictionary<string, IdleAudioInterval>) - 已过时，使用 `TargetTypeIdleAudioIntervals` 替代
- `EnableIdleAudio` (Dictionary<ModelTarget, bool>) - 已过时，使用 `TargetTypeEnableIdleAudio` 替代
- `AICharacterEnableIdleAudio` (Dictionary<string, bool>) - 已过时，使用 `TargetTypeEnableIdleAudio` 替代

**注意事项**：
- 最小间隔时间不能小于 0.1 秒
- 最大间隔时间不能小于最小间隔时间
- 只有配置了 `"idle"` 标签音效的模型才会自动播放待机音效
- 只有启用了自动播放的角色类型才会自动播放待机音效（通过 `TargetTypeEnableIdleAudio` 配置控制）
- 玩家角色默认不允许自动播放待机音效，可以通过配置启用

### ModelAudioConfig.json

模型音频开关配置。**⚠️ 已升级至 v2 版本，旧格式已过时。**

```json
{
  "Version": 2,
  "TargetTypeEnableModelAudio": {
    "built-in:Character": true,
    "built-in:Pet": true,
    "built-in:AICharacter_*": true,
    "built-in:AICharacter_Cname_Wolf": true
  },
  "TargetTypeModelAudioVolume": {
    "built-in:Character": 1.0,
    "built-in:Pet": 1.0,
    "built-in:AICharacter_*": 1.0,
    "built-in:AICharacter_Cname_Wolf": 0.8
  }
}
```

- `Version`：配置文件版本（当前为 `2`）
- `TargetTypeEnableModelAudio`：字典类型，键为目标类型 ID（字符串格式），值为布尔值，控制该目标类型是否使用模型音频
  - `built-in:Character`：玩家角色是否使用模型音频（默认：`true`）
    - 设置为 `false` 时，玩家角色的所有模型音频都不会播放（包括按键触发和待机音频）
    - 可在模型选择界面的目标设置区域中切换此选项
  - `built-in:Pet`：宠物角色是否使用模型音频（默认：`true`）
    - 设置为 `false` 时，宠物角色的所有模型音频都不会播放（包括 AI 自动触发和待机音频）
    - 可在模型选择界面的目标设置区域中切换此选项
  - `built-in:AICharacter_*`：所有 AI 角色的默认值（默认：`true`）
  - `built-in:AICharacter_<角色名>`：特定 AI 角色的配置
    - 可以为每个 AI 角色单独配置是否使用模型音频
    - **配置选择逻辑**：音频设置会根据实际使用的模型来选择配置
      - 如果 AI 角色使用的是自己的模型配置（在 `UsingModel.json` 中为该 AI 角色单独配置了模型），则使用该 AI 角色的音频设置
      - 如果 AI 角色使用的是回退模型（`*`，即"所有 AI 角色"的默认模型），则使用`*`的音频设置
    - 可在模型选择界面的目标设置区域中切换此选项
- `TargetTypeModelAudioVolume`：字典类型，键为目标类型 ID（字符串格式），值为浮点数（0-1），控制该目标类型的模型音效音量
  - `built-in:Character`：玩家角色的模型音效音量（默认：`1.0`，即 100%）
    - 可在模型选择界面的目标设置区域中通过滑块调整
  - `built-in:Pet`：宠物角色的模型音效音量（默认：`1.0`，即 100%）
    - 可在模型选择界面的目标设置区域中通过滑块调整
  - `built-in:AICharacter_*`：所有 AI 角色的默认音量（默认：`1.0`，即 100%）
  - `built-in:AICharacter_<角色名>`：特定 AI 角色的音量配置
    - 可以为每个 AI 角色单独配置音量
    - 如果 AI 角色没有特定配置，会回退到 `built-in:AICharacter_*` 的配置
    - 可在模型选择界面的目标设置区域中通过滑块调整

**⚠️ 过时格式（v1）**：
- `EnableModelAudio` (Dictionary<ModelTarget, bool>) - 已过时，使用 `TargetTypeEnableModelAudio` 替代
- `AICharacterEnableModelAudio` (Dictionary<string, bool>) - 已过时，使用 `TargetTypeEnableModelAudio` 替代

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
  - **参数显示界面**：用于实时查看和监控 Animator 参数的值
    - **打开方式**：
      - 在设置界面中点击"显示动画器参数"开关，或使用配置的快捷键打开
      - 按 `ESC` 键或点击窗口右上角的关闭按钮可关闭界面
    - **角色切换**：通过顶部的下拉框选择不同的角色，查看其 Animator 参数
      - 下拉框会显示所有可用的角色（包括主角色、宠物和 AI 角色）
      - 每个角色会显示其名称、哈希值和距离信息
    - **类型过滤**：通过类型过滤下拉框筛选参数类型
      - 支持多选过滤：`float`、`int`、`bool`、`trigger`
      - 可以同时选择多个类型进行过滤
    - **使用状态过滤**：通过使用状态过滤下拉框筛选参数的使用状态
      - 支持多选过滤：`已使用`、`未使用`
      - "已使用"表示该参数在 Animator Controller 中被使用
      - "未使用"表示该参数在 Animator Controller 中未被使用
    - **搜索功能**：支持通过搜索框快速查找参数
      - **普通搜索**：直接输入参数名称的关键词，支持大小写不敏感匹配
        - 默认情况下，所有输入都按普通搜索处理
        - 例如：输入 `Move` 可以匹配所有包含 "Move" 的参数（如 "MoveSpeed"、"MoveDirX" 等）
      - **正则表达式搜索**：使用 JavaScript 风格的正则表达式格式，可以更精确地匹配参数名称
        - **格式**：使用 `/pattern/` 格式来启用正则表达式搜索
        - **说明**：正则表达式搜索默认忽略大小写
        - **示例**：
          - `/^Move.*/` 可以匹配所有以 "Move" 开头的参数（如 "MoveSpeed"、"MoveDirX"）
          - `/.*Speed$/` 可以匹配所有以 "Speed" 结尾的参数（如 "MoveSpeed"、"AmmoRate" 不会匹配）
          - `/(Move|Run|Dash).*/` 可以匹配包含 "Move"、"Run" 或 "Dash" 的参数
          - `/^[A-Z].*/` 可以匹配所有以大写字母开头的参数
        - **注意**：只有使用 `/pattern/` 格式时才会启用正则表达式搜索，否则按普通搜索处理
      - 搜索会实时过滤参数列表，支持与类型过滤和使用状态过滤组合使用
    - **参数显示**：
      - 参数以网格布局显示，每个参数显示其名称、类型和当前值
      - 参数值会实时更新，显示当前 Animator 中的实际值
      - 参数颜色会根据状态变化：
        - **白色**：参数值与初始值相同
        - **黄色**：参数值与初始值不同（已改变）
        - **橙色**：参数值正在变化中
    - **窗口操作**：
      - 支持拖拽窗口标题栏移动窗口位置
      - 支持拖拽窗口边缘调整窗口大小
      - 窗口大小有最小限制（400x300 像素）
  - **表情快捷键修饰键**：配置表情快捷键功能的两个修饰键
    - 修饰键1（默认：逗号键 `,`）：按住此键 + F1-F8 可设置 `EmotionValue1` 参数（值为 0-7）
    - 修饰键2（默认：句号键 `.`）：按住此键 + F1-F8 可设置 `EmotionValue2` 参数（值为 0-7）
    - 可在设置界面中点击按钮进行设置
    - 操作方式：按住修饰键后，按 F1-F8 键即可设置对应的表情参数值
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

### 身高与物品大小

Prefab、VRM 和 YSM 共用模型调整界面。选定目标和模型后，可在支持的模型上调整身高；物品大小按装备挂点分别折叠显示，倍率范围为 `0.1`–`5`，`1` 保持原大小。调整倍率与模型、动画原有的缩放叠加，可单项重置或全部还原。

这些设置按目标类型和模型分别保存到配置目录的 `RuntimeData`，下次使用自动恢复。“所有 AI”不保存这类单模型运行时设置，需要选择具体 AI。更改模型 ID 后会使用另一份设置。

### 打开模型选择界面

- 默认按键：`\`（反斜杠键）
- 可通过修改 `UIConfig.json` 中的 `ToggleKey` 来更改按键
- 按 `ESC` 键可关闭界面
- **DCM 按钮**：在主菜单或背包界面时，屏幕会显示一个固定的 DCM 按钮（默认位置：左上）
  - 点击按钮可以快速打开/关闭模型选择界面
  - 按钮位置和显示状态可在设置界面中配置
  - 按钮位置支持 9 个锚点位置（左上、上中、右上、左中、中央、右中、左下、下中、右下）和自定义偏移值
  - 配置更改后会立即应用到按钮位置，无需重启游戏

### 动作轮盘

Prefab、VRM 和 YSM 使用相同的轮盘操作。Humanoid 模型还可直接选用[外部动作](#外部动作与配套音频)。模型需要提供有效的菜单与动作：Prefab／VRM 使用 [RadialMenus 配置](#prefab-与-vrm-的动作轮盘)，YSM 读取模型自带菜单。

- 默认按 **Z** 打开或关闭轮盘，可在设置页更改按键；**Esc** 关闭。
- 每页显示八项，滚轮翻页；点击动作执行，点击分类进入子菜单，点击返回项回到上层。
- 内环配置入口打开对应表单，可调整复选框、数值或选项；表单值自动保存。
- 左键点击轮盘中心切换动作锁定：未锁定时，移动输入取消附加动作并恢复常规动画；锁定后，角色可带着动作移动。播放过程中也可随时切换。
- 右键点击轮盘中心停止当前附加动作及配套音频。
- 关闭轮盘不会停止动作。同一模型重开保留分类和页数，切换模型时重置。

动作如何结束、循环或被打断取决于模型配置：Prefab／VRM 由 Animator 参数与状态机处理，YSM 遵循模型自带动作设置。

### 外部动作与配套音频

有效的 Humanoid Animator 模型可直接播放 `.vmd`（MMD 动作）和 `.vrma`，包括 Humanoid Prefab 与 VRM，无需为每个动作制作控制器。YSM 和非 Humanoid Prefab 尚不支持这类外部动作；YSM 自带动作照常使用原有菜单。

动作可随模型分发，无需放入固定目录：在模型菜单的 `Actions` 中通过 `Motion` 引用相对模型文件／AssetBundle 所在目录的动作，例如 `motions/dance.vmd`；同 AssetBundle 内的动作也可作为 TextAsset 提供。模型自带文件通过菜单显式引用，不会自动扫描整个模型目录。

需要多个模型共用时，也可把文件放入 `ModConfigs/DuckovCustomModel/Motions`，刷新模型列表后，在轮盘的“外部动作”分类中选择。此公共目录会自动扫描，允许子文件夹。与动作同名的 `.ogg`、`.wav` 或 `.mp3` 自动作为配套音频，例如 `dance.vmd` 和 `dance.ogg`；没有音频就只播放动作。

需要调整显示名称或播放方式时，在动作旁添加可选的 `dance.motion.json`：

```json
{
  "Name": "舞蹈",
  "Playback": {
    "Audio": "dance.ogg",
    "Loop": true,
    "AudioOffset": 0,
    "Volume": 0.8
  }
}
```

模型自带动作的配置示例：将下面的动作放入模型配置中 `RadialMenus` 的 `Actions`，即可与参数动作混排，放入任意菜单或子菜单。动作文件放在模型旁的 `motions/dance.vmd`，同名音频可放在 `motions/dance.ogg`：

```json
{
  "Id": "dance",
  "Name": "舞蹈",
  "Motion": "motions/dance.vmd",
  "Playback": { "Loop": true }
}
```

- `Motion`：动作路径，与 `Parameter` 二选一。模型菜单优先查同 AssetBundle 的 TextAsset（可用 `.bytes` 导入），再查相对模型文件／AssetBundle 所在目录的文件，最后查公共 Motions 目录。使用相对路径，不能越出相应目录。
- `Playback`（可选）：播放设置；省略时使用以下默认值。
- `Playback.Audio`：配套音频路径；省略时自动查同名音频。公共动作的配置相对动作所在文件夹，模型菜单的配置相对模型文件／AssetBundle 所在目录。音频以外部文件提供。
- `Playback.Loop`：是否循环，默认 `false`。动作与音频一起重新开始；再次选择同一动作从头重播。
- `Playback.Speed`：速度倍率，默认 `1`，范围 `0.1`–`5`，同时调整动作与音频速度。
- `Playback.AudioOffset`：音频起播偏移，单位秒，默认 `0`；正数延后起播，负数跳过音频开头。
- `Playback.Volume`：音量，默认 `1`，范围 `0`–`1`，叠加目标的模型音量并遵循模型音频开关。
- `Playback.SpatialAudio`：是否从角色位置播放，默认 `true`；设为 `false` 使用二维音频。
- `Playback.InterruptOnMove`：移动输入是否取消动作，默认 `true`；轮盘中心的运行时锁定开关优先，锁定后可带着动作移动。击退或惯性位移不会按移动输入处理。翻滚、近战、使用物品、瞄准、射击及死亡仍会让出控制。
- `Playback.BlendIn`、`Playback.BlendOut`：进入与退出的混合秒数，默认均为 `0.2`。停止后恢复原有动画和持枪 IK。
- `Playback.InPlace`：限制场景根节点位移，默认 `true`；动作仍可在模型内部产生身体位移，不会驱动游戏角色走动。
- `Playback.UpperBodyOnly`：仅覆盖上半身，默认 `false`。
- `Playback.PositionScale`：动作位移倍率，默认 `1`，大于 `0`，不大于 `100`；基础位移按人形比例适配。
- `Playback.RotationY`：VMD 坐标朝向偏转，默认 `180` 度；VRMA 按其人形坐标处理。
- `Playback.ArmAngle`：VMD 初始手臂下垂角，默认 `40` 度，可在 `-90`–`90` 间调整。
- `Playback.BoneMappings`：VMD 骨骼名到 Unity `HumanBodyBones` 名称的覆盖映射，例如 `{ "左腕": "LeftUpperArm" }`。
- `Playback.ExpressionMappings`：表情名称覆盖映射；VRM 使用表情名称，Prefab 使用 BlendShape 名称，例如 `{ "あ": "aa" }`。

关闭轮盘继续播放；右键点击轮盘中心结束当前动作和音频。游戏暂停时同步暂停，恢复后继续。接口 `ModelHandler.SetRadialActionPaused(bool)` 可单独暂停或恢复外部动作。换模型、强制重载和卸载会取消未完成的加载并释放播放资源。

VMD 支持常用人形与手指骨骼、贝塞尔插值、表情和足部 IK 开关；MMD 专用附加骨骼、继承约束和完整 PMX 物理不会自动转移到其他骨架，非标准动作可能需要映射和姿势调整。镜头、灯光轨道不接管游戏场景。VRMA 使用 UniVRM 的人形重定向；模型自己的动骨继续运行。

动作按需加载，共享解析／导入缓存；刷新会读取新增或更改的文件，强制重载会清空缓存。外部动作路径不依赖 Unity 编辑器、系统日文代码页或额外的 Windows 原生插件，适用于游戏支持的桌面平台。

## 模型安装

将模型包放置在：`游戏安装路径/ModConfigs/DuckovCustomModel/Models`

**注意**：如果游戏安装目录为只读环境，模组会自动将模型路径切换到游戏存档的上一级目录的 ModConfigs（Windows: `AppData\LocalLow\TeamSoda\Duckov\ModConfigs\DuckovCustomModel\Models`，macOS/Linux: 对应的用户数据目录）。模组会自动检测并处理这种情况，无需手动配置。

Prefab 使用包含 `bundleinfo.json` 和 AssetBundle 的模型包文件夹；YSM 可使用单文件或模型文件夹；VRM 可直接使用 `.vrm` 文件。各格式均从上面的 Models 目录扫描，刷新后在同一界面选择。安装模组时保留 `Resources` 子目录。

### YSM 模型

将 `.ysm` 文件或包含 `ysm.json`／`main.json` 的模型文件夹放入 `ModConfigs/DuckovCustomModel/Models`，刷新模型列表后选用，无需转换成 AssetBundle。

模型自带的函数、GUI 表单和附加动作菜单随模型一起读取，不需要在侧车中重复定义。操作方式见[动作轮盘](#动作轮盘)。

#### 侧车配置

需要调整游戏绑定时，在 `fox.ysm` 旁添加 `fox.ysm.duckov.json`；文件夹模型 `fox/` 则使用其中的 `fox.ysm.duckov.json`。例如：

```json
{
  "Name": "自定义模型",
  "ModelTarget": "player/main",
  "TargetTypes": ["built-in:Character", "built-in:AICharacter_*"],
  "Scale": 1.0,
  "HeadPitchLimit": 45,
  "HeadYawLimit": 85,
  "LocatorMappings": {
    "RightHandLocator": "模型内实际定位器名称"
  }
}
```

- `ModelID`、`Name`、`Author`、`Description`：模型标识、名称、作者、描述；省略时使用文件或模型自带信息。
- `ThumbnailPath`：相对 YSM 所在目录／模型文件夹的图片。未填写时查找模型旁的同名 PNG，文件夹模型查找 `thumbnail.png`、`preview.png`，也可使用包内缩略图。
- `ModelTarget`：模型中的目标（`"player/main"`）。
- `TargetTypes`：角色、宠物、全部／指定 AI 或已注册扩展类型；省略时为角色和全部 AI，见[通用配置](#字段说明)。
- `Scale`：整体缩放（`1`，大于 0）。
- `HeadPitchLimit`、`HeadYawLimit`：头部俯仰、左右转动上限，单位为度（`45`、`85`，不小于 0）。
- `HeadPitchSign`、`HeadYawSign`：头部角度方向（`1`）；反向模型可填 `-1`。
- `LimbSwingScale`：行走摆动倍率（`1`，不小于 0）。
- `TeleportDistance`：判定瞬移的距离（`8`，大于 0）。
- `AttackDurationSeconds`、`HurtDurationSeconds`：攻击、受伤状态持续秒数（均为 `0.3`，大于 0）。
- `ItemCategories`、`ItemUseAnimations`、`ItemTags`：以游戏物品 TypeID 为键，分别指定物品分类、使用动画类型（如 `eat`）和标签数组，如 `"ItemCategories": { "123": "pistol" }`；请替换为实际物品 ID。
- `FireAnimation`、`ReloadAnimation`、`DashAnimation`：指定模型中的射击、换弹、冲刺动画名；需使用模型实际存在的动画。
- `LocatorMappings`：游戏[挂点名](#定位锚点)到模型真实定位器名的映射；缺失时保留原角色挂点。
- `LocatorOffsets`：按挂点名设置 `Position`、`Rotation` 三元素数组，分别为局部位移和角度。
- `Ik`：[持枪手臂 IK](#持枪-ik)，YSM 需显式指定上臂、前臂、手骨骼。
- `ValueCompatibility`、`HandCompatibility`：表达式值与持物动画兼容模式，默认 `"Ysm241"`、`"Version241"`；另可选择 `"Default"`、`"Modern"`。通常保持默认。
- `BundlePath`：相对 YSM 所在目录／模型文件夹的附加 AssetBundle。
- `DeathLootBoxPrefabPath`、`DeathLootBoxYsmPath`、`DeathLootBoxAnimation`：尸体 Prefab 路径、另一 YSM 文件路径及其展示动画，见下文。

#### 游戏状态

Molang 状态从当前角色读取，包括移动、生命、物品使用和受伤等状态；`ysm.food_level` 使用角色能量比例映射到 0–20。`query.time_stamp`、`query.time_of_day` 使用游戏时钟，`query.day_number`、`query.moon_phase` 使用游戏日期；没有世界时钟的界面使用模型预览状态。物品使用、受伤计时、装备数量和移动输入分别可通过 `query.is_using_item`、`query.hurt_time`、`query.equipment_count`、`ysm.xxa`／`ysm.yya`／`ysm.zza` 读取。按状态命名的控制器仅在对应状态下运行。

### VRM 模型

将 `.vrm` 文件放入 `ModConfigs/DuckovCustomModel/Models`，刷新模型列表后选用。支持 VRM 0.x 和 1.0；未指定动画控制器时使用内置动作。

可在模型旁添加 `fox.vrm.duckov.json`：

```json
{
  "Name": "自定义模型",
  "TargetTypes": ["built-in:Character", "built-in:AICharacter_*"],
  "BundlePath": "animations.bundle",
  "Vrm": {
    "AnimatorControllerPath": "Assets/Animations/Character.controller",
    "Scale": 1.0,
    "RotationY": 0,
    "Locators": {
      "RightHandLocator": {
        "Bone": "RightHand",
        "Position": [0, 0, 0],
        "Rotation": [0, 0, 0]
      }
    }
  }
}
```

- `BundlePath`：相对 `.vrm` 所在目录的 AssetBundle 文件。仅使用内置动画且没有包内附加资源时可省略。
- `Vrm.AnimatorControllerPath`：该包内的 Animator Controller 或 Animator Override Controller 资源路径；省略或留空使用内置控制器。路径不是磁盘上的 `.controller` 文件路径。
- `Vrm.Scale`：模型缩放，默认 `1`，必须大于 `0`；`Vrm.RotationY`：朝向偏转角度，默认 `0`。
- `Vrm.Locators`：按[挂点名称](#定位锚点)调整装备位置。`Bone` 使用 Humanoid 骨骼名（如 `Head`、`Chest`、`RightHand`）；`Position`、`Rotation` 分别是相对骨骼的局部位移和欧拉角，均为三个数字。未配置的常用挂点自动绑定。

自定义动画需在 Unity 中按 **Humanoid** 导入并制作控制器，将控制器与其动画依赖一起打入 AssetBundle，再填写上述路径。模组不会直接加载散放的 FBX。控制器按[通用 Animator 参数](#动画器配置)设置状态切换，例如 `Moving`、`Running`、`Dashing`（Bool）、`MoveSpeed`（Float）、`Shoot`（Trigger）。角色位移由游戏控制，Root Motion 不会驱动角色。

侧车也接受[通用模型配置](#字段说明)，包括名称、作者、版本、缩略图、目标类型、音效、Buff 参数、[动作轮盘](#prefab-与-vrm-的动作轮盘)和 `Ik`。使用内置动画时默认开启持枪 IK；使用自定义控制器时需显式添加 `"Ik": { "Enabled": true }` 才启用，也可设为 `false` 关闭。

在模型包中使用 VRM 时，填写 `"SourceKind": "Vrm"`、`"SourcePath": "Assets/Models/fox.vrm"`，将 `Vrm` 写在该模型项内；资源查找方式见[共用配置与资源路径](#共用配置与资源路径)。

### AssetBundle 模型包结构

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
  "SpriteAtlasPaths": [
    "Assets/Sprites/Emojis.asset",
    "Assets/Sprites/Icons.asset"
  ],
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

#### 字段说明

**BundleName**（必需）：模型包名称，用于标识和显示

**BundlePath**（必需）：AssetBundle 文件路径，相对于模型包文件夹的路径

**SpriteAtlasPaths**（可选）：TextMeshPro Sprite Asset 路径数组，用于在 TMP 文本中渲染自定义 Sprite
- 路径为 AssetBundle 内的资源路径（如 `"Assets/Sprites/Emojis.asset"`）
- 系统会自动加载并注册到 TMP 的全局 fallback sprite assets 列表
- 在 TMP 文本中使用 `<sprite name="sprite_name">` 语法引用

**Models**（必需）：模型信息数组，可包含多个模型

**ModelInfo 字段**（通用字段的填写位置见[共用配置与资源路径](#共用配置与资源路径)）：

- `ModelID`（必需）：模型的唯一标识符，用于在配置文件中引用模型
- `Name`（可选）：模型在界面中显示的名称
- `Author`（可选）：模型作者
- `Description`（可选）：模型描述信息
- `Version`（可选）：模型版本号
- `ThumbnailPath`（可选）：缩略图路径，相对于模型包文件夹的外部文件路径（如 `"thumbnail.png"`）
- `SourceKind`（可选）：`AssetBundle`（默认）、`Ysm` 或 `Vrm`。选择 `Ysm`／`Vrm` 时用 `SourcePath` 指定 `.ysm`／`.vrm` 文件；先读取同包内的 `TextAsset`（可用 `.bytes` 导入），找不到时读取 AssetBundle 文件所在目录的相对路径
- `PrefabPath`（Prefab 模型必需）：模型 Prefab 在 AssetBundle 内的资源路径（如 `"Assets/Model.prefab"`）
- `DeathLootBoxPrefabPath`（可选）：死亡战利品箱 Prefab 在 AssetBundle 内的资源路径（如 `"Assets/DeathLootBox.prefab"`）
- `DeathLootBoxYsmPath`（可选）：死亡战利品箱使用另一个 `.ysm` 文件；先查同包，再查 AssetBundle 文件所在目录的相对路径。可用 `DeathLootBoxAnimation` 指定展示动作；同时指定 Prefab 时优先使用 Prefab。YSM 模型也可反向指定 `DeathLootBoxPrefabPath`
  - 当角色使用该模型并死亡时，如果配置了此字段，死亡战利品箱会使用自定义的 Prefab 替换默认模型
  - 如果未配置此字段，死亡战利品箱将使用默认模型
- `TargetTypes`（可选）：模型适用的目标类型 ID 数组（默认：`["built-in:Character"]`）
  - 使用字符串格式的目标类型 ID，支持内置类型和扩展类型
  - 内置类型示例：`"built-in:Character"`（角色）、`"built-in:Pet"`（宠物）、`"built-in:AICharacter_*"`（所有 AI 角色）、`"built-in:AICharacter_<角色名>"`（特定 AI 角色）
  - 扩展类型示例：`"extension:CustomType"`（由第三方扩展注册的自定义类型）
  - 可以同时包含多个值，表示该模型同时适用于多个目标类型
  - 模型选择界面会根据当前选择的目标类型过滤显示兼容的模型
  - **示例**：
    - 适用于角色和所有 AI 角色：`["built-in:Character", "built-in:AICharacter_*"]`
    - 适用于特定 AI 角色：`["built-in:AICharacter_Cname_Wolf", "built-in:AICharacter_Cname_Scav"]`
    - 适用于角色、宠物和所有 AI 角色：`["built-in:Character", "built-in:Pet", "built-in:AICharacter_*"]`

**⚠️ 过时字段（v1.10.0 起已过时，但仍支持向后兼容）**：
- `Target`（可选）：模型适用的目标类型数组（已过时，使用 `TargetTypes` 替代）
  - 可选值：`"Character"`（角色）、`"Pet"`（宠物）、`"AICharacter"`（AI 角色标记）
  - 系统会自动从 `Target` 和 `SupportedAICharacters` 迁移到 `TargetTypes`
  - **注意**：`"AICharacter"` 只是一个标记，表示需要处理 `SupportedAICharacters`，它本身不会被转换为目标类型
- `SupportedAICharacters`（可选）：支持的 AI 角色名称键数组（已过时，使用 `TargetTypes` 替代）
  - 仅在 `Target` 包含 `"AICharacter"` 时有效
  - 可以指定该模型适用于哪些 AI 角色
  - 特殊值 `"*"`：表示该模型适用于所有 AI 角色
  - 如果为空数组且 `Target` 包含 `"AICharacter"`，则该模型不会应用于任何 AI 角色
  - **重要**：如果 `Target` 中没有 `"AICharacter"` 标记，即使 `SupportedAICharacters` 有值，也不会被处理
  - 系统会自动将 `Target` 和 `SupportedAICharacters` 转换为 `TargetTypes` 格式（如 `"built-in:AICharacter_*"` 或 `"built-in:AICharacter_<角色名>"`）
- `CustomSounds`（可选）：自定义音效信息数组，支持为音效配置标签
  - 每个音效可以配置多个标签（`normal`、`surprise`、`death`）
  - 未指定标签时，默认为 `["normal"]`
  - 同一音效文件可以同时用于多个场景
  - 音效文件路径在 `Path` 中指定，相对于模型包文件夹
- `SoundTagPlayChance`（可选）：音效标签播放概率配置
  - 字典类型，键为音效标签（不区分大小写），值为播放概率（0-100）
  - 当触发该标签的音效时，会根据配置的概率决定是否播放
  - 如果未配置，则始终播放；概率为 0 时不播放
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
    {
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
    }
    ```
  - 配置的 Buff 参数会在调试界面中显示，位于自定义参数和动画器参数之后

- `Features`（可选）：`"NoAutoShaderReplace"` 保留自带 Shader；`"SkipShowBackMaterial"` 跳过遮挡背面材质处理。
- `Vrm`（可选）：VRM 缩放、朝向、挂点和控制器，见 [VRM 模型](#vrm-模型)。
- `Ik`（可选）：持枪手臂适配，见下文。
- `RadialMenus`（可选）：Prefab／VRM 的菜单、动作及表单配置，见[动作轮盘配置](#prefab-与-vrm-的动作轮盘)。

### 共用配置与资源路径

模型包内的 Prefab、YSM 和 VRM 都在 `bundleinfo.json` 的 `Models` 项中填写通用 `ModelInfo` 字段。独立 VRM 在 `<文件名>.vrm.duckov.json` 根对象中填写这些字段；独立 YSM 的 `<文件名>.ysm.duckov.json` 使用 [YSM 专用字段](#侧车配置)，不能直接照搬全部通用字段。

同一 `Models` 数组可混用三种模型。YSM／VRM 用 `SourceKind` 和 `SourcePath` 指定资源：优先读取同包内的 `TextAsset`（可通过 `.bytes` 导入），找不到时读取 AssetBundle 文件所在目录的相对文件。YSM 的游戏绑定侧车仍放在磁盘对应路径旁，例如 `Assets/Models/fox.ysm.duckov.json`；展示信息、目标类型、IK 和尸体配置写在该模型项中。

`DeathLootBoxPrefabPath` 和 `DeathLootBoxYsmPath` 是共用的死亡战利品箱替换配置，可跨模型格式引用 Prefab 或另一个 YSM，同时指定时 Prefab 优先。Prefab 从同包读取；YSM 优先查同包，再查相对文件。独立 YSM／VRM 可用 `BundlePath` 指定附加资源包。独立 YSM 的相对路径以模型所在目录为基准，不能指向 Models 之外；模型包资源的外部路径以 AssetBundle 文件所在目录为基准。`DeathLootBoxAnimation` 指定目标 YSM 的展示动画。当前没有 VRM 尸体路径字段。

### Prefab 动骨

在 SDK 中给模型根物体添加 `Duckov Custom Model > Spring Bones`，将头发、裙摆等根骨骼加入骨链。可锁定 Inspector 后多选骨骼，点击“Add Selected Roots”；无分叉时自动延伸，有分叉时指定末端骨骼。刚度控制回弹，阻尼控制惯性衰减，重力和碰撞半径可按链调整。

在头部、身体等骨骼上添加“Spring Bone Collider”，配置球体或胶囊体，再点击“Collect Model Colliders”。碰撞范围可在 Scene 中拖动调整，仅影响动骨。惯性参考可留空；指定骨骼后，惯性相对该骨骼计算，参考骨骼不可参与模拟。

在场景或 Prefab 编辑模式中点击“Start Preview”，可选动画和往返移动测试；停止预览、保存或进入播放模式会还原预览姿势。打包后自动运行，无需 VRM 组件或额外 JSON。不要让其他动骨组件同时驱动相同骨骼。

### 持枪 IK


在模型配置中添加 `"Ik": { "Enabled": true }`。Humanoid Prefab/VRM 可自动使用手臂骨骼；其他 Prefab 需填写骨骼路径，YSM 需填写模型内唯一的骨骼名称或 ID。两条手臂分别配置，骨骼必须构成上臂、前臂、手的父子链。

- `Enabled`、`Weight`、`BlendSpeed`：启用（`true`）、影响权重（`1`，范围 0–1）、混合速度（`12`，大于 0）。
- `RightHand`、`LeftHand`：是否控制右手、左手（均为 `true`）。
- `RightArm`、`LeftArm`：各包含 `UpperArmPath`、`LowerArmPath`、`HandPath`。Prefab/VRM 使用相对模型根节点的 `/` 分隔路径；YSM 使用骨骼名称或 ID。
- `RightHandOffset`：瞄准方向坐标系中的右手目标偏移，按臂长缩放（`[-0.1, -0.25, 0.6]`）。
- `LeftHandSocket`：枪械上的左手挂点名（`"LeftHand"`）。
- `LeftHandOffset`、`LeftHandRotation`：左手目标的枪械局部位移偏移、旋转角度偏移（均为 `[0, 0, 0]`）。

未填写 `Ik` 时，仅使用内置动画的 VRM 默认启用；自定义控制器、普通 Prefab 和 YSM 需主动配置。换弹、使用物品、近战和冲刺等动作会让出手臂控制。

### Prefab 与 VRM 的动作轮盘

Prefab 与 VRM 共用 `RadialMenus` 配置，支持多个菜单、子菜单、动作和参数表单。

- 模型包内的 Prefab／VRM：在 `bundleinfo.json` 对应的 `Models` 项中填写 `RadialMenus`。
- 独立 VRM：在模型旁的 `fox.vrm.duckov.json` 根对象中填写 `RadialMenus`，与 `Vrm` 同级。
- YSM：直接读取模型自带菜单，见 [YSM 模型](#ysm-模型)。

模型实际使用的 Animator Controller 必须包含对应参数及动画状态过渡。VRM 如需通过 Animator 控制器新增自定义动作，需通过 `BundlePath` 和 `Vrm.AnimatorControllerPath` 配置包含这些动作的控制器；仅填写菜单不会自动生成动画。以下配置可放入上述模型项或 VRM 配置文件的根对象：

```json
{
  "RadialMenus": [
    {
      "Id": "gestures", "Name": "动作",
      "Actions": [
        {
          "Id": "wave", "Name": "挥手", "Parameter": "Wave", "Mode": "Trigger",
          "Forms": [
            { "Id": "style", "Name": "动作风格", "Parameter": "Style", "Mode": "Int",
              "Options": [{ "Name": "普通", "Value": 0 }, { "Name": "活泼", "Value": 1 }] },
            { "Id": "speed", "Name": "速度", "Parameter": "Speed", "Mode": "Float",
              "Min": 0.5, "Max": 2.0, "Step": 0.1 }
          ]
        }
      ],
      "Menus": [
        {
          "Id": "poses", "Name": "姿势",
          "Actions": [
            { "Id": "pose", "Name": "摆姿势", "Parameter": "Pose", "Mode": "Bool" },
            { "Id": "mood", "Name": "表情 2", "Parameter": "Mood", "Mode": "Int", "Value": 2 }
          ]
        }
      ]
    }
  ]
}
```

菜单和动作各有 `Id` 与 `Name`。`Id` 在同类项目中须唯一，`Name` 是轮盘上的文字。参数动作的 `Parameter` 是 Animator 参数名；`Mode` 可为 `Trigger`（默认）、`Bool`、`Int` 或 `Float`。`Int`／`Float` 动作点击时将 `Value` 写入 Animator。选择另一动作或右键点击轮盘中心停止会把当前 `Bool`／`Int`／`Float` 参数还原为 Animator 默认值；`Trigger` 的结束与返回由 Animator 状态机控制。关闭轮盘不会停止动作。

动作可包含 `Forms` 以在扇区内侧打开右侧配置列表。表单绑定 `Bool` 时显示开关；`Int` 带 `Options` 时显示单选项，否则用 `Min`／`Max` 显示整数滑杆；`Float` 用 `Min`／`Max`／`Step` 显示范围滑杆。表单设置按目标和模型保存，可在列表中一键还原为 Animator 默认值。

参数名不能与模组驱动的 Animator 参数或 `BuffAnimatorParams` 重复；类型不匹配、缺少参数及重复的 `Id`／`Parameter` 会使对应动作从轮盘中被过滤，空菜单也不会显示。最多可嵌套五层菜单。未配置有效动作时不显示动作轮盘。操作方式见[动作轮盘](#动作轮盘)。

## 定位锚点

装备绑定使用各模型共用的定位锚点名称：

- Prefab：在模型层级中创建对应名称的 Transform。
- VRM：按 Humanoid 骨骼自动创建常用挂点，用 `Vrm.Locators` 调整骨骼、位置和旋转。
- YSM：使用模型定位器，通过 `LocatorMappings` 映射名称，`LocatorOffsets` 调整偏移。

以下名称表示同一装备槽位，不要求 VRM／YSM 手工创建 Prefab GameObject。

### 常用定位锚点

根据模型需要提供以下装备槽位：

- `LeftHandLocator`：左手定位锚点，用于绑定左手装备
- `RightHandLocator`：右手定位锚点，用于绑定右手装备
- `ArmorLocator`：护甲定位锚点，用于绑定护甲装备
- `HelmetLocator`：头盔定位锚点，用于绑定头盔装备
- `FaceLocator`：面部定位锚点，用于绑定面部装备
- `BackpackLocator`：背包定位锚点，用于绑定背包装备
- `MeleeWeaponLocator`：近战武器定位锚点，用于绑定近战武器装备
- `PopTextLocator`：弹出文本定位锚点，用于显示弹出文本
- `VehicleLocator`：载具定位锚点，用于指示玩家角色在载具中的位置

### 可选的定位锚点

模型还可以提供以下定位锚点：

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

- 模组从模型层级或对应格式的挂点映射取得定位锚点
- 找到的定位锚点会被设置为游戏装备系统的绑定点
- 装备会按照定位锚点的位置和旋转进行绑定
- 缺少自定义挂点时回退到原角色挂点；若需装备准确跟随自定义模型，应提供对应挂点

### 注意事项

- 定位锚点的名称必须**完全匹配**（区分大小写）
- Prefab 的定位锚点可以是空的 GameObject，只需要设置正确的位置和旋转
- 建议根据原始模型的装备位置来设置定位锚点的位置

## 动画器配置

Prefab 和 VRM 共用 Animator 参数、动画层及状态机行为组件。Prefab 在模型中配置 `Animator` 和控制器；VRM 默认使用内置控制器，也可通过 `Vrm.AnimatorControllerPath` 指定自定义控制器。

游戏状态由通用数据更新逻辑提供。YSM 通过 Molang 的 `query.*`、`ysm.*` 等变量读取适配后的状态；它不直接使用下表的 Animator 参数名。具体变量、单位和默认值以各接口的适配为准，不能仅改名互换。

新增移动与动作参数也提供 YSM Molang 入口，读取同一角色的通用参数缓存：

- `LocomotionX`／`LocomotionY` → `query.duckov_locomotion_x`／`query.duckov_locomotion_y`
- `LocomotionRate` → `query.duckov_locomotion_rate`
- `RollPose` → `query.duckov_roll_pose`
- `RollProgress` → `query.duckov_roll_progress`
- `AttackProgress` → `query.duckov_attack_progress`

这些数据适用于玩家、宠物、AI 和通过角色控制器接入的扩展目标；目标没有的动作或装备按对应参数的空闲值提供。YSM 的原有变量保持各自语义，上述入口用于模型主动适配。

### Animator Controller 参数

Prefab 与 VRM 的 Animator Controller 共用以下参数，按需在控制器中声明对应名称和类型：

#### Bool 类型参数

- `Grounded`：角色是否在地面上
- `Die`：角色是否死亡
- `Moving`：角色是否正在移动
- `Running`：角色是否正在奔跑
- `Dashing`：角色是否正在冲刺
- `RollPose`：当前是否正在执行翻滚动作（`CA_Dash.Running`）
- `GunReady`：枪械是否准备就绪
- `Loaded`：枪械是否已装弹（持枪时按当前弹量更新，空手时为 `false`）
- `Reloading`：是否正在装弹
- `RightHandOut`：右手是否伸出
- `ActionRunning`：是否正在执行动作（由 `CharacterMainControl.CurrentAction` 决定）
- `UsingItem`：是否正在执行使用物品动作
- `Hidden`：角色是否处于隐藏状态
- `Invisible`：角色是否处于隐身状态
- `BodyInWater`：身体是否在水中
- `HeadInWater`：头部是否在水中
- `FootInWater`：脚部是否在水中
- `Swimming`：角色是否正在游泳
- `ThermalOn`：热成像是否开启
- `InAds`：是否正在瞄准（ADS - Aim Down Sights）
- `HideOriginalEquipment`：是否隐藏原有装备（由 `HideEquipmentConfig.json` 中对应目标类型 ID 的配置控制）
- `LeftHandEquip`：左手槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `RightHandEquip`：右手槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `ArmorEquip`：护甲槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `HelmetEquip`：头盔槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `HeadsetEquip`：耳机槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `FaceEquip`：面部槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `BackpackEquip`：背包槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `MeleeWeaponEquip`：近战武器槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `HavePopText`：是否有弹出文本（检测弹出文本槽位是否有子对象）
- `Sleeping`：角色是否处于睡眠状态
- `IsVehicle`：角色是否为载具
- `IsControllingOtherCharacter`：角色是否正在控制其他角色
- `IsControllingVehicle`：角色是否正在控制载具（为 `true` 时，`IsControllingOtherCharacter` 必定为 `true`）
- `IsPlayerControlling`：角色是否为当前玩家正在操作的角色

#### Float 类型参数

- `MoveSpeed`：移动速度比例（正常走 0~1，跑步可达 2）
- `LocomotionX`、`LocomotionY`：平滑后的模型 Animator 局部平面移动方向乘步态速度，X 向右、Y 向前；向量长度约为 0（停止）、1（行走）、2（奔跑），可用于二维 Blend Tree
- `LocomotionRate`：步频倍率，静止时为 1；移动时根据当前／原始步行速度调整，范围 0.1–5
- `RollProgress`：翻滚动作进度（0–1）；结束后保留最后采样值，供动画淡出使用
- `AttackProgress`：近战攻击计时的归一化进度（0–1），非攻击时为 0
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
- `Health`：当前生命值
- `MaxHealth`：最大生命值
- `HealthRate`：生命值比率（0.0 - 1.0，当前生命值 / 最大生命值）
- `Energy`：当前能量值
- `MaxEnergy`：最大能量值
- `EnergyRate`：能量比率（当前能量值 / 最大能量值）
- `FoodLevel`：与 MC 饱食度对应的数值（能量比率 × 20，范围 0 - 20）
- `Water`：当前水分值
- `MaxWater`：最大水分值
- `WaterRate`：水分比率（0.0 - 1.0，当前水分 / 最大水分）
- `Weight`：当前总重量（包括搬运中的物品）
- `MaxWeight`：最大负重
- `WeightRate`：重量比率（当前总重量 / 最大负重，可能大于 1.0）
- `ActionProgress`：动作进度百分比（0.0 - 1.0，当前动作的进度，由 `IProgress.GetProgress().progress` 获取）
- `Time`：当前 24 小时时间（0.0 - 24.0，由 `TimeOfDayController.Instance.Time` 获取，不可用时为 -1.0）
- `Mod:ShoulderSurfing:CameraPitch`：ShoulderSurfing mod 的相机俯仰角值（需要安装 ShoulderSurfing mod 才能使用，未安装时为 0.0）

#### Int 类型参数

- `CurrentCharacterType`：当前角色类型
  - `0`：角色（Character）
  - `1`：宠物（Pet）
  - `2`：AI 角色（AICharacter）
  - `-1`：自定义类型（Extension，由第三方扩展注册的目标类型）
- `CustomCharacterTypeID`：自定义类型 ID（仅在 `CurrentCharacterType` 为 `-1` 时有效）
  - 使用 `targetTypeId` 字符串生成的哈希值，用于唯一标识自定义目标类型
  - 当 `CurrentCharacterType` 不为 `-1` 时，此参数值为 `0`
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
- `Ammo`：当前枪械弹药数（未持枪时为 `0`）
- `MaxAmmo`：当前枪械最大弹药容量（未持枪时为 `0`）
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
- `ActionType`：动作类型 ID（由 `CharacterActionDefinitions` 定义，无动作时为 `-1`）
  - `1`：Action_Fishing（钓鱼）
  - `2`：Action_FishingV2（钓鱼 V2）
  - `3`：CA_Attack（攻击）
  - `4`：CA_Carry（搬运）
  - `5`：CA_Dash（冲刺）
  - `6`：CA_Interact（交互）
  - `7`：CA_Reload（装弹）
  - `8`：CA_Skill（技能）
  - `9`：CA_UseItem（使用物品）
  - `10`：CA_ControlOtherCharacter（控制其他角色）
  - 当 `ActionRunning` 为 `true` 时，动作类型可以精确判断角色正在执行的动作类型
  - 动作类型定义库支持扩展，可通过 `CharacterActionDefinitions.RegisterActionType<T>(id)` 注册新的动作类型
- `ActionFishingRodTypeID`：钓鱼动作中使用的鱼竿 TypeID（仅在 `ActionType` 为 `1` 或 `2` 时有效，其他情况为 `0`）
- `ActionBaitTypeID`：钓鱼动作中使用的鱼饵 TypeID（仅在 `ActionType` 为 `1` 或 `2` 时有效，其他情况为 `0`）
- `ActionUseItemTypeID`：使用物品动作中使用的物品 TypeID（仅在 `ActionType` 为 `9` 时有效，其他情况为 `0`）
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
- `EmotionValue1`：表情参数值1（int 类型，初始值 0）
  - 可通过表情快捷键功能设置：按住修饰键1（默认逗号键 `,`）+ F1-F8 设置（值为 0-7）
  - 可在设置界面中配置修饰键1（`EmotionModifierKey1`）
- `EmotionValue2`：表情参数值2（int 类型，初始值 0）
  - 可通过表情快捷键功能设置：按住修饰键2（默认句号键 `.`）+ F1-F8 设置（值为 0-7）
  - 可在设置界面中配置修饰键2（`EmotionModifierKey2`）
- `RidingVehicleType`：角色正在骑乘的载具类型（int 类型，初始值 0）

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

模组提供了以下状态机行为组件，可以在动画状态进入时触发音效、对话或控制参数：

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

模型音效由共用音频系统处理，支持按键、游戏事件、脚步、待机和 Animator 状态机触发。Prefab／VRM 可在通用模型配置中填写 `CustomSounds`；模型包内 YSM 也可使用通用字段，独立 YSM 侧车不接受 `CustomSounds`。音频开关、音量和待机设置统一按目标类型管理。

### 音效配置

在通用模型配置的 `CustomSounds` 中配置音效（模型包的 `Models` 项或独立 VRM 侧车）：

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
  - `"footstep_horse_walk_light"`、`"footstep_horse_walk_heavy"`、`"footstep_horse_run_light"`、`"footstep_horse_run_heavy"`：马匹脚步声（轻/重步行、轻/重跑步）
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
  - 哪些角色类型允许自动播放可通过 `TargetTypeEnableIdleAudio` 配置控制
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

在模型的通用 `ModelInfo` 配置中可以填写音效标签的播放概率（模型包的 `Models` 项或独立 VRM 侧车）：

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

Prefab 与 VRM 可通过共用的 Animator 状态机组件 `ModelDialogueTrigger` 触发对话气泡，配置方式如下。YSM 使用自己的动画／函数机制，不直接挂载 Animator 状态机组件。

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

#### 声明支持的目标类型

Prefab、VRM 和 YSM 都通过 `TargetTypes` 声明支持的目标。模型包在 `bundleinfo.json` 对应的 `Models` 项中填写；独立 YSM／VRM 在各自侧车中填写。独立 YSM／VRM 未指定时默认支持角色和全部 AI；模型包的模型项仍需明确声明 AI 目标。

1. 在 `TargetTypes` 数组中包含 AI 角色相关的目标类型 ID（推荐方式，v1.10.0+）
2. 或使用过时的 `Target` 和 `SupportedAICharacters` 字段（向后兼容）

**推荐方式（v1.10.0+）**：

```json
{
  "ModelID": "ai_model_id",
  "Name": "AI 模型",
  "TargetTypes": ["built-in:AICharacter_*", "built-in:AICharacter_Cname_Wolf", "built-in:AICharacter_Cname_Scav"]
}
```

- `"built-in:AICharacter_*"`：表示该模型适用于所有 AI 角色
- `"built-in:AICharacter_<角色名>"`：表示该模型适用于特定的 AI 角色（如 `"built-in:AICharacter_Cname_Wolf"`）
- 可以同时包含多个值，表示该模型适用于多个 AI 角色

**过时方式（向后兼容）**：

```json
{
  "ModelID": "ai_model_id",
  "Name": "AI 模型",
  "Target": ["AICharacter"],
  "SupportedAICharacters": ["Cname_Wolf", "Cname_Scav", "*"]
}
```

- 如果 `SupportedAICharacters` 包含 `"*"`，系统会自动转换为 `"built-in:AICharacter_*"`
- 如果 `SupportedAICharacters` 包含具体的 AI 角色名称键，系统会自动转换为 `"built-in:AICharacter_<角色名>"`
- 如果 `SupportedAICharacters` 为空数组，则该模型不会应用于任何 AI 角色

#### 在 UsingModel.json 中配置

在 `UsingModel.json` 中，可以为每个 AI 角色单独配置模型：

**推荐方式（v1.10.0+）**：

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

**过时方式（向后兼容）**：

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

1. 首先检查该 AI 角色是否有单独配置的模型（`built-in:AICharacter_<角色名>` 或过时的 `AICharacterModelIDs[<角色名>]`）
2. 如果没有，则检查 `"built-in:AICharacter_*"` 或过时的 `AICharacterModelIDs["*"]` 对应的默认模型
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

- 三种模型格式都可用于 AI；装备挂点和动画按各格式的配置方式处理
- 模型包通过 `Models` 项的 `TargetTypes` 声明 AI 支持；独立 YSM／VRM 通过侧车覆盖默认目标
  - **推荐方式（v1.10.0+）**：在 `TargetTypes` 中包含 `"built-in:AICharacter_*"` 或 `"built-in:AICharacter_<角色名>"`
  - **过时方式（向后兼容）**：在 `Target` 中包含 `"AICharacter"`，并在 `SupportedAICharacters` 中声明支持的 AI 角色
- 如果模型没有正确配置，AI 角色将使用原始模型
