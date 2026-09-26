# YSM 模型与动作轮盘

将 `.ysm` 文件或包含 `ysm.json` 的模型文件夹放入本模组配置目录的 `Models`，在原有模型列表刷新并选用。通常目录为游戏安装目录下 `ModConfigs/DuckovCustomModel/Models`；安装目录不可写时沿用模组原有的持久化目录。无需把模型转换成 AssetBundle，原有 AssetBundle 模型仍使用原加载流程。

选用 YSM 模型后，默认按 **Z** 打开或关闭轮盘，可在设置页改键。每页八格，点击动作执行，滚轮上下翻页，分类项进入子页，返回项返回上层，Esc 关闭。内环配置入口显示模型原有的复选框、范围和单选项。中心按钮切换移动锁定，停止按钮请求停止当前附加动作。关闭轮盘本身不会停止动作；动作的循环、自然结束和打断由运行库控制。同模型重开保留分类和页数，切换模型重置。

需要调整模型的游戏绑定时，可添加侧车配置：单文件模型使用 `名称.ysm.duckov.json`，文件夹模型使用其中的 `duckov.ysm.json`。例如：

```json
{
  "Name": "自定义模型",
  "ModelTarget": "player/main",
  "Scale": 1.0,
  "HeadPitchLimit": 45,
  "HeadYawLimit": 85,
  "LocatorMappings": {
    "RightHandLocator": "模型内实际定位器名称"
  }
}
```

定位器映射必须使用模型内的真实名称；缺失时保留原角色挂点。`LocatorOffsets` 可为各挂点配置 `Position` 与 `Rotation` 三元素数组，分别为 Unity 局部单位和角度。物品分类、射击／换弹／冲刺动画可在绑定配置中显式指定，不能假定所有模型都有这些动画。

安装时保留 `Resources` 子目录结构。模型资产不包含在模组安装包中。
