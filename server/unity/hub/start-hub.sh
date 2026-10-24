#!/usr/bin/env bash
set -e
export DISPLAY=:1
Xvfb :1 -screen 0 1600x1000x24 &
sleep 1
fluxbox >/dev/null 2>&1 &
x11vnc -display :1 -forever -shared -nopw -localhost -quiet &
websockify --web /usr/share/novnc 6080 localhost:5900 &
# /usr/local/bin/unityhub adds --no-sandbox (Electron refuses to run as root without it).
exec unityhub
