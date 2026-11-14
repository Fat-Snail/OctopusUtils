#!/usr/bin/env bash
set -Eeuo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT_DIR/samples/OctopusEx.Sample.WebApi/OctopusEx.Sample.WebApi.csproj"
PORT="${SAMPLE_PORT:-5097}"
URL="http://127.0.0.1:${PORT}"
REDIS_CONTAINER="octopusutils-sample-redis"
REDIS_STARTED=0
APP_PID=""

cleanup() {
  [[ -n "$APP_PID" ]] && kill "$APP_PID" 2>/dev/null || true
  if [[ "$REDIS_STARTED" == "1" ]]; then
    docker rm -f "$REDIS_CONTAINER" >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT INT TERM

command -v dotnet >/dev/null || { echo "未找到 dotnet" >&2; exit 1; }
command -v curl >/dev/null || { echo "未找到 curl" >&2; exit 1; }

REDIS_CONNECTION="${Redis__ConnectionString:-${REDIS_CONNECTION_STRING:-}}"
if [[ -z "$REDIS_CONNECTION" ]] && command -v docker >/dev/null && docker info >/dev/null 2>&1; then
  if ! docker ps --format '{{.Names}}' | grep -qx "$REDIS_CONTAINER"; then
    docker run -d --rm --name "$REDIS_CONTAINER" -p "${REDIS_PORT:-6379}:6379" redis:7-alpine >/dev/null
    REDIS_STARTED=1
    for _ in {1..20}; do
      docker exec "$REDIS_CONTAINER" redis-cli ping 2>/dev/null | grep -q PONG && break
      sleep 0.5
    done
  fi
  REDIS_CONNECTION="127.0.0.1:${REDIS_PORT:-6379}"
fi

echo "[1/4] 构建 Sample"
dotnet build "$PROJECT" --no-restore --verbosity:quiet

echo "[2/4] 启动 Sample（端口 ${PORT}）"
if [[ -n "$REDIS_CONNECTION" ]]; then
  Redis__ConnectionString="$REDIS_CONNECTION" ASPNETCORE_URLS="$URL" dotnet run --project "$PROJECT" --no-build >"${TMPDIR:-/tmp}/octopus-sample-${PORT}.log" 2>&1 &
  echo "使用 Redis: $REDIS_CONNECTION"
else
  ASPNETCORE_URLS="$URL" dotnet run --project "$PROJECT" --no-build >"${TMPDIR:-/tmp}/octopus-sample-${PORT}.log" 2>&1 &
  echo "未检测到 Redis，使用 InMemory 锁"
fi
APP_PID=$!

echo "[3/4] 等待服务就绪"
for _ in {1..40}; do
  curl -fsS "$URL/health/live" >/dev/null 2>&1 && break
  sleep 0.5
done
curl -fsS "$URL/health/live" >/dev/null

echo "[4/4] 执行冒烟测试"
curl -fsS "$URL/" | grep -q "WebCore 能力展示"
curl -fsS "$URL/openapi/v1.json" | grep -q '"openapi"'
curl -fsS "$URL/coordination/demo" | grep -q '"contenders"'
curl -fsS "$URL/octopus/diagnostics" >/dev/null
echo "✅ Sample 启动、健康检查、OpenAPI、诊断和分布式锁演示全部通过"
echo "日志：${TMPDIR:-/tmp}/octopus-sample-${PORT}.log"
