# 更新日志

[English](CHANGELOG_EN.md) | 中文

## v1.9.4

- 重构动画参数更新系统，优化性能和代码结构
  - 将 `IAnimatorParameterUpdater` 接口和 `AnimatorParameterUpdaterManager` 管理器从 Core 移到非 Core 项目，避免不必要的依赖
  - 移除 `AnimatorUpdateContext` 上下文类，直接使用 `CustomAnimatorControl` 的属性访问，避免每帧创建对象
  - 接口改为直接接受 `CustomAnimatorControl` 类型，避免装箱拆箱操作
  - 在 `CustomAnimatorControl` 中添加公共属性访问器，暴露更新所需的数据
- 优化 ShoulderSurfing mod 扩展支持
  - `Mod:ShoulderSurfing:CameraPitch` 参数仅对主角色有效，其他角色类型不会更新该参数
  - 添加第三人称模式检查，当 `shoulderCameraToggled` 为 false 时直接返回 0，避免不必要的反射调用
  - 优化性能，使用帧缓存机制，同一帧内多次调用只执行一次反射操作

## v1.9.3

- 重构 AI 角色列表管理，支持动态补充和替换
  - 修改 Core 侧的 `AICharacters` 类，将静态只读集合改为可变的 `HashSet`，支持动态添加角色
  - 新增 `AddAICharacter` 和 `AddAICharacters` 方法，支持动态添加单个或批量角色
  - 新增 `Contains` 方法，用于检查角色是否在支持列表中
  - 在非 Core 侧创建 `AICharactersManager` 管理器，从游戏的预设系统动态收集所有 AI 角色
  - 从 `GameplayDataSettings.CharacterRandomPresetData.presets` 获取所有角色预设，自动补充到支持列表中
- 新增 AI 角色模型替换警告提示
  - 在目标设置面板中，当选择以 `Character_` 开头的 AI 角色或"所有 AI 角色"选项时，会显示警告信息
- 新增多语言翻译支持
  - 新增语言：韩语、法语、德语、西班牙语、俄语、意大利语、葡萄牙语、波兰语、土耳其语、泰语、越南语

## v1.9.2

- 优化 UI 滚动条实现
  - 统一使用 UIFactory 工具类方法创建和配置滚动条，提升代码一致性
  - 为 TargetListScrollView、ModelListScrollView、TargetSettingsScrollView 添加右侧滚动条
  - 为 SettingsScrollView 添加左侧滚动条
  - 为更新日志滚动视图添加滚动条
- 修复 UI 布局问题
  - 修复 TargetListPanel 中按钮宽度问题，通过调整内容区域 padding 为滚动条留出空间
  - 优化滚动条父对象设置逻辑，确保滚动条正确显示在滚动视图内部
  - 统一所有 RectTransform 操作使用 UIFactory 工具类方法，避免直接操作

## v1.9.1

- 优化更新检查界面功能
  - 新增更新日志显示功能，支持在设置界面直接查看更新日志内容
  - 新增 Markdown 转 Unity 富文本支持，支持标题、粗体、斜体、列表、链接等格式
  - 新增下载链接显示功能，支持在更新日志下方显示多个下载链接按钮
  - 支持点击下载链接按钮直接打开浏览器下载
  - 新增发布时间显示，显示最新版本的发布时间（自动转换为本地时区）
  - 优化更新信息布局，将版本号和版本名称显示在同一行
  - 将"上次检查"时间显示在检查更新按钮文本中，格式为"检查更新 (上次检查: 时间)"
  - 优化滚动视图高度调整逻辑，内容不足最大高度时自动适应内容高度
  - 修复了多个 UI 布局问题，包括按钮尺寸、容器高度、文本间距等
- 改进时区处理
  - 将发布时间从 `DateTime` 改为 `DateTimeOffset`，确保时区信息正确保存和显示
  - 发布时间显示时自动转换为本地时区
