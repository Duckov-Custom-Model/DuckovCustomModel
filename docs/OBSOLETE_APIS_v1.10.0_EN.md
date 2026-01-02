# Obsolete API List

This document lists all obsolete APIs and their alternatives. These APIs are still available for backward compatibility, but it is recommended to migrate to the new APIs as soon as possible.

## Core Types

### ModelTarget Enum
- **Status**: ⚠️ Obsolete
- **Alternative**: Use `ModelTargetType` string identifiers
- **Description**: The `ModelTarget` enum has been replaced by a string identifier system to support third-party extensions

### ModelTargetExtensions Class
- **Status**: ⚠️ Obsolete
- **Alternative**: Use `ModelTargetType` string identifiers and related methods directly
- **Methods**:
  - `ToTargetTypeId(ModelTarget target, string? aiCharacterName = null)` → Use `ModelTargetType` constants directly (e.g., `ModelTargetType.Character`) or `ModelTargetType.CreateAICharacterTargetType(nameKey)` to create AI character target types
  - `FromTargetTypeId(string targetTypeId)` → No conversion needed, use string identifiers directly

## Configuration Classes

### UsingModel

#### Properties
- `ModelIDs` (Dictionary<ModelTarget, string>) → `TargetTypeModelIDs` (Dictionary<string, string>)
- `AICharacterModelIDs` (Dictionary<string, string>) → `TargetTypeModelIDs` (Dictionary<string, string>)

#### Methods
- `GetModelID(ModelTarget target)` → `GetModelID(string targetTypeId)`
- `SetModelID(ModelTarget target, string modelID)` → `SetModelID(string targetTypeId, string modelID)`
- `GetAICharacterModelID(string nameKey)` → `GetModelID(ModelTargetType.CreateAICharacterTargetType(nameKey))`
- `GetAICharacterModelIDWithFallback(string nameKey)` → Use `GetModelID(ModelTargetType.CreateAICharacterTargetType(nameKey))` to get the specific AI character's model ID, if empty then use `GetModelID(ModelTargetType.AllAICharacters)` as fallback
- `SetAICharacterModelID(string nameKey, string modelID)` → `SetModelID(ModelTargetType.CreateAICharacterTargetType(nameKey), string modelID)`

### HideEquipmentConfig

#### Properties
- `HideEquipment` (Dictionary<ModelTarget, bool>) → `TargetTypeHideEquipment` (Dictionary<string, bool>)
- `HideAICharacterEquipment` (Dictionary<string, bool>) → `TargetTypeHideEquipment` (Dictionary<string, bool>)

#### Methods
- `GetHideEquipment(ModelTarget target)` → `GetHideEquipment(string targetTypeId)`
- `SetHideEquipment(ModelTarget target, bool value)` → `SetHideEquipment(string targetTypeId, bool value)`
- `GetHideAICharacterEquipment(string nameKey)` → If `nameKey` is empty, use `GetHideEquipment(ModelTargetType.AllAICharacters)`; otherwise use `GetHideEquipment(ModelTargetType.CreateAICharacterTargetType(nameKey))`, if not found then fallback to `GetHideEquipment(ModelTargetType.AllAICharacters)`
- `SetHideAICharacterEquipment(string nameKey, bool value)` → `SetHideEquipment(ModelTargetType.CreateAICharacterTargetType(nameKey), bool value)`

### IdleAudioConfig

#### Properties
- `IdleAudioIntervals` (Dictionary<ModelTarget, IdleAudioInterval>) → `TargetTypeIdleAudioIntervals` (Dictionary<string, IdleAudioInterval>)
- `AICharacterIdleAudioIntervals` (Dictionary<string, IdleAudioInterval>) → `TargetTypeIdleAudioIntervals` (Dictionary<string, IdleAudioInterval>)
- `EnableIdleAudio` (Dictionary<ModelTarget, bool>) → `TargetTypeEnableIdleAudio` (Dictionary<string, bool>)
- `AICharacterEnableIdleAudio` (Dictionary<string, bool>) → `TargetTypeEnableIdleAudio` (Dictionary<string, bool>)

