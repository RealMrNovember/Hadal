#!/usr/bin/env bash
# The backup restore drill on CT 160, from the dev machine (runbook: docs/HADAL_SERVER_INFRASTRUCTURE.md, "Restore drill"):
#
#   bash server/tests/run-restore-drill.sh                                   newest dump in /opt/hadal/backups, code at HEAD
#   bash server/tests/run-restore-drill.sh HEAD /opt/hadal/backups/<file>.sql  a chosen dump
#   KEEP_DB=1 bash server/tests/run-restore-drill.sh                         keep the restored database for a look around
#
# Restores into a throwaway database next to the live one, boots the gateway code on it and loads every player.
# The live database is only read (row counts, READ ONLY); the live gateway is not touched.
set -euo pipefail

HOST="${HADAL_DEPLOY_HOST:-cicibyte@dl380}"
CT="${HADAL_CT:-160}"
DIR="${HADAL_DRILL_DIR:-/opt/hadal-restoredrill}"
REPO="$(git -C "$(dirname "${BASH_SOURCE[0]}")" rev-parse --show-toplevel)"
TREE="${1:-HEAD}"
DUMP="${2:-}"

case "${DIR}" in
  /opt/hadal|/opt/hadal/*) echo "!! HADAL_DRILL_DIR must not be the live tree"; exit 2 ;;
esac

git -C "${REPO}" archive --format=tar.gz "${TREE}" -- shared server/Hadal.Game server/Hadal.Gateway server/Hadal.Game.Tests server/tests \
  | ssh -o BatchMode=yes "${HOST}" "export PATH=\$PATH:/usr/sbin; sudo pct exec ${CT} -- env KEEP_DB=${KEEP_DB:-0} bash -c \
      'rm -rf ${DIR} && mkdir -p ${DIR} && tar --no-same-owner -m -xzf - -C ${DIR} \
       && sed -i \"s/\r\$//\" ${DIR}/server/tests/*.sh && bash ${DIR}/server/tests/ct-restore-drill.sh ${DIR} \"${DUMP}\"'"
