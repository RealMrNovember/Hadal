#!/bin/sh
# Hub re-registers itself as the unityhub:// handler with `Exec=unityhub %U`. As root, Electron
# aborts without --no-sandbox, so the sign-in deep link silently died. This wrapper sits earlier
# in PATH than /usr/bin/unityhub, logs deep links and always adds --no-sandbox.
case "$1" in unityhub://*) echo "$1" >> /tmp/hub-links.log ;; esac
exec /usr/bin/unityhub --no-sandbox "$@"
