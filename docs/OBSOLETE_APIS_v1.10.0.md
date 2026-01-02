# 已过时 API 列表

本文档列出了所有已过时的 API 及其替代方案。这些 API 仍然可用以保持向后兼容，但建议尽快迁移到新的 API。

## 核心类型

### ModelTarget 枚举
- **状态**: ⚠️ 已过时
- **替代方案**: 使用 `ModelTargetType` 字符串标识符
- **说明**: `ModelTarget` 枚举已被字符串标识符系统替代，以支持第三方扩展

### ModelTargetExtensions 类
- **状态**: ⚠️ 已过时
- **替代方案**: 直接使用 `ModelTargetType` 字符串标识符和相关方法
- **方法**:
  - `ToTargetTypeId(ModelTarget target, string? aiCharacterName = null)` → 直接使用 `ModelTargetType` 常量（如 `ModelTargetType.Character`）或 `ModelTargetType.CreateAICharacterTargetType(nameKey)` 创建 AI 角色目标类型
  - `FromTargetTypeId(string targetTypeId)` → 不再需要转换，直接使用字符串标识符

## 配置类

### UsingModel

#### 属性
- `ModelIDs` (Dictionary<ModelTarget, string>) → `TargetTypeModelIDs` (Dictionary<string, string>)
- `AICharacterModelIDs` (Dictionary<string, string>) → `TargetTypeModelIDs` (Dictionary<string, string>)

#### 方法
- `GetModelID(ModelTarget target)` → `GetModelID(string targetTypeId)`
- `SetModelID(ModelTarget target, string modelID)` → `SetModelID(string targetTypeId, string modelID)`
- `GetAICharacterModelID(string nameKey)` → `GetModelID(ModelTargetType.CreateAICharacterTargetType(nameKey))`
- `GetAICharacterModelIDWithFallback(string nameKey)` → 使用 `GetModelID(ModelTargetType.CreateAICharacterTargetType(nameKey))` 获取特定 AI 角色的模型 ID，如果为空则使用 `GetModelID(ModelTargetType.AllAICharacters)` 作为回退
- `SetAICharacterModelID(string nameKey, string modelID)` → `SetModelID(ModelTargetType.CreateAICharacterTargetType(nameKey), string modelID)`

### HideEquipmentConfig

#### 属性
- `HideEquipment` (Dictionary<ModelTarget, bool>) → `TargetTypeHideEquipment` (Dictionary<string, bool>)
- `HideAICharacterEquipment` (Dictionary<string, bool>) → `TargetTypeHideEquipment` (Dictionary<string, bool>)

#### 方法
- `GetHideEquipment(ModelTarget target)` → `GetHideEquipment(string targetTypeId)`
- `SetHideEquipment(ModelTarget target, bool value)` → `SetHideEquipment(string targetTypeId, bool value)`
- `GetHideAICharacterEquipment(string nameKey)` → 如果 `nameKey` 为空，使用 `GetHideEquipment(ModelTargetType.AllAICharacters)`；否则使用 `GetHideEquipment(ModelTargetType.CreateAICharacterTargetType(nameKey))`，如果未找到则回退到 `GetHideEquipment(ModelTargetType.AllAICharacters)`
- `SetHideAICharacterEquipment(string nameKey, bool value)` → `SetHideEquipment(ModelTargetType.CreateAICharacterTargetType(nameKey), bool value)`

### IdleAudioConfig

#### 属性
- `IdleAudioIntervals` (Dictionary<ModelTarget, IdleAudioInterval>) → `TargetTypeIdleAudioIntervals` (Dictionary<string, IdleAudioInterval>)
- `AICharacterIdleAudioIntervals` (Dictionary<string, IdleAudioInterval>) → `TargetTypeIdleAudioIntervals` (Dictionary<string, IdleAudioInterval>)
- `EnableIdleAudio` (Dictionary<ModelTarget, bool>) → `TargetTypeEnableIdleAudio` (Dictionary<string, bool>)
- `AICharacterEnableIdleAudio` (Dictionary<string, bool>) → `TargetTypeEnableIdleAudio` (Dictionary<string, bool>)

