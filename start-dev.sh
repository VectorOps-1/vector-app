#!/usr/bin/env bash
set -euo pipefail

PORT="${PORT:-5000}"
PROJECT="${PROJECT:-vector-app-local.csproj}"
PULL="${PULL:-0}"
FORCE_PORT="${FORCE_PORT:-0}"
WATCH="${WATCH:-0}"
NO_BUILD="${NO_BUILD:-0}"

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT"

echo "AcuityOps local launcher"
echo "Repo: $ROOT"
echo "Port: $PORT"

if [[ "$PULL" == "1" ]]; then
    echo "Pulling latest main..."
    git pull --ff-only origin main
else
    echo "Skipping git pull. Set PULL=1 when you explicitly want the latest remote main."
fi

find_port_pids() {
    if command -v lsof >/dev/null 2>&1; then
        lsof -ti tcp:"$PORT" 2>/dev/null || true
        return
    fi

    if command -v fuser >/dev/null 2>&1; then
        fuser "$PORT"/tcp 2>/dev/null || true
        return
    fi
}

mapfile -t PORT_PIDS < <(find_port_pids | tr ' ' '\n' | sed '/^$/d' | sort -u)

if (( ${#PORT_PIDS[@]} > 0 )); then
    echo "Port $PORT is already in use."

    for PID in "${PORT_PIDS[@]}"; do
        COMMAND_NAME="$(ps -p "$PID" -o comm= 2>/dev/null || true)"

        if [[ "$COMMAND_NAME" == dotnet* || "$FORCE_PORT" == "1" ]]; then
            echo "Stopping PID $PID ($COMMAND_NAME)..."
            kill "$PID" 2>/dev/null || true
        else
            echo "Port $PORT is already used by PID $PID ($COMMAND_NAME). Close it or rerun with FORCE_PORT=1." >&2
            exit 1
        fi
    done

    sleep 1
fi

echo
echo "Open:"
echo "  http://127.0.0.1:$PORT/CompanyLogin?workspace=x-med"
echo
echo "Starting app..."

DOTNET_ARGS=()
if [[ "$WATCH" == "1" ]]; then
    DOTNET_ARGS+=(watch run)
else
    DOTNET_ARGS+=(run)
fi

if [[ "$NO_BUILD" == "1" ]]; then
    DOTNET_ARGS+=(--no-build)
fi

DOTNET_ARGS+=(--project "$PROJECT" --urls "http://0.0.0.0:$PORT")

dotnet "${DOTNET_ARGS[@]}"