#### Methods
- `GetIdleAudioInterval(ModelTarget target)` → `GetIdleAudioInterval(string targetTypeId)`
- `SetIdleAudioInterval(ModelTarget target, float min, float max)` → `SetIdleAudioInterval(string targetTypeId, float min, float max)`
- `IsIdleAudioEnabled(ModelTarget target)` → `IsIdleAudioEnabled(string targetTypeId)`
- `SetIdleAudioEnabled(ModelTarget target, bool enabled)` → `SetIdleAudioEnabled(string targetTypeId, bool enabled)`
- `GetAICharacterIdleAudioInterval(string nameKey)` → If `nameKey` is empty, use `GetIdleAudioInterval(ModelTargetType.AllAICharacters)`; otherwise use `GetIdleAudioInterval(ModelTargetType.CreateAICharacterTargetType(nameKey))`, if returns default value (30-45 seconds) then fallback to `GetIdleAudioInterval(ModelTargetType.AllAICharacters)`
- `SetAICharacterIdleAudioInterval(string nameKey, float min, float max)` → `SetIdleAudioInterval(ModelTargetType.CreateAICharacterTargetType(nameKey), float min, float max)`
- `IsAICharacterIdleAudioEnabled(string nameKey)` → If `nameKey` is empty, use `IsIdleAudioEnabled(ModelTargetType.AllAICharacters)`; otherwise use `IsIdleAudioEnabled(ModelTargetType.CreateAICharacterTargetType(nameKey))`, if not found then fallback to `IsIdleAudioEnabled(ModelTargetType.AllAICharacters)`
- `SetAICharacterIdleAudioEnabled(string nameKey, bool enabled)` → `SetIdleAudioEnabled(ModelTargetType.CreateAICharacterTargetType(nameKey), bool enabled)`

### ModelAudioConfig

#### Properties
- `EnableModelAudio` (Dictionary<ModelTarget, bool>) → `TargetTypeEnableModelAudio` (Dictionary<string, bool>)
- `AICharacterEnableModelAudio` (Dictionary<string, bool>) → `TargetTypeEnableModelAudio` (Dictionary<string, bool>)

#### Methods
- `IsModelAudioEnabled(ModelTarget target)` → `IsModelAudioEnabled(string targetTypeId)`
- `SetModelAudioEnabled(ModelTarget target, bool enabled)` → `SetModelAudioEnabled(string targetTypeId, bool enabled)`
- `IsAICharacterModelAudioEnabled(string nameKey)` → If `nameKey` is empty, use `IsModelAudioEnabled(ModelTargetType.AllAICharacters)`; otherwise use `IsModelAudioEnabled(ModelTargetType.CreateAICharacterTargetType(nameKey))`, if not found then fallback to `IsModelAudioEnabled(ModelTargetType.AllAICharacters)`
- `SetAICharacterModelAudioEnabled(string nameKey, bool enabled)` → `SetModelAudioEnabled(ModelTargetType.CreateAICharacterTargetType(nameKey), bool enabled)`

## Data Classes

### ModelInfo

#### Properties
- `Target` (ModelTarget[]) → `TargetTypes` (string[])
- `SupportedAICharacters` (string[]) → `TargetTypes` (string[])

#### Methods
- `CompatibleWithType(ModelTarget modelTarget)` → `CompatibleWithTargetType(string targetTypeId)`

**Note**: The `Target` and `SupportedAICharacters` properties are automatically migrated to `TargetTypes` in the `Validate()` method.