- 新增多语言支持
  - 新增"下载链接"（Download Links）多语言文本
  - 新增"发布时间"（Published At）多语言文本
  - 支持中文、繁体中文、日文、英文

## v1.9.0

- 重构动画参数更新系统，使用注册机制替代直接调用
  - 创建 `IAnimatorParameterUpdater` 接口和 `AnimatorParameterUpdaterManager` 管理器，统一管理动画参数更新器
  - 将所有更新器拆分为独立类，从 `CustomAnimatorControl` 中分离，提高代码可维护性
  - 创建 `AnimatorUpdateContext` 上下文类，统一管理更新所需的数据
- 新增 ShoulderSurfing mod 扩展支持
  - 添加 `Mod:ShoulderSurfing:CameraPitch` 动画参数，提供 ShoulderSurfing mod 的相机俯仰角值

## v1.8.13

- 新增 Buff 驱动的动画器参数配置功能
  - 在 `bundleinfo.json` 的 `ModelInfo` 中添加 `BuffAnimatorParams` 字段，支持配置基于 Buff 状态的动画器参数
  - 支持通过 Buff ID 或 DisplayNameKey 匹配 Buff，满足任意条件即可触发参数
  - 配置的 Buff 参数会在调试界面中显示，位于自定义参数和动画器参数之后
  - 优化了动画器参数设置性能，缓存有效参数列表，避免对不存在参数的无效调用

## v1.8.12

- 新增模型特性 `SkipShowBackMaterial`，支持在模型定义中跳过 ShowBack 材质的附加逻辑
  - 在 `ModelFeatures` 中添加 `SkipShowBackMaterial` 特性常量
  - 在模型的 `bundleinfo.json` 的 `Features` 数组中添加 `"SkipShowBackMaterial"` 即可跳过 ShowBack 材质的附加逻辑

## v1.8.11

- 修复了可搬运物品放下时的清理逻辑
  - 将 `Carriable.Drop` 的 Harmony Patch 从 Postfix 改为 Prefix，确保在放下前正确清理
  - 在放下物品时自动移除 `CustomSocketMarker` 和 `DontHideAsEquipment` 组件
  - 为 `UnregisterCustomSocketObject` 方法添加 `restore` 参数，允许在放下时不恢复物品位置

## v1.8.10

- 新增模型切换事件订阅功能
  - 在 `ModelListManager` 中添加 `OnModelChanged` 静态事件，用于订阅模型切换通知
  - 新增 `ModelChangedEventArgs` 事件参数类，包含目标类型、模型ID、模型名称、是否恢复、是否成功、处理器数量等信息
  - 在模型切换、恢复等操作时自动触发事件，包括成功和失败的情况
  - 支持为角色、宠物和AI角色分别监听模型切换事件

## v1.8.9

- 新增 `ModelSoundStopTrigger` 组件，支持在动画状态机中停止音效播放
  - 支持停止指定事件名称的音效
  - 支持停止所有正在播放的音效
  - 支持使用内置事件名称（如 `idle`）或自定义触发器事件名称
  - 支持在状态进入或退出时触发停止操作
  - 提供 Unity 编辑器自定义界面，包含条件显示、警告提示和帮助信息

## v1.8.8-fix1

- 修正了 ModelParameterDriver 的参数记录逻辑，以解决编辑器数据锁定的问题，并提升了相关数据的简洁性

## v1.8.8

- 新增版本更新检测功能，自动检测是否有新版本可用
- 新增手动检测更新机制，可在设置界面手动触发更新检测
- 新增新版本提示功能，当检测到新版本时会在版本显示处显示提示信息

## v1.8.7

- 新增自定义对话系统，支持在动画状态机中触发对话
  - 新增 `ModelDialogueTrigger` 组件，可在动画状态进入时触发对话
  - 新增 `CustomDialogueManager` 管理器，统一管理对话的加载和播放
  - 支持多语言对话文件，自动根据当前语言加载对应文件
  - 支持多种对话播放模式：顺序播放、随机播放、随机不重复播放、连续播放