#### 方法
- `GetIdleAudioInterval(ModelTarget target)` → `GetIdleAudioInterval(string targetTypeId)`
- `SetIdleAudioInterval(ModelTarget target, float min, float max)` → `SetIdleAudioInterval(string targetTypeId, float min, float max)`
- `IsIdleAudioEnabled(ModelTarget target)` → `IsIdleAudioEnabled(string targetTypeId)`
- `SetIdleAudioEnabled(ModelTarget target, bool enabled)` → `SetIdleAudioEnabled(string targetTypeId, bool enabled)`
- `GetAICharacterIdleAudioInterval(string nameKey)` → 如果 `nameKey` 为空，使用 `GetIdleAudioInterval(ModelTargetType.AllAICharacters)`；否则使用 `GetIdleAudioInterval(ModelTargetType.CreateAICharacterTargetType(nameKey))`，如果返回默认值（30-45秒）则回退到 `GetIdleAudioInterval(ModelTargetType.AllAICharacters)`
- `SetAICharacterIdleAudioInterval(string nameKey, float min, float max)` → `SetIdleAudioInterval(ModelTargetType.CreateAICharacterTargetType(nameKey), float min, float max)`
- `IsAICharacterIdleAudioEnabled(string nameKey)` → 如果 `nameKey` 为空，使用 `IsIdleAudioEnabled(ModelTargetType.AllAICharacters)`；否则使用 `IsIdleAudioEnabled(ModelTargetType.CreateAICharacterTargetType(nameKey))`，如果未找到则回退到 `IsIdleAudioEnabled(ModelTargetType.AllAICharacters)`
- `SetAICharacterIdleAudioEnabled(string nameKey, bool enabled)` → `SetIdleAudioEnabled(ModelTargetType.CreateAICharacterTargetType(nameKey), bool enabled)`

### ModelAudioConfig

#### 属性
- `EnableModelAudio` (Dictionary<ModelTarget, bool>) → `TargetTypeEnableModelAudio` (Dictionary<string, bool>)
- `AICharacterEnableModelAudio` (Dictionary<string, bool>) → `TargetTypeEnableModelAudio` (Dictionary<string, bool>)

#### 方法
- `IsModelAudioEnabled(ModelTarget target)` → `IsModelAudioEnabled(string targetTypeId)`
- `SetModelAudioEnabled(ModelTarget target, bool enabled)` → `SetModelAudioEnabled(string targetTypeId, bool enabled)`
- `IsAICharacterModelAudioEnabled(string nameKey)` → 如果 `nameKey` 为空，使用 `IsModelAudioEnabled(ModelTargetType.AllAICharacters)`；否则使用 `IsModelAudioEnabled(ModelTargetType.CreateAICharacterTargetType(nameKey))`，如果未找到则回退到 `IsModelAudioEnabled(ModelTargetType.AllAICharacters)`
- `SetAICharacterModelAudioEnabled(string nameKey, bool enabled)` → `SetModelAudioEnabled(ModelTargetType.CreateAICharacterTargetType(nameKey), bool enabled)`

## 数据类

### ModelInfo

#### 属性
- `Target` (ModelTarget[]) → `TargetTypes` (string[])
- `SupportedAICharacters` (string[]) → `TargetTypes` (string[])

#### 方法
- `CompatibleWithType(ModelTarget modelTarget)` → `CompatibleWithTargetType(string targetTypeId)`

**注意**: `Target` 和 `SupportedAICharacters` 属性在 `Validate()` 方法中会自动迁移到 `TargetTypes`。

**迁移规则**:
- `Target` 中的 `Character` 和 `Pet` 会转换为对应的目标类型 ID（`built-in:Character`、`built-in:Pet`）
- `Target` 中的 `AICharacter` **不会被转换**，它只是一个标记，表示需要处理 `SupportedAICharacters`
- 只有当 `Target` 包含 `AICharacter` 标记时，`SupportedAICharacters` 中的值才会被转换为目标类型 ID
- 如果 `Target` 中没有 `AICharacter` 标记，即使 `SupportedAICharacters` 有值，也不会添加任何 AI 角色目标类型
- `SupportedAICharacters` 中的 `"*"` 会转换为 `built-in:AICharacter_*`，其他值会转换为 `built-in:AICharacter_<角色名>`

