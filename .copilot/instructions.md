# Duckov Custom Model - GitHub Copilot 指令

> 此文件用于 GitHub Copilot 的自定义指令。Copilot 会自动读取此文件来了解项目规范。

## 项目概述
这是一个用于 Duckov 游戏的自定义玩家模型模组，使用 C# 和 Unity 开发，通过 Harmony 进行代码补丁。

## 核心规则

### 语言偏好
- **所有 AI 生成的内容必须使用中文**（代码注释、提交信息、文档等）
- Git 提交信息必须使用中文编写
- 代码注释应使用中文

### 代码风格
- 遵循现有代码风格
- 使用 C# 现代语法特性（集合初始化器 `[]`、模式匹配等）
- 目标框架：`netstandard2.1`
- 使用 ReSharper 注释抑制特定警告（如 `// ReSharper disable MemberCanBeMadeStatic.Local`）

### 命名规范
- 类名、方法名、常量：PascalCase
- 私有字段：camelCase
- 公共字段：PascalCase
- 命名空间：与文件夹结构一致

### 注释规范
- **如果没有要求，仅考虑现有代码中是否存在注释**
- 如果代码中已有注释，按照现有风格添加
- 如果代码中没有注释，则不添加（除非用户明确要求）
- 注释使用中文
- **不使用 XML 文档注释**（`/// <summary>`）

### 项目结构
- `DuckovCustomModel`: 主项目（游戏模块和 UI）
- `DuckovCustomModel.Core`: 核心数据结构和工具类
- `DuckovCustomModel.ModLoader`: 模组加载器
- `DuckovCustomModelRegister`: 模组模型注册器

## 技术栈

### 核心依赖
- **Harmony** (Lib.Harmony 2.4.1): 代码补丁
- **Publicizer** (Krafs.Publicizer 2.3.0): 访问私有成员
- **Unity**: 游戏引擎
- **Newtonsoft.Json**: JSON 序列化

### Harmony 补丁
- 补丁类放在 `HarmonyPatches` 文件夹
- 使用 `HarmonyPatch` 特性标记
- 包含适当的错误处理

### 配置文件
- 使用 JSON 格式
- 继承 `ConfigBase` 或实现 `IConfigBase`
- 实现 `Validate()` 方法（返回 true 表示配置被修改，false 表示未修改）
- 配置文件位置：`游戏安装路径/ModConfigs/DuckovCustomModel`

## 包管理

### 添加依赖包
- **不要直接编辑 .csproj 文件添加包版本**
- 使用命令行工具：`dotnet add DuckovCustomModel/DuckovCustomModel.csproj package PackageName`
- 优先使用最新稳定版本或用户指定的目标版本

## 特殊注意事项

### Unity 相关
- 使用 `Object.Destroy` 而不是 `Destroy`
- 使用 `DontDestroyOnLoad` 保持对象在场景切换时不被销毁
- 注意 Unity 生命周期方法（`Awake`、`Start`、`Update` 等）

### Harmony 补丁
- 确保补丁方法有适当的错误处理
- 使用 `HarmonyPatch` 特性标记
- 注意补丁的优先级和条件

### 资源管理
- AssetBundle 加载后应正确卸载
- 注意内存泄漏，及时释放不再使用的资源
- 使用 `AssetBundleManager` 管理资源加载

### 配置管理
- 配置文件应包含默认值
- `Validate()` 方法：返回 true = 配置被修改，返回 false = 配置未修改
- 使用 `ConfigManager` 统一管理配置文件

### 模型管理
- 模型包应包含 `bundleinfo.json` 配置文件
- 支持增量更新，使用哈希缓存机制
- 模型目标类型：Character、Pet、AICharacter

### 日志记录
- 使用 `ModLogger` 类进行日志记录
- 日志消息包含模组标签 `[Duckov Custom Model]`
- 使用适当的日志级别：`Log`、`LogWarning`、`LogError`

### 本地化
- 本地化文件位于 `Localizations` 文件夹
- 支持语言：中文（简体/繁体）、英语、日语
- 使用 JSON 格式存储本地化字符串
- 本地化类：`Localization`、`LocalizedText`、`LocalizedDropdown`

## 生成限制

### 不要自动生成
- **如果没有要求，不要生成说明文档、示例文件**
- **如果没有要求，不要进行编译测试**
- 仅在用户明确要求时才生成文档或进行测试

## Git 提交信息规范

### 格式
使用 **Conventional Commits** 格式，必须使用中文：
```
<type>(<scope>): <简短描述>
```

### 提交类型
- `feat`: 新功能
- `fix`: 修复问题
- `refactor`: 重构代码
- `chore`: 构建过程或辅助工具的变动
- `docs`: 文档变更
- `style`: 代码格式（不影响代码运行的变动）
- `perf`: 性能优化
- `test`: 测试相关

### 提交范围（可选）
- 模块名、文件名或功能区域：`carriable`、`config`、`ui`、`model`、`audio`、`harmony` 等
- 使用小写

### 示例
- `fix: 修复用户登录时的验证逻辑问题`
- `fix(auth): 修复用户登录时的验证逻辑问题`
- `feat: 新增数据导出功能`
- `feat(model): 新增模型搜索功能`
- `refactor: 优化数据库查询逻辑以提升性能`
- `chore: 更新依赖包版本至最新稳定版`

## Git Tag 规范
- 版本号格式：遵循语义化版本（SemVer）：`主版本号.次版本号.修订号`（可选：`-预发布标识符`）
- 示例：`1.8.11`、`2.0.0`、`1.8.9-beta.1`、`1.8.8-fix1`
- 允许使用 "v" 前缀（如 `v1.8.11`），但需与项目现有格式保持一致
- 生成 tag 前应先检查项目中现有的 tag 格式
- Tag 信息可以使用中文描述

