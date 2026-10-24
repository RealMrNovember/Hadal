#!/bin/sh
# Chrome refuses to run as root without --no-sandbox; the container has no user namespaces anyway.
exec /usr/bin/google-chrome-stable --no-sandbox --no-first-run --no-default-browser-check \
  --user-data-dir=/root/.config/hub-chrome "$@"
