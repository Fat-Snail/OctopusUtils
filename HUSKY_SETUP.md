# Husky.NET 代码质量检查配置

本项目已配置 Husky.NET 来自动执行代码质量检查，确保提交的代码符合团队标准。

## 🚀 快速开始

### 首次设置（仅一次）

1. **安装 dotnet tools**：
   ```bash
   dotnet tool install -g dotnet-format
   dotnet tool install husky
   ```

2. **安装 Git hooks**：
   ```bash
   dotnet husky install
   ```

### 日常使用

Git hooks 会在以下时机自动运行：

#### 📝 提交前检查 (pre-commit)

- **代码格式检查**：使用 `dotnet format` 检查代码格式
- **项目构建检查**：验证项目能正常编译
- **远程同步检查**：确保本地代码与远程同步

#### 📋 提交信息检查 (commit-msg)

验证提交信息格式，要求遵循 [Conventional Commits](https://www.conventionalcommits.org/) 规范：

```
<类型>(<范围>): <描述>

[可选的正文]

[可选的脚注]
```

**允许的类型：**
- `feat`: 新功能
- `fix`: 修复 bug
- `chore`: 构建过程或辅助工具的变动
- `docs`: 文档更新
- `style`: 代码格式（不影响代码运行的变动）
- `refactor`: 重构（既不是新增功能，也不是修改bug的代码变动）
- `perf`: 性能优化
- `test`: 增加测试或修改测试
- `build`: 构建系统或外部依赖的变动
- `ci`: CI配置文件和脚本的变动
- `revert`: 回滚之前的提交

**示例：**
```
feat(auth): 添加JWT身份验证功能

- 实现JWT token生成和验证
- 添加登录和注册API端点
- 集成Swagger文档

Closes #123
```

## 🔧 配置文件说明

### `.husky/task-runner.json`

定义了自动运行的任务：

```json
{
  "tasks": [
    {
      "name": "build-check",
      "command": "dotnet",
      "args": [ "build", "--no-restore", "--verbosity", "quiet" ],
      "include": ["*.sln"],
      "continueOnError": false
    },
    {
      "name": "format-check",
      "command": "dotnet",
      "args": [ "format", "--verify-no-changes", "--verbosity", "quiet", "--exclude", "**/bin/**", "--exclude", "**/obj/**" ],
      "include": ["*.sln"],
      "continueOnError": true
    }
  ]
}
```

**任务说明：**
- `build-check`: 检查项目是否能正常编译（失败则阻止提交）
- `format-check`: 检查代码格式（失败但允许继续提交）

### `.husky/pre-commit`

提交前执行的钩子脚本：
1. 检查是否为合并操作（合并时跳过检查）
2. 检查本地代码是否与远程同步
3. 运行 Husky.NET 任务

### `.husky/commit-msg`

提交信息检查脚本：
1. 跳过特殊提交类型（合并、回滚等）
2. 验证普通提交的信息格式
3. 提供格式错误的详细说明

## 🛠️ 自定义配置

### 修改格式检查规则

编辑 `.husky/task-runner.json` 文件：

```json
{
  "tasks": [
    {
      "name": "format-check",
      "command": "dotnet",
      "args": [ 
        "format", 
        "--verify-no-changes", 
        "--verbosity", "quiet",
        "--exclude", "**/bin/**", 
        "--exclude", "**/obj/**" 
      ],
      "include": ["*.sln"],
      "continueOnError": false  // 改为 false 使格式错误阻止提交
    }
  ]
}
```

### 添加新的检查任务

1. 在 `task-runner.json` 中添加新任务
2. 在 `.husky/pre-commit` 中添加相应的逻辑

### 修改提交信息规则

编辑 `.husky/commit-msg` 文件中的正则表达式：

```bash
# 修改类型列表
if ! echo "$commit_msg" | grep -E "^(your|custom|types)(\(.+\))?[:：].{1,}" > /dev/null; then
  echo "❌ 错误：提交信息格式不正确。"
  echo "📝 请使用以下格式：<类型>(<范围>): <描述>"
  echo "🏷️  类型可以是：your, custom, types"
  exit 1
fi
```

## 🔧 故障排除

### 格式检查失败

如果 `dotnet format` 检查失败：

1. **自动修复格式**：
   ```bash
   dotnet format
   ```

2. **检查特定项目**：
   ```bash
   dotnet format YourProject.csproj
   ```

3. **查看格式差异**：
   ```bash
   git diff
   ```

### 构建检查失败

如果项目构建失败：

1. **查看详细错误**：
   ```bash
   dotnet build --verbosity normal
   ```

2. **清理并重建**：
   ```bash
   dotnet clean
   dotnet build
   ```

### 提交信息格式错误

如果提交信息格式不正确：

1. **使用正确的格式重新提交**：
   ```bash
   git commit -m "feat(scope): 添加新功能描述"
   ```

2. **修改最后一次提交信息**：
   ```bash
   git commit --amend -m "feat(scope): 正确的提交信息"
   ```

### 跳过检查（紧急情况）

⚠️ **警告：仅在紧急情况下使用**

#### 跳过 pre-commit 检查
```bash
git commit --no-verify -m "紧急提交"
```

#### 跳过 commit-msg 检查
```bash
git commit --no-verify -m "任意格式的提交信息"
```

## 📋 检查清单

在提交代码前，请确保：

- [ ] 代码已经通过 `dotnet format` 格式化
- [ ] 项目能够正常编译
- [ ] 本地代码已与远程同步（除非是新分支）
- [ ] 提交信息符合 Conventional Commits 格式
- [ ] 已经测试了相关功能

## 🔄 更新配置

当添加新的开发工具或修改检查规则时：

1. 更新相应的配置文件
2. 提交配置文件到 Git
3. 团队成员运行 `git pull` 获取最新配置
4. 重新安装 hooks（如果需要）：
   ```bash
   dotnet husky install
   ```

## 📚 参考资源

- [Husky.NET 官方文档](https://alirezanet.github.io/Husky.Net/)
- [Conventional Commits 规范](https://www.conventionalcommits.org/)
- [dotnet format 文档](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-format)

## 🤝 团队协作

当新成员加入项目时，请指引他们：

1. 克隆项目后，进入项目目录
2. 安装必要的 dotnet tools
3. 运行 `dotnet husky install` 激活 Git hooks
4. 阅读本配置文档

---

通过这些自动化检查，我们可以确保代码质量和团队协作效率。如果有任何问题或建议，请随时提出改进建议！