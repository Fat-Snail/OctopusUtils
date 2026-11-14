#!/usr/bin/env bash
set -Eeuo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT_DIR/samples/OctopusEx.Sample.WebApi/OctopusEx.Sample.WebApi.csproj"
PORT="${SAMPLE_PORT:-5000}"
REDIS_CONNECTION="${Redis__ConnectionString:-${REDIS_CONNECTION_STRING:-}}"

command -v dotnet >/dev/null || { echo "未找到 dotnet" >&2; exit 1; }

echo "构建 Sample..."
dotnet build "$PROJECT" --no-restore --verbosity:quiet
echo "启动地址：http://localhost:${PORT}/"
echo "停止服务：Ctrl+C"

if [[ -n "$REDIS_CONNECTION" ]]; then
  echo "Redis：$REDIS_CONNECTION"
  Redis__ConnectionString="$REDIS_CONNECTION" ASPNETCORE_URLS="http://127.0.0.1:${PORT}" \
    dotnet run --project "$PROJECT" --no-build
else
  echo "Redis：未配置，使用 InMemory 分布式锁"
  ASPNETCORE_URLS="http://127.0.0.1:${PORT}" \
    dotnet run --project "$PROJECT" --no-build
fi
