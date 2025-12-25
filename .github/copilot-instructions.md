# Duckov Custom Model - GitHub Copilot 指令

## 项目概述
这是一个用于 Duckov 游戏的自定义玩家模型模组，使用 C# 和 Unity 开发，通过 Harmony 进行代码补丁。

## 核心规则

### 语言偏好
- 所有 AI 生成的内容（包括代码注释、提交信息、文档等）应使用中文
- Git 提交信息必须使用中文编写
- 代码注释应使用中文（如果代码中已有中文注释）

### C# 代码风格
- 遵循现有代码风格
- 使用 C# 现代语法特性（如集合初始化器 `[]`、模式匹配等）
- 使用 `netstandard2.1` 目标框架
- 保持与项目现有代码的一致性
- 使用 ReSharper 注释来抑制特定警告（如 `// ReSharper disable MemberCanBeMadeStatic.Local`）

### 代码组织
- 使用 `using` 语句按命名空间分组
- 保持类和方法的结构清晰
- 使用 `namespace` 组织代码，与文件夹结构对应

### 命名规范
- 类名：PascalCase（如 `ModelManager`、`UIConfig`）
- 方法名：PascalCase（如 `UpdateModelBundles`、`LoadConfig`）
- 字段名：camelCase（私有字段）或 PascalCase（公共字段）
- 常量：PascalCase（如 `ModName`、`HarmonyId`）
- 命名空间：与文件夹结构一致（如 `DuckovCustomModel.Managers`）

### 注释规范
- **如果没有要求，仅考虑现有代码中是否存在注释**
- 如果代码中已存在方法注释等，按照现有注释风格添加新注释
- 如果代码中没有注释，则不添加注释（除非用户明确要求）
- 注释应使用中文
- 当前项目代码中通常不包含 XML 文档注释（`/// <summary>`），保持此风格

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
- 所有 Harmony 补丁类应放在 `HarmonyPatches` 文件夹中
- 使用 Harmony 特性标记补丁方法
- 补丁类应包含适当的错误处理

### 配置文件
- 配置文件使用 JSON 格式
- 配置文件类继承自 `ConfigBase` 或实现 `IConfigBase`
- 配置文件应包含验证逻辑（`Validate()` 方法）
- 配置文件位置：`游戏安装路径/ModConfigs/DuckovCustomModel`

## 包管理

### 添加依赖包
- **避免通过直接编辑配置文件来添加包版本**（如 .csproj、package.json 等）
- 优先使用命令行工具通过调用相关程序以添加最新版本或需要的目标版本
- 对于 .NET 项目，使用 `dotnet add package` 命令
- 示例：`dotnet add DuckovCustomModel/DuckovCustomModel.csproj package PackageName`

### NuGet 包
- 项目使用 NuGet 进行包管理
- 主要依赖包：
  - `Lib.Harmony` (2.4.1)
  - `Krafs.Publicizer` (2.3.0)
  - `DuckovGameLibs` (CI 环境使用)

## 本地化
- 本地化文件位于 `Localizations` 文件夹
- 支持语言：中文（简体/繁体）、英语、日语
- 使用 JSON 格式存储本地化字符串
- 本地化类：`Localization`、`LocalizedText`、`LocalizedDropdown`

## 日志记录
- 使用 `ModLogger` 类进行日志记录
- 日志消息应包含模组标签 `[Duckov Custom Model]`
- 使用适当的日志级别：`Log`、`LogWarning`、`LogError`

## 文档与测试

### 生成限制
- **如果没有要求，不要生成说明文档、示例文件**
- **如果没有要求，不要进行编译测试**
- 仅在用户明确要求时才生成文档或进行测试

## Git 提交信息规范

### 提交信息格式
使用 **Conventional Commits** 格式，必须使用中文：

```
<type>(<scope>): <简短描述>
```

### 提交类型 (type)
- `feat`: 新功能
- `fix`: 修复问题
- `refactor`: 重构代码
- `chore`: 构建过程或辅助工具的变动
- `docs`: 文档变更
- `style`: 代码格式（不影响代码运行的变动）
- `perf`: 性能优化
- `test`: 测试相关

### 提交范围 (scope，可选)
- 可以是模块名、文件名或功能区域，如：`carriable`、`config`、`ui`、`model`、`audio`、`harmony` 等
- 使用小写，可以用括号包裹

### 简短描述
- 使用中文，简洁明了地描述本次提交的内容
- 不超过 50 字
- 使用祈使句，如"修复"、"新增"、"优化"等

### 详细描述（可选）
如果提交内容较复杂，可以在简短描述后添加空行和详细描述：

```
## 变更内容

- 具体变更点 1
- 具体变更点 2

## 版本更新

- 版本号更新至 vX.X.X

## 相关文件

- 文件路径 1
- 文件路径 2
```

