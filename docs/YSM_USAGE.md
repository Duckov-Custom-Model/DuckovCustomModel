# YSM 模型与动作轮盘

将 `.ysm` 文件或包含 `ysm.json` 的模型文件夹放入本模组配置目录的 `Models`，在原有模型列表刷新并选用。通常目录为游戏安装目录下 `ModConfigs/DuckovCustomModel/Models`；安装目录不可写时沿用模组原有的持久化目录。无需把模型转换成 AssetBundle，原有 AssetBundle 模型仍使用原加载流程。

选用 YSM 模型后，默认按 **Z** 打开或关闭轮盘，可在设置页改键。每页八格，点击动作执行，滚轮上下翻页，分类项进入子页，返回项返回上层，Esc 关闭。内环配置入口显示模型原有的复选框、范围和单选项。中心按钮切换移动锁定，停止按钮请求停止当前附加动作。关闭轮盘本身不会停止动作；动作的循环、自然结束和打断由运行库控制。同模型重开保留分类和页数，切换模型重置。

需要调整模型的游戏绑定时，可添加侧车配置，文件名统一为 `<模型名称>.ysm.duckov.json`：`fox.ysm` 对应同目录的 `fox.ysm.duckov.json`；`fox/` 模型文件夹对应其中的 `fox.ysm.duckov.json`。例如：

```json
{
  "Name": "自定义模型",
  "ModelTarget": "player/main",
  "TargetTypes": ["built-in:Character", "built-in:AICharacter_*"],
  "Scale": 1.0,
  "HeadPitchLimit": 45,
  "HeadYawLimit": 85,
  "BundlePath": "props.assetbundle",
  "DeathLootBoxPrefabPath": "Assets/Props/Corpse.prefab",
  "LocatorMappings": {
    "RightHandLocator": "模型内实际定位器名称"
  }
}
```

`TargetTypes` 可指定角色、宠物、全部或特定 AI（如 `built-in:Character`、`built-in:Pet`、`built-in:AICharacter_*`、`built-in:AICharacter_Cname_Wolf`），也可使用已注册的扩展目标类型。不填写时，独立 YSM 默认适用于角色和全部 AI。`BundlePath` 相对该 YSM 文件所在目录或模型文件夹；`DeathLootBoxPrefabPath` 指向该包内 Prefab。尸体也可改用同目录另一个 `.ysm` 文件，例如 `"DeathLootBoxYsmPath": "corpse.ysm"`，并可设置 `"DeathLootBoxAnimation": "idle"`；同时配置时优先使用 Prefab。目标 YSM 先从同一 AssetBundle 中查找，再查找磁盘同目录相对路径，且不能指向 `Models` 之外。

定位器映射必须使用模型内的真实名称；缺失时保留原角色挂点。`LocatorOffsets` 可为各挂点配置 `Position` 与 `Rotation` 三元素数组，分别为 Unity 局部单位和角度。物品分类、射击／换弹／冲刺动画可在绑定配置中显式指定，不能假定所有模型都有这些动画。

模型的 Molang 状态从当前角色读取。默认视为站立、满生命与满饱食度；`ysm.food_level` 使用角色能量比例映射到 0–20。`query.time_stamp` 和 `query.time_of_day` 使用游戏时钟，`query.day_number` 与 `query.moon_phase` 使用游戏日期；没有世界时钟的界面仍使用模型预览状态。物品使用、受伤计时、装备数量、移动输入分别可通过 `query.is_using_item`、`query.hurt_time`、`query.equipment_count`、`ysm.xxa`／`ysm.yya`／`ysm.zza` 读取。按状态命名的控制器只在相应状态下运行，例如 `player.elytra_fly` 只在滑翔时运行，`player.pre_sleep` 只在睡眠时运行。

`bundleinfo.json` 的 `Models` 项也可使用 `"SourceKind": "Ysm"` 和 `"SourcePath": "Assets/Models/fox.ysm"` 选择包内 YSM；包内资源需作为 Unity `TextAsset`（可用 `.bytes` 导入），优先读取同包资源，找不到时读取 AssetBundle 文件所在目录的相对路径。Prefab 模型可用 `DeathLootBoxYsmPath` 指向同包或同目录的独立 YSM；YSM 模型可用 `DeathLootBoxPrefabPath` 指向同包 Prefab。

安装时保留 `Resources` 子目录结构。模型资产不包含在模组安装包中。
