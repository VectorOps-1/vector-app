#!/usr/bin/env bash
set -euo pipefail

PORT="${PORT:-5000}"
PROJECT="${PROJECT:-vector-app-local.csproj}"
PULL="${PULL:-1}"
FORCE_PORT="${FORCE_PORT:-0}"

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT"

echo "Vector dev launcher"
echo "Repo: $ROOT"
echo "Port: $PORT"

if [[ "$PULL" == "1" ]]; then
    echo "Pulling latest main..."
    git pull --ff-only origin main
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
            echo "Leaving PID $PID ($COMMAND_NAME) running. Set FORCE_PORT=1 to stop it anyway."
        fi
    done

    sleep 1
fi

echo "Starting app..."
dotnet watch run --project "$PROJECT" --urls "http://0.0.0.0:$PORT"
