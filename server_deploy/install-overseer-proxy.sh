#!/usr/bin/env bash
# Renders nginx-extension-hadal-overseer.conf on kale with the Overseer proxy key taken from the
# CT's .env. The key travels CT → this machine → kale through pipes only; it is never printed.
# Backs up the extension directory first, tests the config and rolls back if nginx -t fails.
#
#   bash server_deploy/install-overseer-proxy.sh
set -euo pipefail

DL380="${DL380:-cicibyte@dl380}"
CT="${CT:-160}"
KALE="${KALE:-root@100.120.224.33}"
EXT="/www/server/panel/vhost/nginx/extension/hadal.cicibyte.com"
HERE="$(cd "$(dirname "$0")" && pwd)"

key="$(ssh -o BatchMode=yes "${DL380}" "export PATH=\$PATH:/usr/sbin; sudo pct exec ${CT} -- sh -c 'grep ^HADAL_ADMIN_PROXY_KEY= /opt/hadal/server/.env | cut -d= -f2-'")"
if [[ ${#key} -lt 32 ]]; then
  echo "!! HADAL_ADMIN_PROXY_KEY missing or short on the CT" >&2
  exit 1
fi

{ printf '%s\n' "${key}"; cat "${HERE}/nginx-extension-hadal-overseer.conf"; } | ssh -o BatchMode=yes "${KALE}" "bash -c '
  set -euo pipefail
  read -r key
  stamp=\$(date +%Y%m%d-%H%M%S)
  mkdir -p /root/hadal-backups
  tar -czf /root/hadal-backups/nginx-ext-\${stamp}.tgz -C ${EXT} .
  umask 077
  sed \"s|__HADAL_ADMIN_PROXY_KEY__|\${key}|\" > ${EXT}/hadal-overseer.conf
  mkdir -p /www/wwwroot/hadal.cicibyte.com/download
  if /www/server/nginx/sbin/nginx -t 2>/tmp/hadal-nginx-test.log; then
    /etc/init.d/nginx reload
    echo \"installed (backup /root/hadal-backups/nginx-ext-\${stamp}.tgz)\"
  else
    rm -f ${EXT}/hadal-overseer.conf
    cat /tmp/hadal-nginx-test.log
    echo \"!! nginx -t failed, extension removed\" >&2
    exit 1
  fi
'"