**Migration Rules**:
- `Character` and `Pet` in `Target` are converted to corresponding target type IDs (`built-in:Character`, `built-in:Pet`)
- `AICharacter` in `Target` **is not converted**, it is only a marker indicating that `SupportedAICharacters` needs to be processed
- Only when `Target` contains the `AICharacter` marker, values in `SupportedAICharacters` are converted to target type IDs
- If `Target` does not contain the `AICharacter` marker, even if `SupportedAICharacters` has values, no AI character target types will be added
- `"*"` in `SupportedAICharacters` is converted to `built-in:AICharacter_*`, other values are converted to `built-in:AICharacter_<character name>`

### ModelChangedEventArgs

#### New Properties (Not Obsolete)
- `Handler` (Object?) - The `ModelHandler` instance that triggered the event
- `TargetTypeId` (string) - Target type ID
- `ModelID` (string?) - Model ID
- `ModelName` (string?) - Model name
- `IsRestored` (bool) - Whether the original model was restored

#### Obsolete Properties
- `Target` (ModelTarget) → `TargetTypeId` (string)
- `AICharacterNameKey` (string?) → Use `ModelTargetType.ExtractAICharacterName(TargetTypeId)` to extract
- `HandlerCount` (int) → Obsolete, no longer used (not needed in event-driven mechanism)
- `Success` (bool) → Obsolete, no longer used

### TargetInfo

#### Properties
- `TargetType` (ModelTarget) → `TargetTypeId` (string)
- `AICharacterNameKey` (string) → Use `GetAICharacterNameKey()` method to get

#### Methods
- Use `GetTargetTypeId()` method to get the current target type ID
- Use `GetAICharacterNameKey()` method to get the AI character name

## Manager Classes

### ModelManager

#### Methods
- `InitializeModelHandler(CharacterMainControl characterMainControl, ModelTarget target = ModelTarget.Character)` → `InitializeModelHandler(CharacterMainControl characterMainControl, string targetTypeId)`
- `GetAllModelHandlers(ModelTarget target)` → `GetAllModelHandlersByTargetType(string targetTypeId)`
- `GetAICharacterModelHandlers(string nameKey)` → `GetAllModelHandlersByTargetType(ModelTargetType.CreateAICharacterTargetType(nameKey))`

### ModelListManager

#### Methods
- `ApplyModelToTarget(ModelTarget target, string modelID, bool forceReapply = false)` → `SetModelInConfig(string targetTypeId, string modelID, bool saveConfig = true)`
- `ApplyModelToTargetType(string targetTypeId, string modelID, bool forceReapply = false)` → `SetModelInConfig(string targetTypeId, string modelID, bool saveConfig = true)`
- `ApplyModelToTargetAfterRefresh(ModelTarget target, string modelID, IReadOnlyCollection<string>? bundlesToReload = null)` → `SetModelInConfig(string targetTypeId, string modelID, bool saveConfig = true)`
- `ApplyAllModelsFromConfig(bool forceReapply = false)` → `RefreshAndApplyAllModels()`
- `ApplyModelToAICharacter(string nameKey, string modelID, bool forceReapply = false)` → `SetModelInConfigForAICharacter(string nameKey, string modelID, bool saveConfig = true)`
- `RestoreOriginalModelForTarget(ModelTarget target)` → Call the corresponding `ModelHandler`'s `UpdateModelPriorityList()` method directly, or use `RefreshAndApplyAllModels()` to refresh all handlers
- `RestoreOriginalModelForTargetType(string targetTypeId)` → Call the corresponding `ModelHandler`'s `UpdateModelPriorityList()` method directly, or use `RefreshAndApplyAllModels()` to refresh all handlers

**Note**: All obsolete methods are recommended to use new configuration management methods (`SetModelInConfig`, `SetModelInConfigForAICharacter`) and methods that directly operate on `ModelHandler` (such as `UpdateModelPriorityList()`).

#### Removed Methods and Properties (No Longer Available)

