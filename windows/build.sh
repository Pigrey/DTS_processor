#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet run --project windows/Tests/CoreTests.csproj -c Release
dotnet publish windows/Shixiang.csproj -c Release -r win-x64 -o release/native
cp release/native/Shixiang-Tracker-windows-x64.exe release/Shixiang-Tracker-windows-x64.exe
python3 - <<'PY'
import hashlib,pathlib
p=pathlib.Path('release/Shixiang-Tracker-windows-x64.exe')
pathlib.Path('release/SHA256SUMS.txt').write_text(hashlib.sha256(p.read_bytes()).hexdigest()+'  '+p.name+'\n')
PY