- 新增 `ModelSoundTrigger` 组件，支持在动画状态机中直接触发音效
  - 可在动画状态进入时根据配置的音效标签播放音效
  - 支持随机或顺序选择多个音效标签
  - 支持配置音效播放模式（Normal、StopPrevious、SkipIfPlaying、UseTempObject）
- 增强音效触发功能，新增多个战斗相关音效标签
  - 新增 `trigger_on_hit_target`：命中目标时触发
  - 新增 `trigger_on_kill_target`：击杀目标时触发
  - 新增 `trigger_on_crit_hurt`：受到暴击伤害时触发
  - 新增 `trigger_on_crit_dead`：暴击死亡时触发
  - 新增 `trigger_on_crit_hit_target`：暴击命中目标时触发
  - 新增 `trigger_on_crit_kill_target`：暴击击杀目标时触发
- 增强动画器控制功能，新增多个战斗相关触发器
  - 新增 `Hurt`、`Dead`、`HitTarget`、`KillTarget` 触发器
  - 新增 `CritHurt`、`CritDead`、`CritHitTarget`、`CritKillTarget` 触发器
  - 在 `CustomAnimatorControl` 中新增对应的触发方法
- 增强 `ModelHandler` 音效触发功能
  - 支持根据伤害信息判断是否为暴击，自动触发对应的音效和动画器触发器
  - 支持监听全局伤害和死亡事件，在命中或击杀目标时触发音效和动画器触发器
  - 新增音效播放概率配置（`SoundTagPlayChance`），支持为不同音效标签配置播放概率
- 优化音效标签处理，移除标签验证限制，允许使用任意自定义标签

## v1.8.6-fix1

- 修复 AnimatorParameterDriverManager 中的随机整数生成逻辑
- 修正 AnimatorParameterDriverManager 中随机整数生成的范围，确保包含最大值
- 修正复制参数操作的范围转换逻辑
- 更新 ModelParameterDriver 和 BlueprintID 组件的文档注释，增强可读性和理解性

## v1.8.6

- 新增 `ModelParameterDriver` 组件，支持在动画状态机中自定义参数控制
  - 支持多种参数操作类型：
    - `Set`：直接设置参数值
    - `Add`：在现有值基础上增加指定值
    - `Random`：随机设置参数值（支持范围随机和概率触发）
    - `Copy`：从源参数复制值到目标参数（支持范围转换）
  - 支持所有 Animator 参数类型（Float、Int、Bool、Trigger）
  - 在动画状态进入时自动应用参数驱动
  - 支持参数验证，确保目标参数和源参数存在后才应用驱动
- 新增 `AnimatorParameterDriverManager` 管理器，统一管理参数驱动的初始化和应用逻辑
- 增强动画参数显示器功能：
  - 添加参数缓存机制，优化参数获取性能，减少重复计算
  - 支持显示 Animator 控制器中定义的外部参数，默认排列在列表末尾
- 新增 `BlueprintID` 组件，用于为游戏对象分配唯一标识符（当前暂无实际功能）
- 更新依赖版本：`DuckovGameLibs` 从 1.1.6-Steam 更新到 1.2.5-Steam

## v1.8.5

- 改进音效播放系统，统一使用 `ModelHandler.PlaySound` 方法管理所有音效播放
- 新增音效播放模式支持（Normal、StopPrevious、SkipIfPlaying、UseTempObject），提供更精细的音效控制
- 新增音效实例管理功能，支持停止指定音效或所有音效
- 改进音效打断机制：
  - 玩家按键触发（F1 嘎嘎）和 AI 自动触发（`normal`、`surprise` 标签）共享同一打断组，新播放的音效会打断同组内正在播放的音效
  - 脚步声拥有独立的打断组，新播放的脚步声会打断同组内正在播放的脚步声
  - 受伤音效（`trigger_on_hurt`）如果已有音效正在播放则跳过，避免重复播放
  - 死亡音效（`trigger_on_death`）会先停止所有正在播放的音效，然后播放死亡音效