### ModelChangedEventArgs

#### 新增属性（非过时）
- `Handler` (Object?) - 触发事件的 `ModelHandler` 实例
- `TargetTypeId` (string) - 目标类型 ID
- `ModelID` (string?) - 模型 ID
- `ModelName` (string?) - 模型名称
- `IsRestored` (bool) - 是否恢复原始模型

#### 过时属性
- `Target` (ModelTarget) → `TargetTypeId` (string)
- `AICharacterNameKey` (string?) → 使用 `ModelTargetType.ExtractAICharacterName(TargetTypeId)` 提取
- `HandlerCount` (int) → 已过时，不再使用（事件驱动机制中不再需要）
- `Success` (bool) → 已过时，不再使用

### TargetInfo

#### 属性
- `TargetType` (ModelTarget) → `TargetTypeId` (string)
- `AICharacterNameKey` (string) → 使用 `GetAICharacterNameKey()` 方法获取

#### 方法
- 使用 `GetTargetTypeId()` 方法获取当前目标类型 ID
- 使用 `GetAICharacterNameKey()` 方法获取 AI 角色名称

## 管理器类

### ModelManager

#### 方法
- `InitializeModelHandler(CharacterMainControl characterMainControl, ModelTarget target = ModelTarget.Character)` → `InitializeModelHandler(CharacterMainControl characterMainControl, string targetTypeId)`
- `GetAllModelHandlers(ModelTarget target)` → `GetAllModelHandlersByTargetType(string targetTypeId)`
- `GetAICharacterModelHandlers(string nameKey)` → `GetAllModelHandlersByTargetType(ModelTargetType.CreateAICharacterTargetType(nameKey))`

### ModelListManager

#### 方法
- `ApplyModelToTarget(ModelTarget target, string modelID, bool forceReapply = false)` → `SetModelInConfig(string targetTypeId, string modelID, bool saveConfig = true)`
- `ApplyModelToTargetType(string targetTypeId, string modelID, bool forceReapply = false)` → `SetModelInConfig(string targetTypeId, string modelID, bool saveConfig = true)`
- `ApplyModelToTargetAfterRefresh(ModelTarget target, string modelID, IReadOnlyCollection<string>? bundlesToReload = null)` → `SetModelInConfig(string targetTypeId, string modelID, bool saveConfig = true)`
- `ApplyAllModelsFromConfig(bool forceReapply = false)` → `RefreshAndApplyAllModels()`
- `ApplyModelToAICharacter(string nameKey, string modelID, bool forceReapply = false)` → `SetModelInConfigForAICharacter(string nameKey, string modelID, bool saveConfig = true)`
- `RestoreOriginalModelForTarget(ModelTarget target)` → 直接调用对应 `ModelHandler` 的 `UpdateModelPriorityList()` 方法，或使用 `RefreshAndApplyAllModels()` 刷新所有 handler
- `RestoreOriginalModelForTargetType(string targetTypeId)` → 直接调用对应 `ModelHandler` 的 `UpdateModelPriorityList()` 方法，或使用 `RefreshAndApplyAllModels()` 刷新所有 handler

**注意**: 所有过时的方法都建议使用新的配置管理方法（`SetModelInConfig`、`SetModelInConfigForAICharacter`）和直接操作 `ModelHandler` 的方法（如 `UpdateModelPriorityList()`）。

#### 已删除的方法和属性（不再可用）

