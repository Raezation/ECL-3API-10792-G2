#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
dotnet restore
dotnet run --project src/BibliotecaWeb --no-launch-profile --urls http://127.0.0.1:5082