- 改进 `AudioUtils.PlayAudioWithTempObject` 方法，返回 `EventInstance` 以便更好地管理音效生命周期
- 移除不再使用的 `SoundTags.Death` 常量，统一使用 `trigger_on_death` 标签

## v1.8.4

- 新增脚步声音效标签支持，支持为不同材质（有机、机械、危险、无声）和不同状态（轻/重步行、轻/重跑步）配置自定义脚步声
- 新增 `footstep_organic_walk_light`、`footstep_organic_walk_heavy`、`footstep_organic_run_light`、`footstep_organic_run_heavy` 等16个脚步声音效标签
- 新增 `AudioUtils` 工具类，用于播放音频并自动清理临时对象
- 改进死亡音效播放逻辑，使用临时对象确保音效正确播放和清理

## v1.8.3

- 修复了音效播放逻辑，确保音效播放时包含正确的游戏对象引用

## v1.8.2

- 优化了 UI 组件的布局和初始化逻辑，使用 UIFactory 方法简化代码，提升可读性
- 改进了事件订阅清理逻辑，防止内存泄漏

## v1.8.1

- 新增音效标签 `trigger_on_hurt`，用于角色受伤时自动播放音效
- 新增 `search_found_item_quality_xxx` 系列音效标签，可在搜索完成时根据物品品质触发不同音效（支持 none/white/green/blue/purple/orange/red/q7/q8）
- 改进了事件订阅的清理逻辑，防止内存泄漏

## v1.8.0-fix1

- 修复了因修改加载方式导致的多语言加载失败问题

## v1.8.0

- 拆分了部分逻辑到不同程序集，以便于之后的 SDK 开发
- 调整了加载逻辑，增加了额外的 Mod 加载包装器，以减少因为 Harmony 加载顺序导致的问题
- 现在在工作正常的情况下，应当不再需要考虑本 mod 是否在 Harmony 前被加载了

## v1.7.12

- 新增音效标签 `trigger_on_death`，用于角色死亡时自动播放音效
- 修复了在切换模型时血条的高度没有刷新的问题

## v1.7.11

- 新增模型脚步声频率配置功能，支持在 `bundleinfo.json` 中为每个模型单独配置走路和跑步时的脚步声触发频率
- 新增 `WalkSoundFrequency` 字段（可选），用于配置走路时每秒的脚步声触发频率（如果未指定，将自动使用原始角色的走路脚步声频率设置）
- 新增 `RunSoundFrequency` 字段（可选），用于配置跑步时每秒的脚步声触发频率（如果未指定，将自动使用原始角色的跑步脚步声频率设置）

## v1.7.10

- 动画器参数窗口新增目标切换功能，可以在窗口内切换查看角色自身或宠物的动画器参数
- 默认查看角色自身，可通过并排的两个按钮（角色/宠物）进行切换
- 切换目标时会自动清空参数状态，重新开始计算参数变化

## v1.7.9

- 修复了在只读环境（如 macOS）下 ModConfigs 目录无法创建导致功能失效的问题
- 当安装目录为只读时，自动切换到游戏存档的上一级目录的 ModConfigs（Windows: `AppData\LocalLow\TeamSoda\Duckov\ModConfigs`，macOS/Linux: 对应的用户数据目录）

## v1.7.8-fix2

- 改进了 Harmony Patch 的应用和移除机制，增加了备用方法以提高容错性，当主方法失败时会自动尝试备用方法

## v1.7.8

- 新增参数显示界面快捷键功能，可在设置界面中配置用于开关参数显示界面的快捷键
- 默认值为没有按键，需要用户主动在设置界面中设置
- 修复了自定义模型的显示判定逻辑，现在模型会严格遵循游戏内原生的视野范围逻辑切换显示

## v1.7.7