**方法**:
- `ApplyAllAICharacterModelsFromConfig(bool forceReapply)` → 使用 `RefreshAndApplyAllModels()` 替代。该方法会遍历所有 AI 角色并应用模型，现在统一使用 `RefreshAndApplyAllModels()` 来处理所有目标类型
- `WaitForRefreshCompletion()` → 已删除。如果需要等待刷新完成，可以订阅 `OnRefreshCompleted` 事件
- `WaitForModelBundleReady(string modelID, CancellationToken cancellationToken = default)` → 已删除。现在模型加载是异步的，可以通过 `RefreshModelList()` 方法并订阅 `OnRefreshCompleted` 事件来等待刷新完成

**属性**:
- `CurrentRefreshingBundles` (IReadOnlyCollection<string>?) → 已删除。不再提供当前正在刷新的 Bundle 列表

**事件**:
- `OnRefreshProgress` (Action<string>?) → 已删除。不再提供刷新进度事件

## MonoBehaviour 类

### ModelHandler

#### 属性
- `Target` (ModelTarget) → `TargetTypeId` (string)

#### 方法
- `Initialize(CharacterMainControl characterMainControl, ModelTarget target = ModelTarget.Character)` → `Initialize(CharacterMainControl characterMainControl, string targetTypeId)`
- `SetTarget(ModelTarget target)` → `TargetTypeId` 是只读属性，只能在初始化时设置。如需更改目标类型，需要重新调用 `Initialize(CharacterMainControl, string targetTypeId)` 方法

#### 访问 TargetTypeId
- 直接使用 `TargetTypeId` 属性（public getter）或 `GetTargetTypeId()` 方法

## 迁移指南

### 基本迁移步骤

1. **替换枚举为字符串标识符**
   ```csharp
   // 旧代码
   ModelTarget.Character
   
   // 新代码
   ModelTargetType.Character
   ```

2. **更新方法调用**
   ```csharp
   // 旧代码
   usingModel.GetModelID(ModelTarget.Character);
   
   // 新代码
   usingModel.GetModelID(ModelTargetType.Character);
   ```

3. **处理 AI 角色**
   ```csharp
   // 旧代码
   usingModel.GetAICharacterModelID("Character_Duck");
   
   // 新代码
   var targetTypeId = ModelTargetType.CreateAICharacterTargetType("Character_Duck");
   usingModel.GetModelID(targetTypeId);
   ```

4. **更新事件处理**
   ```csharp
   // 旧代码
   void OnModelChanged(ModelChangedEventArgs e)
   {
       if (e.Target == ModelTarget.Character) { ... }
   }
   
   // 新代码
   void OnModelChanged(ModelChangedEventArgs e)
   {
       if (e.TargetTypeId == ModelTargetType.Character) { ... }
   }
   ```

### 配置文件迁移

所有配置文件会自动从旧格式迁移到新格式。迁移会在首次加载时自动执行，无需手动操作。

### ModelInfo 配置迁移

在 `bundleinfo.json` 中，`Target` 和 `SupportedAICharacters` 会自动迁移到 `TargetTypes`：

**示例 1**：基本迁移
```json
// 旧格式
{
  "Target": ["Pet", "AICharacter"],
  "SupportedAICharacters": ["Cname_Wolf"]
}

// 迁移后（自动）
{
  "TargetTypes": ["built-in:Pet", "built-in:AICharacter_Cname_Wolf"]
}
```

**示例 2**：没有 AICharacter 标记
```json
// 旧格式
{
  "Target": ["Pet"],
  "SupportedAICharacters": ["Cname_Wolf"]  // 会被忽略
}

// 迁移后（自动）
{
  "TargetTypes": ["built-in:Pet"]
}
```

**示例 3**：AICharacter 标记但 SupportedAICharacters 为空
```json
// 旧格式
{
  "Target": ["Pet", "AICharacter"],
  "SupportedAICharacters": []
}

// 迁移后（自动）
{
  "TargetTypes": ["built-in:Pet"]
}
```

### 向后兼容性

- 所有过时 API 仍然可用，但会在编译时显示警告
- 配置文件会自动从旧格式迁移到新格式
- 现有代码可以继续工作，但建议尽快迁移到新 API

## 版本信息

本文档对应版本：**v1.10.0**

