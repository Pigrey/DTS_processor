#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
npm run build
rm -rf desktop/web
mkdir -p desktop/web release
cp -R dist/. desktop/web/
cd desktop
GOOS=windows GOARCH=amd64 CGO_ENABLED=0 go build -trimpath -ldflags='-s -w' -o ../release/Shixiang-Tracker-windows-x64.exe .
