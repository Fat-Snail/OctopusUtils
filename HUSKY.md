# Husky.NET 代码质量检查配置

本项目使用 Husky.NET 作为 Git 钩子管理工具，在提交代码前自动运行代码质量检查。

## 已安装的钩子

### 1. Pre-commit 钩子 (`.husky/pre-commit`)
在提交前执行以下检查：
- 🔍 代码质量检查（通过 `dotnet husky run`）
- 🔄 远程代码同步检查
- 📝 代码格式检查

### 2. Commit-msg 钩子 (`.husky/commit-msg`)
验证提交信息格式：
- 🏷️ 要求使用约定式提交格式
- 🔀 跳过合并提交、回滚提交等特殊情况

## 配置的任务

在 `.husky/task-runner.json` 中定义了以下任务：

### Build Check
```json
{
   "name": "build-check",
   "command": "dotnet",
   "args": [ "build", "--no-restore", "--verbosity", "quiet" ],
   "include": ["*.sln"]
}
```

### Format Check
```json
{
   "name": "format-check",
   "command": "dotnet",
   "args": [ "format", "--verify-no-changes", "--verbosity", "quiet" ],
   "include": ["*.sln"],
   "continueOnError": true
}
```

## 使用说明

### 1. 安装和设置
```bash
# 安装 Husky.NET 工具
dotnet tool install husky

# 初始化 Git 钩子
dotnet husky install
```

### 2. 手动运行检查
```bash
# 运行所有配置的任务
dotnet husky run
```

### 3. 跳过检查（不推荐）
```bash
# 跳过 pre-commit 检查
git commit --no-verify

# 跳过 commit-msg 检查
git commit --no-verify -m "commit message"
```

## 提交信息格式

项目采用约定式提交格式：

```
<类型>(<范围>): <描述>

[可选的正文]

[可选的脚注]
```

### 类型说明
- `feat`: 新功能
- `fix`: 修复 bug
- `chore`: 构建过程或辅助工具的变动
- `docs`: 文档更新
- `style`: 代码格式调整，不影响代码逻辑
- `refactor`: 代码重构，既不是新增功能也不是修复 bug
- `perf`: 性能优化
- `test`: 增加测试或修改测试
- `build`: 构建系统或外部依赖的变动
- `ci`: CI 配置文件或脚本的变动
- `revert`: 回滚之前的提交

### 示例
```bash
git commit -m "feat(auth): 添加登录功能"
git commit -m "fix(consoleex): 修复异步日志输出问题"
git commit -m "docs(readme): 更新安装说明"
```

## 常见问题

### Q: 提交时格式检查失败怎么办？
A: 运行 `dotnet format` 自动修复格式问题，然后重新提交。

### Q: 构建检查失败怎么办？
A: 检查编译错误并修复，确保项目可以正常编译。

### Q: 提交信息格式检查失败怎么办？
A: 按照约定式提交格式重新编写提交信息。

## 故障排除

如果 Git 钩子无法正常工作：

1. 确保已安装 Husky.NET：
   ```bash
   dotnet tool install husky
   ```

2. 重新安装钩子：
   ```bash
   dotnet husky install
   ```

3. 检查 `.git/hooks` 目录下是否有对应的钩子文件。

4. 确保钩子文件有执行权限（Linux/macOS）：
   ```bash
   chmod +x .husky/pre-commit
   chmod +x .husky/commit-msg
   ```

## 自定义配置

### 添加新的检查任务
在 `.husky/task-runner.json` 中添加新任务：

```json
{
   "name": "custom-task",
   "command": "your-command",
   "args": ["--option", "value"],
   "include": ["*.csproj"],
   "continueOnError": false
}
```

### 修改钩子行为
编辑 `.husky/pre-commit` 或 `.husky/commit-msg` 文件来自定义钩子逻辑。

---

## 相关链接
- [Husky.NET 官方文档](https://alirezanet.github.io/Husky.Net/)
- [约定式提交规范](https://www.conventionalcommits.org/zh-hans/v1.0.0/)
- [.NET 格式化工具](https://learn.microsoft.com/en-us/dotnet/core/formatting/)