- 新增"所有 AI 角色"的目标设置功能，用于统一设置所有 AI 角色的默认配置项
- 优化了模型查找逻辑：优先使用单独设置，如果没有则查找`*`的设置，最后才使用原版模型
- 优化了使用自定义模型时的部分碰撞判定计算逻辑
- 修正了隐藏角色装备的情况下无法触发爆头判定的问题
- 修正了部分槽位在切换模型时会变得不可见的问题
- 修正了AI角色配置的回退逻辑，现在音频、装备隐藏等设置会根据实际使用的模型来选择配置：如果使用的是回退模型（`*`），则使用`*`的设置；如果使用的是自己的模型配置，则使用自己的设置

## v1.7.6

- 修正了获取角色单位原本模型的逻辑，现在应该能正确匹配各个类型角色的原始模型并进行处理了
- 增加了角色模型的遮挡显示处理，现在角色模型应当能在被遮挡的情况下正确显示轮廓了
- 修复了 bundleinfo.json 解析错误导致后续逻辑丢失的问题
- 现在即使某个 bundle 的 JSON 文件格式错误，也会记录错误日志并跳过该 bundle，继续处理其他正常的 bundle
- 增强了模型加载过程中的错误处理，确保单个 bundle 的错误不会影响整体功能
- 修复了模型列表界面中错误信息（如加载错误、prefab 缺失等）没有正确显示的问题
- 现在错误信息会正确显示在模型列表项中，而不仅仅是标红处理

## v1.7.5

- 新增模型音频开关功能，可在目标设置区配置是否使用模型提供的音频
- 禁用后，对应角色的所有模型音频（包括按键触发、AI 自动触发和待机音频）都不会播放
- 支持为角色、宠物和 AI 角色分别配置

## v1.7.4

- 优化了界面的结构和逻辑，以保证各个语言下的界面显示符合要求
- 增加了打开模型文件夹的按钮

## v1.7.3

- 新增 `Weather` Animator 参数（int 类型），用于获取当前天气状态（晴天、多云、雨天、风暴 I、风暴 II）
- 新增 `Time` Animator 参数（float 类型），用于获取当前 24 小时时间
- 新增 `TimePhase` Animator 参数（int 类型），用于获取当前时间阶段（白天、黎明、夜晚）
- 当 `TimeOfDayController.Instance` 不可用时，这三个参数会被设置为 -1

## v1.7.2

- 为 AnimatorParamInfo 添加了 InitialValue 属性，用于存储参数的初始值
- 修复了 ShootMode 参数的初始值（从 0 改为 -1）
- 增强了动画参数窗口的显示功能，添加了参数值变化检测和颜色高亮显示
  - 参数值改变时会显示为黄色
  - 参数值正在改变时会显示为橙色
  - 未改变的参数显示为白色
- 调整了动画参数窗口的高度（从 800 增加到 1000）和字体大小（从 13 增加到 16）

## v1.7.1

- 修改了动画器参数更新逻辑，即使没有使用自定义模型时也能看到动画参数的更新（只是不生效于animator）
- 将 CustomAnimatorControl 改为常驻实例，挂在和 ModelHandler 同一个对象上，只改目标 Animator，避免频繁创建和销毁组件
- 修复了动画参数窗口在未使用自定义模型时不显示的问题

## v1.7.0

- 在屏幕左上角添加了固定的设置按钮，点击可以打开/关闭设置界面
- 设置按钮会在主菜单或背包界面时自动显示

## v1.6.8

- 现在会自动修改模型内包含 Renderer 的对象的 layer 为 "Character"

## v1.6.7

- 修复了手持装备相关参数（如 `LeftHandEquip`、`RightHandEquip` 等）在某些情况下不会正确更新的问题
- 修复了部分 Animator 参数（如 `GunState`、`WeaponInLocator`）不会显示在参数窗口中的问题，现在所有定义的参数都会正确显示

## v1.6.6

- 新增 `GunState` Animator 参数，用于指示枪械的当前状态（射击冷却、就绪、开火、连发每发冷却、空弹、装弹中）

## v1.6.5