**Methods**:
- `ApplyAllAICharacterModelsFromConfig(bool forceReapply)` → Use `RefreshAndApplyAllModels()` instead. This method used to iterate through all AI characters and apply models, now unified to use `RefreshAndApplyAllModels()` to handle all target types
- `WaitForRefreshCompletion()` → Removed. If you need to wait for refresh completion, you can subscribe to the `OnRefreshCompleted` event
- `WaitForModelBundleReady(string modelID, CancellationToken cancellationToken = default)` → Removed. Model loading is now asynchronous, you can wait for refresh completion by calling `RefreshModelList()` and subscribing to the `OnRefreshCompleted` event

**Properties**:
- `CurrentRefreshingBundles` (IReadOnlyCollection<string>?) → Removed. No longer provides the list of currently refreshing bundles

**Events**:
- `OnRefreshProgress` (Action<string>?) → Removed. No longer provides refresh progress events

## MonoBehaviour Classes

### ModelHandler

#### Properties
- `Target` (ModelTarget) → `TargetTypeId` (string)

#### Methods
- `Initialize(CharacterMainControl characterMainControl, ModelTarget target = ModelTarget.Character)` → `Initialize(CharacterMainControl characterMainControl, string targetTypeId)`
- `SetTarget(ModelTarget target)` → `TargetTypeId` is a read-only property and can only be set during initialization. To change the target type, re-call the `Initialize(CharacterMainControl, string targetTypeId)` method

#### Accessing TargetTypeId
- Use the `TargetTypeId` property (public getter) or `GetTargetTypeId()` method directly

## Migration Guide

### Basic Migration Steps

1. **Replace Enum with String Identifiers**
   ```csharp
   // Old code
   ModelTarget.Character
   
   // New code
   ModelTargetType.Character
   ```

2. **Update Method Calls**
   ```csharp
   // Old code
   usingModel.GetModelID(ModelTarget.Character);
   
   // New code
   usingModel.GetModelID(ModelTargetType.Character);
   ```

3. **Handle AI Characters**
   ```csharp
   // Old code
   usingModel.GetAICharacterModelID("Character_Duck");
   
   // New code
   var targetTypeId = ModelTargetType.CreateAICharacterTargetType("Character_Duck");
   usingModel.GetModelID(targetTypeId);
   ```

4. **Update Event Handlers**
   ```csharp
   // Old code
   void OnModelChanged(ModelChangedEventArgs e)
   {
       if (e.Target == ModelTarget.Character) { ... }
   }
   
   // New code
   void OnModelChanged(ModelChangedEventArgs e)
   {
       if (e.TargetTypeId == ModelTargetType.Character) { ... }
   }
   ```

### Configuration File Migration

All configuration files are automatically migrated from old format to new format. Migration is performed automatically on first load, no manual operation required.

### ModelInfo Configuration Migration

In `bundleinfo.json`, `Target` and `SupportedAICharacters` are automatically migrated to `TargetTypes`:

**Example 1**: Basic Migration
```json
// Old format
{
  "Target": ["Pet", "AICharacter"],
  "SupportedAICharacters": ["Cname_Wolf"]
}

// After migration (automatic)
{
  "TargetTypes": ["built-in:Pet", "built-in:AICharacter_Cname_Wolf"]
}
```

**Example 2**: No AICharacter Marker
```json
// Old format
{
  "Target": ["Pet"],
  "SupportedAICharacters": ["Cname_Wolf"]  // Will be ignored
}

// After migration (automatic)
{
  "TargetTypes": ["built-in:Pet"]
}
```

**Example 3**: AICharacter Marker but SupportedAICharacters is Empty
```json
// Old format
{
  "Target": ["Pet", "AICharacter"],
  "SupportedAICharacters": []
}

// After migration (automatic)
{
  "TargetTypes": ["built-in:Pet"]
}
```

### Backward Compatibility

- All obsolete APIs are still available, but will show warnings at compile time
- Configuration files are automatically migrated from old format to new format
- Existing code can continue to work, but it is recommended to migrate to new APIs as soon as possible

## Version Information

This document corresponds to version: **v1.10.0**

