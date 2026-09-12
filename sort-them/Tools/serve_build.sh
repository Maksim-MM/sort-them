#!/bin/sh
# Раздаёт WebGL-сборку по локальной сети для теста на телефоне (профайлер, FPS).
# Использование: Tools/serve_build.sh [папка] [порт]   по умолчанию Builds/WebGL_Profile 8000
DIR="${1:-Builds/WebGL_Profile}"; PORT="${2:-8000}"
IP=$(ipconfig getifaddr en0 2>/dev/null || ipconfig getifaddr en1)
echo "Открой на телефоне: http://$IP:$PORT/  (телефон и Mac в одной Wi-Fi сети)"
cd "$DIR" && python3 -m http.server "$PORT"