- 修复了 纸箱定位点 和 搬运定位点 的工作逻辑

## v1.6.4

- 新增 `WeaponInLocator` Animator 参数，用于指示武器当前所在的定位点类型（左手、右手或近战武器）

## v1.6.3

- 模型增加了 `DeathLootBoxPrefabPath` 字段，用于指定死亡战利品箱的样式（可选，默认为原版模型）
- 现在允许自定义死亡战利品箱的模型了

## v1.6.2

- 取消了对玩家角色的 idle 音频功能限制，现在玩家也可以启用自动音频了
- 增加了对每个目标角色的 idle 音频功能设置，现在可以对每个目标单独设置是否允许自动音频和自动音频间隔了
- 重构了模型管理器的 UI 界面，提升用户舒适度和界面美观度与结构化

## v1.6.1

- 新增待机音频自动播放功能，支持为非玩家角色（AI 角色和宠物）配置待机音频自动播放间隔
- 新增 idle 音效标签支持，模型音效可配置 "idle" 标签用于待机音频播放

## v1.6.0

- 优化了部分逻辑处理
- 新增纸箱定位点支持，自定义模型可包含 `PaperBoxLocator` 定位点，纸箱会自动附加到该定位点并跟随模型
- 现在隐藏装备时不再会导致纸箱被隐藏了
- 新增可搬运物品定位点支持，自定义模型可包含 `CarriableLocator` 定位点，搬运物品时会自动附加并保存原始位置信息

## v1.5.1-fix2

- 修正了重新加载模型时音频设置没有正确应用的问题

## v1.5.1-fix1

- 修正了选择模型时界面没有刷新的问题

## v1.5.1

- 新增 `AICharacter` 目标，用于非玩家和宠物的 AI 角色（暂不支持NPC，因为 NPC 实际是建筑的一部分，并非常规模型结构）
- 新增一键重置无效模型设置的按钮，用于将使用了不兼容、不存在的模型的角色快速重置回默认模型
- 新增音频播放相关接口，支持替换玩家嘎嘎和 AI 的自动嘎嘎

## v1.4.1

- 新增大量 Animator 参数支持，增强动画控制能力
- 新增枪械相关参数：`Loaded`（装弹状态）、`Shoot`（射击触发）、`ShootMode`（射击模式）、`AmmoRate`（弹药比率）
- 新增瞄准相关参数：`InAds`（瞄准状态）、`AdsValue`（瞄准进度）、`AimType`（瞄准类型）、`AimDirX/Y/Z`（瞄准方向）
- 新增动作相关参数：`ActionRunning`（动作执行状态）、`ActionProgress`（动作进度）、`ActionPriority`（动作优先级）
- 新增状态相关参数：`Hidden`（隐藏状态）、`ThermalOn`（热成像状态）、`VelocityX/Y/Z`（速度分量）
- 优化了部分代码逻辑和性能
- 增加了 Animator 参数窗口，可以实时查看支持的 Animator 参数数值

## v1.2.0

- 重构配置系统：
  - `UsingModel.json` 改为字典格式
  - 增加 `HideEquipmentConfig.json` 并将相关配置从 `UIConfig.json` 移除
  - 支持自动迁移旧配置
- 性能优化：实现增量更新机制，使用运行时哈希缓存，只在 Bundle 文件或配置变更时重新加载
- 多对象支持：每个 ModelTarget 可对应多个游戏对象，切换模型时统一应用到所有对象
- 刷新流程优化：无变更时跳过 Bundle 加载和状态检查，避免无意义的重复加载
- 临时模型恢复：Bundle 更新时自动临时切换回原版模型，更新完成后自动恢复
- 用户选择保护：刷新过程中用户更改模型时，不会覆盖用户的选择
- UI 改进：模型信息显示 Bundle 名称
- 修复刷新时模型重复加载的问题

## v1.1.2

- 优化了部分逻辑
- 增加了 `CurrentCharacterType` 参数，用于判断当前生效于玩家角色（0）还是宠物（1）