### 提交信息示例
- `fix: 修复用户登录时的验证逻辑问题`
- `fix(auth): 修复用户登录时的验证逻辑问题`
- `feat: 新增数据导出功能`
- `feat(model): 新增模型搜索功能`
- `refactor: 优化数据库查询逻辑以提升性能`
- `refactor(harmony): 重构 Harmony 补丁加载逻辑`
- `chore: 更新依赖包版本至最新稳定版`

### 注意事项
- 提交信息必须使用中文
- 遵循 Conventional Commits 规范
- 简短描述要清晰表达提交目的
- 复杂变更应包含详细描述

## Git Tag 规范
- **版本号格式**：尽可能遵循语义化版本（SemVer）规范：`主版本号.次版本号.修订号`（可选：`-预发布标识符`）
  - 标准格式示例：`1.8.11`、`2.0.0`、`1.8.9-beta.1`、`1.8.8-fix1`
- **v 前缀**：允许使用 "v" 前缀（如 `v1.8.11`），但**如果项目中已有的格式不一致，以项目现有格式为准**
- **项目格式优先**：在生成 tag 前，应先检查项目中现有的 tag 格式，保持一致
- Tag 信息可以使用中文描述

## Git 操作规范

### ⚠️ 重要警告
**在执行任何 Git 操作（合并、提交、打标签）之前，必须先检查状态，不要随意提交、合并或修改历史！**

### 合并到 main 并打标签的标准流程

#### 1. 检查当前状态（必须）
```bash
git status                    # 检查工作区状态
git branch --show-current     # 确认当前分支
git log --oneline -5          # 查看最近提交
```

#### 2. 确保在正确的分支
- **必须在 `main` 分支上操作**
- 确保工作区干净（没有未提交的更改）
- 如果不在 main 分支，先切换：`git checkout main`

#### 3. 同步远程 main（如果需要）
```bash
git fetch origin
git reset --hard origin/main
```

#### 4. 合并 develop 分支（必须使用详细描述格式）
**格式要求：**
```bash
git merge develop -m "<type>: 合并 develop 到 main，发布版本 <版本号>" \
  -m "" \
  -m "## 变更内容" \
  -m "" \
  -m "- 变更点 1" \
  -m "- 变更点 2" \
  -m "" \
  -m "## 版本更新" \
  -m "" \
  -m "- 版本号更新至 <版本号>"
```

**示例：**
```bash
git merge develop -m "fix: 合并 develop 到 main，发布版本 1.9.5-fix1" \
  -m "" \
  -m "## 变更内容" \
  -m "" \
  -m "- 修复 Markdown 转 Unity 富文本转换器中多级列表处理问题" \
  -m "" \
  -m "## 版本更新" \
  -m "" \
  -m "- 版本号更新至 v1.9.5-fix1"
```

#### 5. 打标签（必须包含描述）
```bash
git tag <版本号> -m "<标签描述>"
```

**示例：**
```bash
git tag 1.9.5-fix1 -m "修复 Markdown 转 Unity 富文本转换器中多级列表处理问题"
```

### 禁止的操作
1. ❌ **不要随意提交**：在历史没清理干净前不要提交
2. ❌ **不要随意 reset**：确认要 reset 到哪里，不要乱改历史
3. ❌ **不要跳过检查**：每次操作前必须检查状态（`git status`、`git log`）
4. ❌ **不要使用简短的合并信息**：必须包含详细描述（## 变更内容、## 版本更新）
5. ❌ **不要在 detached HEAD 状态提交**：先切回正确分支
6. ❌ **不要在历史混乱时操作**：先清理历史，确认状态后再操作

### 操作前检查清单
在执行合并和打标签前，必须确认：
- [ ] 当前在 `main` 分支（`git branch --show-current`）
- [ ] 工作区干净（`git status` 显示 "nothing to commit"）
- [ ] 本地 main 和 origin/main 同步（或已确认差异）
- [ ] 合并信息格式正确，包含详细描述（## 变更内容、## 版本更新）
- [ ] 标签信息正确

### 查看参考格式
查看之前的合并提交格式：
```bash
git show 17b3db0 --format=fuller
```

查看之前的标签格式：
```bash
git show 1.9.5-fix1
```

## 特殊注意事项

### 配置管理
- 配置文件应包含默认值
- 实现 `Validate()` 方法验证配置有效性
  - 返回 true 的含义为验证过程中对配置进行修改
  - 返回 false 的含义为配置没有修改
- 使用 `ConfigManager` 统一管理配置文件

### 日志记录
- 使用 `ModLogger` 类进行日志记录
- 日志消息包含模组标签 `[Duckov Custom Model]`
- 使用适当的日志级别：`Log`、`LogWarning`、`LogError`

### 本地化
- 本地化文件位于 `Localizations` 文件夹
- 本地化类：`Localization`、`LocalizedText`、`LocalizedDropdown`

## 代码审查要点
- 确保代码符合项目风格
- 检查是否有潜在的内存泄漏
- 验证 Harmony 补丁的正确性
- 确保配置文件验证逻辑完整
- 检查错误处理和日志记录
